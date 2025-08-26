using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using VerificationAPI.Models;

namespace VerificationAPI.Services

{
    public class WeatherService
    {
        private const string DHMZ_URL = "https://vrijeme.hr/hrvatska_n.xml";
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<List<WeatherReport>> GetAllWeatherAsync()
            => GetWeatherByCityAsync(string.Empty);

        public async Task<List<WeatherReport>> GetWeatherByCityAsync(string cityName)
        {
            var weatherReports = new List<WeatherReport>();

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, DHMZ_URL);
                var resp = await _httpClient.SendAsync(req);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("DHMZ HTTP {Status} za {Url}", (int)resp.StatusCode, DHMZ_URL);
                    return weatherReports;
                }

                var xmlText = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(xmlText))
                {
                    _logger.LogWarning("DHMZ vratio prazan sadržaj");
                    return weatherReports;
                }

                var xmlDoc = new XmlDocument
                {
                    XmlResolver = null
                };
                xmlDoc.LoadXml(xmlText);

                // FIX: čitamo <Hrvatska>/<Grad>, a ne nepostojeći <Report>
                var gradNodes = xmlDoc.SelectNodes("//Hrvatska/Grad");
                if (gradNodes == null || gradNodes.Count == 0)
                {
                    _logger.LogInformation("Nema <Grad> čvorova u DHMZ XML-u.");
                    return weatherReports;
                }

                foreach (XmlNode gradNode in gradNodes)
                {
                    var name = gradNode.SelectSingleNode("GradIme")?.InnerText?.Trim() ?? string.Empty;

                    // filter po nazivu grada (case-insensitive, partial match)
                    if (!string.IsNullOrEmpty(cityName) &&
                        !name.Contains(cityName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var dataNode = gradNode.SelectSingleNode("Podatci");

                    string Get(string x) => dataNode?.SelectSingleNode(x)?.InnerText?.Trim() ?? "N/A";

                    var report = new WeatherReport
                    {
                        CityName = string.IsNullOrWhiteSpace(name) ? "N/A" : name,
                        Temperature = Get("Temp"),
                        // DHMZ endpoint nema max/min temperature – ostavljamo "N/A"
                        MaxTemperature = "N/A",
                        MinTemperature = "N/A",
                        Humidity = Get("Vlaga"),
                        Pressure = Get("Tlak"),
                        WindDirection = Get("VjetarSmjer"),
                        WindSpeed = Get("VjetarBrzina"),
                        Weather = Get("Vrijeme")
                    };

                    weatherReports.Add(report);
                }

                _logger.LogInformation("Našao {Count} unosa (filter='{CityFilter}')", weatherReports.Count, cityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri dohvaćanju ili parsiranju DHMZ XML-a");
            }

            return weatherReports;
        }
    }
}
