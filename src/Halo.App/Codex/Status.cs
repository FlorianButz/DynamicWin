using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Halo.Codex;

internal enum CodexSurface { Cli, Desktop }

internal sealed record CodexLimit(double UsedPercent, int WindowMinutes, DateTimeOffset? ResetsAt);

[Flags]
internal enum CodexSnapshotFields
{
    None = 0,
    ContextUsed = 1 << 0,
    ContextMax = 1 << 1,
    PromptTokens = 1 << 2,
    PrimaryLimit = 1 << 3,
    SecondaryLimit = 1 << 4,
}

internal sealed record CodexSnapshot(
    CodexSurface Source, string State, string? CurrentTool, DateTimeOffset? StartedAt,
    DateTimeOffset? CompactedAt, string? Message, string? Cwd, int Pid, int ConsolePid,
    long ContextUsed, long ContextMax, long PromptTokens, CodexLimit? PrimaryLimit,
    CodexLimit? SecondaryLimit, DateTimeOffset UpdatedAt, bool ProcessAlive)
{
    internal CodexSnapshotFields PresentFields { get; init; }
}

internal static class CodexRollout
{
    internal static CodexSnapshot? Parse(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var state = "idle";
            string? currentTool = null;
            DateTimeOffset? startedAt = null;
            DateTimeOffset? compactedAt = null;
            string? message = null;
            string? cwd = null;
            var source = CodexSurface.Cli;
            long contextUsed = 0;
            long contextMax = 0;
            long promptTokens = 0;
            CodexLimit? primaryLimit = null;
            CodexLimit? secondaryLimit = null;
            var presentFields = CodexSnapshotFields.None;
            var updatedAt = DateTimeOffset.MinValue;
            var sawEvent = false;

            foreach (var line in ReadSharedLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement;
                    var payload = Property(root, "payload") ?? root;
                    var eventType = String(payload, "type") ?? String(root, "type");
                    var timestamp = Timestamp(root, "timestamp");

                    sawEvent = true;
                    if (timestamp is { } value && value > updatedAt)
                        updatedAt = value;

                    switch (eventType)
                    {
                        case "session_meta":
                            cwd ??= String(payload, "cwd");
                            if (String(payload, "originator")?.Contains("Desktop", StringComparison.OrdinalIgnoreCase) == true)
                                source = CodexSurface.Desktop;
                            break;
                        case "task_started":
                            state = "working";
                            startedAt = timestamp;
                            if (Number(payload, "model_context_window") is { } startedContextMax)
                            {
                                contextMax = startedContextMax;
                                presentFields |= CodexSnapshotFields.ContextMax;
                            }
                            break;
                        case "custom_tool_call":
                            state = "working";
                            currentTool = ShortTool(String(payload, "name") ?? String(payload, "tool_name") ?? String(payload, "tool"));
                            break;
                        case "request_user_input":
                        case "request_user_approval":
                        case "approval":
                            state = "waiting_input";
                            message = String(payload, "message") ?? String(payload, "text") ?? String(payload, "prompt") ?? message;
                            break;
                        case "task_complete":
                            state = "idle";
                            currentTool = null;
                            startedAt = null;
                            break;
                        case "pre_compact":
                        case "precompact":
                        case "PreCompact":
                            state = "compacting";
                            startedAt ??= timestamp;
                            break;
                        case "post_compact":
                        case "postcompact":
                        case "PostCompact":
                            state = "working";
                            compactedAt = timestamp;
                            break;
                        case "token_count":
                            var info = Property(payload, "info");
                            if (info is { } tokenInfo)
                            {
                                if (Number(tokenInfo, "model_context_window") is { } tokenContextMax)
                                {
                                    contextMax = tokenContextMax;
                                    presentFields |= CodexSnapshotFields.ContextMax;
                                }
                                var lastTokens = TotalTokens(Property(tokenInfo, "last_token_usage"));
                                var contextTokens = lastTokens ?? TotalTokens(Property(tokenInfo, "total_token_usage"));
                                if (contextTokens is { } currentTokens)
                                {
                                    contextUsed = currentTokens;
                                    presentFields |= CodexSnapshotFields.ContextUsed;
                                }
                                if (lastTokens is { } turnTokens)
                                {
                                    promptTokens = turnTokens;
                                    presentFields |= CodexSnapshotFields.PromptTokens;
                                }
                            }

                            var limits = Property(payload, "rate_limits");
                            if (limits is { } rateLimits)
                            {
                                if (Limit(Property(rateLimits, "primary"), timestamp) is { } primary)
                                {
                                    primaryLimit = primary;
                                    presentFields |= CodexSnapshotFields.PrimaryLimit;
                                }
                                if (Limit(Property(rateLimits, "secondary"), timestamp) is { } secondary)
                                {
                                    secondaryLimit = secondary;
                                    presentFields |= CodexSnapshotFields.SecondaryLimit;
                                }
                            }
                            break;
                    }
                }
                catch (JsonException)
                {

                }
            }

            if (!sawEvent)
                return null;

            if (updatedAt == DateTimeOffset.MinValue)
                updatedAt = File.GetLastWriteTimeUtc(path);

            return new CodexSnapshot(
                source, state, currentTool, startedAt, compactedAt, message, cwd, 0, 0,
                contextUsed, contextMax, promptTokens, primaryLimit, secondaryLimit, updatedAt, false)
            {
                PresentFields = presentFields,
            };
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static CodexLimit? Limit(JsonElement? value, DateTimeOffset? timestamp)
    {
        if (value is not { } limit)
            return null;

        var usedPercent = NumberDouble(limit, "used_percent");
        var windowMinutes = Number(limit, "window_minutes");
        if (usedPercent is null || windowMinutes is null)
            return null;

        var resetsAt = Timestamp(limit, "resets_at");
        if (resetsAt is null && Number(limit, "resets_in_seconds") is { } seconds)
            resetsAt = (timestamp ?? DateTimeOffset.UtcNow).AddSeconds(seconds);

        return new CodexLimit(usedPercent.Value, checked((int)windowMinutes.Value), resetsAt);
    }

    private static long? TotalTokens(JsonElement? usage) => usage is { } value ? Number(value, "total_tokens") : null;

    private static string? ShortTool(string? tool) => tool?.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

    private static JsonElement? Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;

    private static string? String(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static long? Number(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt64(out var number) ? number :
        Property(element, name) is { ValueKind: JsonValueKind.String } text && long.TryParse(text.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ? number : null;

    private static double? NumberDouble(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number) ? number :
        Property(element, name) is { ValueKind: JsonValueKind.String } text && double.TryParse(text.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) ? number : null;

    private static DateTimeOffset? Timestamp(JsonElement element, string name)
    {
        var value = Property(element, name);
        if (value is { ValueKind: JsonValueKind.Number } && value.Value.TryGetInt64(out var unix))
            return DateTimeOffset.FromUnixTimeSeconds(unix);

        if (value is { ValueKind: JsonValueKind.String } && DateTimeOffset.TryParse(value.Value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            return timestamp;

        return null;
    }

    internal static CodexSurface? IdentifySurface(string path)
    {
        try
        {
            foreach (var line in ReadSharedLines(path).Take(8))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement;
                    if (String(root, "type") != "session_meta" || Property(root, "payload") is not { } payload)
                        continue;

                    return String(payload, "originator")?.Contains("Desktop", StringComparison.OrdinalIgnoreCase) == true
                        ? CodexSurface.Desktop
                        : CodexSurface.Cli;
                }
                catch (JsonException)
                {
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    private static IEnumerable<string> ReadSharedLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
            yield return line;
    }
}

internal sealed class CodexStatusStore : IDisposable
{
    private const int ReloadDelayMilliseconds = 40;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly string _statusDirectory;
    private readonly string _sessionsDirectory;
    private readonly Func<int, bool> _processAlive;
    private readonly Func<string, CodexSnapshot?> _parseRollout;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<CodexDesktopPresence> _desktopPresence;
    private readonly object _workGate = new();
    private readonly object _publicationGate = new();
    private readonly object _scheduleGate = new();
    private readonly Timer _reloadTimer;
    private readonly FileSystemWatcher? _statusWatcher;
    private readonly FileSystemWatcher? _rolloutWatcher;
    private readonly HashSet<string> _pendingRolloutPaths = new(StringComparer.OrdinalIgnoreCase);
    private CodexSnapshot? _desktopStatus;
    private CodexSnapshot? _cliStatus;
    private CodexSnapshot? _desktopRollout;
    private CodexSnapshot? _cliRollout;
    private CodexSnapshot? _desktopCandidate;
    private CodexSnapshot? _cliCandidate;
    private string? _desktopRolloutPath;
    private string? _cliRolloutPath;
    private DateTime _desktopRolloutWriteTime;
    private DateTime _cliRolloutWriteTime;
    private CodexSnapshot? _current;
    private int _version;
    private long _publicationGeneration;
    private bool _pendingStatusReload;
    private bool _pendingFullRescan;
    private volatile bool _disposed;

    internal CodexSnapshot? Current
    {
        get => ReevaluateCurrent();
    }

    private readonly (CodexSnapshot? live, DateTimeOffset at, int version)[] _surfaceCache
        = new (CodexSnapshot?, DateTimeOffset, int)[2];

    internal CodexSnapshot? Candidate(CodexSurface surface)
    {
        var now = _clock();
        int version; CodexSnapshot? snap;
        lock (_publicationGate)
        {
            version = _version;
            snap = surface == CodexSurface.Desktop ? _desktopCandidate : _cliCandidate;
        }
        ref var c = ref _surfaceCache[(int)surface];
        if (c.version == version && now - c.at <= TimeSpan.FromSeconds(1))
            return c.live;
        snap = surface == CodexSurface.Desktop
            ? NormalizeDesktop(RefreshProcessAlive(snap), _desktopPresence(), now)
            : RefreshProcessAlive(snap);
        c = (IsActive(snap, now) ? snap : null, now, version);
        return c.live;
    }

    internal int Version
    {
        get
        {
            ReevaluateCurrent();
            lock (_publicationGate) return _version;
        }
    }

    internal CodexStatusStore()
        : this(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "notch"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions"),
            IsProcessAlive,
            watchFiles: true,
            desktopPresence: () => CodexDesktopRuntime.Shared.Presence)
    {
    }

    internal CodexStatusStore(
        string statusDirectory,
        string sessionsDirectory,
        Func<int, bool> processAlive,
        bool watchFiles,
        Func<string, CodexSnapshot?>? parseRollout = null,
        Func<DateTimeOffset>? clock = null,
        Func<CodexDesktopPresence>? desktopPresence = null)
    {
        _statusDirectory = statusDirectory;
        _sessionsDirectory = sessionsDirectory;
        _processAlive = processAlive;
        _parseRollout = parseRollout ?? CodexRollout.Parse;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _desktopPresence = desktopPresence ?? (() => CodexDesktopRuntime.Shared.Presence);
        Directory.CreateDirectory(_statusDirectory);
        Directory.CreateDirectory(_sessionsDirectory);
        _reloadTimer = new Timer(_ => ProcessScheduledReload(), null, Timeout.Infinite, Timeout.Infinite);

        if (watchFiles)
        {
            _statusWatcher = CreateWatcher(_statusDirectory, "*.json", includeSubdirectories: false, rollout: false);
            _rolloutWatcher = CreateWatcher(_sessionsDirectory, "*.jsonl", includeSubdirectories: true, rollout: true);
        }

        Reload(statusChanged: true, fullRescan: true, []);
    }

    internal void ForceRefresh() => Reload(statusChanged: true, fullRescan: true, []);

    internal static CodexSnapshot? Select(CodexSnapshot? desktop, CodexSnapshot? cli, DateTimeOffset now) =>
        IsActive(desktop, now) ? desktop : IsActive(cli, now) ? cli : null;

    public void Dispose()
    {
        lock (_scheduleGate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _statusWatcher?.Dispose();
            _rolloutWatcher?.Dispose();
            _reloadTimer.Dispose();
        }

        lock (_workGate)
        {
        }
    }

    private FileSystemWatcher CreateWatcher(string directory, string filter, bool includeSubdirectories, bool rollout)
    {
        var watcher = new FileSystemWatcher(directory, filter)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        if (rollout)
        {
            watcher.Changed += (_, e) => ScheduleReload(rolloutPath: e.FullPath);
            watcher.Created += (_, e) => ScheduleReload(rolloutPath: e.FullPath);
            watcher.Deleted += (_, _) => ScheduleReload(fullRescan: true);
            watcher.Renamed += (_, _) => ScheduleReload(fullRescan: true);
        }
        else
        {
            watcher.Changed += (_, _) => ScheduleReload(statusChanged: true);
            watcher.Created += (_, _) => ScheduleReload(statusChanged: true);
            watcher.Deleted += (_, _) => ScheduleReload(statusChanged: true);
            watcher.Renamed += (_, _) => ScheduleReload(statusChanged: true);
        }
        watcher.Error += HandleWatcherError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    internal void HandleWatcherError(object? sender, ErrorEventArgs e) =>
        ScheduleReload(statusChanged: true, fullRescan: true);

    private void ScheduleReload(bool statusChanged = false, bool fullRescan = false, string? rolloutPath = null)
    {
        lock (_scheduleGate)
        {
            if (_disposed)
                return;

            _pendingStatusReload |= statusChanged;
            _pendingFullRescan |= fullRescan;
            if (rolloutPath is not null)
                _pendingRolloutPaths.Add(rolloutPath);
            _reloadTimer.Change(ReloadDelayMilliseconds, Timeout.Infinite);
        }
    }

    private void ProcessScheduledReload()
    {
        bool statusChanged;
        bool fullRescan;
        string[] rolloutPaths;
        lock (_scheduleGate)
        {
            if (_disposed)
                return;

            statusChanged = _pendingStatusReload;
            fullRescan = _pendingFullRescan;
            rolloutPaths = [.. _pendingRolloutPaths];
            _pendingStatusReload = false;
            _pendingFullRescan = false;
            _pendingRolloutPaths.Clear();
        }

        Reload(statusChanged, fullRescan, rolloutPaths);
    }

    private void Reload(bool statusChanged, bool fullRescan, IReadOnlyCollection<string> rolloutPaths)
    {
        lock (_workGate)
        {
            if (_disposed)
                return;

            if (statusChanged || fullRescan)
            {
                ApplyStatusRead(ref _desktopStatus, ReadStatusWithRetry(
                    Path.Combine(_statusDirectory, "desktop.json"), CodexSurface.Desktop, _desktopStatus is not null));
                ApplyStatusRead(ref _cliStatus, ReadStatusWithRetry(
                    Path.Combine(_statusDirectory, "cli.json"), CodexSurface.Cli, _cliStatus is not null));
            }
            else
            {
                _desktopStatus = RefreshProcessAlive(_desktopStatus);
                _cliStatus = RefreshProcessAlive(_cliStatus);
            }

            if (fullRescan)
                ScanRollouts();
            else if (rolloutPaths.Count > 0)
                ProcessRolloutChanges(rolloutPaths);

            if (_disposed)
                return;

            var now = _clock();
            var desktopCandidate = NormalizeDesktop(Merge(_desktopStatus, _desktopRollout, now), _desktopPresence(), now);
            var cliCandidate = Merge(_cliStatus, _cliRollout, now);
            var next = Select(desktopCandidate, cliCandidate, now);
            lock (_publicationGate)
            {
                if (_disposed)
                    return;
                _desktopCandidate = desktopCandidate;
                _cliCandidate = cliCandidate;
                _current = next;
                _version++;
                _publicationGeneration++;
            }
        }
    }

    private CodexSnapshot? ReevaluateCurrent()
    {
        while (true)
        {
            CodexSnapshot? desktop;
            CodexSnapshot? cli;
            long generation;
            lock (_publicationGate)
            {
                desktop = _desktopCandidate;
                cli = _cliCandidate;
                generation = _publicationGeneration;
            }

            var now = _clock();
            desktop = NormalizeDesktop(RefreshProcessAlive(desktop), _desktopPresence(), now);
            cli = RefreshProcessAlive(cli);
            var next = Select(desktop, cli, now);

            lock (_publicationGate)
            {
                if (generation != _publicationGeneration)
                    continue;

                if (!Equals(_current, next))
                {
                    _current = next;
                    _version++;
                }
                return _current;
            }
        }
    }

    private StatusRead ReadStatusWithRetry(string path, CodexSurface source, bool hasExisting)
    {
        var read = ReadStatus(path, source);
        if (read.Kind == StatusReadKind.Transient || read.Kind == StatusReadKind.Missing && hasExisting)
        {
            Thread.Sleep(ReloadDelayMilliseconds);
            read = ReadStatus(read.Path, read.Source);
        }
        return read;
    }

    private static void ApplyStatusRead(ref CodexSnapshot? target, StatusRead read)
    {
        if (read.Kind == StatusReadKind.Success)
            target = read.Snapshot;
        else if (read.Kind == StatusReadKind.Missing)
            target = null;
    }

    private CodexSnapshot? RefreshProcessAlive(CodexSnapshot? snapshot) =>
        snapshot is null ? null : snapshot with { ProcessAlive = ProcessAlive(snapshot.Pid) };

    private static bool IsActive(CodexSnapshot? snapshot, DateTimeOffset now) =>
        snapshot is not null && snapshot.State != "ended" &&
        (snapshot.ProcessAlive || snapshot.UpdatedAt >= now.AddSeconds(-30));

    private static bool IsFresh(CodexSnapshot snapshot, DateTimeOffset now) =>
        snapshot.ProcessAlive || snapshot.UpdatedAt >= now.AddSeconds(-30);

    internal static CodexSnapshot? NormalizeDesktop(
        CodexSnapshot? snapshot, CodexDesktopPresence presence, DateTimeOffset now)
    {
        if (!presence.Running)
            return null;
        if (snapshot is null || snapshot.UpdatedAt < presence.StartedAt)
            return EmptyDesktop(presence.StartedAt);
        if (snapshot.UpdatedAt < now.AddSeconds(-30) &&
            snapshot.State is "working" or "waiting_input" or "compacting")
            return snapshot with { State = "idle", CurrentTool = null, StartedAt = null, ProcessAlive = true };
        return snapshot with { ProcessAlive = true };
    }

    private static CodexSnapshot EmptyDesktop(DateTimeOffset startedAt) => new(
        CodexSurface.Desktop, "idle", null, null, null, null, null, 0, 0, 0, 0, 0,
        null, null, startedAt, true);

    private static CodexSnapshot? Merge(CodexSnapshot? hook, CodexSnapshot? rollout, DateTimeOffset now)
    {
        if (hook is null)
            return rollout;
        if (rollout is null)
            return hook;

        var lifecycle = IsFresh(hook, now) ? hook : rollout;
        var fields = rollout.PresentFields;
        return lifecycle with
        {
            Source = hook.Source,
            Cwd = lifecycle.Cwd ?? hook.Cwd ?? rollout.Cwd,
            Pid = hook.Pid,
            ConsolePid = hook.ConsolePid,
            ContextUsed = fields.HasFlag(CodexSnapshotFields.ContextUsed) ? rollout.ContextUsed : hook.ContextUsed,
            ContextMax = fields.HasFlag(CodexSnapshotFields.ContextMax) ? rollout.ContextMax : hook.ContextMax,
            PromptTokens = fields.HasFlag(CodexSnapshotFields.PromptTokens) ? rollout.PromptTokens : hook.PromptTokens,
            PrimaryLimit = fields.HasFlag(CodexSnapshotFields.PrimaryLimit) ? rollout.PrimaryLimit : hook.PrimaryLimit,
            SecondaryLimit = fields.HasFlag(CodexSnapshotFields.SecondaryLimit) ? rollout.SecondaryLimit : hook.SecondaryLimit,
            UpdatedAt = hook.UpdatedAt > rollout.UpdatedAt ? hook.UpdatedAt : rollout.UpdatedAt,
            ProcessAlive = hook.ProcessAlive,
            PresentFields = hook.PresentFields | rollout.PresentFields,
        };
    }

    private StatusRead ReadStatus(string path, CodexSurface source)
    {
        try
        {
            if (!File.Exists(path))
                return new StatusRead(path, source, StatusReadKind.Missing, null);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var status = JsonSerializer.Deserialize<HookStatus>(stream, JsonOptions);
            if (status is null)
                return new StatusRead(path, source, StatusReadKind.Transient, null);

            var updatedAt = status.UpdatedAt ?? File.GetLastWriteTimeUtc(path);
            var snapshot = new CodexSnapshot(
                source, status.State ?? "idle", status.CurrentTool, status.StartedAt, status.CompactedAt,
                status.Message, status.Cwd, status.Pid, status.ConsolePid, status.ContextUsed, status.ContextMax,
                status.PromptTokens, status.PrimaryLimit, status.SecondaryLimit, updatedAt,
                ProcessAlive(status.Pid));
            return new StatusRead(path, source, StatusReadKind.Success, snapshot);
        }
        catch (IOException)
        {
            return new StatusRead(path, source, StatusReadKind.Transient, null);
        }
        catch (JsonException)
        {
            return new StatusRead(path, source, StatusReadKind.Transient, null);
        }
        catch (UnauthorizedAccessException)
        {
            return new StatusRead(path, source, StatusReadKind.Transient, null);
        }
    }

    private bool ProcessAlive(int pid)
    {
        try
        {
            return pid > 0 && _processAlive(pid);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void ScanRollouts()
    {
        RolloutCandidate? desktop = null;
        RolloutCandidate? cli = null;

        try
        {
            foreach (var path in Directory.EnumerateFiles(_sessionsDirectory, "*.jsonl", SearchOption.AllDirectories))
            {
                var source = CodexRollout.IdentifySurface(path);
                if (source is null)
                    continue;

                var candidate = new RolloutCandidate(path, File.GetLastWriteTimeUtc(path));
                if (source == CodexSurface.Desktop && (desktop is null || candidate.WriteTime > desktop.Value.WriteTime))
                    desktop = candidate;
                else if (source == CodexSurface.Cli && (cli is null || candidate.WriteTime > cli.Value.WriteTime))
                    cli = candidate;
            }

            ApplyRolloutCandidate(CodexSurface.Desktop, desktop);
            ApplyRolloutCandidate(CodexSurface.Cli, cli);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void ApplyRolloutCandidate(CodexSurface source, RolloutCandidate? candidate)
    {
        if (candidate is null)
        {
            SetRollout(source, null, null, DateTime.MinValue);
            return;
        }

        var snapshot = _parseRollout(candidate.Value.Path);
        if (snapshot is not null)
            SetRollout(source, snapshot, candidate.Value.Path, candidate.Value.WriteTime);
    }

    private void ProcessRolloutChanges(IEnumerable<string> paths)
    {
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (!File.Exists(path) || CodexRollout.IdentifySurface(path) is not { } source)
                    continue;

                var writeTime = File.GetLastWriteTimeUtc(path);
                var currentPath = source == CodexSurface.Desktop ? _desktopRolloutPath : _cliRolloutPath;
                var currentWriteTime = source == CodexSurface.Desktop ? _desktopRolloutWriteTime : _cliRolloutWriteTime;
                if (!string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase) && writeTime < currentWriteTime)
                    continue;

                var snapshot = _parseRollout(path);
                if (snapshot is not null)
                    SetRollout(source, snapshot, path, writeTime);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private void SetRollout(CodexSurface source, CodexSnapshot? snapshot, string? path, DateTime writeTime)
    {
        if (source == CodexSurface.Desktop)
        {
            _desktopRollout = snapshot;
            _desktopRolloutPath = path;
            _desktopRolloutWriteTime = writeTime;
        }
        else
        {
            _cliRollout = snapshot;
            _cliRolloutPath = path;
            _cliRolloutWriteTime = writeTime;
        }
    }

    private static bool IsProcessAlive(int pid)
    {
        if (pid <= 0)
            return false;

        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private sealed class HookStatus
    {
        public string? State { get; set; }
        public string? CurrentTool { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompactedAt { get; set; }
        public string? Message { get; set; }
        public string? Cwd { get; set; }
        public int Pid { get; set; }
        public int ConsolePid { get; set; }
        public long ContextUsed { get; set; }
        public long ContextMax { get; set; }
        public long PromptTokens { get; set; }
        public CodexLimit? PrimaryLimit { get; set; }
        public CodexLimit? SecondaryLimit { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    private enum StatusReadKind { Missing, Success, Transient }

    private readonly record struct StatusRead(
        string Path,
        CodexSurface Source,
        StatusReadKind Kind,
        CodexSnapshot? Snapshot);

    private readonly record struct RolloutCandidate(string Path, DateTime WriteTime);
}
