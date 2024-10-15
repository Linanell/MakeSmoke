using MakeSmoke.Enums;
using MakeSmoke.Models;
using static MakeSmoke.Utils.StringUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSmoke.Interfaces
{
    public class LinkDictionary: ILinkDictionary
    {
        public Dictionary<string, LinkData> Dictionary { get; } = new();
        public Dictionary<string, List<string>> Errors { get; } = new();
        public Dictionary<string, string> Redirects { get; } = new();
        public ILogger _Logger { get; }
        public string FilterURL { get; set; } = String.Empty;
        public List<string> ExcludedErrors { get; set; } = new();
        public SemaphoreSlim _SemaphoreDictionary { get; } = new(1, 1);
        public SemaphoreSlim _SemaphoreErrors { get; } = new(1, 1);

        public string[] staticFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".mp4", ".mp3",];

        public LinkDictionary(ILoggerFactory loggerFactory)
        {
            _Logger = loggerFactory.CreateLogger<LinkDictionary>();
        }

        public async void AddToDictionaryAsync(string URL, string whereFound)
        {
            await _SemaphoreDictionary.WaitAsync();

            if (whereFound != String.Empty)
            {
                whereFound = RemoveFragment(whereFound);
            }
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

            _SemaphoreDictionary.Release();
        }

        public async void AddToErrorsAsync(string url, ReadOnlyCollection<OpenQA.Selenium.LogEntry> errors)
        {
            await _SemaphoreErrors.WaitAsync();

            Errors.Add(url, new List<string>());
            foreach (var error in errors)
            {
                string errorString = error.ToString();
                _Logger.LogError("New error found: {error}", errorString);
                if (ExcludedErrors.Count > 0 && !ExcludedErrors.All(e => !errorString.Contains(e)))
                {
                    _Logger.LogInformation("Error has been excluded from adding to list: {error}", errorString);
                } else
                {
                    Errors[url].Add(errorString);
                }
            }

            _SemaphoreErrors.Release();
        }

        public void AddToRedirects(string originalUrl, string redirectedURL)
        {
            Redirects.Add(originalUrl, redirectedURL);
        }

        public async Task<string> GetNextLinkAsync()
        {
            await _SemaphoreDictionary.WaitAsync();
            string? link =
                Dictionary
                .Where(i => (i.Value.Checked == false) && (i.Value.Type == LinkType.InternalPage))
                .Select(i => i.Key)
                .FirstOrDefault()
            ;
            _Logger.LogDebug("Next link to parse: {URLToParse}", link);
            MarkAsChecked(link);
            _SemaphoreDictionary.Release();
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

        public bool CheckForExternal(string URL)
        {
            return !URL.Contains(FilterURL);
        }
    }
}
