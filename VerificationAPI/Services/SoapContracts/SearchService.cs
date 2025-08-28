using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Services.SoapContracts
{
    public class SearchService : ISearchService
    {
        private readonly List<VideoMetadata> _store;
        private readonly ILogger<SearchService> _log;
        private const string XmlPath = "Data/videos.xml";

        public SearchService(List<VideoMetadata> store, ILogger<SearchService> log)
        {
            _store = store;
            _log = log;
        }

        public List<VideoMetadata> Search(string term)
        {
            // 1️⃣ Write the XML snapshot
            Directory.CreateDirectory("Data");
            var listSer = new XmlSerializer(typeof(List<VideoMetadata>));
            using (var fw = File.Create(XmlPath))
                listSer.Serialize(fw, _store);

            _log.LogInformation("DEBUG: XML snapshot written to {Path}", Path.GetFullPath(XmlPath));
            _log.LogInformation("DEBUG: First 300 chars = {Dump}",
                File.ReadAllText(XmlPath).Substring(0, Math.Min(300, (int)new FileInfo(XmlPath).Length)));

            // 2️⃣ NEW: Call Java JAXB validation service
            ValidateXmlWithJaxb();

            // 3️⃣ Prepare XPath nav + namespace
            var doc = new XPathDocument(XmlPath);
            var nav = doc.CreateNavigator();
            var ns = new XmlNamespaceManager(nav.NameTable);
            ns.AddNamespace("d", "http://schemas.datacontract.org/2004/07/VerificationAPI.XmlModels");

            // How many VideoMetadata elements exist at all?
            var totalNodes = (int)(double)nav.Evaluate("count(//VideoMetadata)");
            _log.LogInformation("DEBUG: total VideoMetadata nodes = {Total}", totalNodes);

            // ➊ build XPath (no namespace, lower-case title/author)
            var expr = $@"
                    //VideoMetadata[
                      contains(
                         translate(title,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),
                         '{Escape(term.ToLower())}'
                      )
                      or
                      contains(
                         translate(author,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),
                         '{Escape(term.ToLower())}'
                      )
                    ]";
            var nodes = nav.Select(expr);
            _log.LogInformation("DEBUG: nodes matching expr = {Count}", nodes.Count);

            // ➋ deserializer expects <VideoMetadata> in *empty* namespace
            var xs = new XmlSerializer(
                typeof(VideoMetadata),
                new XmlRootAttribute("VideoMetadata") { Namespace = "" });

            var results = new List<VideoMetadata>();

            foreach (XPathNavigator n in nodes)
            {
                using var subtree = n.ReadSubtree();
                subtree.MoveToContent();
                var vm = (VideoMetadata)xs.Deserialize(subtree)!;
                results.Add(vm);
            }

            _log.LogInformation("SOAP Search('{Term}') → {Count} hits", term, results.Count);
            return results;
        }

        private void ValidateXmlWithJaxb()
        {
            try
            {
                var xmlContent = File.ReadAllText(XmlPath);

                using var httpClient = new HttpClient();
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
                _log.LogError(ex, "Error calling JAXB validation service");
            }
        }

        private static string Escape(string s) => s.Replace("'", "&apos;");
    }
}