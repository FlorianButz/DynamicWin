using DynamicWin.Main;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu.Menus
{
    internal class TestMenu : BaseMenu
    {
        public override List<UIObject> InitializeMenu()
        {
            var objects = base.InitializeMenu();

            var btn = new DWTextButton(RendererMain.Instance.MainIsland, "Hello", new Vec2(0, 0), new Vec2(125, 25), () =>
            {
                System.Diagnostics.Debug.WriteLine("Clicked!");
            }, UIAlignment.Center);

            btn.clickCallback += () => { MenuManager.Instance.ActiveMenu = new TestMenu(); };

            objects.Add(btn);

            return objects;
        }
    }
}
