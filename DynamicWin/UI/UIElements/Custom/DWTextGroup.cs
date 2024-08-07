using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicWin.UI.UIElements.Custom
{
    internal class DWTextGroup : UIObject
    {
        List<DWText> textPieces = new List<DWText>();

        public float textSize = 24;


        public DWTextGroup(UIObject? parent, string text, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, Vec2.zero, alignment)
        {
            SetText(text);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);

            float xAdded = 0;

            foreach (var textPiece in textPieces)
            {
                xAdded += textPiece.TextBounds.X / 2;
                textPiece.LocalPosition.X = xAdded;
                xAdded += textPiece.TextBounds.X / 2 + 1.5f;
            }

            Size.X = xAdded;
        }

        string text = "";

        public void SetText(string text)
        {
            if (this.text == text) return;

            List<int> sameCharacters = new List<int>();
            for(int i = 0; i < this.text.Length; i++)
            {
                if (this.text.Length <= i || text.Length <= i) continue;
                if (this.text[i] == text[i]) sameCharacters.Add(i);
            }

            this.text = text;

            float xAdded = 0;

            int counter = 0;
            foreach (var c in text)
            {
                if (!sameCharacters.Contains(counter)) continue;
                if (textPieces.Count <= counter)
                {
                    var textPiece = new DWText(this, c.ToString(), new Vec2(xAdded, 0));
                    xAdded += textPiece.TextBounds.X;
                    textPiece.TextSize = textSize;
                    textPieces.Add(textPiece);
                    textPiece.SilentSetActive(false);
                    textPiece.SetActive(true);

                    AddLocalObject(textPiece);
                }
                else
                {
                    var textPiece = textPieces[counter];
                    textPiece.TextSize = textSize;
                    textPiece.Text = c.ToString();
                }
                counter++;
            }

            for (int i = text.Length - 1; i < textPieces.Count; i++)
            {
                DestroyLocalObject(textPieces[i]);
            }
        }

        public void Refresh()
        {
            SetText(text);
        }
    }
}
