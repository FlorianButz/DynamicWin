using DynamicWin.Main;
using DynamicWin.Utils;
using SkiaSharp;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DynamicWin.UI.UIElements.Custom
{
    internal class Tray : UIObject
    {
        static string[]? cachedTrayFiles;

        float yOffset = 0f;
        float mouseSensitivity = 0.225f;

        DWImage noFilesImage;

        public Tray(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, alignment)
        {
            MainForm.onScrollEvent += OnScroll;

            noFilesImage = new DWImage(this, Resources.Res.PlaceItem, Vec2.zero, new Vec2(100, 100), UIAlignment.Center);

            AddLocalObject(noFilesImage);
        }

        public override ContextMenu? GetContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            if (selectedFiles.Count != 0)
            {
                contextMenu.Items.Insert(0, new MenuItem() { Header = "Selected Files: " + selectedFiles.Count, IsEnabled = false });

                MenuItem removeSelected = new MenuItem() { Header = "Remove Selected File" + ((selectedFiles.Count > 1) ? $"s" : "")  };
                removeSelected.Click += (x, y) =>
                {
                    new List<TrayFile>(selectedFiles).ForEach((f) =>
                    {
                        if (File.Exists(f.FileName)) File.Delete(f.FileName);
                        RemoveFileObject(f);
                    });
                    selectedFiles.Clear();
                };

                MenuItem copySelected = new MenuItem() { Header = "Copy Selected File" + ((selectedFiles.Count > 1) ? $"s" : "") };
                copySelected.Click += (x, y) =>
                {
                    StringCollection paths = new StringCollection();
                    selectedFiles.ForEach((f) => paths.Add(f.FileName));
                    Clipboard.SetFileDropList(paths);
                };

                contextMenu.Items.Add(removeSelected);
                contextMenu.Items.Add(copySelected);
                contextMenu.Items.Add(new Separator());
            }

            MenuItem removeAll = new MenuItem() { Header = "Remove All Files" };
            removeAll.Click += (x, y) =>
            {
                new List<TrayFile>(fileObjects).ForEach((f) =>
                {
                    if (File.Exists(f.FileName)) File.Delete(f.FileName);
                    RemoveFileObject(f);
                });

                selectedFiles.Clear();
            };
            contextMenu.Items.Add(removeAll);

            return contextMenu;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            MainForm.onScrollEvent -= OnScroll;
        }

        Vec2 mouseDownPos = Vec2.zero;
        bool isDragging = false;
        bool canDrag = false;

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            mouseDownPos = new Vec2(RendererMain.CursorPosition.X, RendererMain.CursorPosition.Y);
            canDrag = true;
        }

        void OnScroll(MouseWheelEventArgs e)
        {
            scrollFac += e.Delta * mouseSensitivity;
        }

        float scrollFac = 0f;
        int timer = 0;
        float mouseYLastSmooth = 0f;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            noFilesImage.SetActive(fileObjects.Count <= 0);

            if (timer % 64 == 0) { cachedTrayFiles = GetFiles(); AddFileObjects(); }
            timer++;

            if (cachedTrayFiles == null) return;

            mouseYLastSmooth = Mathf.Lerp(mouseYLastSmooth, 0, 10f * deltaTime);

            var fileW = 60;
            var fileH = 100;
            var spacing = 50;
            var xAdd = 5;

            float allY = 0;
            float scrollLimY = 0;

            var maxFilesInOneLine = (int)(Size.X / (fileW + spacing / 2f));

            var fileMovementSmoothing = 5f;

            for (int i = 0; i < fileObjects.Count; i++)
            {
                var fileObject = fileObjects[i];
                int line = i / maxFilesInOneLine;

                if (!fileObject.IsEnabled) continue;

                fileObject.LocalPosition.X = Mathf.Lerp(fileObject.LocalPosition.X,
                    (fileW + spacing) * (i % maxFilesInOneLine) + xAdd,
                    fileMovementSmoothing * deltaTime);

                fileObject.LocalPosition.Y = Mathf.Lerp(fileObject.LocalPosition.Y,
                    (fileH * line) + yOffset,
                    fileMovementSmoothing * deltaTime);

                allY += fileH;

                if((allY >= Size.Y) && (i % maxFilesInOneLine == 0))
                    scrollLimY += fileH;
            }

            int lines = fileObjects.Count / (maxFilesInOneLine);

            scrollFac = Mathf.Lerp(scrollFac, Mathf.Clamp(scrollFac, -scrollLimY, 0f), 25f * deltaTime);
            yOffset = Mathf.Lerp(yOffset, scrollFac + mouseYLastSmooth, 50f * deltaTime);

            selectedFiles.Clear();
            fileObjects.ForEach((f) =>
            {
                if (f.IsSelected)
                {
                    if (File.Exists(f.FileName))
                    {
                        selectedFiles.Add(f);
                    }
                }
            });

            if (!isDragging && canDrag && IsMouseDown && Vec2.Distance(RendererMain.CursorPosition, mouseDownPos) >= 25)
            {
                isDragging = true;

                List<string> draggedFiles = new List<string>();
                fileObjects.ForEach((f) =>
                {
                    if (f.IsSelected)
                    {
                        if(File.Exists(f.FileName))
                        {
                            draggedFiles.Add(f.FileName);
                        }
                    }
                });

                MainForm.Instance.StartDrag(draggedFiles.ToArray(), () =>
                {
                    List<TrayFile> toRemove = new List<TrayFile>();
                    fileObjects.ForEach((f) =>
                    {
                        if (draggedFiles.Contains(f.FileName))
                        {
                            toRemove.Add(f);
                        }
                    });

                    toRemove.ForEach((x) => RemoveFileObject(x));

                });

                isDragging = false;
                canDrag = false;

                AddFileObjects();
            }

            new List<TrayFile>(fileObjects).ForEach((f) =>
            {
                if(f != null)
                    f.UpdateCall(deltaTime);
            });


            new List<TrayFile>(removedFiles).ForEach((file) =>
            {
                if (file != null)
                {
                    if (!file.IsEnabled) removedFiles.Remove(file);
                    else file.UpdateCall(deltaTime);
                }
            });
        }

        public override void Draw(SKCanvas canvas)
        {
            var rect = GetRect();
            rect.Inflate(25, 0);

            if(fileObjects.Count <= 0)
            {
                var paint = GetPaint();

                var placeRect = new SKRoundRect(SKRect.Create(Position.X, Position.Y, Size.X, Size.Y), 25);
                placeRect.Deflate(5, 5);

                float[] intervals = { 10, 10 };
                paint.PathEffect = SKPathEffect.CreateDash(intervals, 0f);

                paint.IsStroke = true;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;
                paint.StrokeWidth = 2f;

                paint.Color = GetColor(Theme.Primary).Value();

                canvas.DrawRoundRect(placeRect, paint);

                paint.Color = GetColor(Theme.Secondary).Value();
                paint.IsStroke = false;

                placeRect.Deflate(10, 10);

                canvas.DrawRoundRect(placeRect, paint);
            }

            canvas.ClipRoundRect(rect, antialias: true);

            new List<TrayFile>(fileObjects).ForEach((f) =>
            {
                if(f != null)
                    f.DrawCall(canvas);
            });

            new List<TrayFile>(removedFiles).ForEach((file) =>
            {
                if (file != null)
                    file.DrawCall(canvas);
            });
        }

        public List<TrayFile> fileObjects = new List<TrayFile>();
        public List<TrayFile> selectedFiles = new List<TrayFile>();

        public List<TrayFile> removedFiles = new List<TrayFile>();

        void RemoveFileObject(TrayFile file)
        {
            removedFiles.Add(file);

            if(fileObjects.Contains(file)) fileObjects.Remove(file);
            if(selectedFiles.Contains(file)) selectedFiles.Remove(file);

            file.SetActive(false);
        }

        void AddFileObjects()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                List<TrayFile> filesToRemove = new List<TrayFile>();
                fileObjects.ForEach((f) => filesToRemove.Add(f));

                if (cachedTrayFiles == null) cachedTrayFiles = new string[0];

                foreach (var x in cachedTrayFiles)
                {
                    bool hasFileAlready = false;
                    TrayFile fileAlreadyExists = null;
                    fileObjects.ForEach((y) => { if (y.FileName.Equals(x)) { hasFileAlready = true; fileAlreadyExists = y; } });

                    if (fileAlreadyExists != null)
                        filesToRemove.Remove(fileAlreadyExists);

                    if (hasFileAlready) continue;

                    var f = new TrayFile(this, x, Vec2.zero, this, UIAlignment.TopLeft)
                    {
                        Anchor = new Vec2(0, 0)
                    };

                    fileObjects.Add(f);
                    //AddLocalObject(f);

                    Thread.Sleep(20);
                }

                filesToRemove.ForEach((f) =>
                {
                    if (f != null)
                    {
                        //DestroyLocalObject(f);
                        fileObjects.Remove(f);
                    }
                });

            }).Start();
        }

        public static string[]? GetFiles()
        {
            var dirPath = Path.Combine(SaveManager.SavePath, "TrayFiles");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                return null;
            }

            return Directory.GetFiles(dirPath);
        }

        public static void AddFiles(string[] files)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var dirPath = Path.Combine(SaveManager.SavePath, "TrayFiles");

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
            }).Start();
        }
    }
}
