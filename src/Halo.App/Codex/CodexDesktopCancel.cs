using System;
using System.Runtime.InteropServices;

namespace Halo.Codex;

internal static class CodexDesktopCancel
{
    internal const uint WmKeyDown = 0x0100;
    internal const uint WmKeyUp = 0x0101;
    internal const int VkEscape = 0x1B;
    private const int SwRestore = 9;

    internal static IntPtr RootWindow(IntPtr handle) => GetAncestor(handle, 2);

    internal static bool Post(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmKeyDown)
        {
            if (IsIconic(handle)) ShowWindow(handle, SwRestore);
            SetForegroundWindow(handle);
            System.Threading.Thread.Sleep(80);
            return SendKey(up: false);
        }
        return SendKey(up: true);
    }

    private static bool SendKey(bool up)
    {
        var input = new INPUT
        {
            type = 1,
            ki = new KEYBDINPUT { wVk = VkEscape, dwFlags = up ? 2u : 0u },
        };
        return SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>()) == 1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
        public long _pad;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr handle, uint flags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr handle, int cmd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);
}
