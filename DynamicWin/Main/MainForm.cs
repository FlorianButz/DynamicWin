using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using Microsoft.VisualBasic;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Main
{
    public class MainForm : Form
    {
        private static MainForm instance;
        public static MainForm Instance { get => instance; }

        public static Action<System.Windows.Forms.MouseEventArgs> onScrollEvent;

        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        public MainForm()
        {
            instance = this;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            TransparencyKey = BackColor;
            ShowInTaskbar = false;

            IntPtr hWnd = this.Handle;
            SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

            var customControl = new RendererMain
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(customControl);

            MainForm.Instance.AllowDrop = true;
        }

        public void OnScroll(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            onScrollEvent?.Invoke(e);
        }

        public bool isDragging = false;

        public void MainForm_DragOver(object? sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("DragOver");

            isDragging = true;
        }

        public void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("DragEnter");

            isDragging = true;
            e.Effect = DragDropEffects.Copy;

            if (!(MenuManager.Instance.ActiveMenu is DropFileMenu))
            {
                MenuManager.OpenMenu(new DropFileMenu());
            }
        }

        public void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("DragDrop");

            isDragging = false;
            DropFileMenu.Drop(e);

            MenuManager.Instance.QueueOpenMenu(Resources.Resources.HomeMenu);
        }

        public void MainForm_DragLeave(object? sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("DragLeave");

            isDragging = false;

            MenuManager.OpenMenu(Resources.Resources.HomeMenu);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        internal void StartDrag(string file)
        {
            return;

            DoDragDrop(new DataObject(DataFormats.FileDrop, new string[] { String.Join(file, "") }), DragDropEffects.Copy);
            //RendererMain.Instance.MainIsland.hidden = true;
        }
    }
}
