using System.Globalization;

namespace DynamicWin.Utils
{
    internal class Theme
    {
        private static Col textMain;
        private static Col textSecond;
        private static Col textThird;
        private static Col prim;
        private static Col sec;
        private static Col islandBackground;
        private static Col success;
        private static Col error;

        public Theme()
        {
            var darkTheme = new ThemeHolder { 
                IslandColor = "#000000",
                TextMain = "#ffffff",
                TextSecond = "#a6a6a6",
                TextThird = "#595959",
                Primary = "#6988b7",
                Secondary = "#061122",
                Success = "#bad844",
                Error = "#d84444"
            };

            var lightTheme = new ThemeHolder
            {
                IslandColor = "#f5f5f5",
                TextMain = "#050505",
                TextSecond = "#404040",
                Primary = "#94b3e0",
                Secondary = "#bed0ed",
                Success = "#b8d522",
                Error = "#d84444"
            };

            ApplyTheme(darkTheme);
        }
        
        public void ApplyTheme(ThemeHolder theme)
        {
            textMain = GetColor(theme.TextMain);
            textSecond = GetColor(theme.TextSecond);
            textThird = GetColor(theme.TextThird);
            prim = GetColor(theme.Primary);
            sec = GetColor(theme.Secondary);
            islandBackground = GetColor(theme.IslandColor);
            success = GetColor(theme.Success);
            error = GetColor(theme.Error);
        }

        public Col GetColor(string hex)
        {
            return Col.FromHex(hex);
        }

        public static Col TextMain { get => textMain; }
        public static Col TextSecond { get => textSecond; }
        public static Col TextThird { get => textThird; }
        public static Col Primary { get => prim; }
        public static Col Secondary { get => sec; }
        public static Col IslandBackground { get => islandBackground; }
        public static Col Success { get => success; }
        public static Col Error { get => error; }
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
    }
}
