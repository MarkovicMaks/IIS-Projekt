using System.Xml.Serialization;

namespace VerificationAPI.XmlModels
{
    [XmlRoot("Video", Namespace = "urn:tiktok")]
    public class Video
    {
        public string Id { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string Author { get; set; } = default!;
        public string Title { get; set; } = default!;
        public long Duration { get; set; }

        [XmlArray("Medias")]
        [XmlArrayItem("Media")]
        public List<Media> Medias { get; set; } = new();
    }

    public class Media
    {
        public string Url { get; set; } = default!;
        public long DataSize { get; set; }
        public string Quality { get; set; } = default!;
        public string Extension { get; set; } = default!;
        public string Type { get; set; } = default!;
    }
}
