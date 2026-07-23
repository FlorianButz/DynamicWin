using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Halo.ClaudeCode;

internal static class CcCancel
{
    public static void Request(int pid)
    {
        try
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                Path.Combine(local, "Halo", "hooks", "Halo.Hooks.exe"),
                Path.Combine(AppContext.BaseDirectory, "Halo.Hooks.exe"),
            };
            var exe = candidates.FirstOrDefault(File.Exists);
            if (exe == null) return;
            Process.Start(new ProcessStartInfo(exe, $"cancel {pid}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
        }
        catch
        {
        }
    }
}
