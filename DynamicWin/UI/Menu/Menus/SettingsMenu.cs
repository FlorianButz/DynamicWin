using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu.Menus
{
    internal class SettingsMenu : BaseMenu
    {

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            var backBtn = new DWTextButton(island, "Save and Back", new Vec2(0, -45), new Vec2(350, 40), () => { MenuManager.OpenMenu(Resources.Resources.HomeMenu); }, UIAlignment.BottomCenter)
            {
                roundRadius = 25
            };
            backBtn.Text.Font = Resources.Resources.InterBold;

            objects.Add(backBtn);

            return objects;
        }

        public override Vec2 IslandSize()
        {
            return new Vec2(400, 550);
        }
    }
}
