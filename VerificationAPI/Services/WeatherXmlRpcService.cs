using System.Xml;
using VerificationAPI.Models;

namespace VerificationAPI.Services
{
    public class WeatherXmlRpcService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherXmlRpcService> _logger;
        private const string DHMZ_URL = "https://vrijeme.hr/hrvatska_n.xml";

        public WeatherXmlRpcService(HttpClient httpClient, ILogger<WeatherXmlRpcService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task ProcessXmlRpcRequest(HttpContext context)
        {
            try
            {
                // Read the XML-RPC request
                using var reader = new StreamReader(context.Request.Body);
                var requestXml = await reader.ReadToEndAsync();

                _logger.LogInformation("XML-RPC Request received: {Request}", requestXml);

                // Parse the XML-RPC request
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(requestXml);

                var methodNameNode = xmlDoc.SelectSingleNode("//methodName");
                if (methodNameNode?.InnerText != "weather.getTemperatureByCity")
                {
                    await SendXmlRpcFault(context, -32601, "Method not found");
                    return;
                }

                // Extract city name parameter
                var paramNode = xmlDoc.SelectSingleNode("//params/param/value/string");
                var cityName = paramNode?.InnerText ?? "";

                _logger.LogInformation("XML-RPC method call: weather.getTemperatureByCity with parameter: {CityName}", cityName);

                // Get weather data
                var weatherData = await GetWeatherByCityAsync(cityName);

                // Send XML-RPC response
                await SendXmlRpcResponse(context, weatherData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing XML-RPC request");
                await SendXmlRpcFault(context, -32603, "Internal error");
            }
        }

        public async Task<List<WeatherReport>> GetWeatherByCityAsync(string cityName)
        {
            var weatherReports = new List<WeatherReport>();

            try
            {
                var xmlContent = await _httpClient.GetStringAsync(DHMZ_URL);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                var reportNodes = xmlDoc.SelectNodes("//Grad");
                if (reportNodes == null) return weatherReports;

                foreach (XmlNode reportNode in reportNodes)
                {
                    var currentCityName = reportNode.SelectSingleNode("GradIme")?.InnerText ?? "";

                    // Filter by city name (case-insensitive partial match)
                    if (string.IsNullOrEmpty(cityName) ||
                        currentCityName.Contains(cityName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Look inside Podatci element for the actual weather data
                        var podatciNode = reportNode.SelectSingleNode("Podatci");

                        var report = new WeatherReport
                        {
                            CityName = currentCityName,
                            Temperature = podatciNode?.SelectSingleNode("Temp")?.InnerText?.Trim() ?? "N/A",
                            MaxTemperature = "N/A", // Not available in this XML structure
                            MinTemperature = "N/A", // Not available in this XML structure  
                            Humidity = podatciNode?.SelectSingleNode("Vlaga")?.InnerText?.Trim() ?? "N/A",
                            Pressure = podatciNode?.SelectSingleNode("Tlak")?.InnerText?.Trim() ?? "N/A",
                            WindDirection = podatciNode?.SelectSingleNode("VjetarSmjer")?.InnerText?.Trim() ?? "N/A",
                            WindSpeed = podatciNode?.SelectSingleNode("VjetarBrzina")?.InnerText?.Trim() ?? "N/A",
                            Weather = podatciNode?.SelectSingleNode("Vrijeme")?.InnerText?.Trim() ?? "N/A"
                        };

                        weatherReports.Add(report);
                    }
                }

                _logger.LogInformation("Found {Count} weather reports for city: {CityName}", weatherReports.Count, cityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching DHMZ weather data");
            }

            return weatherReports;
        }

        private async Task SendXmlRpcResponse(HttpContext context, List<WeatherReport> weatherData)
        {
            var responseXml = BuildXmlRpcResponse(weatherData);

            context.Response.ContentType = "text/xml";
            await context.Response.WriteAsync(responseXml);

            _logger.LogInformation("XML-RPC Response sent with {Count} weather reports", weatherData.Count);
        }

        private async Task SendXmlRpcFault(HttpContext context, int faultCode, string faultString)
        {
            var faultXml = $@"<?xml version=""1.0""?>
<methodResponse>
    <fault>
        <value>
            <struct>
                <member>
                    <name>faultCode</name>
                    <value><int>{faultCode}</int></value>
                </member>
                <member>
                    <name>faultString</name>
                    <value><string>{System.Security.SecurityElement.Escape(faultString)}</string></value>
                </member>
            </struct>
        </value>
    </fault>
</methodResponse>";

            context.Response.ContentType = "text/xml";
            context.Response.StatusCode = 200; // XML-RPC always returns 200, faults are in the XML
            await context.Response.WriteAsync(faultXml);
        }

        private string BuildXmlRpcResponse(List<WeatherReport> weatherData)
        {
            var xml = @"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param>
            <value>
                <array>
                    <data>";

            foreach (var weather in weatherData)
            {
                xml += $@"
                        <value>
                            <struct>
                                <member>
                                    <name>cityName</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.CityName)}</string></value>
                                </member>
                                <member>
                                    <name>temperature</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.Temperature)}</string></value>
                                </member>
                                <member>
                                    <name>maxTemperature</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.MaxTemperature)}</string></value>
                                </member>
                                <member>
                                    <name>minTemperature</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.MinTemperature)}</string></value>
                                </member>
                                <member>
                                    <name>humidity</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.Humidity)}</string></value>
                                </member>
                                <member>
                                    <name>pressure</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.Pressure)}</string></value>
                                </member>
                                <member>
                                    <name>windDirection</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.WindDirection)}</string></value>
                                </member>
                                <member>
                                    <name>windSpeed</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.WindSpeed)}</string></value>
                                </member>
                                <member>
                                    <name>weather</name>
                                    <value><string>{System.Security.SecurityElement.Escape(weather.Weather)}</string></value>
                                </member>
                            </struct>
                        </value>";
            }

            xml += @"
                    </data>
                </array>
            </value>
        </param>
    </params>
</methodResponse>";

            return xml;
        }
    }
}