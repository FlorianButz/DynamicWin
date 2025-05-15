using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicWin.Utils;
using SkiaSharp;

namespace DynamicWin.UI.UIElements
{
    public class DWMultiSelectionButton : UIObject
    {
        string[] options;
        DWTextButton[] buttons;

        public Action<int> onClick;

        int selectedIndex = 0;
        public int SelectedIndex { get => selectedIndex; set => SetSelected(value); }

        public DWMultiSelectionButton(UIObject? parent, string[] options, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter, int maxInOneRow = 4) : base(parent, position, size, alignment)
        {
            this.options = options;
            this.buttons = new DWTextButton[options.Length];

            float xPos = 0;
            float yPos = 0;
            int counter = 0;

            for (int i = 0; i < options.Length; i++)
            {
                var lambdaIndex = i; // Either I'm going insane or I don't understand lambdas, but it seems like only the pointer given in to the OnClick() method. This is why this line is needed!
                var action = () => { OnClick(lambdaIndex); }; // For some it just outputs the length of options if there is no seperate variable for it.

                if (counter >= maxInOneRow)
                {
                    counter = 0;
                    yPos += 35;
                    xPos = 0;
                }

                var btn = new DWTextButton(this, options[i], new Vec2(xPos, yPos), new Vec2(Math.Max(75, options[i].Length >= 11 ? (options[i].Length * 9) : 0), 25), action, UIAlignment.MiddleLeft);
                btn.Text.Color = Theme.TextSecond;
                btn.Anchor.X = 0;
                buttons[i] = btn;

                AddLocalObject(btn);

                xPos += btn.Size.X + 15;
                counter++;
            }

            Size.Y = Size.Y + yPos;

            SelectedIndex = 0;
        }

        public override void Draw(SKCanvas canvas)
        {
        }

        void SetSelected(int index)
        {
            selectedIndex = index;

            foreach (var button in buttons)
            {
                button.normalColor = Theme.Secondary.Override(a: 0.9f);
            }

            buttons[index].normalColor = (Theme.Primary * 0.65f).Override(a: 0.75f);
        }

        void OnClick(int index)
        {
            SelectedIndex = index;

            try
            {
                onClick?.Invoke(index);
            }
            catch (NullReferenceException e)
            {
                Debug.WriteLine("DWMultiSelectionButton ERROR: logic not yet implemented.");
            }
        }
    }
}
