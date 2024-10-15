using MakeSmoke.Models;
using OpenQA.Selenium;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MakeSmoke.Interfaces
{
    public interface ILinkDictionary
    {
        public Dictionary<string, LinkData> Dictionary { get; }
        public Dictionary<string, List<string>> Errors { get; }
        public Dictionary<string, string> Redirects { get; }
        public string FilterURL { get; set; }
        public List<string> ExcludedErrors { get; set; }

        public void AddToDictionaryAsync(string link, string whereFound);
        public void AddToErrorsAsync(string url, ReadOnlyCollection<LogEntry> errors);
        public void AddToRedirects(string originalUrl, string redirectedUrl);
        public Task<string> GetNextLinkAsync();
        public void MarkAsChecked(string URL);
        }
}
