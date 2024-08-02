using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
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
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        public MainForm()
        {
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

            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
            this.DragLeave += MainForm_DragLeave;
            this.DragOver += MainForm_DragOver;
        }

        private void MainForm_DragOver(object? sender, DragEventArgs e)
        {
            
        }

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;

            if (!(MenuManager.Instance.ActiveMenu is DropFileMenu))
            {
                MenuManager.OpenMenu(new DropFileMenu());
            }
        }

        private void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            DropFileMenu.Drop(e);

            MenuManager.Instance.QueueOpenMenu(new HomeMenu());
        }

        private void MainForm_DragLeave(object? sender, EventArgs e)
        {
            MenuManager.OpenMenu(new HomeMenu());
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
    }
}
