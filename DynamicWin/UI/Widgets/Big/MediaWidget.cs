using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Big
{
    class RegisterMediaWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => false;
        public string WidgetName => "Media Playback Control";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new MediaWidget(parent, position, alignment);
        }
    }

    public class MediaWidget : WidgetBase
    {
        MediaController controller;
        AudioVisualizer audioVisualizer;
        AudioVisualizer audioVisualizerBig;

        DWImageButton playPause;
        DWImageButton next;
        DWImageButton prev;

        DWText noMediaPlaying;

        DWText title;
        DWText artist;

        /*protected override float GetWidgetWidth()
        {
            return base.GetWidgetWidth() * 2f;
        }*/

        public MediaWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            InitMediaPlayer();

            playPause = new DWImageButton(this, Resources.Res.PlayPause, new Vec2(0, 25), new Vec2(30, 30), () =>
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

            next = new DWImageButton(this, Resources.Res.Next, new Vec2(50, 25), new Vec2(30, 30), () =>
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

            prev = new DWImageButton(this, Resources.Res.Previous, new Vec2(-50, 25), new Vec2(30, 30), () =>
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

            audioVisualizerBig = new AudioVisualizer(this, new Vec2(0, 0), GetWidgetSize(), length: 16, alignment: UIAlignment.Center,
                Primary: spotifyCol.Override(a: 0.35f), Secondary: spotifyCol.Override(a: 0.025f) * 0.1f)
            {
                divisor = 2f
            };
            audioVisualizerBig.SilentSetActive(false);

            noMediaPlaying = new DWText(this, "No Media Playing", new Vec2(0, 30))
            {
                Color = Theme.TextSecond,
                Font = Resources.Res.InterBold,
                TextSize = 16
            };
            noMediaPlaying.SilentSetActive(false);
            AddLocalObject(noMediaPlaying);

            title = new DWText(this, "Title", new Vec2(0, 22.5f))
            {
                Color = Theme.TextSecond,
                Font = Resources.Res.InterBold,
                TextSize = 15
            };
            title.SilentSetActive(false);
            AddLocalObject(title);

            artist = new DWText(this, "Artist", new Vec2(0, 42.5f))
            {
                Color = Theme.TextThird,
                Font = Resources.Res.InterRegular,
                TextSize = 13
            };
            artist.SilentSetActive(false);
            AddLocalObject(artist);
        }

        float smoothedAmp = 0f;
        float smoothing = 1.5f;

        int cycle = 0;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (cycle % 32 == 0)
            {
                isSpotifyAvaliable = IsSpotifyAvaliable();

                if (isSpotifyAvaliable)
                {
                    string titleString;
                    string artistString;
                    string error;

                    GetSpotifyTrackInfo(out titleString, out artistString, out error);

                    if (string.IsNullOrEmpty(error))
                    {
                        title.Text = DWText.Truncate(titleString, (GetWidgetWidth() == 400 ? 50 : 20));
                        artist.Text = DWText.Truncate(artistString, (GetWidgetWidth() == 400 ? 60 : 28));
                    }
                    else
                    {
                        if (!error.Equals("Paused") || title.Text.ToLower().Equals("title") || title.Text.ToLower().Equals("error"))
                        {
                            title.Text = "Error";
                            artist.Text = DWText.Truncate(error, 24);
                        }

                    }
                }
            }
            cycle++;

            prev.normalColor = Theme.IconColor * audioVisualizer.GetActionCol().Override(a: 0.2f);
            next.normalColor = Theme.IconColor * audioVisualizer.GetActionCol().Override(a: 0.2f);
            playPause.normalColor = Theme.IconColor * audioVisualizer.GetActionCol().Override(a: 0.2f);

            if(!isSpotifyAvaliable)
                smoothedAmp = (float)Math.Max(Mathf.Lerp(smoothedAmp, audioVisualizer.AverageAmplitude, smoothing * deltaTime), audioVisualizer.AverageAmplitude);
            else
                smoothedAmp = (float)Math.Max(Mathf.Lerp(smoothedAmp, audioVisualizer.AverageAmplitude, smoothing * deltaTime), audioVisualizer.AverageAmplitude);

            if (smoothedAmp < 0.005f) smoothedAmp = 0f;

            spotifyBlur = Mathf.Lerp(spotifyBlur, isSpotifyAvaliable ? 0f : 25f, 10f * deltaTime);

            noMediaPlaying.SetActive(smoothedAmp.Equals(0f) && !isSpotifyAvaliable);
            title.SetActive(isSpotifyAvaliable);
            artist.SetActive(isSpotifyAvaliable);
            audioVisualizerBig.SetActive(isSpotifyAvaliable);
            audioVisualizer.SetActive(!isSpotifyAvaliable);

            audioVisualizerBig.UpdateCall(deltaTime);
        }

        private void InitMediaPlayer()
        {
            controller = new MediaController();
        }

        float spotifyBlur = 0f;
        bool isSpotifyAvaliable = false;
        Col spotifyCol = Col.FromHex("#1cb351");

        public override void DrawWidget(SKCanvas canvas)
        {
            var paint = GetPaint();
            paint.Color = GetColor(Theme.WidgetBackground).Value();
            canvas.DrawRoundRect(GetRect(), paint);

            int saveCanvas = canvas.Save();

            canvas.ClipRoundRect(GetRect());

            audioVisualizerBig.Alpha = 0.75f;
            audioVisualizerBig.blurAmount = 15f;
            audioVisualizerBig.DrawCall(canvas);

            canvas.RestoreToCount(saveCanvas);

            if (isSpotifyAvaliable || spotifyBlur <= 24f)
            {
                paint.Color = spotifyCol.Override(a: Color.a * (1f - (spotifyBlur / 25f))).Value();

                var blur = SKImageFilter.CreateBlur((float)Math.Max(GetBlur(), spotifyBlur) + 0.1f, (float)Math.Max(GetBlur(), spotifyBlur) + 0.1f);
                paint.ImageFilter = blur;

                var r = GetRect();

                var inward = 5;
                var w = 25 + spotifyBlur * (isSpotifyAvaliable ? 2.5f : -0.15f);
                var h = 25 + spotifyBlur * (isSpotifyAvaliable ? 2.5f : -0.15f);
                var x = Position.X + r.Width - w / 2 - inward;
                var y = Position.Y - h / 2 + inward;

                canvas.DrawBitmap(Resources.Res.Spotify, SKRect.Create(x, y, w, h), paint);

                paint.Color = GetColor(spotifyCol.Override(a: 0.25f * Color.a)).Value();
                paint.StrokeWidth = 2f;

                float[] intervals = { 5, 10 };
                paint.PathEffect = SKPathEffect.CreateDash(intervals, (float)-cycle * 0.1f);

                paint.IsStroke = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;

                canvas.DrawRoundRect(r, paint);

                canvas.RestoreToCount(saveCanvas);

                var shadowPaint = GetPaint();

                var drop = SKImageFilter.CreateDropShadowOnly(0, 0, (25f - spotifyBlur) / 2, (25f - spotifyBlur) / 2, 
                    spotifyCol.Override(a: (25f - spotifyBlur) / 100f).Value());
                shadowPaint.ImageFilter = drop;
                shadowPaint.IsStroke = true;
                shadowPaint.StrokeWidth = 15;

                int s = canvas.Save();
                canvas.ClipRoundRect(GetRect());
                canvas.DrawRoundRect(GetRect(), shadowPaint);
                canvas.RestoreToCount(s);
            }
        }

        public static void GetSpotifyTrackInfo(out string title, out string artist, out string error)
        {
            var proc = Process.GetProcessesByName("Spotify").FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

            if (proc == null)
            {
                error = "Spotify is not open!";
                title = null;
                artist = null;
                return;
            }

            if (proc.MainWindowTitle.ToLower().StartsWith("spotify"))
            {
                error = "Paused";
                title = null;
                artist = null;
                return;
            }

            string[] strings = proc.MainWindowTitle.Split(" - ");
            
            if(strings.Length >= 2)
            {
                title = strings[1];
                artist = strings[0];
                error = null;
            }
            else
            {
                error = null;
                title = "Advertisement";
                artist = "";
                return;
            }
        }

        public bool IsSpotifyAvaliable()
        {
            var processes = Process.GetProcessesByName("Spotify");
            return processes.Any();
        }
    }
}
