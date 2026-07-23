using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;

namespace Halo.Widgets;

internal static class VlcMonitor
{
    public static volatile string? Name;
    public static IntPtr Hwnd;
    public static string? ExePath;
    public static int Version;

    private const string Suffix = " - VLC media player";
    private static Timer? _timer;
    private static readonly System.Text.StringBuilder Buf = new(512);

    public static void Poke()
    {
        if (_timer != null) return;
        VlcHttp.EnsureConfigured();
        _timer = new Timer(_ => Scan(), null, 700, 1000);
    }

    private static void Scan()
    {
        try
        {
            string? name = null; IntPtr hwnd = IntPtr.Zero;
            Halo.Interop.Win32.EnumWindows((h, _) =>
            {
                if (!Halo.Interop.Win32.IsWindowVisible(h)) return true;
                int len = Halo.Interop.Win32.GetWindowTextLengthW(h);
                if (len < Suffix.Length || len > 400) return true;
                Buf.Clear();
                if (Halo.Interop.Win32.GetWindowTextW(h, Buf, Buf.Capacity) == 0) return true;
                string t = Buf.ToString();
                if (!t.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase)) return true;
                try
                {
                    Halo.Interop.Win32.GetWindowThreadProcessId(h, out uint pid);
                    using var p = System.Diagnostics.Process.GetProcessById((int)pid);
                    if (!p.ProcessName.Equals("vlc", StringComparison.OrdinalIgnoreCase)) return true;
                    if (Name != t) ExePath = p.MainModule?.FileName;
                }
                catch { return true; }
                name = Fx.CleanText(t.Substring(0, t.Length - Suffix.Length));
                hwnd = h;
                return false;
            }, IntPtr.Zero);

            Hwnd = hwnd;
            bool justClosed = name == null && Name != null;
            if (name != Name) { Name = name; Interlocked.Increment(ref Version); }
            if (name != null) VlcHttp.Poll();
            else if (justClosed) VlcHttp.EnsureConfigured();
        }
        catch { }
    }
}

internal sealed class VlcWidget : IWidget
{
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);
    private static readonly Color Orange = Color.FromArgb(255, 136, 0);

    private readonly MediaSessions _sessions;
    private readonly float[] _hover = new float[NBtn];

    public VlcWidget(MediaSessions sessions)
    {
        _sessions = sessions;
        VlcMonitor.Poke();
    }

    private bool SmtcHasVlc()
    {
        for (int i = 0; i < MediaSessions.MaxSlots; i++)
            if (_sessions.SlotApp(i).Contains("vlc")) return true;
        return false;
    }

    public string Icon => "";
    public Bitmap? IconImage => AppIcon.ForAumid(VlcMonitor.ExePath);
    public bool IsActive => VlcMonitor.Name != null && !SmtcHasVlc();

    public int Version => VlcMonitor.Version + (VlcHttp.Online
        ? (int)(VlcHttp.Rate * 100) + (VlcHttp.Playing ? 1 : 0) + (VlcHttp.SubsOn ? 2 : 0) : 0);
    public Color? Ring => IsActive ? Orange : null;

    private static readonly double[] SpeedPresets = { 1.0, 1.25, 1.5, 2.0 };

    private static readonly (string label, int taps)[] Speeds = { ("1×", 0), ("1.2×", 2), ("1.45×", 4), ("1.95×", 7) };
    private int _speedIdx;
    private const int NBtn = 5;
    private static RectangleF[] BtnRects(int w, int h)
    {
        const float size = 40, gap = 14;
        float colL = 26 + 132 + 22, cx = (colL + (w - 26)) / 2f, total = NBtn * size + (NBtn - 1) * gap, x0 = cx - total / 2f;
        var r = new RectangleF[NBtn];
        for (int i = 0; i < NBtn; i++) r[i] = new RectangleF(x0 + i * (size + gap), 158, size, size);
        return r;
    }

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
    {
        var r = BtnRects(w, h);
        return new (RectangleF, Action<PointF>)[]
        {
            (r[0], _ => Seek(-10)),
            (r[1], _ => Play()),
            (r[2], _ => Seek(10)),
            (r[3], _ => CycleSpeed()),
            (r[4], _ => Subtitle()),
        };
    }

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        if (fade <= 0.01f) return;
        string? name = VlcMonitor.Name;
        if (name == null) return;
        var icon = IconImage;

        const float artX = 26, artY = 26, artSize = 132;
        Fx.Glow(g, w, h, fade, artX + artSize / 2f, artY + artSize / 2f, w * 0.85f, h * 1.2f, 34, Orange);
        using (var path = Fx.Rounded(new RectangleF(artX, artY, artSize, artSize), 14f))
        {
            if (icon != null)
            {
                g.SetClip(path);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                float inset = artSize * 0.14f;
                g.DrawImage(icon, artX + inset, artY + inset, artSize - inset * 2, artSize - inset * 2);
                g.ResetClip();
            }
            else { using var b = new SolidBrush(Mul(Color.FromArgb(40, 255, 255, 255), fade)); g.FillPath(b, path); }
        }

        float tx = artX + artSize + 22, tw = w - tx - 26;
        using var titleF = new Font("Segoe UI Semibold", 21f, GraphicsUnit.Pixel);
        using var bodyF = new Font("Segoe UI", 14f, GraphicsUnit.Pixel);
        using (var tb = new SolidBrush(Mul(White, fade)))
        using (var sf = new StringFormat(StringFormat.GenericTypographic)
        { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | (Fx.IsRtl(name) ? StringFormatFlags.DirectionRightToLeft : 0) })
            g.DrawString(name, titleF, tb, new RectangleF(tx, 40, tw, 30), sf);
        using (var lb = new SolidBrush(Mul(Dim, fade)))
            g.DrawString("VLC media player", bodyF, lb, tx, 76);

        var rects = BtnRects(w, h);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        for (int i = 0; i < rects.Length; i++)
        {
            var r = rects[i];
            var hit = r; hit.Inflate(4f, 4f);
            bool hov = WidgetInput.Over && hit.Contains(WidgetInput.Mouse);
            _hover[i] += ((hov ? 1f : 0f) - _hover[i]) * 0.35f;
            float t = _hover[i], sc = 1f + 0.09f * t, d = r.Width * sc;
            var rr = new RectangleF(r.X + (r.Width - d) / 2f, r.Y + (r.Height - d) / 2f, d, d);
            if (i != 4)
            {
                using (var fb = new SolidBrush(Mul(Color.FromArgb((int)(15 + 19 * t), 255, 255, 255), fade)))
                    g.FillEllipse(fb, rr);
                using (var pen = new Pen(Mul(Color.FromArgb((int)(34 + 30 * t), 255, 255, 255), fade), 1f))
                    g.DrawEllipse(pen, rr);
            }
            float a = fade * (0.8f + 0.2f * t);
            switch (i)
            {
                case 0: Fx.DrawSeekArrow(g, rr, forward: false, a); break;

                case 1:
                    bool showPause = VlcHttp.Online && VlcHttp.Playing;
                    DrawGlyphPath(g, rr, ((char)(showPause ? 0xE769 : 0xE768)).ToString(), 22f, a, showPause ? 0f : 1.5f);
                    break;
                case 2: Fx.DrawSeekArrow(g, rr, forward: true, a); break;
                case 3: DrawSpeedLabel(g, rr, SpeedLabel(), a); break;

                case 4:
                    bool subsOff = VlcHttp.Online && !VlcHttp.SubsOn;
                    Fx.DrawCcMark(g, rr, a * (subsOff ? 0.32f : 1f));
                    if (subsOff) DrawSubOffSlash(g, rr, a);
                    break;
            }
        }
    }

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        string? name = VlcMonitor.Name;
        if (name == null) return;
        float sz = h - 14f, x = 9, y = (h - sz) / 2f;
        Fx.Glow(g, w, h, fade, x + sz / 2f, h / 2f, w * 0.7f, h * 2.2f, 30, Orange);
        var icon = IconImage;
        if (icon != null)
        {
            using var path = Fx.Rounded(new RectangleF(x, y, sz, sz), sz * 0.28f);
            g.SetClip(path);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(icon, x + 2, y + 2, sz - 4, sz - 4);
            g.ResetClip();
        }
        using var f = new Font("Segoe UI Semibold", 14f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade));
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap | (Fx.IsRtl(name) ? StringFormatFlags.DirectionRightToLeft : 0), LineAlignment = StringAlignment.Center };
        g.DrawString(name, f, b, new RectangleF(x + sz + 10, 0, w - (x + sz + 10) - 12, h), sf);
    }

    private string SpeedLabel() => VlcHttp.Online
        ? VlcHttp.Rate.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×"
        : Speeds[_speedIdx].label;

    private static void Seek(int seconds)
    {
        if (VlcHttp.Online) VlcHttp.Seek(seconds);
        else KeyInject.Send(VlcMonitor.Hwnd, (byte)(seconds < 0 ? 0x25 : 0x27), alt: true);
    }

    private static void Play()
    {
        if (VlcHttp.Online) VlcHttp.TogglePlay();
        else KeyInject.Send(VlcMonitor.Hwnd, 0x20);
    }

    private static void Subtitle()
    {
        if (VlcHttp.Online) VlcHttp.CycleSubtitle();
        else KeyInject.Send(VlcMonitor.Hwnd, (byte)'V');
    }

    private void CycleSpeed()
    {
        if (VlcHttp.Online) { VlcHttp.SetRate(VlcHttp.NextPreset(VlcHttp.Rate, SpeedPresets)); return; }

        _speedIdx = (_speedIdx + 1) % Speeds.Length;
        var seq = new byte[1 + Speeds[_speedIdx].taps];
        seq[0] = 0xBB;
        for (int i = 1; i < seq.Length; i++) seq[i] = 0xDD;
        KeyInject.SendSeq(VlcMonitor.Hwnd, seq);
    }

    private static void DrawSubOffSlash(Graphics g, RectangleF r, float a)
    {
        float pad = r.Width * 0.14f;
        using var pen = new Pen(Mul(White, a * 0.9f), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, r.Left + pad, r.Bottom - pad, r.Right - pad, r.Top + pad);
    }

    private static void DrawSpeedLabel(Graphics g, RectangleF r, string label, float a)
    {
        using var f = new Font("Segoe UI Semibold", 13.5f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, a * 0.92f));
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, b, r, sf);
    }

    private static readonly FontFamily Fluent = new("Segoe Fluent Icons");
    private static void DrawGlyphPath(Graphics g, RectangleF r, string glyph, float px, float fade, float dx = 0)
    {
        using var path = new GraphicsPath();
        using var sf = new StringFormat(StringFormat.GenericTypographic);
        path.AddString(glyph, Fluent, (int)FontStyle.Regular, px, PointF.Empty, sf);
        path.Flatten();
        var b = path.GetBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        using var m = new Matrix();
        m.Translate(MathF.Round(r.X + (r.Width - b.Width) / 2f - b.X + dx),
                    MathF.Round(r.Y + (r.Height - b.Height) / 2f - b.Y));
        path.Transform(m);
        using var br = new SolidBrush(Mul(White, fade * 0.92f));
        g.FillPath(br, path);
    }

    private static Color Mul(Color c, float a) => Color.FromArgb((int)Math.Clamp(c.A * a, 0, 255), c.R, c.G, c.B);
}
