using Microsoft.AspNetCore.Mvc;
using System.Xml.Serialization;
using VerificationAPI.Services;
using VerificationAPI.XmlModels;   

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]          
    public class XmlValidationController : Controller
    {
        private readonly XmlValidationService _validator;
        private readonly List<VideoMetadata> _store;   
        private readonly ILogger<XmlValidationController> _log;

        public XmlValidationController(
            XmlValidationService validator,
            List<VideoMetadata> store,               
            ILogger<XmlValidationController> log)
        {
            _validator = validator;
            _store = store;
            _log = log;
        }

        [HttpGet("all")]
        public ActionResult<List<VideoMetadata>> GetAll()
        {
            return Ok(_store);
        }

        [ApiController]
        [Route("debug/[controller]")]
        public class DebugController : Controller
        {
            [HttpGet("videos")]
            public IActionResult GetXmlFile()
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "videos.xml");
                if (!System.IO.File.Exists(path))
                    return NotFound("videos.xml not found");
                var xml = System.IO.File.ReadAllText(path);
                return Content(xml, "application/xml");
            }
        }

        [HttpPost("import")]
        [Consumes("application/xml")]                          
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(List<string>))]
        public IActionResult Import([FromBody] string xml)     
        {
            var (isValid, errs) = _validator.Validate(xml);
            if (!isValid) return BadRequest(errs);

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

            _store.Add(item);

            return Ok("XML validated and video metadata stored.");
        }

    }
}
