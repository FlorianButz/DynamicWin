using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements.Custom
{
    public class DropFileElement : UIObject
    {
        public DropFileElement(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            roundRadius = 25;

            AddLocalObject(new DWText(null, "Drop Files to Tray", Vec2.zero, UIAlignment.Center) { Font = Resources.Res.InterBold });
        }

        Col currentCol = Theme.Secondary;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            currentCol = Col.Lerp(currentCol, IsHovering ? Theme.Secondary * 2f : Theme.Secondary, 5f * deltaTime);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            var rect = GetRect();

            float[] intervals = { 10, 10 };
            paint.PathEffect = SKPathEffect.CreateDash(intervals, 0f);

            paint.IsStroke = true;
            paint.StrokeCap = SKStrokeCap.Round;
            paint.StrokeJoin = SKStrokeJoin.Round;
            paint.StrokeWidth = 2f;

            paint.Color = GetColor(Theme.Primary).Value();

            canvas.DrawRoundRect(rect, paint);

            paint.Color = GetColor(currentCol).Value();
            paint.IsStroke = false;

            rect.Deflate(10, 10);

            canvas.DrawRoundRect(rect, paint);
        }
    }
}
