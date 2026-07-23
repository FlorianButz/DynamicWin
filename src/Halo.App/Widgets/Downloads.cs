using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Halo.Interop;

namespace Halo.Widgets;

internal static class Downloads
{
    public static volatile string? Name;
    public static volatile int Percent;
    public static volatile string? ExePath;
    public static volatile string? IconFile;
    public static volatile bool Installing;
    public static volatile bool Waiting;
    public static volatile bool Paused;
    public static volatile bool IsStore;
    public static volatile bool CanControl;
    public static volatile bool NoPct;
    public static long Downloaded, Total;
    public static IntPtr Hwnd;
    public static int Version;

    public static void Reveal()
    {
        var h = Hwnd;
        if (h == IntPtr.Zero) return;
        try
        {
            Win32.ShowWindow(h, Win32.SW_RESTORE);

            uint fore = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), out _);
            uint self = Win32.GetCurrentThreadId();
            bool attached = fore != 0 && fore != self && Win32.AttachThreadInput(fore, self, true);
            Win32.SetForegroundWindow(h);
            if (attached) Win32.AttachThreadInput(fore, self, false);
        }
        catch { }
    }

    public static void StopProcess()
    {
        var h = Hwnd;
        if (h == IntPtr.Zero) return;
        try
        {
            Win32.GetWindowThreadProcessId(h, out uint pid);
            if (pid == 0) return;
            using var p = System.Diagnostics.Process.GetProcessById((int)pid);
            p.Kill(entireProcessTree: true);
        }
        catch { }
    }

    public static void StorePause()  { if (IsStore) StoreInstall.Pause(); }
    public static void StoreResume() { if (IsStore) StoreInstall.Resume(); }
    public static void StoreCancel() { if (IsStore) StoreInstall.Cancel(); }

    private static string _lastLog = "";
    internal static void LogState()
    {
        try
        {
            string s = $"name='{Name}' store={IsStore} canControl={CanControl} hwnd={(Hwnd != IntPtr.Zero)} exe='{ExePath}' pct={Percent} inst={Installing} wait={Waiting}";
            if (s == _lastLog) return;
            _lastLog = s;
            System.IO.File.AppendAllText(System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Halo", "dl-debug.txt"),
                $"{System.DateTime.Now:HH:mm:ss} {s}\r\n");
        }
        catch { }
    }

    private static Timer? _timer;
    private static readonly Regex Pct = new(@"^\s*\[?\s*(\d{1,3})\s*%", RegexOptions.Compiled);
    private static readonly StringBuilder Buf = new(512);
    private static readonly string[] Browsers =
        { "chrome", "msedge", "firefox", "brave", "opera", "vivaldi", "iexplore", "waterfox", "librewolf" };

    public static void Poke() => _timer ??= new Timer(_ => Scan(), null, 500, 1000);

    private static void Scan()
    {
        try
        {
            string? name = null; int pct = 0; IntPtr hwnd = IntPtr.Zero;
            Win32.EnumWindows((h, _) =>
            {
                if (!Win32.IsWindowVisible(h)) return true;
                int len = Win32.GetWindowTextLengthW(h);
                if (len < 3 || len > 400) return true;
                Buf.Clear();
                if (Win32.GetWindowTextW(h, Buf, Buf.Capacity) == 0) return true;
                string t = Buf.ToString();
                var m = Pct.Match(t);
                if (!m.Success) return true;
                int p = int.Parse(m.Groups[1].Value);
                if (p >= 100) return true;
                if (IsBrowser(h)) return true;
                name = Clean(t, m); pct = p; hwnd = h;
                return false;
            }, IntPtr.Zero);

            if (name == null)
            {

                var ph = StoreInstall.Poll(out string app, out int spct, out long done, out long total);
                if (ph != StoreInstall.Phase.None)
                {
                    bool installing = ph == StoreInstall.Phase.Installing;
                    bool waiting = ph == StoreInstall.Phase.Waiting;
                    bool paused = ph == StoreInstall.Phase.Paused;
                    IconFile = null;
                    if (!IsStore) ExePath = StoreAumid;
                    if (!IsStore || Name != app || Percent != spct || Installing != installing
                        || Waiting != waiting || Paused != paused || Downloaded != done || Total != total)
                    {
                        Name = app; Percent = spct; Installing = installing; Waiting = waiting; Paused = paused;
                        Downloaded = done; Total = total; IsStore = true; CanControl = true; NoPct = false;
                        Hwnd = IntPtr.Zero; Interlocked.Increment(ref Version); LogState();
                    }
                    return;
                }

                if (GameInstall.Poll(out string gApp, out long gDone, out long gTotal, out bool gStalled))
                {
                    int gPct = gTotal > 0 ? (int)Math.Clamp(gDone * 100 / gTotal, 0, 99) : 0;
                    bool gNoPct = gTotal <= 0;
                    IconFile = GameInstall.LogoPath;
                    if (!IsStore) ExePath = StoreAumid;
                    if (!IsStore || Name != gApp || Downloaded != gDone || Total != gTotal
                        || Percent != gPct || Paused != gStalled || NoPct != gNoPct)
                    {
                        Name = gApp; Percent = gPct; Installing = false; Waiting = false; Paused = gStalled;
                        Downloaded = gDone; Total = gTotal; IsStore = true; CanControl = false; NoPct = gNoPct;
                        Hwnd = IntPtr.Zero; Interlocked.Increment(ref Version); LogState();
                    }
                    return;
                }
                if (Name != null)
                {
                    Name = null; Percent = 0; ExePath = null; IconFile = null; Installing = false; Waiting = false; Paused = false;
                    IsStore = false; CanControl = false; NoPct = false; Downloaded = Total = 0; Hwnd = IntPtr.Zero;
                    Interlocked.Increment(ref Version);
                }
                return;
            }
            Hwnd = hwnd;
            if (name != Name || pct != Percent || IsStore)
            {
                if (name != Name || IsStore) ExePath = ExeOf(hwnd);
                Name = name; Percent = pct; Installing = false; Waiting = false; Paused = false; IconFile = null;
                IsStore = false; CanControl = false; NoPct = false; Downloaded = Total = 0;
                Interlocked.Increment(ref Version); LogState();
            }
        }
        catch { }
    }

    private const string StoreAumid = "Microsoft.WindowsStore_8wekyb3d8bbwe!App";

    private static bool IsBrowser(IntPtr h)
    {
        try
        {
            Win32.GetWindowThreadProcessId(h, out uint pid);
            if (pid == 0) return false;
            using var p = Process.GetProcessById((int)pid);
            string pn = p.ProcessName.ToLowerInvariant();
            foreach (var b in Browsers) if (pn.Contains(b)) return true;
            return false;
        }
        catch { return false; }
    }

    private static string? ExeOf(IntPtr h)
    {
        try
        {
            Win32.GetWindowThreadProcessId(h, out uint pid);
            using var p = Process.GetProcessById((int)pid);
            return p.MainModule?.FileName;
        }
        catch { return null; }
    }

    private static string Clean(string title, Match m)
    {
        string s = title.Substring(m.Index + m.Length).TrimStart(']', ' ', '-', ':', '\t', '|', '»');
        return s.Length == 0 ? title.Trim() : s;
    }
}
