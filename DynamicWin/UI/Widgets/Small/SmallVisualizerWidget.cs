using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    internal class SmallVisualizerWidget : SmallWidgetBase
    {
        AudioVisualizer audioVisualizer;

        public SmallVisualizerWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            audioVisualizer = new AudioVisualizer(this, Vec2.zero, new Vec2(GetWidgetSize().X, GetWidgetSize().Y), UIAlignment.Center, fftLength: 16)
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
