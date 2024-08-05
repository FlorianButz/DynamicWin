using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.Utils;
using Microsoft.VisualBasic;
using OpenTK.Input;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public bool isDragging = false;

        public void OnScroll(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            onScrollEvent?.Invoke(e);
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

        bool isLocalDrag = false;

        internal void StartDrag(string file)
        {
            if (isLocalDrag) return;

            System.Diagnostics.Debug.WriteLine(file);
            try
            {
                isLocalDrag = true;

                if (RendererMain.Instance != null) RendererMain.Instance.Destroy();
                Controls.Clear();

                DataObject dataObject = new DataObject(DataFormats.FileDrop, new string[] { file });
                var effects = DoDragDrop(dataObject, DragDropEffects.Copy |
                    DragDropEffects.Move);

                var customControl = new RendererMain
                {
                    Dock = DockStyle.Fill
                };
                Controls.Add(customControl);

                isLocalDrag = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            if(e.Action == DragAction.Cancel)
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
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
            base.OnDragOver(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            isDragging = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Handle the dropped files here

                DropFileMenu.Drop(e);

                MenuManager.Instance.QueueOpenMenu(Resources.Resources.HomeMenu);
            }
            base.OnDragDrop(e);
        }
    }
}
