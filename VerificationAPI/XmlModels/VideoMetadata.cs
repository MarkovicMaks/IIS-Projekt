using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VerificationAPI.XmlModels
{
    [XmlRoot("item")]
    public class VideoMetadata
    {
        [XmlElement("url")]
        public string Url { get; set; }

        [XmlElement("source")]
        public string Source { get; set; }

        [XmlElement("id")]
        public long Id { get; set; }

        [XmlElement("unique_id")]
        public string UniqueId { get; set; }

        [XmlElement("author")]
        public string Author { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("thumbnail")]
        public string Thumbnail { get; set; }

        [XmlElement("duration")]
        public int Duration { get; set; }

        [XmlArray("medias")]
        [XmlArrayItem("media")]
        public List<Media> Medias { get; set; }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("error")]
        public bool Error { get; set; }

        [XmlElement("time_end")]
        public int TimeEnd { get; set; }
    }

    public class Media
    {
        [XmlElement("url")]
        public string Url { get; set; }

        [XmlElement("data_size")]
        public long DataSize { get; set; }

        [XmlElement("quality")]
        public string Quality { get; set; }

        [XmlElement("extension")]
        public string Extension { get; set; }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("duration")]
        public int? Duration { get; set; }
    }
}