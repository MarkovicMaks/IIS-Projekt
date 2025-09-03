using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Serialization;
using VerificationAPI.Data;
using VerificationAPI.Services;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RngValidationController : Controller
    {
        private readonly RngValidationService _rngValidator;
        private readonly VerificationDbContext _context; 
        private readonly ILogger<RngValidationController> _log;

        public RngValidationController(
            RngValidationService rngValidator,
            VerificationDbContext context, 
            ILogger<RngValidationController> log)
        {
            _rngValidator = rngValidator;
            _context = context;
            _log = log;
        }

        [HttpPost("import")]
        [Consumes("application/xml")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(List<string>))]
        public async Task<IActionResult> ImportAsync([FromBody] string xml)
        {
            var (ok, errs) = _rngValidator.Validate(xml);
            if (!ok) return BadRequest(errs);

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

            var existingItem = await _context.VideoMetadata.FindAsync(item.Id);
            if (existingItem != null)
            {
                _log.LogInformation("VideoMetadata with ID {Id} already exists", item.Id);
                return Ok("XML validated by RNG - item already exists in database");
            }

            _context.VideoMetadata.Add(item);
            await _context.SaveChangesAsync();

            _log.LogInformation("VideoMetadata with ID {Id} saved to database", item.Id);
            return Ok("XML validated by RNG and stored.");
        }
    }
}