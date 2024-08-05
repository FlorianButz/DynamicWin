using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    internal class DWImageButton : DWButton
    {
        DWImage image;
        public float imageScale = 0.85f;

        public DWImage Image { get { return image; } set => image = value; }

        public DWImageButton(UIObject? parent, SKBitmap sprite, Vec2 position, Vec2 size, Action clickCallback, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, clickCallback, alignment)
        {

            image = new DWImage(this, sprite, Vec2.zero, size * imageScale, UIAlignment.Center);
            AddLocalObject(image);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Image.Size = Size * imageScale;
        }
    }
}
