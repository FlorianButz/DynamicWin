using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace Halo.Widgets;

internal static class Fx
{
    public static readonly Color White = Color.FromArgb(238, 255, 255, 255);

    public static string CleanText(string? s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        try { return s.IsNormalized(System.Text.NormalizationForm.FormKC) ? s : s.Normalize(System.Text.NormalizationForm.FormKC); }
        catch { return s; }
    }

    public static bool IsRtl(string? s)
    {
        if (s == null) return false;
        foreach (var c in s) if (c >= 0x0590 && c <= 0x08FF) return true;
        return false;
    }

    private static readonly ConditionalWeakTable<Bitmap, object> AccentCache = new();

    public static Color AccentOf(Bitmap? icon)
    {
        if (icon is null) return White;
        if (AccentCache.TryGetValue(icon, out var cached)) return (Color)cached;
        var accent = Accent(icon);
        AccentCache.AddOrUpdate(icon, accent);
        return accent;
    }

    private static readonly Bitmap GlowTex = BuildGlowTex();

    private static Bitmap BuildGlowTex()
    {
        const int n = 128;

        var bmp = new Bitmap(n, n, PixelFormat.Format32bppPArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, n, n), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        var bytes = new byte[data.Stride * n];
        var rnd = new Random(1);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n / 2f) / (n / 2f), dy = (y - n / 2f) / (n / 2f);
                float t = MathF.Min(1f, MathF.Sqrt(dx * dx + dy * dy));
                float a = MathF.Pow(1f - t, 1.8f) * 255f + rnd.Next(-5, 6);
                int i = y * data.Stride + x * 4;
                byte av = (byte)Math.Clamp((int)a, 0, 255);
                bytes[i] = bytes[i + 1] = bytes[i + 2] = av;
                bytes[i + 3] = av;
            }
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        bmp.UnlockBits(data);
        return bmp;
    }

    public static void Glow(Graphics g, int w, int h, float fade, float cx, float cy,
        float rx, float ry, int alpha, Color accent)
    {
        if (accent == White || fade <= 0.01f) return;
        using var clip = PillClip(w, h);
        var old = g.Clip;
        g.SetClip(clip);
        var oldInterp = g.InterpolationMode;
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(new ColorMatrix
        {
            Matrix00 = accent.R / 255f,
            Matrix11 = accent.G / 255f,
            Matrix22 = accent.B / 255f,
            Matrix33 = alpha * fade / 255f,
        });
        ia.SetWrapMode(WrapMode.TileFlipXY);
        g.DrawImage(GlowTex, new Rectangle((int)(cx - rx), (int)(cy - ry), (int)(rx * 2), (int)(ry * 2)),
            0, 0, GlowTex.Width, GlowTex.Height, GraphicsUnit.Pixel, ia);
        g.InterpolationMode = oldInterp;
        g.Clip = old;
    }

    private static GraphicsPath PillClip(int w, int h) => PillPath(w, h, Math.Min(h / 2f, 30f));

    public static GraphicsPath PillPath(int w, int h, float r)
    {
        float d = Math.Min(r, Math.Min(w, h) / 2f) * 2f;
        var p = new GraphicsPath();
        p.AddLine(0, 0, w, 0);
        p.AddArc(w - d, h - d, d, d, 0, 90);
        p.AddArc(0, h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static Color Accent(Bitmap art)
    {
        try
        {
            using var small = new Bitmap(12, 12, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(art, 0, 0, 12, 12);
            }
            float best = -1f; Color pick = White;
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                {
                    var p = small.GetPixel(x, y);
                    if (p.A < 128) continue;
                    RgbToHsv(p, out _, out float s, out float v);
                    if (v < 0.2f || v > 0.98f) continue;
                    float score = s * (v < 0.85f ? v : 1.7f - v);
                    if (score > best) { best = score; pick = p; }
                }
            if (best <= 0.05f) return White;
            RgbToHsv(pick, out float ph, out float ps, out float pv);
            return HsvToRgb(ph, Math.Min(1f, ps * 1.1f), Math.Max(pv, 0.85f));
        }
        catch { return White; }
    }

    public static Bitmap Badge(Bitmap icon, char ch)
    {
        var b = new Bitmap(icon.Width, icon.Height, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.DrawImage(icon, 0, 0, icon.Width, icon.Height);
        float d = icon.Width * 0.42f, x = icon.Width - d, y = icon.Height - d;
        using (var bg = new SolidBrush(Color.FromArgb(230, 24, 24, 26)))
            g.FillEllipse(bg, x, y, d, d);
        using var f = new Font("Segoe UI Semibold", d * 0.62f, GraphicsUnit.Pixel);
        using var wb = new SolidBrush(Color.FromArgb(240, 255, 255, 255));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(ch.ToString(), f, wb, new RectangleF(x, y - d * 0.02f, d, d), sf);
        return b;
    }

    public static Color Shade(Color c, int step)
    {
        if (step <= 0) return c;
        RgbToHsv(c, out float h, out float s, out float v);
        return HsvToRgb(h, Math.Min(1f, s * (1f + 0.22f * step)), Math.Max(0.35f, v * (1f - 0.26f * step)));
    }

    public static void RgbToHsv(Color c, out float h, out float s, out float v)
    {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        v = max; s = max <= 0f ? 0f : d / max; h = 0f;
        if (d > 0f)
        {
            if (max == r) h = (g - b) / d % 6f;
            else if (max == g) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;
            h *= 60f; if (h < 0f) h += 360f;
        }
    }

    public static Color HsvToRgb(float h, float s, float v)
    {
        float c = v * s, x = c * (1f - Math.Abs(h / 60f % 2f - 1f)), m = v - c;
        float r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }
        return Color.FromArgb(255, (int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    private static Bitmap? _flagGhost;
    private static Bitmap? _flagGhostFor;

    public static Bitmap FlagGhost(Bitmap flag)
    {
        if (_flagGhost != null && ReferenceEquals(_flagGhostFor, flag)) return _flagGhost;
        const int fw = 420, fh = 264, amp = 12;
        const int oh = fh + amp * 2;
        using var scaled = new Bitmap(fw, fh, PixelFormat.Format32bppArgb);
        using (var sg = Graphics.FromImage(scaled))
        {
            sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            sg.DrawImage(flag, new Rectangle(0, 0, fw, fh), 0, 0, flag.Width, flag.Height, GraphicsUnit.Pixel);
        }
        var src = scaled.LockBits(new Rectangle(0, 0, fw, fh), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var sb = new byte[src.Stride * fh];
        System.Runtime.InteropServices.Marshal.Copy(src.Scan0, sb, 0, sb.Length);
        int stride = src.Stride;
        scaled.UnlockBits(src);

        var bmp = new Bitmap(fw, oh, PixelFormat.Format32bppPArgb);
        var dst = bmp.LockBits(new Rectangle(0, 0, fw, oh), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        var ob = new byte[dst.Stride * oh];
        for (int x = 0; x < fw; x++)
        {
            float ph = x / (float)fw * MathF.Tau * 2.4f;
            float dy = amp * MathF.Sin(ph);
            float shade = 1f + 0.10f * MathF.Cos(ph);
            float ex = (x - fw / 2f) / (fw / 2f);
            float fadeX = 1f - ex * ex;
            for (int y = 0; y < oh; y++)
            {
                float sy = y - amp - dy;
                int y0 = (int)MathF.Floor(sy);
                if (y0 < -1 || y0 >= fh) continue;
                float fr = sy - y0;
                int ia = Math.Clamp(y0, 0, fh - 1) * stride + x * 4;
                int ib = Math.Clamp(y0 + 1, 0, fh - 1) * stride + x * 4;

                float aa = (y0 >= 0 ? sb[ia + 3] : 0) * (1f - fr) + (y0 + 1 < fh ? sb[ib + 3] : 0) * fr;
                float ey = (sy - fh / 2f) / (fh / 2f);
                float fadeY = Math.Max(0f, 1f - ey * ey);
                float alpha = aa / 255f * fadeX * fadeY;
                if (alpha <= 0.004f) continue;
                int o = y * dst.Stride + x * 4;
                for (int c = 0; c < 3; c++)
                {
                    float ch = (sb[ia + c] * (1f - fr) + sb[ib + c] * fr) * shade;
                    ob[o + c] = (byte)(Math.Min(ch, 255f) * alpha);
                }
                ob[o + 3] = (byte)(alpha * 255f);
            }
        }
        System.Runtime.InteropServices.Marshal.Copy(ob, 0, dst.Scan0, ob.Length);
        bmp.UnlockBits(dst);
        var old = _flagGhost;
        _flagGhost = bmp;
        _flagGhostFor = flag;
        old?.Dispose();
        return bmp;
    }

    public static void DrawFlagGhost(Graphics g, System.Drawing.Bitmap? flag, int w, int h, float a)
    {
        if (flag is null) return;
        var ghost = FlagGhost(flag);
        const int gw = 210;
        int gh = ghost.Height * gw / ghost.Width;
        var dest = new Rectangle((w - gw) / 2, (h - gh) / 2 + 4, gw, gh);
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(new ColorMatrix { Matrix33 = 0.16f * a });
        var oldInterp = g.InterpolationMode;
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        g.DrawImage(ghost, dest, 0, 0, ghost.Width, ghost.Height, GraphicsUnit.Pixel, ia);
        g.InterpolationMode = oldInterp;
    }

    public static void DrawSeekArrow(Graphics g, RectangleF chip, bool forward, float alpha, string label = "10")
    {
        var c = Color.FromArgb((int)(238 * alpha), 255, 255, 255);
        float cx = chip.X + chip.Width / 2f, cy = chip.Y + chip.Height / 2f, r = chip.Width * 0.30f;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        const float gap = 80f;
        using (var pen = new Pen(c, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, 270f + gap / 2f, 360f - gap);

        float deg = forward ? 270f - gap / 2f : 270f + gap / 2f;
        float th = deg * MathF.PI / 180f;
        var p = new PointF(cx + r * MathF.Cos(th), cy + r * MathF.Sin(th));
        var dir = forward ? new PointF(-MathF.Sin(th), MathF.Cos(th)) : new PointF(MathF.Sin(th), -MathF.Cos(th));
        var perp = new PointF(-dir.Y, dir.X);
        float ah = chip.Width * 0.13f, aw = chip.Width * 0.10f;
        using (var b = new SolidBrush(c))
        using (var tri = new GraphicsPath())
        {
            tri.AddPolygon(new[]
            {
                new PointF(p.X + dir.X * ah, p.Y + dir.Y * ah),
                new PointF(p.X - dir.X * ah * 0.4f + perp.X * aw, p.Y - dir.Y * ah * 0.4f + perp.Y * aw),
                new PointF(p.X - dir.X * ah * 0.4f - perp.X * aw, p.Y - dir.Y * ah * 0.4f - perp.Y * aw),
            });
            g.FillPath(b, tri);
        }
        using var f = new Font("Segoe UI Semibold", chip.Width * 0.26f, GraphicsUnit.Pixel);
        using var tb = new SolidBrush(c);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, tb, new RectangleF(chip.X, chip.Y + 0.5f, chip.Width, chip.Height), sf);
    }

    public static void DrawCcMark(Graphics g, RectangleF chip, float alpha)
    {
        using var f = new Font("Segoe UI Semibold", chip.Width * 0.34f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Color.FromArgb((int)(238 * alpha), 255, 255, 255));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("CC", f, b, chip, sf);
    }

    public static void DrawPipMark(Graphics g, RectangleF chip, float alpha)
    {
        var c = Color.FromArgb((int)(238 * alpha), 255, 255, 255);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float w = chip.Width * 0.44f, h = w * 0.72f;
        var f = new RectangleF(chip.X + (chip.Width - w) / 2f, chip.Y + (chip.Height - h) / 2f, w, h);
        using (var op = Rounded(f, 2f))
        using (var pen = new Pen(c, 1.5f))
            g.DrawPath(pen, op);

        var a = new PointF(f.X + w * 0.30f, f.Y + h * 0.30f);
        var b = new PointF(f.Right - w * 0.22f, f.Bottom - h * 0.26f);
        var d = new PointF(b.X - a.X, b.Y - a.Y);
        float len = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
        d = new PointF(d.X / len, d.Y / len);
        using (var pen = new Pen(c, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(pen, a, b);
        float ah = w * 0.28f;
        var perp = new PointF(-d.Y, d.X);
        using var tb = new SolidBrush(c);
        using var tri = new GraphicsPath();
        tri.AddPolygon(new[]
        {
            b,
            new PointF(b.X - d.X * ah + perp.X * ah * 0.55f, b.Y - d.Y * ah + perp.Y * ah * 0.55f),
            new PointF(b.X - d.X * ah - perp.X * ah * 0.55f, b.Y - d.Y * ah - perp.Y * ah * 0.55f),
        });
        g.FillPath(tb, tri);
    }

    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
