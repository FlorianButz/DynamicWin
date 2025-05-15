using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    class RegisterSmallVisualizerWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => true;
        public string WidgetName => "Audio Visualiser";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new SmallVisualizerWidget(parent, position, alignment);
        }
    }

    public class SmallVisualizerWidget : SmallWidgetBase
    {
        AudioVisualizer audioVisualizer;

        public SmallVisualizerWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            audioVisualizer = new AudioVisualizer(this, new Vec2(-2.25f, 0), new Vec2(GetWidgetSize().X, GetWidgetSize().Y), UIAlignment.Center, length: 16, 16)
            {
                divisor = 1.75f,
                barDownSmoothing = 10,
                barUpSmoothing = 20
            };
            AddLocalObject(audioVisualizer);
        }

        protected override float GetWidgetWidth()
        {
            return base.GetWidgetWidth() * 2;
        }
    }
}
