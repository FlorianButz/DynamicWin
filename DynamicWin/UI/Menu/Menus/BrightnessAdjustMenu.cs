using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.UIElements.Custom;
using DynamicWin.Utils;
using NAudio.CoreAudioApi;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Management;

namespace DynamicWin.UI.Menu.Menus
{
    internal class BrightnessAdjustMenu : BaseMenu
    {

        DWImage brightnessImage;
        DWProgressBar brightness;

        static BrightnessAdjustMenu instance;

        float islandScale = 1.25f;

        public BrightnessAdjustMenu()
        {
            instance = this;
            timerUntilClose = 0f;
        }

        public static void PressBK()
        {
            instance.islandScale = 1.025f;
        }

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            brightnessImage = new DWImage(island, Res.Brightness, new Vec2(20, 0), new Vec2(20, 20), UIAlignment.MiddleLeft);
            brightnessImage.Anchor.X = 0;
            objects.Add(brightnessImage);

            brightness = new DWProgressBar(island, new Vec2(-20, 0), new Vec2(150, 5f), UIAlignment.MiddleRight);
            brightness.Anchor.X = 1;
            objects.Add(brightness);

            return objects;
        }

        public static float timerUntilClose = 0f;

        public override void Update()
        {
            base.Update();

            if (timerUntilClose > 2.75f) MenuManager.CloseOverlay();

            islandScale = Mathf.Lerp(islandScale, 1f, 5f * RendererMain.Instance.DeltaTime);

            timerUntilClose += RendererMain.Instance.DeltaTime;

            this.brightness.value = (float)WindowsSettingsBrightnessController.Get() / 100f;

            var volXOffset = KeyHandler.keyDown.Contains(System.Windows.Forms.Keys.VolumeUp) ? 2f :
                KeyHandler.keyDown.Contains(System.Windows.Forms.Keys.VolumeDown) ? -2f : 0;

            this.brightness.LocalPosition.X = Mathf.Lerp(this.brightness.LocalPosition.X, volXOffset, 
                (Math.Abs(volXOffset) > Math.Abs(this.brightness.LocalPosition.X) ? 4.5f : 2.5f) * RendererMain.Instance.DeltaTime);
        }

        public override Vec2 IslandSize()
        {
            return new Vec2(250, 35) * islandScale;
        }

        public override Vec2 IslandSizeBig()
        {
            return base.IslandSizeBig() * 1.05f;
        }

        internal static int GetBrightness()
        {
            return WindowsSettingsBrightnessController.Get();
        }

        public override Col IslandBorderColor()
        {
            return new Col(0.5f, 0.5f, 0.5f);
        }
    }

    static class WindowsSettingsBrightnessController
    {
        static bool notSupported = false;

        public static int Get()
        {
            if(notSupported)
                return 100;

            try
            {
                using var mclass = new ManagementClass("WmiMonitorBrightness")
                {
                    Scope = new ManagementScope(@"\\.\root\wmi")
                };
                using var instances = mclass.GetInstances();
                foreach (ManagementObject instance in instances)
                {
                    return (byte)instance.GetPropertyValue("CurrentBrightness");
                }
                return 0;
            }catch(System.Management.ManagementException e)
            {
                notSupported = true;
                return 100;
            }
        }

        public static void Set(int brightness)
        {
            try
            {
                using var mclass = new ManagementClass("WmiMonitorBrightnessMethods")
                {
                    Scope = new ManagementScope(@"\\.\root\wmi")
                };
                using var instances = mclass.GetInstances();
                var args = new object[] { 1, brightness };
                foreach (ManagementObject instance in instances)
                {
                    instance.InvokeMethod("WmiSetBrightness", args);
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
