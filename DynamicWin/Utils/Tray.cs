using DynamicWin.Main;
using DynamicWin.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    internal class Tray : UIObject
    {
        static string[]? cachedTrayFiles;

        int maxFilesInOneLine = 6;
        int fileHeight = 80;

        float yOffset = 0f;
        float mouseSensitivity = 0.15f;

        public Tray(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            MainForm.onScrollEvent += OnScroll;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        
            MainForm.onScrollEvent -= OnScroll;
        }

        void OnScroll(System.Windows.Forms.MouseEventArgs e)
        {
            yOffset += e.Delta * mouseSensitivity;
        }

        int timer = 0;

        float mouseYLast = 0f;
        Vec2 mouseStart;
        public override void OnMouseDown()
        {
            mouseYLast = RendererMain.CursorPosition.Y;
            mouseStart = new Vec2(RendererMain.CursorPosition.X, RendererMain.CursorPosition.Y);
        }

        float mouseYLastSmooth = 0f;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (timer % 64 == 0) cachedTrayFiles = GetFiles();
            timer++;

            if (cachedTrayFiles == null) return;

            mouseYLastSmooth = Mathf.Lerp(mouseYLastSmooth, 0, 10f * deltaTime);
            
            if (IsMouseDown)
            {
                yOffset += (RendererMain.CursorPosition.Y - mouseYLast) * (mouseSensitivity * 5f);
                mouseYLastSmooth = (RendererMain.CursorPosition.Y - mouseYLast) * (mouseSensitivity * 7.5f);
                mouseYLast = RendererMain.CursorPosition.Y;

                if(Vec2.Distance(mouseStart, new Vec2(RendererMain.MousePosition.X, RendererMain.MousePosition.Y)) >= 25)
                {
                    MainForm.Instance.StartDrag(cachedTrayFiles[0]);
                }
            }
            else
            {
                int lines = cachedTrayFiles.Length / maxFilesInOneLine;

                yOffset += mouseYLastSmooth;
                yOffset = Mathf.Lerp(yOffset, Mathf.Clamp(yOffset, -((lines-1) * (fileHeight)), 0f), 25f * deltaTime);
            }
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();

            if (cachedTrayFiles == null) return;

            var fileWidth = Size.X / maxFilesInOneLine;
            var sizeSub = 10;

            int save = canvas.Save();
            canvas.ClipRoundRect(GetRect());
            canvas.Translate(0, yOffset);

            for (int i = 0; i < cachedTrayFiles.Length; i++)
            {
                int line = i / maxFilesInOneLine;

                var file = cachedTrayFiles[i];

                var rect = SKRect.Create(
                    Position.X + (fileWidth * i) + (sizeSub / maxFilesInOneLine) * ((i - (line * maxFilesInOneLine)) + 1) - (Size.X * line),
                    Position.Y + (fileHeight * line) + (sizeSub / maxFilesInOneLine) * (line + 1),
                    fileWidth - sizeSub, fileHeight - sizeSub);
                canvas.DrawRect(rect, paint);
            }

            canvas.RestoreToCount(save);
        }

        public static string[]? GetFiles()
        {
            var dirPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "TrayFiles");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                return null;
            }

            return Directory.GetFiles(dirPath);
        }

        public static void AddFiles(string[] files)
        {
            var dirPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "TrayFiles");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            foreach (var item in files)
            {
                System.Diagnostics.Debug.WriteLine("Added file: " + item + " to Tray!");

                if (File.Exists(item) && !File.Exists(Path.Combine(dirPath, Path.GetFileName(item))))
                {
                    File.Copy(item, Path.Combine(dirPath, Path.GetFileName(item)));
                }
            }

            cachedTrayFiles = GetFiles();
        }
    }
}
