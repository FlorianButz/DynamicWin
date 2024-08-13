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

            var candyTheme = GetTheme("{\r\n  \"IslandColor\": \"#f7cac9\",\r\n  \"TextMain\": \"#ff6f61\",\r\n  \"TextSecond\": \"#d66853\",\r\n  \"TextThird\": \"#b94a45\",\r\n  \"Primary\": \"#ff6f61\",\r\n  \"Secondary\": \"#f7cac9\",\r\n  \"Success\": \"#88b04b\",\r\n  \"Error\": \"#c0392b\",\r\n  \"IconColor\": \"#ff6f61\",\r\n  \"WidgetBackground\": \"#88ffebee\"\r\n}\r\n");
            var forestDawnTheme = GetTheme("{\r\n  \"IslandColor\": \"#1c1c1c\",\r\n  \"TextMain\": \"#8e44ad\",\r\n  \"TextSecond\": \"#9b59b6\",\r\n  \"TextThird\": \"#6c3483\",\r\n  \"Primary\": \"#8e44ad\",\r\n  \"Secondary\": \"#34495e\",\r\n  \"Success\": \"#27ae60\",\r\n  \"Error\": \"#e74c3c\",\r\n  \"IconColor\": \"#8e44ad\",\r\n  \"WidgetBackground\": \"#2c3e50\"\r\n}\r\n");
            var sunsetGlow = GetTheme("{\r\n  \"IslandColor\": \"#2c3e50\",\r\n  \"TextMain\": \"#f39c12\",\r\n  \"TextSecond\": \"#e67e22\",\r\n  \"TextThird\": \"#d35400\",\r\n  \"Primary\": \"#e74c3c\",\r\n  \"Secondary\": \"#c0392b\",\r\n  \"Success\": \"#27ae60\",\r\n  \"Error\": \"#c0392b\",\r\n  \"IconColor\": \"#f39c12\",\r\n  \"WidgetBackground\": \"#22ecf0f1\"\r\n}\r\n");

            if (Settings.Theme != -1)
            {
                switch (Settings.Theme)
                {
                    case 0:
                    ApplyTheme(darkTheme);
                        break;
                    case 1:
                        ApplyTheme(lightTheme);
                        break;
                    case 2:
                        ApplyTheme(candyTheme);
                        break;
                    case 3:
                        ApplyTheme(forestDawnTheme);
                        break;
                    case 4:
                        ApplyTheme(sunsetGlow);
                        break;
                }
            }

            var customTheme = new ThemeHolder();

            try
            {
                string defaultTheme = "{\r\n  \"IslandColor\": \"#000000\",\r\n  \"TextMain\": \"#dd11dd\",\r\n  \"TextSecond\": \"#aa11aa\",\r\n  \"TextThird\": \"#661166\",\r\n  \"Primary\": \"#dd11dd\",\r\n  \"Secondary\": \"#111111\",\r\n  \"Success\": \"#991199\",\r\n  \"Error\": \"#3311933\",\r\n  \"IconColor\": \"#dd11dd\",\r\n  \"WidgetBackground\": \"#11ffffff\"\r\n}";

                var directory = SaveManager.SavePath;
                var fileName = "Theme.json";

                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var fullPath = Path.Combine(directory, fileName);
                if (!File.Exists(fullPath))
                {
                    var fs = File.Create(fullPath);
                    fs.Close();
                    File.WriteAllText(fullPath, defaultTheme);
                }

                var json = File.ReadAllText(fullPath);

                if (string.IsNullOrEmpty(json))
                {
                    File.WriteAllText(fullPath, defaultTheme);
                    json = defaultTheme;
                }

                System.Diagnostics.Debug.WriteLine("Loaded theme: " + json);

                customTheme = GetTheme(json);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Couldn't load custom theme.");
                customTheme = darkTheme;
            }

            if(Settings.Theme == -1)
            {
                ApplyTheme(customTheme);
            }

            if (refreshRenderer)
                MainForm.Instance.AddRenderer();
        }

        ThemeHolder GetTheme(string json)
        {
            var customTheme = new ThemeHolder();
            customTheme = JsonConvert.DeserializeObject<ThemeHolder>(json);
            return customTheme;
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
