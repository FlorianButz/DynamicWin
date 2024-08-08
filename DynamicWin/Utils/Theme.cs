using DynamicWin.Main;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DynamicWin.Utils
{
    public class Theme
    {
        public static Theme Instance { get; private set; }

        public Theme()
        {
            Instance = this;

            UpdateTheme();
        }
        
        public void ApplyTheme(ThemeHolder theme)
        {
            TextMain = GetColor(theme.TextMain);
            TextSecond = GetColor(theme.TextSecond);
            TextThird = GetColor(theme.TextThird);
            Primary = GetColor(theme.Primary);
            Secondary = GetColor(theme.Secondary);
            IslandBackground = GetColor(theme.IslandColor);
            Success = GetColor(theme.Success);
            Error = GetColor(theme.Error);
            IconColor = GetColor(theme.IconColor);
            WidgetBackground = GetColor(theme.WidgetBackground);
        }

        public Col GetColor(string hex)
        {
            return Col.FromHex(hex);
        }

        public void UpdateTheme(bool refreshRenderer = false)
        {
            var darkTheme = new ThemeHolder
            {
                IslandColor = "#000000",
                TextMain = "#ffffff",
                TextSecond = "#a6a6a6",
                TextThird = "#595959",
                Primary = "#6988b7",
                Secondary = "#061122",
                Success = "#bad844",
                Error = "#d84444",
                IconColor = "#ffffff",
                WidgetBackground = "#11ffffff"
            };

            var lightTheme = new ThemeHolder
            {
                IslandColor = "#ffffff",
                TextMain = "#000000",
                TextSecond = "#333333",
                TextThird = "#666666",
                Primary = "#7a9fd6",
                Secondary = "#c1d7f7",
                Success = "#99d844",
                Error = "#ff6666",
                IconColor = "#000000",
                WidgetBackground = "#11000000"
            };

            if (Settings.Theme != -1)
            {
                if (Settings.Theme == 0)
                    ApplyTheme(darkTheme);
                else
                    ApplyTheme(lightTheme);
            }
            else
            {
                try
                {
                    string defaultTheme = "{\r\n  \"IslandColor\": \"#000000\",\r\n  \"TextMain\": \"#dd11dd\",\r\n  \"TextSecond\": \"#aa11aa\",\r\n  \"TextThird\": \"#661166\",\r\n  \"Primary\": \"#dd11dd\",\r\n  \"Secondary\": \"#111111\",\r\n  \"Success\": \"#991199\",\r\n  \"Error\": \"#3311933\",\r\n  \"IconColor\": \"#dd11dd\",\r\n  \"WidgetBackground\": \"#11ffffff\"\r\n}";

                    var directory = SaveManager.SavePath;
                    var fileName = "Theme.dwt";
                    
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                    var fullPath = Path.Combine(directory, fileName);
                    if (!File.Exists(fullPath))
                    {
                        File.Create(fullPath);
                        File.WriteAllText(fullPath, defaultTheme);
                    }

                    var json = File.ReadAllText(fullPath);
                    
                    if(string.IsNullOrEmpty(json))
                    {
                        File.WriteAllText(fullPath, defaultTheme);
                        json = defaultTheme;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Loaded theme: " + json);

                    var customTheme = new ThemeHolder();
                    customTheme = JsonConvert.DeserializeObject<ThemeHolder>(json);
                    ApplyTheme(customTheme);
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't load custom theme.");
                    ApplyTheme(darkTheme);
                }
            }

            if (refreshRenderer)
                MainForm.Instance.AddRenderer();
        }

        public static Col TextMain { get; set; }
        public static Col TextSecond { get; set; }
        public static Col TextThird { get; set; }
        public static Col Primary { get; set; }
        public static Col Secondary { get; set; }
        public static Col IslandBackground { get; set; }
        public static Col Success { get; set; }
        public static Col Error { get; set; }
        public static Col IconColor { get; set; }
        public static Col WidgetBackground { get; set; }
    }

    public struct ThemeHolder
    {
        public string TextMain;
        public string TextSecond;
        public string TextThird;
        public string Primary;
        public string Secondary;
        public string IslandColor;
        public string Success;
        public string Error;
        public string IconColor;
        public string WidgetBackground;
    }
}
