using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Halo.ClaudeCode;

namespace Halo.Widgets;

internal sealed class GenericAgentWidget : IWidget
{
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);
    private static readonly Color Green = Color.FromArgb(62, 207, 92);
    private static readonly Color Amber = Color.FromArgb(255, 176, 32);
    private static readonly Color Red = Color.FromArgb(229, 72, 77);

    public static string Directory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".halo", "agents");

    public static StatusStore NewStore() => new(
        Path.Combine(Directory, "agent.json"), StatusStore.GetProcessStartedAt, watchFiles: true);

    private readonly StatusStore _store;
    private readonly int _slot;

    public GenericAgentWidget(StatusStore store, int slot)
    {
        _store = store;
        _slot = slot;
    }

    private CcStatus? Live => _store.SessionLive(_slot);

    public string Icon => "";
    public bool IsActive => Live is not null;
    public IEnumerable<int> OwnerPids => Live is { } st ? new[] { st.Pid, st.ConsolePid } : Array.Empty<int>();
    public int Version => _store.Version;
    public string GroupKey => Live?.Name?.ToLowerInvariant() ?? "agent";
    public Color? Ring => Live is { } st ? RingColor(st) : null;

    private static Color RingColor(CcStatus st) => st.State switch
    {
        "working" => Green,
        "waiting_input" or "waiting" => Amber,
        "error" => Red,
        _ => White,
    };

    private Bitmap? _icon, _badged;
    private string? _iconKey;

    public Bitmap? IconImage
    {
        get
        {
            var path = Live?.Icon;
            if (string.IsNullOrEmpty(path)) return null;
            if (path != _iconKey)
            {
                _badged?.Dispose();
                _icon?.Dispose();
                try { _icon = new Bitmap(path); } catch { _icon = null; }
                _badged = _icon is null ? null : Fx.Badge(_icon, (char)('1' + _slot));
                _iconKey = path;
            }
            return _badged;
        }
    }

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
        => Array.Empty<(RectangleF, Action<PointF>)>();

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        var st = Live;
        if (st is null) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float sz = (h - 16f) * 0.82f, x = 13, y = (h - sz) / 2f;
        using (var pen = new Pen(Mul(RingColor(st), fade * 0.55f), 1.9f))
            g.DrawEllipse(pen, x - 2.5f, y - 2.5f, sz + 5f, sz + 5f);
        DrawIconCircle(g, x, y, sz, fade);

        string text = st.State == "working" ? Verb(st) + Elapsed(st) : st.Name ?? "agent";
        using var f = new Font("Segoe UI Semibold", 15f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade));
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };

        g.DrawString(text, f, b, new RectangleF(x + sz + 8, -1.5f, w - (x + sz + 8) - 14, h), sf);
    }

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        if (fade <= 0.01f) return;
        var st = Live;
        if (st is null) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Fx.Glow(g, w, h, fade, w * 0.16f, h * 0.35f, w * 0.85f, h * 1.2f, 30, Fx.AccentOf(IconImage));

        float x = 26;
        using (var pen = new Pen(Mul(RingColor(st), fade * 0.55f), 2.2f))
            g.DrawEllipse(pen, x - 3, 23, 46, 46);
        DrawIconCircle(g, x, 26, 40, fade);

        using var titleF = new Font("Segoe UI Semibold", 22f, GraphicsUnit.Pixel);
        using var bodyF = new Font("Segoe UI", 15f, GraphicsUnit.Pixel);
        using var dimB = new SolidBrush(Mul(Dim, fade));
        using var whiteB = new SolidBrush(Mul(White, fade));
        g.DrawString(st.Name ?? "Agent", titleF, whiteB, x + 56, 28);
        g.DrawString(Verb(st) + Elapsed(st), bodyF, dimB, x + 56, 60);
        if (!string.IsNullOrEmpty(st.Cwd))
            g.DrawString(st.Cwd, bodyF, dimB, x, 108);
        if (!string.IsNullOrEmpty(st.Message))
            g.DrawString(st.Message, bodyF, whiteB, x, 136);
    }

    private void DrawIconCircle(Graphics g, float x, float y, float sz, float fade)
    {
        var img = IconImage;
        if (img != null)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(x, y, sz, sz);
            var clip = g.Clip;
            g.SetClip(path);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, x, y, sz, sz);
            g.Clip = clip;
            return;
        }
        using var f = new Font("Segoe MDL2 Assets", sz * 0.62f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(Icon, f, b, new RectangleF(x, y, sz, sz), sf);
    }

    private static string Verb(CcStatus st) => st.State switch
    {
        "working" => string.IsNullOrEmpty(st.CurrentTool) ? "working…" : st.CurrentTool!,
        "waiting_input" or "waiting" => "your move ;)",
        "error" => "error",
        _ => "idle",
    };

    private static string Elapsed(CcStatus st)
    {
        if (st.State != "working" || !DateTimeOffset.TryParse(st.StartedAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var t0)) return "";
        var e = DateTimeOffset.UtcNow - t0;
        if (e < TimeSpan.Zero || e > TimeSpan.FromDays(1)) return "";
        return e.TotalMinutes >= 1 ? $" · {(int)e.TotalMinutes}m {e.Seconds}s" : $" · {e.Seconds}s";
    }

    private static Color Mul(Color c, float a)
        => Color.FromArgb((int)Math.Clamp(c.A * a, 0, 255), c.R, c.G, c.B);
}
