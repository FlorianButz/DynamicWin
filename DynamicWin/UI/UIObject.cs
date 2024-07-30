using DynamicWin.Main;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI
{
    internal abstract class UIObject
    {
        private UIObject? parent;

        private Vec2 position;
        private Vec2 localPosition;
        private Vec2 anchor = new Vec2(0.5f, 0.5f);
        private Vec2 size;
        private Col color = Col.White;

        public Vec2 Position { get => GetPosition() + localPosition; set => position = value; }
        public Vec2 LocalPosition { get => localPosition; set => localPosition = value; }
        public Vec2 Anchor { get => anchor; set => anchor = value; }
        public Vec2 Size { get => size; set => size = value; }
        public Col Color { get => color; set => color = value; }

        private bool isHovering = false;
        public bool IsHovering { get => isHovering; private set => isHovering = value; }

        public UIAlignment alignment = UIAlignment.TopCenter;

        public float blurAmount = 0f;
        public float roundRadius = 0f;
        public bool maskInToIsland = true;

        public UIObject(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter)
        {
            this.parent = parent;
            this.position = position;
            this.size = size;
            this.alignment = alignment;
        }

        protected virtual Vec2 GetPosition()
        {
            if(parent == null)
            {
                Vec2 screenDim = RendererMain.ScreenDimensions;
                switch (alignment)
                {
                    case UIAlignment.TopLeft:
                        return new Vec2(position.X - (Size.X * Anchor.X),
                            position.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.TopCenter:
                        return new Vec2(position.X + screenDim.X / 2 - (Size.X * Anchor.X),
                            position.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.TopRight:
                        return new Vec2(position.X + screenDim.X - (Size.X * Anchor.X),
                            position.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.MiddleLeft:
                        return new Vec2(position.X - (Size.X * Anchor.X),
                            position.Y + screenDim.Y / 2 - (Size.Y * Anchor.Y));
                    case UIAlignment.Center:
                        return new Vec2(position.X + screenDim.X / 2 - (Size.X * Anchor.X),
                            position.Y + screenDim.Y / 2 - (Size.Y * Anchor.Y));
                    case UIAlignment.MiddleRight:
                        return new Vec2(position.X + screenDim.X - (Size.X * Anchor.X),
                            position.Y + screenDim.Y / 2 - (Size.Y * Anchor.Y));
                    case UIAlignment.BottomLeft:
                        return new Vec2(position.X - (Size.X * Anchor.X),
                            position.Y + screenDim.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.BottomCenter:
                        return new Vec2(position.X + screenDim.X / 2 - (Size.X * Anchor.X),
                            position.Y + screenDim.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.BottomRight:
                        return new Vec2(position.X + screenDim.X - (Size.X * Anchor.X),
                            position.Y + screenDim.Y - (Size.Y * Anchor.Y));
                }
            }
            else
            {
                Vec2 parentDim = parent.Size;
                Vec2 parentPos = parent.GetPosition();

                switch (alignment)
                {
                    case UIAlignment.TopLeft:
                        return new Vec2(parentPos.X + position.X - (Size.X * Anchor.X),
                            parentPos.Y + position.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.TopCenter:
                        return new Vec2(parentPos.X + position.X + parentDim.X / 2 - (Size.X * Anchor.X),
                            parentPos.Y + position.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.TopRight:
                        return new Vec2(parentPos.X + position.X + parentDim.X - (Size.X * Anchor.X),
                            parentPos.Y + position.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.MiddleLeft:
                        return new Vec2(parentPos.X + position.X - (Size.X * Anchor.X),
                            parentPos.Y + position.Y + parentDim.Y / 2 - (Size.Y * Anchor.Y));
                    case UIAlignment.Center:
                        return new Vec2(parentPos.X + position.X + parentDim.X / 2 - (Size.X * Anchor.X),
                            parentPos.Y + position.Y + parentDim.Y / 2 - (Size.Y * Anchor.Y));
                    case UIAlignment.MiddleRight:
                        return new Vec2(parentPos.X + position.X + parentDim.X - (Size.X * Anchor.X),
                            parentPos.Y + position.Y + parentDim.Y / 2 - (Size.Y * Anchor.Y));
                    case UIAlignment.BottomLeft:
                        return new Vec2(parentPos.X + position.X - (Size.X * Anchor.X),
                            parentPos.Y + position.Y + parentDim.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.BottomCenter:
                        return new Vec2(parentPos.X + position.X + parentDim.X / 2 - (Size.X * Anchor.X),
                            parentPos.Y + position.Y + parentDim.Y - (Size.Y * Anchor.Y));
                    case UIAlignment.BottomRight:
                        return new Vec2(parentPos.X + position.X + parentDim.X - (Size.X * Anchor.X),
                            parentPos.Y + position.Y + parentDim.Y - (Size.Y * Anchor.Y));
                }
            }

            return Vec2.zero;
        }

        public virtual void Update(float deltaTime)
        {
            var rect = SKRect.Create(RendererMain.CursorPosition.X, RendererMain.CursorPosition.Y, 1, 1);
            isHovering = GetRect().Contains(rect);
        }

        public virtual void Draw(SKCanvas canvas)
        {
            var rect = SKRect.Create(Position.X, Position.Y, Size.X, Size.Y);

            var paint = GetPaint();

            canvas.DrawRect(rect, paint);
        }

        protected virtual SKPaint GetPaint()
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = Color.Value(),
                IsAntialias = true,
                IsDither = true
            };

            if(blurAmount != 0f)
            {
                var blur = SKImageFilter.CreateBlur(blurAmount, blurAmount);
                paint.ImageFilter = blur;
            }

            return paint;
        }

        public virtual SKRoundRect GetRect()
        {
            var rect = SKRect.Create((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            return new SKRoundRect(rect, roundRadius);
        }
    }

    public enum UIAlignment
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}