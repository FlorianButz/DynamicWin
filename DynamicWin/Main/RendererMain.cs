using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using DynamicWin.Resources;
using DynamicWin.UI;
using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace DynamicWin.Main
{
    internal class RendererMain : SKElement
    {
        private System.Windows.Forms.Timer timer;

        private IslandObject islandObject;
        public IslandObject MainIsland { get => islandObject; }

        private List<UIObject> objects { get => MenuManager.Instance.ActiveMenu.UiObjects; }

        public static Vec2 ScreenDimensions { get => new Vec2(MainForm.Instance.Width, MainForm.Instance.Height); }
        public static Vec2 CursorPosition { get => new Vec2(Mouse.GetPosition(MainForm.Instance).X, Mouse.GetPosition(MainForm.Instance).Y); }

        private static RendererMain instance;
        public static RendererMain Instance { get { return instance; } }

        public Vec2 renderOffset = Vec2.zero;

        public RendererMain()
        {
            MenuManager m = new MenuManager();
            Theme theme = new Theme();

            // Init control

            instance = this;

            islandObject = new IslandObject();
            m.Init();

            // Set up the timer
            timer = new System.Windows.Forms.Timer
            {
                Interval = 14
            };
            timer.Tick += (sender, args) => Update();
            timer.Tick += (sender, args) => Render();
            timer.Start();

            KeyHandler.onKeyDown += OnKeyRegistered;

            {
                MainForm.Instance.DragEnter += MainForm.Instance.MainForm_DragEnter;
                MainForm.Instance.DragLeave += MainForm.Instance.MainForm_DragLeave;
                MainForm.Instance.Drop += MainForm.Instance.OnDrop;

                MainForm.Instance.MouseWheel += MainForm.Instance.OnScroll;
            }

            isInitialized = true;
        }

        public void Destroy()
        {
            timer.Stop();
            timer.Dispose();

            KeyHandler.onKeyDown -= OnKeyRegistered;

            {
                MainForm.Instance.DragEnter -= MainForm.Instance.MainForm_DragEnter;
                MainForm.Instance.DragLeave -= MainForm.Instance.MainForm_DragLeave;

                MainForm.Instance.MouseWheel -= MainForm.Instance.OnScroll;
            }

            instance = null;
        }

        void OnKeyRegistered(Keys key, KeyModifier modifier)
        {
            if(key == Keys.LWin && modifier.isCtrlDown)
            {
                islandObject.hidden = !islandObject.hidden;
            }
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

            if (MenuManager.Instance.ActiveMenu is DropFileMenu && !MainForm.Instance.isDragging)
                MenuManager.OpenMenu(Res.HomeMenu);

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
            this.InvalidateVisual();
        }

        public int canvasWithoutClip;
        bool isInitialized = false;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            if (!isInitialized) return;

            // Render

            var canvas = e.Surface.Canvas;
            if (canvas == null) return;
            canvas.Clear(SKColors.Transparent);

            canvasWithoutClip = canvas.Save();

            if(islandObject.maskInToIsland) Mask(canvas);
            islandObject.DrawCall(canvas);

            if (MainIsland.hidden) return;

            bool hasContextMenu = false;
            foreach (UIObject uiObject in objects)
            {
                canvas.RestoreToCount(canvasWithoutClip);

                if(uiObject.IsHovering && uiObject.GetContextMenu() != null)
                {
                    hasContextMenu = true;

                    var contextMenu = uiObject.GetContextMenu();
                    //contextMenu.BackColor = Theme.IslandBackground.ValueSystem();
                    //contextMenu.ForeColor = Theme.TextMain.ValueSystem();

                    ContextMenu = contextMenu;
                }

                if (uiObject.maskInToIsland)
                {
                    Mask(canvas);
                }

                canvas.Translate(renderOffset.X, renderOffset.Y);
                uiObject.DrawCall(canvas);
            }

            if(!hasContextMenu) ContextMenu = null;

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
