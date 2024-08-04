﻿using DynamicWin.Main;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.Widgets;
using DynamicWin.UI.Widgets.Big;
using DynamicWin.UI.Widgets.Small;
using DynamicWin.Utils;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DynamicWin.UI.Menu.Menus
{
    internal class HomeMenu : BaseMenu
    {
        public List<SmallWidgetBase> smallLeftWidgets = new List<SmallWidgetBase>();
        public List<SmallWidgetBase> smallRightWidgets = new List<SmallWidgetBase>();

        public List<WidgetBase> bigWidgets = new List<WidgetBase>();

        public override Vec2 IslandSize()
        {
            Vec2 size = new Vec2(200, 35);

            float sizeTogether = 0f;
            smallLeftWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);
            smallRightWidgets.ForEach(x => sizeTogether += x.GetWidgetSize().X);

            sizeTogether += smallWidgetsSpacing * (smallLeftWidgets.Count + smallRightWidgets.Count + 0.25f) + middleWidgetsSpacing;

            size.X = (float)Math.Max(size.X, sizeTogether);

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

                sizeTogetherBiggest += bCD + (bigWidgetsSpacing * (bigWidgets.Count / maxBigWidgetInOneRow)) + topSpacing;

                // Set the container height to the total height of all rows
                size.Y = Math.Max(size.Y, sizeTogetherBiggest);
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

            smallLeftWidgets.Add(new TimeWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleLeft));
            smallLeftWidgets.Add(new SmallVisualizerWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleLeft));
            smallRightWidgets.Add(new BatteryWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleRight));
            smallRightWidgets.Add(new UsedDevicesWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleRight));

            bigWidgets.Add(new MediaWidget(bigWidgetsContainer, Vec2.zero, UIAlignment.BottomCenter));
            bigWidgets.Add(new TestWidget(bigWidgetsContainer, Vec2.zero, UIAlignment.BottomCenter));

            topContainer = new UIObject(island, new Vec2(0, 30), new Vec2(island.currSize.X, 50))
            {
                Color = Theme.IslandBackground.Inverted().Override(a: 0.035f),
                roundRadius = 10f
            };
            bigMenuItems.Add(topContainer);

            widgetButton = new DWTextImageButton(topContainer, Resources.Resources.Widgets, "Widgets", new Vec2(75 / 2 + 5, 0), new Vec2(75, 20), () =>
            {
                isWidgetMode = true;
            },
            UIAlignment.MiddleLeft);
            widgetButton.normalColor = Col.Transparent;
            widgetButton.hoverColor = Col.Transparent;
            widgetButton.clickColor = Theme.Primary.Override(a: 0.35f);
            widgetButton.roundRadius = 25;

            bigMenuItems.Add(widgetButton);

            trayButton = new DWTextImageButton(topContainer, Resources.Resources.Tray, "Tray", new Vec2(105, 0), new Vec2(50, 20), () =>
            {
                isWidgetMode = false;
            },
            UIAlignment.MiddleLeft);
            trayButton.normalColor = Col.Transparent;
            trayButton.hoverColor = Col.Transparent;
            trayButton.clickColor = Theme.Primary.Override(a: 0.35f);
            trayButton.roundRadius = 25;

            bigMenuItems.Add(trayButton);

            var settingsButton = new DWImageButton(topContainer, Resources.Resources.Settings, new Vec2(-20f, 0), new Vec2(20, 20), () =>
            {
                MenuManager.OpenMenu(new SettingsMenu());
            },
            UIAlignment.MiddleRight);
            settingsButton.normalColor = Col.Transparent;
            settingsButton.hoverColor = Col.Transparent;
            settingsButton.clickColor = Theme.Primary.Override(a: 0.35f);
            settingsButton.roundRadius = 25;

            bigMenuItems.Add(settingsButton);

            tray = new Tray(island, new Vec2(0, -bCD / 2), Vec2.zero, UIAlignment.BottomCenter);
            tray.Anchor.Y = 1f;
            tray.SilentSetActive(false);

            bigMenuItems.Add(tray);

            // Add lists

            smallLeftWidgets.ForEach(x => {
                objects.Add(x);
            });

            smallRightWidgets.ForEach(x => { 
                objects.Add(x);
            });

            bigMenuItems.ForEach(x =>
            {
                objects.Add(x);
                x.SilentSetActive(false);
                });

            bigWidgets.ForEach(x => {
                objects.Add(x);
                x.SilentSetActive(false);
                });

            return objects;
        }

        public float topSpacing = 20;
        public float bigWidgetsSpacing = 15;
        int maxBigWidgetInOneRow = 2;

        public float smallWidgetsSpacing = 15;
        public float middleWidgetsSpacing = 35;

        bool wasHovering = false;

        float sCD = 35;
        float bCD = 50;

        bool isWidgetMode = true;
        bool wasWidgetMode = false;

        public override void Update()
        {
            tray.Size = new Vec2(IslandSizeBig().X - bCD, RendererMain.Instance.MainIsland.Size.Y - bCD - topSpacing - topContainer.Size.Y / 2);
            tray.SetActive(!(isWidgetMode || !RendererMain.Instance.MainIsland.IsHovering));

            widgetButton.normalColor = Col.Lerp(widgetButton.normalColor, isWidgetMode ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);
            trayButton.normalColor = Col.Lerp(trayButton.normalColor, (!isWidgetMode) ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);
            widgetButton.hoverColor = Col.Lerp(widgetButton.hoverColor, isWidgetMode ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);
            trayButton.hoverColor = Col.Lerp(trayButton.hoverColor, (!isWidgetMode) ? Col.White.Override(a: 0.075f) : Col.Transparent, 15f * RendererMain.Instance.DeltaTime);

            if (!RendererMain.Instance.MainIsland.IsHovering)
            {
                var smallContainerSize = IslandSize();
                smallContainerSize -= sCD;
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

                smallLeftWidgets.ForEach(x => x.SetActive(true));
                smallRightWidgets.ForEach(x => x.SetActive(true));
                
                if (wasHovering)
                {
                    bigWidgets.ForEach(x => x.SetActive(false));
                    bigMenuItems.ForEach(x => x.SetActive(false));
                }
                
                wasHovering = false;
            }
            else if (RendererMain.Instance.MainIsland.IsHovering)
            {
                topContainer.Size = new Vec2(RendererMain.Instance.MainIsland.currSize.X - bCD, 30);

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

                if (!wasHovering)
                {
                    smallLeftWidgets.ForEach(x => x.SetActive(false));
                    smallRightWidgets.ForEach(x => x.SetActive(false));

                    bigWidgets.ForEach(x => x.SetActive(isWidgetMode));
                    bigMenuItems.ForEach(x => x.SetActive(true));
                }

                if (isWidgetMode && !wasWidgetMode)
                {
                    wasWidgetMode = true;
                    bigWidgets.ForEach(x => x.SetActive(true));
                }
                else if (!isWidgetMode && wasWidgetMode)
                {
                    wasWidgetMode = false;
                    bigWidgets.ForEach(x => x.SetActive(false));
                }

                wasHovering = true;
            }
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