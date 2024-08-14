using DynamicWin.Main;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    class RegisterTimeWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => true;

        public string WidgetName => "Time Display";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new TimeWidget(parent, position, alignment);
        }
    }

    public class TimeWidget : SmallWidgetBase
    {
        DWText timeText;

        public TimeWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            timeText = new DWText(this, GetTime(), Vec2.zero, UIAlignment.Center);
            timeText.TextSize = 14;
            AddLocalObject(timeText);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            timeText.Text = GetTime();
        }

        protected override float GetWidgetWidth() { return Settings.MilitaryTime ? 35 : 50; }

        string GetTime()
        {
            return Settings.MilitaryTime ? DateTime.Now.ToString("HH:mm") : DateTime.Now.ToString("hh:mm tt");
        }
    }
}
