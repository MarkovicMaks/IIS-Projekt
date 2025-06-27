using Microsoft.AspNetCore.Mvc;

namespace VerificationAPI.Controllers
{
    public class DeBugger : Controller
    {

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

    }
}
