using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Halo.Widgets;

internal sealed class DownloadWidget : IWidget
{
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);
    private static readonly Color Track = Color.FromArgb(46, 255, 255, 255);
    private static readonly Color Blue = Color.FromArgb(120, 170, 255);
    private static readonly FontFamily Fluent = new("Segoe Fluent Icons");

    public DownloadWidget() => Downloads.Poke();

    public string Icon => "";
    private static string? _icoFile;
    private static Bitmap? _icoCache;

    private static Bitmap? Ico()
    {
        if (Downloads.IconFile is { } f)
        {
            if (f != _icoFile) { _icoCache?.Dispose(); _icoCache = LoadFile(f); _icoFile = f; }
            if (_icoCache != null) return _icoCache;
        }
        return Downloads.IsStore && Downloads.ExePath is { } aumid
            ? Halo.Notifications.ShellIcon.ForAumid(aumid)
            : AppIcon.ForAumid(Downloads.ExePath);
    }
    private static Bitmap? LoadFile(string f)
    {
        try { using var t = new Bitmap(f); return new Bitmap(t); } catch { return null; }
    }
    public Bitmap? IconImage => Ico();
    public bool IsActive => Downloads.Name != null;

    public float RingProgress => Downloads.Name == null || Downloads.Installing || Downloads.Waiting || Downloads.NoPct
        ? -1f : Math.Clamp(Downloads.Percent / 100f, 0f, 1f);

    private static bool Spinning => Downloads.Installing || Downloads.Waiting || (Downloads.NoPct && !Downloads.Paused);
    public int Version => Downloads.Version + (Spinning ? (int)(Environment.TickCount64 / 60) : 0);
    public bool Animating => Spinning;
    public Color? Ring => Downloads.Name == null ? null : Accent();

    private static Color Accent()
    {
        var a = Fx.AccentOf(Ico());
        return a == Fx.White ? Blue : a;
    }

    private const float ArtX = 26, ArtY = 26, ArtSize = 132;
    private static RectangleF[] CtlRects(int w, int n)
    {
        const float size = 40, gap = 18;
        float colL = ArtX + ArtSize + 22, colR = w - 26, cx = (colL + colR) / 2f;
        float total = n * size + (n - 1) * gap, x0 = cx - total / 2f, y = 158;
        var r = new RectangleF[n];
        for (int i = 0; i < n; i++) r[i] = new RectangleF(x0 + i * (size + gap), y, size, size);
        return r;
    }

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
    {
        if (Downloads.Name == null) return Array.Empty<(RectangleF, Action<PointF>)>();
        if (Downloads.IsStore && Downloads.CanControl)
        {
            var r = CtlRects(w, 2);
            return new (RectangleF, Action<PointF>)[]
            {
                (r[0], _ => { if (Downloads.Paused) Downloads.StoreResume(); else Downloads.StorePause(); }),
                (r[1], _ => Downloads.StoreCancel()),
            };
        }
        if (Downloads.Hwnd == IntPtr.Zero) return Array.Empty<(RectangleF, Action<PointF>)>();
        var wr = CtlRects(w, 2);
        return new (RectangleF, Action<PointF>)[]
        {
            (wr[0], _ => Downloads.Reveal()),
            (wr[1], _ => Downloads.StopProcess()),
        };
    }

    private static float _fracShown = -1f;
    private static string? _lastName;

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        if (fade <= 0.01f) return;
        string? name = Downloads.Name;
        if (name == null) return;
        bool installing = Downloads.Installing || Downloads.Waiting || (Downloads.NoPct && !Downloads.Paused);
        bool paused = Downloads.Paused;
        int pct = Math.Clamp(Downloads.Percent, 0, 100);
        long done = Downloads.Downloaded, tot = Downloads.Total;
        var icon = Ico();
        var accent = icon != null ? Accent() : Blue;
        float pulse = 0.5f + 0.5f * MathF.Sin(Environment.TickCount64 / 480f);

        g.SmoothingMode = SmoothingMode.AntiAlias;

        Fx.Glow(g, w, h, fade * (installing ? 0.55f + 0.45f * pulse : 1f),
            ArtX + ArtSize / 2f, ArtY + ArtSize / 2f, w * 0.85f, h * 1.2f, 38, accent);
        DrawArt(g, icon, fade);

        float tx = ArtX + ArtSize + 22, tw = w - tx - 26;
        using var titleF = new Font("Segoe UI Semibold", 22f, GraphicsUnit.Pixel);
        using var bodyF = new Font("Segoe UI", 15f, GraphicsUnit.Pixel);
        using var timeF = new Font("Segoe UI", 12f, GraphicsUnit.Pixel);
        using (var tb = new SolidBrush(Mul(White, fade)))
            DrawEllipsized(g, name, titleF, tb, tx, 34, tw, 30);
        string state = Downloads.Waiting ? "Waiting…" : installing ? "Installing…"
            : paused ? "Paused" : "Downloading";
        string sub = installing || Downloads.NoPct ? state : $"{state}   ·   {pct}%";
        using (var lb = new SolidBrush(Mul(Dim, fade)))
            DrawEllipsized(g, sub, bodyF, lb, tx, 66, tw, 22);

        float by = 116, bh = 5;
        Fill(g, tx, by, tw, bh, Mul(Track, fade), bh / 2);
        if (installing)
        {
            float seg = tw * 0.34f, sx = tx + (tw - seg) * (0.5f + 0.5f * MathF.Sin(Environment.TickCount64 / 700f));
            Fill(g, sx, by, seg, bh, Mul(accent, fade * (0.5f + 0.5f * pulse)), bh / 2);
        }
        else
        {
            float frac = tot > 1_048_576 ? Math.Clamp(done / (float)tot, 0f, 1f) : pct / 100f;
            if (name != _lastName) { _lastName = name; _fracShown = frac; }
            _fracShown = _fracShown < 0 ? frac : _fracShown + (frac - _fracShown) * 0.18f;
            if (Math.Abs(frac - _fracShown) < 0.002f) _fracShown = frac;
            if (_fracShown > 0) Fill(g, tx, by, tw * _fracShown, bh, Mul(paused ? Dim : accent, fade), bh / 2);

            using var eb = new SolidBrush(Mul(Dim, fade));
            if (done > 1_048_576) g.DrawString(Bytes(done), timeF, eb, tx, by + 9);
            if (tot > 1_048_576)
            {
                var ts = g.MeasureString(Bytes(tot), timeF);
                g.DrawString(Bytes(tot), timeF, eb, tx + tw - ts.Width, by + 9);
            }
        }

        if (Downloads.IsStore && Downloads.CanControl)
        {
            var r = CtlRects(w, 2);
            DrawCtl(g, r[0], paused ? 0xE768 : 0xE769, fade, danger: false);
            DrawCtl(g, r[1], 0xE711, fade, danger: true);
        }
        else if (Downloads.Hwnd != IntPtr.Zero)
        {
            var wr = CtlRects(w, 2);
            DrawCtl(g, wr[0], 0xE838, fade, danger: false);
            DrawStop(g, wr[1], fade);
        }
    }

    private static void DrawArt(Graphics g, Bitmap? icon, float fade)
        => IconTile(g, new RectangleF(ArtX, ArtY, ArtSize, ArtSize), ArtSize * 0.24f, icon, fade, 46f, border: true);

    private static void IconTile(Graphics g, RectangleF box, float radius, Bitmap? icon, float fade, float glyphPx, bool border)
    {
        using var path = Fx.Rounded(box, radius);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (icon != null)
        {
            int s = Math.Max(1, (int)Math.Ceiling(box.Width));
            using var scaled = new Bitmap(s, s, PixelFormat.Format32bppPArgb);
            using (var sg = Graphics.FromImage(scaled))
            {
                sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                sg.SmoothingMode = SmoothingMode.HighQuality;
                using var ia = new ImageAttributes();
                ia.SetWrapMode(WrapMode.TileFlipXY);
                ia.SetColorMatrix(new ColorMatrix { Matrix33 = fade });
                int side = Math.Min(icon.Width, icon.Height);
                sg.DrawImage(icon, new Rectangle(0, 0, s, s),
                    (icon.Width - side) / 2, (icon.Height - side) / 2, side, side, GraphicsUnit.Pixel, ia);
            }
            using var tb = new TextureBrush(scaled) { WrapMode = WrapMode.Clamp };
            tb.TranslateTransform(box.X, box.Y);
            g.FillPath(tb, path);
        }
        else
        {
            using var gb = new SolidBrush(Mul(Track, fade));
            g.FillPath(gb, path);
            DrawGlyph(g, box, ((char)0xE896).ToString(), glyphPx, fade);
        }
        if (border)
        {
            using var pen = new Pen(Mul(Color.FromArgb(28, 255, 255, 255), fade), 1f);
            g.DrawPath(pen, path);
        }
    }

    private static void DrawCollapsedIcon(Graphics g, Bitmap? icon, float x, float y, float sz, float fade)
        => IconTile(g, new RectangleF(x, y, sz, sz), sz * 0.28f, icon, fade, sz * 0.5f, border: false);

    private static string Bytes(long b)
    {
        if (b <= 0) return "0 MB";
        double mb = b / 1048576.0;
        return mb >= 1024 ? $"{mb / 1024:0.0} GB" : $"{mb:0} MB";
    }

    private static readonly Color Ctl = Color.FromArgb(255, 255, 255, 255);

    private void DrawCtl(Graphics g, RectangleF r, int glyph, float fade, bool danger)
    {
        bool hov = WidgetInput.Over && r.Contains(WidgetInput.Mouse);
        var tint = danger ? Red : Ctl;
        using (var bg = new SolidBrush(Mul(Color.FromArgb(hov ? 58 : 34, tint), fade)))
            g.FillEllipse(bg, r);
        using (var pen = new Pen(Mul(Color.FromArgb(hov ? 70 : 40, tint), fade), 1f))
            g.DrawEllipse(pen, r);
        DrawGlyph(g, r, ((char)glyph).ToString(), r.Width * 0.40f, fade * (hov ? 1f : 0.85f), danger ? tint : White);
    }

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        string? name = Downloads.Name;
        if (name == null) return;
        var icon = Ico();
        var accent = icon != null ? Accent() : Blue;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float sz = h - 14f, ix = 9, iy = (h - sz) / 2f;
        float tx = ix + sz + 12;

        if (Downloads.Waiting)
        {
            float pulse = 0.5f - 0.5f * MathF.Cos(Environment.TickCount % 2400 / 2400f * MathF.Tau);
            using (var pb = new SolidBrush(Mul(accent, fade * (0.05f + 0.12f * pulse))))
            using (var pp = Fx.PillPath(w, h, h / 2f))
                g.FillPath(pb, pp);
            DrawCollapsedIcon(g, icon, ix, iy, sz, fade);
            using var nf = new Font("Segoe UI Semibold", 14f, GraphicsUnit.Pixel);
            using var nb = new SolidBrush(Mul(White, fade));
            DrawEllipsized(g, name, nf, nb, tx, (h - 18f) / 2f, w - tx - 14, 18);
            return;
        }

        DrawCollapsedIcon(g, icon, ix, iy, sz, fade);
        float by = h / 2f - 3, bh = 6;
        if (Downloads.Installing || (Downloads.NoPct && !Downloads.Paused))
        {
            float p = 0.5f + 0.5f * MathF.Sin(Environment.TickCount64 / 480f);
            float bw = w - tx - 16;
            Fill(g, tx, by, bw, bh, Track, bh / 2);
            float seg = bw * 0.38f, sx = tx + (bw - seg) * (0.5f + 0.5f * MathF.Sin(Environment.TickCount64 / 700f));
            Fill(g, sx, by, seg, bh, Mul(accent, 0.5f + 0.5f * p), bh / 2);
            return;
        }
        int pct = Math.Clamp(Downloads.Percent, 0, 100);
        using var f = new Font("Segoe UI Semibold", 14f, GraphicsUnit.Pixel);
        using var tb = new SolidBrush(White);
        var tsz = g.MeasureString(pct + "%", f);
        float px = w - tsz.Width - 14, py = (h - tsz.Height) / 2f;
        g.DrawString(pct + "%", f, tb, px, py);
        float bw2 = px - tx - 12;
        Fill(g, tx, by, bw2, bh, Track, bh / 2);
        Fill(g, tx, by, bw2 * pct / 100f, bh, Downloads.Paused ? Dim : accent, bh / 2);
    }

    private static readonly Color Red = Color.FromArgb(255, 120, 110);
    private static void DrawStop(Graphics g, RectangleF r, float fade)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool hov = WidgetInput.Over && r.Contains(WidgetInput.Mouse);
        using (var bg = new SolidBrush(Mul(Color.FromArgb(hov ? 60 : 38, 255, 120, 110), fade)))
            g.FillEllipse(bg, r);
        float s = r.Width * 0.34f;
        var sq = new RectangleF(r.X + (r.Width - s) / 2f, r.Y + (r.Height - s) / 2f, s, s);
        using var b = new SolidBrush(Mul(Red, fade * (hov ? 1f : 0.85f)));
        using var p = Fx.Rounded(sq, s * 0.22f);
        g.FillPath(b, p);
    }

    private static Color Mul(Color c, float a) => Color.FromArgb((int)(c.A * a), c.R, c.G, c.B);

    private static void Fill(Graphics g, float x, float y, float w, float h, Color c, float r = 0)
    {
        if (w <= 0.5f) return;
        using var b = new SolidBrush(c);
        if (r <= 0) { g.FillRectangle(b, x, y, w, h); return; }
        using var p = Fx.Rounded(new RectangleF(x, y, w, h), r);
        g.FillPath(b, p);
    }

    private static void DrawEllipsized(Graphics g, string s, Font f, Brush b, float x, float y, float w, float h)
    {
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(s, f, b, new RectangleF(x, y, w, h), sf);
    }

    private static void DrawGlyph(Graphics g, RectangleF r, string glyph, float px, float fade, Color? tint = null)
    {
        using var path = new GraphicsPath();
        using var sf = new StringFormat(StringFormat.GenericTypographic);
        path.AddString(glyph, Fluent, (int)FontStyle.Regular, px, PointF.Empty, sf);
        path.Flatten();
        var bnd = path.GetBounds();
        if (bnd.Width <= 0 || bnd.Height <= 0) return;
        using var m = new Matrix();
        m.Translate(MathF.Round(r.X + (r.Width - bnd.Width) / 2f - bnd.X),
                    MathF.Round(r.Y + (r.Height - bnd.Height) / 2f - bnd.Y));
        path.Transform(m);
        using var br = new SolidBrush(Mul(tint ?? White, fade * 0.9f));
        g.FillPath(br, path);
    }
}
