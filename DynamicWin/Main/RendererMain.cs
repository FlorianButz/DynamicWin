using System.Diagnostics;
using System.Windows.Forms;
using DynamicWin.UI;
using DynamicWin.Utils;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace DynamicWin.Main
{

    public class RendererMain : SKControl
    {
        private System.Windows.Forms.Timer timer;

        private IslandObject islandObject;

        List<UIObject> objects = new List<UIObject>();

        public static Vec2 ScreenDimensions { get => new Vec2(instance.Width, instance.Height); }
        public static Vec2 CursorPosition { get => new Vec2(Cursor.Position.X, Cursor.Position.Y); }

        private static RendererMain instance;
        public static RendererMain Instance { get { return instance; } }

        public RendererMain()
        {
            instance = this;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            islandObject = new IslandObject();

            objects.Add(new DWText(islandObject, "Test Text", Vec2.zero, UIAlignment.Center));
            objects.Add(new DWImage(islandObject, Resources.Resources.search, Vec2.zero, new Vec2(25, 25), UIAlignment.Center));

            // Set up the timer
            timer = new System.Windows.Forms.Timer
            {
                Interval = 6
            };
            timer.Tick += (sender, args) => Update();
            timer.Tick += (sender, args) => Render();
            timer.Start();
        }

        float deltaTime = 0f;
        public float DeltaTime { get { return deltaTime; } private set => deltaTime = value; }

        Stopwatch? updateStopwatch;

        int te = 0;

        // Called once every frame to update values

        private new void Update()
        {
            if (updateStopwatch != null)
            {
                updateStopwatch.Stop();
                deltaTime = updateStopwatch.ElapsedMilliseconds / 1000f;
                System.Diagnostics.Debug.WriteLine(deltaTime);
            }
            else
                deltaTime = 1f / 1000f;

            // Update logic here

            te++;

            islandObject.Update(DeltaTime);

            foreach (UIObject uiObject in objects)
            {
                uiObject.Update(DeltaTime);

                if(te >= 100)
                {
                    if (uiObject.GetType() == typeof(DWText))
                    {
                        ((DWText)uiObject).Text = "Test End!";
                    }
                    te = 0;
                }
            }

            // End of update

            updateStopwatch = new Stopwatch();
            updateStopwatch.Start();
        }

        // Called once every frame to render frame, called after Update

        private void Render()
        {
            Invalidate();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            // Render

            var canvas = e.Surface.Canvas;
            if (canvas == null) return;
            
            canvas.Clear(SKColors.Transparent);

            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Firebrick,
                IsAntialias = true,
                IsDither = true
            };

            var rect = SKRect.Create(CursorPosition.X, CursorPosition.Y, 5, 5);
            canvas.DrawRect(rect, paint);

            int saveState = canvas.Save();

            Mask(canvas);
            islandObject.Draw(canvas);

            foreach(UIObject uiObject in objects)
            {
                canvas.RestoreToCount(saveState);

                if (uiObject.maskInToIsland)
                {
                    Mask(canvas);
                }

                uiObject.Draw(canvas);
            }
        }

        void Mask(SKCanvas canvas)
        {
            var islandMask = GetMask();
            canvas.ClipRoundRect(islandMask);
        }

        public SKRoundRect GetMask()
        {
            var islandMask = islandObject.GetRect();
            islandMask.Deflate(new SKSize(1, 1));
            return islandMask;
        }
    }

}
