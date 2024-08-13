using DynamicWin.Resources;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public static List<string> smallWidgetsLeft;
        public static List<string> smallWidgetsRight;
        public static List<string> smallWidgetsMiddle;
        public static List<string> bigWidgets;

        //public static ThemeHolder CustomTheme { get => customTheme; set => customTheme = value; }

        public static void InitializeSettings()
        {
            try
            {

                if (SaveManager.Contains("settings"))
                {
                    IslandMode = ((Int64)SaveManager.Get("settings.islandmode") == 0) ? IslandObject.IslandMode.Island : IslandObject.IslandMode.Notch;

                    AllowBlur = (bool)SaveManager.Get("settings.allowblur");
                    AllowAnimation = (bool)SaveManager.Get("settings.allowanimtion");
                    AntiAliasing = (bool)SaveManager.Get("settings.antialiasing");

                    Theme = (int)((Int64)SaveManager.Get("settings.theme"));

                    Settings.smallWidgetsLeft = new List<string>();
                    Settings.smallWidgetsRight = new List<string>();
                    Settings.smallWidgetsMiddle = new List<string>();
                    Settings.bigWidgets = new List<string>();

                    var smallWidgetsLeft = (JArray)SaveManager.Get("settings.smallwidgetsleft");
                    var smallWidgetsRight = (JArray)SaveManager.Get("settings.smallwidgetsright");
                    var smallWidgetsMiddle = (JArray)SaveManager.Get("settings.smallwidgetsmiddle");
                    var bigWidgets = (JArray)SaveManager.Get("settings.bigwidgets");

                    foreach (var x in smallWidgetsLeft)
                        Settings.smallWidgetsLeft.Add(x.ToString());
                    foreach (var x in smallWidgetsRight)
                        Settings.smallWidgetsRight.Add(x.ToString());
                    foreach (var x in smallWidgetsMiddle)
                        Settings.smallWidgetsMiddle.Add(x.ToString());
                    foreach (var x in bigWidgets)
                        Settings.bigWidgets.Add(x.ToString());
                }
                else
                {
                    smallWidgetsLeft = new List<string>();
                    smallWidgetsRight = new List<string>();
                    smallWidgetsMiddle = new List<string>();
                    bigWidgets = new List<string>();

                    smallWidgetsRight.Add("DynamicWin.UI.Widgets.Small.RegisterUsedDevicesWidget");
                    smallWidgetsLeft.Add("DynamicWin.UI.Widgets.Small.RegisterTimeWidget");
                    bigWidgets.Add("DynamicWin.UI.Widgets.Big.RegisterMediaWidget");

                    IslandMode = IslandObject.IslandMode.Island;
                    AllowBlur = true;
                    AllowAnimation = true;
                    AntiAliasing = true;

                    Settings.Theme = 0;

                    SaveManager.SaveData.Add("settings", 1);
                }


                // This must be run after loading all settings
                AfterSettingsLoaded();
            }catch(Exception e)
            {
                MessageBox.Show("An error occured trying to load the settings. Please revert back to the default settings by deleting the \"Settings.json\" file located under \"%appdata%/DynamicWin/\".");
            }
        }

        static void AfterSettingsLoaded()
        {
            DynamicWin.Utils.Theme.Instance.UpdateTheme();

            var customOptions = SettingsMenu.LoadCustomOptions();

            foreach (var item in customOptions)
            {
                item.LoadSettings();
            }
        }

        public static void Save()
        {
            SaveManager.Add("settings.islandmode", (IslandMode == IslandObject.IslandMode.Island) ? 0 : 1);

            SaveManager.Add("settings.allowblur", AllowBlur);
            SaveManager.Add("settings.allowanimtion", AllowAnimation);
            SaveManager.Add("settings.antialiasing", AntiAliasing);

            SaveManager.Add("settings.theme", Theme);

            SaveManager.Add("settings.smallwidgetsleft", smallWidgetsLeft);
            SaveManager.Add("settings.smallwidgetsright", smallWidgetsRight);
            SaveManager.Add("settings.smallwidgetsmiddle", smallWidgetsMiddle);
            SaveManager.Add("settings.bigwidgets", bigWidgets);

            SaveManager.SaveAll();
        }
    }
}
