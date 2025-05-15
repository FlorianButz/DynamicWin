using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using SkiaSharp;

namespace DynamicWin.UI.Menu.Menus
{
    public class WeatherMenu : BaseMenu
    {
        public WeatherMenu()
        {
            MainForm.onScrollEvent += (MouseWheelEventArgs x) =>
            {
                yScrollOffset += x.Delta * 0.25f;
            };
        }

        void SaveChanges()
        {
            DynamicWinMain.UpdateStartup();
            Res.SettingsMenu = new SettingsMenu();
            MenuManager.OpenMenu(Res.SettingsMenu);

            Settings.Save();
        }

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            var _WeatherLocationTitle = new DWText(island, "Select Location", new Vec2(25, 0), UIAlignment.TopLeft);
            _WeatherLocationTitle.Font = Res.InterBold;
            _WeatherLocationTitle.Anchor.X = 0;
            objects.Add(_WeatherLocationTitle);

            objects.Add(new DWText(island, "Test", new Vec2(25, 0), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                Anchor = new Vec2(0, 0),
            });

            var _saveChangesBtn = new DWTextButton(island, "Save changes", new Vec2(0, -45), new Vec2(250, 40), () => { SaveChanges(); }, UIAlignment.BottomCenter)
            {
                roundRadius = 25
            };
            _saveChangesBtn.Text.Font = Res.InterBold;

            bottomMask = new UIObject(island, Vec2.zero, new Vec2(IslandSizeBig().X - 230, 75), UIAlignment.BottomCenter)
            {
                Anchor = new Vec2(0.5, 1.1),
                Color = Theme.IslandBackground,
                roundRadius = 50
            };

            objects.Add(bottomMask);
            objects.Add(_saveChangesBtn);

            return objects;
        }

        UIObject bottomMask;
        
        float yScrollOffset = 0f;
        float ySmoothScroll = 0f;

        public override Vec2 IslandSize()
        {
            var vec = new Vec2(525, 425);

            return vec;
        }

        public override Vec2 IslandSizeBig()
        {
            return IslandSize() + 5;
        }

        public override void Update()
        {
            base.Update();

            ySmoothScroll = Mathf.Lerp(ySmoothScroll, yScrollOffset, 10f * RendererMain.Instance.DeltaTime);

            bottomMask.blurAmount = 5;

            var yScrollLim = 0f;
            var yPos = 35f;
            var spacing = 15f;

            for (int i = 0; i < UiObjects.Count - 2; i++)
            {
                var uiObject = UiObjects[i];
                if (!uiObject.IsEnabled) continue;

                uiObject.LocalPosition.Y = yPos + ySmoothScroll;
                yPos += uiObject.Size.Y + spacing;

                if (yPos > IslandSize().Y - 45) yScrollLim += uiObject.Size.Y + spacing;
            }

            yScrollOffset = Mathf.Lerp(yScrollOffset,
                Mathf.Clamp(yScrollOffset, -yScrollLim, 0f), 15f * RendererMain.Instance.DeltaTime);
        }
    }
}
