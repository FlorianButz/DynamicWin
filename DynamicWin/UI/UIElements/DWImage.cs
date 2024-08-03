using DynamicWin.Resources;
using DynamicWin.Utils;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    internal class DWImage : UIObject
    {
        private SKBitmap image;

        public SKBitmap Image { get { return image; } set => image = value; }

        public bool dynamicColor;
        public bool dynamicAutoColor;

        public DWImage(UIObject? parent, SKBitmap sprite, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter, bool dynamicColor = false) : base(parent, position, size, alignment)
        {
            image = sprite;
            this.dynamicColor = dynamicColor;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if(dynamicAutoColor)
                if(dynamicColor) Color = Theme.TextMain;
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();

            if(dynamicColor)
            {
                var imageFilter = SKImageFilter.CreateBlendMode(SKBlendMode.DstIn,
                    SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(Color.Override(a: 1f).Value(), SKBlendMode.Darken)));

                if (GetBlur() != 0f)
                {
                    var blur = SKImageFilter.CreateBlur(GetBlur(), GetBlur());

                    if (blur != null && imageFilter != null)
                    {
                        var composedFilter = SKImageFilter.CreateCompose(blur, imageFilter);
                        paint.ImageFilter = composedFilter;
                    }
                }
                else
                    paint.ImageFilter = imageFilter;
            }

            canvas.DrawBitmap(image, SKRect.Create(Position.X, Position.Y, Size.X, Size.Y), paint);
        }
    }
}
