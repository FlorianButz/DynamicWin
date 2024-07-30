using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI
{
    internal class IslandObject : UIObject
    {
        public float topOffset = 5f;

        public SecondOrder scaleSecondOrder;

        public Vec2 secondOrderValuesExpand = new Vec2(3f, 0.6f);
        public Vec2 secondOrderValuesContract = new Vec2(2.5f, 0.7f);

        public Vec2 currSize;

        public IslandObject() : base(null, Vec2.zero, new Vec2(250, 50), UIAlignment.TopCenter)
        {
            currSize = Size;

            Anchor = new Vec2(0.5f, 0f);

            Color = new Col(0, 0, 0);
            roundRadius = 35f;

            LocalPosition = new Vec2(0, 15f);

            scaleSecondOrder = new SecondOrder(Size, secondOrderValuesExpand.X, secondOrderValuesExpand.Y, 0.1f);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Size = scaleSecondOrder.Update(deltaTime, currSize);

            if (IsHovering)
                currSize = new Vec2(450, 100);
            else
                currSize = new Vec2(250, 50);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
            canvas.DrawRoundRect(GetRect(), paint);
        }
    }
}
