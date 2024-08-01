using DynamicWin.Main;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu
{
    internal class MenuManager
    {
        private BaseMenu activeMenu;
        public BaseMenu ActiveMenu { get => activeMenu; set => SetActiveMenu(value); }

        private static MenuManager instance;
        public static MenuManager Instance { get => instance; }

        public MenuManager()
        {
            instance = this;
        }

        Thread menuThread;

        private void SetActiveMenu(BaseMenu newActiveMenu)
        {
            if (menuThread != null) return;

            menuThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                int lengthI = 125;
                int lengthO = 375;
                float yOffset = RendererMain.Instance.MainIsland.Size.Y;

                if(activeMenu != null)
                {
                    Vec2[] startPositions = new Vec2[activeMenu.UiObjects.Count];
                    
                    for(int x = 0; x < activeMenu.UiObjects.Count; x++)
                    {
                        startPositions[x] = activeMenu.UiObjects[x].RawPosition;
                    }

                    for (int i = 0; i < lengthI; i++)
                    {
                        try
                        {
                            Thread.Sleep(1);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            activeMenu = newActiveMenu;
                            return;
                        }

                        float t = Easings.EaseInQuint((float)i / lengthI);

                        int y = 0;
                        activeMenu.UiObjects.ForEach(obj =>
                        {
                            obj.blurAmount = Mathf.Lerp(0, 35, t);

                            obj.Position = startPositions[y] + new Vec2(0, yOffset * t);
                            y++;
                        });
                    }

                    activeMenu.UiObjects.ForEach(obj =>
                    {
                        obj.DestroyCall();
                    });
                }

                activeMenu = newActiveMenu;

                {
                    Vec2[] startPositions = new Vec2[activeMenu.UiObjects.Count];

                    for (int x = 0; x < activeMenu.UiObjects.Count; x++)
                    {
                        startPositions[x] = activeMenu.UiObjects[x].RawPosition;
                        activeMenu.UiObjects[x].Position -= new Vec2(0, yOffset);
                    }

                    for (int i = 0; i < lengthO; i++)
                    {
                        try
                        {
                            Thread.Sleep(1);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            activeMenu = newActiveMenu;
                            return;
                        }

                        float t = Easings.EaseOutQuint((float)i / lengthO);

                        int y = 0;
                        activeMenu.UiObjects.ForEach(obj =>
                        {
                            obj.blurAmount = Mathf.Lerp(35, 0, t);

                            obj.Position = startPositions[y] - new Vec2(0, yOffset * (1f - t));
                            y++;
                        });
                    }
                }

                menuThread = null;
            });
            menuThread.Start();
        }
    }
}
