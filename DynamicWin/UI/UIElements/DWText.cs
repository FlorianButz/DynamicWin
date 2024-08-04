using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    internal class DWText : UIObject
    {
        private string text = "";
        public string Text { get { return text; } set { SetText(value); } }

        public float textSize = 24;

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
            font = Resources.Resources.InterRegular;
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.TextSize = textSize;
            paint.Typeface = font;

            // Measure the width of the text
            Size.X = paint.MeasureText(text);

            // Measure the height of the text
            var fontMetrics = paint.FontMetrics;
            Size.Y = fontMetrics.Descent + fontMetrics.Ascent;

            textBounds = new Vec2(paint.MeasureText(text), fontMetrics.Descent + fontMetrics.Ascent);

            canvas.DrawText(text, new SKPoint(Position.X, Position.Y), paint);
        }

        Animator changeTextAnim;

        private void SetText(string text)
        {
            if (this.text == text) return;
            if (changeTextAnim != null && changeTextAnim.IsRunning) return;

            float ogTextSize = textSize;

            changeTextAnim = new Animator(450, 1);

            changeTextAnim.onAnimationUpdate += (x) =>
            {
                if(x <= 0.5f)
                {
                    float t = Easings.EaseInQuint(x * 2);

                    textSize = Mathf.Lerp(ogTextSize, ogTextSize * 1.5f, t);
                    localBlurAmount = Mathf.Lerp(0, 10, t);
                    alpha = Mathf.Lerp(1, 0, x);
                }
                else
                {
                    this.text = text;

                    float t = Easings.EaseOutQuint((x - 0.5f) * 2);

                    textSize = Mathf.Lerp(ogTextSize / 2.5f, ogTextSize, t);
                    localBlurAmount = Mathf.Lerp(10, 0, t);
                    alpha = Mathf.Lerp(0, 1, x);
                }
            };

            changeTextAnim.Start();
            changeTextAnim.onAnimationEnd += () =>
            {
                this.text = text;
                textSize = ogTextSize;
            };
        }
    }
}
