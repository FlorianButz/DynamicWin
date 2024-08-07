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
    public class DWImage : UIObject
    {
        private SKBitmap image;

        public SKBitmap Image { get { return image; } set => image = value; }

        public bool maskOwnRect = false;
        public bool allowIconThemeColor = true;

        public DWImage(UIObject? parent, SKBitmap sprite, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter, bool maskOwnRect = false) : base(parent, position, size, alignment)
        {
            image = sprite;
            this.maskOwnRect = maskOwnRect;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Color = Theme.IconColor;
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();

            if (allowIconThemeColor)
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

            if (maskOwnRect)
            {
                int save = canvas.Save();
                canvas.ClipRoundRect(GetRect(), antialias: true);

                canvas.DrawBitmap(image, SKRect.Create(Position.X, Position.Y, Size.X, Size.Y), paint);
                canvas.RestoreToCount(save);
            }
            else
            {
                canvas.DrawBitmap(image, SKRect.Create(Position.X, Position.Y, Size.X, Size.Y), paint);
            }
        }
    }
}
