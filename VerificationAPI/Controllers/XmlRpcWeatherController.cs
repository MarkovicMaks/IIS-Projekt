using Microsoft.AspNetCore.Mvc;
using System.Text;
using VerificationAPI.Models;
using VerificationAPI.Services;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class XmlRpcWeatherController : Controller
    {
        private readonly ILogger<XmlRpcWeatherController> _logger;

        public XmlRpcWeatherController(ILogger<XmlRpcWeatherController> logger)
        {
            _logger = logger;
        }

        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherReport>))]
        public async Task<ActionResult<List<WeatherReport>>> GetAllWeather()
        {
            return await CallXmlRpcMethod("");
        }

        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherReport>))]
        public async Task<ActionResult<List<WeatherReport>>> SearchWeather([FromBody] WeatherSearchRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CityName))
            {
                return BadRequest("City name is required");
            }

            return await CallXmlRpcMethod(request.CityName.Trim());
        }

        private async Task<ActionResult<List<WeatherReport>>> CallXmlRpcMethod(string cityName)
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var xmlRpcService = scope.ServiceProvider.GetRequiredService<WeatherXmlRpcService>();

                var weatherReports = await xmlRpcService.GetWeatherByCityAsync(cityName);

                _logger.LogInformation("Direct call returned {Count} weather reports for city: {CityName}", weatherReports.Count, cityName);
                return Ok(weatherReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling weather service for city: {CityName}", cityName);
                return StatusCode(500, "Error retrieving weather data");
            }
        }

        private static string BuildXmlRpcRequest(string cityName)
        {
            return $@"<?xml version=""1.0""?>
<methodCall>
    <methodName>weather.getTemperatureByCity</methodName>
    <params>
        <param>
            <value><string>{System.Security.SecurityElement.Escape(cityName)}</string></value>
        </param>
    </params>
</methodCall>";
        }

        private List<WeatherReport> ParseXmlRpcResponse(string xmlResponse)
        {
            var weatherReports = new List<WeatherReport>();

            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlResponse);

                // Check for fault first
                var faultNode = xmlDoc.SelectSingleNode("//fault");
                if (faultNode != null)
                {
                    var faultString = xmlDoc.SelectSingleNode("//fault//member[n='faultString']/value/string")?.InnerText;
                    _logger.LogWarning("XML-RPC fault returned: {FaultString}", faultString);
                    return weatherReports;
                }

                // Parse successful response
                var structNodes = xmlDoc.SelectNodes("//array/data/value/struct");
                if (structNodes == null) return weatherReports;

                foreach (System.Xml.XmlNode structNode in structNodes)
                {
                    var report = new WeatherReport
                    {
                        CityName = GetMemberValue(structNode, "cityName"),
                        Temperature = GetMemberValue(structNode, "temperature"),
                        MaxTemperature = GetMemberValue(structNode, "maxTemperature"),
                        MinTemperature = GetMemberValue(structNode, "minTemperature"),
                        Humidity = GetMemberValue(structNode, "humidity"),
                        Pressure = GetMemberValue(structNode, "pressure"),
                        WindDirection = GetMemberValue(structNode, "windDirection"),
                        WindSpeed = GetMemberValue(structNode, "windSpeed"),
                        Weather = GetMemberValue(structNode, "weather")
                    };

                    weatherReports.Add(report);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing XML-RPC response");
            }

            return weatherReports;
        }

        private string GetMemberValue(System.Xml.XmlNode structNode, string memberName)
        {
            var memberNode = structNode.SelectSingleNode($"member[n='{memberName}']/value/string");
            return memberNode?.InnerText ?? "N/A";
        }
    }

    public class WeatherSearchRequest
    {
        public string CityName { get; set; } = string.Empty;
    }
}