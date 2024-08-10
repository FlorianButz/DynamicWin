using DynamicWin.UI.UIElements;
using DynamicWin.UI.Widgets.Big;
using DynamicWin.Utils;
using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DynamicWin.UI.Widgets.Small
{
    class RegisterSystemUsageWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => true;

        public string WidgetName => "System Usage Display";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new SystemUsageWidget(parent, position, alignment);
        }
    }

    public class SystemUsageWidget : SmallWidgetBase
    {
        DWText text;

        Computer computer;

        public SystemUsageWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            text = new DWText(this, GetUsage(), Vec2.zero, UIAlignment.Center);
            text.TextSize = 12;
            text.Color = Theme.TextSecond;
            AddLocalObject(text);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            updateCycle += deltaTime;

            if(updateCycle > 1.5f)
            {
                text.SilentSetText(GetUsage());
                updateCycle = 0f;
            }
        }

        float updateCycle = 0f;

        string GetUsage()
        {
            return HardwareMonitor.usageString;
        }

        protected override float GetWidgetWidth()
        {
            return Math.Max(225f, text != null ? text.TextBounds.X : 10 - 10);
        }
    }
}
