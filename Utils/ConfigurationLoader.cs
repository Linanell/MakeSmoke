using System.IO;
using System.Xml.Serialization;

public class ConfigurationLoader
{
    const string filePath = "settings.xml";
    public static Configuration LoadConfiguration()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            return (Configuration)serializer.Deserialize(fs);
        }
    }
}