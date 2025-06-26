using System.Text.Json;
using System.Xml.Serialization;
using VerificationAPI.DTO;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Services
{
    public class XmlConverter
    {
        private readonly XmlSerializer _xmlSer = new(typeof(Video));

        public async Task<string> ConvertJsonToXmlAsync(Stream jsonStream)
        {
            var dto = await JsonSerializer.DeserializeAsync<APIResponse>(jsonStream)
                      ?? throw new InvalidDataException("Invalid JSON");

            var video = dto.ToXmlModel();

            using var sw = new StringWriter();
            _xmlSer.Serialize(sw, video);          // UTF-16; change if you need UTF-8
            return sw.ToString();
        }
    }
}
