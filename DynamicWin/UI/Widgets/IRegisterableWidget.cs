using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets
{
    public interface IRegisterableWidget
    {
        public bool IsSmallWidget { get; }
        public void RegisterWidget(out string widgetName);
        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter);
    }
}
