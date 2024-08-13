using DynamicWin.Main;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements.Custom
{
    public class DWSlider : DWProgressBar
    {
        public Action<float> clickCallback;

        public DWSlider(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {

        }

        float valueBefore = 0f;

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            valueBefore = value;
        }

        public override void OnGlobalMouseUp()
        {
            base.OnGlobalMouseUp();

            if(valueBefore != value)
                clickCallback?.Invoke(this.value);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (IsMouseDown)
                this.value = Mathf.Clamp(Mathf.Remap(RendererMain.CursorPosition.X - Position.X, 0, Size.X, 0, 1),
                    0.05f, 1);
        }
    }
}
