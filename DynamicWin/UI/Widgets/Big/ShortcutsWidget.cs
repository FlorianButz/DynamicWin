using DynamicWin.Resources;
using DynamicWin.UI.Menu;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.UIElements.Custom;
using DynamicWin.Utils;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Shapes;
using ThumbnailGenerator;

namespace DynamicWin.UI.Widgets.Big
{
    class RegisterShortcutsWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => false;
        public string WidgetName => "Shortcuts";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new ShortcutsWidget(parent, position, alignment);
        }
    }

    public class ShortcutsWidget : WidgetBase
    {
        public ShortcutsWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            AddLocalObject(new ShortcutButton(this, "s1", new Vec2(GetWidgetWidth() / 4, GetWidgetHeight() / 4),
                GetWidgetSize() / 2f, UIAlignment.TopLeft));
            AddLocalObject(new ShortcutButton(this, "s2", new Vec2(-GetWidgetWidth() / 4, GetWidgetHeight() / 4),
                GetWidgetSize() / 2f, UIAlignment.TopRight));
            AddLocalObject(new ShortcutButton(this, "s3", new Vec2(GetWidgetWidth() / 4, -GetWidgetHeight() / 4),
                GetWidgetSize() / 2f, UIAlignment.BottomLeft));
            AddLocalObject(new ShortcutButton(this, "s4", new Vec2(-GetWidgetWidth() / 4, -GetWidgetHeight() / 4),
                GetWidgetSize() / 2f, UIAlignment.BottomRight));
        }

        public override void DrawWidget(SKCanvas canvas)
        {
            base.DrawWidget(canvas);

            var paint = GetPaint();
            paint.Color = GetColor(Theme.WidgetBackground).Value();
            canvas.DrawRoundRect(GetRect(), paint);
        }
    }

    internal class ShortcutButton : DWButton
    {
        public ShortcutSave savedShortcut;
        string saveId;

        DWText shortcutTitle;
        DWImage shortcutIcon;

        public ShortcutButton(UIObject? parent, string shortcutSaveId, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size - 12.5f, null, alignment)
        {
            roundRadius = 15;
            hoverScaleMulti = Vec2.one * 1;
            clickScaleMulti = Vec2.one * 1;
            scaleSecondOrder.SetValues(4.5f, 1, 0.1f);

            if(SaveManager.Contains(shortcutSaveId))
            {
                savedShortcut = (ShortcutSave)SaveManager.Get(shortcutSaveId);
            }
            else
            {
                savedShortcut = new ShortcutSave();
            }

            shortcutTitle = new DWText(this, " ", new Vec2(15f, 0), UIAlignment.MiddleLeft);
            shortcutTitle.TextSize = 9.5f;
            shortcutTitle.Anchor.X = 0f;
            shortcutTitle.Font = Res.InterBold;
            shortcutTitle.Color = Theme.TextSecond;
            shortcutTitle.SilentSetActive(false);
            AddLocalObject(shortcutTitle);

            shortcutIcon = new DWImage(this, Res.FileIcon, new Vec2(0, 0), new Vec2(20, 20), UIAlignment.MiddleLeft);
            shortcutIcon.SilentSetActive(false);
            AddLocalObject(shortcutIcon);

            clickCallback = () => { RunShortcut(); };

            this.saveId = shortcutSaveId;
            LoadShortcut();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            shortcutTitle.SetActive(!string.IsNullOrEmpty(savedShortcut.path));
            shortcutIcon.SetActive(!string.IsNullOrEmpty(savedShortcut.path));
        }

        public override void Draw(SKCanvas canvas)
        {
            var rect = GetRect();
            //rect.Deflate(7.5f, 5);
            
            var paint = GetPaint();

            if(string.IsNullOrEmpty(savedShortcut.path))
            {
                float[] intervals = [5, 5];
                paint.PathEffect = SKPathEffect.CreateDash(intervals, 0f);

                paint.IsStroke = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;
                paint.StrokeWidth = 2f;

                canvas.DrawRoundRect(rect, paint);
            }
            else
            {
                canvas.DrawRoundRect(rect, paint);

                paint.Color = Theme.Primary.Override(a: 0.35f).Value();
                paint.IsStroke = true;
                paint.StrokeWidth = 2f;

                canvas.DrawRoundRect(rect, paint);
            }
        }

        public override ContextMenu? GetContextMenu()
        {
            var ctx = new ContextMenu();

            if (string.IsNullOrEmpty(savedShortcut.path))
            {
                var config = new MenuItem() { Header = "Configure Shortcut" };
                config.Click += (s, e) =>
                {
                    ConfigureShortcut();
                };
                ctx.Items.Add(config);
            }
            else
            {
                var run = new MenuItem() { Header = "Run Shortcut" };
                run.Click += (s, e) =>
                {
                    RunShortcut();
                };
                ctx.Items.Add(run);

                var remove = new MenuItem() { Header = "Remove Shortcut" };
                remove.Click += (s, e) =>
                {
                    RemoveShortcut();
                };
                ctx.Items.Add(remove);
            }

            return ctx;
        }

        internal void SetShortcut(ShortcutSave save)
        {
            if(!string.IsNullOrEmpty(save.path))
            {
                this.savedShortcut = save;

                SaveManager.Add("shortcuts." + saveId, JsonConvert.SerializeObject(save));
                SaveManager.SaveAll();

                UpdateDisplay();
            }
        }

        Bitmap thumbnail;
        
        void UpdateDisplay()
        {
            shortcutTitle.SilentSetText(DWText.Truncate(string.IsNullOrEmpty(savedShortcut.name) ? " " : savedShortcut.name, 9));

            Task.Run(() =>
            {
                try
                {
                    int THUMB_SIZE = 256;
                    thumbnail = WindowsThumbnailProvider.GetThumbnail(
                       savedShortcut.path, THUMB_SIZE, THUMB_SIZE, ThumbnailOptions.None);
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    System.Diagnostics.Debug.WriteLine("Could not load icon.");

                    new Thread(() =>
                    {
                        try
                        {
                            Thread.Sleep(1500);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            return;
                        }

                        UpdateDisplay();
                    }).Start();

                }
                catch (FileNotFoundException fnfE)
                {
                    return;
                }
                finally
                {
                    SKBitmap bMap = null;
                    if(thumbnail != null) bMap = thumbnail.ToSKBitmap();
                    else bMap = Resources.Res.FileIcon;

                    shortcutIcon.Image = bMap;

                    if (thumbnail != null)
                        thumbnail.Dispose();
                }

                SetActive(true);
            });
        }

        void LoadShortcut()
        {
            if (!SaveManager.Contains("shortcuts." + saveId)) return;
            var shortcut = (ShortcutSave)JsonConvert.DeserializeObject<ShortcutSave>((string)SaveManager.Get("shortcuts." + saveId));

            if(!string.IsNullOrEmpty(shortcut.path))
            {
                savedShortcut = shortcut;
                UpdateDisplay();
            }
        }

        void RunShortcut()
        {
            if (string.IsNullOrEmpty(savedShortcut.path))
            {
                ConfigureShortcut();
                return;
            }

            OpenWithDefaultProgram(savedShortcut.path);
        }

        void ConfigureShortcut()
        {
            MenuManager.OpenMenu(new ConfigureShortcutMenu(this));
        }

        void OpenWithDefaultProgram(string path)
        {
            if (!File.Exists(path))
            {
                RemoveShortcut();
                return;
            }

            using Process fileopener = new Process();

            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }

        void RemoveShortcut()
        {
            if (!SaveManager.Contains("shortcuts." + saveId)) return;
            SaveManager.Remove(("shortcuts." + saveId));
            SaveManager.SaveAll();

            savedShortcut = new ShortcutSave();
        }

        public struct ShortcutSave
        {
            public string path; // The path to the app / url to open
            public string name; // The displayname of the shortcut
        }
    }
}
