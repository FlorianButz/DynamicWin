using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicWin.Utils
{
    public class MediaController
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        private const byte VK_MEDIA_PREV_TRACK = 0xB1;

        public void PlayPause()
        {
            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, 0);
        }

        public void Next()
        {
            keybd_event(VK_MEDIA_NEXT_TRACK, 0, 0, 0);
        }

        public void Previous()
        {
            keybd_event(VK_MEDIA_PREV_TRACK, 0, 0, 0);
        }
    }
}
