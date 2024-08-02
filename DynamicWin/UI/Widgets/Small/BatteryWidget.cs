using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    internal class BatteryWidget : SmallWidgetBase
    {
        DWImage batteryImage;
        DWImage batteryFillLevel;

        DWImage noBattery;
        DWImage batteryCharging;

        float imageScale = 1.75f;

        public BatteryWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            batteryImage = new DWImage(this, Resources.Resources.Battery, Vec2.zero, new Vec2(Size.Y * imageScale, Size.Y * imageScale), UIAlignment.Center, true);
            AddLocalObject(batteryImage);

            batteryFillLevel = new DWImage(this, Resources.Resources.BatteryLevel_10P, Vec2.zero, new Vec2(Size.Y * imageScale, Size.Y * imageScale), UIAlignment.Center, true);
            AddLocalObject(batteryFillLevel);

            noBattery = new DWImage(this, Resources.Resources.NoBattery, Vec2.zero, new Vec2(Size.Y * imageScale, Size.Y * imageScale), UIAlignment.Center, true);
            AddLocalObject(noBattery);
            batteryCharging = new DWImage(this, Resources.Resources.BatteryCharging, Vec2.zero, new Vec2(Size.Y * imageScale, Size.Y * imageScale), UIAlignment.Center, true);
            AddLocalObject(batteryCharging);

            noBattery.SilentSetActive(false);
            batteryCharging.SilentSetActive(false);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            var batteryStatus = PowerStatusChecker.GetPowerStatus();

            if (batteryStatus.BatteryFlag != ((byte)128))
            {
                if (batteryStatus.ACLineStatus == 0)
                {
                    if (batteryStatus.BatteryLifePercent > 75) batteryFillLevel.Image = Resources.Resources.BatteryLevel_Full;
                    else if (batteryStatus.BatteryLifePercent > 50) batteryFillLevel.Image = Resources.Resources.BatteryLevel_75P;
                    else if (batteryStatus.BatteryLifePercent > 25) batteryFillLevel.Image = Resources.Resources.BatteryLevel_50P;
                    else if (batteryStatus.BatteryLifePercent > 10) batteryFillLevel.Image = Resources.Resources.BatteryLevel_25P;
                    else batteryFillLevel.Image = Resources.Resources.BatteryLevel_10P;

                    if (!batteryImage.IsEnabled)
                    {
                        batteryImage.SetActive(true);
                        batteryFillLevel.SetActive(true);

                        noBattery.SetActive(false);
                        batteryCharging.SetActive(false);
                    }
                }
                else
                {
                    if (!batteryCharging.IsEnabled)
                    {
                        batteryImage.SetActive(false);
                        batteryFillLevel.SetActive(false);

                        noBattery.SetActive(false);
                        batteryCharging.SetActive(true);
                    }
                }
            }
            else
            {
                if (!noBattery.IsEnabled)
                {
                    batteryImage.SetActive(false);
                    batteryFillLevel.SetActive(false);

                    noBattery.SetActive(true);
                    batteryCharging.SetActive(false);
                }
            }
        }

        protected override float GetWidgetWidth()
        {
            return 20;
        }
    }
}
