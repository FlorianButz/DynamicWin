using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using IWshRuntimeLibrary;

namespace DynamicWin.Utils
{

    public class StartupShortcutManager
    {
        private static string GetStartupFolderPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        }

        private static string GetShortcutPath(string shortcutName)
        {
            return Path.Combine(GetStartupFolderPath(), $"{shortcutName}.lnk");
        }

        public static void CreateShortcut()
        {
            string appPath = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            string shortcutPath = GetShortcutPath(appPath);

            if (System.IO.File.Exists(shortcutPath))
            {
                Console.WriteLine("Shortcut already exists.");
                return;
            }

            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            WshShell wshShell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Description = "Launches the app on system startup.";
            shortcut.Save();

            Console.WriteLine("Shortcut created successfully.");
        }

        public static bool RemoveShortcut()
        {
            string appPath = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            string shortcutPath = GetShortcutPath(appPath);

            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
                Console.WriteLine("Shortcut removed successfully.");
                return true;
            }

            Console.WriteLine("Shortcut does not exist.");
            return false;
        }
    }

}
