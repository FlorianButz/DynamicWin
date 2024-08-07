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
    public class ErrorForm : Window
    {
        public ErrorForm()
        {
            var result = MessageBox.Show("Only one instance of DynamicWin can run at a time.", "An error occured.");
            Process.GetCurrentProcess().Kill();
        }
    }
}
