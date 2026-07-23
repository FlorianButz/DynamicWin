using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;

namespace Halo.Widgets;

internal static class GameInstall
{
    private static readonly object _lock = new();
    private static string? _folder, _name, _storeId, _logo;
    private static long _bytes, _total, _lastGrewTick = -600_000, _scanAt;
    private static bool _baselined, _startupDone, _sizeAsked;
    private static int _busy;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

    public static string? LogoPath { get { lock (_lock) return _logo; } }

    public static bool Poll(out string name, out long done, out long total, out bool stalled)
    {
        name = "Xbox game"; done = 0; total = 0; stalled = false;

        string? folder = FindStagingFolder();
        if (folder == null) { lock (_lock) { _folder = null; _name = null; _bytes = _total = 0; } _startupDone = true; return false; }
        if (folder != Volatile.Read(ref _folder))
        {
            lock (_lock) { _folder = folder; _bytes = _total = 0; _baselined = false; _sizeAsked = false; (_name, _storeId, _logo) = ReadConfig(folder); }
            if (_startupDone) Interlocked.Exchange(ref _lastGrewTick, Environment.TickCount64);
        }
        _startupDone = true;
        FetchTotalOnce();

        long now = Environment.TickCount64;
        long sinceGrew = now - Interlocked.Read(ref _lastGrewTick);
        bool active = sinceGrew < 30_000;
        long interval = active ? 20000 : 40000;

        if (now - Interlocked.Read(ref _scanAt) > interval && Interlocked.Exchange(ref _busy, 1) == 0)
        {
            Interlocked.Exchange(ref _scanAt, now);
            ThreadPool.QueueUserWorkItem(_ => { try { Rescan(); } finally { Volatile.Write(ref _busy, 0); } });
        }

        if (sinceGrew > 90_000) return false;
        lock (_lock) { name = _name ?? "Xbox game"; done = _bytes; total = _total; }
        stalled = !active;
        return true;
    }

    private static string? FindStagingFolder()
    {
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.DriveType != DriveType.Fixed) continue;
                string root = Path.Combine(d.Name, "XboxGames");
                if (!Directory.Exists(root)) continue;
                foreach (var dir in Directory.GetDirectories(root))
                    if (Guid.TryParse(Path.GetFileName(dir), out _)) return dir;
            }
        }
        catch { }
        return null;
    }

    private static void Rescan()
    {
        string? folder;
        lock (_lock) folder = _folder;
        if (folder == null) return;
        long size = DirSize(folder);
        lock (_lock)
        {
            if (folder != _folder) return;

            if (_baselined && size > _bytes + 262_144) Interlocked.Exchange(ref _lastGrewTick, Environment.TickCount64);
            _bytes = size; _baselined = true;
        }
    }

    private static long DirSize(string dir)
    {
        long sum = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                try { sum += new FileInfo(f).Length; } catch { }
        }
        catch { }
        return sum;
    }

    private static void FetchTotalOnce()
    {
        string? id;
        lock (_lock) { if (_sizeAsked || _storeId == null) return; _sizeAsked = true; id = _storeId; }
        ThreadPool.QueueUserWorkItem(_ =>
        {
            long t = QueryCatalogSize(id!);
            if (t > 0) lock (_lock) { if (_storeId == id) _total = t; }
        });
    }

    private static long QueryCatalogSize(string storeId)
    {
        try
        {
            string url = $"https://displaycatalog.mp.microsoft.com/v7.0/products/{storeId}?market=US&languages=en-US&fieldsTemplate=Details";
            var root = JsonNode.Parse(Http.GetStringAsync(url).GetAwaiter().GetResult());
            long max = 0;
            if (root?["Product"]?["DisplaySkuAvailabilities"] is JsonArray skus)
                foreach (var s in skus)
                    if (s?["Sku"]?["Properties"]?["Packages"] is JsonArray pkgs)
                        foreach (var p in pkgs)
                            if (p?["MaxDownloadSizeInBytes"]?.GetValue<long>() is { } b && b > max) max = b;
            return max;
        }
        catch { return 0; }
    }

    private static (string? name, string? storeId, string? logo) ReadConfig(string dir)
    {
        string? name = null, storeId = null, logo = null;
        try
        {
            string cfg = Path.Combine(dir, "Content", "MicrosoftGame.config");
            if (File.Exists(cfg))
            {
                string s = File.ReadAllText(cfg);
                var sm = Regex.Match(s, @"<StoreId>([^<]+)</StoreId>");
                if (sm.Success) storeId = sm.Groups[1].Value.Trim();
                var nm = Regex.Match(s, @"DefaultDisplayName=""([^""]+)""");
                if (nm.Success) name = nm.Groups[1].Value.Trim();

                var lm = Regex.Match(s, @"Square150x150Logo=""([^""]+)""");
                if (!lm.Success) lm = Regex.Match(s, @"StoreLogo=""([^""]+)""");
                if (lm.Success)
                {
                    string p = Path.Combine(dir, "Content", lm.Groups[1].Value.Replace('/', '\\'));
                    if (File.Exists(p)) logo = p;
                }
            }
        }
        catch { }
        if (name == null)
            try
            {
                string mf = Path.Combine(dir, "Content", "appxmanifest.xml");
                if (File.Exists(mf))
                {
                    var m = Regex.Match(File.ReadAllText(mf), @"<DisplayName>([^<]+)</DisplayName>");
                    if (m.Success) name = m.Groups[1].Value.Trim();
                }
            }
            catch { }
        return (name, storeId, logo);
    }
}
