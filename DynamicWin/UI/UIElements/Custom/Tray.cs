using DynamicWin.Main;
using DynamicWin.UI;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements.Custom
{
    internal class Tray : UIObject
    {
        static string[]? cachedTrayFiles;

        int maxFilesInOneLine = 6;

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

        void OnScroll(MouseEventArgs e)
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

            if (timer % 64 == 0) { cachedTrayFiles = GetFiles(); AddFileObjects(); }
            timer++;

            if (cachedTrayFiles == null) return;

            mouseYLastSmooth = Mathf.Lerp(mouseYLastSmooth, 0, 10f * deltaTime);

            var maxFilesInOneLine = (int)(Size.X / 60);

            for (int i = 0; i < fileObjects.Count; i++)
            {
                var fileObject = fileObjects[i];
                int line = i / maxFilesInOneLine;

                fileObject.LocalPosition.X = 60 * (i - (maxFilesInOneLine * line)) - (Size.X * line);
                fileObject.LocalPosition.Y = (75 * line);
            }

            int lines = cachedTrayFiles.Length / maxFilesInOneLine;

            yOffset += mouseYLastSmooth;
            yOffset = Mathf.Lerp(yOffset, Mathf.Clamp(yOffset, -((lines - 1) * 75), 0f), 25f * deltaTime);
        }

        public override void Draw(SKCanvas canvas)
        {
            var paint = GetPaint();
        }

        List<TrayFile> fileObjects = new List<TrayFile>();

        void AddFileObjects()
        {
            fileObjects.ForEach(x => DestroyLocalObject(x));
            fileObjects = new List<TrayFile>();

            foreach(var x in cachedTrayFiles)
            {
                var f = new TrayFile(this, x, Vec2.zero, UIAlignment.TopLeft)
                {
                    Anchor = new Vec2(0, 0)
                };
                fileObjects.Add(f);
                AddLocalObject(f);
            }
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
