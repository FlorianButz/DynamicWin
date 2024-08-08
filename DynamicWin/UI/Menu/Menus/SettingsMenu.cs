using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicWin.UI.Menu.Menus
{
    public class SettingsMenu : BaseMenu
    {

        public SettingsMenu()
        {
            MainForm.onScrollEvent += (MouseWheelEventArgs x) => 
            {
                yScrollOffset += x.Delta * 0.25f;
            };
        }

        bool changedTheme = false;

        void SaveAndBack()
        {
            Settings.AllowBlur = allowBlur.IsChecked;
            Settings.AllowAnimation = allowAnimation.IsChecked;
            Settings.AntiAliasing = antiAliasing.IsChecked;

            if (changedTheme)
                Theme.Instance.UpdateTheme(true);
            else
            {
                Res.HomeMenu = new HomeMenu();
                MenuManager.OpenMenu(Res.HomeMenu);
            }

            Settings.Save();
        }

        Checkbox allowBlur;
        Checkbox allowAnimation;
        Checkbox antiAliasing;

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            var backBtn = new DWTextButton(island, "Apply and Back", new Vec2(0, -45), new Vec2(350, 40), () => { SaveAndBack(); }, UIAlignment.BottomCenter)
            {
                roundRadius = 25
            };
            backBtn.Text.Font = Resources.Res.InterBold;

            objects.Add(backBtn);

            var generalTitle = new DWText(island, "General", new Vec2(25, 0), UIAlignment.TopLeft);
            generalTitle.Font = Res.InterBold;
            generalTitle.Anchor.X = 0;
            objects.Add(generalTitle);


            {
                var islandModesTitle = new DWText(island, "Island Mode", new Vec2(25, 0), UIAlignment.TopLeft);
                islandModesTitle.Font = Res.InterRegular;
                islandModesTitle.TextSize = 15;
                islandModesTitle.Anchor.X = 0;
                objects.Add(islandModesTitle);

                var islandModes = new string[] { "Island", "Notch" };
                var islandMode = new MultiSelectionButton(island, islandModes, new Vec2(25, 0), new Vec2(IslandSize().X - 50, 25), UIAlignment.TopLeft);
                islandMode.SelectedIndex = (Settings.IslandMode == IslandObject.IslandMode.Island) ? 0 : 1;
                islandMode.Anchor.X = 0;
                islandMode.onClick += (index) =>
                {
                    Settings.IslandMode = (index == 0) ? IslandObject.IslandMode.Island : IslandObject.IslandMode.Notch;
                };
                objects.Add(islandMode);
            }

            allowBlur = new Checkbox(island, "Allow Blur", new Vec2(25, 0), new Vec2(25, 25), () => { }, UIAlignment.TopLeft);
            allowBlur.IsChecked = Settings.AllowBlur;
            allowBlur.Anchor.X = 0;
            objects.Add(allowBlur);

            allowAnimation = new Checkbox(island, "Allow SO Animation", new Vec2(25, 0), new Vec2(25, 25), () => { }, UIAlignment.TopLeft);
            allowAnimation.IsChecked = Settings.AllowAnimation;
            allowAnimation.Anchor.X = 0;
            objects.Add(allowAnimation);

            antiAliasing = new Checkbox(island, "Anti Aliasing", new Vec2(25, 0), new Vec2(25, 25), () => { }, UIAlignment.TopLeft);
            antiAliasing.IsChecked = Settings.AntiAliasing;
            antiAliasing.Anchor.X = 0;
            objects.Add(antiAliasing);

            {
                var themeTitle = new DWText(island, "Themes", new Vec2(25, 0), UIAlignment.TopLeft);
                themeTitle.Font = Res.InterRegular;
                themeTitle.TextSize = 15;
                themeTitle.Anchor.X = 0;
                objects.Add(themeTitle);

                var themeOptions = new string[] { "Custom", "Dark", "Light" };
                var theme = new MultiSelectionButton(island, themeOptions, new Vec2(25, 0), new Vec2(IslandSize().X - 50, 25), UIAlignment.TopLeft);
                theme.SelectedIndex = Settings.Theme + 1;
                theme.Anchor.X = 0;
                theme.onClick += (index) =>
                {
                    Settings.Theme = index - 1;
                    changedTheme = true;
                };
                objects.Add(theme);
            }

            var widgetsTitle = new DWText(island, "Widgets", new Vec2(25, 0), UIAlignment.TopLeft);
            widgetsTitle.Font = Res.InterBold;
            widgetsTitle.Anchor.X = 0;
            objects.Add(widgetsTitle);

            {
                var wTitle = new DWText(island, "Small Widgets Left", new Vec2(25, 0), UIAlignment.TopLeft);
                wTitle.Font = Res.InterRegular;
                wTitle.TextSize = 15;
                wTitle.Anchor.X = 0;
                objects.Add(wTitle);
            }

            {
                var wTitle = new DWText(island, "Small Widgets Middle", new Vec2(25, 0), UIAlignment.TopLeft);
                wTitle.Font = Res.InterRegular;
                wTitle.TextSize = 15;
                wTitle.Anchor.X = 0;
                objects.Add(wTitle);
            }

            {
                var wTitle = new DWText(island, "Small Widgets Right", new Vec2(25, 0), UIAlignment.TopLeft);
                wTitle.Font = Res.InterRegular;
                wTitle.TextSize = 15;
                wTitle.Anchor.X = 0;
                objects.Add(wTitle);
            }

            {
                var wTitle = new DWTextImageButton(island, Res.Add, "Big Widgets", new Vec2(25, 0), new Vec2(125, 25), null, UIAlignment.TopLeft);

                wTitle.clickCallback = () =>
                {
                    var ctx = new System.Windows.Controls.ContextMenu();

                    foreach (var availableWidget in Res.availableBigWidgets)
                    {
                        if (Settings.bigWidgets.Contains(availableWidget.GetType().FullName)) continue;

                        var item = new MenuItem() { Header = availableWidget.GetType().Namespace.Split('.')[0] + ": " + availableWidget.WidgetName };
                        item.Click += (x, y) =>
                        {
                            Settings.bigWidgets.Add(availableWidget.GetType().FullName);
                            System.Diagnostics.Debug.WriteLine(availableWidget.GetType().FullName);

                            wTitle.SetActive(true);
                        };

                        ctx.Items.Add(item);
                    }

                    MainForm.Instance.ContextMenu = ctx;
                    MainForm.Instance.ContextMenu.IsOpen = true;
                    MainForm.Instance.ContextMenu = null;

                    wTitle.SilentSetActive(false);
                };

                wTitle.Text.Font = Res.InterRegular;
                wTitle.Text.alignment = UIAlignment.MiddleLeft;
                wTitle.Text.Anchor.X = 0f;
                wTitle.Text.LocalPosition.X = 37.5f;
                wTitle.Text.TextSize = 15;
                wTitle.hoverScaleMulti = Vec2.one * 1.05f;
                wTitle.clickScaleMulti = Vec2.one * 0.975f;
                wTitle.Anchor.X = 0;

                objects.Add(wTitle);
            }

            return objects;
        }

        float yScrollOffset = 0f;
        float ySmoothScroll = 0f;

        public override void Update()
        {
            base.Update();

            ySmoothScroll = Mathf.Lerp(ySmoothScroll,
                yScrollOffset, 5f * RendererMain.Instance.DeltaTime);

            var yPos = 35f;
            var spacing = 15f;

            for(int i = 1; i < 9; i++)
            {
                var uiObject = UiObjects[i];
                uiObject.LocalPosition.Y = yPos + ySmoothScroll;
                yPos += uiObject.Size.Y + spacing;
            }

            yPos = 35f;

            for (int i = 9; i < UiObjects.Count; i++)
            {
                var uiObject = UiObjects[i];
                uiObject.LocalPosition.Y = yPos + ySmoothScroll;
                uiObject.LocalPosition.X = 275;

                yPos += uiObject.Size.Y + spacing;
            }

            yScrollOffset = Mathf.Lerp(yScrollOffset,
                Mathf.Clamp(yScrollOffset, 0f, -yPos), 15f * RendererMain.Instance.DeltaTime);
        }

        public override Vec2 IslandSize()
        {
            return new Vec2(745, 470);
        }

        public override Vec2 IslandSizeBig()
        {
            return new Vec2(750, 475);
        }
    }

    internal class Checkbox : DWImageButton
    {
        bool isChecked = false;
        public bool IsChecked { get { return isChecked; } set => SetChecked(value); }

        void SetChecked(bool isChecked)
        {
            this.isChecked = isChecked;
            Image.Image = isChecked ? Res.Check : null;
        }

        public Checkbox(UIObject? parent, string buttonText, Vec2 position, Vec2 size, Action clickCallback, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, Res.Check, position, size, clickCallback, alignment)
        {
            var text = new DWText(this, buttonText, new Vec2(15, 0), UIAlignment.MiddleRight);
            text.Color = Theme.TextSecond;
            text.Anchor.X = 0;
            text.TextSize = size.Y / 1.5f;
            AddLocalObject(text);

            SetChecked(false);

            hoverScaleMulti = new Vec2(1.05f, 1f);
            clickScaleMulti = new Vec2(0.975f, 1f);
        }

        public override void OnMouseUp()
        {
            base.OnMouseUp();
            IsChecked = !IsChecked;
        }
    }

    internal class MultiSelectionButton : UIObject
    {
        string[] options;
        DWTextButton[] buttons;

        public Action<int> onClick;

        int selectedIndex = 0;
        public int SelectedIndex { get => selectedIndex; set => SetSelected(value); }

        public MultiSelectionButton(UIObject? parent, string[] options, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            this.options = options;
            this.buttons = new DWTextButton[options.Length];

            float xPos = 0;
            for(int i = 0; i < options.Length; i++)
            {
                var index = i;
                var action = () => { OnClick(index); };

                var btn = new DWTextButton(this, options[i], new Vec2(xPos, 0f), new Vec2(75, 25), action, UIAlignment.MiddleLeft);
                btn.Text.Color = Theme.TextSecond;
                btn.Anchor.X = 0;
                buttons[i] = btn;
                
                AddLocalObject(btn);

                xPos += btn.Size.X + 15;
            }

            SelectedIndex = 0;
        }

        public override void Draw(SKCanvas canvas)
        {
        }

        void SetSelected(int index)
        {
            selectedIndex = index;

            foreach (var button in buttons)
            {
                button.normalColor = Theme.Secondary.Override(a: 0.9f);
            }

            buttons[index].normalColor = (Theme.Primary * 0.65f).Override(a: 0.75f);
        }

        void OnClick(int index)
        {
            SelectedIndex = index;

            onClick.Invoke(index);
        }
    }
}
