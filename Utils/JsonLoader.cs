using Newtonsoft.Json.Linq;
using System.IO;

namespace MakeSmoke.Utils
{
    public static class JsonLoader
    {
        public static JArray? LoadJson(string filePath, string key)
        {
            string jsonContent = string.Empty;
            try
            {
                jsonContent = File.ReadAllText(filePath);
            }
            catch
            {
                return null;
            }
            
            var jsonObject = JObject.Parse(jsonContent);

            if (jsonObject.ContainsKey(key)) return (JArray)jsonObject[key];
            return null;
        }
    }
}
