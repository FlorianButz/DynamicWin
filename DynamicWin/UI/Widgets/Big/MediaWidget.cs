using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Big
{
    internal class MediaWidget : WidgetBase
    {
        public MediaWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
        }

        protected override float GetWidgetWidth()
        {
            return 400;
        }
    }
}
