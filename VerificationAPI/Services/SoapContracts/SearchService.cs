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
            Directory.CreateDirectory("Data");
            var ser = new XmlSerializer(typeof(List<VideoMetadata>));
            using (var fw = File.Create(XmlPath))
            {
                ser.Serialize(fw, _store);
            }

            var doc = new XPathDocument(XmlPath);
            var nav = doc.CreateNavigator();

            var expr = $@"
            //item[
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
            var results = new List<VideoMetadata>();
            var xs = new XmlSerializer(typeof(VideoMetadata));
            foreach (XPathNavigator n in nodes)
            {
                using var r = n.ReadSubtree();
                r.MoveToContent();
                var vm = (VideoMetadata)xs.Deserialize(r)!;
                results.Add(vm);
            }

            _log.LogInformation("SOAP Search('{Term}') → {Count} hits", term, results.Count);
            return results;
        }

        private static string Escape(string s) => s.Replace("'", "&apos;");
    }
}
