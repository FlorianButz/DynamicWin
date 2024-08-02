using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DynamicWin.UI;
using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace DynamicWin.Main
{

    internal class RendererMain : SKControl
    {
        private System.Windows.Forms.Timer timer;

        private IslandObject islandObject;
        public IslandObject MainIsland { get => islandObject; }

        private List<UIObject> objects { get { return MenuManager.Instance.ActiveMenu.UiObjects; } }

        public static Vec2 ScreenDimensions { get => new Vec2(instance.Width, instance.Height); }
        public static Vec2 CursorPosition { get => new Vec2(Cursor.Position.X, Cursor.Position.Y); }

        private static RendererMain instance;
        public static RendererMain Instance { get { return instance; } }

        public RendererMain()
        {
            MenuManager m = new MenuManager();
            Theme theme = new Theme();

            instance = this;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            islandObject = new IslandObject();
            m.Init();

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
            }
            else
                deltaTime = 1f / 1000f;

            // Update Menu

            if (MenuManager.Instance.ActiveMenu != null)
                MenuManager.Instance.ActiveMenu.Update();

            // Update logic here

            islandObject.UpdateCall(DeltaTime);

            foreach (UIObject uiObject in objects)
            {
                uiObject.UpdateCall(DeltaTime);
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

        public int canvasWithoutClip;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            // Render

            var canvas = e.Surface.Canvas;
            if (canvas == null) return;
            canvas.Clear(SKColors.Transparent);

            canvasWithoutClip = canvas.Save();

            if(islandObject.maskInToIsland) Mask(canvas);
            islandObject.DrawCall(canvas);

            foreach(UIObject uiObject in objects)
            {
                canvas.RestoreToCount(canvasWithoutClip);

                if (uiObject.maskInToIsland)
                {
                    Mask(canvas);
                }

                uiObject.DrawCall(canvas);
            }

            canvas.Flush();
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
