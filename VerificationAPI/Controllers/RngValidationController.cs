using Microsoft.AspNetCore.Mvc;
using System.Xml.Serialization;
using VerificationAPI.Services;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    
    public class RngValidationController : Controller
    {
        private readonly RngValidationService _rngValidator;
        private readonly List<VideoMetadata> _store;
        private readonly ILogger<RngValidationController> _log;

        public RngValidationController(
            RngValidationService rngValidator,
            List<VideoMetadata> store,
            ILogger<RngValidationController> log)
        {
            _rngValidator = rngValidator;
            _store = store;
            _log = log;
        }

        [HttpPost("import")]
        [Consumes("application/xml")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(List<string>))]
        public IActionResult Import([FromBody] string xml)
        {
            // 1️⃣  RNG validacija
            var (ok, errs) = _rngValidator.Validate(xml);
            if (!ok) return BadRequest(errs);

            // 2️⃣  Deserijalizacija → VideoMetadata
            VideoMetadata item;
            try
            {
                var xs = new XmlSerializer(typeof(VideoMetadata));
                using var sr = new StringReader(xml);
                item = (VideoMetadata)xs.Deserialize(sr)!;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Deserialization failed.");
                return BadRequest(new List<string> { $"Deserialization error: {ex.Message}" });
            }

            // 3️⃣  Spremi u memoriju (ista lista kao XSD ruta)
            _store.Add(item);

            return Ok("XML validated by RNG and stored.");
        }
    }
}
