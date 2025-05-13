using DynamicWin.Main;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicWin.UI
{
    public class UIObject
    {
        private UIObject? parent;
        public UIObject? Parent { get { return parent; } set { parent = value; } }

        private Vec2 position = Vec2.zero;
        private Vec2 localPosition = Vec2.zero;
        private Vec2 anchor = new Vec2(0.5f, 0.5f);
        private Vec2 size = Vec2.one;
        private Col color = Col.White;

        public Vec2 RawPosition { get => position; }
        public Vec2 Position { get => GetPosition() + localPosition; set => position = value; }
        public Vec2 LocalPosition { get => localPosition; set => localPosition = value; }
        public Vec2 Anchor { get => anchor; set => anchor = value; }

        // TODO: look further into this null pointer issue
        public Vec2 Size { get => size ?? Vec2.one; set => size = value; } // Temporary fix for null size especially with BottomLeft getter
        public Col Color { get => new Col(color.r, color.g, color.b, color.a * Alpha); set => color = value; }

        private bool isHovering = false;
        private bool isMouseDown = false;
        private bool isGlobalMouseDown = false;
        protected bool drawLocalObjects = true;

        public bool IsHovering { get => isHovering; private set => isHovering = value; }
        public bool IsMouseDown { get => isMouseDown; private set => isMouseDown = value; }

        public UIAlignment alignment = UIAlignment.TopCenter;

        protected float localBlurAmount = 0f;
        public float blurAmount = 0f;
        public float roundRadius = 0f;
        public bool maskInToIsland = true;

        private List<UIObject> localObjects = new List<UIObject>();
        public List<UIObject> LocalObjects { get => localObjects; }

        private bool isEnabled = true;
        public bool IsEnabled { get => isEnabled; set => SetActive(value); }

        public float blurSizeOnDisable = 50;

        private float pAlpha = 1f;
        private float oAlpha = 1f;

        public float Alpha { get => (float) Math.Min(pAlpha, Math.Min(oAlpha, RendererMain.Instance.alphaOverride)); set => oAlpha = value; }

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

            this.contextMenu = CreateContextMenu();

            RendererMain.Instance.ContextMenuOpening += CtxOpen;
            RendererMain.Instance.ContextMenuClosing += CtxClose;
        }

        void CtxOpen(object sender, RoutedEventArgs e)
        {
            if(RendererMain.Instance.ContextMenu != null)
                canInteract = false;
        }

        void CtxClose(object sender, RoutedEventArgs e)
        {
            canInteract = true;
        }

        public Vec2 GetScreenPosFromRawPosition(Vec2 position, Vec2 Size = null, UIAlignment alignment = UIAlignment.None, UIObject parent = null)
        {
            if (parent == null) parent = this.parent;
            if (Size == null) Size = this.Size;
            if (alignment == UIAlignment.None) alignment = this.alignment;

            if (parent == null)
            {
                Vec2 screenDim = RendererMain.ScreenDimensions;
                if (Size == null) Size = Vec2.one;
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
            if (!Settings.AllowBlur) return 0f;
            return Math.Max(blurAmount, Math.Max(localBlurAmount, Math.Max((parent == null) ? 0f : parent.GetBlur(), RendererMain.Instance.blurOverride)));
        }

        bool canInteract = true;
        
        public void UpdateCall(float deltaTime)
        {
            if (!isEnabled) return;

            if (parent != null)
                pAlpha = parent.Alpha;

            if (canInteract)
            {
                var rect = SKRect.Create(RendererMain.CursorPosition.X, RendererMain.CursorPosition.Y, 1, 1);
                isHovering = GetInteractionRect().Contains(rect);

                if (!isGlobalMouseDown && Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    isGlobalMouseDown = true;
                    OnGlobalMouseDown();
                }
                else if (isGlobalMouseDown && !(Mouse.LeftButton == MouseButtonState.Pressed))
                {
                    isGlobalMouseDown = false;
                    OnGlobalMouseUp();
                }

                if (IsHovering && !IsMouseDown && Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    IsMouseDown = true;
                    OnMouseDown();
                }
                else if (IsHovering && IsMouseDown && !(Mouse.LeftButton == MouseButtonState.Pressed))
                {
                    IsMouseDown = false;
                    OnMouseUp();
                }
                else if (IsMouseDown && !(Mouse.LeftButton == MouseButtonState.Pressed))
                {
                    IsMouseDown = false;
                }
            }

            Update(deltaTime);

            if (drawLocalObjects)
            {
                new List<UIObject>(localObjects).ForEach((UIObject obj) =>
                {
                    obj.blurAmount = GetBlur();
                    obj.UpdateCall(deltaTime);
                });
            }
        }

        public virtual void Update(float deltaTime) { }

        public void DrawCall(SKCanvas canvas)
        {
            if (!isEnabled) return;

            Draw(canvas);

            if (drawLocalObjects)
            {
                new List<UIObject>(localObjects).ForEach((UIObject obj) =>
                {
                    obj.DrawCall(canvas);
                });
            }
        }

        public virtual void Draw(SKCanvas canvas)
        {
            var rect = SKRect.Create(Position.X, Position.Y, Size.X, Size.Y);
            var roundRect = new SKRoundRect(rect, roundRadius);

            var paint = GetPaint();

            canvas.DrawRoundRect(roundRect, paint);
        }

        public virtual SKPaint GetPaint()
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = this.Color.Value(),
                IsAntialias = Settings.AntiAliasing,
                IsDither = true,
                SubpixelText = false,
                FilterQuality = SKFilterQuality.Medium,
                HintingLevel = SKPaintHinting.Normal,
                IsLinearText = true
            };

            if(GetBlur() != 0f)
            {
                var blur = SKImageFilter.CreateBlur(GetBlur(), GetBlur());
                paint.ImageFilter = blur;
            }

            return paint;
        }

        public Col GetColor(Col col)
        {
            return new Col(col.r, col.g, col.b, col.a * Alpha);
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
        public virtual void OnGlobalMouseDown() { }

        public virtual void OnMouseUp() { }
        public virtual void OnGlobalMouseUp() { }

        public void SilentSetActive(bool isEnabled)
        {
            this.isEnabled = isEnabled;
        }

        Animator toggleAnim;
        bool lastSetActiveCall = true;

        public void SetActive(bool isEnabled)
        {
            if(this.isEnabled == isEnabled && lastSetActiveCall == isEnabled) return;
            if (toggleAnim != null && toggleAnim.IsRunning) toggleAnim.Stop();

            lastSetActiveCall = isEnabled;

            if (isEnabled)
            {
                localBlurAmount = blurSizeOnDisable;
                Alpha = 0f;
                this.isEnabled = isEnabled;
            }

            toggleAnim = new Animator(250, 1);
            toggleAnim.onAnimationUpdate += (t) =>
            {
                if(t >= 0.5f) this.isEnabled = isEnabled;

                if (isEnabled)
                {
                    var tEased = Easings.EaseOutCubic(t);

                    localBlurAmount = Mathf.Lerp(blurSizeOnDisable, 0, tEased);
                    Alpha = Mathf.Lerp(0, 1, tEased);
                }
                else
                {
                    var tEased = Easings.EaseOutCubic(t);

                    localBlurAmount = Mathf.Lerp(0, blurSizeOnDisable, tEased);
                    Alpha = Mathf.Lerp(1, 0, tEased);
                }
            };
            toggleAnim.onAnimationEnd += () =>
            {
                this.isEnabled = isEnabled;
                localBlurAmount = 0f;
                Alpha = 1f;
                DestroyLocalObject(toggleAnim);
            };

            AddLocalObject(toggleAnim);
            toggleAnim.Start();
        }

        public virtual SKRoundRect GetRect()
        {
            var rect = SKRect.Create((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            return new SKRoundRect(rect, roundRadius);
        }

        public int expandInteractionRect = 5;

        public virtual SKRoundRect GetInteractionRect()
        {
            var rect = SKRect.Create((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            var r = new SKRoundRect(rect, roundRadius);
            r.Deflate(-expandInteractionRect, -expandInteractionRect);
            return r;
        }

        ContextMenu? contextMenu = null;

        public virtual ContextMenu? CreateContextMenu() { return null; }
        public virtual ContextMenu? GetContextMenu() { return contextMenu; }
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
        BottomRight,
        None
    }
}