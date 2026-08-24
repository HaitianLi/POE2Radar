using POE2Radar.Core;
using POE2Radar.Core.Game;

namespace POE2Radar.Overlay.Zoom;

/// <summary>
/// Opt-in camera zoom un-clamp for PoE2 — the ONE write-side feature in POE2Radar.
///
/// <para>How the game limits zoom: a <c>minss xmm1, [rip+const]</c> instruction clamps the camera
/// distance. This module rewrites that clamp to a user-chosen value (see <see cref="Apply"/>): it
/// allocates a small page of executable memory near the game module, writes a trampoline that forces
/// the clamp, and redirects the original instruction to it with a 5-byte JMP (+ NOPs). The patch is
/// fully reversible (<see cref="Remove"/> restores the original bytes) and lives only as long as the
/// game process.</para>
///
/// <para>This violates the project's "external, read-only" rule by design — it is opt-in, default
/// OFF, and never applied unless the dashboard enables it. It writes code into the game and may
/// violate PoE's Terms of Service; use at your own risk. The clamp-site byte pattern is ported from
/// the community "WheresMyZoomAt" PoE2 plugin and, like every offset here, may drift per patch — a
/// stale pattern fails gracefully (reported via <see cref="Note"/>) rather than patching the wrong
/// address.</para>
/// </summary>
public sealed class ZoomPatch : IDisposable
{
    /// <summary>Precise form: <c>minss xmm1,[rip+rel32] ; movss [rdi+450h],xmm1 ; REX</c>.</summary>
    private static readonly byte?[] PatternPrecise =
    [
        0xF3, 0x0F, 0x5D, 0x0D, null, null, null, null,   // minss xmm1, [rip+rel32]
        0xF3, 0x0F, 0x11, 0x8F, null, null, null, null,   // movss [rdi+disp32], xmm1
        0x41,                                               // REX prefix of the following instruction
    ];

    /// <summary>Looser community form (register wildcarded); the matched ModRM reg field is validated
    /// to be xmm1 before it is used.</summary>
    private static readonly byte?[] PatternLoose =
    [
        0xF3, 0x0F, 0x5D, null, null, null, null, null,
        0xF3, 0x0F, 0x11, null, null, null, null, null,
        0x41,
    ];

    private const int TrampolineSize = 32;   // float value + padding + 8-byte minss + 5-byte jmp + ret
    private const uint AllocSize = 64;

    private readonly ProcessHandle _process;
    private readonly MemoryReader _reader;    // own read stack: only used for the one-shot AOB scan
    private MemoryWriter? _writer;

    private nint _patchSite;
    private nint _tramp;
    private byte[] _original = Array.Empty<byte>();
    private float _appliedValue;
    private bool _applied;
    private string _note = "not applied";

    public bool Applied => _applied;
    public float LastValue => _appliedValue;
    public string Note => _note;

    public ZoomPatch(ProcessHandle process)
    {
        _process = process;
        _reader = new MemoryReader(process);   // independent read stack (apply runs on the render thread)
    }

    /// <summary>
    /// Apply (or re-apply, when <paramref name="zoomValue"/> changed) the zoom un-clamp. Returns true
    /// on success; on failure <see cref="Note"/> explains why (pattern stale, not admin, …).
    /// </summary>
    public bool Apply(float zoomValue)
    {
        if (_applied && MathF.Abs(_appliedValue - zoomValue) < 0.001f)
            return true;                        // already applied with this value

        if (_applied) Remove();                 // value changed → restore, then patch fresh

        _note = "";
        try
        {
            _writer ??= MemoryWriter.Open(_process);

            var site = FindPatchSite();
            if (site == 0)
            {
                _note = "clamp pattern not found — offsets likely stale for this PoE2 patch";
                return false;
            }

            var tramp = _writer.AllocExecNear(_process.MainModuleBase, AllocSize);
            if (tramp == 0)
            {
                _note = "VirtualAllocEx failed — run POE2Radar as Administrator";
                return false;
            }

            // Trampoline layout (PAGE_EXECUTE_READWRITE, rel32-reachable from the patch site):
            //   [0..3]   float zoomValue
            //   [8..15]  minss xmm1, [rip+rel32 → value]   (8 bytes)
            //   [16..20] jmp rel32 → site+8                (5 bytes)
            //   [21]     ret (unused fallback)
            var code = new byte[TrampolineSize];
            BitConverter.TryWriteBytes(code.AsSpan(0, 4), zoomValue);
            code[8] = 0xF3; code[9] = 0x0F; code[10] = 0x5D; code[11] = 0x0D;
            BitConverter.TryWriteBytes(code.AsSpan(12, 4), (int)((tramp + 0) - (tramp + 16)));
            code[16] = 0xE9;
            BitConverter.TryWriteBytes(code.AsSpan(17, 4), (int)((site + 8) - (tramp + 21)));
            code[21] = 0xC3;

            if (!_writer.WriteBytes(tramp, code))
            {
                _note = "WriteProcessMemory (trampoline) failed";
                _writer.Free(tramp);
                return false;
            }

            // Read the original 8 instruction bytes so Remove() can restore them.
            var original = new byte[8];
            if (_reader.TryReadBytes(site, original) != original.Length)
            {
                _note = "could not read the original code bytes at the patch site";
                _writer.Free(tramp);
                return false;
            }

            // 5-byte E9 JMP to the trampoline + 3 NOPs (fills the rest of the 8-byte minss).
            var jump = new byte[8];
            jump[0] = 0xE9;
            BitConverter.TryWriteBytes(jump.AsSpan(1, 4), (int)((tramp + 8) - (site + 5)));
            jump[5] = 0x90; jump[6] = 0x90; jump[7] = 0x90;

            if (!_writer.PatchCode(site, jump))
            {
                _note = "WriteProcessMemory (patch site) failed";
                _writer.Free(tramp);
                return false;
            }

            _patchSite = site;
            _tramp = tramp;
            _original = original;
            _appliedValue = zoomValue;
            _applied = true;
            _note = $"applied — camera can zoom out to {zoomValue:0.#}";
            return true;
        }
        catch (Exception ex)
        {
            _note = $"apply failed: {ex.Message}";
            return false;
        }
    }

    /// <summary>Restore the original instruction bytes and free the trampoline (best-effort).</summary>
    public void Remove()
    {
        if (_writer is { IsOpen: true })
        {
            if (_applied && _patchSite != 0 && _original.Length == 8)
                _writer.PatchCode(_patchSite, _original);
            if (_tramp != 0)
                _writer.Free(_tramp);
        }

        _patchSite = 0;
        _tramp = 0;
        _original = Array.Empty<byte>();
        _applied = false;
        _note = "not applied";
    }

    /// <summary>Scan the executable sections for the clamp site; returns 0 when not found.</summary>
    private nint FindPatchSite()
    {
        // Prefer the precise pattern; fall back to the loose one (validating the register is xmm1).
        foreach (var pattern in new[] { PatternPrecise, PatternLoose })
        {
            foreach (var (sectionBase, bytes) in AobScanner.ReadExecutableSections(_process, _reader))
            {
                foreach (var match in AobScanner.FindPattern(bytes, pattern))
                {
                    // Loose pattern: ensure the minss destination register is xmm1 (ModRM bits 5..3 == 001).
                    if (ReferenceEquals(pattern, PatternLoose))
                    {
                        var modrm = bytes[match + 3];
                        if ((modrm & 0x38) != 0x08) continue;   // not xmm1
                    }
                    return sectionBase + match;
                }
            }
        }
        return 0;
    }

    public void Dispose() => Remove();
}
