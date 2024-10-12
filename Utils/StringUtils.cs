namespace MakeSmoke.Utils
{
    public static class StringUtils
    {
        public static string RemoveGrid(string url)
        {
            return url.EndsWith('#') ? url.Remove(url.Length - 2) : url;
        }
    }
}
