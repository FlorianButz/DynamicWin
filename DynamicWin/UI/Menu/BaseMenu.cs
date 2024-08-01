using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu
{
    internal class BaseMenu
    {
        private List<UIObject> uiObjects;

        public List<UIObject> UiObjects { get { return uiObjects; } }

        public BaseMenu()
        {
            uiObjects = InitializeMenu();
        }

        public virtual Vec2 IslandSize() { return new Vec2(250, 25); }
        public virtual Vec2 IslandSizeBig() { return new Vec2(500, 50); }

        public virtual List<UIObject> InitializeMenu() { return new List<UIObject>(); }
    }
}
