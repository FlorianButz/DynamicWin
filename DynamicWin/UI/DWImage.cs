using DynamicWin.Resources;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI
{
    internal class DWImage : UIObject
    {
        private SkiaSharp.SKBitmap image;

        public DWImage(UIObject? parent, SkiaSharp.SKBitmap sprite, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            this.image = sprite;
        }

        public override void Draw(SKCanvas canvas)
        {
            canvas.DrawBitmap(image, SKRect.Create(Position.X, Position.Y, Size.X, Size.Y), GetPaint());
        }
    }
}
