using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI
{
    internal class DWImageButton : DWButton
    {
        DWImage image;

        public DWImage Image { get { return image; } private set => image = value; }

        public DWImageButton(UIObject? parent, SkiaSharp.SKBitmap sprite, Vec2 position, Vec2 size, Action clickCallback, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, clickCallback, alignment)
        {
            image = new DWImage(this, sprite, Vec2.zero, size * 0.85f, UIAlignment.Center);
            AddLocalObject(image);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Image.Size = Size * 0.85f;
        }
    }
}
