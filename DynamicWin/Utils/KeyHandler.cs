using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DynamicWin.Utils
{
    public class KeyHandler
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void Start()
        {
            _hookID = SetHook(_proc);
        }

        public static void Stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        public static List<Keys> keyDown = new List<Keys>();
        public static Action<Keys, KeyModifier> onKeyDown;

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && !keyDown.Contains((Keys)vkCode))
            {
                keyDown.Add((Keys)vkCode);

                var keyModi = new KeyModifier();
                keyModi.isShiftDown = keyDown.Contains(Keys.LShiftKey) || keyDown.Contains(Keys.RShiftKey);
                keyModi.isCtrlDown = keyDown.Contains(Keys.LControlKey) || keyDown.Contains(Keys.RControlKey);

                onKeyDown?.Invoke((Keys)vkCode, keyModi);
            }
            else if(nCode >= 0 && wParam == (IntPtr)WM_KEYUP && keyDown.Contains((Keys)vkCode))
            {
                keyDown.Remove((Keys)vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public struct KeyModifier
    {
        public bool isCtrlDown = false;
        public bool isShiftDown = false;

        public KeyModifier()
        {
        }
    }
}
