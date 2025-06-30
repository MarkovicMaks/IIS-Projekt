using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class MetadataController : ControllerBase
    {
        // in-memory store of VideoMetadata items
        private static readonly List<VideoMetadata> _store = new();

        // GET  /api/metadata
        [HttpGet]
        public ActionResult<List<VideoMetadata>> GetAll() =>
            Ok(_store);

        // GET  /api/metadata/{id}
        [HttpGet("{id}")]
        public ActionResult<VideoMetadata> GetById(long id)
        {
            var item = _store.FirstOrDefault(v => v.Id == id);
            return item is null ? NotFound() : Ok(item);
        }

        // POST /api/metadata
        [HttpPost]
        public ActionResult Create([FromBody] VideoMetadata vm)
        {
            if (_store.Any(v => v.Id == vm.Id))
                return Conflict($"Item with ID {vm.Id} already exists.");
            _store.Add(vm);
            return CreatedAtAction(nameof(GetById), new { id = vm.Id }, vm);
        }

        // PUT  /api/metadata/{id}
        [HttpPut("{id}")]
        public ActionResult Update(long id, [FromBody] VideoMetadata vm)
        {
            var idx = _store.FindIndex(v => v.Id == id);
            if (idx < 0) return NotFound();
            _store[idx] = vm;
            return NoContent();
        }

        // DELETE /api/metadata/{id}
        [HttpDelete("{id}")]
        public ActionResult Delete(long id)
        {
            var idx = _store.FindIndex(v => v.Id == id);
            if (idx < 0) return NotFound();
            _store.RemoveAt(idx);
            return NoContent();
        }
    }
}
