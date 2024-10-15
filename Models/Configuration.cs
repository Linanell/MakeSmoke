using System.Collections.Generic;
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

    [XmlElement("LogFileName")]
    public string LogFileName { get; set; }

    [XmlElement("BlackList")]
    public string BlackList { get; set; }

    [XmlElement("VerifyLinks")]
    public string VerifyLinks { get; set; }
}