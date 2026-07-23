using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Halo.Codex;

namespace Halo.Widgets;

internal enum CodexCancelRoute { None, Cli, Desktop }

internal sealed class CodexWidget : IWidget
{
    private static readonly Color Blue = Color.FromArgb(91, 157, 255);
    private static readonly Color Green = Color.FromArgb(62, 207, 92);
    private static readonly Color Amber = Color.FromArgb(255, 176, 32);
    private static readonly Color Red = Color.FromArgb(229, 72, 77);
    private static readonly Color Track = Color.FromArgb(38, 255, 255, 255);
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);

    private readonly CodexStatusStore _store;
    private readonly CodexSurface _surface;
    private readonly Action _cancel;
    private readonly Func<bool> _canCancelDesktop;

    public CodexWidget(CodexStatusStore store, CodexSurface surface, Action cancel, Func<bool>? canCancelDesktop = null)
    {
        _store = store;
        _surface = surface;
        _cancel = cancel;
        _canCancelDesktop = canCancelDesktop ?? (static () => false);
        CodexLimits.Attach(store);
    }

    private CodexSnapshot? Current => _store.Candidate(_surface);

    private static readonly Bitmap? OpenAiIcon = LoadIcon();

    private static readonly Color Accent = Fx.AccentOf(OpenAiIcon) is var a && a != Fx.White
        ? a : Color.FromArgb(16, 163, 127);

    public string Icon => "\uE756";

    private Bitmap? _badged;

    public Bitmap? IconImage
    {
        get
        {
            if (OpenAiIcon is null) return null;
            var other = _surface == CodexSurface.Desktop ? CodexSurface.Cli : CodexSurface.Desktop;
            if (_store.Candidate(other) is null) return OpenAiIcon;
            return _badged ??= Fx.Badge(OpenAiIcon, _surface == CodexSurface.Desktop ? '1' : '2');
        }
    }

    public string Id => "codex";
    public string? AgentState => Current?.State;

    public bool IsActive => Current is not null;
    public Color? Ring => Current is { } st ? RingColor(st) : null;
    public int Version => _store.Version + CodexNetMon.Version + CodexLimits.Version;
    public bool IsDesktop => _surface == CodexSurface.Desktop;
    public AgentNotice AgentNotice => Current is { } status
        ? new AgentNotice(status.State, status.CompactedAt, status.Message)
        : AgentNotice.None;
    public IEnumerable<int> OwnerPids => Current is { } st ? new[] { st.Pid, st.ConsolePid } : Array.Empty<int>();

    public bool Animating => _appear < 1f || Compacting(Current);

    private string _shownKey = "";
    private float _appear = 1f;

    private static Bitmap? LoadIcon()
    {
        try
        {
            using var s = typeof(CodexWidget).Assembly.GetManifestResourceStream("Halo.Assets.openai.png");
            return s != null ? new Bitmap(s) : null;
        }
        catch { return null; }
    }

    private bool CanCancel
    {
        get
        {
            var snapshot = Current;
            var canCancelDesktop = snapshot is { Source: CodexSurface.Desktop, State: "working" } &&
                _canCancelDesktop();
            return GetCancelRoute(snapshot, canCancelDesktop) != CodexCancelRoute.None;
        }
    }

    internal static CodexCancelRoute GetCancelRoute(CodexSnapshot? snapshot, bool canCancelDesktop) => snapshot switch
    {
        { Source: CodexSurface.Cli, State: "working", ConsolePid: > 0 } => CodexCancelRoute.Cli,
        { Source: CodexSurface.Desktop, State: "working" } when canCancelDesktop => CodexCancelRoute.Desktop,
        _ => CodexCancelRoute.None,
    };

    private bool _wasOpen;

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        bool open = fade > 0.01f;
        if (open && !_wasOpen) CodexLimits.ForceRefresh();
        _wasOpen = open;
        if (open)
        {
            var snapshot = Current;
            if (snapshot is not null) CodexLimits.UpdateFrom(snapshot);
            CodexNetMon.Poke();
            Halo.ClaudeCode.IpCountry.Poke();
            Fx.Glow(g, w, h, fade, w * 0.16f, h * 0.35f, w * 0.85f, h * 1.2f, 30, Accent);
            DrawExpanded(g, w, h, fade, snapshot);
        }
    }

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        var st = Current;
        float sz = (h - 16f) * 0.82f, x = 13, y = (h - sz) / 2f;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Fx.Glow(g, w, h, fade, x + sz / 2f, h / 2f, w * 0.7f, h * 2.2f, 26, Accent);
        if (Compacting(st))
        {
            float pulse = 0.5f - 0.5f * MathF.Cos(Environment.TickCount % 2400 / 2400f * MathF.Tau);
            using var pb = new SolidBrush(Mul(Blue, fade * (0.05f + 0.11f * pulse)));
            using var pp = Fx.PillPath(w, h, h / 2f);
            g.FillPath(pb, pp);
        }

        using (var pen = new Pen(Mul(RingColor(st), fade * 0.55f), 1.9f))
            g.DrawEllipse(pen, x - 2.5f, y - 2.5f, sz + 5f, sz + 5f);
        if (OpenAiIcon != null) DrawIcon(g, OpenAiIcon, x, y, sz, fade, sz / 2f);
        else
            using (var db = new SolidBrush(Mul(RingColor(st), fade)))
                g.FillEllipse(db, x, y, sz, sz);

        string verb = OutageText() ?? (LimitHit ? "outta juice :(" : st?.State switch
        {
            "working" => ToolVerb(st.CurrentTool),
            "compacting" when Compacting(st) => "compacting…",
            "waiting_input" => "your move ;)",
            _ => IdleMood(st),
        });
        string el = LimitHit ? LimitReset() : Elapsed(st);
        if (Compacting(st) && !LimitHit && el.Length > 0) el = CompactPct(st!) + " · " + el;
        if (verb != _shownKey) { _shownKey = verb; _appear = 0f; }
        else if (_appear < 1f) _appear = Math.Min(1f, _appear + 0.1f);
        float e = 1f - MathF.Pow(1f - _appear, 3);
        bool busy = st?.State == "working" || Compacting(st) || LimitHit;
        bool centred = !busy && st?.State != "waiting_input";

        float textX = x + sz + 11;
        if (st?.State == "waiting_input") textX += 16;
        using var tf2 = new Font("Segoe UI", 13f, GraphicsUnit.Pixel);
        float elW = el.Length > 0 ? g.MeasureString(el, tf2, int.MaxValue, StringFormat.GenericTypographic).Width : 0;
        float avail = (w - 14) - textX - (elW > 0 ? elW + 10 : 0);

        float px = 15f;
        using (var fm = new Font("Segoe UI Semibold", px, GraphicsUnit.Pixel))
        {
            var m0 = g.MeasureString(verb, fm, int.MaxValue, StringFormat.GenericTypographic);
            if (m0.Width > avail && m0.Width > 0) px = Math.Max(9f, px * avail / m0.Width);
        }
        using var f = new Font("Segoe UI Semibold", px, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade * e));
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = centred ? StringAlignment.Center : StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        var clip = g.Clip;
        g.SetClip(new RectangleF(x + sz + 2, 0, w - (x + sz + 2), h));
        float zoneW = centred ? avail - 34f : avail + 16f;

        g.DrawString(verb, f, b, new RectangleF(textX - 16f * (1f - e), -1.5f, zoneW, h), sf);
        g.Clip = clip;

        if (elW > 0)
            using (var eb = new SolidBrush(Mul(Dim, fade * e)))
            using (var esf = new StringFormat(StringFormat.GenericTypographic)
            { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(el, tf2, eb, new RectangleF(w - 14 - elW - 4, -1.5f, elW + 4, h), esf);

    }

    private static DateTimeOffset? _cancelledCompactKey;

    public static void MarkCompactCancelled(DateTimeOffset? startedAt) => _cancelledCompactKey = startedAt;

    private static bool Compacting(CodexSnapshot? st) =>
        st?.State == "compacting" && st.StartedAt is { } t && t != _cancelledCompactKey
        && DateTimeOffset.UtcNow - t < TimeSpan.FromMinutes(3);

    private static string CompactPct(CodexSnapshot st) => st.StartedAt is { } t
        ? $"~{(int)Math.Clamp(100 * (DateTimeOffset.UtcNow - t).TotalSeconds / 180, 1, 99)}%" : "";

    private static void DrawIcon(Graphics g, Bitmap img, float x, float y, float size, float fade, float radius)
    {
        using var path = Rounded(new RectangleF(x, y, size, size), radius);
        int s = Math.Max(1, (int)Math.Ceiling(size));
        using var scaled = new Bitmap(s, s, PixelFormat.Format32bppPArgb);
        using (var sg = Graphics.FromImage(scaled))
        {
            sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using var ia = new ImageAttributes();
            ia.SetWrapMode(WrapMode.TileFlipXY);
            ia.SetColorMatrix(new ColorMatrix { Matrix33 = fade });
            int side = Math.Min(img.Width, img.Height);
            sg.DrawImage(img, new Rectangle(0, 0, s, s), (img.Width - side) / 2, (img.Height - side) / 2, side, side, GraphicsUnit.Pixel, ia);
        }
        using var tb = new TextureBrush(scaled) { WrapMode = WrapMode.Clamp };
        tb.TranslateTransform(x, y);
        g.FillPath(tb, path);
    }

    private void DrawExpanded(Graphics g, int w, int h, float a, CodexSnapshot? st)
    {
        int pad = 26;
        using var title = new Font("Segoe UI Semibold", 21f, GraphicsUnit.Pixel);
        using var body = new Font("Segoe UI", 14f, GraphicsUnit.Pixel);
        using var small = new Font("Segoe UI", 12.5f, GraphicsUnit.Pixel);

        var dot = StateColor(st?.State == "compacting" && !Compacting(st) ? null : st?.State);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Fx.DrawFlagGhost(g, Halo.ClaudeCode.IpCountry.Flag, w, h, a);
        using (var db = new SolidBrush(Mul(dot, a)))
            g.FillEllipse(db, pad, pad + 8, 11, 11);
        using (var tb = new SolidBrush(Mul(White, a)))
            g.DrawString("Codex", title, tb, pad + 20, pad - 2);
        string line = st?.State == "waiting_input" && !string.IsNullOrEmpty(st.Message)
            ? st.Message! : Activity(st);
        using (var ab = new SolidBrush(Mul(st?.State == "waiting_input" ? Amber : Dim, a)))
            g.DrawString(line, small, ab, pad + 20, pad + 24);

        float y = pad + 58;
        int barW = w - pad * 2;
        if (st is null)
        {
            using var nb = new SolidBrush(Mul(Dim, a));
            g.DrawString("No active Codex session", body, nb, pad, y + 4);
            y += 40;
        }
        else if (st.PresentFields.HasFlag(CodexSnapshotFields.ContextUsed | CodexSnapshotFields.ContextMax) && st.ContextMax > 0)
        {
            double ctx = ContextFrac(st);
            long maxK = st.ContextMax / 1000, usedK = Math.Min(st.ContextUsed / 1000, maxK);
            string maxLabel = maxK >= 1000 ? $"{maxK / 1000f:0.#}M" : $"{maxK}K";
            DrawBar(g, pad, y, barW, "Context", $"{usedK}K / {maxLabel}", ctx, Blue, a, body, small);
            y += 40;
        }

        string LimitValue(float f, DateTimeOffset? reset, float rowY)
        {
            bool hov = WidgetInput.Over && WidgetInput.Mouse.Y >= rowY && WidgetInput.Mouse.Y < rowY + 36
                && WidgetInput.Mouse.X >= pad && WidgetInput.Mouse.X <= pad + barW;
            if (reset is null) return Pct(f);
            return hov ? $"{f * 100:0.#}%  ·  resets {reset.Value.ToLocalTime():ddd HH:mm}"
                       : $"{Pct(f)}  ·  {ResetIn(reset.Value)}";
        }
        if (CodexLimits.Current?.Primary is { } primary)
        {
            float used = (float)(primary.UsedPercent / 100d);
            DrawBar(g, pad, y, barW, LimitLabel(primary),
                LimitValue(used, primary.ResetsAt, y), used, UsageColor(used), a, body, small);
            y += 40;
        }
        if (CodexLimits.Current?.Secondary is { } secondary)
        {
            float used = (float)(secondary.UsedPercent / 100d);
            DrawBar(g, pad, y, barW, LimitLabel(secondary),
                LimitValue(used, secondary.ResetsAt, y), used, UsageColor(used), a, body, small);
        }

        var rr = RefreshRect(w, h);
        bool rHover = WidgetInput.Over && rr.Contains(WidgetInput.Mouse);
        string age = CodexLimits.LastSuccess == DateTimeOffset.MinValue ? "usage never fetched"
            : $"updated {AgeText(DateTime.UtcNow - CodexLimits.LastSuccess)}";
        string rtxt = $"{age}  ·  ⟳ refresh";
        using (var rb = new SolidBrush(Mul(rHover ? White : Dim, a)))
        using (var rsf = new StringFormat(StringFormat.GenericTypographic)
        { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            g.DrawString(rtxt, small, rb, rr, rsf);

        DrawCancel(g, w, h, a, body);
    }

    private static string LimitLabel(CodexLimit limit) => limit.WindowMinutes switch
    {
        300 => "5-hour limit",
        10_080 => "Weekly limit",
        _ => "Plan limit",
    };

    private void DrawCancel(Graphics g, int w, int h, float a, Font font)
    {
        var r = CancelRect(w, h);
        bool on = CanCancel;
        var col = on ? Red : Color.FromArgb(120, 255, 255, 255);
        float ba = on ? a : a * 0.4f;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(Mul(Color.FromArgb(46, col), a)))
            g.FillEllipse(b, r.X, r.Y, r.Width, r.Height);
        using (var pen = new Pen(Mul(col, ba), 1.4f))
            g.DrawEllipse(pen, r.X, r.Y, r.Width, r.Height);
        float sq = r.Width * 0.34f;
        using (var sb = new SolidBrush(Mul(on ? Red : Dim, a)))
        using (var sp = Rounded(new RectangleF(r.X + (r.Width - sq) / 2, r.Y + (r.Height - sq) / 2, sq, sq), 2f))
            g.FillPath(sb, sp);

        DrawNet(g, r.X - 26, a);
    }

    private static void DrawNet(Graphics g, float rightX, float a)
    {
        var (net, api) = CodexNetMon.Snapshot();
        const float stepX = 5f, gh = 22f;
        int n = net.Length;
        float gw = (n - 1) * stepX, x0 = rightX - gw, top = 19, barsY = top + 14;

        int cap = 150;
        foreach (var v in net) if (v > cap) cap = v;
        foreach (var v in api) if (v > cap) cap = v;
        cap = (cap + 49) / 50 * 50;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        float ax = x0 - 5;
        using (var axis = new Pen(Mul(Dim, a * 0.6f), 1f))
        {
            g.DrawLine(axis, ax, barsY - 3, ax, barsY + gh);
            g.DrawLine(axis, ax, barsY + gh, x0 + gw, barsY + gh);
        }
        using (var tf = new Font("Segoe UI", 9f, GraphicsUnit.Pixel))
        using (var tb = new SolidBrush(Mul(Dim, a * 0.8f)))
        {
            var sz = g.MeasureString(cap.ToString(), tf);
            g.DrawString(cap.ToString(), tf, tb, ax - sz.Width - 1, barsY - 5);
            sz = g.MeasureString("0", tf);
            g.DrawString("0", tf, tb, ax - sz.Width - 1, barsY + gh - 9);
        }

        float Y(int ms) => barsY + gh * (1 - Math.Clamp((float)ms / cap, 0.04f, 1f));

        void Series(int[] s, Color col)
        {
            var pts = new List<(PointF p, bool lost)>();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == CodexNetMon.Empty) continue;
                bool lost = s[i] == CodexNetMon.Lost;
                pts.Add((new PointF(x0 + i * stepX, lost ? barsY : Y(s[i])), lost));
            }
            using var ok = new Pen(Mul(col, a), 1.6f) { LineJoin = LineJoin.Round };
            using var bad = new Pen(Mul(Red, a), 1.6f) { LineJoin = LineJoin.Round };
            for (int i = 1; i < pts.Count; i++)
                g.DrawLine(pts[i - 1].lost || pts[i].lost ? bad : ok, pts[i - 1].p, pts[i].p);
            if (pts.Count > 0)
                using (var db = new SolidBrush(Mul(pts[^1].lost ? Red : col, a)))
                    g.FillEllipse(db, pts[^1].p.X - 2f, pts[^1].p.Y - 2f, 4.5f, 4.5f);
        }
        Series(net, Green);
        Series(api, Blue);

        int lastN = LastSample(net), lastA = LastSample(api);
        string tn = "net " + (lastN == CodexNetMon.Empty ? "…" : lastN == CodexNetMon.Lost ? ":(" : lastN.ToString());
        string ta = "api " + (lastA == CodexNetMon.Empty ? "…" : lastA == CodexNetMon.Lost ? ":(" : lastA + " ms");
        using (var f = new Font("Segoe UI", 11f, GraphicsUnit.Pixel))
        {
            float wN = g.MeasureString(tn, f).Width, wS = g.MeasureString(" · ", f).Width, wA = g.MeasureString(ta, f).Width;
            float lx = rightX - (wN + wS + wA);
            using (var b = new SolidBrush(Mul(lastN == CodexNetMon.Lost ? Red : Green, a))) g.DrawString(tn, f, b, lx, top - 2);
            using (var b = new SolidBrush(Mul(Dim, a))) g.DrawString(" · ", f, b, lx + wN, top - 2);
            using (var b = new SolidBrush(Mul(lastA == CodexNetMon.Lost ? Red : Blue, a))) g.DrawString(ta, f, b, lx + wN + wS, top - 2);
        }

        DrawNetHover(g, a, net, api, x0, stepX, barsY, gh, rightX, Y);
    }

    private static int LastSample(int[] s)
    {
        for (int i = s.Length - 1; i >= 0; i--) if (s[i] != CodexNetMon.Empty) return s[i];
        return CodexNetMon.Empty;
    }

    private static void DrawNetHover(Graphics g, float a, int[] net, int[] api,
        float x0, float stepX, float top, float gh, float right, Func<int, float> Y)
    {
        var m = WidgetInput.Mouse;
        if (!WidgetInput.Over || m.X < x0 - 9 || m.X > right + 6 || m.Y < top - 10 || m.Y > top + gh + 10)
            return;
        int idx = Math.Clamp((int)MathF.Round((m.X - x0) / stepX), 0, net.Length - 1);
        int vN = net[idx], vA = api[idx];
        if (vN == CodexNetMon.Empty && vA == CodexNetMon.Empty) return;

        float gx = x0 + idx * stepX;
        using (var guide = new Pen(Mul(White, a * 0.35f), 1f) { DashStyle = DashStyle.Dot })
            g.DrawLine(guide, gx, top - 3, gx, top + gh);
        void Mark(int v, Color col)
        {
            if (v == CodexNetMon.Empty) return;
            using var hb = new SolidBrush(Mul(v == CodexNetMon.Lost ? Red : col, a));
            g.FillEllipse(hb, gx - 2.5f, (v == CodexNetMon.Lost ? top : Y(v)) - 2.5f, 5.5f, 5.5f);
        }
        Mark(vN, Green); Mark(vA, Blue);

        int lostN = 0, cntN = 0, lostA = 0, cntA = 0;
        for (int i = 0; i < net.Length; i++)
        {
            if (net[i] != CodexNetMon.Empty) { cntN++; if (net[i] == CodexNetMon.Lost) lostN++; }
            if (api[i] != CodexNetMon.Empty) { cntA++; if (api[i] == CodexNetMon.Lost) lostA++; }
        }
        string F(int v) => v == CodexNetMon.Lost ? ":(" : v == CodexNetMon.Empty ? "–" : $"{v} ms";
        var lines = new List<(string t, Color c)>
        {
            ($"net {F(vN)}   api {F(vA)}", White),
            ($"loss  net {lostN}/{cntN}  ·  api {lostA}/{cntA}", Dim),
            ("google.com  ·  chatgpt.com", Dim),
        };
        if (vA == CodexNetMon.Lost && vN >= 0) lines.Add(("OpenAI's side :(", Amber));
        else if (vN == CodexNetMon.Lost) lines.Add(("your internet :(", Red));

        using var f2 = new Font("Segoe UI", 11f, GraphicsUnit.Pixel);
        float bw2 = 0;
        foreach (var l in lines) bw2 = Math.Max(bw2, g.MeasureString(l.t, f2).Width);
        bw2 += 16;
        float bh2 = lines.Count * 14 + 10;
        float bx = Math.Min(gx + 8, right - bw2), by = top + gh + 8;
        using (var path = Rounded(new RectangleF(bx, by, bw2, bh2), 7))
        {
            using (var bg = new SolidBrush(Mul(Color.FromArgb(232, 20, 20, 22), a))) g.FillPath(bg, path);
            using (var pen = new Pen(Mul(Track, a), 1f)) g.DrawPath(pen, path);
        }
        for (int i = 0; i < lines.Count; i++)
            using (var b = new SolidBrush(Mul(lines[i].c, a)))
                g.DrawString(lines[i].t, f2, b, bx + 8, by + 5 + i * 14);
    }

    private static RectangleF CancelRect(int w, int h)
    {
        const float d = 34, margin = 22;
        return new RectangleF(w - margin - d, 20, d, d);
    }

    private static RectangleF RefreshRect(int w, int h) => new(w - 26 - 220, h - 26, 220, 20);

    private static string AgeText(TimeSpan d) =>
        d.TotalMinutes < 1 ? "just now"
        : d.TotalHours < 1 ? $"{(int)d.TotalMinutes}m ago"
        : d.TotalDays < 1 ? $"{(int)d.TotalHours}h ago"
        : $"{(int)d.TotalDays}d ago";

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
        => new[]
        {
            (CancelRect(w, h), (Action<PointF>)(_ => { if (CanCancel) _cancel(); })),
            (RefreshRect(w, h), (Action<PointF>)(_ => { _store.ForceRefresh(); CodexLimits.ForceRefresh(); })),
        };

    private static void DrawBar(Graphics g, float x, float y, float w, string label, string value,
        double frac, Color fill, float a, Font labelFont, Font valueFont)
    {
        using (var lb = new SolidBrush(Mul(White, a)))
            g.DrawString(label, labelFont, lb, x, y);
        var sz = g.MeasureString(value, valueFont);
        using (var vb = new SolidBrush(Mul(Dim, a)))
            g.DrawString(value, valueFont, vb, x + w - sz.Width, y + 1);

        float by = y + 24, bh = 6;
        Fill(g, x, by, w, bh, Mul(Track, a));
        double f = Math.Clamp(frac, 0, 1);
        if (f > 0)
            Fill(g, x, by, (float)(w * f), bh, Mul(fill, a));
    }

    private static void Fill(Graphics g, float x, float y, float w, float h, Color c)
    {
        if (w <= 0) return;
        using var path = Rounded(new RectangleF(x, y, w, h), h / 2f);
        using var b = new SolidBrush(c);
        g.FillPath(b, path);
    }

    private static GraphicsPath Rounded(RectangleF r, float radius)
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

    private static Color Mul(Color c, float a)
        => Color.FromArgb((int)Math.Clamp(c.A * a, 0, 255), c.R, c.G, c.B);

    private static double ContextFrac(CodexSnapshot? st) =>
        st is null || st.ContextMax <= 0 ? 0 : Math.Clamp((double)st.ContextUsed / st.ContextMax, 0, 1);

    private static Color StateColor(string? state) => state switch
    {
        "working" => Green,
        "compacting" => Blue,
        "waiting_input" => Amber,
        _ => Color.FromArgb(140, 255, 255, 255),
    };

    private static Color RingColor(CodexSnapshot? st)
        => CodexNetMon.ApiDown || CodexNetMon.NetDown ? Red
         : LimitHit ? Amber
         : st?.State == "waiting_input" ? Amber
         : Compacting(st) ? Blue
         : st?.State == "working" ? (string.IsNullOrEmpty(st.CurrentTool) ? Amber : Green)
         : White;

    private static string Pct(float f) => $"{(int)Math.Round(f * 100)}%";

    private static Color LerpC(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    private static Color UsageColor(float f) =>
        f <= 0.5f ? Blue
        : f <= 0.75f ? LerpC(Blue, Amber, (f - 0.5f) / 0.25f)
        : LerpC(Amber, Red, Math.Clamp((f - 0.75f) / 0.25f, 0f, 1f));

    private static string ResetIn(DateTimeOffset r)
    {
        if (r == default) return "";
        var d = r - DateTimeOffset.UtcNow;
        if (d.TotalSeconds <= 0) return "now";
        if (d.TotalDays >= 1) return $"{(int)d.TotalDays}d {d.Hours}h";
        if (d.TotalHours >= 1) return $"{(int)d.TotalHours}h {d.Minutes}m";
        return $"{d.Minutes}m";
    }

    internal static string DisplayText(string state, string? tool, bool apiDown, bool netDown) =>
        netDown ? "net error :(" : apiDown ? "api error :(" : state switch
        {
            "working" => ToolVerb(tool),
            "compacting" => "compacting…",
            "waiting_input" => "your move ;)",
            _ => "let's work :)",
        };

    private static string Activity(CodexSnapshot? st)
    {
        string verb = OutageText() ?? (LimitHit ? "outta juice :(" : st?.State switch
        {
            "working" => ToolVerb(st.CurrentTool),
            "compacting" when Compacting(st) => "compacting…",
            "waiting_input" => "your move ;)",
            _ => IdleMood(st),
        });
        if (!LimitHit && st?.State != "working" && !Compacting(st)) return verb;
        var el = LimitHit ? LimitReset() : Elapsed(st);
        return el.Length > 0 ? $"{verb}  ·  {el}" : verb;
    }

    private static bool LimitHit => CodexLimits.FiveHour >= 0.99f || CodexLimits.Week >= 0.99f;

    private static string LimitReset()
    {
        var r = ResetIn(CodexLimits.FiveHour >= 0.99f ? CodexLimits.FiveHourReset : CodexLimits.WeekReset);
        return r.Length > 0 ? "back in " + r : "";
    }

    private static string IdleMood(CodexSnapshot? st) =>
        CodexNetMon.NetDown ? "offline :("
        : CodexNetMon.ApiDown ? "api down :("
        : JustCompacted(st) ? "compacted :)"
        : CodexLimits.FiveHour >= 0.95f ? "outta juice XD"
        : "let's work :)";

    private static bool JustCompacted(CodexSnapshot? st) =>
        st?.CompactedAt is { } t && DateTimeOffset.UtcNow - t < TimeSpan.FromSeconds(20);

    private static string? OutageText() =>
        CodexNetMon.NetDown ? "net error :(" : CodexNetMon.ApiDown ? "api error :(" : null;

    private static string ToolVerb(string? tool) => tool switch
    {
        "exec" or "shell" or "local_shell" or "exec_command" or "container" => "running…",
        "apply_patch" or "edit" or "write_file" => "patching…",
        "read_file" or "view" or "cat" => "reading…",
        "grep" or "rg" or "find" or "list_dir" or "ls" => "digging…",
        "web_search" or "search" => "googling :P",
        "browser" or "fetch" or "open_url" => "fetching…",
        "view_image" or "screenshot" => "peeking o.o",
        "update_plan" or "plan" => "plotting…",
        "spawn" or "agent" or "subagent" or "thread_spawn" => "delegating…",
        "request_user_input" or "ask" => "asking you :)",
        null or "" => "hmm…",
        _ => tool!.ToLowerInvariant() + "…",
    };

    private static string Elapsed(CodexSnapshot? st)
    {
        if ((st?.State != "working" && !Compacting(st)) || st?.StartedAt is not { } t) return "";
        var d = DateTimeOffset.UtcNow - t;
        if (d.TotalSeconds < 1) return "";
        return d.TotalMinutes >= 1 ? $"{(int)d.TotalMinutes}m {d.Seconds}s" : $"{d.Seconds}s";
    }
}
