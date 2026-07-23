using System;
using System.Diagnostics;
using System.Threading;

namespace Halo.ClaudeCode;

internal static class NetMon
{
    public const int Lost = -1, Empty = -2;
    private static readonly int[] _net = CreateBuf(), _api = CreateBuf();
    private static int _idx;
    private static DateTime _until = DateTime.MinValue;
    private static Thread? _thread;

    public static int Version;

    public static volatile bool ApiDown, NetDown, Slow;
    private const int SlowMs = 1500;
    private static int _slowStreak;

    private static int[] CreateBuf()
    {
        var b = new int[24];
        Array.Fill(b, Empty);
        return b;
    }

    static NetMon() => EnsureThread();

    public static void Poke()
    {
        IpCountry.Poke();
        _until = DateTime.UtcNow.AddSeconds(8);
        EnsureThread();
    }

    private static void EnsureThread()
    {
        if (_thread == null)
        {
            _thread = new Thread(Loop) { IsBackground = true };
            _thread.Start();
        }
    }

    public static (int[] net, int[] api) Snapshot()
    {
        lock (_net)
        {
            var n = new int[_net.Length];
            var a = new int[_api.Length];
            for (int i = 0; i < _net.Length; i++)
            {
                n[i] = _net[(_idx + i) % _net.Length];
                a[i] = _api[(_idx + i) % _api.Length];
            }
            return (n, a);
        }
    }

    private static void Loop()
    {
        var lastBg = DateTime.MinValue;
        while (true)
        {

            if (DateTime.UtcNow - lastBg > TimeSpan.FromSeconds(10))
            {
                lastBg = DateTime.UtcNow;
                bool apiDown = HttpLatency(HttpApi, "https://api.anthropic.com/v1/messages", fresh: true) == Lost;
                int netMs = HttpLatency(HttpNet, "https://www.google.com/generate_204", fresh: true);
                bool netDown = apiDown && netMs == Lost;
                SetHealth(apiDown, netDown);
                bool bad = netMs == Lost || netMs > SlowMs;
                _slowStreak = bad ? _slowStreak + 1 : 0;
                SetSlow(_slowStreak >= 2);
            }
            if (DateTime.UtcNow < _until)
            {

                int apiMs = Lost;
                var apiTask = new Thread(() => apiMs = HttpLatency(HttpApi, "https://api.anthropic.com/v1/messages")) { IsBackground = true };
                apiTask.Start();
                int netMs = HttpLatency(HttpNet, "https://www.google.com/generate_204");
                apiTask.Join(2600);

                lock (_net) { _net[_idx] = netMs; _api[_idx] = apiMs; _idx = (_idx + 1) % _net.Length; }
                Interlocked.Increment(ref Version);
                Thread.Sleep(700);
            }
            else Thread.Sleep(300);
        }
    }

    private static void SetHealth(bool apiDown, bool netDown)
    {
        if (apiDown == ApiDown && netDown == NetDown) return;
        ApiDown = apiDown;
        NetDown = netDown;
        IpCountry.Invalidate();
        Interlocked.Increment(ref Version);
    }

    private static void SetSlow(bool slow)
    {
        if (slow == Slow) return;
        Slow = slow;
        Interlocked.Increment(ref Version);
    }

    private static readonly System.Net.Http.HttpClient HttpApi = new(ProxiedHandler())
    { Timeout = TimeSpan.FromSeconds(2.5) };
    private static readonly System.Net.Http.HttpClient HttpNet = new(
        new System.Net.Http.SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5), UseProxy = false })
    { Timeout = TimeSpan.FromSeconds(2.5) };

    private static System.Net.Http.SocketsHttpHandler ProxiedHandler()
    {
        var h = new System.Net.Http.SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };

        var proxy = Environment.GetEnvironmentVariable("HTTPS_PROXY") ?? Environment.GetEnvironmentVariable("HTTP_PROXY")
            ?? Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(proxy))
            try { h.Proxy = new System.Net.WebProxy(proxy); h.UseProxy = true; } catch { h.UseProxy = false; }
        else
            h.UseProxy = false;
        return h;
    }

    private static int HttpLatency(System.Net.Http.HttpClient http, string url, bool fresh = false)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            if (fresh) req.Headers.ConnectionClose = true;
            using var resp = http.Send(req, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            int sc = (int)resp.StatusCode;

            return sc >= 500 || sc == 403 || sc == 407 || sc == 429 ? Lost : (int)sw.ElapsedMilliseconds;
        }
        catch { return Lost; }
    }
}

internal static class IpCountry
{
    public static volatile System.Drawing.Bitmap? Flag;
    private static string? _ip;
    private static Timer? _timer;
    private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public static void Poke() => _timer ??= new Timer(_ => Refresh(), null, 0, 300_000);

    public static void Invalidate() => _timer?.Change(3_000, 300_000);

    private static void Refresh()
    {
        try
        {
            var json = Http.GetStringAsync("https://ipwho.is/?fields=ip,country_code").Result;
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var ip = doc.RootElement.GetProperty("ip").GetString();
            if (ip == _ip) return;
            var cc = doc.RootElement.GetProperty("country_code").GetString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(cc)) return;
            var png = Http.GetByteArrayAsync($"https://flagcdn.com/w80/{cc}.png").Result;
            Flag = new System.Drawing.Bitmap(new System.IO.MemoryStream(png));
            _ip = ip;
            Interlocked.Increment(ref NetMon.Version);
        }
        catch { }
    }
}
