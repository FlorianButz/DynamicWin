using DynamicWin.Main;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace DynamicWin.UI.Menu
{
    internal class MenuManager
    {
        private BaseMenu activeMenu;
        public BaseMenu ActiveMenu { get => activeMenu; }

        private static MenuManager instance;
        public static MenuManager Instance { get => instance; }

        public Action<BaseMenu, BaseMenu> onMenuChange;
        public Action<BaseMenu> onMenuChangeEnd;

        public MenuManager()
        {
            instance = this;
        }

        public void Init()
        {
            activeMenu = new HomeMenu();
        }

        Thread menuThread;

        public static void OpenMenu(BaseMenu newActiveMenu)
        {
            Instance.Open(newActiveMenu);
        }

        private void Open(BaseMenu newActiveMenu)
        {
            SetActiveMenu(newActiveMenu);
        }

        public static void OpenOverlayMenu(BaseMenu newActiveMenu, float time = 5f)
        {
            Instance.OpenOverlay(newActiveMenu, time);
        }

        private void OpenOverlay(BaseMenu newActiveMenu, float time)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                BaseMenu lastMenu = activeMenu;

                SetActiveMenu(newActiveMenu);
                int timeMillis = (int)(time * 1000);

                Thread.Sleep(timeMillis);

                if (lastMenu == null) throw new NullReferenceException();
                SetActiveMenu(lastMenu);

            }).Start();
        }

        List<BaseMenu> menuLoadQueue = new List<BaseMenu>();

        private void SetActiveMenu(BaseMenu newActiveMenu)
        {
            if (menuThread != null)
            {
                /*if(activeMenu != newActiveMenu)
                    menuLoadQueue = newActiveMenu;*/
                menuThread.Interrupt();
                menuThread = null;
                return;
            }

            onMenuChange?.Invoke(activeMenu, newActiveMenu);

            menuThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                int lengthI = 85;
                int lengthO = 85;
                float yOffset = RendererMain.Instance.MainIsland.Size.Y;

                if(activeMenu != null)
                {
                    List<UIObject> currentObjects = new List<UIObject>(activeMenu.UiObjects);

                    Vec2[] startPositions = new Vec2[currentObjects.Count];
                    
                    for(int x = 0; x < currentObjects.Count; x++)
                    {
                        startPositions[x] = currentObjects[x].RawPosition;
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

                            for (int x = 0; x < currentObjects.Count; x++)
                            {
                                currentObjects[x].Position = startPositions[x];
                            }

                            return;
                        }

                        float t = Easings.EaseInQuint((float)i / lengthI);

                        int y = 0;
                        currentObjects.ForEach(obj =>
                        {
                            if (obj != null)
                            {
                                obj.blurAmount = Mathf.Lerp(0, 35, t);
                                obj.Position = startPositions[y] + new Vec2(0, yOffset * t);
                                y++;
                            }
                        });
                    }

                    for (int x = 0; x < currentObjects.Count; x++)
                    {
                        currentObjects[x].Position = startPositions[x];
                    }

                    currentObjects.ForEach(obj =>
                    {
                        obj.DestroyCall();
                    });
                }

                activeMenu = newActiveMenu;

                {
                    List<UIObject> currentObjects = new List<UIObject>(activeMenu.UiObjects);

                    Vec2[] startPositions = new Vec2[currentObjects.Count];

                    for (int x = 0; x < currentObjects.Count; x++)
                    {
                        startPositions[x] = currentObjects[x].RawPosition;
                        currentObjects[x].Position -= new Vec2(0, yOffset);
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

                            for (int x = 0; x < currentObjects.Count; x++)
                            {
                                currentObjects[x].Position = startPositions[x];
                            }

                            return;
                        }

                        float t = Easings.EaseOutQuint((float)i / lengthO);

                        int y = 0;
                        currentObjects.ForEach(obj =>
                        {
                            if (obj != null)
                            {
                                obj.blurAmount = Mathf.Lerp(35, 0, t);

                                obj.Position = startPositions[y] - new Vec2(0, yOffset * (1f - t));
                                y++;
                            }
                        });
                    }

                    for (int x = 0; x < currentObjects.Count; x++)
                    {
                        currentObjects[x].Position = startPositions[x];
                    }
                }

                menuThread = null;
                onMenuChangeEnd?.Invoke(activeMenu);

                if (menuLoadQueue.Count != 0)
                {
                    var queueObj = menuLoadQueue[0];

                    if (queueObj == activeMenu)
                    {
                        menuLoadQueue.Remove(queueObj);
                        return;
                    }
                    else OpenMenu(queueObj);
                    
                    menuLoadQueue.Remove(queueObj);
                }
            });
            menuThread.Start();
        }

        public void QueueOpenMenu(BaseMenu menu)
        {
            if (menuThread == null) OpenMenu(menu);
            else
            {
                menuLoadQueue.Add(menu);
            }
        }
    }
}
