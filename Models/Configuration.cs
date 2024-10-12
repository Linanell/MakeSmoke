using System.Xml.Serialization;

[XmlRoot("Configuration")]
public class Configuration
{
    [XmlElement("URL")]
    public string URL { get; set; }

    [XmlElement("FilterURL")]
    public string FilterURL { get; set; }

    [XmlElement("DebugMode")]
    public bool DebugMode { get; set; }

    [XmlElement("Recursive")]
    public bool Recursive { get; set; }

    [XmlElement("ThreadsCount")]
    public byte ThreadsCount { get; set; }
}