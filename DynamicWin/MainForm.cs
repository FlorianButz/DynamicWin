using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin
{
    public class MainForm : Form
    {
        private System.Windows.Forms.Timer timer;

        public MainForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.TransparencyKey = this.BackColor;
            this.ShowInTaskbar = false;

            var customControl = new MyCustomControl
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(customControl);

            // Set up the timer
            timer = new System.Windows.Forms.Timer
            {
                Interval = 16 // Approximately 60 fps
            };
            timer.Tick += (sender, args) => customControl.Invalidate();
            timer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = System.Drawing.Color.Transparent;
        }
    }
}
