using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicWin.Utils;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Resources;
using Newtonsoft.Json;
using System.Net.Http;
using System.Diagnostics;
using DynamicWin.UI.Menu;
using System.IO;

/*
*   Overview:
*    - Implement new Weather API that allows the user to change their location and display its weather.
*/

namespace DynamicWin.UI.Widgets.Big
{
    class RegisterNewWeatherWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => false;
        public string WidgetName => "Weather v2";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new NewWeatherWidget(parent, position, alignment);
        }
    }

    class RegisterableNewWeatherWidgetSettings : IRegisterableSetting
    {
        public string SettingID => "newweatherwidget";
        public string SettingTitle => "Weather v2";
        public static NewWeatherWidgetSaveData saveData;

        public struct NewWeatherWidgetSaveData
        {
            public bool hideLocation;
            public bool useCelsius;
            public string selectedLocation;
        }

        public void LoadSettings()
        {
            if (SaveManager.Contains(SettingID))
                saveData = JsonConvert.DeserializeObject<NewWeatherWidgetSaveData>((string)SaveManager.Get(SettingID));
            else saveData = new NewWeatherWidgetSaveData() { useCelsius = true };
        }

        public void SaveSettings() { SaveManager.Add(SettingID, JsonConvert.SerializeObject(saveData)); }

        public List<UIObject> SettingsObjects()
        {
            var objects = new List<UIObject>();

            var hideLocationCheckbox = new Checkbox(null, "Hide location", new Vec2(25, 25), new Vec2(25, 25), null, alignment: UIAlignment.TopLeft);
            hideLocationCheckbox.IsChecked = saveData.hideLocation;

            hideLocationCheckbox.clickCallback += () => saveData.hideLocation = hideLocationCheckbox.IsChecked;

            var useCelsiusCheckbox = new Checkbox(null, "Use Celsius as temperature measurement", new Vec2(25, 0), new Vec2(25, 25), null, alignment: UIAlignment.TopLeft);
            useCelsiusCheckbox.IsChecked = saveData.useCelsius;

            useCelsiusCheckbox.clickCallback += () => saveData.useCelsius = useCelsiusCheckbox.IsChecked;

            var selectLocationText = new DWText(null, "Change weather location", new Vec2(0, 0), UIAlignment.TopLeft);

            var selectLocationButton = new DWTextButton(null, "Default", new Vec2(50, 25), new Vec2(150, 30), null, alignment: UIAlignment.TopLeft);
            selectLocationButton.clickCallback += () =>
            {
                MenuManager.OpenMenu(new WeatherMenu());
            };

            objects.Add(hideLocationCheckbox);
            objects.Add(useCelsiusCheckbox);
            objects.Add(selectLocationText);
            objects.Add(selectLocationButton);

            return objects;
        }
    }

    class NewWeatherWidget : WidgetBase
    {
        DWText _TemperatureText;
        DWText _WeatherText;
        DWText _LocationText;

        UIObject _LocationTextReplacement;

        static WeatherAPI _WeatherAPI;

        DWImage _ForecastIcon;

        public NewWeatherWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            AddLocalObject(new DWImage(this, Res.Location, new Vec2(20, 17.5f), new Vec2(12.5f, 12.5f), UIAlignment.TopLeft)
            {
                Color = Theme.TextSecond,
                allowIconThemeColor = true,
            });

            _LocationText = new DWText(this, "--", new Vec2(32.5f, 17.5f), UIAlignment.TopLeft)
            {
                TextSize = 15,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextSecond
            };

            AddLocalObject(_LocationText);

            AddLocalObject(new DWImage(this, Res.Weather, new Vec2(20, 37.5f), new Vec2(12.5f, 12.5f), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                allowIconThemeColor = true
            });

            _WeatherText = new DWText(this, "--", new Vec2(32.5f, 37.5f), UIAlignment.TopLeft)
            {
                TextSize = 13,
                Font = Res.InterBold,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextThird
            };

            AddLocalObject(_WeatherText);

            _TemperatureText = new DWText(this, "--", new Vec2(15, -27.5f), UIAlignment.BottomLeft)
            {
                TextSize = 34,
                Font = Res.InterBold,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextMain
            };

            AddLocalObject(_TemperatureText);

            _ForecastIcon = new DWImage(this, Res.Weather, new Vec2(0, 0), new Vec2(100, 100), UIAlignment.MiddleRight)
            {
                Color = Theme.TextThird,
                allowIconThemeColor = true
            };
        }
    }

    public class WeatherAPI
    {
        private NewWeatherData _WeatherData = new NewWeatherData();
        public NewWeatherData _Weather { get => _WeatherData; }

        public Action<NewWeatherData>? _OnWeatherDataReceived;

        //public void Fetch()
        //{
        //    Task.Run(async () =>
        //    {
        //        var httpClient = new HttpClient();
        //        var response = await httpClient.GetStringAsync("https://");
        //    });
        //}
    }

    struct NewLocation
    {
        public string city;
        public string region;
        public string country;
        public string location;
    }

    public struct NewWeatherData
    {
        public string city;
        public string region;
        public string weatherText;
        public string celsius;
        public string fahrenheit;
    }
}
