using System;
using System.Threading;
using System.Threading.Tasks;
using Halo.Interop;

namespace Halo.Widgets;

internal static class KeyInject
{
    private const byte VK_MENU = 0x12;

    public static void Send(IntPtr hwnd, byte vk, bool alt = false)
        => SendSeq(hwnd, new[] { vk }, alt);

    public static void SendSeq(IntPtr hwnd, byte[] vks, bool alt = false)
    {
        if (hwnd == IntPtr.Zero || vks.Length == 0) return;
        Task.Run(() =>
        {
            try
            {
                if (Win32.GetForegroundWindow() != hwnd)
                {
                    IntPtr fg = Win32.GetForegroundWindow();
                    uint me = Win32.GetCurrentThreadId();
                    uint fgT = Win32.GetWindowThreadProcessId(fg, out _);
                    uint tgT = Win32.GetWindowThreadProcessId(hwnd, out _);
                    Win32.AttachThreadInput(me, fgT, true);
                    Win32.AttachThreadInput(me, tgT, true);
                    for (int i = 0; i < 4 && Win32.GetForegroundWindow() != hwnd; i++)
                    {
                        Win32.SetForegroundWindow(hwnd);
                        Thread.Sleep(40);
                    }
                    Win32.AttachThreadInput(me, fgT, false);
                    Win32.AttachThreadInput(me, tgT, false);
                    Thread.Sleep(70);
                    if (Win32.GetForegroundWindow() != hwnd) return;
                }
                if (alt) Win32.keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
                foreach (byte vk in vks)
                {
                    Win32.keybd_event(vk, 0, 0, UIntPtr.Zero);
                    Win32.keybd_event(vk, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    Thread.Sleep(35);
                }
                if (alt) Win32.keybd_event(VK_MENU, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        });
    }
}
