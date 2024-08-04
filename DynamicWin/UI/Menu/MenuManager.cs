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
            Resources.Resources.CreateStaticMenus();
            activeMenu = Resources.Resources.HomeMenu;
        }

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
                    BaseMenu lastMenu = activeMenu;

                    SetActiveMenu(newActiveMenu);
                    int timeMillis = (int)(time * 1000);

                    Thread.Sleep(timeMillis);

                    if (lastMenu == null) throw new NullReferenceException();
                    SetActiveMenu(lastMenu);

            }).Start();
        }

        List<BaseMenu> menuLoadQueue = new List<BaseMenu>();

        Animator menuAnimatorIn;
        Animator menuAnimatorOut;

        private void SetActiveMenu(BaseMenu newActiveMenu)
        {
            if (menuAnimatorIn != null && menuAnimatorIn.IsRunning) return;
            if (menuAnimatorOut != null && menuAnimatorOut.IsRunning) return;

            onMenuChange?.Invoke(activeMenu, newActiveMenu);

            float yOffset = RendererMain.Instance.MainIsland.Size.Y * 0.75f;

            int length = 250;

            List<UIObject> currentObjects = new List<UIObject>(activeMenu.UiObjects);

            {
                menuAnimatorOut = new Animator(length, 1);

                currentObjects = new List<UIObject>(activeMenu.UiObjects);

                menuAnimatorOut.onAnimationUpdate += (t) =>
                {
                    float tEased = Easings.EaseOutCubic(t);

                    currentObjects.ForEach(obj =>
                    {
                        if (obj != null)
                        {
                            obj.blurAmount = Mathf.Lerp(35, 0, tEased);
                        }
                    });

                    RendererMain.Instance.renderOffset.Y = Mathf.Lerp(-yOffset, 0, tEased);
                };

                menuAnimatorOut.onAnimationEnd += () =>
                {
                    activeMenu = newActiveMenu;

                    RendererMain.Instance.renderOffset.Y = 0;
                    LoadMenuEnd();

                    return;
                };
            }

            if (activeMenu != null)
            {
                menuAnimatorIn = new Animator(length, 1);

                menuAnimatorIn.onAnimationUpdate += (t) =>
                {
                    float tEased = Easings.EaseInCubic(t);

                    currentObjects.ForEach(obj =>
                    {
                        if (obj != null)
                        {
                            obj.blurAmount = Mathf.Lerp(0, 35, tEased);
                        }
                    });

                    RendererMain.Instance.renderOffset.Y = Mathf.Lerp(0, yOffset, tEased);
                };

                menuAnimatorIn.onAnimationEnd += () =>
                {
                    activeMenu = newActiveMenu;

                    RendererMain.Instance.renderOffset.Y = -yOffset;

                    currentObjects = new List<UIObject>(activeMenu.UiObjects);
                    currentObjects.ForEach(obj =>
                    {
                        if (obj != null)
                        {
                            obj.blurAmount = 35;
                        }
                    });
                    
                    if(menuAnimatorOut == null)
                    {
                        LoadMenuEnd();
                        return;
                    }

                    menuAnimatorOut.Start();
                };
            }

            if (menuAnimatorIn == null) menuAnimatorOut.Start();
            else menuAnimatorIn.Start();
        }

        void LoadMenuEnd()
        {
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

            menuAnimatorIn = null;
            menuAnimatorOut = null;
        }

        public void QueueOpenMenu(BaseMenu menu)
        {
            if (menuAnimatorIn == null && menuAnimatorOut == null) OpenMenu(menu);
            else
            {
                menuLoadQueue.Add(menu);
            }
        }
    }
}
