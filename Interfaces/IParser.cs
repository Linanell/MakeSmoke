namespace MakeSmoke.Interfaces
{
    public interface IParser
    {
        public void Parse(string URL, string baseURL, bool isRecursive);
    }
}
