using Microsoft.AspNetCore.Mvc;
using VerificationAPI.DTO;
using VerificationAPI.Services;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class XmlValidationController : Controller
    {
        private readonly XmlConverter _converter;
        private readonly XmlValidationService _validator;
        private readonly ILogger<XmlValidationController> _log;

        public XmlValidationController(
            XmlConverter converter,
            XmlValidationService validator,
            ILogger<XmlValidationController> log)
        {
            _converter = converter;
            _validator = validator;
            _log = log;
        }

        // POST /api/video/import
        [HttpPost("import")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(List<string>))]
        public async Task<IActionResult> ImportAsync([FromBody] APIResponse dto)
        {
            // DTO → XML string
            var xml = SerializeToXml(dto.ToXmlModel());

            // XSD validation
            var (ok, errs) = _validator.Validate(xml);
            if (!ok) return BadRequest(errs);

            // TODO: persist ‘dto’ or XML to DB/file/storage here

            _log.LogInformation("Video {Id} imported", dto.Id);
            return Ok("Saved");
        }

        private static string SerializeToXml(object obj)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            using var sw = new StringWriter();
            ser.Serialize(sw, obj);
            return sw.ToString();
        }
    }
}
