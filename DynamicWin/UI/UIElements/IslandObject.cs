using DynamicWin.Main;
using DynamicWin.UI.Menu;
using DynamicWin.Utils;
using SkiaSharp;

namespace DynamicWin.UI.UIElements
{
    public class IslandObject : UIObject
    {
        public float topOffset = 15f;

        public SecondOrder scaleSecondOrder;

        public Vec2 secondOrderValuesExpand = new Vec2(3f, 0.55f);
        public Vec2 secondOrderValuesContract = new Vec2(3f, 0.9f);

        public bool hidden = false;
        
        public Vec2 currSize;

        public enum IslandMode { Island, Notch };
        public IslandMode mode = Settings.IslandMode;

        float dropShadowStrength = 0f;
        float dropShadowSize = 0f;

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
                //Size = Vec2.lerp(Size, currSize, deltaTime * 15f);

                LocalPosition.Y = Mathf.Lerp(LocalPosition.Y, topOffset, 15f * deltaTime);
            }
            else
            {
                scaleSecondOrder.SetValues(secondOrderValuesContract.X, secondOrderValuesContract.Y, 0.1f);

                Size = scaleSecondOrder.Update(deltaTime, new Vec2(500, 15));
                //Size = Vec2.lerp(Size, new Vec2(500, 10), deltaTime * 15f);

                LocalPosition.Y = Mathf.Lerp(LocalPosition.Y, -Size.Y + 15f, 15f * deltaTime);
            }

            MainForm.Instance.Opacity = hidden ? 0.75f : 1f;

            mode = Settings.IslandMode;

            topOffset = Mathf.Lerp(topOffset, (mode == IslandMode.Island) ? 15f : 5f, 15f * deltaTime);

            dropShadowStrength = Mathf.Lerp(dropShadowStrength, IsHovering ? 0.75f : 0.25f, 10f * deltaTime);
            dropShadowSize = Mathf.Lerp(dropShadowSize, IsHovering ? 35f : 7.5f, 10f * deltaTime);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.IsAntialias = Settings.AntiAliasing;

            paint.Color = Theme.IslandBackground.Value();

            if (!hidden)
            {
                paint.ImageFilter = SKImageFilter.CreateDropShadow(1, 1, dropShadowSize, dropShadowSize, new Col(0, 0, 0).Override(a: dropShadowStrength).Value());
            }

            canvas.DrawRoundRect(GetRect(), paint);

            paint.ImageFilter = null;

            if (mode == IslandMode.Notch && !hidden)
            {
                var path = new SKPath();

                var awidth = (float)(Math.Max(Size.Magnitude / 16, 25));
                var aheight = (float)(Math.Max(Size.Y / 4, 15)) + (LocalPosition.Y - topOffset);
                var y = 5;

                { // Left notch curve

                    var x = Position.X - awidth;

                    path.MoveTo(x - awidth, y);
                    path.CubicTo(
                        x + 0, y,
                        x + awidth, y,
                        x + awidth, y + aheight);
                    path.LineTo(x + awidth, y);
                    path.LineTo(x + 0, y);
                }

                { // Right notch curve

                    var x = Position.X + Size.X + awidth;

                    path.MoveTo(x + awidth, y);
                    path.CubicTo(
                        x - 0, y,
                        x - awidth, y,
                        x - awidth, y + aheight);
                    path.LineTo(x - awidth, y);
                    path.LineTo(x - 0, y);
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
