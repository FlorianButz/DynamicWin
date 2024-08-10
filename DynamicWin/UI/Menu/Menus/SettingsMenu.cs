using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.Widgets;
using DynamicWin.UI.Widgets.Small;
using DynamicWin.Utils;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Xml.Linq;
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

            var generalTitle = new DWText(island, "General", new Vec2(25, 0), UIAlignment.TopLeft);
            generalTitle.Font = Res.InterBold;
            generalTitle.Anchor.X = 0;
            objects.Add(generalTitle);


            {
                var islandModesTitle = new DWText(island, "Island Mode", new Vec2(25, 0), UIAlignment.TopLeft);
                islandModesTitle.Font = Res.InterRegular;
                islandModesTitle.Color = Theme.TextSecond;
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
            widgetsTitle.Color = Theme.TextSecond;
            widgetsTitle.Anchor.X = 0;
            objects.Add(widgetsTitle);

            {
                var wTitle = new DWText(island, "Small Widgets (Right click to add / edit)", new Vec2(25, 0), UIAlignment.TopLeft);
                wTitle.Font = Res.InterRegular;
                wTitle.Color = Theme.TextSecond;
                wTitle.TextSize = 15;
                wTitle.Anchor.X = 0;
                objects.Add(wTitle);

                smallWidgetAdder = new SmallWidgetAdder(island, Vec2.zero, new Vec2(IslandSize().X - 50, 35), UIAlignment.TopCenter);
                objects.Add(smallWidgetAdder);
            }

            objects.Add(new DWText(island, " ", new Vec2(25, 0), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                Anchor = new Vec2(0, 0.5f),
                TextSize = 5
            });

            {
                var wTitle = new DWText(island, "Big Widgets (Right click to add / edit)", new Vec2(25, 15), UIAlignment.TopLeft);
                wTitle.Font = Res.InterRegular;
                wTitle.Color = Theme.TextSecond;
                wTitle.TextSize = 15;
                wTitle.Anchor.X = 0;
                objects.Add(wTitle);

                bigWidgetAdder = new BigWidgetAdder(island, Vec2.zero, new Vec2(IslandSize().X - 50, 35), UIAlignment.TopCenter);
                objects.Add(bigWidgetAdder);
            }


            objects.Add(new DWText(island, " ", new Vec2(25, 0), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                Anchor = new Vec2(0, 0.5f),
                TextSize = 20
            });

            objects.Add(new DWText(island, "Software Version: " + DynamicWinMain.Version, new Vec2(25, 0), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                Anchor = new Vec2(0, 0.5f),
                TextSize = 15
            });

            objects.Add(new DWText(island, "Licensed under the MIT license.", new Vec2(25, 0), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                Anchor = new Vec2(0, 0.5f),
                TextSize = 15
            });

            var backBtn = new DWTextButton(island, "Apply and Back", new Vec2(0, -45), new Vec2(350, 40), () => { SaveAndBack(); }, UIAlignment.BottomCenter)
            {
                roundRadius = 25
            };
            backBtn.Text.Font = Resources.Res.InterBold;

            bottomMask = new UIObject(island, Vec2.zero, new Vec2(IslandSizeBig().X + 100, 200), UIAlignment.BottomCenter)
            {
                Color = Theme.IslandBackground
            };

            objects.Add(bottomMask);
            objects.Add(backBtn);

            return objects;
        }

        UIObject bottomMask;
        SmallWidgetAdder smallWidgetAdder;
        BigWidgetAdder bigWidgetAdder;

        float yScrollOffset = 0f;
        float ySmoothScroll = 0f;

        public override void Update()
        {
            base.Update();

            ySmoothScroll = Mathf.Lerp(ySmoothScroll,
                yScrollOffset, 10f * RendererMain.Instance.DeltaTime);

            bottomMask.blurAmount = 15;

            var yScrollLim = 0f;
            var yPos = 35f;
            var spacing = 15f;

            for(int i = 0; i < UiObjects.Count - 2; i++)
            {
                var uiObject = UiObjects[i];
                uiObject.LocalPosition.Y = yPos + ySmoothScroll;
                yPos += uiObject.Size.Y + spacing;

                if (yPos > IslandSize().Y - 45) yScrollLim += uiObject.Size.Y + spacing;
            }

            yScrollOffset = Mathf.Lerp(yScrollOffset,
                Mathf.Clamp(yScrollOffset, -yScrollLim, 0f), 15f * RendererMain.Instance.DeltaTime);
        }

        public override Vec2 IslandSize()
        {
            var vec = new Vec2(450, 475);

            if(smallWidgetAdder != null)
            {
                vec.X = Math.Max(vec.X, smallWidgetAdder.Size.X + 50);
            }

            return vec;
        }

        public override Vec2 IslandSizeBig()
        {
            return IslandSize() + 5;
        }
    }

    internal class BigWidgetAdder : UIObject
    {
        AddNew addNew;

        public BigWidgetAdder(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            Color = Theme.WidgetBackground.Override(a: 0.1f);
            roundRadius = 20;

            Anchor.Y = 0;

            addNew = new AddNew(this, Vec2.zero, new Vec2(size.X, 45), UIAlignment.BottomLeft);
            addNew.Anchor.Y = 0;
            AddLocalObject(addNew);

            UpdateWidgetDisplay();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            int line = (int)(Math.Floor(displays.Count / maxE));

            addNew.LocalPosition.Y  = Mathf.Lerp(addNew.LocalPosition.Y, -line * 45 - 45, 15f * deltaTime);
            addNew.LocalPosition.X  = Mathf.Lerp(addNew.LocalPosition.X, isDisplayEven() ? Size.X / 2f : Size.X / 1.3333333f, 15f * deltaTime);
            addNew.Size.X           = Mathf.Lerp(addNew.Size.X, isDisplayEven() ? Size.X : Size.X / 2, 15f * deltaTime);

            var lines2 = (int)Math.Max(1, (displays.Count / maxE + 1));
            Size.Y = Mathf.Lerp(Size.Y, lines2 * 45, 15f * RendererMain.Instance.DeltaTime);
        }

        bool isDisplayEven()
        {
            return displays.Count % 2 == 0;
        }

        List<BigWidgetAdderDisplay> displays = new List<BigWidgetAdderDisplay>();
        float maxE = 2;

        void UpdateWidgetDisplay()
        {
            displays.ForEach((x) => DestroyLocalObject(x));
            displays.Clear();

            Dictionary<string, IRegisterableWidget> bigWidgets = new Dictionary<string, IRegisterableWidget>();


            foreach (var widget in Res.availableBigWidgets)
            {
                if (bigWidgets.ContainsKey(widget.GetType().FullName)) continue;
                bigWidgets.Add(widget.GetType().FullName, widget);
                System.Diagnostics.Debug.WriteLine(widget.GetType().FullName);
            }

            float xPos = 0f;

            int c = 0;
            foreach (var bigWidget in Settings.bigWidgets)
            {
                if (!bigWidgets.ContainsKey(bigWidget)) continue;

                var widget = bigWidgets[bigWidget.ToString()];

                var display = new BigWidgetAdderDisplay(this, widget.WidgetName, UIAlignment.BottomLeft);

                display.onEditRemoveWidget += () => {
                    Settings.bigWidgets.Remove(bigWidget);
                    UpdateWidgetDisplay();
                };

                display.onEditMoveWidgetRight += () => {
                    int index = Math.Clamp(Settings.bigWidgets.IndexOf(bigWidget) + 1, 0, Settings.bigWidgets.Count - 1);
                    Settings.bigWidgets.Remove(bigWidget);

                    Settings.bigWidgets.Insert(index, bigWidget);
                    UpdateWidgetDisplay();
                };

                display.onEditMoveWidgetLeft += () => {
                    int index = Math.Clamp(Settings.bigWidgets.IndexOf(bigWidget) - 1, 0, Settings.bigWidgets.Count - 1);
                    Settings.bigWidgets.Remove(bigWidget);

                    Settings.bigWidgets.Insert(index, bigWidget);
                    UpdateWidgetDisplay();
                };

                int line = (int)(Math.Round(c / maxE));

                display.LocalPosition.X = xPos - (line * Size.X);
                display.LocalPosition.Y -= 45 + line * 45;
                xPos += display.Size.X;

                displays.Add(display);
                AddLocalObject(display);

                c++;
            }
        }

        public override ContextMenu? GetContextMenu()
        {
            var ctx = new System.Windows.Controls.ContextMenu();
            bool anyWidgetsLeft = false;

            foreach (var availableWidget in Res.availableBigWidgets)
            {
                if (Settings.bigWidgets.Contains(availableWidget.GetType().FullName)) continue;

                anyWidgetsLeft = true;

                var item = new MenuItem() { Header = availableWidget.GetType().Namespace.Split('.')[0] + ": " + availableWidget.WidgetName };
                item.Click += (x, y) =>
                {
                    Settings.bigWidgets.Add(availableWidget.GetType().FullName);
                    UpdateWidgetDisplay();
                };

                ctx.Items.Add(item);
            }

            if (!anyWidgetsLeft)
            {
                var ctx2 = new ContextMenu();
                ctx2.Items.Add(new MenuItem()
                {
                    Header = "No Widgets Available.",
                    IsEnabled = false
                });
                return ctx2;
            }

            return ctx;
        }
    }

    internal class AddNew : UIObject
    {
        public AddNew(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            AddLocalObject(new DWImage(this, Res.Add, Vec2.zero, new Vec2(15, 15), UIAlignment.Center)
            {
                Color = Theme.IconColor
            });

            Color = Theme.IconColor.Override(a: 0.4f);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();

            var placeRect = new SKRoundRect(SKRect.Create(Position.X, Position.Y, Size.X, Size.Y), 25);
            placeRect.Deflate(5, 5);

            float[] intervals = { 10, 10 };
            paint.PathEffect = SKPathEffect.CreateDash(intervals, 0f);

            paint.IsStroke = true;
            paint.StrokeCap = SKStrokeCap.Round;
            paint.StrokeJoin = SKStrokeJoin.Round;
            paint.StrokeWidth = 2f;

            canvas.DrawRoundRect(placeRect, paint);

            placeRect.Deflate(5f, 5f);
            paint.Color = Color.Override(a: 0.05f).Value();
            paint.IsStroke = false;

            canvas.DrawRoundRect(placeRect, paint);
        }
    }

    internal class BigWidgetAdderDisplay : UIObject
    {
        public BigWidgetAdderDisplay(UIObject? parent, string widgetName, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, Vec2.zero, Vec2.zero, alignment)
        {
            Size.X = parent.Size.X / 2;
            Size.Y = 45;

            Anchor = Vec2.zero;

            AddLocalObject(new DWText(this, DWText.Truncate(widgetName, 25), Vec2.zero, UIAlignment.Center)
            {
                TextSize = 14
            });

            roundRadius = 45;

            color = Theme.WidgetBackground.Override(a: 0.15f);
        }

        public override void Draw(SKCanvas canvas)
        {
            int canvasRestore = canvas.Save();

            var p = Position + Size / 2;
            canvas.Scale(this.s, this.s, p.X, p.Y);

            var paint = GetPaint();
            var rect = GetRect();

            paint.Color = color.Value();

            rect.Deflate(5, 5);
            canvas.DrawRoundRect(rect, paint);

            canvas.RestoreToCount(canvasRestore);
        }

        Col color;
        float s = 1;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            color.a = Mathf.Lerp(color.a, IsHovering ? 0.2f : 0.15f, 7.5f * deltaTime);
            s = Mathf.Lerp(s, IsHovering ? 1.025f : 1, 15f * deltaTime);
        }

        public Action onEditRemoveWidget;
        public Action onEditMoveWidgetLeft;
        public Action onEditMoveWidgetRight;

        public override ContextMenu? GetContextMenu()
        {
            var ctx = new System.Windows.Controls.ContextMenu();

            MenuItem remove = new MenuItem() { Header = "Remove" };
            remove.Click += (x, y) => onEditRemoveWidget?.Invoke();

            MenuItem pL = new MenuItem() { Header = "<- Push Left" };
            pL.Click += (x, y) => onEditMoveWidgetLeft?.Invoke();

            MenuItem pR = new MenuItem() { Header = "Push Right ->" };
            pR.Click += (x, y) => onEditMoveWidgetRight?.Invoke();

            ctx.Items.Add(remove);
            ctx.Items.Add(pL);
            ctx.Items.Add(pR);

            return ctx;
        }
    }

    internal class SmallWidgetAdder : UIObject
    {
        public SmallWidgetAdder(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            Color = Theme.WidgetBackground.Override(a: 0.1f);
            roundRadius = 25;

            container = new UIObject(this, Vec2.zero, new Vec2(size.X - 100, size.Y), UIAlignment.Center);
            container.Color = Col.Transparent;
            AddLocalObject(container);

            UpdateWidgetDisplay();
        }

        UIObject container;

        public List<SmallWidgetBase> smallLeftWidgets = new List<SmallWidgetBase>();
        public List<SmallWidgetBase> smallRightWidgets = new List<SmallWidgetBase>();
        public List<SmallWidgetBase> smallCenterWidgets = new List<SmallWidgetBase>();

        void UpdateWidgetDisplay()
        {
            smallRightWidgets.ForEach((x) => DestroyLocalObject(x));
            smallLeftWidgets.ForEach((x) => DestroyLocalObject(x));
            smallCenterWidgets.ForEach((x) => DestroyLocalObject(x));

            smallRightWidgets.Clear();
            smallLeftWidgets.Clear();
            smallCenterWidgets.Clear();

            Dictionary<string, IRegisterableWidget> smallWidgets = new Dictionary<string, IRegisterableWidget>();


            foreach (var widget in Res.availableSmallWidgets)
            {
                smallWidgets.Add(widget.GetType().FullName, widget);
                System.Diagnostics.Debug.WriteLine(widget.GetType().FullName);
            }

            foreach (var smallWidget in Settings.smallWidgetsMiddle)
            {
                if (!smallWidgets.ContainsKey(smallWidget)) continue;

                var widget = smallWidgets[smallWidget.ToString()];

                var instance = (SmallWidgetBase)widget.CreateWidgetInstance(container, Vec2.zero, UIAlignment.Center);
                instance.isEditMode = true;

                instance.onEditRemoveWidget += () => {
                    Settings.smallWidgetsMiddle.Remove(smallWidget);
                    UpdateWidgetDisplay();
                };

                instance.onEditMoveWidgetLeft += () => {
                    int index = Math.Clamp(Settings.smallWidgetsMiddle.IndexOf(smallWidget) + 1, 0, Settings.smallWidgetsMiddle.Count - 1);
                    Settings.smallWidgetsMiddle.Remove(smallWidget);

                    Settings.smallWidgetsMiddle.Insert(index, smallWidget);
                    UpdateWidgetDisplay();
                };

                instance.onEditMoveWidgetRight += () => {
                    int index = Math.Clamp(Settings.smallWidgetsMiddle.IndexOf(smallWidget) - 1, 0, Settings.smallWidgetsMiddle.Count - 1);
                    Settings.smallWidgetsMiddle.Remove(smallWidget);

                    Settings.smallWidgetsMiddle.Insert(index, smallWidget);
                    UpdateWidgetDisplay();
                };

                smallCenterWidgets.Add(instance);
            }

            foreach (var smallWidget in Settings.smallWidgetsLeft)
            {
                if (!smallWidgets.ContainsKey(smallWidget)) continue;

                var widget = smallWidgets[smallWidget.ToString()];

                var instance = (SmallWidgetBase)widget.CreateWidgetInstance(container, Vec2.zero, UIAlignment.MiddleLeft);
                instance.isEditMode = true;

                instance.onEditRemoveWidget += () => {
                    Settings.smallWidgetsLeft.Remove(smallWidget);
                    UpdateWidgetDisplay();
                };

                instance.onEditMoveWidgetLeft += () => {
                    int index = Math.Clamp(Settings.smallWidgetsLeft.IndexOf(smallWidget) + 1, 0, Settings.smallWidgetsLeft.Count - 1);
                    Settings.smallWidgetsLeft.Remove(smallWidget);

                    Settings.smallWidgetsLeft.Insert(index, smallWidget);
                    UpdateWidgetDisplay();
                };

                instance.onEditMoveWidgetRight += () => {
                    int index = Math.Clamp(Settings.smallWidgetsLeft.IndexOf(smallWidget) - 1, 0, Settings.smallWidgetsLeft.Count - 1);
                    Settings.smallWidgetsLeft.Remove(smallWidget);

                    Settings.smallWidgetsLeft.Insert(index, smallWidget);
                    UpdateWidgetDisplay();
                };

                smallLeftWidgets.Add(instance);
            }

            foreach (var smallWidget in Settings.smallWidgetsRight)
            {
                if (!smallWidgets.ContainsKey(smallWidget)) continue;

                var widget = smallWidgets[smallWidget.ToString()];

                var instance = (SmallWidgetBase)widget.CreateWidgetInstance(container, Vec2.zero, UIAlignment.MiddleRight);
                instance.isEditMode = true;

                instance.onEditRemoveWidget += () => {
                    Settings.smallWidgetsRight.Remove(smallWidget);
                    UpdateWidgetDisplay();
                };

                instance.onEditMoveWidgetLeft += () => {
                    int index = Math.Clamp(Settings.smallWidgetsRight.IndexOf(smallWidget) + 1, 0, Settings.smallWidgetsRight.Count - 1);
                    Settings.smallWidgetsRight.Remove(smallWidget);

                    Settings.smallWidgetsRight.Insert(index, smallWidget);
                    UpdateWidgetDisplay();
                };

                instance.onEditMoveWidgetRight += () => {
                    int index = Math.Clamp(Settings.smallWidgetsRight.IndexOf(smallWidget) - 1, 0, Settings.smallWidgetsRight.Count - 1);
                    Settings.smallWidgetsRight.Remove(smallWidget);

                    Settings.smallWidgetsRight.Insert(index, smallWidget);
                    UpdateWidgetDisplay();
                };

                smallRightWidgets.Add(instance);
            }

            smallCenterWidgets.ForEach((x) => AddLocalObject(x));
            smallLeftWidgets.ForEach((x) => AddLocalObject(x));
            smallRightWidgets.ForEach((x) => AddLocalObject(x));
        }

        public float smallWidgetsSpacing = 30;
        public float middleWidgetsSpacing = 35;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            { // Left Small Widgets
                float leftStackedPos = 15f;
                foreach (var smallLeft in smallLeftWidgets)
                {
                    smallLeft.Anchor.X = 0;
                    smallLeft.LocalPosition.X = leftStackedPos;

                    leftStackedPos += smallWidgetsSpacing + smallLeft.GetWidgetSize().X;
                }
            }

            { // Right Small Widgets
                float rightStackedPos = -15f;
                foreach (var smallRight in smallRightWidgets)
                {
                    smallRight.Anchor.X = 1;
                    smallRight.LocalPosition.X = rightStackedPos;

                    rightStackedPos -= smallWidgetsSpacing + smallRight.GetWidgetSize().X;
                }
            }

            { // Center Small Widgets
                float centerStackPos = 0f;
                foreach (var smallCenter in smallCenterWidgets)
                {
                    smallCenter.Anchor.X = 1;
                    smallCenter.LocalPosition.X = centerStackPos;

                    centerStackPos -= smallWidgetsSpacing + smallCenter.GetWidgetSize().X;
                }

                foreach (var smallCenter in smallCenterWidgets)
                {
                    smallCenter.LocalPosition.X -= centerStackPos / 2 + smallWidgetsSpacing;
                }
            }

            Vec2 size = Size;

            float sizeTogether = 0f;
            smallLeftWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);
            smallRightWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);
            smallCenterWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);

            sizeTogether += smallWidgetsSpacing * (smallCenterWidgets.Count + smallLeftWidgets.Count + smallRightWidgets.Count + 0.25f) + middleWidgetsSpacing;

            size.X = (float)Math.Max(size.X, sizeTogether);
        }

        public override ContextMenu? GetContextMenu()
        {
            var ctx = new System.Windows.Controls.ContextMenu();
            bool anyWidgetsLeft = false;

            MenuItem left = new MenuItem() { Header = "Left" };
            MenuItem middle = new MenuItem() { Header = "Middle" };
            MenuItem right = new MenuItem() { Header = "Right" };

            foreach (var availableWidget in Res.availableSmallWidgets)
            {
                if (Settings.smallWidgetsRight.Contains(availableWidget.GetType().FullName) ||
                    Settings.smallWidgetsLeft.Contains(availableWidget.GetType().FullName) ||
                    Settings.smallWidgetsMiddle.Contains(availableWidget.GetType().FullName)) continue;

                anyWidgetsLeft = true;

                var itemR = new MenuItem() { Header = availableWidget.GetType().Namespace.Split('.')[0] + ": " + availableWidget.WidgetName };
                itemR.Click += (x, y) =>
                {
                    Settings.smallWidgetsRight.Add(availableWidget.GetType().FullName);
                    UpdateWidgetDisplay();
                };

                var itemM = new MenuItem() { Header = availableWidget.GetType().Namespace.Split('.')[0] + ": " + availableWidget.WidgetName };
                itemM.Click += (x, y) =>
                {
                    Settings.smallWidgetsMiddle.Add(availableWidget.GetType().FullName);
                    UpdateWidgetDisplay();
                };

                var itemL = new MenuItem() { Header = availableWidget.GetType().Namespace.Split('.')[0] + ": " + availableWidget.WidgetName };
                itemL.Click += (x, y) =>
                {
                    Settings.smallWidgetsLeft.Add(availableWidget.GetType().FullName);
                    UpdateWidgetDisplay();
                };

                left.Items.Add(itemL);
                middle.Items.Add(itemM);
                right.Items.Add(itemR);
            }

            ctx.Items.Add(left);
            ctx.Items.Add(middle);
            ctx.Items.Add(right);

            if (!anyWidgetsLeft)
            {
                var ctx2 = new ContextMenu();
                ctx2.Items.Add(new MenuItem()
                {
                    Header = "No Widgets Available.",
                    IsEnabled = false
                });
                return ctx2;
            }

            return ctx;
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
