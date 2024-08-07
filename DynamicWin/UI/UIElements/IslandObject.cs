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
                Size = scaleSecondOrder.Update(deltaTime, new Vec2(500, 15));

                LocalPosition.Y = Mathf.Lerp(LocalPosition.Y, -Size.Y + 15f, 15f * deltaTime);
            }

            MainForm.Instance.Opacity = hidden ? 0.75f : 1f;

            mode = Settings.IslandMode;

            topOffset = Mathf.Lerp(topOffset, (mode == IslandMode.Island) ? 15f : 5f, 15f * deltaTime);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.IsAntialias = Settings.AntiAliasing;

            paint.Color = Theme.IslandBackground.Value();

            if (!hidden)
            {
                if (!IsHovering)
                    paint.ImageFilter = SKImageFilter.CreateDropShadow(1, 1, 7.5f, 7.5f, new Col(0, 0, 0).Override(a: 0.35f).Value());
                else
                    paint.ImageFilter = SKImageFilter.CreateDropShadow(1, 1, 35, 35, (Theme.IslandBackground * 0.5f).Override(a: 0.85f).Value());
            }

            canvas.DrawRoundRect(GetRect(), paint);

            paint.ImageFilter = null;

            if (mode == IslandMode.Notch && !hidden)
            {
                var path = new SKPath();

                var awidth = (float)(Math.Max(Size.Magnitude / 16, 50));
                var aheight = (float)(Math.Max(Size.Magnitude / 8, 20f)) + (LocalPosition.Y - topOffset);

                { // Left notch curve
                    var y = LocalPosition.Y - topOffset;

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
                    var y = Position.Y - topOffset;

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
