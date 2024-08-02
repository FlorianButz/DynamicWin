using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    internal class TimeWidget : SmallWidgetBase
    {
        DWText timeText;

        public TimeWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            timeText = new DWText(this, GetTime(), Vec2.zero, UIAlignment.Center);
            timeText.textSize = 14;
            AddLocalObject(timeText);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            timeText.Text = GetTime();
        }

        string GetTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }
    }
}
