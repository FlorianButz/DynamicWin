using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    internal class DWButton : UIObject
    {
        // Button Color

        public Col normalColor = new Col(1f, 1f, 1f, 0.25f);
        public Col hoverColor = new Col(1f, 1f, 1f, 0.45f);
        public Col clickColor = new Col(1f, 1f, 1f, 0.15f);

        public float colorSmoothingSpeed = 15f;

        // Button Size

        protected Vec2 initialScale;

        public Vec2 normalScaleMulti = Vec2.one * 1f;
        public Vec2 hoverScaleMulti = Vec2.one * 1.05f;
        public Vec2 clickScaleMulti = Vec2.one * 1f - 0.05f;

        public SecondOrder scaleSecondOrder;

        // Events

        public Action clickCallback;

        public DWButton(UIObject? parent, Vec2 position, Vec2 size, Action clickCallback, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            initialScale = size;

            roundRadius = 5f;
            scaleSecondOrder = new SecondOrder(size, 4.5f, 0.45f, 0.15f);
            this.clickCallback = clickCallback;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Vec2 currentSize = initialScale;

            if (IsHovering && !IsMouseDown)
                currentSize *= hoverScaleMulti;
            else if (IsMouseDown)
                currentSize *= clickScaleMulti;
            else if (!IsHovering && !IsMouseDown)
                currentSize *= normalScaleMulti;
            else
                currentSize *= normalScaleMulti;

            Size = scaleSecondOrder.Update(deltaTime, currentSize);

            if (IsHovering && !IsMouseDown)
                Color = Col.Lerp(Color, hoverColor, colorSmoothingSpeed * deltaTime);
            else if (IsMouseDown)
                Color = Col.Lerp(Color, clickColor, colorSmoothingSpeed * deltaTime);
            else if (!IsHovering && !IsMouseDown)
                Color = Col.Lerp(Color, normalColor, colorSmoothingSpeed * deltaTime);
            else
                Color = Col.Lerp(Color, normalColor, colorSmoothingSpeed * deltaTime);
        }

        public override void OnMouseUp()
        {
            clickCallback?.Invoke();
        }
    }
}
