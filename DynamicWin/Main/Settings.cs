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
        public static IslandObject.IslandMode IslandMode { get; set; }
        public static bool AllowBlur { get; set; }
        public static bool AllowAnimation { get; set; }
        public static bool AntiAliasing { get; set; }

        public static bool UseCustomTheme { get; set; }
        public static ThemeHolder CustomTheme { get; set; }

        public static void InitializeSettings()
        {
            IslandMode = IslandObject.IslandMode.Notch;
            AllowBlur = true;
            AllowAnimation = true;
            AntiAliasing = true;

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
                Theme.Instance.ApplyTheme(CustomTheme);
        }
    }
}
