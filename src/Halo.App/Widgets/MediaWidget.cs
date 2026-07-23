using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Halo.Widgets;

internal sealed class MediaWidget : IWidget
{
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);
    private static readonly Color Track = Color.FromArgb(46, 255, 255, 255);

    private readonly object _lock = new();
    private readonly MediaSessions _sessions;
    private readonly int _slot;
    private GlobalSystemMediaTransportControlsSession? _session;

    private string? _title, _artist, _trackKey, _appId;
    private bool _playing, _isVideo;
    private double _rate = 1.0;
    private bool _rateEnabled;
    private bool _seekEnabled;
    private bool _thumbWide;

    private GlobalSystemMediaTransportControlsSessionPlaybackStatus _status;
    private byte[]? _thumb;
    private TimeSpan _pos, _end;
    private DateTime _posAt;
    private int _version;

    private string? _artKey;
    private Bitmap? _art;
    private Bitmap[]? _frames;
    private int[]? _delays;
    private int _totalDelay;
    private Color _accent = White;

    public MediaWidget(MediaSessions sessions, int slot)
    {
        _sessions = sessions;
        _slot = slot;
        _sessions.Changed += Resync;
        Resync();
    }

    public string App => _sessions.SlotApp(_slot);

    public string Icon => "\uE768";

    public bool IsActive
    {
        get
        {
            lock (_lock)
            {
                return _title != null
                    && (_status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                     || _status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused);
            }
        }
    }
    public int Version { get { lock (_lock) { return _version; } } }

    public Bitmap? IconImage
    {
        get
        {
            string? id; lock (_lock) { id = _appId; }
            var app = AppIcon.ForAumid(id);
            if (app != null) return app;
            EnsureArt();
            return _art;
        }
    }

    private void EnsureArt()
    {
        byte[]? thumb; string? key;
        lock (_lock) { thumb = _thumb; key = _trackKey; }
        if (key != _artKey)
        {
            DisposeFrames();
            (_frames, _delays) = DecodeFrames(thumb);
            _art = _frames is { Length: > 0 } ? _frames[0] : null;
            _totalDelay = 0;
            if (_delays != null) foreach (var d in _delays) _totalDelay += d;
            _artKey = key;
            _accent = _art != null ? Fx.Accent(_art) : White;
            _palette = Palette(_accent);
        }
    }

    private void DisposeFrames()
    {
        if (_frames != null) foreach (var f in _frames) f?.Dispose();
        _frames = null; _delays = null; _art = null;
    }

    private Bitmap? CurArt()
    {
        if (_frames == null || _frames.Length == 0) return null;
        if (_frames.Length == 1 || _totalDelay <= 0) return _frames[0];
        int t = (int)(Environment.TickCount64 % _totalDelay);
        for (int i = 0; i < _frames.Length; i++) { t -= _delays![i]; if (t < 0) return _frames[i]; }
        return _frames[^1];
    }

    private void Resync() => Hook(_sessions.Session(_slot));

    private void Hook(GlobalSystemMediaTransportControlsSession? s)
    {
        string? newId = s?.SourceAppUserModelId;
        lock (_lock)
        {
            if (s != null && _session != null && newId == _appId) return;
            _session = s; _appId = newId;
        }
        if (s == null) { Clear(); return; }
        try
        {
            s.MediaPropertiesChanged += (_, __) => RefreshProps(s);
            s.PlaybackInfoChanged += (_, __) => RefreshPlayback(s);
            s.TimelinePropertiesChanged += (_, __) => RefreshTimeline(s);
            RefreshProps(s);
            RefreshPlayback(s);
            RefreshTimeline(s);
        }
        catch { }
    }

    private async void RefreshProps(GlobalSystemMediaTransportControlsSession s)
    {
        try
        {
            var props = await s.TryGetMediaPropertiesAsync();
            string title = Fx.CleanText(props.Title);
            string artist = Fx.CleanText(props.Artist);
            string key = title + "" + artist;
            byte[]? thumb = props.Thumbnail != null ? await ReadStream(props.Thumbnail) : null;
            bool wide = ThumbIsWide(thumb);
            bool trackChanged;
            lock (_lock)
            {
                if (!ReferenceEquals(_session, s)) return;
                trackChanged = key != _trackKey;
                _title = title.Length > 0 ? title : (artist.Length > 0 ? artist : null);
                _artist = artist;
                _trackKey = key;
                if (thumb != null || trackChanged) { _thumb = thumb; _thumbWide = wide; }

                if (trackChanged) { _pos = TimeSpan.Zero; _end = TimeSpan.Zero; _posAt = DateTime.UtcNow; _trackEpoch++; }
                _version++;
            }
            if (trackChanged) DebugLog(title);
        }
        catch { }
    }

    private void DebugLog(string title)
    {
        try
        {
            string app = App, id; bool video; lock (_lock) { id = _appId ?? ""; video = _isVideo; }
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Halo", "media-debug.txt"),
                $"{DateTime.Now:HH:mm:ss} app='{app}' aumid='{id}' video={video} title='{title}'\r\n");
        }
        catch { }
    }

    private void RefreshPlayback(GlobalSystemMediaTransportControlsSession s)
    {
        try
        {
            var info = s.GetPlaybackInfo();
            lock (_lock)
            {
                if (!ReferenceEquals(_session, s)) return;
                _status = info.PlaybackStatus;
                _playing = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                _isVideo = info.PlaybackType == Windows.Media.MediaPlaybackType.Video;
                _rateEnabled = info.Controls.IsPlaybackRateEnabled;
                _seekEnabled = info.Controls.IsPlaybackPositionEnabled;
                if (info.PlaybackRate is double pr && pr > 0) _rate = pr;
                _version++;
            }
        }
        catch { }
    }

    private void RefreshTimeline(GlobalSystemMediaTransportControlsSession s)
    {
        try
        {
            var t = s.GetTimelineProperties();
            lock (_lock)
            {
                if (!ReferenceEquals(_session, s)) return;
                _pos = t.Position;
                _end = t.EndTime;
                _posAt = DateTime.UtcNow;
                _version++;
            }
        }
        catch { }
    }

    private void Clear()
    {
        lock (_lock)
        {
            _status = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed;
            if (_title == null) return;
            _title = _artist = _trackKey = null;
            _thumb = null;
            _pos = _end = TimeSpan.Zero;
            _version++;
        }
    }

    private static async Task<byte[]?> ReadStream(IRandomAccessStreamReference r)
    {
        try
        {
            using var s = await r.OpenReadAsync();
            uint size = (uint)s.Size;
            if (size == 0) return null;
            using var reader = new DataReader(s);
            await reader.LoadAsync(size);
            var bytes = new byte[size];
            reader.ReadBytes(bytes);
            return bytes;
        }
        catch { return null; }
    }

    private GlobalSystemMediaTransportControlsSession? Cur() { lock (_lock) { return _session; } }
    private void Toggle() { var s = Cur(); if (s != null) _ = s.TryTogglePlayPauseAsync(); }
    private void Prev() { var s = Cur(); if (s != null) _ = s.TrySkipPreviousAsync(); }
    private void Next() { var s = Cur(); if (s != null) _ = s.TrySkipNextAsync(); }
    private void Stop() { var s = Cur(); if (s != null) _ = s.TryStopAsync(); }

    private void SeekBy(int secs)
    {
        var s = Cur();
        TimeSpan pos, end; bool playing; DateTime at;
        lock (_lock) { pos = _pos; end = _end; playing = _playing; at = _posAt; }
        if (s == null) return;
        var cur = playing ? pos + (DateTime.UtcNow - at) : pos;
        var target = cur + TimeSpan.FromSeconds(secs);
        if (target < TimeSpan.Zero) target = TimeSpan.Zero;
        if (end > TimeSpan.Zero && target > end) target = end;
        try { _ = s.TryChangePlaybackPositionAsync(target.Ticks); } catch { }
    }
    private void SetVol(float f) { _meter.SetVolume(f); Bump(); }
    private void Mute() { _meter.ToggleMute(); Bump(); }
    private void Bump() { lock (_lock) { _version++; } }

    private void Seek(float f)
    {
        var s = Cur();
        TimeSpan end; lock (_lock) { end = _end; }
        if (s == null || end <= TimeSpan.Zero) return;
        try { _ = s.TryChangePlaybackPositionAsync((long)(Math.Clamp(f, 0f, 1f) * end.Ticks)); } catch { }
    }

    private static (RectangleF bar, RectangleF mute) VolLayout(int w) => (new RectangleF(62, 178, 96, 20), new RectangleF(24, 172, 32, 32));
    private static RectangleF SeekRect(int w) { float tx = 180; return new RectangleF(tx, 108, w - tx - 26, 18); }

    private enum Btn { Prev, Play, Next, Back10, Fwd10, Cc, Speed }

    private static readonly double[] Rates = { 1.0, 1.25, 1.5, 1.75, 2.0 };

    private Btn[] Layout()
    {
        var app = App;
        if (!IsVideo()) return new[] { Btn.Prev, Btn.Play, Btn.Next };
        bool rateOk, seekOk; lock (_lock) { rateOk = _rateEnabled; seekOk = _seekEnabled; }
        var l = new List<Btn>();
        if (seekOk) l.Add(Btn.Back10);
        l.Add(Btn.Play);
        if (seekOk) l.Add(Btn.Fwd10);
        if (rateOk) l.Add(Btn.Speed);
        if (SubtitleKey(app) != 0) l.Add(Btn.Cc);
        return l.ToArray();
    }

    private void CycleSpeed()
    {
        var s = Cur();
        if (s == null) return;
        double cur; lock (_lock) { cur = _rate; }
        double next = Rates[0];
        foreach (var r in Rates) if (r > cur + 0.01) { next = r; break; }
        lock (_lock) { _rate = next; }
        try { _ = s.TryChangePlaybackRateAsync(next); } catch { }
        Bump();
    }

    private static string RateText(double r) =>
        (r % 1 == 0 ? ((int)r).ToString() : r.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) + "×";

    private bool IsVideo()
    {
        bool video, wide; string? title, artist; TimeSpan end;
        lock (_lock) { video = _isVideo; wide = _thumbWide; title = _title; artist = _artist; end = _end; }
        return video || wide || IsVideoApp(App) || HasVideoExt(title)
            || (IsBrowser(App) && (string.IsNullOrEmpty(artist) || end <= TimeSpan.Zero));
    }

    private static bool ThumbIsWide(byte[]? thumb)
    {
        if (thumb == null || thumb.Length == 0) return false;
        try
        {
            using var ms = new MemoryStream(thumb);
            using var img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
            return img.Height > 0 && img.Width >= img.Height * 1.4f;
        }
        catch { return false; }
    }

    private static bool IsVideoApp(string app) =>
        app.Contains("vlc") || app.Contains("mpc") || app.Contains("mpv") || app.Contains("potplayer")
        || app.Contains("wmplayer") || app.Contains("kmplayer") || app.Contains("gom")
        || app.Contains("smplayer") || app.Contains("video.ui") || app.Contains("media.player");

    private static readonly string[] VideoExt =
        { ".mkv", ".mp4", ".avi", ".mov", ".webm", ".m4v", ".flv", ".wmv", ".mpg", ".mpeg", ".ts", ".3gp", ".ogv" };
    private static bool HasVideoExt(string? title)
    {
        if (string.IsNullOrEmpty(title)) return false;
        var t = title.ToLowerInvariant();
        foreach (var e in VideoExt) if (t.Contains(e)) return true;
        return false;
    }

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
    {
        var (vbar, mute) = VolLayout(w);
        var seek = SeekRect(w);
        var list = new List<(RectangleF, Action<PointF>)>
        {
            (vbar, pt => SetVol((pt.X - vbar.X) / vbar.Width)),
            (mute, _ => Mute()),
        };
        bool seekOk2; lock (_lock) { seekOk2 = _seekEnabled; }
        if (seekOk2) list.Insert(0, (seek, pt => Seek((pt.X - seek.X) / seek.Width)));
        var layout = Layout();
        var r = BtnRects(w, h, layout.Length);
        for (int i = 0; i < layout.Length; i++)
        {
            Action act = layout[i] switch
            {
                Btn.Prev => Prev,
                Btn.Next => Next,
                Btn.Back10 => () => SeekBy(-10),
                Btn.Fwd10 => () => SeekBy(10),
                Btn.Speed => CycleSpeed,
                Btn.Cc => () => SendHotkey(SubtitleKey(App)),
                _ => Toggle,
            };
            list.Add((r[i], _ => act()));
        }
        return list;
    }

    private static bool IsBrowser(string app) =>
        app.Contains("chrome") || app.Contains("msedge") || app.Contains("edge") || app.Contains("firefox")
        || app.Contains("brave") || app.Contains("opera") || app.Contains("vivaldi");

    private static byte SubtitleKey(string app) =>
        app.Contains("vlc") || app.Contains("mpv") ? (byte)'V' : IsBrowser(app) ? (byte)'C' : (byte)0;

    private void SendHotkey(byte vk)
    {
        if (vk == 0) return;
        string? title; lock (_lock) { title = _title; }
        KeyInject.Send(PlayerWindow(App, title), vk);
    }

    private static IntPtr PlayerWindow(string app, string? mediaTitle)
    {
        if (app.Length == 0) return IntPtr.Zero;
        string hint = (mediaTitle ?? "").Trim();
        if (hint.Length > 24) hint = hint[..24];
        IntPtr first = IntPtr.Zero, matched = IntPtr.Zero;
        var buf = new System.Text.StringBuilder(512);
        Halo.Interop.Win32.EnumWindows((h, _) =>
        {
            if (!Halo.Interop.Win32.IsWindowVisible(h) || Halo.Interop.Win32.GetWindowTextLengthW(h) == 0) return true;
            try
            {
                Halo.Interop.Win32.GetWindowThreadProcessId(h, out uint pid);
                using var p = System.Diagnostics.Process.GetProcessById((int)pid);
                string pn = p.ProcessName.ToLowerInvariant();
                if (pn != app && !pn.Contains(app) && !app.Contains(pn)) return true;
                if (first == IntPtr.Zero) first = h;
                if (hint.Length >= 4)
                {
                    buf.Clear();
                    Halo.Interop.Win32.GetWindowTextW(h, buf, buf.Capacity);
                    if (buf.ToString().Contains(hint, StringComparison.OrdinalIgnoreCase)) { matched = h; return false; }
                }
                else return false;
            }
            catch { }
            return true;
        }, IntPtr.Zero);
        return matched != IntPtr.Zero ? matched : first;
    }

    private static RectangleF[] BtnRects(int w, int h, int n)
    {
        const float artX = 26, artSize = 132, size = 40, gap = 18;
        float colL = artX + artSize + 22, colR = w - 26;
        float cx = (colL + colR) / 2f, total = n * size + (n - 1) * gap, x0 = cx - total / 2f, y = 158;
        var r = new RectangleF[n];
        for (int i = 0; i < n; i++) r[i] = new RectangleF(x0 + i * (size + gap), y, size, size);
        return r;
    }

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        if (fade <= 0.01f) return;
        string? title, artist; bool playing; TimeSpan pos, end; DateTime posAt;
        lock (_lock)
        {
            title = _title; artist = _artist; playing = _playing;
            pos = _pos; end = _end; posAt = _posAt;
        }
        if (title == null) return;

        EnsureArt();

        const float artX = 26, artY = 26, artSize = 132;
        Fx.Glow(g, w, h, fade, artX + artSize / 2f, artY + artSize / 2f, w * 0.85f, h * 1.2f, 38, _accent);
        DrawArt(g, artX, artY, artSize, fade);

        float tx = artX + artSize + 22, tw = w - tx - 26;
        using var titleF = new Font("Segoe UI Semibold", 22f, GraphicsUnit.Pixel);
        using var bodyF = new Font("Segoe UI", 15f, GraphicsUnit.Pixel);
        using var timeF = new Font("Segoe UI", 12f, GraphicsUnit.Pixel);
        using (var tb = new SolidBrush(Mul(White, fade)))
            DrawLine(g, title, titleF, tb, tx, 34, tw);
        if (!string.IsNullOrEmpty(artist))
            using (var ab = new SolidBrush(Mul(Dim, fade)))
                DrawLine(g, artist, bodyF, ab, tx, 66, tw);

        var now = playing ? pos + (DateTime.UtcNow - posAt) : pos;
        float frac = end > TimeSpan.Zero ? (float)Math.Clamp(now / end, 0, 1) : 0f;
        int epoch; lock (_lock) epoch = _trackEpoch;
        if (epoch != _shownEpoch) { _shownEpoch = epoch; _fracShown = frac; }
        _fracShown = _fracShown < 0 ? frac : _fracShown + (frac - _fracShown) * 0.18f;
        if (Math.Abs(frac - _fracShown) < 0.002f) _fracShown = frac;
        float by = 116, bh = 5;
        Fill(g, tx, by, tw, bh, Mul(Track, fade));
        if (_fracShown > 0) Fill(g, tx, by, tw * _fracShown, bh, Mul(White, fade));
        if (end > TimeSpan.Zero)
        {
            using var eb = new SolidBrush(Mul(Dim, fade));
            g.DrawString(Fmt(now), timeF, eb, tx, by + 8);
            var ts = g.MeasureString(Fmt(end), timeF);
            g.DrawString(Fmt(end), timeF, eb, tx + tw - ts.Width, by + 8);
        }

        var (vbar, mute) = VolLayout(w);
        bool muted = _meter.Muted();
        float volNow = muted ? 0f : _meter.Volume();
        _volShown = _volShown < 0 ? volNow : _volShown + (volNow - _volShown) * 0.30f;
        if (Math.Abs(volNow - _volShown) < 0.004f) _volShown = volNow;
        float vol = _volShown;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var volHit = RectangleF.Union(vbar, mute); volHit.Inflate(4f, 6f);
        bool vHov = WidgetInput.Over && volHit.Contains(WidgetInput.Mouse);
        _volHover += ((vHov ? 1f : 0f) - _volHover) * 0.35f;
        float vt = _volHover;
        using (var fb = new SolidBrush(Mul(Color.FromArgb((int)(13 + 16 * vt), 255, 255, 255), fade)))
            g.FillEllipse(fb, mute);
        using (var pen = new Pen(Mul(Color.FromArgb((int)(28 + 26 * vt), 255, 255, 255), fade), 1f))
            g.DrawEllipse(pen, mute);
        DrawGlyphSoft(g, mute, muted ? "\uE74F" : "\uE767", 16f, muted ? fade * 0.55f : fade * (0.8f + 0.2f * vt));
        float vy = vbar.Y + vbar.Height / 2f, bh2 = 4f + 2f * vt;
        Fill(g, vbar.X, vy - bh2 / 2f, vbar.Width, bh2, Mul(Color.FromArgb(34, 255, 255, 255), fade));
        if (vol > 0)
            Fill(g, vbar.X, vy - bh2 / 2f, vbar.Width * vol, bh2,
                Mul(Color.FromArgb((int)(185 + 45 * vt), 255, 255, 255), fade));

        var layout = Layout();
        var rects = BtnRects(w, h, layout.Length);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        for (int i = 0; i < layout.Length; i++)
        {
            var r = rects[i];
            var hit = r; hit.Inflate(4f, 4f);
            bool hov = WidgetInput.Over && hit.Contains(WidgetInput.Mouse);
            _btnHover[i] += ((hov ? 1f : 0f) - _btnHover[i]) * 0.35f;
            if (Math.Abs((hov ? 1f : 0f) - _btnHover[i]) < 0.03f) _btnHover[i] = hov ? 1f : 0f;
            float t = _btnHover[i], sc = 1f + 0.09f * t, d = r.Width * sc;
            var rr = new RectangleF(r.X + (r.Width - d) / 2f, r.Y + (r.Height - d) / 2f, d, d);
            var kind = layout[i];
            bool bare = kind == Btn.Cc;
            if (!bare)
            {
                using (var fb = new SolidBrush(Mul(Color.FromArgb((int)(15 + 19 * t), 255, 255, 255), fade)))
                    g.FillEllipse(fb, rr);
                using (var pen = new Pen(Mul(Color.FromArgb((int)(34 + 30 * t), 255, 255, 255), fade), 1f))
                    g.DrawEllipse(pen, rr);
            }
            float a = fade * (0.8f + 0.2f * t);
            if (kind == Btn.Cc) { Fx.DrawCcMark(g, rr, a); continue; }
            if (kind == Btn.Back10) { Fx.DrawSeekArrow(g, rr, forward: false, a); continue; }
            if (kind == Btn.Fwd10) { Fx.DrawSeekArrow(g, rr, forward: true, a); continue; }
            if (kind == Btn.Speed) { double rate; lock (_lock) { rate = _rate; } DrawRateLabel(g, rr, rate, a); continue; }
            bool isPlay = kind == Btn.Play;
            string glyph = isPlay ? Glyph(playing ? 0xE769 : 0xE768)
                : kind == Btn.Prev ? Glyph(0xE892) : Glyph(0xE893);
            DrawGlyphSoft(g, rr, glyph, (isPlay ? 22f : 17f) * sc, a, isPlay && !playing ? 1.5f : 0f);
        }
    }

    private static string Glyph(int codepoint) => ((char)codepoint).ToString();

    private void DrawRateLabel(Graphics g, RectangleF r, double rate, float fade)
    {
        string t = RateText(rate);
        float px = r.Height * (t.Length >= 5 ? 0.26f : 0.32f);
        using var f = new Font("Segoe UI Semibold", px, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade * 0.92f));
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(t, f, b, r, sf);
    }

    private readonly float[] _btnHover = new float[8];
    private float _volHover;
    private float _volShown = -1f, _fracShown = -1f;
    private int _trackEpoch, _shownEpoch;

    private static readonly FontFamily FluentFamily = new("Segoe Fluent Icons");

    private void DrawGlyphSoft(Graphics g, RectangleF r, string glyph, float px, float fade, float opticalDx = 0f)
    {
        using var path = new GraphicsPath();
        using var sf = new StringFormat(StringFormat.GenericTypographic);
        path.AddString(glyph, FluentFamily, (int)FontStyle.Regular, px, PointF.Empty, sf);
        path.Flatten();
        var b = path.GetBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        using var m = new Matrix();

        m.Translate(MathF.Round(r.X + (r.Width - b.Width) / 2f - b.X + opticalDx),
                    MathF.Round(r.Y + (r.Height - b.Height) / 2f - b.Y));
        path.Transform(m);
        using var br = new SolidBrush(Mul(White, fade * 0.92f));
        g.FillPath(br, path);
    }

    private void DrawArt(Graphics g, float x, float y, float size, float fade, float radius = 14f)
    {
        using var path = Rounded(new RectangleF(x, y, size, size), radius);

        Bitmap? img = CurArt();
        if (img == null) { string? id; lock (_lock) { id = _appId; } img = AppIcon.ForAumid(id); }
        if (img != null)
        {
            CoverFill(g, img, x, y, size, path, fade);
        }
        else
        {
            using var b = new SolidBrush(Mul(Color.FromArgb(40, 255, 255, 255), fade));
            g.FillPath(b, path);
            DrawGlyph(g, new RectangleF(x, y, size, size), "\uE8D6", size * 0.5f, fade * 0.7f);
        }
    }

    private static void CoverFill(Graphics g, Bitmap img, float x, float y, float size, GraphicsPath path, float fade)
    {
        int s = Math.Max(1, (int)Math.Ceiling(size));
        using var scaled = new Bitmap(s, s, PixelFormat.Format32bppPArgb);
        using (var sg = Graphics.FromImage(scaled))
        {
            sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
            sg.SmoothingMode = SmoothingMode.HighQuality;
            using var ia = new ImageAttributes();
            ia.SetWrapMode(WrapMode.TileFlipXY);
            ia.SetColorMatrix(new ColorMatrix { Matrix33 = fade });
            int side = Math.Min(img.Width, img.Height);
            sg.DrawImage(img, new Rectangle(0, 0, s, s),
                (img.Width - side) / 2, (img.Height - side) / 2, side, side, GraphicsUnit.Pixel, ia);
        }
        using var tb = new TextureBrush(scaled) { WrapMode = WrapMode.Clamp };
        tb.TranslateTransform(x, y);
        g.FillPath(tb, path);
    }

    public bool Animating { get { lock (_lock) { return _title != null && _playing; } } }

    public Color? Ring
    {
        get { lock (_lock) return _title != null && _end > TimeSpan.Zero ? _accent : (Color?)null; }
    }
    public float RingProgress
    {
        get
        {
            TimeSpan pos, end; bool playing; DateTime at; string? t;
            lock (_lock) { pos = _pos; end = _end; playing = _playing; at = _posAt; t = _title; }
            if (t == null || end <= TimeSpan.Zero) return -1f;
            var now = playing ? pos + (DateTime.UtcNow - at) : pos;
            return (float)Math.Clamp(now / end, 0, 1);
        }
    }

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        string? title; bool playing;
        lock (_lock) { title = _title; playing = _playing; }
        if (title == null) return;
        EnsureArt();
        float sz = h - 14f, x = 9, y = (h - sz) / 2f;
        Fx.Glow(g, w, h, fade, x + sz / 2f, h / 2f, w * 0.7f, h * 2.2f, 34, _accent);
        DrawArt(g, x, y, sz, fade, sz * 0.28f);
        DrawEqualizer(g, w - 14f, h / 2f, fade, playing);
    }

    private const int EqBars = 9;
    private readonly AudioMeter _meter = new();
    private readonly float[] _eq = new float[EqBars];
    private float _amp;

    private void DrawEqualizer(Graphics g, float rightX, float cy, float fade, bool playing)
    {
        const float barW = 2.6f, gap = 2.6f, maxH = 22f, minH = 2.6f;
        float totalW = EqBars * barW + (EqBars - 1) * gap;
        float x0 = rightX - totalW;

        float[]? bands = playing ? AudioSpectrum.Bands() : null;
        bool live = bands != null && AudioSpectrum.Available;
        float peak = playing ? _meter.Peak() : 0f;
        _amp += (Math.Clamp((float)Math.Sqrt(peak) * 1.4f, 0f, 1f) - _amp) * 0.22f;
        double t = Environment.TickCount / 1000.0;

        for (int i = 0; i < EqBars; i++)
        {
            float target;
            if (live)
            {
                target = minH + (maxH - minH) * bands![i];
            }
            else
            {

                float env = 0.25f + 0.75f * (float)Math.Sin(Math.PI * (i + 0.5) / EqBars);
                float phase = 0.5f + 0.5f * (float)Math.Sin(t * (1.7 + i * 0.4) + i * 1.9);
                target = minH + (maxH - minH) * _amp * env * (0.35f + 0.65f * phase);
            }

            float rise = live ? 0.80f : 0.35f, fall = live ? 0.32f : 0.12f;
            _eq[i] += (target - _eq[i]) * (target > _eq[i] ? rise : fall);
            float bh = Math.Max(minH, _eq[i]);
            Color col = playing ? PaletteAt((float)i / (EqBars - 1)) : Color.FromArgb(120, 255, 255, 255);
            Fill(g, x0 + i * (barW + gap), cy - bh / 2f, barW, bh, Mul(col, fade));
        }
    }

    private Color[] _palette = { White, White, White };

    private static Color[] Palette(Color accent)
    {
        Fx.RgbToHsv(accent, out float h, out float s, out float v);
        return new[] { Fx.HsvToRgb((h - 22f + 360f) % 360f, s, v), accent, Fx.HsvToRgb((h + 22f) % 360f, s, v) };
    }

    private Color PaletteAt(float f)
    {
        f = Math.Clamp(f, 0f, 1f);
        return f <= 0.5f ? LerpColor(_palette[0], _palette[1], f * 2f) : LerpColor(_palette[1], _palette[2], (f - 0.5f) * 2f);
    }

    private static Color LerpColor(Color a, Color b, float t)
        => Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    private void DrawGlyph(Graphics g, RectangleF r, string glyph, float px, float fade)
    {
        using var f = new Font("Segoe Fluent Icons", px, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade));

        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph, f, b, r, sf);
    }

    private static (Bitmap[]? frames, int[]? delays) DecodeFrames(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return (null, null);
        try
        {
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms);
            int n = 1;
            try { n = img.GetFrameCount(FrameDimension.Time); } catch { }
            if (n <= 1) return (new[] { new Bitmap(img) }, new[] { 0 });

            var frames = new Bitmap[n];
            var delays = new int[n];
            byte[]? pd = null;
            try { pd = img.GetPropertyItem(0x5100)?.Value; } catch { }
            for (int i = 0; i < n; i++)
            {
                img.SelectActiveFrame(FrameDimension.Time, i);
                frames[i] = new Bitmap(img);
                int cs = pd != null && pd.Length >= (i + 1) * 4 ? BitConverter.ToInt32(pd, i * 4) : 10;
                delays[i] = Math.Max(20, cs * 10);
            }
            return (frames, delays);
        }
        catch { return (null, null); }
    }

    private static void DrawLine(Graphics g, string text, Font f, Brush b, float x, float y, float w)
    {
        using var sf = new StringFormat(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter };
        if (IsRtl(text)) sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
        g.DrawString(text, f, b, new RectangleF(x, y, w, f.Height + 4), sf);
    }

    private static bool IsRtl(string s)
    {
        foreach (var c in s)
            if (c >= 0x0590 && c <= 0x08FF) return true;
        return false;
    }

    private static string Fmt(TimeSpan t)
        => t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";

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
}
