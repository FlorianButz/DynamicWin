using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.Menu.Menus;
using DynamicWin.UI.UIElements;
using DynamicWin.Utils;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using static DynamicWin.UI.Widgets.Small.RegisterUsedDevicesOptions;

namespace DynamicWin.UI.Widgets.Big
{
    class RegisterWeatherWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => false;
        public string WidgetName => "Weather";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new WeatherWidget(parent, position, alignment);
        }
    }

    class RegisterWeatherWidgetSettings : IRegisterableSetting
    {
        public string SettingID => "weatherwidget";

        public string SettingTitle => "Weather Widget";

        public static WeatherWidgetSaveData saveData;

        public struct WeatherWidgetSaveData
        {
            public bool hideLocation;
            public bool useCelcius;
        }

        public void LoadSettings()
        {
            if (SaveManager.Contains(SettingID))
                saveData = JsonConvert.DeserializeObject<WeatherWidgetSaveData>((string)SaveManager.Get(SettingID));
            else
            {
                saveData = new WeatherWidgetSaveData() { useCelcius = true };
            }
        }

        public void SaveSettings()
        {
            SaveManager.Add(SettingID, JsonConvert.SerializeObject(saveData));
        }

        public List<UIObject> SettingsObjects()
        {
            var objects = new List<UIObject>();

            var hideLocationCheckbox = new Checkbox(null, "Hide current location", new Vec2(25, 0), new Vec2(25, 25), null, alignment: UIAlignment.TopLeft);
            hideLocationCheckbox.IsChecked = saveData.hideLocation;

            hideLocationCheckbox.clickCallback += () =>
            {
                saveData.hideLocation = hideLocationCheckbox.IsChecked;
            };

            objects.Add(hideLocationCheckbox);

            var useCelciusCheckbox = new Checkbox(null, "Use Celsius as temperature measurement", new Vec2(25, 0), new Vec2(25, 25), null, alignment: UIAlignment.TopLeft);
            useCelciusCheckbox.IsChecked = saveData.useCelcius;

            useCelciusCheckbox.clickCallback += () =>
            {
                saveData.useCelcius = useCelciusCheckbox.IsChecked;
            };

            objects.Add(useCelciusCheckbox);

            return objects;
        }
    }

    public class WeatherWidget : WidgetBase
    {
        DWText temperatureText;
        DWText weatherText;
        DWText locationText;

        UIObject locationTextReplacement;

        static WeatherFetcher weatherFetcher;

        DWImage weatherTypeIcon;

        public WeatherWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            AddLocalObject(new DWImage(this, Res.Location, new Vec2(20, 17.5f), new Vec2(12.5f, 12.5f), UIAlignment.TopLeft)
            {
                Color = Theme.TextSecond,
                allowIconThemeColor = true
            });

            locationText = new DWText(this, "--", new Vec2(32.5f, 17.5f), UIAlignment.TopLeft)
            {
                TextSize = 15,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextSecond
            };
            AddLocalObject(locationText);

            locationTextReplacement = new UIObject(this, new Vec2(32.5f, 17.5f), new Vec2(75, 15), UIAlignment.TopLeft)
            {
                roundRadius = 5f,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextSecond
            };
            AddLocalObject(locationTextReplacement);

            AddLocalObject(new DWImage(this, Res.Weather, new Vec2(20, 37.5f), new Vec2(12.5f, 12.5f), UIAlignment.TopLeft)
            {
                Color = Theme.TextThird,
                allowIconThemeColor = true
            });

            weatherText = new DWText(this, "--", new Vec2(32.5f, 37.5f), UIAlignment.TopLeft)
            {
                TextSize = 13,
                Font = Res.InterBold,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextThird
            };
            AddLocalObject(weatherText);


            temperatureText = new DWText(this, "--", new Vec2(15, -27.5f), UIAlignment.BottomLeft)
            {
                TextSize = 34,
                Anchor = new Vec2(0, 0.5f),
                Color = Theme.TextMain
            };
            AddLocalObject(temperatureText);

            weatherTypeIcon = new DWImage(this, Res.Weather, new Vec2(0, 0), new Vec2(100, 100), UIAlignment.MiddleRight)
            {
                Color = Theme.TextThird,
                allowIconThemeColor = true
            };

            if(weatherFetcher == null)
                weatherFetcher = new WeatherFetcher();

            weatherFetcher.onWeatherDataReceived += OnWeatherDataReceived;
            weatherFetcher.Fetch();

            locationTextReplacement.SilentSetActive(RegisterWeatherWidgetSettings.saveData.hideLocation);
            locationText.SilentSetActive(!RegisterWeatherWidgetSettings.saveData.hideLocation);
        }

        public override ContextMenu? GetContextMenu()
        {
            var ctx = new ContextMenu();

            var hideLocationItem = new MenuItem() { Header = "Hide Location", IsCheckable = true, IsChecked = RegisterWeatherWidgetSettings.saveData.hideLocation };
            hideLocationItem.Click += (x, y) =>
            {
                RegisterWeatherWidgetSettings.saveData.hideLocation = hideLocationItem.IsChecked;

                locationTextReplacement.SetActive(RegisterWeatherWidgetSettings.saveData.hideLocation);
                locationText.SetActive(!RegisterWeatherWidgetSettings.saveData.hideLocation);

                new RegisterWeatherWidgetSettings().SaveSettings();
                SaveManager.SaveAll();
            };

            ctx.Items.Add(hideLocationItem);

            return ctx;
        }

        WeatherData lastWeatherData;

        void OnWeatherDataReceived(WeatherData weatherData)
        {
            lastWeatherData = weatherData;
            
            weatherText.SetText(weatherData.weatherText);
            locationText.SetText(weatherData.city);

            UpdateIcon(weatherData.weatherText);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            temperatureText.SetText(RegisterWeatherWidgetSettings.saveData.useCelcius ? lastWeatherData.temperatureCelcius : lastWeatherData.temperatureFahrenheit);
        }

        void UpdateIcon(string weather)
        {
            if (weather.ToLower().Contains("sun") || weather.ToLower().Contains("clear"))
                weatherTypeIcon.Image = Res.Sunny;
            else if (weather.ToLower().Contains("cloud") || weather.ToLower().Contains("overcast"))
                weatherTypeIcon.Image = Res.Cloudy;
            else if (weather.ToLower().Contains("rain") || weather.ToLower().Contains("shower"))
                weatherTypeIcon.Image = Res.Rainy;
            else if (weather.ToLower().Contains("thunder"))
                weatherTypeIcon.Image = Res.Thunderstorm;
            else if (weather.ToLower().Contains("snow"))
                weatherTypeIcon.Image = Res.Snowy;
            else if (weather.ToLower().Contains("sleet"))
                weatherTypeIcon.Image = Res.Rainy;
            else if (weather.ToLower().Contains("fog") || weather.ToLower().Contains("haze") || weather.ToLower().Contains("mist"))
                weatherTypeIcon.Image = Res.Foggy;
            else if (weather.ToLower().Contains("windy") || weather.ToLower().Contains("breezy"))
                weatherTypeIcon.Image = Res.Windy;
            else
                weatherTypeIcon.Image = Res.SevereWeatherWarning;
        }

        public override void DrawWidget(SKCanvas canvas)
        {
            base.DrawWidget(canvas);

            var paint = GetPaint();
            paint.Color = GetColor(Theme.WidgetBackground).Value();
            canvas.DrawRoundRect(GetRect(), paint);

            canvas.ClipRoundRect(GetRect(), SKClipOperation.Intersect, true);
            weatherTypeIcon.DrawCall(canvas);
        }
    }

    public class WeatherFetcher
    {
        private WeatherData weatherData = new WeatherData();
        public WeatherData Weather { get => weatherData; }

        public Action<WeatherData> onWeatherDataReceived;

        public void Fetch()
        {
            Task.Run(async () =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync("https://ipinfo.io/geo");
                var location = JsonConvert.DeserializeObject<Location>(response);

                var lat = location.loc.Split(',')[0];
                var lon = location.loc.Split(',')[1];

                System.Diagnostics.Debug.WriteLine($"Latitude: {lat}, Longitude: {lon}");

                string temp = null;
                string weather = null;

                XmlTextReader reader = null;
                try
                {
                    string sAddress = String.Format("https://tile-service.weather.microsoft.com/livetile/front/{0},{1}", lat, lon);

                    int nCpt = 0;

                    reader = new XmlTextReader(sAddress);
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            if (nCpt == 1)
                                temp = reader.Value;
                            else if (nCpt == 2)
                                weather = reader.Value;
                            nCpt++;
                        }
                    }
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }

                string tempF = temp.Replace("°", "");
                double tempC = (Double.Parse(tempF) - 32.0) * (double)5 / 9;
                string tempCText = tempC.ToString("#.#");

                System.Diagnostics.Debug.WriteLine(String.Format("{0}, {1}F({2}°C), {3}", location.city, temp, tempCText, weather));

                weatherData = new WeatherData() { city = location.city, region = location.region, temperatureCelcius = tempCText + "°C", temperatureFahrenheit = tempF + "F", weatherText = weather };
                onWeatherDataReceived?.Invoke(weatherData);

                Thread.Sleep(120000);

                Fetch();
            });
        }
    }

    struct Location
    {
        public string city;
        public string region;
        public string country;
        public string loc;
    }

    public struct WeatherData
    {
        public string city;
        public string region;
        public string weatherText;
        public string temperatureCelcius;
        public string temperatureFahrenheit;
    }
}
