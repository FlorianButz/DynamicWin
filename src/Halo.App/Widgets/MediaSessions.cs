using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Halo.Widgets;

internal sealed class MediaSessions
{
    public const int MaxSlots = 3;

    private readonly object _lock = new();
    private readonly string[] _slotIds = new string[MaxSlots];
    private GlobalSystemMediaTransportControlsSessionManager? _mgr;

    public event Action? Changed;

    public MediaSessions()
    {
        for (int i = 0; i < MaxSlots; i++) _slotIds[i] = "";
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        try
        {
            _mgr = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _mgr.SessionsChanged += (_, _) => Reassign();
            _mgr.CurrentSessionChanged += (_, _) => Reassign();
            Reassign();
        }
        catch { }
    }

    private void Reassign()
    {
        var mgr = _mgr;
        if (mgr == null) return;
        List<string> live;
        try
        {
            live = mgr.GetSessions().Select(s => s.SourceAppUserModelId ?? "")
                .Where(id => id.Length > 0).Distinct().ToList();
        }
        catch { return; }
        lock (_lock)
        {
            for (int i = 0; i < MaxSlots; i++)
                if (_slotIds[i].Length > 0 && !live.Contains(_slotIds[i])) _slotIds[i] = "";
            foreach (var id in live)
            {
                if (Array.IndexOf(_slotIds, id) >= 0) continue;
                int free = Array.IndexOf(_slotIds, "");
                if (free >= 0) _slotIds[free] = id;
            }
        }
        Changed?.Invoke();
    }

    public GlobalSystemMediaTransportControlsSession? Session(int slot)
    {
        var mgr = _mgr;
        if (mgr == null || slot < 0 || slot >= MaxSlots) return null;
        string id;
        lock (_lock) { id = _slotIds[slot]; }
        if (id.Length == 0) return null;
        try { foreach (var s in mgr.GetSessions()) if ((s.SourceAppUserModelId ?? "") == id) return s; }
        catch { }
        return null;
    }

    public string SlotApp(int slot)
    {
        string id;
        lock (_lock) { id = slot >= 0 && slot < MaxSlots ? _slotIds[slot] : ""; }
        return id.Length == 0 ? "" : System.IO.Path.GetFileNameWithoutExtension(id).ToLowerInvariant();
    }
}
