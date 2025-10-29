using System.Text.Json;
using System.Text.Json.Serialization;

namespace Townsquare.Services
{
    public interface IWeatherService
    {
        Task<WeatherInfo?> GetWeatherAsync(string location, DateTime eventDate);
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

        public async Task<WeatherInfo?> GetWeatherAsync(string location, DateTime eventDate)
        {
            try
            {
                var (latitude, longitude) = GetCoordinatesForLocation(location);

                // Check if event is more than 16 days in the future (API limit)
                var daysUntilEvent = (eventDate.Date - DateTime.UtcNow.Date).Days;

                string url;

                if (daysUntilEvent < 0)
                {
                    // Event is in the past
                    _logger.LogWarning("Event date {EventDate} is in the past", eventDate);
                    return null;
                }
                else if (daysUntilEvent > 16)
                {
                    // Event is too far in the future for forecast
                    _logger.LogWarning("Event date {EventDate} is more than 16 days in the future", eventDate);
                    return null;
                }
                else if (daysUntilEvent == 0)
                {
                    // Event is today - use current weather
                    url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code&timezone=auto";
                }
                else
                {
                    // Event is in the future - use daily forecast
                    var dateStr = eventDate.ToString("yyyy-MM-dd");
                    url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code,wind_speed_10m_max&timezone=auto&start_date={dateStr}&end_date={dateStr}";
                }

                _logger.LogInformation("Calling weather API: {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Weather API error: Status={Status}, Content={Content}", response.StatusCode, errorContent);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Weather API Response: {Response}", jsonContent);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                if (daysUntilEvent == 0)
                {
                    // Parse current weather
                    var weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(jsonContent, options);

                    if (weatherData?.Current == null)
                    {
                        _logger.LogWarning("Weather API returned null data");
                        return null;
                    }

                    var (description, icon) = GetWeatherInfo(weatherData.Current.WeatherCode);

                    return new WeatherInfo
                    {
                        Temperature = weatherData.Current.Temperature2m,
                        Humidity = weatherData.Current.RelativeHumidity2m,
                        WindSpeed = weatherData.Current.WindSpeed10m,
                        Description = description,
                        Icon = icon
                    };
                }
                else
                {
                    // Parse daily forecast
                    var forecastData = JsonSerializer.Deserialize<OpenMeteoForecastResponse>(jsonContent, options);

                    if (forecastData?.Daily == null || forecastData.Daily.Temperature2mMax == null || !forecastData.Daily.Temperature2mMax.Any())
                    {
                        _logger.LogWarning("Weather API returned null forecast data");
                        return null;
                    }

                    var weatherCode = forecastData.Daily.WeatherCode?[0] ?? 0;
                    var (description, icon) = GetWeatherInfo(weatherCode);

                    // Use average of max and min temperature
                    var avgTemp = (forecastData.Daily.Temperature2mMax[0] + forecastData.Daily.Temperature2mMin[0]) / 2;

                    return new WeatherInfo
                    {
                        Temperature = avgTemp,
                        Humidity = 0, // Not available in daily forecast
                        WindSpeed = forecastData.Daily.WindSpeed10mMax?[0] ?? 0,
                        Description = description,
                        Icon = icon
                    };
                }
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
                0 => ("Clear sky", "☀️"),
                1 => ("Mainly clear", "🌤️"),
                2 => ("Partly cloudy", "⛅"),
                3 => ("Overcast", "☁️"),
                45 => ("Foggy", "🌫️"),
                48 => ("Depositing rime fog", "🌫️"),
                51 => ("Light drizzle", "🌦️"),
                53 => ("Moderate drizzle", "🌦️"),
                55 => ("Dense drizzle", "🌦️"),
                61 => ("Slight rain", "🌧️"),
                63 => ("Moderate rain", "🌧️"),
                65 => ("Heavy rain", "🌧️"),
                71 => ("Slight snow fall", "🌨️"),
                73 => ("Moderate snow fall", "🌨️"),
                75 => ("Heavy snow fall", "🌨️"),
                77 => ("Snow grains", "🌨️"),
                80 => ("Slight rain showers", "🌦️"),
                81 => ("Moderate rain showers", "🌦️"),
                82 => ("Violent rain showers", "🌦️"),
                85 => ("Slight snow showers", "🌨️"),
                86 => ("Heavy snow showers", "🌨️"),
                95 => ("Thunderstorm", "⛈️"),
                96 => ("Thunderstorm with slight hail", "⛈️"),
                99 => ("Thunderstorm with heavy hail", "⛈️"),
                _ => ("Unknown", "❓")
            };
        }

        private (double latitude, double longitude) GetCoordinatesForLocation(string location)
        {
            var locationLower = location.ToLower();

            if (locationLower.Contains("stockholm")) return (59.3293, 18.0686);
            if (locationLower.Contains("göteborg") || locationLower.Contains("gothenburg")) return (57.7089, 11.9746);
            if (locationLower.Contains("malmö") || locationLower.Contains("malmo")) return (55.6050, 13.0038);
            if (locationLower.Contains("uppsala")) return (59.8586, 17.6389);
            if (locationLower.Contains("linköping") || locationLower.Contains("linkoping")) return (58.4108, 15.6214);
            if (locationLower.Contains("örebro") || locationLower.Contains("orebro")) return (59.2741, 15.2066);
            if (locationLower.Contains("västerås") || locationLower.Contains("vasteras")) return (59.6162, 16.5528);
            if (locationLower.Contains("helsingborg")) return (56.0465, 12.6945);
            if (locationLower.Contains("jönköping") || locationLower.Contains("jonkoping")) return (57.7826, 14.1618);
            if (locationLower.Contains("norrköping") || locationLower.Contains("norrkoping")) return (58.5877, 16.1924);
            if (locationLower.Contains("lund")) return (55.7047, 13.1910);
            if (locationLower.Contains("umeå") || locationLower.Contains("umea")) return (63.8258, 20.2630);
            if (locationLower.Contains("gävle") || locationLower.Contains("gavle")) return (60.6745, 17.1417);
            if (locationLower.Contains("borås") || locationLower.Contains("boras")) return (57.7210, 12.9401);

            // Default to Borås
            _logger.LogWarning("Location '{Location}' not found, using default coordinates (Borås)", location);
            return (57.7210, 12.9401);
        }
    }

    // API Response Models
    public class OpenMeteoResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "";

        [JsonPropertyName("current")]
        public CurrentWeather Current { get; set; } = new();
    }

    public class CurrentWeather
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = "";

        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public double RelativeHumidity2m { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed10m { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }

    // For daily forecasts
    public class OpenMeteoForecastResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("daily")]
        public DailyWeather Daily { get; set; } = new();
    }

    public class DailyWeather
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = new();

        [JsonPropertyName("temperature_2m_max")]
        public List<double> Temperature2mMax { get; set; } = new();

        [JsonPropertyName("temperature_2m_min")]
        public List<double> Temperature2mMin { get; set; } = new();

        [JsonPropertyName("wind_speed_10m_max")]
        public List<double> WindSpeed10mMax { get; set; } = new();

        [JsonPropertyName("weather_code")]
        public List<int> WeatherCode { get; set; } = new();

        [JsonPropertyName("precipitation_sum")]
        public List<double> PrecipitationSum { get; set; } = new();
    }
}