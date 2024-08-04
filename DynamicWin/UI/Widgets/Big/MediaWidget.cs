using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;

namespace DynamicWin.UI.Widgets.Big
{
    internal class MediaWidget : WidgetBase
    {
        MediaController controller;
        AudioVisualizer audioVisualizer;

        DWImageButton playPause;
        DWImageButton next;
        DWImageButton prev;

        DWText noMediaPlaying;

        public MediaWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            InitMediaPlayer();

            playPause = new DWImageButton(this, Resources.Resources.PlayPause, new Vec2(0, 25), new Vec2(30, 30), () =>
            {
                controller.PlayPause();
            }, alignment: UIAlignment.Center)
            {
                roundRadius = 25,
                normalColor = Col.Transparent,
                hoverColor = Col.White.Override(a: 0.1f),
                clickColor = Col.White.Override(a: 0.25f),
                hoverScaleMulti = Vec2.one * 1.25f,
                imageScale = 0.8f
            };
            AddLocalObject(playPause);

            next = new DWImageButton(this, Resources.Resources.Next, new Vec2(50, 25), new Vec2(30, 30), () =>
            {
                controller.Next();
            }, alignment: UIAlignment.Center)
            {
                roundRadius = 25,
                normalColor = Col.Transparent,
                hoverColor = Col.White.Override(a: 0.1f),
                clickColor = Col.White.Override(a: 0.25f),
                hoverScaleMulti = Vec2.one * 1.25f,
                imageScale = 0.65f
            };
            AddLocalObject(next);

            prev = new DWImageButton(this, Resources.Resources.Previous, new Vec2(-50, 25), new Vec2(30, 30), () =>
            {
                controller.Previous();
            }, alignment: UIAlignment.Center)
            {
                roundRadius = 25,
                normalColor = Col.Transparent,
                hoverColor = Col.White.Override(a: 0.1f),
                clickColor = Col.White.Override(a: 0.25f),
                hoverScaleMulti = Vec2.one * 1.25f,
                imageScale = 0.65f
            };
            AddLocalObject(prev);

            audioVisualizer = new AudioVisualizer(this, new Vec2(0, 30), new Vec2(125, 25), length: 16)
            {
                divisor = 1.35f
            };
            AddLocalObject(audioVisualizer);

            noMediaPlaying = new DWText(this, "No Media Playing", new Vec2(0, 30))
            {
                Color = Theme.TextSecond,
                Font = Resources.Resources.InterBold,
                textSize = 16
            };
            noMediaPlaying.SilentSetActive(false);
            AddLocalObject(noMediaPlaying);

            prev.Image.dynamicColor         = true;
            next.Image.dynamicColor         = true;
            playPause.Image.dynamicColor    = true;
        }

        float smoothedAmp = 0f;
        float smoothing = 1.5f;

        bool wasEnabled = false;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            prev.normalColor = audioVisualizer.GetActionCol().Override(a: 0.2f);
            next.normalColor = audioVisualizer.GetActionCol().Override(a: 0.2f);
            playPause.normalColor = audioVisualizer.GetActionCol().Override(a: 0.2f);

            smoothedAmp = (float)Math.Max(Mathf.Lerp(smoothedAmp, audioVisualizer.AverageAmplitude, smoothing * deltaTime), audioVisualizer.AverageAmplitude);
            if (smoothedAmp < 0.005f) smoothedAmp = 0f;

            if(smoothedAmp.Equals(0f) && !wasEnabled)
            {
                wasEnabled = true;
                noMediaPlaying.SetActive(true);
            }
            else if (!smoothedAmp.Equals(0f) && wasEnabled)
            {
                wasEnabled = false;
                noMediaPlaying.SetActive(false);
            }
        }

        private void InitMediaPlayer()
        {
            controller = new MediaController();
        }

        public override void DrawWidget(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.Color = (Theme.Primary * 0.1f).Value();
            canvas.DrawRoundRect(GetRect(), paint);
        }
    }
}
