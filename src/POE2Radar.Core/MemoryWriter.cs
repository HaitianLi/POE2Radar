using System.ComponentModel;
using System.Runtime.InteropServices;
using POE2Radar.Core.Native;

namespace POE2Radar.Core;

/// <summary>
/// Write-side memory primitives for the opt-in camera-zoom patch — the ONE place POE2Radar writes to
/// the game process. Deliberately narrow and separated from the read-only <see cref="MemoryReader"/>:
/// every other subsystem stays external/read-only; only <see cref="POE2Radar.Overlay.Zoom.ZoomPatch"/>
/// reaches this.
///
/// <para>Owns its own write-capable <c>OpenProcess</c> handle (read paths keep their read-only handle),
/// and exposes exactly what a code patch needs: write bytes, make a code page writable + write + flush,
/// and allocate a page of executable memory near a target address.</para>
/// </summary>
public sealed class MemoryWriter : IDisposable
{
    private nint _handle;

    public bool IsOpen => _handle != 0;

    private MemoryWriter(nint handle) => _handle = handle;

    /// <summary>
    /// Open a write-capable handle to the target process. Throws <see cref="Win32Exception"/> on
    /// failure (typically ERROR_ACCESS_DENIED — re-run as Administrator). The caller must
    /// <see cref="Dispose"/> the result.
    /// </summary>
    public static MemoryWriter Open(ProcessHandle process)
    {
        var handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE |
            NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_QUERY_INFORMATION,
            false,
            (uint)process.ProcessId);

        if (handle == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"OpenProcess(write) ({process.ProcessId}) failed");

        return new MemoryWriter(handle);
    }

    /// <summary>Write raw bytes to the target process. Returns false on partial/failed write.</summary>
    public unsafe bool WriteBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        if (_handle == 0 || bytes.IsEmpty) return false;
        fixed (byte* p = bytes)
        {
            return NativeMethods.WriteProcessMemory(_handle, address, p, (nuint)bytes.Length, out var written)
                && written == (nuint)bytes.Length;
        }
    }

    /// <summary>
    /// Patch code at <paramref name="address"/>: flip the page to writable, write
    /// <paramref name="patchBytes"/>, restore the original protection, and flush the instruction
    /// cache. The safe single primitive for the JMP/NOP trampoline stub.
    /// </summary>
    public bool PatchCode(nint address, ReadOnlySpan<byte> patchBytes)
    {
        if (_handle == 0 || patchBytes.IsEmpty) return false;

        if (!NativeMethods.VirtualProtectEx(_handle, address, (nuint)patchBytes.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE, out var oldProtect))
            return false;

        var ok = WriteBytes(address, patchBytes);

        NativeMethods.VirtualProtectEx(_handle, address, (nuint)patchBytes.Length, oldProtect, out _);
        if (ok)
            NativeMethods.FlushInstructionCache(_handle, address, (nuint)patchBytes.Length);

        return ok;
    }

    /// <summary>
    /// Allocate <paramref name="size"/> bytes of PAGE_EXECUTE_READWRITE memory near
    /// <paramref name="nearAddress"/> (so a rel32 jump to/from it stays in range). Walks
    /// <see cref="VirtualQueryEx"/> forward from just below <paramref name="nearAddress"/> looking
    /// for a free region, mirroring the community zoom-plugin approach. Returns 0 on failure.
    /// </summary>
    public nint AllocExecNear(nint nearAddress, uint size)
    {
        if (_handle == 0) return 0;

        var mbiSize = (nuint)Marshal.SizeOf<NativeMethods.MemoryBasicInformation>();
        var addr = nearAddress - 0x10000;
        var limit = nearAddress + (nint)0x100000;   // bounded search window (~1 MB above the base)

        while (addr < limit)
        {
            var written = NativeMethods.VirtualQueryEx(_handle, addr, out var mbi, mbiSize);
            if (written == 0) break;

            if (mbi.State == NativeMethods.MEM_FREE && mbi.RegionSize >= size)
            {
                return NativeMethods.VirtualAllocEx(_handle, mbi.BaseAddress, size,
                    NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
                    NativeMethods.PAGE_EXECUTE_READWRITE);
            }

            var next = mbi.BaseAddress + (nint)mbi.RegionSize;
            if (next <= addr) break;   // defensive: avoid a stall on malformed region info
            addr = next;
        }

        return 0;
    }

    /// <summary>Release an allocation made by <see cref="AllocExecNear"/> (best-effort).</summary>
    public bool Free(nint address, uint size = 0)
    {
        if (_handle == 0 || address == 0) return false;
        return NativeMethods.VirtualFreeEx(_handle, address, size, NativeMethods.MEM_RELEASE);
    }

    public void Dispose()
    {
        if (_handle != 0)
        {
            NativeMethods.CloseHandle(_handle);
            _handle = 0;
        }
        GC.SuppressFinalize(this);
    }
}
