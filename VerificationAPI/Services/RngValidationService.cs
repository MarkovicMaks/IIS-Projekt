using System.Xml;
using Commons.Xml.Relaxng;

namespace VerificationAPI.Services          
{
    public class RngValidationService
    {
        private readonly RelaxngPattern _pattern;

        public RngValidationService(string rngPath)
        {
            using var r = XmlReader.Create(rngPath);
            _pattern = RelaxngPattern.Read(r);
        }

        public (bool IsValid, List<string> Errors) Validate(string xml)
        {
            var errors = new List<string>();

            using var xmlReader = XmlReader.Create(new StringReader(xml));
            using var rngReader = new RelaxngValidatingReader(xmlReader, _pattern);

            rngReader.InvalidNodeFound += (src, msg) =>
            {
                errors.Add($"RNG: {msg}");
                return true;                  
            };

            while (rngReader.Read()) { }        
            return (errors.Count == 0, errors);
        }
    }
}
