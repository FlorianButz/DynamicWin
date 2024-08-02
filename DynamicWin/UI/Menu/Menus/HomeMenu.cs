using DynamicWin.Main;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.Widgets;
using DynamicWin.UI.Widgets.Big;
using DynamicWin.UI.Widgets.Small;
using DynamicWin.Utils;
using System.ComponentModel;

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
            return size;
        }

        UIObject smallWidgetsContainer;
        UIObject bigWidgetsContainer;

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            smallWidgetsContainer = new UIObject(island, Vec2.zero, IslandSize(), UIAlignment.Center);
            bigWidgetsContainer = new UIObject(island, Vec2.zero, IslandSize(), UIAlignment.Center);

            // Create elements

            smallLeftWidgets.Add(new TimeWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleLeft));
            smallRightWidgets.Add(new BatteryWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleRight));
            smallRightWidgets.Add(new UsedDevicesWidget(smallWidgetsContainer, Vec2.zero, UIAlignment.MiddleRight));

            bigWidgets.Add(new MediaWidget(bigWidgetsContainer, Vec2.zero, UIAlignment.BottomCenter));
            bigWidgets.Add(new TestWidget(bigWidgetsContainer, Vec2.zero, UIAlignment.BottomCenter));

            // Add lists

            smallLeftWidgets.ForEach(x => objects.Add(x));
            smallRightWidgets.ForEach(x => objects.Add(x));

            bigWidgets.ForEach(x => {
                objects.Add(x);
                x.SilentSetActive(false);
                });

            return objects;
        }

        public float topSpacing = 45;
        public float bigWidgetsSpacing = 15;
        int maxBigWidgetInOneRow = 2;

        public float smallWidgetsSpacing = 15;
        public float middleWidgetsSpacing = 35;

        bool wasHovering = false;

        float sCD = 35;
        float bCD = 50;

        public override void Update()
        {
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

                if (wasHovering)
                {
                    wasHovering = false;

                    smallLeftWidgets.ForEach(x => x.SetActive(true));
                    smallRightWidgets.ForEach(x => x.SetActive(true));

                    bigWidgets.ForEach(x => x.SetActive(false));
                }
            }
            else if (RendererMain.Instance.MainIsland.IsHovering)
            {
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
                            System.Diagnostics.Debug.WriteLine(line);
                            widgetsInOneLine.Clear();
                            line++;
                        }
                    }
                }

                if (!wasHovering)
                {
                    wasHovering = true;
                 
                    smallLeftWidgets.ForEach(x => x.SetActive(false));
                    smallRightWidgets.ForEach(x => x.SetActive(false));
                    
                    bigWidgets.ForEach(x => x.SetActive(true));
                }
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
