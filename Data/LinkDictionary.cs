using MakeSmoke.Enums;
using MakeSmoke.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MakeSmoke.Utils.StringUtils;

namespace MakeSmoke.Interfaces
{
    public class LinkDictionary: ILinkDictionary
    {
        public Dictionary<string, LinkData> Dictionary { get; } = new Dictionary<string, LinkData>();
        public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>();
        public Dictionary<string, string> Redirects { get; } = new Dictionary<string, string>();
        public ILogger _Logger { get; }
        public string _BaseURL { get; set; } = String.Empty;
        public SemaphoreSlim _SemaphoreUnited { get; } = new SemaphoreSlim(1, 1);

        public string[] staticFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".mp4", ".mp3",];

        public LinkDictionary(ILoggerFactory loggerFactory)
        {
            _Logger = loggerFactory.CreateLogger<LinkDictionary>();
        }

        public async void AddToDictionaryAsync(string URL, string whereFound)
        {
            await _SemaphoreUnited.WaitAsync();

            whereFound = RemoveGrid(whereFound);
            if (!Dictionary.ContainsKey(URL))
            {
                _Logger.LogInformation("Adding new link to list: {URL}", URL);
                LinkType linkType = AnalyzeLinkType(URL);
                _Logger.LogDebug("Type of a link: {linkType}", linkType);
                Dictionary.Add(URL, new LinkData(linkType, whereFound));
            } else if (Dictionary[URL].WhereFound.Count < 3 && !Dictionary[URL].WhereFound.Contains(whereFound))
            {
                Dictionary[URL].WhereFound.Add(whereFound);
            }

            _SemaphoreUnited.Release();
        }

        public void AddToErrors(string url, ReadOnlyCollection<OpenQA.Selenium.LogEntry> errors)
        {
            Errors.Add(url, new List<string>());
            foreach (var error in errors)
            {
                _Logger.LogError("New error found: {error}", error.ToString());
                Errors[url].Add(error.ToString());
            }
        }

        public void AddToRedirects(string originalUrl, string redirectedURL)
        {
            Redirects.Add(originalUrl, redirectedURL);
        }

        public async Task<string> GetNextLinkAsync()
        {
            await _SemaphoreUnited.WaitAsync();
            string? link =
                Dictionary
                .Where(i => (i.Value.Checked == false) && (i.Value.Type == LinkType.InternalPage))
                .Select(i => i.Key)
                .FirstOrDefault()
            ;
            _Logger.LogDebug("Next link to parse: {URLToParse}", link);
            MarkAsChecked(link);
            _SemaphoreUnited.Release();
            return link ?? String.Empty;
        }

        public void MarkAsChecked(string URL)
        {
            if (URL != null)
            {
                _Logger.LogDebug("Marking link as checked: {URL}", URL);
                Dictionary[URL].Checked = true;
            }
        }

        public LinkType AnalyzeLinkType(string URL)
        {
            if (CheckForStatic(URL))
            {
                return LinkType.StaticFile;
            }
            else if (CheckForExternal(URL))
            {
                return LinkType.ExternalPage;
            }
            else return LinkType.InternalPage;
        }

        public bool CheckForStatic(string URL)
        {
            foreach (string extension in staticFileExtensions)
            {
                if (URL.EndsWith(extension)) return true;
            }
            return false;
        }

        public void SetFormattedURL(string baseURL)
        {
            _BaseURL = baseURL;
            _Logger.LogDebug("Base URL: { BaseURL }", _BaseURL);
        }

        public bool CheckForExternal(string URL)
        {
            return !URL.Contains(_BaseURL);
        }
    }
}
