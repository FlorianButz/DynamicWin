using DynamicWin.Main;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu
{
    internal class BaseMenu : IDisposable
    {
        private List<UIObject> uiObjects = new List<UIObject>();

        public List<UIObject> UiObjects { get { return uiObjects; } }

        public BaseMenu()
        {
            uiObjects = InitializeMenu(RendererMain.Instance.MainIsland);
        }

        public virtual Vec2 IslandSize() { return new Vec2(200, 45); }
        public virtual Vec2 IslandSizeBig() { return IslandSize(); }

        public virtual List<UIObject> InitializeMenu(IslandObject island) { return new List<UIObject>(); }

        public virtual void Update() { }

        public virtual void OnDeload() { }

        public void Dispose()
        {
            uiObjects.Clear();
        }
    }
}
