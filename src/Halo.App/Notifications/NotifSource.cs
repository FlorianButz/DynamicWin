using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Halo.Notifications;

internal sealed class NotifItem
{
    public uint Id;
    public DateTime Time = DateTime.Now;
    public string App = "";
    public string Title = "";
    public string Body = "";
    public string Aumid = "";
    public System.Drawing.Bitmap? Icon;
    public System.Drawing.Bitmap? Preview;
    public string LaunchPath = "";
    public string Kind = "";
    public double Duration = 6;
    public Action? OnActivate;
    public string Code = "";
    public bool Copied;

    public void Activate()
    {

        if (OnActivate != null) { try { OnActivate(); } catch { } return; }

        if (LaunchPath.Length > 0)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = LaunchPath, UseShellExecute = true }); return; }
            catch { }
        }

        var (launch, actType) = WpnDb.LaunchFor(Id);

        if (launch.Length > 0 && (actType.Equals("protocol", StringComparison.OrdinalIgnoreCase) || LooksLikeUri(launch)))
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = launch, UseShellExecute = true }); return; }
            catch { }
        }
        if (Aumid.Length == 0) return;
        try { ((IApplicationActivationManager)new ApplicationActivationManager()).ActivateApplication(Aumid, launch, 0, out _); return; }
        catch { }
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = "explorer.exe", Arguments = "shell:AppsFolder\\" + Aumid, UseShellExecute = true });
        }
        catch { }
    }

    private static bool LooksLikeUri(string s)
    {
        int c = s.IndexOf(':');
        if (c <= 0 || s.IndexOf('|') >= 0 || !char.IsLetter(s[0])) return false;
        for (int i = 1; i < c; i++)
        {
            char ch = s[i];
            if (!char.IsLetterOrDigit(ch) && ch != '+' && ch != '-' && ch != '.') return false;
        }
        return true;
    }
}

[System.Runtime.InteropServices.ComImport,
 System.Runtime.InteropServices.Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
internal class ApplicationActivationManager { }

[System.Runtime.InteropServices.ComImport,
 System.Runtime.InteropServices.Guid("2e941141-7f97-4756-ba1d-9decde894a3d"),
 System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
internal interface IApplicationActivationManager
{
    void ActivateApplication(
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string appUserModelId,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string arguments,
        uint options, out uint processId);
}

internal sealed class NotifSource
{
    private readonly object _lock = new();
    private readonly Queue<NotifItem> _pending = new();
    private UserNotificationListener? _listener;
    private System.Threading.Timer? _poll;
    private uint _seenMaxId;
    private bool _baselined;
    private readonly long _startTick = Environment.TickCount64;
    private long _lastReheal;
    private int _version;

    public NotifSource() => _ = InitAsync();

    public int Version { get { lock (_lock) { return _version; } } }

    public NotifItem? Dequeue()
    {
        lock (_lock) { return _pending.Count > 0 ? _pending.Dequeue() : null; }
    }

    public bool HasPending { get { lock (_lock) { return _pending.Count > 0; } } }

    public void EnqueueLocal(NotifItem item)
    {
        lock (_lock) { _pending.Enqueue(item); _version++; }
    }

    public void DropPending(string kind)
    {
        lock (_lock)
        {
            if (_pending.Count == 0) return;
            var keep = new Queue<NotifItem>();
            foreach (var it in _pending) if (it.Kind != kind) keep.Enqueue(it);
            _pending.Clear();
            foreach (var it in keep) _pending.Enqueue(it);
        }
    }

    private static readonly string DebugPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "notif-debug.txt");

    private static readonly string SeenPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "notif-seen.txt");

    private void LoadSeen()
    {
        try { if (uint.TryParse((System.IO.File.ReadAllText(SeenPath) ?? "").Trim(), out var v) && v > 0) { _seenMaxId = v; _baselined = true; } }
        catch { }
    }

    private static void SaveSeen(uint v) { try { System.IO.File.WriteAllText(SeenPath, v.ToString()); } catch { } }

    private static void Log(string msg)
    {
        try
        {
            System.IO.File.AppendAllText(DebugPath, $"{DateTime.Now:HH:mm:ss} {msg}\r\n");
        }
        catch { }
    }

    private async Task InitAsync()
    {
        try
        {
            _listener = UserNotificationListener.Current;
            var access = await _listener.RequestAccessAsync();
            Log($"access = {access}");
            if (access != UserNotificationListenerAccessStatus.Allowed) return;
            LoadSeen();
            BannerGate.Enable();
            await Refresh();

            try { _listener.NotificationChanged += (s, e) => { _ = Refresh(); }; }
            catch (Exception ex) { Log("event hook failed: " + ex.Message); }

            _poll = new System.Threading.Timer(_ => { _ = Refresh(); }, null, 250, 250);
        }
        catch (Exception ex) { Log("init failed: " + ex); }
    }

    private async Task Refresh()
    {
        if (_listener == null) return;
        try
        {
            var notes = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            lock (_lock)
            {
                uint maxId = _seenMaxId;
                foreach (var n in notes) if (n.Id > maxId) maxId = n.Id;

                if (!_baselined)
                {
                    if (notes.Count == 0 && Environment.TickCount64 - _startTick < 3000) return;
                    _seenMaxId = maxId;
                    _baselined = true;
                    SaveSeen(maxId);
                    Log($"baseline maxId = {maxId}");
                    return;
                }

                foreach (var n in notes)
                {
                    if (n.Id <= _seenMaxId) continue;
                    var item = Build(n);
                    if (item != null)
                    {
                        BannerGate.SuppressApp(item.Aumid);
                        _pending.Enqueue(item);

                        try { _listener.RemoveNotification(n.Id); } catch { }
                    }
                }
                if (maxId > _seenMaxId) { Log($"new toasts up to id {maxId}, queued {_pending.Count}"); _seenMaxId = maxId; SaveSeen(maxId); }
                if (_pending.Count > 0) _version++;
            }
        }
        catch (Exception ex) { Log("refresh failed: " + ex.Message); TryReheal(); }
    }

    private void TryReheal()
    {
        if (Environment.TickCount64 - _lastReheal < 5_000) return;
        _lastReheal = Environment.TickCount64;
        _ = Reheal();
    }

    private async Task Reheal()
    {
        try
        {
            var l = UserNotificationListener.Current;
            if (await l.RequestAccessAsync() != UserNotificationListenerAccessStatus.Allowed) return;
            _listener = l;
            Log("listener re-acquired after service restart");
        }
        catch (Exception ex) { Log("reheal failed: " + ex.Message); }
    }

    private static NotifItem? Build(UserNotification n)
    {
        string app = "", title = "", body = "", aumid = "";
        try { app = n.AppInfo?.DisplayInfo?.DisplayName ?? ""; }
        catch (Exception ex) { Log("appinfo failed: " + ex.Message); }
        try { aumid = n.AppInfo?.AppUserModelId ?? ""; } catch { }
        try
        {
            var bind = n.Notification?.Visual?.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (bind != null)
            {
                var texts = bind.GetTextElements();
                if (texts.Count > 0) title = texts[0].Text ?? "";
                for (int i = 1; i < texts.Count; i++)
                    body += (body.Length > 0 ? "  " : "") + texts[i].Text;
            }
        }
        catch (Exception ex) { Log("texts failed: " + ex.Message); }
        Log($"toast {n.Id}: aumid='{aumid}' app='{app}' t={title.Length} b={body.Length}");
        if (title.Length == 0 && body.Length == 0)
        {

            if (app.Length == 0) return null;
            title = app;
        }

        System.Drawing.Bitmap? icon = ShellIcon.ForAumid(aumid);
        icon ??= Halo.Widgets.AppIcon.ForAumid(aumid);
        if (icon == null) { try { icon = Logo(n); } catch { } }
        icon ??= ShellIcon.ForAppName(app);
        return new NotifItem { Id = n.Id, App = app, Title = title, Body = body, Aumid = aumid, Icon = icon, Code = DetectCode(title, body) };
    }

    private static readonly System.Text.RegularExpressions.Regex CodeRx =
        new(@"(?<![\d])(\d{6}|\d{8})(?![\d])", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static string DetectCode(string title, string body)
    {
        foreach (var text in new[] { title, body })
        {
            if (string.IsNullOrEmpty(text)) continue;
            var m = CodeRx.Match(text);
            if (m.Success) return m.Groups[1].Value;
        }
        return "";
    }
    private static System.Drawing.Bitmap? Logo(UserNotification n)
    {
        try
        {
            var logoRef = n.AppInfo?.DisplayInfo?.GetLogo(new Windows.Foundation.Size(256, 256));
            if (logoRef == null) return null;
            using var ras = logoRef.OpenReadAsync().AsTask().GetAwaiter().GetResult();
            using var s = System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(ras);
            using var tmp = new System.Drawing.Bitmap(s);
            return new System.Drawing.Bitmap(tmp);
        }
        catch { return null; }
    }
}
