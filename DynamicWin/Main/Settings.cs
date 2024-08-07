using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Main
{
    public class Settings
    {
        private static IslandObject.IslandMode islandMode;
        private static bool allowBlur;
        private static bool allowAnimation;
        private static bool antiAliasing;
        private static int theme;
        private static bool useCustomTheme;
        private static ThemeHolder customTheme;

        public static IslandObject.IslandMode IslandMode { get => islandMode; set => islandMode = value; }
        public static bool AllowBlur { get => allowBlur; set => allowBlur = value; }
        public static bool AllowAnimation { get => allowAnimation; set => allowAnimation = value; }
        public static bool AntiAliasing { get => antiAliasing; set => antiAliasing = value; }

        public static int Theme { get => theme; set => theme = value; }

        public static bool UseCustomTheme { get => useCustomTheme; set => useCustomTheme = value; }
        public static ThemeHolder CustomTheme { get => customTheme; set => customTheme = value; }

        public static void InitializeSettings()
        {
            System.Diagnostics.Debug.WriteLine("wdjoadjia");

            IslandMode = IslandObject.IslandMode.Island;
            AllowBlur = true;
            AllowAnimation = true;
            AntiAliasing = true;

            Settings.Theme = 0;

            UseCustomTheme = false;
            CustomTheme = new ThemeHolder
            {
                IslandColor = "#1A535C", // Deep Teal (background)
                TextMain = "#FFFD82", // Bright Yellow (main text)
                TextSecond = "#FF6B6B", // Coral Red (secondary text)
                TextThird = "#4ECDC4", // Mint Green (tertiary text)
                Primary = "#FFE66D", // Sunshine Yellow (primary color)
                Secondary = "#FF1654", // Watermelon Pink (secondary color)
                Success = "#06D6A0", // Aquamarine (success color)
                Error = "#EF476F", // Punch Pink (error color)
                IconColor = "#FFD166", // Mango Yellow (icon color)
                WidgetBackground = "#11FFFFFF" // Mango Yellow (icon color)
            };

            // This must be run after loading all settings
            AfterSettingsLoaded();
        }

        static void AfterSettingsLoaded()
        {
            if(UseCustomTheme)
                DynamicWin.Utils.Theme.Instance.ApplyTheme(CustomTheme);
        }
    }
}
