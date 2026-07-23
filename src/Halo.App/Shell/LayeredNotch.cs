using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Halo.Interop;

namespace Halo.Shell;

internal struct MenuFrame
{
    public bool Show;
    public float Appear;
    public string[] RowIcons;
    public Bitmap?[] RowImages;
    public int[] RowCounts;
    public Bitmap?[][] SessImages;
    public string[][] SessIcons;
    public Color?[] RowRings;
    public float[] RowProgress;
    public Color?[][] SessRings;
    public float Open;
    public int OpenRow;
    public float RowOpen;
    public bool Dropping;
    public bool Outward;
    public string DropIcon;
    public Bitmap? DropImage;
    public float Drop;
    public float FromX, FromY, ToX, ToY;
}

internal sealed class LayeredNotch
{
    private const int CaptureW = 560, CaptureH = 220;

    public const int CircleD = 40, CircleGap = 4, CircleY = 0;

    private const int PrivacyGap = 10;
    public static int PrivacyPad => Widgets.Privacy.Active ? PrivacyGap : 0;

    private Win32.WndProc _wndProc = null!;
    private int _workLeft, _workTop, _workWidth;

    public float Scale = 1f;
    public float OffsetX;
    public float HandleAlpha;
    private static readonly string ScalePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "scale");

    public void LoadScale()
    {
        try
        {
            if (float.TryParse(System.IO.File.ReadAllText(ScalePath),
                    System.Globalization.CultureInfo.InvariantCulture, out var s))
                Scale = Math.Clamp(s, 0.7f, 1.6f);
        }
        catch { }
    }

    public void SaveScale()
    {
        try
        {
            System.IO.File.WriteAllText(ScalePath,
                Scale.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        catch { }
    }
    private Bitmap? _bg;
    private readonly object _bgLock = new();
    private volatile bool _capturing;
    private int _captureVersion;

    public int CaptureVersion => _captureVersion;

    public IntPtr Hwnd { get; private set; }
    public int WorkLeft => _workLeft;
    public int WorkTop => _workTop;
    public int WorkWidth => _workWidth;

    public event Action<Bitmap, bool>? ClipboardImage;
    private uint _lastClipSeq;
    private long _lastClipTick;

    private static readonly string[] SnipHosts =
        { "screenclippinghost", "snippingtool", "screensketch", "shellexperiencehost",
          "greenshot", "sharex", "lightshot", "flameshot", "snagit32", "snagiteditor", "picpick" };

    public void Show()
    {
        var hInstance = Win32.GetModuleHandle(null);
        _wndProc = WndProc;

        var wc = new Win32.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<Win32.WNDCLASSEX>(),
            lpfnWndProc = _wndProc,
            hInstance = hInstance,
            hCursor = Win32.LoadCursor(IntPtr.Zero, Win32.IDC_ARROW),
            lpszClassName = "HaloNotchWindow",
        };
        if (Win32.RegisterClassEx(ref wc) == 0)
            throw new InvalidOperationException($"RegisterClassEx failed: {Marshal.GetLastWin32Error()}");

        var work = default(Win32.RECT);
        Win32.SystemParametersInfo(Win32.SPI_GETWORKAREA, 0, ref work, 0);
        _workLeft = work.left;
        _workTop = work.top;
        _workWidth = work.right - work.left;
        LoadScale();

        int exStyle = Win32.WS_EX_LAYERED | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST | Win32.WS_EX_NOACTIVATE;
        Hwnd = Win32.CreateWindowEx(exStyle, "HaloNotchWindow", "Halo", Win32.WS_POPUP,
            _workLeft, _workTop, 10, 10, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        if (Hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"CreateWindowEx failed: {Marshal.GetLastWin32Error()}");

        Win32.ShowWindow(Hwnd, Win32.SW_SHOWNOACTIVATE);

        SetCapturable(false);
        Win32.AddClipboardFormatListener(Hwnd);

        _dropTarget = new Halo.Interop.FileDropTarget();
        Win32.RegisterDragDrop(Hwnd, _dropTarget);
    }

    private Halo.Interop.FileDropTarget? _dropTarget;

    public void SetCapturable(bool on)
    {
        if (Environment.GetEnvironmentVariable("HALO_CAPTURABLE") == "1") on = true;
        Win32.SetWindowDisplayAffinity(Hwnd, on ? 0u : Win32.WDA_EXCLUDEFROMCAPTURE);
    }

    public void SetVisible(bool visible)
        => Win32.ShowWindow(Hwnd, visible ? Win32.SW_SHOWNOACTIVATE : Win32.SW_HIDE);

    public void AssertTopmost()
        => Win32.SetWindowPos(Hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);

    public bool IsFullscreen(IntPtr fg)
    {
        if (fg == IntPtr.Zero || fg == Hwnd || IsDesktopWindow(fg)) return false;
        if (!Win32.GetWindowRect(fg, out var r)) return false;
        int cx = Win32.GetSystemMetrics(Win32.SM_CXSCREEN);
        int cy = Win32.GetSystemMetrics(Win32.SM_CYSCREEN);
        return r.left <= 0 && r.top <= 0 && r.right >= cx && r.bottom >= cy;
    }

    public bool ProbeBehind(out IntPtr behindRoot)
    {
        int cx = _workLeft + _workWidth / 2, cy = _workTop + 6;
        Win32.ShowWindow(Hwnd, Win32.SW_HIDE);
        System.Threading.Thread.Sleep(12);

        var behind = Win32.WindowFromPoint(new Win32.POINT { X = cx, Y = cy });
        var root = behind == IntPtr.Zero ? IntPtr.Zero : Win32.GetAncestor(behind, Win32.GA_ROOT);
        bool isDesktop = IsDesktopWindow(behind) || IsDesktopWindow(root);

        Win32.ShowWindow(Hwnd, Win32.SW_SHOWNOACTIVATE);
        behindRoot = isDesktop ? IntPtr.Zero : root;
        return isDesktop;
    }

    public void CaptureFrom(IntPtr behind)
    {
        if (behind == IntPtr.Zero || _capturing) return;
        _capturing = true;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try { DoCapture(behind); } catch { } finally { _capturing = false; }
        });
    }

    private void DoCapture(IntPtr behind)
    {
        if (!Win32.GetWindowRect(behind, out var wr)) return;
        int nx = _workLeft + (_workWidth - CaptureW) / 2, ny = _workTop;
        int sx = nx - wr.left, sy = ny - wr.top;

        var raw = new Bitmap(CaptureW, CaptureH, PixelFormat.Format24bppRgb);
        IntPtr src = Win32.GetWindowDC(behind);
        using (var g = Graphics.FromImage(raw))
        {
            g.Clear(Color.FromArgb(24, 24, 24));
            IntPtr dhdc = g.GetHdc();
            Win32.BitBlt(dhdc, 0, 0, CaptureW, CaptureH, src, sx, sy, Win32.SRCCOPY);
            g.ReleaseHdc(dhdc);
        }
        Win32.ReleaseDC(behind, src);

        if (IsMostlyBlack(raw))
        {
            var pw = CaptureViaPrintWindow(behind, wr, sx, sy);
            if (pw != null) { raw.Dispose(); raw = pw; }
        }

        var blurred = Blur(Blur(raw, 8), 5);
        raw.Dispose();
        lock (_bgLock) { var old = _bg; _bg = blurred; old?.Dispose(); }
        System.Threading.Interlocked.Increment(ref _captureVersion);
    }

    private static bool IsMostlyBlack(Bitmap bmp)
    {
        int dark = 0, total = 0;
        for (int y = 4; y < bmp.Height; y += 16)
            for (int x = 4; x < bmp.Width; x += 16)
            {
                var p = bmp.GetPixel(x, y);
                if (p.R < 12 && p.G < 12 && p.B < 12) dark++;
                total++;
            }
        return total > 0 && dark >= total * 0.97f;
    }

    private Bitmap? CaptureViaPrintWindow(IntPtr behind, Win32.RECT wr, int sx, int sy)
    {
        try
        {
            int ww = wr.right - wr.left, wh = wr.bottom - wr.top;
            if (ww <= 0 || wh <= 0 || ww > 10000 || wh > 10000) return null;
            using var full = new Bitmap(ww, wh, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(full))
            {
                IntPtr hdc = g.GetHdc();
                bool ok = Win32.PrintWindow(behind, hdc, Win32.PW_RENDERFULLCONTENT);
                g.ReleaseHdc(hdc);
                if (!ok) return null;
            }
            var region = new Bitmap(CaptureW, CaptureH, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(region))
            {
                g.Clear(Color.FromArgb(24, 24, 24));
                g.DrawImage(full, new Rectangle(0, 0, CaptureW, CaptureH),
                    new Rectangle(sx, sy, CaptureW, CaptureH), GraphicsUnit.Pixel);
            }
            return region;
        }
        catch { return null; }
    }

    private static bool IsDesktopWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return true;
        var buf = new char[80];
        int n = Win32.GetClassName(hwnd, buf, buf.Length);
        var cls = new string(buf, 0, n);
        return cls is "Progman" or "WorkerW" or "SysListView32" or "Shell_TrayWnd";
    }

    public void Render(int w, int h, int radius, int tintAlpha, float contentFade, float collapsedFade, bool glass,
        MenuFrame menu, Action<Graphics, int, int, float> drawContent, Action<Graphics, int, int, float> drawCollapsed)
    {
        int menuX = w + CircleGap + PrivacyPad;

        int maxFan = 0;
        if (menu.Show)
            foreach (var k in menu.RowCounts) maxFan = Math.Max(maxFan, k);
        int totalW = menu.Show ? menuX + CircleD * (1 + maxFan) : w;
        int totalH = Math.Max(h, menu.Show ? Math.Max(1, menu.RowIcons.Length) * CircleD : 0);

        bool privacy = Widgets.Privacy.Active;
        if (privacy)
        {
            totalW = Math.Max(totalW, w + CircleGap + PrivacyGap);
            int nDots = (Widgets.Privacy.Mic ? 1 : 0) + (Widgets.Privacy.Cam ? 1 : 0);
            totalH = Math.Max(totalH, (int)Math.Ceiling(DotTop + (nDots - 1) * DotStep + DotR + 2f));
        }

        float S = Scale;
        int pw = (int)MathF.Ceiling(totalW * S), ph = (int)MathF.Ceiling(totalH * S);

        var bmi = new Win32.BITMAPINFOHEADER
        {
            biSize = Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
            biWidth = pw,
            biHeight = -ph,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0,
        };
        IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
        IntPtr dib = Win32.CreateDIBSection(screenDc, ref bmi, 0, out var bits, IntPtr.Zero, 0);
        IntPtr memDc = Win32.CreateCompatibleDC(screenDc);
        IntPtr oldObj = Win32.SelectObject(memDc, dib);

        using (var bmp = new Bitmap(pw, ph, pw * 4, PixelFormat.Format32bppPArgb, bits))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.ScaleTransform(S, S);
            DrawShape(g, w, h, radius, tintAlpha, glass);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            if (collapsedFade > 0.01f) drawCollapsed(g, w, h, collapsedFade);
            drawContent(g, w, h, contentFade);
            if (HandleAlpha > 0.01f && contentFade > 0.5f)
            {

                using var hp = new Pen(Color.FromArgb((int)(160 * HandleAlpha * contentFade), 255, 255, 255), 3f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
                int m = 3;
                g.DrawArc(hp, w - 2 * radius + m, h - 2 * radius + m, 2 * (radius - m), 2 * (radius - m), 25, 40);
            }
            float ca = 1f - contentFade;
            if (menu.Show && ca > 0.01f && !menu.Dropping) DrawMenu(g, menuX, w, tintAlpha, glass, menu, ca);
            if (menu.Dropping) DrawDrop(g, menu, tintAlpha, w, h);
            if (privacy) DrawPrivacyDots(g, w);
        }

        var size = new Win32.SIZE { cx = pw, cy = ph };
        var src = new Win32.POINT { X = 0, Y = 0 };
        var dst = new Win32.POINT { X = _workLeft + (_workWidth - (int)(w * S)) / 2 + (int)OffsetX, Y = _workTop };
        var blend = new Win32.BLENDFUNCTION
        {
            BlendOp = Win32.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = Win32.AC_SRC_ALPHA,
        };
        Win32.UpdateLayeredWindow(Hwnd, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, Win32.ULW_ALPHA);

        Win32.SelectObject(memDc, oldObj);
        Win32.DeleteObject(dib);
        Win32.DeleteDC(memDc);
        Win32.ReleaseDC(IntPtr.Zero, screenDc);
    }

    private const float DotR = 3.3f, DotRing = 0.9f, DotStep = 8.5f, DotTop = 9f;
    private static readonly Color MicColor = Color.FromArgb(255, 159, 10);
    private static readonly Color CamColor = Color.FromArgb(48, 209, 88);
    private static void DrawPrivacyDots(Graphics g, int pillW)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cx = pillW + (CircleGap + PrivacyGap) / 2f;
        float y = DotTop;
        if (Widgets.Privacy.Mic) { Dot(g, cx, y, MicColor); y += DotStep; }
        if (Widgets.Privacy.Cam) { Dot(g, cx, y, CamColor); }
    }

    private static void Dot(Graphics g, float cx, float cy, Color c)
    {
        using (var kb = new SolidBrush(Color.FromArgb(230, 0, 0, 0)))
            g.FillEllipse(kb, cx - DotR, cy - DotR, DotR * 2, DotR * 2);
        float ri = DotR - DotRing;
        using var cb = new SolidBrush(c);
        g.FillEllipse(cb, cx - ri, cy - ri, ri * 2, ri * 2);
    }

    internal void DrawShape(Graphics g, int w, int h, int radius, int tintAlpha, bool glass)
    {
        const int ss = 2;
        using var big = new Bitmap(w * ss, h * ss, PixelFormat.Format32bppPArgb);
        using (var bg = Graphics.FromImage(big))
        {
            bg.SmoothingMode = SmoothingMode.AntiAlias;
            bg.PixelOffsetMode = PixelOffsetMode.HighQuality;
            bg.Clear(Color.Transparent);
            using var path = PillPath(w * ss, h * ss, radius * ss);
            lock (_bgLock)
            {
                if (glass && _bg != null)
                {
                    var clip = bg.Clip;
                    bg.SetClip(path);
                    int sx = (CaptureW - w) / 2;
                    bg.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    bg.DrawImage(_bg, new Rectangle(0, 0, w * ss, h * ss), new Rectangle(sx, 0, w, h), GraphicsUnit.Pixel);
                    bg.Clip = clip;
                }
            }
            using var tint = new SolidBrush(Color.FromArgb(tintAlpha, 8, 8, 8));
            bg.FillPath(tint, path);
        }

        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(big, new Rectangle(0, 0, w, h), new Rectangle(0, 0, w * ss, h * ss), GraphicsUnit.Pixel);
    }

    private void DrawMenu(Graphics g, int x, int pillW, int tintAlpha, bool glass, MenuFrame menu, float alpha)
    {
        alpha *= menu.Appear;
        if (alpha <= 0.01f) return;
        int rows = menu.RowIcons.Length;
        float openV = Math.Max(0f, menu.Open);
        float hf = CircleD + (rows - 1) * CircleD * openV;
        int or_ = menu.OpenRow;
        float rowEase = Math.Max(0f, menu.RowOpen);
        float extf = or_ >= 0 && or_ < rows ? menu.RowCounts[or_] * CircleD * rowEase : 0f;
        if (or_ > 0 && CircleD + or_ * CircleD > hf + 0.5f) extf = 0f;
        int mw = (int)Math.Ceiling(CircleD + extf);
        int mh = (int)Math.Ceiling(hf);
        const int ss = 2;
        int D = CircleD * ss;

        using var c = new Bitmap(mw * ss, mh * ss, PixelFormat.Format32bppPArgb);
        using (var cg = Graphics.FromImage(c))
        {
            cg.SmoothingMode = SmoothingMode.AntiAlias;
            cg.PixelOffsetMode = PixelOffsetMode.HighQuality;
            cg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            cg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            cg.Clear(Color.Transparent);

            using var path = new GraphicsPath(FillMode.Winding);
            using (var v = PillPath(D, mh * ss, D / 2))
                path.AddPath(v, false);
            if (extf > 0.5f)
                using (var hp = PillPath((int)((CircleD + extf) * ss), D, D / 2))
                {
                    using var m = new Matrix(1, 0, 0, 1, 0, or_ * D);
                    hp.Transform(m);
                    path.AddPath(hp, false);
                }

            int srcX = (CaptureW - pillW) / 2 + x;
            lock (_bgLock)
            {
                if (glass && _bg != null && srcX >= 0 && srcX + mw <= _bg.Width && CircleY + mh <= _bg.Height)
                {
                    var clip = cg.Clip;
                    cg.SetClip(path);
                    cg.DrawImage(_bg, new Rectangle(0, 0, mw * ss, mh * ss),
                        new Rectangle(srcX, CircleY, mw, mh), GraphicsUnit.Pixel);
                    cg.Clip = clip;
                }
            }
            using (var b = new SolidBrush(Color.FromArgb(tintAlpha, 8, 8, 8)))
                cg.FillPath(b, path);

            void Cell(string icon, Bitmap? img, float cx, float cy, float ia, Color? ring, float progress = -1f)
            {
                if (ia <= 0.01f) return;
                if (img != null)
                {

                    var accent = Widgets.Fx.AccentOf(img);
                    if (accent != Widgets.Fx.White)
                    {
                        var clip = cg.Clip;
                        cg.SetClip(path);
                        using var gb = new SolidBrush(Color.FromArgb((int)(20 * ia), accent));
                        cg.FillRectangle(gb, new RectangleF(cx, cy, D, D));
                        cg.Clip = clip;
                    }
                    DrawCircleImage(cg, img, cx, cy, D, ia);
                }
                else
                    DrawGlyphCentered(cg, icon, cx, cy, D, D * 0.45f, (int)(235 * ia));
                if (ring is { } rc)
                {
                    float inset = D * 0.19f - 2.5f * ss, dd = D - inset * 2;
                    var rr = new RectangleF(cx + inset, cy + inset, dd, dd);
                    if (progress >= 0f)
                    {

                        cg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using var track = new Pen(Color.FromArgb((int)(55 * ia), rc), 1.9f * ss);
                        cg.DrawEllipse(track, rr);
                        if (progress > 0.001f)
                            using (var arc = new Pen(Color.FromArgb((int)(230 * ia), rc), 2.2f * ss)
                            { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                                cg.DrawArc(arc, rr, -90f, 360f * Math.Clamp(progress, 0f, 1f));
                    }
                    else
                    {
                        using var pen = new Pen(Color.FromArgb((int)(140 * ia), rc), 1.9f * ss);
                        cg.DrawEllipse(pen, rr);
                    }
                }
            }

            for (int i = 0; i < rows; i++)
                Cell(menu.RowIcons[i], menu.RowImages[i], 0, i * D,
                    Math.Clamp((hf - i * CircleD) / CircleD, 0f, 1f), menu.RowRings[i], menu.RowProgress[i]);
            if (extf > 0.5f)
                for (int j = 0; j < menu.RowCounts[or_]; j++)
                    Cell(menu.SessIcons[or_][j], menu.SessImages[or_][j], (j + 1) * D, or_ * D,
                        Math.Clamp((extf - j * CircleD) / CircleD, 0f, 1f), menu.SessRings[or_][j]);
        }

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var dst = new Rectangle(x, CircleY, mw, mh);
        if (alpha >= 0.999f) { g.DrawImage(c, dst, 0, 0, c.Width, c.Height, GraphicsUnit.Pixel); return; }

        var stt = g.Save();
        float ax = x, ay = CircleY + CircleD / 2f;
        g.TranslateTransform(ax, ay);
        g.ScaleTransform(alpha, alpha);
        g.TranslateTransform(-ax, -ay);
        using (var attr = new ImageAttributes())
        {
            attr.SetColorMatrix(new ColorMatrix { Matrix33 = alpha });
            g.DrawImage(c, dst, 0, 0, c.Width, c.Height, GraphicsUnit.Pixel, attr);
        }
        g.Restore(stt);
    }

    private void DrawDrop(Graphics g, MenuFrame menu, int tintAlpha, int w, int h)
    {

        float p = menu.Drop - 1f;
        const float k1 = 1.9f, k3 = k1 + 1f;
        float e = 1f + k3 * p * p * p + k1 * p * p;
        float bx = menu.FromX + (menu.ToX - menu.FromX) * e;
        float by = menu.FromY + (menu.ToY - menu.FromY) * e;

        float r2 = CircleD / 2f * (menu.Outward ? 0.8f + 0.2f * e : 1f - 0.2f * e);
        var blob = new PointF(bx, by);
        var c1 = new PointF(w - h / 2f, h / 2f);
        float r1 = h / 2f;

        var fill = Color.FromArgb(Math.Min(tintAlpha + 50, 255), 8, 8, 8);
        using (var b = new SolidBrush(fill))
        {
            Metaball(g, b, c1, r1, blob, r2);
            g.FillEllipse(b, blob.X - r2, blob.Y - r2, r2 * 2, r2 * 2);
        }

        float a = menu.Outward
            ? Math.Clamp(menu.Drop / 0.25f, 0f, 1f)
            : menu.Drop < 0.8f ? 1f : 1f - (menu.Drop - 0.8f) / 0.2f;
        if (menu.DropImage != null)
        {
            DrawCircleImage(g, menu.DropImage, blob.X - r2, blob.Y - r2, r2 * 2, a);
            return;
        }
        using var f = new Font("Segoe MDL2 Assets", r2 * 0.9f, GraphicsUnit.Pixel);
        using var ib = new SolidBrush(Color.FromArgb((int)(235 * a), 255, 255, 255));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(menu.DropIcon, f, ib, new RectangleF(blob.X - r2, blob.Y - r2, r2 * 2, r2 * 2), sf);
    }

    private static void Metaball(Graphics g, Brush brush, PointF c1, float r1, PointF c2, float r2)
    {
        float dx = c2.X - c1.X, dy = c2.Y - c1.Y;
        float d = MathF.Sqrt(dx * dx + dy * dy);
        if (d <= 0.001f || d >= r1 + r2 || d <= MathF.Abs(r1 - r2)) return;

        const float handle = 2.4f, v = 0.5f;
        float u1 = MathF.Acos((r1 * r1 + d * d - r2 * r2) / (2 * r1 * d));
        float u2 = MathF.Acos((r2 * r2 + d * d - r1 * r1) / (2 * r2 * d));
        float ab = MathF.Atan2(dy, dx);
        float maxSpread = MathF.Acos((r1 - r2) / d);

        float a1 = ab + u1 + (maxSpread - u1) * v;
        float a2 = ab - u1 - (maxSpread - u1) * v;
        float a3 = ab + MathF.PI - u2 - (MathF.PI - u2 - maxSpread) * v;
        float a4 = ab - MathF.PI + u2 + (MathF.PI - u2 - maxSpread) * v;

        var p1 = Pt(c1, r1, a1); var p2 = Pt(c1, r1, a2);
        var p3 = Pt(c2, r2, a3); var p4 = Pt(c2, r2, a4);

        float total = r1 + r2;
        float d2 = Math.Min(v * handle, Dist(p1, p3) / total) * Math.Min(1f, d * 2f / total);
        float h1 = r1 * d2, h2 = r2 * d2;

        using var path = new GraphicsPath();
        path.AddBezier(p1, Pt(p1, h1, a1 - MathF.PI / 2), Pt(p3, h2, a3 + MathF.PI / 2), p3);
        path.AddLine(p3, p4);
        path.AddBezier(p4, Pt(p4, h2, a4 - MathF.PI / 2), Pt(p2, h1, a2 + MathF.PI / 2), p2);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    private static PointF Pt(PointF c, float r, float a) => new(c.X + r * MathF.Cos(a), c.Y + r * MathF.Sin(a));
    private static float Dist(PointF a, PointF b) { float dx = a.X - b.X, dy = a.Y - b.Y; return MathF.Sqrt(dx * dx + dy * dy); }

    private static readonly FontFamily _cellGlyphFont = new("Segoe MDL2 Assets");
    private static void DrawGlyphCentered(Graphics g, string glyph, float x, float y, float box, float px, int alpha)
    {
        if (string.IsNullOrEmpty(glyph)) return;
        using var path = new GraphicsPath();
        using var sf = new StringFormat(StringFormat.GenericTypographic);
        path.AddString(glyph, _cellGlyphFont, (int)FontStyle.Regular, px, PointF.Empty, sf);
        path.Flatten();
        var b = path.GetBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        using var m = new Matrix();
        m.Translate(MathF.Round(x + (box - b.Width) / 2f - b.X), MathF.Round(y + (box - b.Height) / 2f - b.Y));
        path.Transform(m);
        using var br = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255));
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillPath(br, path);
        g.SmoothingMode = old;
    }

    private static void DrawCircleImage(Graphics g, Bitmap img, float x, float y, float box, float alpha)
    {
        img = CenteredSquare(img);
        float inset = box * 0.19f, d = box - inset * 2;
        var circle = new RectangleF(x + inset, y + inset, d, d);
        int s = Math.Max(1, (int)Math.Ceiling(d));

        using var scaled = new Bitmap(s, s, PixelFormat.Format32bppPArgb);
        using (var sg = Graphics.FromImage(scaled))
        {
            sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
            sg.SmoothingMode = SmoothingMode.HighQuality;
            using var ia = new ImageAttributes();
            ia.SetWrapMode(WrapMode.TileFlipXY);
            ia.SetColorMatrix(new ColorMatrix { Matrix33 = alpha });
            int side = Math.Min(img.Width, img.Height);
            sg.DrawImage(img, new Rectangle(0, 0, s, s),
                (img.Width - side) / 2, (img.Height - side) / 2, side, side, GraphicsUnit.Pixel, ia);
        }

        using var tb = new TextureBrush(scaled) { WrapMode = WrapMode.Clamp };
        tb.TranslateTransform(circle.X, circle.Y);
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var p = new GraphicsPath()) { p.AddEllipse(circle); g.FillPath(tb, p); }
        g.SmoothingMode = old;
    }

    private static readonly System.Collections.Generic.Dictionary<Bitmap, Bitmap> _centered = new();
    private static Bitmap CenteredSquare(Bitmap src)
    {
        lock (_centered)
        {
            if (_centered.TryGetValue(src, out var c)) return c;
            var made = MakeCenteredSquare(src);
            _centered[src] = made;
            return made;
        }
    }

    private static Bitmap MakeCenteredSquare(Bitmap src)
    {
        try
        {
            var data = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int minX = src.Width, minY = src.Height, maxX = -1, maxY = -1;
            try
            {
                int stride = data.Stride;
                var buf = new byte[stride * src.Height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length);
                for (int yy = 0; yy < src.Height; yy++)
                    for (int xx = 0; xx < src.Width; xx++)
                        if (buf[yy * stride + xx * 4 + 3] > 24)
                        {
                            if (xx < minX) minX = xx; if (xx > maxX) maxX = xx;
                            if (yy < minY) minY = yy; if (yy > maxY) maxY = yy;
                        }
            }
            finally { src.UnlockBits(data); }
            if (maxX < minX) return src;

            int edge = Math.Max(1, Math.Min(src.Width, src.Height) / 64);
            if (minX <= edge && maxX >= src.Width - 1 - edge) return src;
            if (minY <= edge && maxY >= src.Height - 1 - edge) return src;

            float dx = (src.Width - 1) / 2f - (minX + maxX) / 2f;
            float dy = (src.Height - 1) / 2f - (minY + maxY) / 2f;
            if (Math.Abs(dx) < 1.5f && Math.Abs(dy) < 1.5f) return src;

            var shifted = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(shifted))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, (int)Math.Round(dx), (int)Math.Round(dy), src.Width, src.Height);
            }
            return shifted;
        }
        catch { return src; }
    }

    private static Bitmap Blur(Bitmap src, int factor)
    {
        int sw = Math.Max(1, src.Width / factor), sh = Math.Max(1, src.Height / factor);
        var small = new Bitmap(sw, sh, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(small))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.DrawImage(src, new Rectangle(0, 0, sw, sh), new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
        }
        var big = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(big))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(small, new Rectangle(0, 0, src.Width, src.Height), new Rectangle(0, 0, sw, sh), GraphicsUnit.Pixel);
        }
        small.Dispose();
        return big;
    }

    private static GraphicsPath PillPath(int w, int h, int r)
    {
        int d = r * 2;
        var p = new GraphicsPath();
        p.AddLine(0, 0, w, 0);
        p.AddArc(w - d, h - d, d, d, 0, 90);
        p.AddArc(0, h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private void HandleClipboard()
    {
        uint seq = Win32.GetClipboardSequenceNumber();
        if (seq == _lastClipSeq) return;
        _lastClipSeq = seq;
        long now = Environment.TickCount64;
        if (now - _lastClipTick < 800) return;
        if (!Win32.IsClipboardFormatAvailable(Win32.CF_BITMAP)) return;
        bool shot = OwnerIsCapture();
        var bmp = ReadClipboardBitmap();
        if (bmp != null) { _lastClipTick = now; ClipboardImage?.Invoke(bmp, shot); }
    }

    private static bool OwnerIsCapture()
    {
        try
        {
            IntPtr owner = Win32.GetClipboardOwner();
            if (owner == IntPtr.Zero) return true;
            Win32.GetWindowThreadProcessId(owner, out uint pid);
            if (pid == 0) return true;
            using var p = System.Diagnostics.Process.GetProcessById((int)pid);
            string pn = p.ProcessName.ToLowerInvariant();
            foreach (var s in SnipHosts) if (pn.Contains(s)) return true;
            return false;
        }
        catch { return true; }
    }

    private Bitmap? ReadClipboardBitmap()
    {
        if (!Win32.OpenClipboard(Hwnd)) return null;
        try
        {
            IntPtr h = Win32.GetClipboardData(Win32.CF_BITMAP);
            if (h == IntPtr.Zero) return null;
            using var tmp = Image.FromHbitmap(h);
            return new Bitmap(tmp);
        }
        catch { return null; }
        finally { Win32.CloseClipboard(); }
    }

    private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == Win32.WM_DESTROY)
        {
            Win32.PostQuitMessage(0);
            return IntPtr.Zero;
        }
        if (msg == Win32.WM_CLIPBOARDUPDATE)
        {
            HandleClipboard();
            return IntPtr.Zero;
        }
        if (msg is Win32.WM_DISPLAYCHANGE or Win32.WM_SETTINGCHANGE)
        {

            var work = default(Win32.RECT);
            Win32.SystemParametersInfo(Win32.SPI_GETWORKAREA, 0, ref work, 0);
            _workLeft = work.left;
            _workTop = work.top;
            _workWidth = work.right - work.left;
            lock (_bgLock) { _bg?.Dispose(); _bg = null; }
        }
        return Win32.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
