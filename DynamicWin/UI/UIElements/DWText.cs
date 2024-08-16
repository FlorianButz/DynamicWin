using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    public class DWText : UIObject
    {
        private string text = "";
        public string Text { get { return text; } set { SetText(value); } }

        private float textSize = 24;
        public float TextSize { get => textSize; set => textSize = value; }

        private Vec2 textBounds;

        private SKTypeface font;
        public SKTypeface Font { get => font; set => font = value; }

        public Vec2 TextBounds
        {
            get
            {
                if (textBounds == null) return Vec2.zero;
                return textBounds;
            }
        }

        public DWText(UIObject? parent, string text, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, Vec2.zero, alignment)
        {
            this.text = text;
            Color = Theme.TextMain;
            font = Resources.Res.InterRegular;
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.Color = Color.Value();
            paint.TextSize = textSize;
            paint.Typeface = font;

            // Measure the width of the text
            Size.X = paint.MeasureText(text);

            // Measure the height of the text
            var fontMetrics = paint.FontMetrics;
            Size.Y = fontMetrics.Descent + fontMetrics.Ascent;

            SKTextBlob blob = SKTextBlob.Create(text, new SKFont(paint.Typeface, textSize));

            if (blob != null)
            {
                canvas.DrawText(blob, Position.X, Position.Y, paint);
                textBounds = new Vec2(blob.Bounds.Width, blob.Bounds.Height);
            }

            Size = textBounds;

            //canvas.DrawRoundRect(GetRect(), paint);
        }

        public Vec2 GetBoundsForString(string text)
        {
            SKTextBlob blob = SKTextBlob.Create(text, new SKFont(Font, textSize));

            return new Vec2(blob.Bounds.Width, blob.Bounds.Height);
        }

        Animator changeTextAnim;

        public void SilentSetText(string text)
        {
            this.text = text;
        }

        public void SetText(string text)
        {
            if (this.text == text) return;
            if (changeTextAnim != null && changeTextAnim.IsRunning) return;

            float ogTextSize = textSize;

            changeTextAnim = new Animator(350, 1);

            changeTextAnim.onAnimationUpdate += (x) =>
            {
                if(x <= 0.5f)
                {
                    float t = Easings.EaseInQuint(x * 2);

                    textSize = Mathf.Lerp(ogTextSize, ogTextSize / 1.5f, t);
                    localBlurAmount = Mathf.Lerp(0, 10, t);
                    Alpha = Mathf.Lerp(1, 0, x);
                }
                else
                {
                    this.text = text;

                    float t = Easings.EaseOutQuint((x - 0.5f) * 2);

                    textSize = Mathf.Lerp(ogTextSize / 2.5f, ogTextSize, t);
                    localBlurAmount = Mathf.Lerp(10, 0, t);
                    Alpha = Mathf.Lerp(0, 1, x);
                }
            };

            AddLocalObject(changeTextAnim);
            changeTextAnim.Start();
            changeTextAnim.onAnimationEnd += () =>
            {
                this.text = text;
                textSize = ogTextSize;
                DestroyLocalObject(changeTextAnim);
            };
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";
        }
    }
}
