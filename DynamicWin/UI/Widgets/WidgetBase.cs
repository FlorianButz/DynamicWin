using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets
{
    internal class WidgetBase : UIObject
    {
        public bool isEditMode = false;

        public WidgetBase(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, Vec2.zero, alignment)
        {
            Size = GetWidgetSize();

            var objs = InitializeWidget();
            objs.ForEach(obj => AddLocalObject(obj));

            roundRadius = 15f;
        }

        public Vec2 GetWidgetSize() { return new Vec2(GetWidgetWidth(), GetWidgetHeight()); }

        protected virtual float GetWidgetHeight() { return 150; }
        protected virtual float GetWidgetWidth() { return 250; }

        public List<UIObject> InitializeWidget()
        {
            return new List<UIObject>();
        }

        public override void Draw(SKCanvas canvas)
        {
            Size = GetWidgetSize();

            DrawWidget(canvas);

            var paint = GetPaint();
            paint.Color = (Theme.Primary * 0.1f).Value();

            canvas.DrawRoundRect(GetRect(), paint);

            if (isEditMode)
            {
                paint.IsStroke = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;
                paint.StrokeWidth = 2f;

                float expand = 10;
                var brect = SKRect.Create(Position.X - expand / 2, Position.Y - expand/2, Size.X + expand, Size.Y + expand);
                var broundRect = new SKRoundRect(brect, roundRadius);

                int noClip = canvas.Save();

                //if(!RendererMain.Instance.MainIsland.IsHovering)
                //    canvas.ClipRect(clipRect, SKClipOperation.Difference);

                paint.Color = SKColors.DimGray;
                canvas.DrawRoundRect(broundRect, paint);

                canvas.RestoreToCount(noClip);
            }
        }

        public virtual void DrawWidget(SKCanvas canvas) { }
    }
}
