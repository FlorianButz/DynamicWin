using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    class RegisterUsedDevicesWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => true;
        public string WidgetName => "Used Devices";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new UsedDevicesWidget(parent, position, alignment);
        }
    }

    public class UsedDevicesWidget : SmallWidgetBase
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

        float sinCycleCamera = 1f;
        float sinCycleMicrophone = 0f;

        float sinSpeed = 2.75f;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            sinCycleCamera += sinSpeed * deltaTime;
            sinCycleMicrophone += sinSpeed * deltaTime;

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

            paint.Color = Col.Lerp(Theme.Error, Theme.Error * 0.6f, Mathf.Remap((float)Math.Sin(sinCycleCamera), -1, 1, 0, 1)).Value();
            canvas.DrawCircle(camPos.X, camPos.Y, camDotSizeCurrent, paint);

            var micPos = GetScreenPosFromRawPosition(new Vec2(micDotPositionX, 0), new Vec2(0, micDotSizeCurrent / 2), UIAlignment.Center, this);

            paint.Color = Col.Lerp(Theme.Success, Theme.Success * 0.6f, Mathf.Remap((float)Math.Sin(sinCycleMicrophone), -1, 1, 0, 1)).Value();
            canvas.DrawCircle(micPos.X, micPos.Y, micDotSizeCurrent, paint);
        }
    }
}
