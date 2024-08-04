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
        public float topOffset = 15f;

        public SecondOrder scaleSecondOrder;

        public Vec2 secondOrderValuesExpand = new Vec2(3f, 0.6f);
        public Vec2 secondOrderValuesContract = new Vec2(3f, 0.9f);

        public bool hidden = false;
        
        public Vec2 currSize;

        public enum IslandMode { Island, Notch };
        public IslandMode mode;

        public IslandObject() : base(null, Vec2.zero, new Vec2(250, 50), UIAlignment.TopCenter)
        {
            currSize = Size;

            Anchor = new Vec2(0.5f, 0f);

            roundRadius = 35f;

            LocalPosition = new Vec2(0, topOffset);

            scaleSecondOrder = new SecondOrder(Size, secondOrderValuesExpand.X, secondOrderValuesExpand.Y, 0.1f);
            expandInteractionRect = 20;

            maskInToIsland = false;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (!hidden)
            {
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
                LocalPosition.Y = Mathf.Lerp(LocalPosition.Y, topOffset, 15f * deltaTime);
            }
            else
            {
                scaleSecondOrder.SetValues(secondOrderValuesContract.X, secondOrderValuesContract.Y, 0.1f);
                Size = scaleSecondOrder.Update(deltaTime, new Vec2(500, 25));

                LocalPosition.Y = Mathf.Lerp(LocalPosition.Y, -Size.Y + 15f, 15f * deltaTime);
            }
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.IsAntialias = false;

            paint.Color = Theme.IslandBackground.Value();
            canvas.DrawRoundRect(GetRect(), paint);

            if(mode == IslandMode.Notch)
            {
                var path = new SKPath();

                var awidth = (float)(Math.Max(Size.Magnitude / 12, 50));
                var aheight = (float)(Math.Max(Size.Magnitude / 6, 25));

                { // Left
                    var x = Position.X - awidth;

                    path.MoveTo(x - awidth, 0);
                    path.CubicTo(
                        x + 0, 0,
                        x + awidth, 0,
                        x + awidth, aheight);
                    path.LineTo(x + awidth, 0);
                    path.LineTo(x + 0, 0);
                }

                { // Right
                    var x = Position.X + Size.X + awidth;

                    path.MoveTo(x + awidth, 0);
                    path.CubicTo(
                        x - 0, 0,
                        x - awidth, 0,
                        x - awidth, aheight);
                    path.LineTo(x - awidth, 0);
                    path.LineTo(x - 0, 0);
                }

                var r = SKRect.Create(Position.X, 0, Size.X, (Position.Y - topOffset) + topOffset + Size.Y / 2);
                path.AddRect(r);

                canvas.DrawPath(path, paint);
            }
        }

        public override SKRoundRect GetInteractionRect()
        {
            var rect = SKRect.Create(Position.X, Position.Y, Size.X, Size.Y);

            if(IsHovering)
                rect.Inflate(expandInteractionRect + 5, expandInteractionRect + 5);

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
