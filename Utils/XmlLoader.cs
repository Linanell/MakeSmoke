using System.IO;
using System.Xml.Serialization;
using static MakeSmoke.Utils.Constants;

public class XmlLoader
{
    const string filePath = SETTINGS_FILE_NAME;
    public static Configuration LoadConfiguration()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            return (Configuration)serializer.Deserialize(fs);
        }
    }
}