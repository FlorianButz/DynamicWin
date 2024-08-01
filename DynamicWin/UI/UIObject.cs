using DynamicWin.Main;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicWin.UI
{
    internal class UIObject
    {
        private UIObject? parent;

        private Vec2 position = Vec2.zero;
        private Vec2 localPosition = Vec2.zero;
        private Vec2 anchor = new Vec2(0.5f, 0.5f);
        private Vec2 size = Vec2.one;
        private Col color = Col.White;

        public Vec2 RawPosition { get => position + localPosition; }
        public Vec2 Position { get => GetPosition() + localPosition; set => position = value; }
        public Vec2 LocalPosition { get => localPosition; set => localPosition = value; }
        public Vec2 Anchor { get => anchor; set => anchor = value; }
        public Vec2 Size { get => size; set => size = value; }
        public Col Color { get => color; set => color = value; }

        private bool isHovering = false;
        private bool isMouseDown = false;
        public bool IsHovering { get => isHovering; private set => isHovering = value; }
        public bool IsMouseDown { get => isMouseDown; private set => isMouseDown = value; }

        public UIAlignment alignment = UIAlignment.TopCenter;

        protected float localBlurAmount = 0f;
        public float blurAmount = 0f;
        public float roundRadius = 0f;
        public bool maskInToIsland = true;

        private List<UIObject> localObjects = new List<UIObject>();

        private bool isEnabled = true;

        protected void AddLocalObject(UIObject obj)
        {
            obj.parent = this;
            localObjects.Add(obj);
        }

        protected void DestroyLocalObject(UIObject obj)
        {
            obj.DestroyCall();
            localObjects.Remove(obj);
        }

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
                Vec2 parentPos = parent.Position;

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

        public float GetBlur()
        {
            return Math.Max(blurAmount, localBlurAmount);
        }

        public void UpdateCall(float deltaTime)
        {
            if (!isEnabled) return;

            var rect = SKRect.Create(RendererMain.CursorPosition.X, RendererMain.CursorPosition.Y, 1, 1);
            isHovering = GetInteractionRect().Contains(rect);

            if(IsHovering && !IsMouseDown && Control.MouseButtons.HasFlag(MouseButtons.Left))
            {
                IsMouseDown = true;
                OnMouseDown();
            }else if (IsMouseDown && !Control.MouseButtons.HasFlag(MouseButtons.Left))
            {
                IsMouseDown = false;
                OnMouseUp();
            }

            Update(deltaTime);

            localObjects.ForEach((UIObject obj) =>
            {
                obj.blurAmount = GetBlur();
                obj.UpdateCall(deltaTime);
            });
        }

        public virtual void Update(float deltaTime) { }

        public void DrawCall(SKCanvas canvas)
        {
            if (!isEnabled) return;

            Draw(canvas);

            localObjects.ForEach((UIObject obj) =>
            {
                obj.DrawCall(canvas);
            });
        }

        public virtual void Draw(SKCanvas canvas)
        {
            var rect = SKRect.Create(Position.X, Position.Y, Size.X, Size.Y);
            var roundRect = new SKRoundRect(rect, roundRadius);

            var paint = GetPaint();

            canvas.DrawRoundRect(roundRect, paint);
        }

        protected virtual SKPaint GetPaint()
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = Color.Value(),
                IsAntialias = true,
                IsDither = true,
                FilterQuality = SKFilterQuality.High
            };

            if(GetBlur() != 0f)
            {
                var blur = SKImageFilter.CreateBlur(GetBlur(), GetBlur());
                paint.ImageFilter = blur;
            }

            return paint;
        }

        public void DestroyCall() 
        {
            localObjects.ForEach((UIObject obj) =>
            {
                obj.DestroyCall();
            });

            OnDestroy();
        }

        public virtual void OnDestroy() { }

        public virtual void OnMouseDown() { }

        public virtual void OnMouseUp() { }

        Thread toggleThread;

        public void SilentSetActive(bool isEnabled)
        {
            this.isEnabled = isEnabled;
        }

        public void SetActive(bool isEnabled)
        {
            if (toggleThread != null && toggleThread.ThreadState == ThreadState.Background) toggleThread.Interrupt();

            toggleThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                int length = 175;

                for (int i = 0; i < length; i++)
                {
                    try
                    {
                        Thread.Sleep(1);
                    }catch(ThreadInterruptedException e)
                    {
                        this.isEnabled = isEnabled;
                        return;
                    }


                    if (isEnabled)
                    {
                        float t = Mathf.LimitDecimalPoints(Easings.EaseOutQuint((float)i / length), 1);
                        localBlurAmount = Mathf.Lerp(25, 0, t);
                    }
                    else
                    {
                        float t = Mathf.LimitDecimalPoints(Easings.EaseInQuint((float)i / length), 1);
                        localBlurAmount = Mathf.Lerp(0, 25, t);
                    }
                }

                this.isEnabled = isEnabled;
            });
            toggleThread.Start();
        }

        public virtual SKRoundRect GetRect()
        {
            var rect = SKRect.Create((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            return new SKRoundRect(rect, roundRadius);
        }

        protected int expandInteractionRect = 5;

        public virtual SKRoundRect GetInteractionRect()
        {
            var rect = SKRect.Create((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            var r = new SKRoundRect(rect, roundRadius);
            r.Deflate(-expandInteractionRect, -expandInteractionRect);
            return r;
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