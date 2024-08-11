using DynamicWin.Resources;
using DynamicWin.UI.UIElements;
using DynamicWin.UI.UIElements.Custom;
using DynamicWin.UI.Widgets.Big;
using DynamicWin.Utils;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace DynamicWin.UI.Menu.Menus
{
    internal class ConfigureShortcutMenu : BaseMenu
    {
        ShortcutButton _shortcutButtonToConfigure;
        ShortcutButton.ShortcutSave save;

        static ConfigureShortcutMenu instance;

        public static void DropData(System.Windows.DragEventArgs e)
        {
            instance.PDropData(e);
        }

        private void PDropData(System.Windows.DragEventArgs e)
        {
            if (e == null) return;

            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            if (fileList != null && fileList.Length > 0)
            {
                var name = Path.GetFileNameWithoutExtension(fileList[0]) + " ";

                if (File.Exists(fileList[0]))
                {
                    save = new ShortcutButton.ShortcutSave()
                    {
                        path = fileList[0],
                        name = name
                    };
                }

                elementText.SetText(DWText.Truncate(fileList[0], 60));
                elementTitle.SetText(DWText.Truncate(name, 25));
            }

        }

        public ConfigureShortcutMenu(ShortcutButton shortcutButton)
        {
            instance = this;
            this._shortcutButtonToConfigure = shortcutButton;
            this.save = shortcutButton.savedShortcut;
        }

        DropFileElement dropFileElement;
        DWText elementText;
        DWText elementTitle;

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            DWTextButton saveAndBack = new DWTextButton(island, "Save & Back", new Utils.Vec2(0, -30), new Utils.Vec2(IslandSize().X - 30, 30), () =>
            {
                _shortcutButtonToConfigure.SetShortcut(save);
                MenuManager.OpenMenu(Res.HomeMenu);
            }, UIAlignment.BottomCenter);
            saveAndBack.hoverScaleMulti = Vec2.one * 1.025f;
            saveAndBack.roundRadius = 15;
            objects.Add(saveAndBack);

            elementTitle = new DWText(island, "Untitled", new Vec2(25, 25), UIAlignment.TopLeft);
            elementTitle.Anchor.X = 0;
            elementTitle.TextSize = 24;
            elementTitle.Font = Res.InterBold;
            objects.Add(elementTitle);

            dropFileElement = new DropFileElement(island, new Vec2(0, 55), new Vec2(IslandSize().X - 45, 70), "Drop file to open here", 18, alignment: UIAlignment.TopCenter)
            {
                Anchor = new Vec2(0.5f, 0f)
            };
            objects.Add(dropFileElement);

            elementText = new DWText(island, " ", new Vec2(0, 55 + 75 / 2));
            elementText.SilentSetActive(false);
            elementText.TextSize = 16;
            objects.Add(elementText);

            return objects;
        }

        public override void Update()
        {
            base.Update();

            HandleDropAreaActive();
        }

        void HandleDropAreaActive()
        {
            if (string.IsNullOrEmpty(save.path))
            {
                dropFileElement.SetActive(true);
                elementText.SetActive(false);
            }
            else
            {
                elementText.SetActive(true);
                dropFileElement.SetActive(false);
            }
        }

        public override Vec2 IslandSize()
        {
            return new Vec2(600, 200);
        }
    }
}
