using Microsoft.AspNetCore.Mvc;
using VerificationAPI.Models;
using VerificationAPI.Services;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : Controller
    {
        private readonly WeatherService _weatherService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(WeatherService weatherService, ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        [HttpGet("city/{cityName}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherReport>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<WeatherReport>>> GetWeatherByCity(string cityName)
        {
            try
            {
                var weatherReports = await _weatherService.GetWeatherByCityAsync(cityName);

                if (!weatherReports.Any())
                {
                    return NotFound($"No weather data found for city: {cityName}");
                }

                return Ok(weatherReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weather for city: {CityName}", cityName);
                return StatusCode(500, "Error retrieving weather data");
            }
        }

        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherReport>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<WeatherReport>>> GetAllWeather()
        {
            try
            {

                var weatherReports = await _weatherService.GetAllWeatherAsync();
                return Ok(weatherReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all weather data");
                return StatusCode(500, "Error retrieving weather data");
            }
        }

        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WeatherReport>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<WeatherReport>>> SearchWeather([FromBody] WeatherSearchRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CityName))
            {
                return BadRequest("City name is required");
            }

            try
            {
                var weatherReports = await _weatherService.GetWeatherByCityAsync(request.CityName.Trim());
                return Ok(weatherReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching weather for city: {CityName}", request.CityName);
                return StatusCode(500, "Error retrieving weather data");
            }
        }
    }

    public class WeatherSearchRequest
    {
        public string CityName { get; set; } = string.Empty;
    }
}