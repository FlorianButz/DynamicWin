using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Halo.Widgets;

internal sealed class BtWidget : IWidget
{
    private const int HoldMs = 6000;
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);
    private static readonly Color Track = Color.FromArgb(46, 255, 255, 255);
    private static readonly FontFamily Fluent = new("Segoe Fluent Icons");

    private readonly object _lock = new();
    private string _name = "";
    private int _pct;
    private int _glyph = 0xE702;
    private long _until;
    private int _version;
    private float _fillShown = -1f;

    public void Show(string name, int pct)
    {
        lock (_lock)
        {
            _name = name;
            _pct = Math.Clamp(pct, 0, 100);
            _glyph = GlyphFor(name);
            _fillShown = 0f;
            _until = Environment.TickCount64 + HoldMs;
            _version++;
        }
    }

    public bool IsActive { get { lock (_lock) return Environment.TickCount64 < _until; } }
    public int Version { get { lock (_lock) return _version; } }
    public bool Animating => IsActive;

    public string Icon => ((char)0xE702).ToString();

    private static int GlyphFor(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("airpod") || n.Contains("buds") || n.Contains("headphone") || n.Contains("headset")
            || n.Contains("hands-free") || n.Contains(" hf") || n.StartsWith("wh-") || n.StartsWith("wf-")
            || n.Contains("earbud") || n.Contains("pods")) return 0xE7F6;
        if (n.Contains("controller") || n.Contains("dualsense") || n.Contains("dualshock")
            || n.Contains("xbox") || n.Contains("gamepad")) return 0xE7FC;
        if (n.Contains("keyboard")) return 0xE765;
        if (n.Contains("mouse")) return 0xE962;
        if (n.Contains("speaker") || n.Contains("soundbar") || n.Contains("jbl")
            || n.Contains("boom") || n.Contains("sound")) return 0xE7F5;
        if (n.Contains("watch") || n.Contains("band")) return 0xEC92;
        return 0xE8EA;
    }

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        int pct; int glyph;
        lock (_lock) { pct = _pct; glyph = _glyph; }
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float target = pct / 100f;
        _fillShown = _fillShown < 0 ? target : _fillShown + (target - _fillShown) * 0.16f;
        if (Math.Abs(target - _fillShown) < 0.004f) _fillShown = target;
        float fill = Math.Clamp(_fillShown, 0f, 1f);
        Color ringCol = Charge(fill);

        float sz = h - 12f, x = 9f, cy = h / 2f, cx = x + sz / 2f;
        Fx.Glow(g, w, h, fade, cx, cy, w * 0.6f, h * 2.0f, 34, ringCol);

        float ir = sz / 2f - 4.5f;
        using (var disc = new SolidBrush(Mul(Color.FromArgb(20, 255, 255, 255), fade)))
            g.FillEllipse(disc, cx - ir, cy - ir, ir * 2, ir * 2);
        DrawGlyph(g, new RectangleF(cx - ir, cy - ir, ir * 2, ir * 2), glyph, fade, White);

        float rr = sz / 2f - 1f;
        using (var tp = new Pen(Mul(Track, fade), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(tp, cx - rr, cy - rr, rr * 2, rr * 2, 0, 360);
        if (fill > 0.001f)
            using (var fp = new Pen(Mul(ringCol, fade), 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(fp, cx - rr, cy - rr, rr * 2, rr * 2, -90, 360f * fill);

        using var pf = new Font("Segoe UI Semibold", h * 0.42f, GraphicsUnit.Pixel);
        using var pb = new SolidBrush(Mul(White, fade));
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        g.DrawString($"{pct}%", pf, pb, new RectangleF(cx + sz, 0, w - (cx + sz) - 14, h), sf);
    }

    private static Color Charge(float fill)
        => Fx.HsvToRgb(Math.Clamp(fill, 0f, 1f) * 120f, 0.68f, 0.96f);

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        if (fade <= 0.01f) return;
        int pct, glyph; string name;
        lock (_lock) { pct = _pct; glyph = _glyph; name = _name; }
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float fill = pct / 100f;
        Color ringCol = Charge(fill);

        float cx = 70, cy = h / 2f, rr = 44;
        Fx.Glow(g, w, h, fade, cx, cy, w * 0.7f, h * 1.2f, 36, ringCol);
        using (var disc = new SolidBrush(Mul(Color.FromArgb(20, 255, 255, 255), fade)))
            g.FillEllipse(disc, cx - rr + 8, cy - rr + 8, (rr - 8) * 2, (rr - 8) * 2);
        DrawGlyph(g, new RectangleF(cx - rr + 8, cy - rr + 8, (rr - 8) * 2, (rr - 8) * 2), glyph, fade, White);
        using (var tp = new Pen(Mul(Track, fade), 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(tp, cx - rr, cy - rr, rr * 2, rr * 2, 0, 360);
        using (var fp = new Pen(Mul(ringCol, fade), 4.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(fp, cx - rr, cy - rr, rr * 2, rr * 2, -90, 360f * fill);

        float tx = cx + rr + 22;
        using var nf = new Font("Segoe UI Semibold", 22f, GraphicsUnit.Pixel);
        using var bf = new Font("Segoe UI", 15f, GraphicsUnit.Pixel);
        using (var nb = new SolidBrush(Mul(White, fade)))
            g.DrawString(name, nf, nb, tx, cy - 26);
        using (var bb = new SolidBrush(Mul(Dim, fade)))
            g.DrawString($"{pct}% battery", bf, bb, tx, cy + 4);
    }

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
        => Array.Empty<(RectangleF, Action<PointF>)>();

    private static readonly Dictionary<int, Bitmap> _glyphCache = new();
    private static Bitmap GlyphBitmap(int cp)
    {
        lock (_glyphCache)
        {
            if (_glyphCache.TryGetValue(cp, out var cached)) return cached;
            var b = RenderTight(cp);
            _glyphCache[cp] = b;
            return b;
        }
    }

    private static Bitmap RenderTight(int cp)
    {
        const int N = 128;
        var full = new Bitmap(N, N, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(full))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var f = new Font(Fluent, N * 0.7f, GraphicsUnit.Pixel);
            using var br = new SolidBrush(Color.White);
            using var sf = new StringFormat(StringFormat.GenericTypographic)
            { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            sf.FormatFlags |= StringFormatFlags.NoClip;
            g.DrawString(((char)cp).ToString(), f, br, new RectangleF(0, 0, N, N), sf);
        }
        var ink = InkBounds(full);
        if (ink.Width <= 0 || ink.Height <= 0) return full;
        var tight = full.Clone(ink, PixelFormat.Format32bppArgb);
        full.Dispose();
        return tight;
    }

    private static Rectangle InkBounds(Bitmap b)
    {
        var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = data.Stride;
            var buf = new byte[stride * b.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            int minX = b.Width, minY = b.Height, maxX = -1, maxY = -1;
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                    if (buf[y * stride + x * 4 + 3] > 16)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            return maxX < minX ? Rectangle.Empty : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }
        finally { b.UnlockBits(data); }
    }

    private static void DrawGlyph(Graphics g, RectangleF r, int cp, float fade, Color tint)
    {
        var gb = GlyphBitmap(cp);
        float target = r.Height * 0.58f;
        float scale = target / Math.Max(gb.Width, gb.Height);
        float dw = gb.Width * scale, dh = gb.Height * scale;
        var dst = new RectangleF(r.X + (r.Width - dw) / 2f, r.Y + (r.Height - dh) / 2f, dw, dh);
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(new ColorMatrix(new[]
        {
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, fade * 0.95f, 0 },
            new float[] { tint.R / 255f, tint.G / 255f, tint.B / 255f, 0, 1 },
        }));
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(gb, new[] { dst.Location, new PointF(dst.Right, dst.Y), new PointF(dst.X, dst.Bottom) },
            new RectangleF(0, 0, gb.Width, gb.Height), GraphicsUnit.Pixel, ia);
    }

    private static Color Mul(Color c, float a)
        => Color.FromArgb((int)Math.Clamp(c.A * a, 0, 255), c.R, c.G, c.B);
}
