using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements.Custom
{
    internal class TrayFile : UIObject
    {
        string file;

        public TrayFile(UIObject? parent, string file, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter):
            base(parent, position, new Vec2(60, 75), alignment)
        {
            this.file = file;
        }
    }
}
