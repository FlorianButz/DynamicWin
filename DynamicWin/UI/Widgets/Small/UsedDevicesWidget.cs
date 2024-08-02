using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    internal class UsedDevicesWidget : SmallWidgetBase
    {
        public UsedDevicesWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment) { }

        float camDotSize = 2.5f;
        float camDotSizeCurrent = 0f;
        float camDotPositionX = 0f;

        float micDotSize = 2.5f;
        float micDotSizeCurrent = 0f;
        float micDotPositionX = 0f;

        float seperation = 5;

        protected override float GetWidgetWidth()
        {
            return 20;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            bool isCamActive = DeviceUsageChecker.IsWebcamInUse();
            bool isMicActive = DeviceUsageChecker.IsMicrophoneInUse();

            camDotSizeCurrent = Mathf.Lerp(camDotSizeCurrent, isCamActive ? camDotSize : 0f, 5f * deltaTime);
            micDotSizeCurrent = Mathf.Lerp(micDotSizeCurrent, isMicActive ? micDotSize : 0f, 5f * deltaTime);

            if(isCamActive && isMicActive)
            {
                camDotPositionX = Mathf.Lerp(camDotPositionX, seperation, 5f * deltaTime);
                micDotPositionX = Mathf.Lerp(micDotPositionX, -seperation, 5f * deltaTime);
            }
            else
            {
                camDotPositionX = Mathf.Lerp(camDotPositionX, 0, 5f * deltaTime);
                micDotPositionX = Mathf.Lerp(micDotPositionX, 0, 5f * deltaTime);
            }
        }

        public override void DrawWidget(SKCanvas canvas)
        {
            var paint = GetPaint();

            var camPos = GetScreenPosFromRawPosition(new Vec2(camDotPositionX, 0), new Vec2(0, camDotSizeCurrent / 2), UIAlignment.Center, this);

            paint.Color = Theme.Error.Value();
            canvas.DrawCircle(camPos.X, camPos.Y, camDotSizeCurrent, paint);

            var micPos = GetScreenPosFromRawPosition(new Vec2(micDotPositionX, 0), new Vec2(0, micDotSizeCurrent / 2), UIAlignment.Center, this);

            paint.Color = Theme.Success.Value();
            canvas.DrawCircle(micPos.X, micPos.Y, micDotSizeCurrent, paint);
        }
    }
}
