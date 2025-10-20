using System.Text.Json;

namespace Townsquare.Services
{
    public interface IWeatherService
    {
        Task<WeatherInfo?> GetWeatherAsync(string location);
    }

    public class WeatherInfo
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<WeatherInfo?> GetWeatherAsync(string location)
        {
            try
            {
                // Få koordinater för platsen
                var (latitude, longitude) = GetCoordinatesForLocation(location);
                
                // Skapa API URL för Open-Meteo
                var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude:F2}&longitude={longitude:F2}&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code&timezone=Europe%2FStockholm";

                _logger.LogInformation("Calling weather API: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (weatherData?.Current == null)
                {
                    _logger.LogWarning("Weather API returned null data");
                    return null;
                }

                // Konvertera väderkod till beskrivning och ikon
                var (description, icon) = GetWeatherInfo(weatherData.Current.Weather_code);

                return new WeatherInfo
                {
                    Temperature = weatherData.Current.Temperature_2m,
                    Humidity = weatherData.Current.Relative_humidity_2m,
                    WindSpeed = weatherData.Current.Wind_speed_10m,
                    Description = description,
                    Icon = icon
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weather for location: {Location}", location);
                return null;
            }
        }

        private (string description, string icon) GetWeatherInfo(int weatherCode)
        {
            return weatherCode switch
            {
                0 => ("Klart väder", "☀️"),
                1 => ("Huvudsakligen klart", "🌤️"),
                2 => ("Delvis molnigt", "⛅"),
                3 => ("Mulet", "☁️"),
                45 => ("Dimma", "🌫️"),
                48 => ("Rimedimma", "🌫️"),
                51 => ("Lätt duggregn", "🌦️"),
                53 => ("Måttligt duggregn", "🌦️"),
                55 => ("Tätt duggregn", "🌦️"),
                61 => ("Lätt regn", "🌧️"),
                63 => ("Måttligt regn", "🌧️"),
                65 => ("Kraftigt regn", "🌧️"),
                71 => ("Lätt snöfall", "🌨️"),
                73 => ("Måttligt snöfall", "🌨️"),
                75 => ("Kraftigt snöfall", "🌨️"),
                77 => ("Snökorn", "🌨️"),
                80 => ("Lätta regnskurar", "🌦️"),
                81 => ("Måttliga regnskurar", "🌦️"),
                82 => ("Kraftiga regnskurar", "🌦️"),
                85 => ("Lätta snöskurar", "🌨️"),
                86 => ("Kraftiga snöskurar", "🌨️"),
                95 => ("Åska", "⛈️"),
                96 => ("Åska med lätt hagel", "⛈️"),
                99 => ("Åska med kraftigt hagel", "⛈️"),
                _ => ("Okänt väder", "❓")
            };
        }

        private (double latitude, double longitude) GetCoordinatesForLocation(string location)
        {
            var locationLower = location.ToLower();
            
            return locationLower switch
            {
                var loc when loc.Contains("stockholm") => (59.3293, 18.0686),
                var loc when loc.Contains("göteborg") || loc.Contains("gothenburg") => (57.7089, 11.9746),
                var loc when loc.Contains("malmö") || loc.Contains("malmo") => (55.6050, 13.0038),
                var loc when loc.Contains("uppsala") => (59.8586, 17.6389),
                var loc when loc.Contains("linköping") || loc.Contains("linkoping") => (58.4108, 15.6214),
                var loc when loc.Contains("örebro") || loc.Contains("orebro") => (59.2741, 15.2066),
                var loc when loc.Contains("västerås") || loc.Contains("vasteras") => (59.6162, 16.5528),
                var loc when loc.Contains("helsingborg") => (56.0465, 12.6945),
                var loc when loc.Contains("jönköping") || loc.Contains("jonkoping") => (57.7826, 14.1618),
                var loc when loc.Contains("norrköping") || loc.Contains("norrkoping") => (58.5877, 16.1924),
                var loc when loc.Contains("lund") => (55.7047, 13.1910),
                var loc when loc.Contains("umeå") || loc.Contains("umea") => (63.8258, 20.2630),
                var loc when loc.Contains("gävle") || loc.Contains("gavle") => (60.6745, 17.1417),
                var loc when loc.Contains("borås") || loc.Contains("boras") => (57.7210, 12.9401),
                _ => (57.7210, 12.9401) // Default till Borås
            };
        }
    }

    // API Response Models
    public class OpenMeteoResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Timezone { get; set; } = "";
        public CurrentWeather Current { get; set; } = new();
    }

    public class CurrentWeather
    {
        public string Time { get; set; } = "";
        public double Temperature_2m { get; set; }
        public double Relative_humidity_2m { get; set; }
        public double Wind_speed_10m { get; set; }
        public int Weather_code { get; set; }
    }
}
