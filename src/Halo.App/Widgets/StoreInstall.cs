using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace Halo.Widgets;

internal static class StoreInstall
{
    private static AppInstallManager? _mgr;
    private static AppInstallItem? _item;
    private static readonly object _lock = new();

    internal enum Phase { None, Waiting, Downloading, Installing, Paused }

    private static int _lastPct; private static long _lastDone, _lastTotal;

    private static string? _waitPfn; private static long _waitSinceMs;
    private const long WaitGraceMs = 30_000;

    public static Phase Poll(out string name, out int pct, out long done, out long total)
    {
        name = "Store app"; pct = 0; done = 0; total = 0;
        try
        {
            _mgr ??= new AppInstallManager();
            AppInstallItem? active = null;
            AppInstallStatus? st = null;

            IReadOnlyList<AppInstallItem> list;
            try { list = _mgr.AppInstallItemsWithGroupSupport; }
            catch { list = _mgr.AppInstallItems; }

            int bestRank = -1;
            foreach (var it in list)
            {
                AppInstallStatus s;
                try { s = it.GetCurrentStatus(); } catch { continue; }
                var state = s.InstallState;
                if (state is AppInstallState.Completed or AppInstallState.Canceled or AppInstallState.Error) continue;
                int rank = state switch
                {
                    AppInstallState.Paused or AppInstallState.PausedLowBattery
                        or AppInstallState.PausedWiFiRecommended or AppInstallState.PausedWiFiRequired => 1,
                    AppInstallState.Pending or AppInstallState.ReadyToDownload => -1,
                    _ => 2,
                };
                if (rank < 0) continue;
                if (rank > bestRank) { bestRank = rank; active = it; st = s; }
                if (rank == 2) break;
            }
            if (active == null || st == null) { lock (_lock) _item = null; return Phase.None; }
            lock (_lock) _item = active;

            pct = (int)Math.Clamp(st.PercentComplete, 0, 100);
            done = (long)st.BytesDownloaded;
            total = (long)st.DownloadSizeInBytes;
            name = FriendlyName(active.PackageFamilyName);
            var phase = st.InstallState switch
            {
                AppInstallState.Paused or AppInstallState.PausedLowBattery
                    or AppInstallState.PausedWiFiRecommended or AppInstallState.PausedWiFiRequired => Phase.Paused,
                AppInstallState.Downloading => Phase.Downloading,
                AppInstallState.Pending or AppInstallState.ReadyToDownload => Phase.Waiting,
                _ => Phase.Installing,
            };

            if (phase == Phase.Waiting)
            {
                long nowMs = Environment.TickCount64;
                if (_waitPfn != active.PackageFamilyName) { _waitPfn = active.PackageFamilyName; _waitSinceMs = nowMs; }
                else if (nowMs - _waitSinceMs > WaitGraceMs) { lock (_lock) _item = null; return Phase.None; }
            }
            else _waitPfn = null;

            if (phase == Phase.Paused && pct == 0 && _lastPct > 0) { pct = _lastPct; done = _lastDone; total = _lastTotal; }
            else if (phase != Phase.Waiting) { _lastPct = pct; _lastDone = done; _lastTotal = total; }
            return phase;
        }
        catch { lock (_lock) _item = null; return Phase.None; }
    }

    public static void Pause()  { try { lock (_lock) _item?.Pause(); } catch { } }
    public static void Resume() { try { lock (_lock) _item?.Restart(); } catch { } }
    public static void Cancel() { try { lock (_lock) _item?.Cancel(); } catch { } }

    private static string FriendlyName(string pfn)
    {
        if (string.IsNullOrEmpty(pfn)) return "Store app";
        int us = pfn.IndexOf('_');
        string s = us > 0 ? pfn[..us] : pfn;
        int dot = s.LastIndexOf('.');
        return dot >= 0 && dot < s.Length - 1 ? s[(dot + 1)..] : s;
    }
}
