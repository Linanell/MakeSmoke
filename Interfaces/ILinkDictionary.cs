using MakeSmoke.Models;
using OpenQA.Selenium;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MakeSmoke.Interfaces
{
    public interface ILinkDictionary
    {
        public Dictionary<string, LinkData> GetDictionary();
        public Dictionary<string, List<string>> GetErrors();
        public Dictionary<string, string> GetRedirects();

        public void AddToDictionaryAsync(string link, string whereFound);
        public void AddToErrors(string url, ReadOnlyCollection<LogEntry> errors);
        public void AddToRedirects(string originalUrl, string redirectedUrl);

        public Task<string> GetNextLinkAsync();

        public void MarkAsChecked(string URL);

        public void SetFormattedURL(string baseURL);
        }
}
