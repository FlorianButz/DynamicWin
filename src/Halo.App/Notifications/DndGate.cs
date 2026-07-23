using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace Halo.Notifications;

internal static class BannerGate
{
    private const string SettingsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    private static readonly object _lock = new();
    private static readonly Dictionary<string, int?> _orig = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string HaloDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo");
    private static readonly string StatePath = Path.Combine(HaloDir, "banner-orig.tsv");
    private static readonly string DebugPath = Path.Combine(HaloDir, "notif-debug.txt");
    private static void Log(string m) { try { File.AppendAllText(DebugPath, $"{DateTime.Now:HH:mm:ss} [banner] {m}\r\n"); } catch { } }

    private static Timer? _applyTimer;
    private static long _lastRestart = -60_000;

    public static void Enable()
    {
        Log("enable (per-app banner suppression)");
        LoadState();
        bool changed = false;
        lock (_lock)
            foreach (var aumid in new List<string>(_orig.Keys))
                changed |= WriteZero(aumid);
        if (changed) ScheduleApply();
    }

    public static void SuppressApp(string aumid)
    {
        if (string.IsNullOrEmpty(aumid)) return;
        bool changed;
        lock (_lock)
        {
            if (!_orig.ContainsKey(aumid))
            {
                try { using var k = Registry.CurrentUser.OpenSubKey(SettingsPath + "\\" + aumid); _orig[aumid] = k?.GetValue("ShowBanner") as int?; }
                catch { _orig[aumid] = null; }
                AppendState(aumid, _orig[aumid]);
            }
            changed = WriteZero(aumid);
        }
        if (changed) ScheduleApply();
    }

    private static readonly string[] SilenceKeys = { "ShowBanner", "Sound", "AllowUrgentNotifications" };
    private static bool WriteZero(string aumid)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(SettingsPath + "\\" + aumid, writable: true);
            if (k == null) return false;
            bool changed = false;
            foreach (var name in SilenceKeys)
                if ((k.GetValue(name) as int?) != 0) { k.SetValue(name, 0, RegistryValueKind.DWord); changed = true; }
            if (changed) Log($"silenced (banner+sound+urgent) → {aumid}");
            return changed;
        }
        catch (Exception ex) { Log($"suppress {aumid} failed: {ex.Message}"); return false; }
    }

    private static void ScheduleApply()
    {
        lock (_lock)
        {
            _applyTimer ??= new Timer(_ => DoApply(), null, Timeout.Infinite, Timeout.Infinite);
            _applyTimer.Change(3_000, Timeout.Infinite);
        }
    }

    private static void DoApply()
    {
        lock (_lock)
        {
            long since = Environment.TickCount64 - _lastRestart;
            if (since < 60_000) { _applyTimer?.Change(60_000 - since, Timeout.Infinite); return; }
            _lastRestart = Environment.TickCount64;
        }
        Log("applying → WpnUserService restart (listener self-heals)");
        RestartService();
    }

    private static void RestartService()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -WindowStyle Hidden -Command \"Restart-Service -Name 'WpnUserService_*' -Force\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch (Exception ex) { Log("restart failed: " + ex.Message); }
    }

    public static void Restore()
    {
        lock (_lock)
        {
            foreach (var (aumid, prior) in _orig)
            {
                try
                {
                    using var k = Registry.CurrentUser.OpenSubKey(SettingsPath + "\\" + aumid, writable: true);
                    if (k == null) continue;
                    if (prior is int p) k.SetValue("ShowBanner", p, RegistryValueKind.DWord);
                    else k.DeleteValue("ShowBanner", throwOnMissingValue: false);

                    k.DeleteValue("Sound", throwOnMissingValue: false);
                    k.DeleteValue("AllowUrgentNotifications", throwOnMissingValue: false);
                }
                catch { }
            }
            Log("restored native banners");
        }
    }

    public static void Uninstall()
    {
        LoadState();
        Restore();
        RestartService();
        try { File.Delete(StatePath); } catch { }
        Log("uninstall: restored + cleared state");
    }

    private static void LoadState()
    {
        try
        {
            if (!File.Exists(StatePath)) return;
            foreach (var line in File.ReadAllLines(StatePath))
            {
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                _orig[line.Substring(0, tab)] = int.TryParse(line.Substring(tab + 1), out var n) ? n : (int?)null;
            }
            Log($"loaded {_orig.Count} learned app(s)");
        }
        catch (Exception ex) { Log("load state failed: " + ex.Message); }
    }

    private static void AppendState(string aumid, int? orig)
    {
        try { Directory.CreateDirectory(HaloDir); File.AppendAllText(StatePath, $"{aumid}\t{orig?.ToString() ?? ""}\r\n"); }
        catch { }
    }
}
