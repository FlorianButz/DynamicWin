using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Halo.ClaudeCode;
using Halo.Codex;
using Halo.Interop;
using Halo.Widgets;
using Windows.System;

namespace Halo.Shell;

internal enum NotchVisibilityAction
{
    None,
    Hide,
    ShowAndRender,
}

internal readonly record struct NotchVisibilityDecision(
    NotchVisibilityAction Action,
    bool ReturnEarly,
    bool HiddenForFullscreen);

internal static class NotchVisibility
{

    internal static NotchVisibilityDecision Decide(bool fullscreen, bool hiddenForFullscreen)
    {
        if (fullscreen)
            return new(hiddenForFullscreen ? NotchVisibilityAction.None : NotchVisibilityAction.Hide,
                ReturnEarly: true, HiddenForFullscreen: true);

        return new(hiddenForFullscreen ? NotchVisibilityAction.ShowAndRender : NotchVisibilityAction.None,
            ReturnEarly: false, HiddenForFullscreen: false);
    }
}

internal sealed class AgentNoticeCoordinator
{
    private readonly Dictionary<int, AgentNotice> _previous = new();
    private readonly Dictionary<int, NoticeWindow> _pending = new();
    private long _nextOrder;
    private int _restore = -1;

    internal AgentNoticeCoordinator(int primary) => Primary = primary;

    internal int Primary { get; private set; }

    internal bool IsOpen(DateTimeOffset now) => _pending.Values.Any(window => window.Until >= now);

    internal void SetPrimary(int primary)
    {
        if (_restore < 0)
            Primary = primary;
    }

    internal void Observe(int widgetIndex, AgentNotice notice, DateTimeOffset now,
        bool desktopBacked = false, bool allowSelection = true)
    {
        _previous.TryGetValue(widgetIndex, out var previous);
        _previous[widgetIndex] = notice;

        bool started = notice.State == "working" && previous.State != "working";

        bool compacted = notice.CompactedAt is { } doneAt && doneAt != previous.CompactedAt &&
            now - doneAt < TimeSpan.FromSeconds(30);

        if (compacted)
            _pending[widgetIndex] = new NoticeWindow(now.AddSeconds(4), desktopBacked, _nextOrder++);

        if (started && allowSelection && _pending.Count == 0 && _restore < 0)
            Primary = widgetIndex;

        if (allowSelection)
            Select(now, static _ => true);
    }

    internal void Tick(DateTimeOffset now, Func<int, bool>? isActive = null, bool allowSelection = true)
    {
        foreach (var (index, window) in _pending.ToArray())
            if (window.Until < now)
                _pending.Remove(index);

        if (allowSelection)
            Select(now, isActive ?? (static _ => true));
    }

    private void Select(DateTimeOffset now, Func<int, bool> isActive)
    {
        if (_pending.Count > 0)
        {
            if (_restore < 0)
                _restore = Primary;

            Primary = _pending
                .OrderBy(pair => pair.Key == _restore ? 0 : pair.Value.DesktopBacked ? 1 : 2)
                .ThenBy(pair => pair.Value.Order)
                .First().Key;
            return;
        }

        if (_restore >= 0)
        {
            if (isActive(_restore))
                Primary = _restore;
            _restore = -1;
        }
    }

    private readonly record struct NoticeWindow(DateTimeOffset Until, bool DesktopBacked, long Order);
}

internal sealed class NotchController
{
    private const int CollapsedW = 220, CollapsedH = 40, CollapsedR = 20;
    private const int ExpandedW = 560, ExpandedH = 220, ExpandedR = 30;
    private const int TintDeskCollapsed = 255, TintDeskExpanded = 245;
    private const int TintAppCollapsed = 120, TintAppExpanded = 60;
    private const float OpenSeconds = 0.30f, CloseSeconds = 0.38f;

    private const float HoldSeconds = 1.5f;
    private const int CaptureFast = 2, CaptureSlow = 12;
    private const int EmptyCatchAlpha = 1;

    private readonly LayeredNotch _notch;
    private readonly StatusStore _claudeStore;
    private readonly CodexStatusStore _codexStore;
    private readonly CodexDesktopRuntime _codexDesktopRuntime;
    private readonly IWidget[] _widgets;
    private readonly MediaSessions _mediaSessions;
    private readonly AgentNoticeCoordinator _agentNotices;
    private readonly DispatcherQueueTimer _timer;

    private float S => _notch.Scale;
    private int Sc(int v) => (int)MathF.Round(v * S);
    private int _cl => _notch.WorkLeft + (_notch.WorkWidth - Sc(CollapsedW)) / 2 + (int)_offsetX;
    private int _el => _notch.WorkLeft + (_notch.WorkWidth - Sc(ExpandedW)) / 2 + (int)_offsetX;
    private int _ct => _notch.WorkTop;
    private int _et => _notch.WorkTop;

    private int _primary;
    private int _userPicked = -1;
    private float _progress;
    private float _menu;
    private float _drop = -1f;
    private float _arrive = -1f;
    private int _pending;
    private float _dropCX, _dropCY;
    private bool _dropOut;
    private string _dropIcon = "";
    private Bitmap? _dropImage;
    private readonly bool[] _prevActive;
    private int _row = -1;
    private float _rowOpen;
    private float _stripT;
    private int _widgetVersion = -1;
    private int _lastSec = -1;
    private bool _lastMouseDown;
    private bool _prevDragActive;
    private long _trayShowUntil;

    private string? _trayPressPath;
    private Win32.POINT _trayPressAt;
    private int _trayMode = -1;
    private bool _lastTrayDown;
    private bool _resizing;
    private Win32.POINT _resizeFrom;
    private float _scale0, _handle;
    private bool _hiddenForFullscreen;

    private float _offsetX;
    private bool _moving;
    private float _holdT;
    private DateTime _holdStart = DateTime.MaxValue;
    private Win32.POINT _holdAnchor;
    private int _moveGrabDX;
    private bool _pinned;
    private float _pinHov;
    private float _shrink;
    private bool _empty;

    private readonly Halo.Notifications.NotifSource _notifSrc = new();
    private Halo.Notifications.BtBattery? _bt;
    private readonly Widgets.BtWidget _btWidget = new();
    private System.Threading.Timer? _testTrigger;
    private Halo.Notifications.NotifItem? _notif;
    private float _notifT;
    private bool _notifClosing;
    private bool _notifDetailOn;
    private float _notifDetail;
    private int _notifDetailH = NotifBanner.SummaryH + 60;
    private DateTime _notifDeadline;
    private int _curW = CollapsedW, _curH = CollapsedH;
    private bool _lastDesktop = true;
    private IntPtr _lastFg = IntPtr.Zero;
    private uint _lastLangId;
    private IntPtr _langFg;
    private long _langFgSince;
    private IntPtr _behind = IntPtr.Zero;
    private int _captureTick;
    private int _animTick;
    private int _lastCaptureVer;

    private long _alertAt;
    private bool _battWarned;

    private readonly Dictionary<string, (DateTimeOffset reset, DateTime at)> _limitFired = LoadLimitFired();
    private static readonly string LimitFiredPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "limit-fired.txt");

    private static Dictionary<string, (DateTimeOffset, DateTime)> LoadLimitFired()
    {
        var d = new Dictionary<string, (DateTimeOffset, DateTime)>();
        try
        {
            foreach (var line in System.IO.File.ReadAllLines(LimitFiredPath))
            {
                var p = line.Split('|');
                if (p.Length == 3 && DateTimeOffset.TryParse(p[1], out var r) && DateTime.TryParse(p[2], null,
                        System.Globalization.DateTimeStyles.AdjustToUniversal, out var a))
                    d[p[0]] = (r, a);
            }
        }
        catch { }
        return d;
    }

    private void SaveLimitFired()
    {
        try
        {
            var lines = new List<string>();
            foreach (var (k, v) in _limitFired) lines.Add($"{k}|{v.reset:o}|{v.at:o}");
            System.IO.File.WriteAllLines(LimitFiredPath, lines);
        }
        catch { }
    }
    private bool _netBadShown;
    public NotchController(LayeredNotch notch)
    {
        _notch = notch;
        _notch.ClipboardImage += OnClipboardImage;
        _claudeStore = new StatusStore();
        _codexStore = new CodexStatusStore();
        _codexDesktopRuntime = CodexDesktopRuntime.Shared;
        CodexLimits.Attach(_codexStore);
        CodexLimits.UpdateFrom(_codexStore.Current);
        _mediaSessions = new MediaSessions();
        var widgets = new List<IWidget>();
        for (int s = 0; s < MediaSessions.MaxSlots; s++)
            widgets.Add(new MediaWidget(_mediaSessions, s));
        widgets.Add(new VlcWidget(_mediaSessions));
        widgets.Add(new DownloadWidget());
        widgets.Add(new FileTray());
        widgets.Add(_btWidget);
        Privacy.Poke();
        for (int s = 0; s < StatusStore.MaxSessions; s++)
        {
            int slot = s;
            widgets.Add(new ClaudeCodeWidget(_claudeStore, slot, () => CancelClaude(slot)));
        }
        widgets.Add(new CodexWidget(_codexStore, CodexSurface.Desktop, () => CancelCodex(CodexSurface.Desktop),
            () => _codexDesktopRuntime.Presence.Running));
        widgets.Add(new CodexWidget(_codexStore, CodexSurface.Cli, () => CancelCodex(CodexSurface.Cli)));
        var agentStore = GenericAgentWidget.NewStore();
        for (int s = 0; s < StatusStore.MaxSessions; s++)
            widgets.Add(new GenericAgentWidget(agentStore, s));
        _widgets = [.. widgets];

        var active = ActiveIndices();
        LoadOffset();
        _notch.SetCapturable(_pinned);
        _empty = active.Length == 0;
        _shrink = _empty ? 1f : 0f;
        if (!_empty) _primary = active[0];
        _prevActive = new bool[_widgets.Length];
        for (int i = 0; i < _widgets.Length; i++) _prevActive[i] = _widgets[i].IsActive;
        Apply(0f);
        _agentNotices = new AgentNoticeCoordinator(_primary);

        _bt = new Halo.Notifications.BtBattery((name, pct) => _btWidget.Show(name, pct));
        _testTrigger = new System.Threading.Timer(_ => PollTestNotif(), null, 1000, 1000);

        Dispatcher.Ensure();
        var dq = DispatcherQueue.GetForCurrentThread();
        _timer = dq.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(8);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {

        try { Frame(); } catch (Exception ex) { CrashLog(ex); }
    }

    private static void CrashLog(Exception ex)
    {
        try
        {
            var p = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "frame-errors.txt");
            System.IO.File.WriteAllText(p, $"{DateTime.Now:HH:mm:ss}\n{ex}");
        }
        catch { }
    }

    private long _cpuIdle, _cpuBusyBase, _cpuAt;
    private int _fps = 120;

    private bool _heavy;

    private static readonly int[] CpuTiers = { 50, 70, 85, 95 };
    private static readonly int[] RamTiers = { 70, 85, 95 };
    private int _cpuTierFired = -1, _ramTierFired = -1;
    private int _cpuStreak, _ramStreak;
    internal bool Heavy => _heavy;
    private void AdaptFrameRate()
    {
        long now = Environment.TickCount64;
        if (now - _cpuAt < 1000) return;
        _cpuAt = now;
        if (!Win32.GetSystemTimes(out long idle, out long kern, out long user)) return;
        long total = kern + user;

        bool watching = _progress > 0.02f || _notif != null || _drop >= 0f;
        int target = _fps;
        if (_cpuBusyBase != 0 && total > _cpuBusyBase)
        {
            float busy = 1f - (float)(idle - _cpuIdle) / (total - _cpuBusyBase);

            if (watching) target = 60;
            else if (busy > 0.90f) target = 30;
            else if (busy > 0.55f) target = 60;
            else if (busy < 0.45f) target = 120;

            bool heavy = !watching && (_heavy ? busy > 0.40f : busy > 0.50f);
            if (heavy != _heavy)
            {
                _heavy = heavy;
                try { System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                    heavy ? System.Diagnostics.ProcessPriorityClass.BelowNormal
                          : System.Diagnostics.ProcessPriorityClass.Normal; } catch { }
            }
            int pctNow = (int)(busy * 100);
            int tier = TierOf(CpuTiers, pctNow);
            _cpuStreak = tier > _cpuTierFired ? _cpuStreak + 1 : 0;
            if (_cpuStreak >= 10) { _cpuTierFired = tier; _cpuStreak = 0; QueueCpuNotice(pctNow); }
            CheckRam();
        }
        _cpuIdle = idle; _cpuBusyBase = total;
        if (target != _fps)
        {
            _fps = target;
            _timer.Interval = TimeSpan.FromMilliseconds(target >= 120 ? 8 : target >= 60 ? 16 : 33);
        }
    }

    private static int TierOf(int[] tiers, int pct)
    {
        int t = -1;
        for (int i = 0; i < tiers.Length; i++) if (pct >= tiers[i]) t = i;
        return t;
    }

    private void CheckRam()
    {
        var ms = new Win32.MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Win32.MEMORYSTATUSEX>() };
        if (!Win32.GlobalMemoryStatusEx(ref ms)) return;
        int pct = (int)ms.dwMemoryLoad;
        int tier = TierOf(RamTiers, pct);
        _ramStreak = tier > _ramTierFired ? _ramStreak + 1 : 0;
        if (_ramStreak >= 10) { _ramTierFired = tier; _ramStreak = 0; QueueRamNotice(pct); }
    }

    private void QueueRamNotice(int pct)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            string? top = TopRamProcess();
            _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
            {
                App = "System", Title = $"High memory usage — {pct}%",
                Body = top != null ? $"{top} is using the most." : "Memory is running low.",
                Kind = "cpu", Duration = 7, Icon = CpuBadge(),
            });
        });
    }

    private static string? TopRamProcess()
    {
        try
        {
            var procs = System.Diagnostics.Process.GetProcesses();
            string? best = null; long bestWs = 0; int self = Environment.ProcessId;
            foreach (var p in procs)
            {
                try { if (p.Id != self && p.Id > 4 && p.WorkingSet64 > bestWs) { bestWs = p.WorkingSet64; best = p.ProcessName; } }
                catch { }
            }
            foreach (var p in procs) { try { p.Dispose(); } catch { } }
            return best == null || best.Length == 0 ? null : char.ToUpperInvariant(best[0]) + best[1..];
        }
        catch { return null; }
    }

    private void QueueCpuNotice(int sysPct)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            string? top = TopCpuProcess();
            if (top == null) return;
            _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
            {
                App = "System", Title = $"High CPU usage — {sysPct}%",
                Body = $"{top} is using the most.", Kind = "cpu", Duration = 7, Icon = CpuBadge(),
            });
        });
    }

    private static string? TopCpuProcess()
    {
        try
        {
            var procs = System.Diagnostics.Process.GetProcesses();
            var t0 = new Dictionary<int, TimeSpan>();
            foreach (var p in procs) { try { t0[p.Id] = p.TotalProcessorTime; } catch { } }
            System.Threading.Thread.Sleep(450);
            string? best = null; double bestMs = 0; int self = Environment.ProcessId;
            foreach (var p in procs)
            {
                try
                {
                    if (p.Id == self || p.Id <= 4 || !t0.TryGetValue(p.Id, out var a)) continue;
                    p.Refresh();
                    double ms = (p.TotalProcessorTime - a).TotalMilliseconds;
                    if (ms > bestMs) { bestMs = ms; best = p.ProcessName; }
                }
                catch { }
            }
            foreach (var p in procs) { try { p.Dispose(); } catch { } }
            return best == null || best.Length == 0 ? null : char.ToUpperInvariant(best[0]) + best[1..];
        }
        catch { return null; }
    }

    private void CheckAlerts()
    {
        long now = Environment.TickCount64;
        if (now - _alertAt < 1000) return;
        _alertAt = now;
        if (_pinned) _notch.AssertTopmost();
        CheckBattery();
        CheckLimit("Claude", ClaudeCode.Limits.FiveHour, ClaudeCode.Limits.FiveHourReset, "5-hour");
        CheckLimit("Claude", ClaudeCode.Limits.Week, ClaudeCode.Limits.WeekReset, "weekly");
        CheckLimit("Codex", CodexLimits.FiveHour, CodexLimits.FiveHourReset, "primary");
        CheckLimit("Codex", CodexLimits.Week, CodexLimits.WeekReset, "weekly");
        CheckInternet();
        CheckHourly();
    }

    private int _chimedHour = DateTime.Now.Hour;
    private void CheckHourly()
    {
        var t = DateTime.Now;
        if (t.Minute != 0 || t.Hour == _chimedHour) return;
        _chimedHour = t.Hour;
        _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
        {
            App = "Clock", Title = t.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture),
            Kind = "hourly", Duration = 4, Icon = ClockBadge(),
        });
    }

    private static readonly string TestNotifPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "notif-test.txt");
    private void PollTestNotif()
    {
        try
        {
            if (!System.IO.File.Exists(TestNotifPath)) return;
            var line = System.IO.File.ReadAllText(TestNotifPath).Trim();
            System.IO.File.Delete(TestNotifPath);
            if (line.Length == 0) return;
            var parts = line.Split('|');
            string type = parts[0].Trim().ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";
            string proc = parts.Length > 2 && parts[2].Trim().Length > 0 ? parts[2].Trim() : "";
            switch (type)
            {
                case "cpu": case "sys": case "system":
                    _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
                    {
                        App = "System", Title = $"High CPU usage — {(int.TryParse(arg, out var cp) ? cp : 92)}%",
                        Body = $"{(proc.Length > 0 ? proc : TopCpuProcess() ?? "Chrome")} is using the most.",
                        Kind = "cpu", Duration = 7, Icon = CpuBadge(),
                    });
                    break;
                case "ram": case "mem": case "memory":
                    _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
                    {
                        App = "System", Title = $"High memory usage — {(int.TryParse(arg, out var rp) ? rp : 88)}%",
                        Body = $"{(proc.Length > 0 ? proc : TopRamProcess() ?? "Chrome")} is using the most.",
                        Kind = "cpu", Duration = 7, Icon = CpuBadge(),
                    });
                    break;
                case "clock": case "hour": case "hourly":
                    var t = int.TryParse(arg, out var hr) && hr is >= 0 and <= 23 ? DateTime.Today.AddHours(hr) : DateTime.Now;
                    _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
                    {
                        App = "Clock", Title = t.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture),
                        Kind = "hourly", Duration = 5, Icon = ClockBadge(),
                    });
                    break;
            }
        }
        catch { }
    }

    private void CheckBattery()
    {
        if (!Win32.GetSystemPowerStatus(out var s)) return;
        bool onBattery = s.ACLineStatus == 0;
        int pct = s.BatteryLifePercent;
        if (!onBattery) { _battWarned = false; return; }
        if (pct > 20 || pct > 100) { _battWarned = false; return; }
        if (_battWarned) return;
        _battWarned = true;
        _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
        {
            App = "Battery", Title = $"Battery low — {pct}%", Body = "Tap to turn on Power Saver.",
            Kind = "battery", Duration = 8, OnActivate = EnablePowerSaver, Icon = BatteryBadge(),
        });
    }

    private static void EnablePowerSaver()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powercfg", Arguments = "/setactive a1841308-3541-4fab-bc81-f71556f20b4a",
                UseShellExecute = false, CreateNoWindow = true,
            });
        }
        catch { }
    }

    private void CheckLimit(string app, float util, DateTimeOffset reset, string window)
    {
        if (util < 0.80f) return;
        string key = app + window;
        if (_limitFired.TryGetValue(key, out var f)
            && (DateTime.UtcNow - f.at < TimeSpan.FromHours(6)
                || (reset != default && f.reset != default && (reset - f.reset).Duration() < TimeSpan.FromMinutes(30))))
            return;
        _limitFired[key] = (reset, DateTime.UtcNow);
        SaveLimitFired();
        int p = (int)(util * 100);
        _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
        {
            App = app, Title = $"{app} usage {p}%", Body = $"You've used {p}% of your {window} limit.",
            Kind = $"limit-{app}-{window}", Duration = 8, Icon = LimitBadge(),
        });
    }

    private void CheckInternet()
    {
        if (!ClaudeCode.NetMon.Slow) { _netBadShown = false; return; }
        if (_netBadShown) return;
        _netBadShown = true;
        _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
        {
            App = "Network", Title = "Bad internet :/", Kind = "net", Duration = 6, Icon = NetBadge(),
        });
    }

    private float _dt = 0.008f;
    private long _lastFrameAt;
    private void Frame()
    {
        long frameNow = Environment.TickCount64;
        _dt = _lastFrameAt == 0 ? 0.008f : Math.Clamp((frameNow - _lastFrameAt) / 1000f, 0.001f, 0.05f);
        _lastFrameAt = frameNow;
        AdaptFrameRate();
        CheckAlerts();
        var notifStart = _notif;
        var fg = Win32.GetForegroundWindow();
        DetectCompactCancel(fg);
        DetectLanguageChange(fg);
        bool fullscreen = !_pinned && _notch.IsFullscreen(fg);
        var active = fullscreen ? [] : ActiveIndices();

        bool notifLive = _notif != null || _notifSrc.HasPending;
        var visibility = NotchVisibility.Decide(fullscreen && !notifLive, _hiddenForFullscreen);
        _hiddenForFullscreen = visibility.HiddenForFullscreen;

        if (visibility.Action == NotchVisibilityAction.Hide)
            _notch.SetVisible(false);
        else if (visibility.Action == NotchVisibilityAction.ShowAndRender)
        {
            if (active.Length > 0 && Array.IndexOf(active, _primary) < 0)
            {
                _primary = active[0];
                _agentNotices.SetPrimary(_primary);
            }
            _notch.SetVisible(true);
            _lastFg = IntPtr.Zero;
            Apply(_progress);
        }

        if (visibility.ReturnEarly)
            return;

        bool wasEmpty = _empty;
        _empty = active.Length == 0;

        if (!_empty && _drop < 0f && Array.IndexOf(active, _primary) < 0)
        {
            _primary = active[0];
            _agentNotices.SetPrimary(_primary);
        }

        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < _widgets.Length; i++)
        {
            bool desktopBacked = _widgets[i] is CodexWidget codex && codex.IsDesktop;
            _agentNotices.Observe(i, _widgets[i].AgentNotice, now, desktopBacked, allowSelection: _drop < 0f);
        }
        _agentNotices.Tick(now, i => _widgets[i].IsActive, allowSelection: _drop < 0f);
        if (_drop < 0f)
            _primary = _agentNotices.Primary;
        if (!_empty && Array.IndexOf(active, _primary) < 0)
        {
            _primary = active[0];
            _agentNotices.SetPrimary(_primary);
        }

        if (_userPicked >= 0 && Array.IndexOf(active, _userPicked) < 0) _userPicked = -1;

        if (_drop < 0f && !_empty && _userPicked < 0 && _widgets[_primary].AgentNotice.State != "working")
            foreach (var i in active)
                if (_widgets[i] is ClaudeCodeWidget && _widgets[i].AgentNotice.State == "working")
                {
                    _primary = i;
                    _agentNotices.SetPrimary(i);
                    break;
                }

        if (_drop < 0f && !_empty && active.Length > 1 && _primary != _userPicked
            && _widgets[_primary] is ClaudeCodeWidget)
        {
            Win32.GetWindowThreadProcessId(fg, out uint fpid);
            if (fpid != 0 && FgHostsWidget((int)fpid, _primary))
                foreach (var i in active)
                    if (i != _primary && !FgHostsWidget((int)fpid, i))
                    {
                        _primary = i;
                        _agentNotices.SetPrimary(i);
                        break;
                    }
        }
        bool notice = _drop < 0f && _agentNotices.IsOpen(now);

        if (_drop < 0f && !_empty && _userPicked < 0 && !notice)
            for (int i = 0; i < _widgets.Length; i++)
                if (_widgets[i] is DownloadWidget && _widgets[i].IsActive)
                { _primary = i; _agentNotices.SetPrimary(i); break; }

        if (_drop < 0f && _btWidget.IsActive)
            for (int i = 0; i < _widgets.Length; i++)
                if (_widgets[i] is BtWidget) { _primary = i; _agentNotices.SetPrimary(i); break; }

        if (_prevDragActive && !FileTray.DragActive) _trayShowUntil = Environment.TickCount64 + 2500;
        _prevDragActive = FileTray.DragActive;
        if (_drop < 0f && (FileTray.DragActive || Environment.TickCount64 < _trayShowUntil))
            for (int i = 0; i < _widgets.Length; i++)
                if (_widgets[i] is FileTray)
                { _primary = i; _agentNotices.SetPrimary(i); break; }

        for (int i = 0; i < _widgets.Length; i++)
        {
            bool isAct = _widgets[i].IsActive;
            if (isAct && !_prevActive[i] && !fullscreen && _drop < 0f)
            {
                if (i == _primary) _arrive = 0f;
                else if (_progress < 0.1f)
                {
                    _pending = _primary;
                    _dropOut = true;
                    _dropIcon = _widgets[i].Icon;
                    _dropImage = _widgets[i].IconImage;
                    _dropCX = _dropCY = LayeredNotch.CircleD / 2f;
                    _drop = 0f;
                }
            }
            _prevActive[i] = isAct;
        }

        Win32.GetCursorPos(out var p);

        if (_notif == null && !_notifClosing && _progress <= 0.02f && _drop < 0f
            && _notifSrc.Dequeue() is { } item)
        {
            _notif = item;
            _notifDetailOn = false;
            _notifDetail = 0f;
            _notifDetailH = NotifBanner.DetailHeight(item);
            _notifDeadline = DateTime.UtcNow.AddSeconds(item.Duration);
        }
        float prevNotifT = _notifT, prevNotifDetail = _notifDetail;
        bool overNotif = false;
        if (_notif != null)
        {
            overNotif = InRect(p, NotifLeft(), _ct, Sc(_curW), Sc(_curH));
            if (overNotif && !_notifDetailOn && _notif.Kind != "language")
                _notifDeadline = Max(_notifDeadline, DateTime.UtcNow.AddSeconds(2.5));
            if (!_notifDetailOn && DateTime.UtcNow > _notifDeadline) _notifClosing = true;
            _notifT = Math.Clamp(_notifT + (_notifClosing ? -_dt / 0.30f : _dt / 0.24f), 0f, 1f);
            _notifDetail = Math.Clamp(_notifDetail + (_notifDetailOn ? 1 : -1) * _dt / 0.22f, 0f, 1f);
            if (_notifClosing && _notifT <= 0f)
            {
                _notif = null;
                _notifClosing = false;
                _notifDetailOn = false;
                _notifDetail = 0f;
            }
        }

        bool down = (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
        bool inHandle = _progress > 0.9f
            && p.X >= _el + Sc(ExpandedW - 44) && p.X < _el + Sc(ExpandedW) + 8
            && p.Y >= _et + Sc(ExpandedH - 44) && p.Y < _et + Sc(ExpandedH) + 8;
        bool rescaled = false;
        if (_resizing)
        {
            if (down)
            {
                float ns = Math.Clamp(_scale0
                    + ((p.X - _resizeFrom.X) + (p.Y - _resizeFrom.Y)) / (float)(ExpandedW + ExpandedH),
                    0.7f, 1.6f);
                rescaled = ns != _notch.Scale;
                _notch.Scale = ns;
            }
            else { _resizing = false; _notch.SaveScale(); }
        }
        else if (down && !_lastMouseDown && inHandle && !_moving)
        {
            _resizing = true;
            _resizeFrom = p;
            _scale0 = _notch.Scale;
        }
        float prevHandle = _handle;
        _handle = Math.Clamp(_handle + (inHandle || _resizing ? 1 : -1) * _dt / 0.12f, 0f, 1f);
        _notch.HandleAlpha = _handle;

        bool hovered = _resizing || _moving || (_progress > 0.02f
            ? InRect(p, _el, _et, Sc(ExpandedW), Sc(ExpandedH))
            : InRect(p, _cl, _ct, Sc(CollapsedW), Sc(CollapsedH)));
        float prevOffsetX = _offsetX, prevHoldT = _holdT;
        UpdateMove(p, down, hovered);

        bool open = (hovered || notice || FileTray.DragActive) && !_empty && _notif == null && !_moving;

        int dir = open ? 1 : -1;
        float step = _dt / (open ? OpenSeconds : CloseSeconds);

        float next = open && FileTray.DragActive ? 1f : Math.Clamp(_progress + dir * step, 0f, 1f);

        int alt = AltIndices().Length;
        bool inMenu = _progress < 0.05f && _drop < 0f && InMenu(p);
        float mnext = alt >= 2 && inMenu ? Math.Min(_menu + step, 1f) : Math.Max(_menu - step, 0f);

        var rows = Groups();
        int hoverRow = -1;
        if (inMenu && p.Y >= _ct)
        {
            int r0 = (p.Y - _ct) / Sc(LayeredNotch.CircleD);
            if (r0 >= 0 && r0 < rows.Count) hoverRow = r0;
        }
        if (hoverRow != _row && hoverRow >= 0) { _row = hoverRow; _rowOpen = 0f; }
        float rnext = _row >= 0 && _row < rows.Count && rows[_row].Length >= 2 && inMenu && hoverRow == _row
            ? Math.Min(_rowOpen + step, 1f)
            : Math.Max(_rowOpen - step, 0f);
        if (mnext <= 0f && rnext <= 0f) _row = -1;

        float dnext = _drop;
        if (_drop >= 0f)
        {
            dnext = _drop + _dt / 0.34f;
            if (dnext >= 1f)
            {
                if (!_dropOut) { _primary = _pending; _agentNotices.SetPrimary(_primary); _arrive = 0f; _userPicked = _pending; }
                _dropOut = false;
                dnext = -1f;
            }
        }

        float anext = _arrive;
        if (_arrive >= 0f) { anext = _arrive + _dt / 0.22f; if (anext >= 1f) anext = -1f; }

        float prevMenu = _menu, prevDrop = _drop, prevArrive = _arrive, prevRowOpen = _rowOpen;
        _menu = mnext;
        _rowOpen = rnext;
        _drop = dnext;
        _arrive = anext;
        PollClick(p);
        HandleTrayInteraction(p, down);

        bool startExpand = _progress <= 0.02f && next > 0.02f;
        bool deskChanged = false;
        if (fg != _lastFg || startExpand)
        {

            if (fg != _lastFg && _drop < 0f && !_agentNotices.IsOpen(now))
                FollowForeground(fg);
            if (fg != _lastFg) FollowForegroundMedia(ProcessNameOf(fg));
            _lastFg = fg;
            bool desk = _notch.ProbeBehind(out _behind);
            deskChanged = desk != _lastDesktop;
            _lastDesktop = desk;
            if (deskChanged && !desk) _captureTick = CaptureSlow;
        }

        int captureEvery = _progress > 0.5f ? CaptureFast : CaptureSlow;
        if (_heavy) captureEvery *= 3;
        if (!_lastDesktop && _behind != IntPtr.Zero && ++_captureTick >= captureEvery)
        {
            _captureTick = 0;
            _notch.CaptureFrom(_behind);
        }
        int cv = _notch.CaptureVersion;
        bool refreshed = cv != _lastCaptureVer;
        _lastCaptureVer = cv;

        bool tick = DateTime.Now.Second != _lastSec;
        _lastSec = DateTime.Now.Second;

        bool forceAnim = false;
        if (_widgets[_primary].Animating && _progress < 0.5f && ++_animTick >= 4) { _animTick = 0; forceAnim = true; }

        bool overNow = _notif != null ? overNotif : hovered && next > 0.98f;
        var mouse = _notif != null
            ? new PointF((p.X - NotifLeft()) / S, (p.Y - _ct) / S)
            : new PointF((p.X - _el) / S, (p.Y - _et) / S);
        bool mouseMoved = WidgetInput.Over != overNow || (overNow && WidgetInput.Mouse != mouse);
        WidgetInput.Over = overNow;
        WidgetInput.Mouse = mouse;

        float prevStrip = _stripT;
        _stripT = Math.Clamp(_stripT + (AltIndices().Length >= 1 ? 1 : -1) * _dt / 0.22f, 0f, 1f);

        float prevShrink = _shrink;
        _shrink = Math.Clamp(_shrink + (_empty ? 1 : -1) * _dt / 0.28f, 0f, 1f);

        int wv = WidgetVersion();
        bool changed = next != _progress || wv != _widgetVersion || deskChanged || wasEmpty != _empty
            || refreshed || tick || _menu != prevMenu || _drop != prevDrop || _arrive != prevArrive
            || _rowOpen != prevRowOpen || forceAnim || mouseMoved || rescaled || _handle != prevHandle
            || _shrink != prevShrink || _stripT != prevStrip || _notifT != prevNotifT || _notifDetail != prevNotifDetail
            || _offsetX != prevOffsetX || _holdT != prevHoldT || !ReferenceEquals(_notif, notifStart);
        _progress = next;
        _widgetVersion = wv;
        if (changed) Apply(_progress);
    }

    private bool InMenu(Win32.POINT p)
    {
        var rows = Groups();
        if (rows.Count == 0) return false;
        int D = Sc(LayeredNotch.CircleD);
        int x = _cl + Sc(CollapsedW + LayeredNotch.CircleGap + LayeredNotch.PrivacyPad);
        float openV = EaseOutBack(Math.Clamp(_menu, 0f, 1f));
        float hNow = D + (rows.Count - 1) * D * Math.Max(0f, openV);
        if (p.X >= x && p.X < x + D && p.Y >= _ct && p.Y < _ct + Math.Max(D, hNow))
            return true;
        if (_row >= 0 && _row < rows.Count && _rowOpen > 0f)
        {
            float ext = rows[_row].Length * D * EaseOutBack(Math.Clamp(_rowOpen, 0f, 1f));
            if (p.X >= x + D && p.X < x + D + ext
                && p.Y >= _ct + _row * D && p.Y < _ct + (_row + 1) * D)
                return true;
        }
        return false;
    }

    private Color? GroupRing(int[] gr)
    {
        Color? first = null;
        foreach (var i in gr)
        {
            if (_widgets[i].Ring is not { } rc) continue;
            first ??= rc;
            if (rc.R != rc.G || rc.G != rc.B) return rc;
        }
        return first;
    }

    private List<int[]> Groups()
    {
        var byKind = new Dictionary<string, List<int>>();
        var order = new List<string>();
        foreach (var i in AltIndices())
        {
            string kind = _widgets[i] switch
            {
                MediaWidget => "media",
                VlcWidget => "vlc",
                DownloadWidget => "download",
                FileTray => "filetray",
                ClaudeCodeWidget => "claude",
                CodexWidget => "codex",
                GenericAgentWidget ga => "g:" + ga.GroupKey,
                _ => "other",
            };
            if (!byKind.TryGetValue(kind, out var list)) { list = new List<int>(); byKind[kind] = list; order.Add(kind); }
            list.Add(i);
        }
        return order.ConvertAll(k => byKind[k].ToArray());
    }

    private int[] ActiveIndices()
    {
        var active = new List<int>(_widgets.Length);
        for (int i = 0; i < _widgets.Length; i++)
            if (_widgets[i].IsActive)
                active.Add(i);
        return [.. active];
    }

    private int[] AltIndices()
    {
        var act = ActiveIndices();
        int n = 0;
        foreach (var i in act) if (i != _primary) n++;
        var r = new int[n];
        int j = 0;
        foreach (var i in act) if (i != _primary) r[j++] = i;
        return r;
    }

    private int WidgetVersion()
    {
        int v = Privacy.Version;
        foreach (var wgt in _widgets) v += wgt.Version;
        return v;
    }

    private void PollClick(Win32.POINT p)
    {
        bool down = (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
        if (_moving) { _lastMouseDown = down; return; }
        if (down && !_lastMouseDown && !_resizing && _notif != null)
        {

            var copyR = NotifBanner.CopyRect(_notif, _curW);
            if (!InRect(p, NotifLeft(), _ct, Sc(_curW), Sc(_curH)))
                _notifClosing = true;

            else if (!copyR.IsEmpty
                && p.X >= NotifLeft() + copyR.X * S && p.X < NotifLeft() + copyR.Right * S
                && p.Y >= _ct + copyR.Y * S && p.Y < _ct + copyR.Bottom * S)
            {
                Halo.Interop.Clipboard.SetText(_notif.Code);
                _notif.Copied = true;
                _notifDeadline = Max(_notifDeadline, DateTime.UtcNow.AddSeconds(2));
            }
            else if (!_notifDetailOn && _notif.Body.Length > 0 && p.Y >= _ct + Sc(_curH - 22))
            {
                _notifDetailOn = true;
                _notifDeadline = DateTime.MaxValue;
            }
            else
            {
                _notif.Activate();
                _notifClosing = true;
            }
        }
        else if (down && !_lastMouseDown && !_resizing)
        {
            if (_progress > 0.9f)
            {
                var pr = PinRect(ExpandedW, ExpandedH);
                if (p.X >= _el + pr.X * S && p.X < _el + (pr.X + pr.Width) * S
                    && p.Y >= _et + pr.Y * S && p.Y < _et + (pr.Y + pr.Height) * S)
                    { _pinned = !_pinned; SavePin(); _notch.SetCapturable(_pinned); }
                else

                foreach (var (r, onClick) in _widgets[_primary].Buttons(ExpandedW, ExpandedH))
                {
                    float bx = _el + r.X * S, by = _et + r.Y * S;
                    if (p.X >= bx && p.X < bx + r.Width * S && p.Y >= by && p.Y < by + r.Height * S)
                    {
                        onClick(new PointF((p.X - _el) / S, (p.Y - _et) / S));
                        break;
                    }
                }
            }
            else if (_progress < 0.1f && ActiveIndices().Length >= 2 && _drop < 0f && InMenu(p))
            {
                var rows = Groups();
                int D = Sc(LayeredNotch.CircleD);
                int mx = _cl + Sc(CollapsedW + LayeredNotch.CircleGap + LayeredNotch.PrivacyPad);
                int row = Math.Clamp((p.Y - _ct) / D, 0, rows.Count - 1);
                var grp = rows[row];
                int rel = (p.X - mx) / D;
                int pick = rel <= 0 || grp.Length == 1 ? 0 : Math.Clamp(rel - 1, 0, grp.Length - 1);
                _pending = grp[pick];
                _dropIcon = _widgets[_pending].Icon;
                _dropImage = _widgets[_pending].IconImage;
                int DL = LayeredNotch.CircleD;
                _dropCX = rel <= 0 ? DL / 2f : (rel + 0.5f) * DL;
                _dropCY = (row + 0.5f) * DL;
                _drop = 0f;
                _menu = 0f;
                _rowOpen = 0f;
                _row = -1;
            }
        }
        _lastMouseDown = down;
    }

    private void HandleTrayInteraction(Win32.POINT p, bool down)
    {
        if (!(_progress > 0.9f && _drop < 0f && !_moving && _notif == null && _widgets[_primary] is FileTray tray))
        {
            if (_trayMode == 1) FileTray.CancelReorder();
            _trayPressPath = null; _trayMode = -1; _lastTrayDown = down; return;
        }

        var local = new PointF((p.X - _el) / S, (p.Y - _et) / S);
        bool inside = InRect(p, _el, _et, Sc(ExpandedW), Sc(ExpandedH));
        bool ctrl = (Win32.GetAsyncKeyState(Win32.VK_CONTROL) & 0x8000) != 0;

        if (down && !_lastTrayDown)
        {
            _trayPressPath = tray.RowPathAt(ExpandedW, ExpandedH, local);
            _trayPressAt = p;
            _trayMode = 0;
            if (_trayPressPath != null && ctrl) { FileTray.ToggleSelect(_trayPressPath); _trayPressPath = null; _trayMode = -1; }
        }
        else if (down && _trayMode == 0 && _trayPressPath != null)
        {
            int dx = p.X - _trayPressAt.X, dy = p.Y - _trayPressAt.Y;
            if (!inside) StartTrayDragOut();
            else if (dx * dx + dy * dy > 36)
            {
                _trayMode = 1;
                FileTray.BeginReorder(_trayPressPath);
                FileTray.UpdateReorder(tray.RowIndexAt(ExpandedW, ExpandedH, local));
            }
        }
        else if (down && _trayMode == 1)
        {
            if (!inside) { FileTray.CancelReorder(); StartTrayDragOut(); }
            else FileTray.UpdateReorder(tray.RowIndexAt(ExpandedW, ExpandedH, local));
        }

        if (!down && _lastTrayDown)
        {
            if (_trayMode == 1) FileTray.CommitReorder();
            else if (_trayMode == 0 && _trayPressPath != null) { FileTray.ClearSelection(); FileTray.Open(_trayPressPath); }
            _trayPressPath = null; _trayMode = -1;
        }
        _lastTrayDown = down;
    }

    private void StartTrayDragOut()
    {
        var paths = _trayPressPath != null ? FileTray.SelectionOrRow(_trayPressPath) : Array.Empty<string>();
        _trayMode = 2;
        _trayPressPath = null;

        if (paths.Length > 0 && Halo.Interop.FileDrag.Out(paths) && !CursorOverNotch()) FileTray.RemovePaths(paths);
        _trayPressPath = null; _trayMode = -1;
    }

    private bool CursorOverNotch()
    {
        return Win32.GetCursorPos(out var p) && Win32.GetWindowRect(_notch.Hwnd, out var r)
            && p.X >= r.left && p.X < r.right && p.Y >= r.top && p.Y < r.bottom;
    }

    private static bool InRect(Win32.POINT p, int left, int top, int w, int h)
        => p.X >= left && p.X < left + w && p.Y >= top && p.Y < top + h;

    private void Apply(float t)
    {
        float e = EaseOutBack(t);
        int w = (int)Lerp(CollapsedW, ExpandedW, e);
        int h = (int)Lerp(CollapsedH, ExpandedH, e);
        int r = (int)Lerp(CollapsedR, ExpandedR, e);
        if (_shrink > 0f)
        {
            float s = SmoothStep(_shrink);
            w = (int)Lerp(w, 96, s);
            h = (int)Lerp(h, 12, s);
            r = (int)Lerp(r, 6, s);
        }
        bool glass = !_lastDesktop;
        int cT = glass ? TintAppCollapsed : TintDeskCollapsed;
        int eT = glass ? TintAppExpanded : TintDeskExpanded;
        int tint = (int)Lerp(cT, eT, t);

        if (_empty && !Privacy.Active)
            tint = (int)Lerp(tint, EmptyCatchAlpha, SmoothStep(_shrink));
        float fade = Math.Clamp((t - 0.45f) / 0.55f, 0f, 1f);
        float mini = Math.Clamp(1f - t / 0.35f, 0f, 1f);
        if (_notif != null && _notifT > 0f)
        {
            float en = EaseOutBack(_notifT);
            float nh = Lerp(NotifBanner.SummaryH, _notifDetailH, SmoothStep(_notifDetail));
            w = (int)Lerp(w, NotifBanner.W, en);
            h = (int)Lerp(h, nh, en);
            r = (int)Lerp(r, 26, en);
            tint = (int)Lerp(cT, eT, _notifT);
            fade = Math.Clamp((_notifT - 0.45f) / 0.55f, 0f, 1f);
            mini *= Math.Clamp(1f - _notifT / 0.35f, 0f, 1f);
        }
        float arrive = _arrive < 0f ? 1f : 1f - (1f - _arrive) * (1f - _arrive);
        mini *= arrive;

        var groups = _empty ? new List<int[]>() : Groups();
        var frame = new MenuFrame
        {
            Show = groups.Count >= 1 || _stripT > 0.01f,
            Appear = SmoothStep(_stripT),

            RowIcons = groups.ConvertAll(gr => _widgets[gr[0]].Icon).ToArray(),
            RowImages = groups.ConvertAll(gr => gr.Length >= 2 && _widgets[gr[0]] is ClaudeCodeWidget
                ? ClaudeCodeWidget.PlainIcon : _widgets[gr[0]].IconImage).ToArray(),
            RowCounts = groups.ConvertAll(gr => gr.Length >= 2 ? gr.Length : 0).ToArray(),
            SessIcons = groups.ConvertAll(gr => gr.Length >= 2
                ? Array.ConvertAll(gr, i => _widgets[i].Icon) : Array.Empty<string>()).ToArray(),
            SessImages = groups.ConvertAll(gr => gr.Length >= 2
                ? Array.ConvertAll(gr, i => _widgets[i].IconImage) : Array.Empty<Bitmap?>()).ToArray(),
            RowRings = groups.ConvertAll(GroupRing).ToArray(),
            RowProgress = groups.ConvertAll(gr => _widgets[gr[0]].RingProgress).ToArray(),

            SessRings = groups.ConvertAll(gr => gr.Length >= 2
                ? gr.Select((i, j) => (Color?)(_widgets[i].Ring is { } rc ? Fx.Shade(rc, j) : null)).ToArray()
                : Array.Empty<Color?>()).ToArray(),
            Open = EaseOutBack(Math.Clamp(_menu, 0f, 1f)),
            OpenRow = _row,
            RowOpen = EaseOutBack(Math.Clamp(_rowOpen, 0f, 1f)),
            Dropping = _drop >= 0f,
            DropIcon = _dropIcon,
            DropImage = _dropImage,
            Drop = _drop >= 0f ? _drop : 0f,
        };
        frame.Outward = _dropOut;
        if (frame.Dropping)
        {
            float circleX = w + LayeredNotch.CircleGap + LayeredNotch.PrivacyPad + _dropCX;
            float circleY = LayeredNotch.CircleY + _dropCY;
            float pillX = w - h / 2f, pillY = h / 2f;
            (frame.FromX, frame.FromY, frame.ToX, frame.ToY) = _dropOut
                ? (pillX, pillY, circleX, circleY)
                : (circleX, circleY, pillX, pillY);
        }

        Action<Graphics, int, int, float> content = _notif is { } toast && _notifT > 0f
            ? (g, cw, ch, f) => NotifBanner.Draw(g, cw, ch, f, toast, SmoothStep(_notifDetail), _notifDetailOn)
            : _empty ? static (_, _, _, _) => { } : _widgets[_primary].DrawContent;
        bool pin = _notif == null;
        _curW = w;
        _curH = h;
        _notch.OffsetX = _offsetX;
        float holdCue = _moving ? 0f : _holdT;
        _notch.Render(w, h, r, tint, fade, mini, glass, frame,
            (g, cw, ch, f) => { content(g, cw, ch, f); if (pin) DrawPin(g, cw, ch, f); if (holdCue > 0.01f) DrawHoldCue(g, cw, ch); },
            _empty ? static (_, _, _, _) => { } : _widgets[_primary].DrawCollapsed);
    }

    private void FollowForeground(IntPtr fg)
    {
        try
        {
            Win32.GetWindowThreadProcessId(fg, out uint pid);
            if (pid == 0) return;
            for (int i = 0; i < _widgets.Length; i++)
            {
                if (i == _primary || !_widgets[i].IsActive) continue;
                foreach (var owner in _widgets[i].OwnerPids)
                    if (owner == (int)pid)
                    {
                        _primary = i;
                        _agentNotices.SetPrimary(i);
                        return;
                    }
            }
        }
        catch { }
    }

    private void FollowForegroundMedia(string fgProc)
    {
        if (string.IsNullOrEmpty(fgProc)) return;
        if (_widgets[_primary] is not MediaWidget pm || !pm.IsActive || !AppMatches(pm.App, fgProc)) return;
        for (int i = 0; i < _widgets.Length; i++)
            if (i != _primary && _widgets[i] is MediaWidget m && m.IsActive)
            { _primary = i; _agentNotices.SetPrimary(i); return; }
    }

    private static bool AppMatches(string app, string proc)
    {
        proc = proc.ToLowerInvariant();
        return app.Length > 1 && proc.Length > 1 && (app == proc || app.Contains(proc) || proc.Contains(app));
    }

    private Dictionary<int, int> _parentMap = new();
    private long _parentMapAt;
    private Dictionary<int, int> ParentMap()
    {
        long now = Environment.TickCount64;
        if (_parentMap.Count > 0 && now - _parentMapAt < 2000) return _parentMap;
        var snap = Win32.CreateToolhelp32Snapshot(Win32.TH32CS_SNAPPROCESS, 0);
        if (snap == new IntPtr(-1)) return _parentMap;
        try
        {
            var map = new Dictionary<int, int>(512);
            var pe = new Win32.PROCESSENTRY32W
            { dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Win32.PROCESSENTRY32W>() };
            if (Win32.Process32FirstW(snap, ref pe))
                do { map[(int)pe.th32ProcessID] = (int)pe.th32ParentProcessID; }
                while (Win32.Process32NextW(snap, ref pe));
            if (map.Count > 0) { _parentMap = map; _parentMapAt = now; }
        }
        finally { Win32.CloseHandle(snap); }
        return _parentMap;
    }

    private bool FgHostsWidget(int fgPid, int widget)
    {
        if (fgPid <= 4) return false;
        var map = ParentMap();
        foreach (var owner in _widgets[widget].OwnerPids)
        {
            int p = owner, guard = 0;
            while (p > 4 && guard++ < 32)
            {
                if (p == fgPid) return true;
                if (!map.TryGetValue(p, out p)) break;
            }
        }
        return false;
    }

    private static RectangleF PinRect(int w, int h) => new(9, 4, 24, 24);

    private void DrawPin(Graphics g, int w, int h, float a)
    {
        if (a <= 0.01f) return;
        var r = PinRect(w, h);
        bool hov = WidgetInput.Over && r.Contains(WidgetInput.Mouse);
        _pinHov = Toward(_pinHov, hov ? 1f : 0f, _dt / 0.10f);
        float hv = _pinHov * _pinHov * (3f - 2f * _pinHov);
        DrawPushpin(g, r, _pinned, hv, a);
        if (hv > 0.02f)
        {
            using var f = new Font("Segoe UI", 11f, GraphicsUnit.Pixel);
            using var b = new SolidBrush(Color.FromArgb((int)(200 * hv * a), 235, 235, 235));
            using var sf = new StringFormat { LineAlignment = StringAlignment.Center };
            g.DrawString(_pinned ? "unpin" : "pin on top", f, b,
                new RectangleF(r.Right + 6, r.Y, 120, r.Height), sf);
        }
    }

    internal static void DrawPushpin(Graphics g, RectangleF r, bool pinned, float hover, float a)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var st = g.Save();
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, u = r.Width / 24f * 0.7f;
        g.TranslateTransform(cx, cy);
        g.RotateTransform(28f);
        float hr = 6.4f * u;
        var head = new RectangleF(-hr, -3f * u - hr, hr * 2, hr * 2);
        using var needle = new GraphicsPath();
        needle.AddPolygon(new[] { new PointF(-2.3f * u, 2.5f * u), new PointF(2.3f * u, 2.5f * u), new PointF(0, 12f * u) });
        if (pinned)
        {
            var amber = Color.FromArgb((int)(255 * a), 255, 200, 92);
            using (var nb = new SolidBrush(amber)) g.FillPath(nb, needle);
            using (var hp = new GraphicsPath())
            {
                hp.AddEllipse(head);
                using var pgb = new PathGradientBrush(hp)
                {
                    CenterPoint = new PointF(head.X + hr * 0.62f, head.Y + hr * 0.62f),
                    CenterColor = Color.FromArgb((int)(255 * a), 255, 236, 182),
                    SurroundColors = new[] { amber },
                };
                g.FillPath(pgb, hp);
            }
            using var gloss = new SolidBrush(Color.FromArgb((int)(115 * a), 255, 255, 255));
            g.FillEllipse(gloss, head.X + hr * 0.28f, head.Y + hr * 0.26f, hr * 0.8f, hr * 0.8f);
        }
        else
        {
            int dim = (int)((122 + 78 * hover) * a);
            using var pen = new Pen(Color.FromArgb(dim, 255, 255, 255), 1.7f * u)
            { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawPath(pen, needle);
            g.DrawEllipse(pen, head.X, head.Y, hr * 2, hr * 2);
        }
        g.Restore(st);
    }

    private static float Toward(float v, float t, float step)
        => v < t ? Math.Min(t, v + step) : Math.Max(t, v - step);

    private void DetectCompactCancel(IntPtr fg)
    {
        if ((Win32.GetAsyncKeyState(Win32.VK_ESCAPE) & 0x8000) == 0) return;
        bool claude = _claudeStore.Current?.State == "compacting";
        bool codex = _codexStore.Current?.State == "compacting";
        if (!claude && !codex || !ForegroundIsAgentHost(fg)) return;
        if (claude) ClaudeCodeWidget.MarkCompactCancelled(_claudeStore.Current?.StartedAt);
        if (codex) CodexWidget.MarkCompactCancelled(_codexStore.Current?.StartedAt);
    }

    private static string ProcessNameOf(IntPtr hwnd)
    {
        try
        {
            Win32.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return "";
            using var p = System.Diagnostics.Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch { return ""; }
    }

    private static bool ForegroundIsAgentHost(IntPtr fg)
    {
        try
        {
            Win32.GetWindowThreadProcessId(fg, out uint pid);
            if (pid == 0) return false;
            using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
            var name = proc.ProcessName.ToLowerInvariant();
            return name is "windowsterminal" or "wt" or "conhost" or "openconsole" or "powershell"
                or "pwsh" or "cmd" or "bash" or "wsl" or "alacritty" or "wezterm-gui" or "code"
                or "chatgpt" or "codex" || name.Contains("claude");
        }
        catch
        {
            return false;
        }
    }

    private void CancelClaude(int slot)
    {
        var pid = _claudeStore.SessionLive(slot)?.Pid ?? 0;
        if (pid > 0) CcCancel.Request(pid);
    }

    private void CancelCodex(CodexSurface surface)
    {
        var snapshot = _codexStore.Candidate(surface);
        if (snapshot is { Source: CodexSurface.Cli, State: "working", ConsolePid: > 0 })
            CcCancel.Request(snapshot.ConsolePid);
        else if (snapshot is { Source: CodexSurface.Desktop, State: "working" })
            _codexDesktopRuntime.TryCancel();
    }

    private void OnClipboardImage(Bitmap shot, bool isScreenshot)
    {
        string path = "";
        try
        {
            path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"halo-shot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
            shot.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
        catch { path = ""; }
        _notifSrc.EnqueueLocal(new Halo.Notifications.NotifItem
        {
            App = isScreenshot ? "Screenshot" : "Clipboard",
            Title = isScreenshot ? "Screenshot captured" : "Image copied",
            Preview = shot,
            LaunchPath = path,
        });
    }

    private void DetectLanguageChange(IntPtr fg)
    {
        try
        {
            uint tid = Win32.GetWindowThreadProcessId(fg, out _);
            if (tid == 0) return;
            uint lang = (uint)(Win32.GetKeyboardLayout(tid).ToInt64() & 0xFFFF);
            if (lang == 0) return;
            long now = Environment.TickCount64;
            if (fg != _langFg)
            {
                _langFg = fg; _lastLangId = lang; _langFgSince = now;
                return;
            }

            if (_lastLangId != 0 && lang != _lastLangId && now - _langFgSince > 600) ShowLanguageNotif(lang);
            _lastLangId = lang;
        }
        catch { }
    }

    private void ShowLanguageNotif(uint langId)
    {
        string name = "Keyboard", code = "?";
        try
        {
            var ci = new System.Globalization.CultureInfo((int)langId);
            var lang = ci.Parent.EnglishName.Length > 0 ? ci.Parent.EnglishName : ci.EnglishName;
            if (lang.Length > 0) name = lang;
            code = ci.TwoLetterISOLanguageName.ToUpperInvariant();
        }
        catch { }
        var item = new Halo.Notifications.NotifItem
        {
            App = "Keyboard", Title = name, Icon = LangBadge(code),
            Kind = "language", Duration = 1,
        };

        if (_notif is { Kind: "language" } && !_notifClosing)
        {
            _notif.Icon?.Dispose();
            _notif = item;
            _notifDeadline = DateTime.UtcNow.AddSeconds(1);
            return;
        }
        _notifSrc.DropPending("language");
        _notifSrc.EnqueueLocal(item);
    }

    private static Bitmap LangBadge(string code)
    {
        var b = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        int hue = ((code.Length > 0 ? code[0] : 'A') * 37 + (code.Length > 1 ? code[1] : 0) * 17) % 360;
        using (var lg = new LinearGradientBrush(new RectangleF(3, 3, 58, 58),
                   Fx.HsvToRgb(hue, 0.60f, 0.96f), Fx.HsvToRgb((hue + 20) % 360, 0.72f, 0.78f), 90f))
        using (var p = Fx.Rounded(new RectangleF(3, 3, 58, 58), 17f))
            g.FillPath(lg, p);
        using var f = new Font("Segoe UI Semibold", 25f, GraphicsUnit.Pixel);
        using var wb = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(code, f, wb, new RectangleF(0, 0, 64, 64), sf);
        return b;
    }

    private static readonly FontFamily BadgeGlyphFont = new("Segoe Fluent Icons");
    private static Bitmap LocalBadge(int glyphCp, int hue, float glyphPx = 30f)
    {
        var b = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var lg = new LinearGradientBrush(new RectangleF(3, 3, 58, 58),
                   Fx.HsvToRgb(hue, 0.62f, 0.96f), Fx.HsvToRgb((hue + 24) % 360, 0.74f, 0.78f), 90f))
        using (var p = Fx.Rounded(new RectangleF(3, 3, 58, 58), 17f))
            g.FillPath(lg, p);
        using var path = new GraphicsPath();
        using var sf = new StringFormat(StringFormat.GenericTypographic);
        path.AddString(((char)glyphCp).ToString(), BadgeGlyphFont, (int)FontStyle.Regular, glyphPx, PointF.Empty, sf);
        path.Flatten();
        var gb = path.GetBounds();
        if (gb.Width > 0 && gb.Height > 0)
        {
            using var m = new Matrix();
            m.Translate(MathF.Round(32f - gb.Width / 2f - gb.X), MathF.Round(32f - gb.Height / 2f - gb.Y));
            path.Transform(m);
            using var wb = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
            g.FillPath(wb, path);
        }
        return b;
    }

    private static Bitmap BatteryBadge() => LocalBadge(0xE996, 12);
    private static Bitmap NetBadge()     => LocalBadge(0xEB5E, 5, 34f);
    private static Bitmap BtBadge()      => LocalBadge(0xE702, 215);
    private static Bitmap LimitBadge()   => LocalBadge(0xE9D9, 285);
    private static Bitmap ClockBadge()   => LocalBadge(0xE917, 205);
    private static Bitmap CpuBadge()     => LocalBadge(0xE950, 28);

    internal static Bitmap[] AllLocalBadges() => new[] { BatteryBadge(), NetBadge(), LimitBadge(), ClockBadge(), CpuBadge() };

    private int NotifLeft() => _notch.WorkLeft + (_notch.WorkWidth - Sc(_curW)) / 2 + (int)_offsetX;

    private static readonly string HaloDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo");
    private static readonly string OffsetPath = System.IO.Path.Combine(HaloDir, "offset");
    private static readonly string PinPath = System.IO.Path.Combine(HaloDir, "pinned");

    private void LoadOffset()
    {
        try { if (float.TryParse(System.IO.File.ReadAllText(OffsetPath), System.Globalization.CultureInfo.InvariantCulture, out var v)) _offsetX = v; }
        catch { }
        try { _pinned = System.IO.File.ReadAllText(PinPath).Trim() == "1"; } catch { }
    }

    private void SaveOffset()
    {
        try { System.IO.File.WriteAllText(OffsetPath, _offsetX.ToString(System.Globalization.CultureInfo.InvariantCulture)); } catch { }
    }

    private void SavePin()
    {
        try { System.IO.File.WriteAllText(PinPath, _pinned ? "1" : "0"); } catch { }
    }

    private void UpdateMove(Win32.POINT p, bool down, bool hovered)
    {
        int centre = _notch.WorkLeft + _notch.WorkWidth / 2;
        const float snap = 55f;
        if (_moving)
        {
            if (down)
            {
                float raw = Math.Clamp(p.X - _moveGrabDX - centre,
                    -(_notch.WorkWidth / 2f - Sc(CollapsedW) / 2f - 8), _notch.WorkWidth / 2f - Sc(CollapsedW) / 2f - 8);
                _offsetX = MathF.Abs(raw) < snap ? 0f : raw;
            }
            else { if (MathF.Abs(_offsetX) < snap) _offsetX = 0f; _moving = false; _holdT = 0f; SaveOffset(); }
            return;
        }

        bool holding = down && hovered && !_resizing && _notif == null;
        bool still = Math.Abs(p.X - _holdAnchor.X) <= 8 && Math.Abs(p.Y - _holdAnchor.Y) <= 8;
        if (holding && _holdStart != DateTime.MaxValue && still)
        {
            _holdT = Math.Clamp((float)((DateTime.UtcNow - _holdStart).TotalSeconds / HoldSeconds), 0f, 1f);
            if (_holdT >= 1f) { _moving = true; _moveGrabDX = p.X - (int)(centre + _offsetX); _holdStart = DateTime.MaxValue; }
        }
        else if (holding) { _holdStart = DateTime.UtcNow; _holdAnchor = p; _holdT = 0f; }
        else { _holdStart = DateTime.MaxValue; _holdT = 0f; }
    }

    private void DrawHoldCue(Graphics g, int w, int h)
    {
        float t = SmoothStep(_holdT);
        float bw = (w - 64) * t;
        if (bw < 3f) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF((w - bw) / 2f, h - 6f, bw, 2.5f);
        using var p = Fx.Rounded(rect, 1.25f);
        using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
            new RectangleF(rect.X - 0.5f, rect.Y, rect.Width + 1f, rect.Height),
            Color.White, Color.White, 0f);
        int peak = 25 + (int)(110 * t);
        br.InterpolationColors = new System.Drawing.Drawing2D.ColorBlend(3)
        {
            Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(peak, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) },
            Positions = new[] { 0f, 0.5f, 1f },
        };
        g.FillPath(br, p);
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static float SmoothStep(float t) => t * t * (3f - 2f * t);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.2f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * MathF.Pow(p, 3f) + c1 * MathF.Pow(p, 2f);
    }
}
