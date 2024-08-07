using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.Utils;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace DynamicWin
{
    public partial class DynamicWinMain : Application
    {
        [STAThread]
        public static void Main()
        {
            DynamicWinMain m = new DynamicWinMain();
            m.Run();
        }


        Mutex mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            bool result;
            mutex = new System.Threading.Mutex(true, "FlorianButz.DynamicWin", out result);

            if (!result)
            {
                ErrorForm errorForm = new ErrorForm();
                errorForm.Show();
                return;
            }

            Res.Load();
            KeyHandler.Start();
            new Theme();
            Settings.InitializeSettings();

            MainForm mainForm = new MainForm();
            mainForm.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            KeyHandler.Stop();
            GC.KeepAlive(mutex); // Important
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.ExceptionObject}");
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.Exception}");
            e.Handled = true; // Prevent the application from terminating
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
