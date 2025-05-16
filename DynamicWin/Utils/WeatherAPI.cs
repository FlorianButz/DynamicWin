using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Xml;
using CsvHelper;
using DynamicWin.Resources;
using DynamicWin.UI.Widgets.Big;
using Newtonsoft.Json;

/*
*   Overview:
*    - Implement new Weather API that allows the user to change their location and display its weather.
*    - Allows user to easily access a list of countries and cities in a comma-separated value format.
*    - Handles both IP address geo-location and user-configured weather forecast.
*    
*   Author:                 Megan Park
*   GitHub:                 https://github.com/59xa
*   Implementation Date:    16 May 2024
*   Last Modified:          17 May 2024 08:36 KST (UTC+9)
*   
*   TO MAINTAINERS:
*    - When fetching weather data, the API might hallucinate, and retrieve forecast data from a different city.
*    - This only occurs as the task doesn't get killed gracefully when doing a hot reload.
*    - This behaviour will not occur when it's fully compiled for end-user.
*/

namespace DynamicWin.Utils
{
    // Initialise weather API class
    public class WeatherAPI
    {
        // Initialise WeatherData struct
        private WeatherData _WeatherData = new WeatherData();
        public WeatherData _Weather { get => _WeatherData; }

        public Action<WeatherData>? _OnWeatherDataReceived;

        /// <summary>
        /// Handles retrieval of forecast values from a specified city set by user.
        /// </summary>
        /// <param name="idx">The index that the function should use.</param>
        /// <param name="type">Optional parameter that defines whether index value is "default" or "city"</param>
        /// <returns>void</returns>
        public async Task Fetch(int idx, string? type, CancellationToken token = default)
        {   
            // Load required values
            // MAINTAINER: "i feel like this could be implemented in a better way without compromising optimisation"
            string[] _c = await LoadCountryNamesAsync();
            string[] _ct = await LoadCityNamesAsync(RegisterWeatherWidgetSettings.saveData.countryIndex);

            // Asynchronous task to handle HTTP protocol calls

            // TODO: Refactor this to use a single instance of HttpClient
            while (!token.IsCancellationRequested && RegisterWeatherWidgetSettings.saveData.isSettingsMenuOpen == false)
            {
                using var httpClient = new HttpClient();
                string response = string.Empty;
                var lat = string.Empty; var lon = string.Empty;
                Location location = default;

                // If index is Default, fetch geo-location forecast instead
                if (_c[idx] == "Default" && type == "default")
                {
                    Debug.WriteLine("WeatherAPI: DEFAULT", idx, type);
                    response = await httpClient.GetStringAsync("https://ipinfo.io/geo");
                    location = JsonConvert.DeserializeObject<Location>(response);

                    lat = location.loc.Split(',')[0];
                    lon = location.loc.Split(',')[1];
                }
                else // Read preference set by user, then return requested values
                {
                    Debug.WriteLine("WeatherAPI: USER-DEFINED", idx, type);
                    var loc = LoadLatLong(RegisterWeatherWidgetSettings.saveData.countryIndex, RegisterWeatherWidgetSettings.saveData.cityIndex);
                    lat = loc.Split(',')[0];
                    lon = loc.Split(',')[1];

                    location = _ct[idx] == "Default" ? new Location { city = _c[idx], region = _c[idx], loc = loc } : new Location { city = _ct[idx], region = _c[idx], loc = loc };
                }

                string _t = null;
                string _w = null;

                // Initialise XML reader
                XmlTextReader reader = null;
                try
                {
                    // Concatenate retrieved latitude and longitude values to the URI
                    string _u = String.Format("https://tile-service.weather.microsoft.com/livetile/front/{0},{1}", lat, lon);

                    int _n = 0;

                    reader = new XmlTextReader(_u);
                    reader.WhitespaceHandling = WhitespaceHandling.None; // Ensure no whitespaces when fetching data

                    // Read information fetched from the URI
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
                    if (reader != null) reader.Close(); // Ensure this is closed to prevent memory leaks
                }

                string _fahr = _t.Replace("°", "");
                double _celc = (Double.Parse(_fahr) - 32.0) * (double)5 / 9;
                string _celcText = _celc.ToString("#.#");

                Debug.WriteLine(String.Format("{0}, {1}F({2}°C), {3}", location.city, _t, _celcText, _w));

                _WeatherData = new WeatherData() { city = location.city, region = location.region, celsius = _celcText + "°C", fahrenheit = _fahr + "F", weatherText = _w };
                _OnWeatherDataReceived?.Invoke(_WeatherData);

                await Task.Delay(120000, token); // Wait for 2 minutes before re-fetching data

                Debug.WriteLine("WeatherAPI: IDX = {0}, TYPE = {1}", idx, type);
            }

            if (token.IsCancellationRequested || RegisterWeatherWidgetSettings.saveData.isSettingsMenuOpen)
            {
                Debug.WriteLine("WeatherAPI: TASK DISPOSAL RECEIVED");
                throw new OperationCanceledException(token);
            }
        }

        // Initialise Country constructor
        public class Country
        {
            public string country { get; set; }
            public string city { get; set; }
            public double lat { get; set; }
            public double lng { get; set; }
            public string population { get; set; }
        }

        // Logic to load provided comma-separated value file
        static async Task<List<Country>> LoadCsvAsync()
        {
            var defaultVal = new Country { country = "Default" };
            using var stream = new FileStream(Res.WeatherLocations, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);

            string csvText = await reader.ReadToEndAsync();

            using var stringReader = new StringReader(csvText);
            using var csv = new CsvReader(stringReader, System.Globalization.CultureInfo.InvariantCulture);

            List<Country> records = csv.GetRecords<Country>()
                .Where(r => !string.IsNullOrWhiteSpace(r.country)) // Safety check
                .OrderBy(r => r.country)
                .ToList();

            records.Insert(0, defaultVal); // Add "Default" at the start
            return records;
        }

        /// <summary>
        /// Retrieves a list of countries from the given comma-separated value file.
        /// </summary>
        /// <returns>A list of country names.</returns>
        public static async Task<string[]> LoadCountryNamesAsync()
        {
            var countries = await LoadCsvAsync();
            var countryNames = countries
                .Select(c => c.country)
                .Distinct()
                .ToArray(); // Ensure no duplicates when returning list data

            return countryNames;
        }

        /// <summary>
        /// Retrieves a list of cities from a country inside the comma-separated value file.
        /// </summary>
        /// <param name="idx">The index value of a specific country.</param>
        /// <returns>A list of city names for a specific country.</returns>
        public static async Task<string[]> LoadCityNamesAsync(int idx)
        {
            var countries = await LoadCsvAsync();
            var countryNames = countries.Select(c => c.country).Distinct().ToArray();

            var cities = countries
                .Where(c =>
                {
                    if (c.country != countryNames[idx])
                        return false;

                    // Handle empty or malformed population
                    if (string.IsNullOrWhiteSpace(c.population))
                        return false;

                    if (double.TryParse(c.population, out double pop))
                        return pop > 100000;

                    return false;
                })
                .Select(c => c.city)
                .Distinct()
                .Order()
                .ToArray();

            RegisterWeatherWidgetSettings.saveData.totalCities = cities.Length - 1;

            return cities;
        }

        /// <summary>
        /// Retrieves the latitude and longitude values of a city's location in an asynchronous manner.
        /// </summary>
        /// <param name="idx">The index value of a specific country.</param>
        /// <param name="idx2">The index value of a specific city.</param>
        /// <returns>A string that contains both the latitude and longitude value.</returns>
        public static async Task<string> LoadLatLongAsync(int idx, int idx2)
        {
            var countries = await LoadCsvAsync();
            var countryNames = countries.Select(c => c.country).Distinct().ToArray();
            var selectedCountry = countryNames[idx];

            var cities = countries
                .Where(c => c.country == selectedCountry)
                .OrderBy(c => c.city)
                .ToArray();

            if (RegisterWeatherWidgetSettings.saveData.cityIndex < 0 || RegisterWeatherWidgetSettings.saveData.cityIndex >= cities.Length)
                return string.Empty;

            var city = cities[idx2];
            return $"{city.lat},{city.lng}";
        }

        /// <summary>
        /// Retrieves the latitude and longitude values of a city's location in a synchronous manner.
        /// </summary>
        /// <param name="idx">The index value of a specific country.</param>
        /// <param name="idx2">The index value of a specific city.</param>
        /// <returns>A string that contains both the latitude and longitude value.</returns>
        public static string LoadLatLong(int idx, int idx2)
        {
            return LoadLatLongAsync(idx, idx2).GetAwaiter().GetResult();
        }
    }

    // Initialise Location structure
    struct Location
    {
        public string city;
        public string region;
        public string country;
        public string loc;
    }

    // Initialise WeatherData structure
    public struct WeatherData
    {
        public string city;
        public string region;
        public string weatherText;
        public string celsius;
        public string fahrenheit;
    }
}
