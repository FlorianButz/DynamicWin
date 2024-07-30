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

            islandObject.Update(DeltaTime);

            foreach (UIObject uiObject in objects)
            {
                uiObject.Update(DeltaTime);
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
            canvas.Clear(SKColors.Transparent);

            var islandMask = islandObject.GetRect();
            
            islandMask.Deflate(new SKSize(1, 1));
            canvas.ClipRoundRect(islandMask);

            islandObject.Draw(canvas);

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

            foreach(UIObject uiObject in objects)
            {
                canvas.RestoreToCount(saveState);
                uiObject.Draw(canvas);
            }
        }
    }

}
