using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace VerificationAPI.Services
{
    public class XmlValidationService
    {
        private readonly XmlSchemaSet _schemas;

        public XmlValidationService(string xsdPath)
        {
            _schemas = new XmlSchemaSet();
            _schemas.Add("", xsdPath);
            _schemas.Compile();
        }

        public (bool IsValid, List<string> Errors) Validate(string xml)
        {
            var errors = new List<string>();

            try
            {
                var doc = XDocument.Parse(
                    xml,
                    LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

                doc.Validate(_schemas,
                    (o, e) =>
                    {
                        var li = e.Exception as IXmlLineInfo;
                        var line = li?.LineNumber ?? 0;
                        var col = li?.LinePosition ?? 0;
                        errors.Add($"{e.Severity}: {e.Message}");
                    },
                    addSchemaInfo: true);
            }
            catch (XmlException ex)
            {
                errors.Add($"Well-formedness error: {ex.Message}");
            }

            return (errors.Count == 0, errors);
        }
    }
}
