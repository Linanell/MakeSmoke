using System;

namespace MakeSmoke.Utils
{
    public static class StringUtils
    {
        public static string RemoveFragment(Uri url)
        {
            return RemoveFragment(new UriBuilder(url));
        }
        public static string RemoveFragment(string url)
        {
            return RemoveFragment(new UriBuilder(url));
        }
        public static string RemoveFragment(UriBuilder uriBuilder)
        {
            uriBuilder.Port = -1;
            uriBuilder.Fragment = String.Empty;
            return uriBuilder.ToString();
        }
    }
}
