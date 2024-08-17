using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicWin.Resources;
using DynamicWin.UI;
using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using OpenTK.Graphics;
using OpenTK;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace DynamicWin.Main
{
    public class RendererMain : SKElement
    {
        //private DispatcherTimer timer;

        private IslandObject islandObject;
        public IslandObject MainIsland { get => islandObject; }

        private List<UIObject> objects { get => MenuManager.Instance.ActiveMenu.UiObjects; }

        public static Vec2 ScreenDimensions { get => new Vec2(MainForm.Instance.Width, MainForm.Instance.Height); }
        public static Vec2 CursorPosition { get => new Vec2(Mouse.GetPosition(MainForm.Instance).X, Mouse.GetPosition(MainForm.Instance).Y); }

        private static RendererMain instance;
        public static RendererMain Instance { get { return instance; } }

        public Vec2 renderOffset = Vec2.zero;
        public Vec2 scaleOffset = Vec2.one;
        public float blurOverride = 0f;
        public float alphaOverride = 1f;

        public Action<float> onUpdate;
        public Action<SKCanvas> onDraw;

        public RendererMain()
        {
            MenuManager m = new MenuManager();

            // Init control

            instance = this;

            islandObject = new IslandObject();
            m.Init();

            // Set up the timer
            /*            timer = new System.Windows.Forms.Timer
                        {
                            Interval = 14
                        };
                        timer.Tick += (sender, args) => Update();
                        timer.Tick += (sender, args) => Render();
                        timer.Start();*/

            /*timer = new DispatcherTimer();
            timer.Tick += (sender, args) => Update();
            timer.Tick += (sender, args) => Render();
            timer.Interval = TimeSpan.FromMilliseconds(8);
            timer.Start();*/

            MainForm.Instance.onMainFormRender += Update;
            MainForm.Instance.onMainFormRender += Render;

            initialScreenBrightness = BrightnessAdjustMenu.GetBrightness();

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
            //timer.Stop();

            MainForm.Instance.onMainFormRender -= Update;
            MainForm.Instance.onMainFormRender -= Render;

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
            if (key == Keys.LWin && modifier.isCtrlDown)
            {
                islandObject.hidden = !islandObject.hidden;
            }

            if (key == Keys.VolumeDown || key == Keys.VolumeMute || key == Keys.VolumeUp)
            {
                if (MenuManager.Instance.ActiveMenu is HomeMenu)
                {
                    MenuManager.OpenOverlayMenu(new VolumeAdjustMenu(), 100f);
                }
                else
                {
                    if (VolumeAdjustMenu.timerUntilClose != null)
                        VolumeAdjustMenu.timerUntilClose = 0f;
                }
            }

            if(key == Keys.MediaNextTrack || key == Keys.MediaPreviousTrack)
            {
                if (MenuManager.Instance.ActiveMenu is HomeMenu)
                {
                    if (key == Keys.MediaNextTrack) Res.HomeMenu.NextSong(); else Res.HomeMenu.PrevSong();
                }
            }
        }

        float deltaTime = 0f;
        public float DeltaTime { get { return deltaTime; } private set => deltaTime = value; }

        Stopwatch? updateStopwatch;

        int initialScreenBrightness = 0;

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

            updateStopwatch = new Stopwatch();
            updateStopwatch.Start();

            onUpdate?.Invoke(DeltaTime);

            if(BrightnessAdjustMenu.GetBrightness() != initialScreenBrightness)
            {
                initialScreenBrightness = BrightnessAdjustMenu.GetBrightness();
                if (MenuManager.Instance.ActiveMenu is HomeMenu)
                {
                    MenuManager.OpenOverlayMenu(new BrightnessAdjustMenu(), 100f);
                }
                else
                {
                    if (BrightnessAdjustMenu.timerUntilClose != null)
                    {
                        BrightnessAdjustMenu.PressBK();
                        BrightnessAdjustMenu.timerUntilClose = 0f;
                    }
                }
            }

            // Update Menu

            MenuManager.Instance.Update(DeltaTime);

            if (MenuManager.Instance.ActiveMenu != null)
                MenuManager.Instance.ActiveMenu.Update();

            if (MenuManager.Instance.ActiveMenu is DropFileMenu && !MainForm.Instance.isDragging)
                MenuManager.OpenMenu(Res.HomeMenu);

            // Update logic here

            islandObject.UpdateCall(DeltaTime);

            if (MainIsland.hidden) return;

            foreach (UIObject uiObject in objects)
            {
                uiObject.UpdateCall(DeltaTime);
            }
        }

        // Called once every frame to render frame, called after Update

        private void Render()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                this.InvalidateVisual();
            }));
        }

        public int canvasWithoutClip;
        bool isInitialized = false;

        GRContext Context;

/*        public SKSurface GetOpenGlSurface(int width, int height)
        {
            if (Context == null)
            {
                GLControl control = new GLControl(new GraphicsMode(32, 24, 8, 4));
                control.MakeCurrent();
                Context = GRContext.CreateGl();
            }
            var gpuSurface = SKSurface.Create(Context, true, new SKImageInfo(width, height));
            return gpuSurface;
        }*/

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            if (!isInitialized) return;

            // Render

            // Get the canvas and information about the surface
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;
            SKImageInfo info = e.Info;

            canvas.Clear(SKColors.Transparent);

            // Fix screen scale
            double dpiFactor = System.Windows.PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            canvas.Scale((float)dpiFactor, (float)dpiFactor, 0, 0);

            canvasWithoutClip = canvas.Save();

            if(islandObject.maskInToIsland) Mask(canvas);
            islandObject.DrawCall(canvas);

            if (MainIsland.hidden) return;

            bool hasContextMenu = false;
            foreach (UIObject uiObject in objects)
            {
                canvas.RestoreToCount(canvasWithoutClip);
                canvasWithoutClip = canvas.Save();

                if(uiObject.IsHovering && uiObject.GetContextMenu() != null)
                {
                    hasContextMenu = true;
                    var contextMenu = uiObject.GetContextMenu();
                    ContextMenu = contextMenu;
                }

                foreach(UIObject obj in uiObject.LocalObjects)
                {
                    if (obj.IsHovering && obj.GetContextMenu() != null)
                    {
                        hasContextMenu = true;
                        var contextMenu = obj.GetContextMenu();
                        ContextMenu = contextMenu;
                    }
                }

                if (uiObject.maskInToIsland)
                {
                    Mask(canvas);
                }

                canvas.Scale(scaleOffset.X, scaleOffset.Y, islandObject.Position.X + islandObject.Size.X / 2, islandObject.Position.Y + islandObject.Size.Y / 2);

                canvas.Translate(renderOffset.X, renderOffset.Y);
                uiObject.DrawCall(canvas);
            }

            onDraw?.Invoke(canvas);

            if (!hasContextMenu) ContextMenu = null;

            canvas.Flush();
        }

        void SetRenderScale(SKCanvas canvas, float scale, Vec2 point = null)
        {
            if (point == null) point = new Vec2(MainIsland.Position.X + MainIsland.currSize.X / 2, 0);
            canvas.Scale(scale, scale, point.X, point.Y);
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
