using DynamicWin.Main;
using DynamicWin.UI.Menu;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    internal class IslandObject : UIObject
    {
        public float topOffset = 5f;

        public SecondOrder scaleSecondOrder;

        public Vec2 secondOrderValuesExpand = new Vec2(3f, 0.6f);
        public Vec2 secondOrderValuesContract = new Vec2(3f, 0.9f);

        public Vec2 currSize;

        public IslandObject() : base(null, Vec2.zero, new Vec2(250, 50), UIAlignment.TopCenter)
        {
            currSize = Size;

            Anchor = new Vec2(0.5f, 0f);

            roundRadius = 35f;

            LocalPosition = new Vec2(0, 15f);

            scaleSecondOrder = new SecondOrder(Size, secondOrderValuesExpand.X, secondOrderValuesExpand.Y, 0.1f);
            expandInteractionRect = 20;

            maskInToIsland = true;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (IsHovering)
            {
                scaleSecondOrder.SetValues(secondOrderValuesExpand.X, secondOrderValuesExpand.Y, 0.1f);
                currSize = MenuManager.Instance.ActiveMenu.IslandSizeBig();
            }
            else
            {
                scaleSecondOrder.SetValues(secondOrderValuesContract.X, secondOrderValuesContract.Y, 0.1f);
                currSize = MenuManager.Instance.ActiveMenu.IslandSize();
            }

            Size = scaleSecondOrder.Update(deltaTime, currSize);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();

            paint.Color = Theme.IslandBackground.Value();
            canvas.DrawRoundRect(GetRect(), paint);

            paint.IsStroke = true;
            paint.Color = SKColors.Wheat;
            canvas.DrawRoundRect(GetInteractionRect(), paint);
        }

        public override SKRoundRect GetInteractionRect()
        {
            var rect = SKRect.Create(Position.X, Position.Y, Size.X, Size.Y);

            if(IsHovering)
                rect.Inflate(expandInteractionRect + 150, expandInteractionRect + 35);

            rect.Inflate(expandInteractionRect, expandInteractionRect);
            var r = new SKRoundRect(rect, roundRadius);

            return r;
        }

        public override SKRoundRect GetRect()
        {
            var rect = base.GetRect();
            return rect;
        }
    }
}
