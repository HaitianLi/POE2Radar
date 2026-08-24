using System.Runtime.InteropServices;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace POE2Radar.Overlay;

/// <summary>
/// Direct2D bitmap of the walkable terrain mask, built once per area. One pixel per grid
/// cell — alpha = walkability. Cache key is (width, height, areaHash) — two maps can
/// share dimensions, so dimension-only keying would silently keep the previous map's
/// terrain after a transition.
/// </summary>
public sealed class TerrainBitmap : IDisposable
{
    /// <summary>Resolved interior + edge colors (BGRA bytes) the bitmap was baked with. A value-equality
    /// record so a live color/opacity tweak invalidates the cached bitmap and forces a rebuild.</summary>
    public readonly record struct TerrainStyle(byte IB, byte IG, byte IR, byte IA, byte EB, byte EG, byte ER, byte EA);

    private readonly ID2D1RenderTarget _renderTarget;
    private ID2D1Bitmap? _bitmap;
    private int _builtForWidth;
    private int _builtForHeight;
    private uint _builtForAreaHash;
    private TerrainStyle _builtForStyle;
    private int _builtForScale = 1;

    public TerrainBitmap(ID2D1RenderTarget renderTarget)
    {
        _renderTarget = renderTarget;
    }

    public ID2D1Bitmap? Bitmap => _bitmap;
    public int Width  => _builtForWidth;
    public int Height => _builtForHeight;
    public uint AreaHash => _builtForAreaHash;
    /// <summary>Supersample factor the bitmap was baked at (each grid cell is <c>Scale×Scale</c> pixels).</summary>
    public int Scale => _builtForScale;

    /// <summary>
    /// Build (or rebuild) from a flat 0/1 walkable array. Cheap when dimensions +
    /// <paramref name="areaHash"/> match the cached bitmap. <paramref name="inTransition"/> forces
    /// an immediate drop (the area's hash may briefly persist while a zone is loading).
    /// </summary>
    public void EnsureBuiltRaw(byte[] walkable, int width, int height, uint areaHash, bool inTransition, TerrainStyle style)
    {
        if (_bitmap is not null && (inTransition || areaHash != _builtForAreaHash || !style.Equals(_builtForStyle)))
        {
            _bitmap.Dispose(); _bitmap = null; _builtForAreaHash = 0;
        }
        if (inTransition || width <= 0 || height <= 0) return;
        if (_bitmap is not null && width == _builtForWidth && height == _builtForHeight
            && areaHash == _builtForAreaHash && style.Equals(_builtForStyle)) return;
        BuildFrom(walkable, width, height, areaHash, style);
    }

    private void BuildFrom(byte[] walkable, int w, int h, uint areaHash, TerrainStyle style)
    {
        // Supersample the walkable mask so the per-frame pan has SOFT edges to resample instead of hard
        // 1-px outlines. The bitmap is baked ONCE per area and only translated afterwards — never re-rendered
        // per frame — so the higher-resolution bake costs nothing at render time. 4× caps the largest side at
        // 4096 px (~64 MB worst case); Direct2D's Linear filter downsamples it back to cell scale on draw.
        var s = Math.Clamp(4096 / Math.Max(1, Math.Max(w, h)), 1, 4);
        var W = w * s;
        var H = h * s;
        var pixels = new byte[W * H * 4]; // BGRA

        // Pre-filtered (band-limited) bake: every source pixel samples the walkable mask BILINEARLY, so
        // wall boundaries become soft 1-cell ramps instead of hard edges, and the bright edge outline
        // fades over one cell. Resampling a SOFT texture at a drifting sub-pixel phase changes it only
        // imperceptibly — this is what actually kills the shimmer (plain supersampling of hard blocks only
        // shrank it).

        // Per-CELL Chebyshev distance to the nearest wall (cap 2): 0 = adjacent to a wall, 1 = one cell
        // away, ≥2 = interior. The grid's data boundary is NOT a wall (same rule as before).
        var dist = new byte[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                if (walkable[y * w + x] == 0) { dist[y * w + x] = 0; continue; }
                var d = 2;
                for (var dy = -2; dy <= 2 && d > 0; dy++)
                    for (var dx = -2; dx <= 2 && d > 0; dx++)
                    {
                        var ny = y + dy; if (ny < 0 || ny >= h) continue;
                        var nx = x + dx; if (nx < 0 || nx >= w) continue;
                        if (walkable[ny * w + nx] != 0) continue;
                        var dd = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        if (dd < d) d = dd;
                    }
                dist[y * w + x] = (byte)d;
            }

        for (var y = 0; y < H; y++)
        {
            var cy = y / s;
            var fy = (y - cy * s) / (float)s;
            var cy1 = Math.Min(cy + 1, h - 1);
            var row0 = cy * w;
            var row1 = cy1 * w;
            for (var x = 0; x < W; x++)
            {
                var cx = x / s;
                var fx = (x - cx * s) / (float)s;
                var cx1 = Math.Min(cx + 1, w - 1);
                var idx = (y * W + x) * 4;

                // Bilinear coverage of the walkable mask (0/1) → soft wall boundary.
                float c00 = walkable[row0 + cx],  c10 = walkable[row0 + cx1];
                float c01 = walkable[row1 + cx],  c11 = walkable[row1 + cx1];
                var c = c00 + (c10 - c00) * fx + (c01 - c00) * fy + (c11 - c10 - c01 + c00) * fx * fy;
                if (c <= 0f) continue;

                // Bilinear edge-distance field → soft edge→interior ramp.
                float d00 = dist[row0 + cx],  d10 = dist[row0 + cx1];
                float d01 = dist[row1 + cx],  d11 = dist[row1 + cx1];
                var dd = d00 + (d10 - d00) * fx + (d01 - d00) * fy + (d11 - d10 - d01 + d00) * fx * fy;
                var edgeT = 1f - Math.Clamp(dd, 0f, 1f);

                // Raw channels: color = lerp(interior, edge, edgeT); alpha = coverage × style alpha
                // (the existing premultiply pass below folds color × alpha).
                pixels[idx + 0] = (byte)(style.IB + (style.EB - style.IB) * edgeT);   // B
                pixels[idx + 1] = (byte)(style.IG + (style.EG - style.IG) * edgeT);   // G
                pixels[idx + 2] = (byte)(style.IR + (style.ER - style.IR) * edgeT);   // R
                pixels[idx + 3] = (byte)MathF.Round((style.IA + (style.EA - style.IA) * edgeT) * c);
            }
        }

        _bitmap?.Dispose();
        var props = new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied));
        // Premultiply alpha so D2D blends correctly.
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var a = pixels[i + 3];
            if (a == 255) continue;
            var af = a / 255f;
            pixels[i + 0] = (byte)(pixels[i + 0] * af);
            pixels[i + 1] = (byte)(pixels[i + 1] * af);
            pixels[i + 2] = (byte)(pixels[i + 2] * af);
        }

        var size = new SizeI(W, H);
        var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            _bitmap = _renderTarget.CreateBitmap(size, pinned.AddrOfPinnedObject(), (uint)(W * 4), props);
        }
        finally
        {
            pinned.Free();
        }
        _builtForWidth     = w;
        _builtForHeight    = h;
        _builtForScale     = s;
        _builtForAreaHash  = areaHash;
        _builtForStyle     = style;
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
