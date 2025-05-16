using DynamicWin.Utils;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Resources;
using Newtonsoft.Json;
using SkiaSharp;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

/*
*   Overview:
*    - Implement new Weather API that allows the user to change their location and display its weather.
*    - Migrates existing settings configured by user from the legacy WeatherWidget configuration.
*    - Allows user to finally change weather location than locking them with their current IP address geo-location.
*    
*   Author:                 Megan Park
*   GitHub:                 https://github.com/59xa
*   Implementation Date:    16 May 2024
*   Last Modified:          16 May 2024 14:26 KST (UTC+9)
*/

namespace DynamicWin.UI.Widgets.Big
{
    // Register widget
    class RegisterWeatherWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => false;
        public string WidgetName => "Weather";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new WeatherWidget(parent, position, alignment);
        }
    }

    // Initialise widget configurations
    class RegisterWeatherWidgetSettings : IRegisterableSetting
    {
        public string SettingID => "weatherwidget";
        public string SettingTitle => "Weather";
        public static WeatherWidgetSaveData saveData;

        public struct WeatherWidgetSaveData
        {
            public bool hideLocation;
            public bool useCelsius;
            public string selectedLocation;
            public int countryIndex;
            public int cityIndex;
            public int totalCities;
        }

        // Method to load existing configurations
        public void LoadSettings()
        {
            if (SaveManager.Contains(SettingID))
            {
                string _j = (string)SaveManager.Get(SettingID);
                JObject _o = JObject.Parse(_j);

                // If a legacy widget configuration has been detected, migrate existing settings into the new configuration
                if (_o.ContainsKey("useCelcius") && !_o.ContainsKey("useCelsius"))
                {
                    Debug.WriteLine("WeatherWidget contains old configuration from legacy widget, migrating.");
                    _o["useCelsius"] = _o["useCelcius"];
                    _o.Remove("useCelcius");

                    // Include new defaults
                    _o["selectedLocation"] = "Default";
                    _o["countryIndex"] = 0;
                }

                saveData = JsonConvert.DeserializeObject<WeatherWidgetSaveData>(_o.ToString());
            }
            else saveData = new WeatherWidgetSaveData() { useCelsius = true, countryIndex = 0, selectedLocation = "Default" };
        }

        // Save configuration settings
        public void SaveSettings() { SaveManager.Add(SettingID, JsonConvert.SerializeObject(saveData)); }

        // Define interface user can interact with to configure the widget
        public List<UIObject> SettingsObjects()
        {
            var objects = new List<UIObject>();

            // Logic for hiding weather location
            var hideLocationCheckbox = new Checkbox(null, "Hide location", new Vec2(25, 25), new Vec2(25, 25), null, alignment: UIAlignment.TopLeft);
            hideLocationCheckbox.IsChecked = saveData.hideLocation;

            hideLocationCheckbox.clickCallback += () => saveData.hideLocation = hideLocationCheckbox.IsChecked;

            // Logic for toggling temperature measurement preference
            var useCelsiusCheckbox = new Checkbox(null, "Use Celsius as temperature measurement", new Vec2(25, 0), new Vec2(25, 25), null, alignment: UIAlignment.TopLeft);
            useCelsiusCheckbox.IsChecked = saveData.useCelsius;

            useCelsiusCheckbox.clickCallback += () => saveData.useCelsius = useCelsiusCheckbox.IsChecked;

            // Logic for changing weather location
            var selectLocationText = new DWText(null, "Change weather location", new Vec2(0, 0), UIAlignment.TopLeft);

            var _countries = WeatherAPI.LoadCountryNames();

            // Opens context menus for user to change the weather location
            var selectLocationButton = new DWTextButton(null, saveData.selectedLocation, new Vec2(50, 25), new Vec2(150, 30), null, alignment: UIAlignment.TopLeft);
            selectLocationButton.clickCallback += () =>
            {
                var contextMenu = new System.Windows.Controls.ContextMenu();

                var countryTitle = new System.Windows.Controls.MenuItem
                {
                    Header = "Select Country",
                    IsEnabled = false,
                    FontWeight = System.Windows.FontWeights.Bold
                };
                contextMenu.Items.Add(countryTitle);

                // Display a list of countries
                for (int countryIdx = 0; countryIdx < _countries.Length; countryIdx++)
                {
                    var country = _countries[countryIdx];
                    var menuItem = new System.Windows.Controls.MenuItem { Header = country };
                    int capturedCountryIdx = countryIdx;
                    menuItem.Click += (s, e) =>
                    {
                        if (capturedCountryIdx == 0) // If country index == 0, set default configurations
                        {
                            selectLocationButton.Text.SetText(_countries[capturedCountryIdx]);
                            saveData.selectedLocation = _countries[capturedCountryIdx];
                            saveData.countryIndex = capturedCountryIdx;
                            return; // Breaks loop, does not prompt the second context menu
                        }

                        var cities = WeatherAPI.LoadCityNames(capturedCountryIdx);
                        var cityContextMenu = new System.Windows.Controls.ContextMenu();

                        var cityTitle = new System.Windows.Controls.MenuItem
                        {
                            Header = "Select City",
                            IsEnabled = false,
                            FontWeight = System.Windows.FontWeights.Bold
                        };
                        cityContextMenu.Items.Add(cityTitle);

                        // Display a list of cities
                        for (int cityIdx = 0; cityIdx < cities.Length; cityIdx++)
                        {
                            var city = cities[cityIdx];
                            var cityMenuItem = new System.Windows.Controls.MenuItem { Header = city };
                            int capturedCityIdx = cityIdx;
                            var _w = new WeatherAPI();
                            cityMenuItem.Click += (cs, ce) =>
                            {
                                selectLocationButton.Text.SetText(city);
                                saveData.selectedLocation = city;
                                saveData.countryIndex = capturedCountryIdx;
                                saveData.cityIndex = capturedCityIdx;
                                _w.Fetch(capturedCityIdx, "city");
                            };
                            cityContextMenu.Items.Add(cityMenuItem);
                        }

                        cityContextMenu.IsOpen = true;
                        cityContextMenu.MaxHeight = 500f;
                    };
                    contextMenu.Items.Add(menuItem);
                }

                // Context menu display configuration
                contextMenu.IsOpen = true;
                contextMenu.MaxHeight = 500f;
                contextMenu.VerticalOffset = selectLocationButton.Position.Y - 400;
                contextMenu.HorizontalOffset = selectLocationButton.Position.X - 1100;
            };

            // Add all objects
            objects.Add(hideLocationCheckbox);
            objects.Add(useCelsiusCheckbox);
            objects.Add(selectLocationText);
            objects.Add(selectLocationButton);

            return objects;
        }
    }

    // Widget interface logic for HomeMenu display
    class WeatherWidget : WidgetBase
    {
        DWText _TemperatureText;
        DWText _ForecastText;
        DWText _LocationText;

        UIObject _LocationTextReplacement;

        static WeatherAPI _WeatherAPI;

        DWImage _ForecastIcon;

        public WeatherWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            // Location icon
            AddLocalObject(new DWImage(this, Res.Location, new Vec2(20, 17.5f), new Vec2(12.5f, 12.5f), UIAlignment.TopLeft)
            {
                Color = Theme.TextSecond,
                allowIconThemeColor = true,
            });

            // Placeholder text if API is misconfigured or offline
            _LocationText = new DWText(this, "--", new Vec2(32.5f, 17.5f), UIAlignment.TopLeft)
            {
                TextSize = 15,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextSecond
            };

            AddLocalObject(_LocationText);

            // Displays current city as configured by user
            _LocationTextReplacement = new UIObject(this, new Vec2(32.5f, 17.5f), new Vec2(75, 15), UIAlignment.TopLeft)
            {
                roundRadius = 5f,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextSecond
            };
            AddLocalObject(_LocationTextReplacement);

            // Displays small version of the forecast icon
            AddLocalObject(new DWImage(this, Res.Weather, new Vec2(20, 37.5f), new Vec2(12.5f, 12.5f), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                allowIconThemeColor = true
            });

            // Displays current forecast
            _ForecastText = new DWText(this, "--", new Vec2(32.5f, 37.5f), UIAlignment.TopLeft)
            {
                TextSize = 13,
                Font = Res.InterBold,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextThird
            };

            AddLocalObject(_ForecastText);

            // Displays city's current temperature value
            _TemperatureText = new DWText(this, "--", new Vec2(15, -27.5f), UIAlignment.BottomLeft)
            {
                TextSize = 34,
                Font = Res.InterBold,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextMain
            };

            AddLocalObject(_TemperatureText);

            // Displays large version of the forecast icon
            _ForecastIcon = new DWImage(this, Res.Weather, new Vec2(0, 0), new Vec2(100, 100), UIAlignment.MiddleRight)
            {
                Color = Theme.TextThird,
                allowIconThemeColor = true
            };

            // Initialises weather API
            if (_WeatherAPI == null) _WeatherAPI = new WeatherAPI();

            // Updates weather information display
            _WeatherAPI._OnWeatherDataReceived += OnWeatherDataReceived;
            var _countries = WeatherAPI.LoadCountryNames();

            // If country index is set to 0, display location based on user's IP address
            if (_countries[RegisterWeatherWidgetSettings.saveData.countryIndex] == "Default") _WeatherAPI.Fetch(RegisterWeatherWidgetSettings.saveData.countryIndex, "default");
            else _WeatherAPI.Fetch(RegisterWeatherWidgetSettings.saveData.cityIndex, "city"); // Trigger if user configures weather values apart from Default

            // Handles logic if user configures widget to hide weather location
            _LocationTextReplacement.SilentSetActive(RegisterWeatherWidgetSettings.saveData.hideLocation);
            _LocationText.SilentSetActive(!RegisterWeatherWidgetSettings.saveData.hideLocation);
        }


        NewWeatherData lastWeatherData;
        // Logic to handle weather display updates
        void OnWeatherDataReceived(NewWeatherData weatherData)
        {
            lastWeatherData = weatherData;

            _ForecastText.SetText(weatherData.weatherText);
            _LocationText.SetText(weatherData.city);

            UpdateIcon(weatherData.weatherText);
        }

        // Logic to handle forecast icon displays
        void UpdateIcon(string weather)
        {
            string w = weather.ToLower();
            switch (w)
            {
                case string s when s.Contains("sun") || s.Contains("clear"):
                    _ForecastIcon.Image = Res.Sunny;
                    break;
                case string s when s.Contains("cloud") || s.Contains("overcast"):
                    _ForecastIcon.Image = Res.Cloudy;
                    break;
                case string s when s.Contains("rain") || s.Contains("shower"):
                    _ForecastIcon.Image = Res.Rainy;
                    break;
                case string s when s.Contains("thunder"):
                    _ForecastIcon.Image = Res.Thunderstorm;
                    break;
                case string s when s.Contains("snow"):
                    _ForecastIcon.Image = Res.Snowy;
                    break;
                case string s when s.Contains("sleet"):
                    _ForecastIcon.Image = Res.Rainy;
                    break;
                case string s when s.Contains("fog") || s.Contains("haze") || s.Contains("mist"):
                    _ForecastIcon.Image = Res.Foggy;
                    break;
                case string s when s.Contains("windy") || s.Contains("breezy"):
                    _ForecastIcon.Image = Res.Windy;
                    break;
                default:
                    _ForecastIcon.Image = Res.SevereWeatherWarning;
                    break;
            }
        }

        // Override logic for text animations when updating forecast values
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            _TemperatureText.SetText(RegisterWeatherWidgetSettings.saveData.useCelsius ? lastWeatherData.celsius : lastWeatherData.fahrenheit);
        }

        // Override logic for widget aesthetics
        public override void DrawWidget(SKCanvas canvas)
        {
            base.DrawWidget(canvas);

            var _p = GetPaint();
            _p.Color = GetColor(Theme.WidgetBackground).Value();
            canvas.DrawRoundRect(GetRect(), _p);

            canvas.ClipRoundRect(GetRect(), SKClipOperation.Intersect, true);
            _ForecastIcon.DrawCall(canvas);
        }
    }
}
