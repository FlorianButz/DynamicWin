using DynamicWin.Main;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Menu.Menus
{
    internal class TimerOverMenu : BaseMenu
    {
        DWText overText;
        System.Media.SoundPlayer player;

        public override List<UIObject> InitializeMenu(IslandObject island)
        {
            var objects = base.InitializeMenu(island);

            overText = new DWText(island, "Timer Over!", new Utils.Vec2(0, 0), UIAlignment.Center)
            {
                textSize = 20,
                Font = Resources.Res.InterBold
            };

            objects.Add(overText);

            player = new System.Media.SoundPlayer(Resources.Res.TimerOverSound);
            player.PlayLooping();

            return objects;
        }

        public override void OnDeload()
        {
            player.Stop();
        }

        float sinCycle = 0f;
        float speed = 2.5f;
        public override void Update()
        {
            base.Update();

            var delta = RendererMain.Instance.DeltaTime;
            sinCycle += delta * speed;

            overText.textSize = Mathf.Remap((float)Math.Sin(sinCycle), -1, 1, 15, 25);

            if (RendererMain.Instance.MainIsland.IsHovering && sinCycle >= 1)
                MenuManager.CloseOverlay();
        }

        public override Vec2 IslandSize()
        {
            return new Vec2(250, 65);
        }

        public override Vec2 IslandSizeBig()
        {
            return base.IslandSizeBig();
        }
    }
}
