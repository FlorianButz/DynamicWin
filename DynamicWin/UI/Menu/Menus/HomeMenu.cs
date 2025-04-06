using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.UIElements.Custom;
using DynamicWin.UI.Widgets;
using DynamicWin.UI.Widgets.Big;
using DynamicWin.UI.Widgets.Small;
using DynamicWin.Utils;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DynamicWin.UI.Menu.Menus
{
    public class HomeMenu : BaseMenu
    {
        public List<SmallWidgetBase> smallLeftWidgets = new List<SmallWidgetBase>();
        public List<SmallWidgetBase> smallRightWidgets = new List<SmallWidgetBase>();
        public List<SmallWidgetBase> smallCenterWidgets = new List<SmallWidgetBase>();

        public List<WidgetBase> bigWidgets = new List<WidgetBase>();

        float songSizeAddition = 0f;
        float songLocalPosXAddition = 0f;

        public void NextSong()
        {
            if (RendererMain.Instance.MainIsland.IsHovering) return;

            songSizeAddition = 45;
            songLocalPosXAddition = 45;
        }

        public void PrevSong()
        {
            if (RendererMain.Instance.MainIsland.IsHovering) return;

            songSizeAddition = 45;
            songLocalPosXAddition = -45;
        }

        public override Vec2 IslandSize()
        {
            Vec2 size = new Vec2(200, 35);

            float sizeTogether = 0f;
            smallLeftWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);
            smallRightWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);
            smallCenterWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);

            sizeTogether += smallWidgetsSpacing * (smallCenterWidgets.Count + smallLeftWidgets.Count + smallRightWidgets.Count + 0.25f) + middleWidgetsSpacing;

            size.X = (float)Math.Max(size.X, sizeTogether) + songSizeAddition;

            return size;
        }

        public override Vec2 IslandSizeBig()
        {
            Vec2 size = new Vec2(275, 145);

            {
                float sizeTogetherBiggest = 0f;
                float sizeTogether = 0f;

                for (int i = 0; i < bigWidgets.Count; i++)
                {
                    sizeTogether += bigWidgets[i].GetWidgetSize().X + bigWidgetsSpacing * 2;
                    if ((i) % maxBigWidgetInOneRow == 1)
                    {
                        sizeTogetherBiggest = (float)Math.Max(sizeTogetherBiggest, sizeTogether);
                        sizeTogether = 0f;
                    }
                }

                size.X = (float)Math.Max(size.X, sizeTogetherBiggest);
            }

            {
                float sizeTogetherBiggest = 0f;

                for (int i = 0; i < bigWidgets.Count; i++)
                {
                    if ((i) % maxBigWidgetInOneRow == 0)
                    {
                        sizeTogetherBiggest += bigWidgets[i].GetWidgetSize().Y;
                    }
                }

                sizeTogetherBiggest += bCD + (bigWidgetsSpacing * (int)Math.Floor((float)(bigWidgets.Count / maxBigWidgetInOneRow))) + topSpacing;

                // Set the container height to the total height of all rows
                size.Y = Math.Max(size.Y, sizeTogetherBiggest + topSpacing);
            }

            if (!isWidgetMode) size.Y = 250;

            return size;
        }

        UIObject smallWidgetsContainer;
        UIObject bigWidgetsContainer;

        List<UIObject> bigMenuItems = new List<UIObject>();

        UIObject topContainer;

        DWTextImageButton widgetButton;
        DWTextImageButton trayButton;

        Tray tray;

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            smallWidgetsContainer = new UIObject(island, Vec2.zero, IslandSize(), UIAlignment.Center);
            bigWidgetsContainer = new UIObject(island, Vec2.zero, IslandSize(), UIAlignment.Center);

            // Create elements

            topContainer = new UIObject(island, new Vec2(0, 30), new Vec2(island.currSize.X, 50))
            {
                Color = Col.Transparent
            };
            bigMenuItems.Add(topContainer);

            // Initialize next and previous images
            next = new DWImage(island, Resources.Res.Next, new Vec2(50, 0), new Vec2(30, 30), UIAlignment.Center)
            {
            };
            next.SilentSetActive(false);
            
            previous = new DWImage(island, Resources.Res.Previous, new Vec2(-50, 0), new Vec2(30, 30), UIAlignment.Center)
            {
            };
            previous.SilentSetActive(false);
            
            // Add them to objects list
            objects.Add(next);
            objects.Add(previous);

            widgetButton = new DWTextImageButton(topContainer, Resources.Res.Widgets, "Widgets", new Vec2(75 / 2 + 5, 0), new Vec2(75, 20), () =>
            {
                isWidgetMode = true;
            },
            UIAlignment.MiddleLeft);
            widgetButton.Text.alignment = UIAlignment.MiddleLeft;
            widgetButton.Text.Anchor.X = 0;
            widgetButton.Text.Position = new Vec2(28.5f, 0);
            widgetButton.normalColor = Col.Transparent;
            widgetButton.hoverColor = Col.Transparent;
            widgetButton.clickColor = Theme.Primary.Override(a: 0.35f);
            widgetButton.roundRadius = 25;

            bigMenuItems.Add(widgetButton);

            trayButton = new DWTextImageButton(topContainer, Resources.Res.Tray, "Tray", new Vec2(112.5f, 0), new Vec2(57.5f, 20), () =>
            {
                isWidgetMode = false;
            },
            UIAlignment.MiddleLeft);
            trayButton.Text.alignment = UIAlignment.MiddleLeft;
            trayButton.Text.Anchor.X = 0;
            trayButton.Text.Position = new Vec2(27.5f, 0);
            trayButton.normalColor = Col.Transparent;
            trayButton.hoverColor = Col.Transparent;
            trayButton.clickColor = Theme.Primary.Override(a: 0.35f);
            trayButton.roundRadius = 25;

            bigMenuItems.Add(trayButton);

            var settingsButton = new DWImageButton(topContainer, Resources.Res.Settings, new Vec2(-20f, 0), new Vec2(20, 20), () =>
            {
                //new SettingsWindow();
                MenuManager.OpenMenu(new SettingsMenu());
            },
            UIAlignment.MiddleRight);
            settingsButton.normalColor = Col.Transparent;
            settingsButton.hoverColor = Col.Transparent;
            settingsButton.clickColor = Theme.Primary.Override(a: 0.35f);
            settingsButton.roundRadius = 25;

            bigMenuItems.Add(settingsButton);

            tray = new Tray(island, new Vec2(0, -topSpacing * 1.5f), Vec2.zero, UIAlignment.BottomCenter)
            {
                Anchor = new Vec2(0.5f, 1f)
            };
            tray.SilentSetActive(false);
            bigMenuItems.Add(tray);

            // Get all widgets

            Dictionary<string, IRegisterableWidget> smallWidgets = new Dictionary<string, IRegisterableWidget>();
            Dictionary<string, IRegisterableWidget> widgets = new Dictionary<string, IRegisterableWidget>();

            foreach (var widget in Res.availableSmallWidgets)
            {
                if (smallWidgets.ContainsKey(widget.GetType().FullName)) continue;
                smallWidgets.Add(widget.GetType().FullName, widget);
                System.Diagnostics.Debug.WriteLine(widget.GetType().FullName);
            }

            foreach (var widget in Res.availableBigWidgets)
            {
                if (widgets.ContainsKey(widget.GetType().FullName)) continue;
                widgets.Add(widget.GetType().FullName, widget);
                System.Diagnostics.Debug.WriteLine(widget.GetType().FullName);
            }

            // Create widgets

            foreach (var smallWidget in Settings.smallWidgetsMiddle)
            {
                if (!smallWidgets.ContainsKey(smallWidget.ToString())) continue;
                var widget = smallWidgets[smallWidget.ToString()];

                smallCenterWidgets.Add((SmallWidgetBase)widget.CreateWidgetInstance(smallWidgetsContainer, Vec2.zero, UIAlignment.Center));
            }

            foreach (var smallWidget in Settings.smallWidgetsLeft)
            {
                if (!smallWidgets.ContainsKey(smallWidget.ToString())) continue;
                var widget = smallWidgets[smallWidget.ToString()];

                smallLeftWidgets.Add((SmallWidgetBase)widget.CreateWidgetInstance(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleLeft));
            }

            foreach (var smallWidget in Settings.smallWidgetsRight)
            {
                if (!smallWidgets.ContainsKey(smallWidget.ToString())) continue;
                var widget = smallWidgets[smallWidget.ToString()];

                smallRightWidgets.Add((SmallWidgetBase)widget.CreateWidgetInstance(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleRight));
            }

            foreach (var bigWidget in Settings.bigWidgets)
            {
                if (!widgets.ContainsKey(bigWidget.ToString())) continue;
                var widget = widgets[bigWidget.ToString()];

                bigWidgets.Add((WidgetBase)widget.CreateWidgetInstance(bigWidgetsContainer, Vec2.zero, UIAlignment.BottomCenter));
            }

            smallLeftWidgets.ForEach(x => {
                objects.Add(x);
            });

            smallRightWidgets.ForEach(x => {
                objects.Add(x);
            });

            smallCenterWidgets.ForEach(x => {
                objects.Add(x);
            });

            bigWidgets.ForEach(x => {
                objects.Add(x);
                x.SilentSetActive(false);
            });

            // Add lists

            bigMenuItems.ForEach(x =>
            {
                objects.Add(x);
                x.SilentSetActive(false);
                });

            // Initialize next and previous images
            next = new DWImage(island, Resources.Res.Next, new Vec2(50, 0), new Vec2(30, 30), UIAlignment.Center)
            {
            };
            next.SilentSetActive(false);
            objects.Add(next);
            
            previous = new DWImage(island, Resources.Res.Previous, new Vec2(-50, 0), new Vec2(30, 30), UIAlignment.Center)
            {
            };
            previous.SilentSetActive(false);
            objects.Add(previous);

            return objects;
        }

        DWImage next;
        DWImage previous;

        public float topSpacing = 15;
        public float bigWidgetsSpacing = 15;
        int maxBigWidgetInOneRow = 2;

        public float smallWidgetsSpacing = 10;
        public float middleWidgetsSpacing = 35;

        float sCD = 35;
        float bCD = 50;

        public bool isWidgetMode = true;

        int cycle = 0;

        public override void Update()
        {
            tray.Size = new Vec2(topContainer.Size.X, IslandSizeBig().Y - bCD - topSpacing - topContainer.Size.Y / 2);

            if(cycle % 32 == 0)
            {
                var count = Tray.FileCount;
                trayButton.Text.Text = "Tray      " + (count > 0 ? count : "");
            }

            // Enable / Disable small widgets

            smallLeftWidgets.ForEach(x => x.SetActive(!RendererMain.Instance.MainIsland.IsHovering));
            smallCenterWidgets.ForEach(x => x.SetActive(!RendererMain.Instance.MainIsland.IsHovering));
            smallRightWidgets.ForEach(x => x.SetActive(!RendererMain.Instance.MainIsland.IsHovering));

            // Enable / Disable big widgets / Tray

            tray.SetActive(RendererMain.Instance.MainIsland.IsHovering && !isWidgetMode);
            bigWidgets.ForEach(x => x.SetActive(RendererMain.Instance.MainIsland.IsHovering && isWidgetMode));
            bigMenuItems.ForEach(x =>
            {
                if(!(x is Tray))
                {
                    x.SetActive(RendererMain.Instance.MainIsland.IsHovering);
                }
            });

            widgetButton.normalColor = Col.Lerp(widgetButton.normalColor, isWidgetMode ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);
            trayButton.normalColor = Col.Lerp(trayButton.normalColor, (!isWidgetMode) ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);
            widgetButton.hoverColor = Col.Lerp(widgetButton.hoverColor, isWidgetMode ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);
            trayButton.hoverColor = Col.Lerp(trayButton.hoverColor, (!isWidgetMode) ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);

            RendererMain.Instance.MainIsland.LocalPosition.X = Mathf.Lerp(RendererMain.Instance.MainIsland.LocalPosition.X,
                songLocalPosXAddition, 2f * RendererMain.Instance.DeltaTime);
            songLocalPosXAddition = Mathf.Lerp(songLocalPosXAddition, 0f, 10 * RendererMain.Instance.DeltaTime);
            songSizeAddition = Mathf.Lerp(songSizeAddition, 0f, 10 * RendererMain.Instance.DeltaTime);

            if(Math.Abs(songLocalPosXAddition) < 5f)
            {
                if(next != null && next.IsEnabled)
                    next.SetActive(false);
                if (previous != null && previous.IsEnabled)
                    previous.SetActive(false);
            }
            else if (songLocalPosXAddition > 15f)
            {
                if(next != null)
                    next.SetActive(true);
            }
            else if (songLocalPosXAddition < -15f)
            {
                if(previous != null)
                    previous.SetActive(true);
            }

            if (!RendererMain.Instance.MainIsland.IsHovering)
            {
                var smallContainerSize = IslandSize() - songSizeAddition;
                smallContainerSize -= sCD;
                smallWidgetsContainer.LocalPosition.X = -RendererMain.Instance.MainIsland.LocalPosition.X;
                smallWidgetsContainer.Size = smallContainerSize;

                { // Left Small Widgets
                    float leftStackedPos = 0f;
                    foreach (var smallLeft in smallLeftWidgets)
                    {
                        smallLeft.Anchor.X = 0;
                        smallLeft.LocalPosition.X = leftStackedPos;

                        leftStackedPos += smallWidgetsSpacing + smallLeft.GetWidgetSize().X;
                    }
                }

                { // Right Small Widgets
                    float rightStackedPos = 0f;
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
            }
            else if (RendererMain.Instance.MainIsland.IsHovering)
            {
                topContainer.Size = new Vec2(RendererMain.Instance.MainIsland.currSize.X - 30, 30);

                var bigContainerSize = IslandSizeBig();
                bigContainerSize -= bCD;
                bigWidgetsContainer.Size = bigContainerSize;

                { // Big Widgets

                    List<WidgetBase> widgetsInOneLine = new List<WidgetBase>();

                    float lastBiggestY = 0f;

                    for(int i = 0; i < bigWidgets.Count; i++)
                    {
                        int line = i / maxBigWidgetInOneRow; // Correct line calculation

                        widgetsInOneLine.Add(bigWidgets[i]);
                        bigWidgets[i].Anchor.Y = 1;
                        bigWidgets[i].Anchor.X = 0.5f;
                        lastBiggestY = (float)Math.Max(lastBiggestY, bigWidgets[i].GetWidgetSize().Y);

                        bigWidgets[i].LocalPosition.Y = -line * (lastBiggestY + bigWidgetsSpacing);

                        if ((i) % maxBigWidgetInOneRow == 1)
                        {
                            lastBiggestY = 0f;
                            CenterWidgets(widgetsInOneLine, bigWidgetsContainer);
                            widgetsInOneLine.Clear();
                            line++;
                        }
                    }
                }
            }

            cycle++;
        }

        public void CenterWidgets(List<WidgetBase> widgets, UIObject container)
        {
            float spacing = bigWidgetsSpacing;
            float stackedXPosition = 0f;

            float fullWidth = 0f;

            for (int i = 0; i < widgets.Count; i++)
            {
                fullWidth += widgets[i].GetWidgetSize().X;

                widgets[i].LocalPosition.X = stackedXPosition + widgets[i].GetWidgetSize().X / 2 - container.Size.X / 2;
                stackedXPosition += widgets[i].GetWidgetSize().X + spacing;
            }

            float offset = fullWidth / 2 - container.Size.X / 2 + bigWidgetsSpacing / 2;

            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].LocalPosition.X -= offset;
            }
        }
    }
}
