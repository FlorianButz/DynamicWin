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

        Thread changeTextThread;

        private void SetText(string text)
        {
            if (this.text == text) return;

            float ogTextSize = textSize;

            if (changeTextThread != null) return;

            changeTextThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                int length = 150;

                for (int i = 0; i < length; i++)
                {
                    Thread.Sleep(1);

                    float t = Easings.EaseInQuint((float)i / length);

                    textSize = Mathf.Lerp(ogTextSize, ogTextSize * 1.5f, t);
                    localBlurAmount = Mathf.Lerp(0, 5, t);
                }

                this.text = text;

                for (int i = 0; i < length; i++)
                {
                    Thread.Sleep(1);

                    float t = Easings.EaseOutQuint((float)i / length);

                    textSize = Mathf.Lerp(ogTextSize / 1.5f, ogTextSize, t);
                    localBlurAmount = Mathf.Lerp(5, 0, t);
                }

                changeTextThread = null;
            });
            changeTextThread.Start();
        }
    }
}
