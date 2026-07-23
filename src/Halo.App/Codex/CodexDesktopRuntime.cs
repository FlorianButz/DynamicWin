using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Halo.Codex;

internal readonly record struct CodexDesktopPresence(bool Running, DateTimeOffset StartedAt);

internal sealed record CodexDesktopWindow(
    string ProcessName, string ExecutablePath, IntPtr Handle, DateTimeOffset StartedAt);

internal sealed class CodexDesktopRuntime
{
    private static readonly TimeSpan ProbeLifetime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CancelInterval = TimeSpan.FromSeconds(1);

    private readonly Func<IReadOnlyList<CodexDesktopWindow>> _scan;
    private readonly Func<IntPtr, uint, IntPtr, IntPtr, bool> _post;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _gate = new();
    private CodexDesktopWindow? _cachedWindow;
    private DateTimeOffset _probeExpiresAt = DateTimeOffset.MinValue;
    private DateTimeOffset _nextCancelAt = DateTimeOffset.MinValue;

    internal static CodexDesktopRuntime Shared { get; } = new();

    internal CodexDesktopRuntime()
        : this(Scan, CodexDesktopCancel.Post, static () => DateTimeOffset.UtcNow)
    {
    }

    internal CodexDesktopRuntime(
        Func<IReadOnlyList<CodexDesktopWindow>> scan,
        Func<IntPtr, uint, IntPtr, IntPtr, bool> post,
        Func<DateTimeOffset> clock)
    {
        _scan = scan;
        _post = post;
        _clock = clock;
    }

    internal CodexDesktopPresence Presence
    {
        get
        {
            var window = Probe(_clock());
            return window is null ? new CodexDesktopPresence(false, default) :
                new CodexDesktopPresence(true, window.StartedAt);
        }
    }

    internal bool TryCancel()
    {
        var now = _clock();
        var window = Probe(now);
        if (window is null)
            return false;

        lock (_gate)
        {
            if (now < _nextCancelAt)
                return false;

            _nextCancelAt = now.Add(CancelInterval);
        }

        var key = new IntPtr(CodexDesktopCancel.VkEscape);
        return _post(window.Handle, CodexDesktopCancel.WmKeyDown, key, IntPtr.Zero) &
            _post(window.Handle, CodexDesktopCancel.WmKeyUp, key, IntPtr.Zero);
    }

    private CodexDesktopWindow? Probe(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (now < _probeExpiresAt)
                return _cachedWindow;

            _cachedWindow = FindWindow(_scan());
            _probeExpiresAt = now.Add(ProbeLifetime);
            return _cachedWindow;
        }
    }

    private static CodexDesktopWindow? FindWindow(IReadOnlyList<CodexDesktopWindow> windows)
    {
        foreach (var window in windows)
            if (window.Handle != IntPtr.Zero && IsCodexProcess(window))
                return window;

        return null;
    }

    private static bool IsCodexProcess(CodexDesktopWindow window) =>
        (string.Equals(window.ProcessName, "ChatGPT", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(window.ProcessName, "Codex", StringComparison.OrdinalIgnoreCase)) &&
        window.ExecutablePath.Contains("\\WindowsApps\\OpenAI.Codex_", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<CodexDesktopWindow> Scan()
    {
        var windows = new List<CodexDesktopWindow>();
        foreach (var processName in new[] { "ChatGPT", "Codex" })
        {
            foreach (var process in Process.GetProcessesByName(processName))
            using (process)
            {
                try
                {
                    var handle = process.MainWindowHandle;
                    var executablePath = process.MainModule?.FileName;
                    if (handle == IntPtr.Zero || string.IsNullOrEmpty(executablePath))
                        continue;

                    handle = CodexDesktopCancel.RootWindow(handle);
                    if (handle == IntPtr.Zero)
                        continue;

                    windows.Add(new CodexDesktopWindow(
                        process.ProcessName,
                        executablePath,
                        handle,
                        new DateTimeOffset(process.StartTime.ToUniversalTime())));
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
                catch (NotSupportedException)
                {
                }
            }
        }
        return windows;
    }
}
