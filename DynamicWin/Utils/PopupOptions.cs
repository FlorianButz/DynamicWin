using DynamicWin.UI;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.Widgets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    internal class PopupOptions : IRegisterableSetting
    {
        public string SettingID => "popup_options";

        public string SettingTitle => "Pop-up Options";

        public static PopupOptionsSave saveData;

        public struct PopupOptionsSave
        {
            public bool volumePopup;
            public bool brightnessPopup;
        }

        public void LoadSettings()
        {
            if (SaveManager.Contains(SettingID))
            {
                saveData = JsonConvert.DeserializeObject<PopupOptionsSave>((string)SaveManager.Get(SettingID));
            }
            else
            {
                saveData = new PopupOptionsSave() { volumePopup = true, brightnessPopup = true };
            }
        }

        public void SaveSettings()
        {
            SaveManager.Add(SettingID, JsonConvert.SerializeObject(saveData));
        }

        public List<UIObject> SettingsObjects()
        {
            var objects = new List<UIObject>();

            var volume = new Checkbox(null, "Display volume pop-up", new Vec2(25, 0), new Vec2(25, 25), null, UIAlignment.TopLeft);

            volume.clickCallback += () =>
            {
                saveData.volumePopup = volume.IsChecked;
            };

            volume.IsChecked = saveData.volumePopup;
            volume.Anchor.X = 0;
            objects.Add(volume);

            var brightness = new Checkbox(null, "Display brightness pop-up", new Vec2(25, 0), new Vec2(25, 25), null, UIAlignment.TopLeft);

            brightness.clickCallback += () =>
            {
                saveData.brightnessPopup = volume.IsChecked;
            };

            brightness.IsChecked = saveData.brightnessPopup;
            brightness.Anchor.X = 0;
            objects.Add(brightness);

            return objects;
        }
    }
}
