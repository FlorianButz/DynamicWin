using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets
{
    internal class TestWidget : WidgetBase
    {
        public TestWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
        }

        public override void DrawWidget(SKCanvas canvas)
        {
            base.DrawWidget(canvas);

            var paint = GetPaint();
            var rect = GetRect();

            float[] intervals = { 10, 10 };
            paint.PathEffect = SKPathEffect.CreateDash(intervals, 0f);

            paint.IsStroke = true;
            paint.StrokeCap = SKStrokeCap.Round;
            paint.StrokeJoin = SKStrokeJoin.Round;
            paint.StrokeWidth = 2f;

            paint.Color = Theme.Primary.Value();

            canvas.DrawRoundRect(rect, paint);

            paint.Color = Theme.Secondary.Value();
            paint.IsStroke = false;

            rect.Deflate(10, 10);

            canvas.DrawRoundRect(rect, paint);
        }
    }
}
