using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace DynamicWin
{

    public class MyCustomControl : SKControl
    {
        public MyCustomControl()
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = System.Drawing.Color.Transparent;
        }

        float timer = 0;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            timer += 0.05f;

            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.Color = SKColors.Blue;
                paint.StrokeWidth = 5;

                // Draw a simple line
                canvas.DrawLine(0, 0, this.Width, this.Height, paint);

                // Example of blurring
                var blurEffect = SKImageFilter.CreateBlur(25.0f, 5.0f);
                paint.ImageFilter = blurEffect;

                // Draw a blurred rectangle
                canvas.DrawRect(50, 50 + (float)Math.Sin(timer) * 25, 200, 200, paint);
            }
        }
    }

}
