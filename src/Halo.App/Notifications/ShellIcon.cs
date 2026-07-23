using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Halo.Notifications;

internal static class ShellIcon
{

    private static readonly System.Collections.Generic.Dictionary<string, Bitmap> _ok = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Generic.Dictionary<string, long> _missed = new(StringComparer.OrdinalIgnoreCase);

    public static Bitmap? ForAumid(string aumid)
    {
        if (string.IsNullOrEmpty(aumid)) return null;
        lock (_ok)
        {
            if (_ok.TryGetValue(aumid, out var c)) return c;
            if (_missed.TryGetValue(aumid, out var t) && Environment.TickCount64 - t < 5000) return null;
        }
        var bmp = ExtractFrom("shell:AppsFolder\\" + aumid);
        lock (_ok)
        {
            if (bmp != null) { _ok[aumid] = bmp; _missed.Remove(aumid); }
            else _missed[aumid] = Environment.TickCount64;
        }
        return bmp;
    }

    public static Bitmap? ForAppName(string? appName)
    {
        if (string.IsNullOrWhiteSpace(appName)) return null;
        string key = "\0name:" + appName;
        lock (_ok)
        {
            if (_ok.TryGetValue(key, out var c)) return c;
            if (_missed.TryGetValue(key, out var t) && Environment.TickCount64 - t < 30000) return null;
        }
        var lnk = StartMenuLnk(appName);
        var bmp = lnk == null ? null : ExtractFrom(lnk);
        lock (_ok)
        {
            if (bmp != null) { _ok[key] = bmp; _missed.Remove(key); }
            else _missed[key] = Environment.TickCount64;
        }
        return bmp;
    }

    public static Bitmap? ForPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string key = "\0path:" + path;
        lock (_ok)
        {
            if (_ok.TryGetValue(key, out var c)) return c;
            if (_missed.TryGetValue(key, out var t) && Environment.TickCount64 - t < 5000) return null;
        }
        var bmp = ExtractFrom(path);
        lock (_ok)
        {
            if (bmp != null) { _ok[key] = bmp; _missed.Remove(key); }
            else _missed[key] = Environment.TickCount64;
        }
        return bmp;
    }

    private static (string name, string path)[] _lnks = Array.Empty<(string, string)>();
    private static long _lnksAt;

    private static string? StartMenuLnk(string appName)
    {
        string tok = FirstWord(appName);
        if (tok.Length < 3) return null;
        if (_lnks.Length == 0 || Environment.TickCount64 - _lnksAt > 60000)
        {
            var list = new System.Collections.Generic.List<(string, string)>();
            foreach (var root in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) })
            {
                try
                {
                    var dir = System.IO.Path.Combine(root, "Programs");
                    if (System.IO.Directory.Exists(dir))
                        foreach (var f in System.IO.Directory.EnumerateFiles(dir, "*.lnk", System.IO.SearchOption.AllDirectories))
                            list.Add((System.IO.Path.GetFileNameWithoutExtension(f), f));
                }
                catch { }
            }
            _lnks = list.ToArray(); _lnksAt = Environment.TickCount64;
        }
        string? best = null;
        foreach (var (name, path) in _lnks)
        {
            if (name.Equals(tok, StringComparison.OrdinalIgnoreCase)) return path;
            if (best == null && name.StartsWith(tok, StringComparison.OrdinalIgnoreCase)) best = path;
        }
        return best;
    }

    private static string FirstWord(string s)
    {
        int i = 0; while (i < s.Length && !char.IsLetterOrDigit(s[i])) i++;
        int j = i; while (j < s.Length && char.IsLetterOrDigit(s[j])) j++;
        return s.Substring(i, j - i);
    }

    private static Bitmap? ExtractFrom(string parsingName)
    {
        IntPtr hbmp = IntPtr.Zero;
        try
        {
            var iid = typeof(IShellItemImageFactory).GUID;
            if (SHCreateItemFromParsingName(parsingName, IntPtr.Zero, ref iid, out var f) != 0 || f == null)
                return null;

            f.GetImage(new SIZE { cx = 256, cy = 256 }, SIIGBF_ICONONLY | SIIGBF_BIGGERSIZEOK, out hbmp);
            Marshal.ReleaseComObject(f);
            if (hbmp == IntPtr.Zero) return null;

            var ds = new DIBSECTION();
            if (GetObject(hbmp, Marshal.SizeOf<DIBSECTION>(), ref ds) < Marshal.SizeOf<DIBSECTION>() || ds.dsBm.bmBits == IntPtr.Zero)
                { using var fb = Image.FromHbitmap(hbmp); return new Bitmap(fb); }
            var bm = ds.dsBm;
            using var src = new Bitmap(bm.bmWidth, bm.bmHeight, bm.bmWidthBytes,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb, bm.bmBits);
            var copy = new Bitmap(src);

            if (ds.dsBmih.biHeight > 0) copy.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return Trim(copy);
        }
        catch { return null; }
        finally { if (hbmp != IntPtr.Zero) DeleteObject(hbmp); }
    }

    private static Bitmap? Trim(Bitmap b)
    {
        int minX = b.Width, minY = b.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
                if (b.GetPixel(x, y).A > 90)
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }

        if (maxX < minX) { b.Dispose(); return null; }
        int w = maxX - minX + 1, h = maxY - minY + 1;
        if (w >= b.Width - 1 && h >= b.Height - 1) return b;
        var crop = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(crop))
            g.DrawImage(b, new Rectangle(0, 0, w, h), new Rectangle(minX, minY, w, h), GraphicsUnit.Pixel);
        b.Dispose();
        return crop;
    }

    private const int SIIGBF_BIGGERSIZEOK = 0x1;
    private const int SIIGBF_ICONONLY = 0x4;

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType, bmWidth, bmHeight, bmWidthBytes;
        public ushort bmPlanes, bmBitsPixel;
        public IntPtr bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DIBSECTION
    {
        public BITMAP dsBm;
        public BITMAPINFOHEADER dsBmih;
        public uint dsBitfield0, dsBitfield1, dsBitfield2;
        public IntPtr dshSection;
        public uint dsOffset;
    }

    [DllImport("gdi32.dll")]
    private static extern int GetObject(IntPtr h, int c, ref DIBSECTION pv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string path, IntPtr pbc, ref Guid riid, out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        void GetImage(SIZE size, int flags, out IntPtr phbm);
    }
}
