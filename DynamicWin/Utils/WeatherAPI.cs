using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Xml;
using CsvHelper;
using DynamicWin.Main;
using DynamicWin.Resources;
using DynamicWin.UI.Widgets.Big;
using Newtonsoft.Json;

namespace DynamicWin.Utils
{
    public class WeatherAPI
    {
        private NewWeatherData _WeatherData = new NewWeatherData();
        public NewWeatherData _Weather { get => _WeatherData; }

        public Action<NewWeatherData>? _OnWeatherDataReceived;

        public void Fetch(int idx, string? type)
        {
            string[] _c = LoadCountryNames();
            string[] _ct = LoadCityNames(RegisterWeatherWidgetSettings.saveData.countryIndex);
            Task.Run(async () =>
            {
                using var httpClient = new HttpClient();
                string response = string.Empty;
                var lat = string.Empty; var lon = string.Empty;
                NewLocation location = default;

                if (_c[idx] == "Default" && type == "default")
                {
                    response = await httpClient.GetStringAsync("https://ipinfo.io/geo");
                    location = JsonConvert.DeserializeObject<NewLocation>(response);

                    lat = location.loc.Split(',')[0];
                    lon = location.loc.Split(',')[1];
                }
                else
                {
                    var loc = LoadLatLong(RegisterWeatherWidgetSettings.saveData.countryIndex);
                    lat = loc.Split(',')[0];
                    lon = loc.Split(',')[1];

                    location = _ct[idx] == "Default" ? new NewLocation { city = _c[idx], region = _c[idx], loc = loc } : new NewLocation { city = _ct[idx], region = _c[idx], loc = loc };

                    Debug.WriteLine(loc);
                }

                string _t = null;
                string _w = null;

                XmlTextReader reader = null;
                try
                {
                    string _u = String.Format("https://tile-service.weather.microsoft.com/livetile/front/{0},{1}", lat, lon);

                    int _n = 0;

                    reader = new XmlTextReader(_u);
                    reader.WhitespaceHandling = WhitespaceHandling.None;

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            if (_n == 1) _t = reader.Value;
                            else if (_n == 2) _w = reader.Value;
                            _n++;
                        }
                    }
                }

                finally
                {
                    if (reader != null) reader.Close();
                }

                string _fahr = _t.Replace("°", "");
                double _celc = (Double.Parse(_fahr) - 32.0) * (double)5 / 9;
                string _celcText = _celc.ToString("#.#");

                System.Diagnostics.Debug.WriteLine(String.Format("{0}, {1}F({2}°C), {3}", location.city, _t, _celcText, _w));

                _WeatherData = new NewWeatherData() { city = location.city, region = location.region, celsius = _celcText + "°C", fahrenheit = _fahr + "F", weatherText = _w };
                _OnWeatherDataReceived?.Invoke(_WeatherData);

                Thread.Sleep(120000);

                Fetch(idx, type);
            });
        }

        public class Country
        {
            public string country { get; set; }
            public string city { get; set; }
            public double lat { get; set; }
            public double lng { get; set; }
        }

        static List<Country> LoadCsv()
        {
            Country _defaultVal = new Country { country = "Default" };
            var reader = new StreamReader(Res.WeatherLocations);
            var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
            List<Country> records = csv.GetRecords<Country>().OrderBy(c => c.country).ToList();

            records.Insert(0, _defaultVal);

            return records;
        }

        public static string[] LoadCountryNames()
        {
            var countries = LoadCsv();
            var countryNames = countries.Select(c => c.country).Distinct().ToArray();
            return countryNames;
        }

        public static string[] LoadCityNames(int idx)
        {
            var countries = LoadCsv();
            var countryNames = countries.Select(c => c.country).Distinct().ToArray();
            var cities = countries.Where(c => c.country == countryNames[idx]).Select(c => c.city).Distinct().Order().ToArray();
            RegisterWeatherWidgetSettings.saveData.totalCities = cities.Length - 1;
            return cities;
        }

        public static string LoadLatLong(int idx)
        {
            var countries = LoadCsv();
            var countryNames = countries.Select(c => c.country).Distinct().ToArray();
            var selectedCountry = countryNames[idx];
            var cities = countries
                .Where(c => c.country == selectedCountry)
                .OrderBy(c => c.city)
                .ToArray();

            if (RegisterWeatherWidgetSettings.saveData.cityIndex < 0 || RegisterWeatherWidgetSettings.saveData.cityIndex >= cities.Count())
                return string.Empty;

            var city = cities[RegisterWeatherWidgetSettings.saveData.cityIndex];
            return $"{city.lat},{city.lng}";
        }
    }

    struct NewLocation
    {
        public string city;
        public string region;
        public string country;
        public string loc;
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
