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
    public class MenuManager
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
            Resources.Res.CreateStaticMenus();
            activeMenu = Resources.Res.HomeMenu;
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

        static Thread overlayThread;

        public static void CloseOverlay()
        {
            overlayThread.Interrupt();
        }

        private void OpenOverlay(BaseMenu newActiveMenu, float time)
        {
            overlayThread = new Thread(() =>
            {
                BaseMenu lastMenu = activeMenu;

                QueueOpenMenu(newActiveMenu);
                int timeMillis = (int)(time * 1000);

                try
                {
                    Thread.Sleep(timeMillis);
                }
                catch(ThreadInterruptedException e)
                {
                    QueueOpenMenu(lastMenu);
                    return;
                }

                if (lastMenu == null) throw new NullReferenceException();
                QueueOpenMenu(lastMenu);

            });
            overlayThread.Start();
        }

        List<BaseMenu> menuLoadQueue = new List<BaseMenu>();

        Animator menuAnimatorOut;

        public void Update(float deltaTime)
        {
            if (menuAnimatorOut != null)
                menuAnimatorOut.Update(deltaTime);
        }

        private void SetActiveMenu(BaseMenu newActiveMenu)
        {
            if (menuAnimatorOut != null && menuAnimatorOut.IsRunning) return;
            onMenuChange?.Invoke(activeMenu, newActiveMenu);

            menuAnimatorOut = new Animator(300, 1);

            RendererMain.Instance.blurOverride = 35f;

            if (activeMenu != null) activeMenu.OnDeload();
            activeMenu = newActiveMenu;

            menuAnimatorOut.onAnimationUpdate += (t) =>
            {
                float easedTime = Easings.EaseOutCubic(t);
                float easedTime2 = Easings.EaseOutQuint(t);
                float blurSize = Mathf.Lerp(35f, 0f, easedTime);
                float alpha = Mathf.Lerp(0f, 1f, easedTime2);

                var canvasSize = Vec2.lerp(Vec2.one * 0.7f, Vec2.one, easedTime2);

                RendererMain.Instance.blurOverride = blurSize;
                RendererMain.Instance.alphaOverride = alpha;
                RendererMain.Instance.scaleOffset = canvasSize;
            };

            menuAnimatorOut.onAnimationEnd += () =>
            {
                LoadMenuEnd();
            };

            menuAnimatorOut.Start();
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

            menuAnimatorOut = null;
        }

        public void QueueOpenMenu(BaseMenu menu)
        {
            if (menuAnimatorOut == null) OpenMenu(menu);
            else
            {
                menuLoadQueue.Add(menu);
            }
        }
    }
}
