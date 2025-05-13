using DynamicWin.Resources;
using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.Utils;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace DynamicWin.Main
{
    public partial class MainForm : Window
    {
        private static MainForm instance;
        public static MainForm Instance { get => instance; }

        public static Action<System.Windows.Input.MouseWheelEventArgs> onScrollEvent;

        private readonly Forms.NotifyIcon _trayIcon;

        private DateTime _lastRenderTime;
        private readonly TimeSpan _targetElapsedTime = TimeSpan.FromMilliseconds(16); // ~60 FPS

        public Action onMainFormRender;

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr window, int idx, int val);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr window, int idx);

        // Define integer values
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;

        public MainForm()
        {
            InitializeComponent();

            _trayIcon = new Forms.NotifyIcon();

            CompositionTarget.Rendering += OnRendering;

            instance = this;

            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;
            this.AllowsTransparency = true;
            this.ShowInTaskbar = false;
            this.Title = "DynamicWin Overlay";

            // Loaded event to ensure that this does not show the application on the Alt+Tab switcher

            this.Loaded += (s, e) =>
            {
                IntPtr handle = new WindowInteropHelper(this).Handle; // Define Handle
                int winStyle = GetWindowLong(handle, GWL_EXSTYLE); // Fetch defined GWL_EXSTYLE

                // Apply WS_EX_TOOLWINDOW and remove WS_EX_APPWINDOW if it exists
                winStyle = (winStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
                SetWindowLong(handle, GWL_EXSTYLE, winStyle);
            };

            SetMonitor(Settings.ScreenIndex);

            AddRenderer();

            Res.extensions.ForEach((x) => x.LoadExtension());
            MainForm.Instance.AllowDrop = true;

            // Tray icon

            _trayIcon.Icon = new System.Drawing.Icon("Resources/icons/TrayIcon.ico");
            _trayIcon.Text = "DynamicWin";

            _trayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();

            _trayIcon.ContextMenuStrip.Items.Add("Restart Control", null, (x, y) =>
            {
                if (RendererMain.Instance != null) RendererMain.Instance.Destroy();
                this.Content = new Grid();

                AddRenderer();
            });

            _trayIcon.ContextMenuStrip.Items.Add("Settings", null, (x, y) =>
            {
                MenuManager.OpenMenu(new SettingsMenu());
            });

            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (x, y) =>
            {
                SaveManager.SaveAll();
                Process.GetCurrentProcess().Kill();
            });

            _trayIcon.Visible = true;
        }


        public void SetMonitor(int monitorIndex)
        {
            var screen = System.Windows.Forms.Screen.AllScreens[Math.Clamp(monitorIndex, 0, GetMonitorCount() - 1)];
            Settings.ScreenIndex = Math.Clamp(monitorIndex, 0, GetMonitorCount() - 1);

            if (screen != null)
            {
                if (!this.IsLoaded)
                    this.WindowStartupLocation = WindowStartupLocation.Manual;

                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.CanResize;

                var workingArea = screen.WorkingArea;

                this.Left = workingArea.Left;
                this.Top = workingArea.Top;
                this.Width = workingArea.Width;
                this.Height = workingArea.Height;

                this.ResizeMode = ResizeMode.NoResize;
            }
        }

        public static int GetMonitorCount()
        {
            return System.Windows.Forms.Screen.AllScreens.Length;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            if (currentTime - _lastRenderTime >= _targetElapsedTime)
            {
                _lastRenderTime = currentTime;

                onMainFormRender?.Invoke();
            }
        }

        public bool isDragging = false;

        public void OnScroll(object? sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            onScrollEvent?.Invoke(e);
        }

        public void AddRenderer()
        {
            if (RendererMain.Instance != null) RendererMain.Instance.Destroy();

            var customControl = new RendererMain();
            
            var parent = new Grid();
            parent.Children.Add(customControl);

            this.Content = parent;
        }

        public void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("DragEnter");

            isDragging = true;
            e.Effects = DragDropEffects.Copy;

            if (!(MenuManager.Instance.ActiveMenu is DropFileMenu)
                && !(MenuManager.Instance.ActiveMenu is ConfigureShortcutMenu))
            {
                MenuManager.OpenMenu(new DropFileMenu());
            }
        }

        public void MainForm_DragLeave(object? sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("DragLeave");

            isDragging = false;

            if (MenuManager.Instance.ActiveMenu is ConfigureShortcutMenu) return;
            MenuManager.OpenMenu(Res.HomeMenu);
        }

        bool isLocalDrag = false;

        internal void StartDrag(string[] files, Action callback)
        {
            if (isLocalDrag) return;

            Array.ForEach(files, file => { System.Diagnostics.Debug.WriteLine(file); });

            if (files == null) return;
            else if (files.Length <= 0) return;

            try
            {
                isLocalDrag = true;

                DataObject dataObject = new DataObject(DataFormats.FileDrop, files);
                var effects = DragDrop.DoDragDrop((DependencyObject)this, dataObject, DragDropEffects.Move | DragDropEffects.Copy);

                if (RendererMain.Instance != null) RendererMain.Instance.Destroy();
                this.Content = new Grid();
                AddRenderer();

                callback?.Invoke();

                isLocalDrag = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            if (e.Action == DragAction.Cancel)
            {
                isLocalDrag = false;
            }
            else if (e.Action == DragAction.Continue)
            {
                isLocalDrag = true;
            }
            else if (e.Action == DragAction.Drop)
            {
                isLocalDrag = false;
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            base.OnDragOver(e);
        }

        public void OnDrop(object sender, System.Windows.DragEventArgs e)
        {
            isDragging = false;

            if(MenuManager.Instance.ActiveMenu is ConfigureShortcutMenu)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    ConfigureShortcutMenu.DropData(e);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                DropFileMenu.Drop(e);
                MenuManager.Instance.QueueOpenMenu(Res.HomeMenu);
                Res.HomeMenu.isWidgetMode = false;
            }
        }

        internal void DisposeTrayIcon()
        {
            _trayIcon.Dispose();
        }
    }
}
