using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DynamicWin.Main
{
    internal class ErrorForm : Window
    {
        public ErrorForm()
        {
            var result = MessageBox.Show("Only one instance can run at a time.");
            Process.GetCurrentProcess().Kill();
        }
    }
}
