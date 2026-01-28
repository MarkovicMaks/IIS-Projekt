using Microsoft.EntityFrameworkCore;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using VerificationAPI.Data;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Services.SoapContracts
{
    public class SearchService : ISearchService
    {
        private readonly VerificationDbContext _context;
        private readonly ILogger<SearchService> _log;
        private const string XmlPath = "Data/videos.xml";

        public SearchService(VerificationDbContext context, ILogger<SearchService> log)
        {
            _context = context;
            _log = log;
        }

        public List<VideoMetadata> Search(string term)
        {
            var allData = _context.VideoMetadata.Include(v => v.Medias).ToList();

            if (allData.Count == 0)
            {
                return new List<VideoMetadata>();
            }

            GenerateXmlSnapshot(allData);


            var fileInfo = new FileInfo(XmlPath);
            if (fileInfo.Exists)
            {
                var content = File.ReadAllText(XmlPath);
                _log.LogInformation("DEBUG: First {Chars} chars = {Dump}",
                    Math.Min(500, content.Length),
                    content.Substring(0, Math.Min(500, content.Length)));
            }

            ValidateXmlWithJaxb();

            var doc = new XPathDocument(XmlPath);
            var nav = doc.CreateNavigator();

            var totalNodes = (int)(double)nav.Evaluate("count(//item)");
            _log.LogInformation("DEBUG: total item nodes = {Total}", totalNodes);

            if (totalNodes == 0)
            {
                return new List<VideoMetadata>();
            }

            var escapedTerm = Escape(term.ToLower());
            var expr = $@"
                //item[
                  contains(
                     translate(title,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),
                     '{escapedTerm}'
                  )
                  or
                  contains(
                     translate(author,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),
                     '{escapedTerm}'
                  )
                ]";

            var nodes = nav.Select(expr);

            var xs = new XmlSerializer(typeof(VideoMetadata), new XmlRootAttribute("item"));
            var results = new List<VideoMetadata>();

            foreach (XPathNavigator n in nodes)
            {
                try
                {
                    using var subtree = n.ReadSubtree();
                    subtree.MoveToContent();
                    var vm = (VideoMetadata)xs.Deserialize(subtree)!;
                    results.Add(vm);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to deserialize VideoMetadata node");
                }
            }

            _log.LogInformation("SOAP Search('{Term}') → {Count} hits", term, results.Count);
            return results;
        }

        private void GenerateXmlSnapshot(List<VideoMetadata> data)
        {
            try
            {
                Directory.CreateDirectory("Data");

                using var writer = XmlWriter.Create(XmlPath, new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = System.Text.Encoding.UTF8
                });

                writer.WriteStartDocument();
                writer.WriteStartElement("items"); 

                var serializer = new XmlSerializer(typeof(VideoMetadata), new XmlRootAttribute("item"));

                foreach (var item in data)
                {
                    serializer.Serialize(writer, item);
                }

                writer.WriteEndElement(); // items
                writer.WriteEndDocument();

                _log.LogInformation("Generated XML snapshot with {Count} items", data.Count);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error generating XML snapshot");
                throw;
            }
        }

        private void ValidateXmlWithJaxb()
        {
            try
            {
                var xmlContent = File.ReadAllText(XmlPath);

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var response = httpClient.PostAsJsonAsync(
                    "http://localhost:8080/api/validation/jaxb",
                    new { xmlContent }).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    _log.LogInformation("JAXB validation result: {Result}", result);
                }
                else
                {
                    _log.LogWarning("JAXB validation failed: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error calling JAXB validation service - continuing without validation");
            }
        }

        private static string Escape(string s) => s.Replace("'", "&apos;");
    }
}