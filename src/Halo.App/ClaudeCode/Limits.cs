using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;

namespace Halo.ClaudeCode;

internal static class Limits
{
    public static float FiveHour = -1, Week = -1;
    public static DateTimeOffset FiveHourReset, WeekReset;
    public static bool Failed;
    public static bool ExtraUsageOn;
    public static float CreditsUsed = -1;
    public static float CreditsLimit = -1;
    public static float CreditsBalance = -1;

    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "usage-cache.json");

    private static readonly string CredPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");
    private static string? _lastToken;
    private static FileSystemWatcher? _credWatcher;

    static Limits()
    {
        try
        {
            var n = JsonNode.Parse(File.ReadAllText(CachePath));
            FiveHour = n?["fiveHour"]?.GetValue<float>() ?? -1;
            Week = n?["week"]?.GetValue<float>() ?? -1;
            DateTimeOffset.TryParse(n?["fiveHourReset"]?.GetValue<string>(), out FiveHourReset);
            DateTimeOffset.TryParse(n?["weekReset"]?.GetValue<string>(), out WeekReset);
            ExtraUsageOn = n?["extraOn"]?.GetValue<bool>() ?? false;
            CreditsUsed = n?["creditsUsed"]?.GetValue<float>() ?? -1;
            CreditsLimit = n?["creditsLimit"]?.GetValue<float>() ?? -1;
            CreditsBalance = n?["creditsBalance"]?.GetValue<float>() ?? -1;
            if (DateTimeOffset.TryParse(n?["savedAt"]?.GetValue<string>(), out var sa))
                LastSuccess = sa.UtcDateTime;
        }
        catch { }

        try
        {
            var dir = Path.GetDirectoryName(CredPath)!;
            _credWatcher = new FileSystemWatcher(dir, ".credentials.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
                EnableRaisingEvents = true,
            };
            FileSystemEventHandler h = (_, __) => ForceRefresh();
            _credWatcher.Changed += h;
            _credWatcher.Created += h;
            _credWatcher.Renamed += (_, __) => ForceRefresh();
        }
        catch { }
    }

    private static void SaveCache()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            File.WriteAllText(CachePath, new JsonObject
            {
                ["fiveHour"] = FiveHour,
                ["week"] = Week,
                ["fiveHourReset"] = FiveHourReset.ToString("o"),
                ["weekReset"] = WeekReset.ToString("o"),
                ["extraOn"] = ExtraUsageOn,
                ["creditsUsed"] = CreditsUsed,
                ["creditsLimit"] = CreditsLimit,
                ["creditsBalance"] = CreditsBalance,
                ["savedAt"] = DateTimeOffset.UtcNow.ToString("o"),
            }.ToJsonString());
        }
        catch { }
    }

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static DateTime _last = DateTime.MinValue;
    private static TimeSpan _cooldown = TimeSpan.FromSeconds(30);

    private static readonly Timer Heartbeat =
        new(_ => Fetch(force: false), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    private static int _busy;
    private static readonly List<DateTime> _opens = new();

    public static DateTime LastSuccess = DateTime.MinValue;

    public static void Poke() => Fetch(force: false);

    public static void OnPanelOpen()
    {
        var now = DateTime.UtcNow;
        lock (_opens)
        {
            _opens.Add(now);
            _opens.RemoveAll(t => (now - t).TotalSeconds > 60);
            if (_opens.Count > 2 && now - LastSuccess < TimeSpan.FromMinutes(5)) return;
        }
        Fetch(force: false);
    }

    public static void ForceRefresh() => Fetch(force: true);

    private static void Fetch(bool force)
    {
        var now = DateTime.UtcNow;
        if (now - _last < (force ? TimeSpan.FromSeconds(5) : _cooldown)) return;
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        _last = now;
        ThreadPool.QueueUserWorkItem(_ => { try { Refresh(); } finally { _busy = 0; } });
    }

    private static void Refresh()
    {
        try
        {
            var tok = JsonNode.Parse(File.ReadAllText(CredPath))?["claudeAiOauth"]?["accessToken"]?.GetValue<string>();
            if (string.IsNullOrEmpty(tok)) return;
            if (tok != _lastToken)
            {

                _lastToken = tok;
                FiveHour = Week = -1; FiveHourReset = WeekReset = default; Failed = false;
                ExtraUsageOn = false; CreditsUsed = CreditsLimit = CreditsBalance = -1;
            }

            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/api/oauth/usage");
            req.Headers.TryAddWithoutValidation("authorization", "Bearer " + tok);
            req.Headers.TryAddWithoutValidation("anthropic-beta", "oauth-2025-04-20");
            using var resp = Http.Send(req);
            if ((int)resp.StatusCode == 429)
            {

                Probe(tok);
                _cooldown = TimeSpan.FromMinutes(2);
                return;
            }
            _cooldown = TimeSpan.FromSeconds(30);
            var root = JsonNode.Parse(resp.Content.ReadAsStream());

            (float u, DateTimeOffset r) Bucket(string key)
            {
                var n = root?[key];
                float u = n?["utilization"]?.GetValue<float>() ?? -1;
                DateTimeOffset.TryParse(n?["resets_at"]?.GetValue<string>(), out var r);
                return (u < 0 ? -1 : u / 100f, r);
            }
            var (u5, r5) = Bucket("five_hour");
            if (u5 >= 0) { FiveHour = u5; FiveHourReset = r5; }
            var (u7, r7) = Bucket("seven_day");
            if (u7 >= 0) { Week = u7; WeekReset = r7; }

            if (root?["extra_usage"] is { } eu)
            {
                ExtraUsageOn = eu["is_enabled"]?.GetValue<bool>() ?? false;
                int dec = eu["decimal_places"]?.GetValue<int>() ?? 2;
                float div = MathF.Pow(10, dec);
                float used = eu["used_credits"]?.GetValue<float>() ?? -1;
                CreditsUsed = used < 0 ? -1 : used / div;
                float lim = eu["monthly_limit"]?.GetValue<float>() ?? -1;
                CreditsLimit = lim <= 0 ? -1 : lim / div;
            }

            if (root?["spend"]?["balance"]?["amount_minor"]?.GetValue<float>() is { } bal)
            {
                int exp = root?["spend"]?["balance"]?["exponent"]?.GetValue<int>() ?? 2;
                CreditsBalance = bal / MathF.Pow(10, exp);
            }
            if (u5 >= 0 || u7 >= 0) { LastSuccess = DateTime.UtcNow; SaveCache(); }
            Failed = false;
            AdjustHeartbeat();
        }
        catch { Failed = true; }
    }

    private static void AdjustHeartbeat()
    {
        var p = TimeSpan.FromSeconds(FiveHour >= 0.99f || Week >= 0.99f ? 60 : 300);
        try { Heartbeat.Change(p, p); } catch { }
    }

    private static void Probe(string tok)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.TryAddWithoutValidation("authorization", "Bearer " + tok);
            req.Headers.TryAddWithoutValidation("anthropic-beta", "oauth-2025-04-20");
            req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            req.Content = new System.Net.Http.StringContent(
                "{\"model\":\"claude-haiku-4-5-20251001\",\"max_tokens\":1,\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}",
                System.Text.Encoding.UTF8, "application/json");
            using var resp = Http.Send(req);

            float H(string n) => resp.Headers.TryGetValues(n, out var v)
                && float.TryParse(System.Linq.Enumerable.First(v),
                    System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : -1f;
            DateTimeOffset R(string n) => resp.Headers.TryGetValues(n, out var v)
                && long.TryParse(System.Linq.Enumerable.First(v), out var s)
                ? DateTimeOffset.FromUnixTimeSeconds(s) : default;

            float u5 = H("anthropic-ratelimit-unified-5h-utilization");
            float u7 = H("anthropic-ratelimit-unified-7d-utilization");
            if (u5 >= 0) { FiveHour = Math.Min(1f, u5); FiveHourReset = R("anthropic-ratelimit-unified-5h-reset"); }
            if (u7 >= 0) { Week = Math.Min(1f, u7); WeekReset = R("anthropic-ratelimit-unified-7d-reset"); }
            if (u5 >= 0 || u7 >= 0) { LastSuccess = DateTime.UtcNow; SaveCache(); Failed = false; AdjustHeartbeat(); }
        }
        catch { Failed = true; }
    }
}
