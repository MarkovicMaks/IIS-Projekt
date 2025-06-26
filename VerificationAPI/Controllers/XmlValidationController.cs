using Microsoft.AspNetCore.Mvc;
using System.Xml.Serialization;
using VerificationAPI.Services;
using VerificationAPI.XmlModels;   // ⬅️ brings in VideoMetadata & Media

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]          // ->  /api/xmlvalidation/import
    public class XmlValidationController : Controller
    {
        private readonly XmlValidationService _validator;
        private readonly List<VideoMetadata> _store;   // in-memory list
        private readonly ILogger<XmlValidationController> _log;

        public XmlValidationController(
            XmlValidationService validator,
            List<VideoMetadata> store,                 // injected in Program.cs
            ILogger<XmlValidationController> log)
        {
            _validator = validator;
            _store = store;
            _log = log;
        }

        // POST /api/xmlvalidation/import
        [HttpPost("import")]
        [Consumes("application/xml")]                           // Swagger shows xml
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(List<string>))]
        public IActionResult Import([FromBody] string xml)      // ← back again
        {
            // 1️⃣  XSD validation
            var (isValid, errs) = _validator.Validate(xml);
            if (!isValid) return BadRequest(errs);

            // 2️⃣  Deserialize → VideoMetadata
            VideoMetadata item;
            try
            {
                var xs = new XmlSerializer(typeof(VideoMetadata));
                using var sr = new StringReader(xml);
                item = (VideoMetadata)xs.Deserialize(sr)!;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Deserialization failed");
                return BadRequest(new List<string> { $"Deserialization error: {ex.Message}" });
            }

            // 3️⃣  Store in-memory
            _store.Add(item);

            return Ok("XML validated and video metadata stored.");
        }

    }
}
