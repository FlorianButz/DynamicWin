using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.Widgets;
using DynamicWin.Utils;
using SkiaSharp;
using System.IO;
using System.Reflection;

namespace DynamicWin.Resources
{
    public class Res
    {
        public static SKTypeface InterRegular { get => LoadTypeface("Resources\\Inter_24pt-Regular.ttf"); }
        public static SKTypeface InterBold { get => LoadTypeface("Resources\\Inter_24pt-ExtraBold.ttf"); }
        public static SKTypeface CascadiaMono { get => LoadTypeface("Resources\\CascadiaMono.ttf"); }

        public static SKBitmap searchIcon;
        public static SKBitmap editIcon;

        public static SKBitmap Battery;
        public static SKBitmap BatteryCharging;
        public static SKBitmap BatteryLevel_10P;
        public static SKBitmap BatteryLevel_25P;
        public static SKBitmap BatteryLevel_50P;
        public static SKBitmap BatteryLevel_75P;
        public static SKBitmap BatteryLevel_Full;
        public static SKBitmap NoBattery;

        public static SKBitmap Next;
        public static SKBitmap Previous;
        public static SKBitmap PlayPause;
        public static SKBitmap Play;
        public static SKBitmap Stop;

        public static SKBitmap Settings;
        public static SKBitmap Tray;
        public static SKBitmap Widgets;
        public static SKBitmap FileIcon;
        public static SKBitmap PlaceItem;
        public static SKBitmap Spotify;

        public static SKBitmap ArrowUp;
        public static SKBitmap ArrowDown;

        public static string TimerOverSound = "Resources\\sounds\\TimerOver.wav";

        private static HomeMenu homeMenu;
        public static HomeMenu HomeMenu { get => homeMenu; }

        public static List<IRegisterableWidget> availableBigWidgets;
        public static List<IRegisterableWidget> availableSmallWidgets;

        public static List<IDynamicWinExtension> extensions;

        public static void CreateStaticMenus()
        {
            homeMenu = new HomeMenu();
        }

        public static void Load()
        {
            editIcon = LoadImg("Resources\\icons\\edit.png");
            searchIcon = LoadImg("Resources\\icons\\search.png");

            Battery = LoadImg("Resources\\icons\\battery\\Battery.png");
            BatteryCharging = LoadImg("Resources\\icons\\battery\\BatteryCharging.png");
            BatteryLevel_10P = LoadImg("Resources\\icons\\battery\\BatteryLevel_10P.png");
            BatteryLevel_25P = LoadImg("Resources\\icons\\battery\\BatteryLevel_25P.png");
            BatteryLevel_50P = LoadImg("Resources\\icons\\battery\\BatteryLevel_50P.png");
            BatteryLevel_75P = LoadImg("Resources\\icons\\battery\\BatteryLevel_75P.png");
            BatteryLevel_Full = LoadImg("Resources\\icons\\battery\\BatteryLevel_Full.png");
            NoBattery = LoadImg("Resources\\icons\\battery\\NoBattery.png");

            Next = LoadImg("Resources\\icons\\playback\\Next.png");
            Previous = LoadImg("Resources\\icons\\playback\\Previous.png");
            PlayPause = LoadImg("Resources\\icons\\playback\\PlayPause.png");
            Play = LoadImg("Resources\\icons\\playback\\Play.png");
            Stop = LoadImg("Resources\\icons\\playback\\Stop.png");

            Settings = LoadImg("Resources\\icons\\home\\Settings.png");
            Tray = LoadImg("Resources\\icons\\home\\Tray.png");
            Widgets = LoadImg("Resources\\icons\\home\\Widgets.png");
            FileIcon = LoadImg("Resources\\icons\\home\\File.png");
            PlaceItem = LoadImg("Resources\\icons\\home\\PlaceItem.png");
            Spotify = LoadImg("Resources\\icons\\home\\Spotify.png");

            ArrowUp = LoadImg("Resources\\icons\\home\\ArrowUp.png");
            ArrowDown = LoadImg("Resources\\icons\\home\\ArrowDown.png");

            RegisterWidgets();

            // Loaded
            System.Diagnostics.Debug.WriteLine("Loaded Resources");
        }

        private static void RegisterWidgets()
        {
            availableBigWidgets = new List<IRegisterableWidget>();
            availableSmallWidgets = new List<IRegisterableWidget>();
            extensions = new List<IDynamicWinExtension>();

            var registerableWidgets = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IRegisterableWidget).IsAssignableFrom(p) && p.IsClass);

            foreach (var registerableWidget in registerableWidgets)
            {
                var iRegisterableWidgetInstance = (IRegisterableWidget)Activator.CreateInstance(registerableWidget);
                iRegisterableWidgetInstance.RegisterWidget(out string widgetName);
                System.Diagnostics.Debug.WriteLine($"Registered widget: {widgetName}");

                if(!iRegisterableWidgetInstance.IsSmallWidget)
                    availableBigWidgets.Add(iRegisterableWidgetInstance);
                else
                    availableSmallWidgets.Add(iRegisterableWidgetInstance);
            }

            // Loading in custom DLLs

            var dirPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Extensions");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            else
            {
                foreach(var file in Directory.GetFiles(dirPath))
                {
                    if (Path.GetExtension(file).ToLower().Equals(".dll"))
                    {
                        System.Diagnostics.Debug.WriteLine(file);
                        var DLL = new Assembly[]{ Assembly.LoadFile(Path.Combine(dirPath, file)) };

                        var extensions = DLL
                            .SelectMany(s => s.GetTypes())
                            .Where(p => typeof(IDynamicWinExtension).IsAssignableFrom(p) && p.IsClass);

                        foreach (var registerableExtension in extensions)
                        {
                            var iRegisterableWidgetInstance = (IDynamicWinExtension)Activator.CreateInstance(registerableExtension);
                            System.Diagnostics.Debug.WriteLine($"Extension sucessfully registered: {iRegisterableWidgetInstance.GetExtensionName()}");
                            Res.extensions.Add(iRegisterableWidgetInstance);
                        }

                        var customRegisterableWidgets = DLL
                            .SelectMany(s => s.GetTypes())
                            .Where(p => typeof(IRegisterableWidget).IsAssignableFrom(p) && p.IsClass);

                        foreach (var registerableWidget in customRegisterableWidgets)
                        {
                            var iRegisterableWidgetInstance = (IRegisterableWidget)Activator.CreateInstance(registerableWidget);
                            iRegisterableWidgetInstance.RegisterWidget(out string widgetName);
                            System.Diagnostics.Debug.WriteLine($"Registered widget: {widgetName}");

                            if (!iRegisterableWidgetInstance.IsSmallWidget)
                                availableBigWidgets.Add(iRegisterableWidgetInstance);
                            else
                                availableSmallWidgets.Add(iRegisterableWidgetInstance);
                        }
                    }
                }
            }
        }

        public static SKBitmap LoadImg(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var image = SKImage.FromEncodedData(stream);
                    return SKBitmap.FromImage(image);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Could not load texture: " + path);
                return searchIcon;
            }
        }

        public static SKTypeface LoadTypeface(string path)
        {
            try
            {
                return SKTypeface.FromFile(path);
            }catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Could not load font: " + path);
                return InterRegular;
            }
        }
    }
}
