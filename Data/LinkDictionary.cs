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
        public Dictionary<string, LinkData> _Dictionary { get; } = new Dictionary<string, LinkData>();
        public Dictionary<string, List<string>> _Errors { get; } = new Dictionary<string, List<string>>();
        public Dictionary<string, string> _Redirects { get; } = new Dictionary<string, string>();
        public ILogger _Logger { get; }
        public string _BaseURL { get; set; } = String.Empty;
        //public SemaphoreSlim _SemaphoreToAdd { get; } = new SemaphoreSlim(1, 1);
        //public SemaphoreSlim _SemaphoreToGet { get; } = new SemaphoreSlim(1, 1);
        public SemaphoreSlim _SemaphoreUnited { get; } = new SemaphoreSlim(1, 1);

        public string[] staticFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".mp4", ".mp3",];

        public LinkDictionary(ILoggerFactory loggerFactory)
        {
            _Logger = loggerFactory.CreateLogger<LinkDictionary>();
        }

        public Dictionary<string, LinkData> GetDictionary()
        {
            return _Dictionary;
        }

        public Dictionary<string, List<string>> GetErrors()
        {
            return _Errors;
        }

        public Dictionary<string, string> GetRedirects()
        {
            return _Redirects;
        }

        public async void AddToDictionaryAsync(string URL, string whereFound)
        {
            //await _SemaphoreToAdd.WaitAsync();
            await _SemaphoreUnited.WaitAsync();

            whereFound = RemoveGrid(whereFound);
            if (!_Dictionary.ContainsKey(URL))
            {
                _Logger.LogInformation("Adding new link to list: {URL}", URL);
                LinkType linkType = AnalyzeLinkType(URL);
                _Logger.LogDebug("Type of a link: {linkType}", linkType);
                _Dictionary.Add(URL, new LinkData(linkType, whereFound));
            } else if (_Dictionary[URL].WhereFound.Count < 3 && !_Dictionary[URL].WhereFound.Contains(whereFound))
            {
                _Dictionary[URL].WhereFound.Add(whereFound);
            }

            //_SemaphoreToAdd.Release();
            _SemaphoreUnited.Release();
        }

        public void AddToErrors(string url, ReadOnlyCollection<OpenQA.Selenium.LogEntry> errors)
        {
            _Errors.Add(url, new List<string>());
            foreach (var error in errors)
            {
                _Logger.LogError("New error found: {error}", error.ToString());
                _Errors[url].Add(error.ToString());
            }
        }

        public void AddToRedirects(string originalUrl, string redirectedURL)
        {
            _Redirects.Add(originalUrl, redirectedURL);
        }

        public async Task<string> GetNextLinkAsync()
        {
        #nullable enable
            //await _SemaphoreToGet.WaitAsync();
            await _SemaphoreUnited.WaitAsync();
            string? link =
                _Dictionary
                .Where(i => (i.Value.Checked == false) && (i.Value.Type == LinkType.InternalPage))
                .Select(i => i.Key)
                .FirstOrDefault()
            ;
            _Logger.LogDebug("Next link to parse: {URLToParse}", link);
            MarkAsChecked(link);
            //_SemaphoreToGet.Release();
            _SemaphoreUnited.Release();
            return link ?? String.Empty;
        #nullable disable
        }

        public void MarkAsChecked(string URL)
        {
            if (URL != null)
            {
                _Logger.LogDebug("Marking link as checked: {URL}", URL);
                _Dictionary[URL].Checked = true;
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
