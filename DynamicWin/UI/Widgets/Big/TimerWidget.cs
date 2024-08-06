
using DynamicWin.Main;
using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.UIElements.Custom;
using DynamicWin.Utils;
using SkiaSharp;

namespace DynamicWin.UI.Widgets.Big
{
    internal class TimerWidget : WidgetBase
    {
        DWText timerText;

        System.Timers.Timer timer;

        DWImageButton startStopButton;

        DWImageButton hourMore;
        DWImageButton hourLess;
        DWImageButton minuteMore;
        DWImageButton minuteLess;
        DWImageButton secondMore;
        DWImageButton secondLess;

        public static TimerWidget instance;

        public int CurrentTime { get { if (isTimerRunning) return initialSecondsSet - elapsedSeconds; else return -1; } }

        public TimerWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            instance = this;

            timerText = new DWText(parent, "00:00:00", new Vec2(15, 0f), UIAlignment.MiddleLeft)
            {
                textSize = 45,
                Font = Resources.Res.InterRegular
            };
            timerText.Anchor.X = 0;
            AddLocalObject(timerText);

            startStopButton = new DWImageButton(parent, Resources.Res.Play, new Vec2(-35, 0), new Vec2(25, 25), () =>
            {
                ToggleTimer();
            }, alignment: UIAlignment.MiddleRight);
            AddLocalObject(startStopButton);

            // More / Less buttons

            // Hours

            hourMore = new DWImageButton(parent, Resources.Res.ArrowUp, new Vec2(0, -30f), new Vec2(25f, 25f), () =>
            {
                ChangeTimerTime(0, 0, 1);
            }, alignment: UIAlignment.MiddleLeft)
            {
                expandInteractionRect = 0,
                normalColor = Col.Transparent
            };
            AddLocalObject(hourMore);

            hourLess = new DWImageButton(parent, Resources.Res.ArrowDown, new Vec2(0, 30f), new Vec2(25f, 25f), () =>
            {
                ChangeTimerTime(0, 0, -1);
            }, alignment: UIAlignment.MiddleLeft)
            {
                expandInteractionRect = 0,
                normalColor = Col.Transparent
            };
            AddLocalObject(hourLess);

            // Minutes

            minuteMore = new DWImageButton(parent, Resources.Res.ArrowUp, new Vec2(0, -30f), new Vec2(25f, 25f), () =>
            {
                ChangeTimerTime(0, 1, 0);
            }, alignment: UIAlignment.MiddleLeft)
            {
                expandInteractionRect = 0,
                normalColor = Col.Transparent
            };
            AddLocalObject(minuteMore);

            minuteLess = new DWImageButton(parent, Resources.Res.ArrowDown, new Vec2(0, 30f), new Vec2(25f, 25f), () =>
            {
                ChangeTimerTime(0, -1, 0);
            }, alignment: UIAlignment.MiddleLeft)
            {
                expandInteractionRect = 0,
                normalColor = Col.Transparent
            };
            AddLocalObject(minuteLess);

            // Seconds

            secondMore = new DWImageButton(parent, Resources.Res.ArrowUp, new Vec2(0, -30f), new Vec2(25f, 25f), () =>
            {
                ChangeTimerTime(1, 0, 0);
            }, alignment: UIAlignment.MiddleLeft)
            {
                expandInteractionRect = 0,
                normalColor = Col.Transparent
            };
            AddLocalObject(secondMore);

            secondLess = new DWImageButton(parent, Resources.Res.ArrowDown, new Vec2(0, 30f), new Vec2(25f, 25f), () =>
            {
                ChangeTimerTime(-1, 0, 0);
            }, alignment: UIAlignment.MiddleLeft)
            {
                expandInteractionRect = 0,
                normalColor = Col.Transparent
            };
            AddLocalObject(secondLess);

            hourMore.Image.Color = Col.White.Override(a: 0.15f);
            hourLess.Image.Color = Col.White.Override(a: 0.15f);
            minuteMore.Image.Color = Col.White.Override(a: 0.15f);
            minuteLess.Image.Color = Col.White.Override(a: 0.15f);
            secondMore.Image.Color = Col.White.Override(a: 0.15f);
            secondLess.Image.Color = Col.White.Override(a: 0.15f);

            ChangeTimerTime(0, 5, 0);
        }

        bool isTimerRunning = false;
        int initialSecondsSet = 0;

        public void ChangeTimerTime(int seconds, int minutes, int hours)
        {
            initialSecondsSet += seconds + minutes * 60 + (hours * 60) * 60;
            
            initialSecondsSet = (int)Mathf.Clamp(initialSecondsSet, 0, int.MaxValue);
            
            TimeSpan t = TimeSpan.FromSeconds(initialSecondsSet);
            string answer = string.Format("{0:D2}:{1:D2}:{2:D2}",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);
            timerText.SilentSetText(answer);
        }

        public void ToggleTimer()
        {
            if (isTimerRunning) StopTimer();
            else StartTimer();

            if (isTimerRunning) startStopButton.Image.Image = Resources.Res.Stop;
            else startStopButton.Image.Image = Resources.Res.Play;
        }

        public void StopTimer()
        {
            timer.Stop();
            isTimerRunning = false;

            TimeSpan t = TimeSpan.FromSeconds(initialSecondsSet);
            string answer = string.Format("{0:D2}:{1:D2}:{2:D2}",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);
            timerText.SetText(answer);
        }

        void TimerEnd()
        {
            StopTimer();
            RendererMain.Instance.MainIsland.hidden = false;

            MenuManager.OpenOverlayMenu(new TimerOverMenu(), 15f);
        }

        int elapsedSeconds = 0;
        public void StartTimer()
        {
            instance = this;
            isTimerRunning = true;
            elapsedSeconds = 0;

            timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, e) =>
            {
                elapsedSeconds++;

                if(initialSecondsSet - elapsedSeconds <= 0)
                {
                    TimerEnd();
                    return;
                }

                TimeSpan t = TimeSpan.FromSeconds(initialSecondsSet - elapsedSeconds);

                string answer = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                t.Hours,
                                t.Minutes,
                                t.Seconds);

                timerText.SilentSetText(answer);
            };
            timer.Start();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            timerText.textSize = Mathf.Lerp(timerText.textSize, isTimerRunning ? 29 : 25, 10f * deltaTime);

            var tOff = -5f;
            var mul = 0.365f;

            TimeSpan t = TimeSpan.FromSeconds(initialSecondsSet);
            var h = (timerText.GetBoundsForString(string.Format("{0:D2}", t.Hours)).X) * mul;
            var m = (timerText.GetBoundsForString(string.Format("{0:D2}", t.Minutes)).X) * mul;
            var s = (timerText.GetBoundsForString(string.Format("{0:D2}", t.Seconds)).X) * mul;

            hourLess.LocalPosition.X = tOff + h;
            hourMore.LocalPosition.X = tOff + h;

            minuteLess.LocalPosition.X = tOff + m + h;
            minuteMore.LocalPosition.X = tOff + m + h;

            secondLess.LocalPosition.X = tOff + s + m + h;
            secondMore.LocalPosition.X = tOff + s + m + h;

            hourLess.SetActive(!isTimerRunning);
            hourMore.SetActive(!isTimerRunning);
            minuteLess.SetActive(!isTimerRunning);
            minuteMore.SetActive(!isTimerRunning);
            secondLess.SetActive(!isTimerRunning);
            secondMore.SetActive(!isTimerRunning);
        }

        public override void DrawWidget(SKCanvas canvas)
        {
            base.DrawWidget(canvas);

            var paint = GetPaint();
            paint.Color = Col.White.Override(a: 0.1f).Value();
            canvas.DrawRoundRect(GetRect(), paint);
        }
    }
}
