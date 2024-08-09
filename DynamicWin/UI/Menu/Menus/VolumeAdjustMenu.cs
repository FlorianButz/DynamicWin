using AudioSwitcher.AudioApi.CoreAudio;
using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu.Menus
{
    internal class VolumeAdjustMenu : BaseMenu
    {
        // P/Invoke to call the Windows API function
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        double GetVolumePercent()
        {
            // Get the current volume (0-100%)
            double volume = defaultPlaybackDevice.Volume;

            return volume;
        }

        bool IsMuted()
        {
            return defaultPlaybackDevice.IsMuted;
        }

        static CoreAudioDevice defaultPlaybackDevice;

        DWImage volumeImage;
        UIObject mutedBg;
        DWText muteText;
        DWProgressBar volume;

        static VolumeAdjustMenu instance;

        float shakeStrength = 0f;

        public VolumeAdjustMenu()
        {
            instance = this;
            timerUntilClose = 0f;

            shakeStrength = 0f;

            if (defaultPlaybackDevice == null)
            {
                // Initialize the CoreAudioController
                var controller = new CoreAudioController();

                // Get the default playback device (speakers/headphones)
                defaultPlaybackDevice = controller.DefaultPlaybackDevice;
            }
        }

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            mutedBg = new UIObject(island, new Vec2(10, 0), new Vec2(40, 20), UIAlignment.MiddleLeft);
            mutedBg.Color = Theme.Error;
            mutedBg.roundRadius = 15;
            mutedBg.Anchor.X = 0;
            objects.Add(mutedBg);

            volumeImage = new DWImage(island, Res.VolumeOn, new Vec2(20, 0), new Vec2(20, 20), UIAlignment.MiddleLeft);
            volumeImage.Anchor.X = 0;
            objects.Add(volumeImage);

            muteText = new DWText(island, "Silent", new Vec2(-15, 0), UIAlignment.MiddleRight);
            muteText.Anchor.X = 1;
            muteText.TextSize = 15;
            muteText.Font = Res.InterBold;
            muteText.Color = Theme.Error;
            objects.Add(muteText);

            volume = new DWProgressBar(island, new Vec2(-15, 0), new Vec2(100, 10), UIAlignment.MiddleRight);
            volume.Anchor.X = 1;
            objects.Add(volume);

            return objects;
        }

        public static float timerUntilClose = 0f;

        float timer = 0;
        float shakeSpeed = 35;

        float Func(float x)
        {
            //double result = -Math.Pow(x, 3f) + (-Math.Pow(x - 1, 6f * 4)) + 1;
            //double result = -Math.Pow((x / 0.5) - 1, 2) + 1;
            double result = 1f - (x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2);

            return (float)result;
        }

        float seconds = 0f;
        bool mute = false;

        public override void Update()
        {
            base.Update();

            if (timerUntilClose > 3.5f) MenuManager.CloseOverlay();

            var volume = GetVolumePercent();
            var isMuted = IsMuted();

            if (volume <= 0f || isMuted)
            {
                mute = true;

                volumeImage.Image = Res.VolumeOff;
                RendererMain.Instance.MainIsland.LocalPosition.X = Mathf.Lerp(RendererMain.Instance.MainIsland.LocalPosition.X,
                    0, 10f * RendererMain.Instance.DeltaTime);

                muteText.SetActive(true);
                mutedBg.SetActive(true);
                this.volume.SetActive(false);
            }
            else if (volume >= 0f && !isMuted)
            {
                if (mute)
                {
                    seconds = 0f;
                    timer = 0f;

                    mute = false;
                }


                RendererMain.Instance.MainIsland.LocalPosition.X = Mathf.Lerp(RendererMain.Instance.MainIsland.LocalPosition.X,
                    (float)Math.Sin(timer) * shakeStrength, 10f * RendererMain.Instance.DeltaTime);

                volumeImage.Image = Res.VolumeOn;

                muteText.SetActive(false);
                mutedBg.SetActive(false);
                this.volume.SetActive(true);
            }

            timer += RendererMain.Instance.DeltaTime * shakeSpeed;
            seconds += RendererMain.Instance.DeltaTime;

            shakeStrength = Mathf.Clamp(Func(Mathf.Clamp(seconds * 1.5f, 0, 1)), 0, 1f) * 45;

            timerUntilClose += RendererMain.Instance.DeltaTime;

            this.volume.value = (float)volume / 100f;
        }

        public override Vec2 IslandSize()
        {
            return new Vec2(250, 35);
        }
    }

    internal class DWProgressBar : UIObject
    {
        UIObject content;

        public float value = 1f;
        public float originalSize = 0f;

        public DWProgressBar(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            content = new UIObject(this, Vec2.zero, size, UIAlignment.MiddleRight);
            content.Anchor.X = 0;

            originalSize = content.Size.X;

            roundRadius = 15f;
            content.roundRadius = 15f;

            Color = Theme.IconColor.Override(a: 0.25f);
            content.Color = Theme.IconColor.Override(a: 1f);

            AddLocalObject(content);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            content.Size.X = originalSize * value;
        }
    }
}
