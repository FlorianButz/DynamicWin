using DynamicWin.Resources;
using System.Diagnostics;

namespace DynamicWin.Main
{
    internal static class DynamicWinMain
    {
        [STAThread]
        static void Main()
        {
            Resources.Resources.Load();

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        private static readonly DateTime Jan1st1970 = new DateTime
            (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        public static long NanoTime()
        {
            long nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }
    }
}