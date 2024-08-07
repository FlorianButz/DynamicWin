using System.Globalization;

namespace DynamicWin.Utils
{
    public class Theme
    {
        public static Theme Instance { get; private set; }

        public Theme()
        {
            Instance = this;

            var darkTheme = new ThemeHolder { 
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


            ApplyTheme(darkTheme);
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
