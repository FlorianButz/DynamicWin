using System;
using System.Threading;
using Microsoft.Win32;

namespace Halo.Widgets;

internal static class Privacy
{
    public static volatile bool Mic, Cam;
    public static int Version;
    public static bool Active => Mic || Cam;

    private const string Base =
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\";
    private static Timer? _timer;

    public static void Poke() => _timer ??= new Timer(_ => Scan(), null, 800, 1200);

    private static void Scan()
    {
        try
        {
            bool mic = InUse("microphone"), cam = InUse("webcam");
            if (mic == Mic && cam == Cam) return;
            Mic = mic; Cam = cam;
            Interlocked.Increment(ref Version);
        }
        catch { }
    }

    private static bool InUse(string capability)
    {
        using var root = Registry.CurrentUser.OpenSubKey(Base + capability);
        return root != null && AnyLive(root, 0);
    }

    private static readonly string[] Ignore = { "pythonw.exe" };

    private static bool AnyLive(RegistryKey key, int depth)
    {
        if (key.GetValue("LastUsedTimeStop") is long stop && stop == 0) return true;
        if (depth >= 3) return false;
        foreach (var name in key.GetSubKeyNames())
        {
            bool skip = false;
            foreach (var ig in Ignore)
                if (name.EndsWith(ig, StringComparison.OrdinalIgnoreCase)) { skip = true; break; }
            if (skip) continue;
            using var sub = key.OpenSubKey(name);
            if (sub != null && AnyLive(sub, depth + 1)) return true;
        }
        return false;
    }
}
