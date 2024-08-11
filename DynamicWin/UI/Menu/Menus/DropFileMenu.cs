using DynamicWin.UI.UIElements;
using DynamicWin.UI.UIElements.Custom;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DynamicWin.UI.Menu.Menus
{
    public class DropFileMenu : BaseMenu
    {
        private static DropFileMenu instance;

        public override Vec2 IslandSize()
        {
            return new Vec2(450, 200);
        }

        public static void Drop(DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Dropped!");

            if (e == null) return;

            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            if (fileList != null && fileList.Length > 0)
            {
                Tray.AddFiles(fileList);
            }
        }

        public DropFileMenu()
        {
            instance = this;
        }

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);
            
            var dropObj = new DropFileElement(island, Vec2.zero, new Vec2(400, 150), alignment: UIAlignment.Center);
            objects.Add(dropObj);

            return objects;
        }
    }
}
