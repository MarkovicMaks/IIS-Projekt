using System.Xml.Schema;
using System.Xml;

namespace VerificationAPI.Services
{
    public class XmlValidationService
    {
        private readonly XmlSchemaSet _schemas;
        public XmlValidationService(string xsdPath)
        {
            _schemas = new XmlSchemaSet();
            _schemas.Add("urn:tiktok", xsdPath);
            _schemas.Compile();
        }

        public (bool IsValid, List<string> Errors) Validate(string xml)
        {
            var errors = new List<string>();

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemas
            };
            settings.ValidationEventHandler += (_, e) =>
                errors.Add($"{e.Severity}: {e.Message}");

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read()) { }    // iterate to trigger validation

            return (errors.Count == 0, errors);
        }
    }
}
