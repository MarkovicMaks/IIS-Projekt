using System.Xml.Serialization;

namespace VerificationAPI.Models
{
    [XmlRoot("Reports")]
    public class WeatherReports
    {
        [XmlElement("Report")]
        public List<WeatherReport> Reports { get; set; } = new List<WeatherReport>();
    }

    public class WeatherReport
    {
        [XmlElement("GradIme")]
        public string CityName { get; set; } = string.Empty;

        [XmlElement("Temp")]
        public string Temperature { get; set; } = string.Empty;

        [XmlElement("TempMax")]
        public string MaxTemperature { get; set; } = string.Empty;

        [XmlElement("TempMin")]
        public string MinTemperature { get; set; } = string.Empty;

        [XmlElement("Vlaga")]
        public string Humidity { get; set; } = string.Empty;

        [XmlElement("Tlak")]
        public string Pressure { get; set; } = string.Empty;

        [XmlElement("VjetarSmjer")]
        public string WindDirection { get; set; } = string.Empty;

        [XmlElement("VjetarBrzina")]
        public string WindSpeed { get; set; } = string.Empty;

        [XmlElement("Vrijeme")]
        public string Weather { get; set; } = string.Empty;
    }
}