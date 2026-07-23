using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Halo.Hooks;

internal static class Program
{
    private static readonly string ClaudeDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "notch");
    private static readonly string ClaudeStatusPath = Path.Combine(ClaudeDir, "status.json");
    private static readonly string CodexDir = Environment.GetEnvironmentVariable("HALO_CODEX_STATUS_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "notch");

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0) return 0;
            var codex = args.Length >= 2 && args[0] == "codex";
            var cmd = codex ? args[1] : args[0];

            if (cmd == "cancel")
            {
                if (args.Length >= 2 && int.TryParse(args[1], out var pid))
                    Cancel(pid);
                return 0;
            }

            CodexSurface? surface = codex ? DetectCodexSurface() : null;
            var dir = codex ? CodexDir : ClaudeDir;

            uint agentPid = 0;
            var path = codex ? CodexStatusPath(surface!.Value)
                : IsClaudeApp() ? Path.Combine(ClaudeDir, "app.json") : ClaudeSessionPath(out agentPid);
            Directory.CreateDirectory(dir);
            var input = ReadInput();
            var status = LoadOrNew(path);

            if (agentPid != 0) status["pid"] = (int)agentPid;

            if (cmd == "session-end" && !codex && path != ClaudeStatusPath)
            {
                try { File.Delete(path); } catch { }
                try { File.Delete(ClaudeStatusPath); } catch { }
                return 0;
            }

            string? Field(string name) => input?[name]?.GetValue<string>();

            if (codex)
            {
                status["source"] = surface == CodexSurface.Desktop ? "desktop" : "cli";
                if (Field("cwd") is { } cwd) status["cwd"] = cwd;
            }

            switch (cmd)
            {
                case "session-start":
                    if (!codex) SweepDeadSessions();
                    status["sessionId"] = Field("session_id");
                    status["cwd"] = Field("cwd");
                    status["state"] = "idle";
                    if (Field("source") == "compact")
                        status["compactedAt"] = DateTimeOffset.UtcNow.ToString("o");
                    else if (Field("source") is "clear" or "startup")
                        status.Remove("session");
                    RecordProcess(status, codex);
                    break;
                case "pre-compact":
                    status["state"] = "compacting";
                    status["startedAt"] = DateTimeOffset.UtcNow.ToString("o");
                    status["message"] = null;
                    break;
                case "prompt":
                    status["state"] = "working";
                    status["lastPrompt"] = Truncate(Field("prompt"), 120);
                    status["currentTool"] = null;
                    status["startedAt"] = DateTimeOffset.UtcNow.ToString("o");
                    status["message"] = null;
                    RecordProcess(status, codex);
                    UpdateContext(status, Field("transcript_path"));
                    break;
                case "tool":
                    status["state"] = "working";
                    status["currentTool"] = Field("tool_name");
                    break;
                case "tool-done":
                    status["state"] = "working";

                    status["currentTool"] = null;
                    UpdateContext(status, Field("transcript_path"));
                    break;
                case "post-compact":

                    if (!codex && status["state"]?.GetValue<string>() == "compacting"
                        && DateTimeOffset.TryParse(status["startedAt"]?.GetValue<string>(), null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var compactStart))
                    {
                        var ms = (long)(DateTimeOffset.UtcNow - compactStart).TotalMilliseconds;
                        if (ms is > 3000 and < 600_000) status["lastCompactMs"] = ms;
                    }

                    status["state"] = codex || Field("trigger") == "auto" ? "working" : "idle";
                    status["compactedAt"] = DateTimeOffset.UtcNow.ToString("o");
                    if (!codex)
                    {
                        if (Field("trigger") != "auto") status["startedAt"] = null;
                        UpdateContext(status, Field("transcript_path"));
                    }
                    break;
                case "notify":
                    status["state"] = "waiting_input";
                    status["message"] = Truncate(Field("message"), 160);
                    break;
                case "stop":
                    status["state"] = "idle";
                    status["currentTool"] = null;
                    status["startedAt"] = null;
                    status["message"] = null;
                    UpdateContext(status, Field("transcript_path"));
                    break;
                case "session-end":
                    status["state"] = "idle";
                    status["currentTool"] = null;
                    status["startedAt"] = null;
                    break;
                default:
                    return 0;
            }

            status["updatedAt"] = DateTimeOffset.UtcNow.ToString("o");
            Save(status, path);
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static JsonObject? ReadInput()
    {
        try
        {
            var text = Console.In.ReadToEnd();
            if (string.IsNullOrWhiteSpace(text)) return null;
            return JsonNode.Parse(text) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private static string CodexStatusPath(CodexSurface surface) =>
        Path.Combine(CodexDir, surface == CodexSurface.Desktop ? "desktop.json" : "cli.json");

    private static void SweepDeadSessions()
    {
        try
        {
            foreach (var f in Directory.GetFiles(ClaudeDir, "status-*.json"))
            {
                try
                {
                    var pid = (JsonNode.Parse(File.ReadAllText(f)) as JsonObject)?["pid"]?.GetValue<int>() ?? 0;
                    bool alive = false;
                    if (pid > 0)
                        try { using var p = System.Diagnostics.Process.GetProcessById(pid); alive = !p.HasExited; }
                        catch { }
                    if (!alive) File.Delete(f);
                }
                catch { }
            }
        }
        catch { }
    }

    private static string ClaudeSessionPath(out uint pid)
    {
        pid = Ancestor(ProcessMap(), (uint)Environment.ProcessId,
            n => n.Contains("claude") || n == "node.exe");
        return pid == 0 ? ClaudeStatusPath : Path.Combine(ClaudeDir, $"status-{pid}.json");
    }

    private static JsonObject LoadOrNew(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                if (JsonNode.Parse(text) is JsonObject o) return o;
            }
        }
        catch
        {
        }
        return new JsonObject();
    }

    private static void Save(JsonObject status, string path)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, status.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, overwrite: true);
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max] + "…");

    private static void UpdateContext(JsonObject status, string? transcriptPath)
    {
        try
        {
            if (string.IsNullOrEmpty(transcriptPath) || !File.Exists(transcriptPath)) return;
            var lines = File.ReadAllLines(transcriptPath);

            var started = DateTimeOffset.MinValue;
            if (status["startedAt"] is JsonNode sn)
                DateTimeOffset.TryParse(sn.GetValue<string>(), null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out started);

            long latest = 0, turn = 0;
            string? model = null;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                JsonNode? node;
                try { node = JsonNode.Parse(lines[i]); } catch { continue; }
                var usage = node?["message"]?["usage"] ?? node?["usage"];
                if (usage == null) continue;

                long ctx = Get(usage, "input_tokens") + Get(usage, "cache_read_input_tokens")
                    + Get(usage, "cache_creation_input_tokens");
                if (latest == 0 && ctx > 0)
                {
                    latest = ctx;
                    model = (node?["message"]?["model"] ?? node?["model"])?.GetValue<string>();
                }

                if (started == DateTimeOffset.MinValue) { if (latest > 0) break; continue; }
                var tsNode = node?["timestamp"]?.GetValue<string>();
                if (!DateTimeOffset.TryParse(tsNode, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var ts)) continue;
                if (ts < started) { if (latest > 0) break; continue; }
                turn += Get(usage, "input_tokens") + Get(usage, "cache_creation_input_tokens")
                    + Get(usage, "output_tokens");
            }
            if (latest <= 0) return;

            var session = status["session"] as JsonObject ?? new JsonObject();
            session["contextUsed"] = latest;
            session["contextMax"] = ContextWindow(model);
            session["promptTokens"] = turn;
            status["session"] = session;
        }
        catch
        {
        }
    }

    private static long Get(JsonNode usage, string key)
    {
        try { return usage[key]?.GetValue<long>() ?? 0; }
        catch { return 0; }
    }

    private static long ContextWindow(string? model)
    {
        var m = (model ?? "").ToLowerInvariant();
        if (m.Contains("haiku")) return 200_000;
        if (m.Contains("opus") || m.Contains("fable") || m.Contains("sonnet")) return 1_000_000;
        return 200_000;
    }

    private static void RecordProcess(JsonObject status, bool codex = false)
    {
        var map = ProcessMap();
        uint start = (uint)Environment.ProcessId;

        uint agent = Ancestor(map, start, codex
            ? n => n is "codex.exe" or "codex-code-mode-host.exe" or "chatgpt.exe"
            : n => n.Contains("claude") || n == "node.exe");
        if (agent != 0) status["pid"] = (int)agent;

        uint term = Ancestor(map, start, IsTerminal);
        if (term != 0) status["consolePid"] = (int)term;
    }

    private enum CodexSurface { Cli, Desktop }

    private static bool IsClaudeApp()
    {
        var o = Environment.GetEnvironmentVariable("HALO_CLAUDE_SURFACE");
        if (!string.IsNullOrEmpty(o)) return o.Equals("app", StringComparison.OrdinalIgnoreCase);
        return Ancestor(ProcessMap(), (uint)Environment.ProcessId, IsTerminal) == 0;
    }

    private static CodexSurface DetectCodexSurface()
    {
        var overrideSurface = Environment.GetEnvironmentVariable("HALO_CODEX_SURFACE");
        if (string.Equals(overrideSurface, "desktop", StringComparison.OrdinalIgnoreCase))
            return CodexSurface.Desktop;
        if (string.Equals(overrideSurface, "cli", StringComparison.OrdinalIgnoreCase))
            return CodexSurface.Cli;

        var map = ProcessMap();
        uint start = (uint)Environment.ProcessId;
        if (Ancestor(map, start, n => n is "chatgpt.exe" or "codex-code-mode-host.exe") != 0)
            return CodexSurface.Desktop;
        if (Ancestor(map, start, IsTerminal) != 0)
            return CodexSurface.Cli;
        return CodexSurface.Cli;
    }

    private static bool IsTerminal(string name) => name is
        "windowsterminal.exe" or "wt.exe" or "conhost.exe" or "openconsole.exe" or
        "powershell.exe" or "pwsh.exe" or "cmd.exe" or "bash.exe" or "wsl.exe" or
        "alacritty.exe" or "wezterm-gui.exe" or "code.exe";

    private static uint Ancestor(Dictionary<uint, (uint parent, string name)> map, uint start, Func<string, bool> match)
    {
        uint cur = start;
        for (int i = 0; i < 16 && cur != 0 && map.TryGetValue(cur, out var e); i++)
        {
            if (match(e.name.ToLowerInvariant())) return cur == start ? e.parent : cur;
            cur = e.parent;
        }
        return 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32FirstW")]
    private static extern bool Process32First(IntPtr snap, ref PROCESSENTRY32 pe);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32NextW")]
    private static extern bool Process32Next(IntPtr snap, ref PROCESSENTRY32 pe);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr h);

    private static Dictionary<uint, (uint parent, string name)> ProcessMap()
    {
        var map = new Dictionary<uint, (uint, string)>();
        var snap = CreateToolhelp32Snapshot(0x2, 0);
        if (snap == IntPtr.Zero || snap == new IntPtr(-1)) return map;
        try
        {
            var pe = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
            if (Process32First(snap, ref pe))
                do { map[pe.th32ProcessID] = (pe.th32ParentProcessID, pe.szExeFile); }
                while (Process32Next(snap, ref pe));
        }
        finally { CloseHandle(snap); }
        return map;
    }

    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AttachConsole(uint pid);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool FreeConsole();
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteConsoleInput(IntPtr h, INPUT_RECORD[] buffer, uint length, out uint written);

    [StructLayout(LayoutKind.Sequential)]
    private struct KEY_EVENT_RECORD
    {
        public int bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public ushort UnicodeChar;
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT_RECORD
    {
        public ushort EventType;
        public ushort _pad;
        public KEY_EVENT_RECORD Key;
    }

    private static void Cancel(int pid)
    {
        FreeConsole();
        if (!AttachConsole((uint)pid)) return;
        try
        {
            const uint GENERIC_RW = 0x80000000 | 0x40000000, SHARE_RW = 1 | 2, OPEN_EXISTING = 3;
            IntPtr hIn = CreateFile("CONIN$", GENERIC_RW, SHARE_RW, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (hIn == IntPtr.Zero || hIn == new IntPtr(-1)) return;
            var recs = new[]
            {
                new INPUT_RECORD { EventType = 1, Key = new KEY_EVENT_RECORD { bKeyDown = 1, wRepeatCount = 1, wVirtualKeyCode = 0x1B, wVirtualScanCode = 0x01, UnicodeChar = 0x1B } },
                new INPUT_RECORD { EventType = 1, Key = new KEY_EVENT_RECORD { bKeyDown = 0, wRepeatCount = 1, wVirtualKeyCode = 0x1B, wVirtualScanCode = 0x01, UnicodeChar = 0x1B } },
            };
            WriteConsoleInput(hIn, recs, (uint)recs.Length, out _);
            CloseHandle(hIn);
        }
        finally { FreeConsole(); }
    }
}
