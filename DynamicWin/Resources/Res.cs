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
        public static SKBitmap Check;
        public static SKBitmap Add;

        public static SKBitmap AddWidget;

        public static SKBitmap VolumeOn;
        public static SKBitmap VolumeOff;

        public static SKBitmap Brightness;

        public static SKBitmap Weather;
        public static SKBitmap Location;
        public static SKBitmap Cloudy;
        public static SKBitmap Sunny;
        public static SKBitmap Rainy;
        public static SKBitmap Windy;
        public static SKBitmap Thunderstorm;
        public static SKBitmap Foggy;
        public static SKBitmap Snowy;
        public static SKBitmap SevereWeatherWarning;

        public static string TimerOverSound = "Resources\\sounds\\TimerOver.wav";

        private static HomeMenu homeMenu;
        public static HomeMenu HomeMenu { get => homeMenu; set => homeMenu = value; }

        public static List<IRegisterableWidget> availableBigWidgets;
        public static List<IRegisterableWidget> availableSmallWidgets;

        public static List<IDynamicWinExtension> extensions;

        public static void CreateStaticMenus()
        {
            homeMenu = new HomeMenu();
        }

        public static void Load()
        {
            editIcon = LoadImg("edit.png");
            searchIcon = LoadImg("search.png");

            Battery = LoadImg("battery\\Battery.png");
            BatteryCharging = LoadImg("battery\\BatteryCharging.png");
            BatteryLevel_10P = LoadImg("battery\\BatteryLevel_10P.png");
            BatteryLevel_25P = LoadImg("battery\\BatteryLevel_25P.png");
            BatteryLevel_50P = LoadImg("battery\\BatteryLevel_50P.png");
            BatteryLevel_75P = LoadImg("battery\\BatteryLevel_75P.png");
            BatteryLevel_Full = LoadImg("battery\\BatteryLevel_Full.png");
            NoBattery = LoadImg("battery\\NoBattery.png");

            Next = LoadImg("playback\\Next.png");
            Previous = LoadImg("playback\\Previous.png");
            PlayPause = LoadImg("playback\\PlayPause.png");
            Play = LoadImg("playback\\Play.png");
            Stop = LoadImg("playback\\Stop.png");

            Settings = LoadImg("home\\Settings.png");
            Tray = LoadImg("home\\Tray.png");
            Widgets = LoadImg("home\\Widgets.png");
            FileIcon = LoadImg("home\\File.png");
            PlaceItem = LoadImg("home\\PlaceItem.png");
            Spotify = LoadImg("home\\Spotify.png");

            ArrowUp = LoadImg("home\\ArrowUp.png");
            ArrowDown = LoadImg("home\\ArrowDown.png");
            Check = LoadImg("home\\Check.png");
            Add = LoadImg("home\\Add.png");

            AddWidget = LoadImg("settings\\AddWidget.png");

            VolumeOn = LoadImg("home\\VolumeOn.png");
            VolumeOff = LoadImg("home\\VolumeOff.png");
            Brightness = LoadImg("home\\Brightness.png");

            Location = LoadImg("home\\Location.png");
            Weather = LoadImg("home\\Weather.png");
            Cloudy = LoadImg("weather\\Cloudy.png");
            Sunny = LoadImg("weather\\Sunny.png");
            Rainy = LoadImg("weather\\Rainy.png");
            Windy = LoadImg("weather\\Windy.png");
            Thunderstorm = LoadImg("weather\\home\\Thunderstorm.png");
            Foggy = LoadImg("weather\\Foggy.png");
            Snowy = LoadImg("weather\\Snowy.png");
            SevereWeatherWarning = LoadImg("weather\\SevereWeatherWarning.png");

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
                var widgetName = iRegisterableWidgetInstance.WidgetName;
                System.Diagnostics.Debug.WriteLine($"Registered widget: {widgetName}");

                if(!iRegisterableWidgetInstance.IsSmallWidget)
                    availableBigWidgets.Add(iRegisterableWidgetInstance);
                else
                    availableSmallWidgets.Add(iRegisterableWidgetInstance);
            }

            // Loading in custom DLLs

            var dirPath = Path.Combine(SaveManager.SavePath, "Extensions");

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
                            var iRegisterableExtensionInstance = (IDynamicWinExtension)Activator.CreateInstance(registerableExtension);
                            System.Diagnostics.Debug.WriteLine($"Extension sucessfully registered: {iRegisterableExtensionInstance.ExtensionName}");
                            Res.extensions.Add(iRegisterableExtensionInstance);

                            foreach(var registerableWidget in iRegisterableExtensionInstance.GetExtensionWidgets())
                            {
                                var widgetName = registerableWidget.WidgetName;
                                System.Diagnostics.Debug.WriteLine($"Registered widget: {widgetName}");

                                if (!registerableWidget.IsSmallWidget)
                                    availableBigWidgets.Add(registerableWidget);
                                else
                                    availableSmallWidgets.Add(registerableWidget);
                            }
                        }
                    }
                }
            }
        }

        public static SKBitmap LoadImg(string path)
        {
            try
            {
                using (var stream = File.OpenRead("Resources\\icons\\" + path))
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
