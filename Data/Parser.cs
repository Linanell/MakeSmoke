using HtmlAgilityPack;
using MakeSmoke.Enums;
using MakeSmoke.Interfaces;
using MakeSmoke.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSmoke.Data
{
    public class Parser : IParser
    {
        protected ILogger _Logger { get; }
        protected IWebDriver _Driver { get; }
        protected Dictionary<string, LinkData> _Dictionary { get; } = new Dictionary<string, LinkData>();
        protected Dictionary<string, List<string>> _Errors { get; } = new Dictionary<string, List<string>>();
        protected string _originalURL = String.Empty;
        protected string _originalURI = String.Empty;

        protected string[] staticFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".mp4", ".mp3",];

        public Parser(ILoggerFactory loggerFactory, IWebDriver driver)
        {
            _Logger = loggerFactory.CreateLogger<Parser>();
            _Driver = driver;
        }

        public void Parse(string URLToParse, bool isRecursive)
        {
            Uri uri = new Uri(URLToParse);
            string uriToParse = $"{uri.Scheme}://{uri.Host}";
            if (!uri.IsDefaultPort)
            {
                uriToParse += $":{uri.Port}";
            }
            string? currentURL;
            _originalURL = URLToParse;
            _originalURI = uriToParse;
            AddToDictionary(_originalURL);

            try
            {
                if (isRecursive)
                {
                    _Logger.LogInformation("Entering recursive mode.");
                    while ((currentURL = GetNextLink()) != null)
                    {
                        CheckLink(currentURL);
                    }
                    _Logger.LogInformation("Links to parse are over. Ends parsing.");
                } else
                {
                    _Logger.LogInformation("Start parsing URL: {URL}", _originalURL);
                    CheckLink(_originalURL);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogCritical("Something gone wrong during parsing: {exception}", ex);
                throw;
            }
            finally
            {

                File.WriteAllText($"makesmoke_links_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt", ToJson(_Dictionary));
                File.WriteAllText($"makesmoke_errors_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt", ToJson(_Errors));
                _Logger.LogDebug("Shutdown driver.");
                _Driver.Quit();
                _Logger.LogDebug("Driver shutdowned.");
            }

        }

        public void CheckLink(string URL)
        {
            _Logger.LogInformation("Going to URL: {URLToGo}", URL);
            _Driver.Navigate().GoToUrl(URL);
            Thread.Sleep(150);
            CheckErrorsOnPage(URL);
            ParseLinksOnPage(URL);
            MarkAsChecked(URL);
        }

        public void CheckErrorsOnPage(string URL)
        {
            var errors = _Driver.Manage().Logs.GetLog(LogType.Browser);
            if (errors.Count > 0)
            {
                _Errors.Add(URL, new List<string>());
                foreach (var error in errors)
                {
                    {
                        _Errors[URL].Add(error.ToString());
                    }
                }
            }
        }

        public void ParseLinksOnPage(string URL)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(_Driver.PageSource);
            List<string> listOfLinks =
            [
                .. html
                    .DocumentNode
                    .SelectNodes("//a[@href]")
                    .Select(node => node.GetAttributeValue("href", string.Empty))
                    .Where(href => !string.IsNullOrEmpty(href) && !href.StartsWith("#"))
                    .ToList(),
            ];

            foreach (var linkOnPage in listOfLinks)
            {
                string newLink = CheckForHalfLink(linkOnPage, _originalURI);
                if (!_Dictionary.ContainsKey(newLink))
                {
                    AddToDictionary(newLink);
                }
            }
        }

        private string? GetNextLink()
        {
            string? link = 
                _Dictionary
                .Where(i => (i.Value.Checked == false) && (i.Value.Type == LinkType.InternalPage))
                .Select(i => i.Key)
                .FirstOrDefault()
            ;
            _Logger.LogDebug("Next link to parse: {URLToParse}", link);
            return link;
        }

        protected void MarkAsChecked (string URL)
        {
            _Logger.LogDebug("Marking link as checked: {URL}", URL);
            _Dictionary[URL].Checked = true;
        }


        protected void AddToDictionary (string URL)
        {
            _Logger.LogInformation("Adding new link to list: {URL}", URL);
            LinkType linkType = AnalyzeLinkType(URL);
            _Logger.LogDebug("Type of a link: {linkType}", linkType);
            _Dictionary.Add(URL, new LinkData(linkType));
        }

        protected LinkType AnalyzeLinkType(string URL)
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

        protected bool CheckForStatic(string URL)
        {
            foreach (string extension in staticFileExtensions) 
            {
                if (URL.EndsWith(extension)) return true;
            }
            return false;
        }

        protected bool CheckForExternal(string URL)
        {
            return !URL.Contains(_originalURL);
        }

        protected string CheckForHalfLink(string URL, string firstHalfOfLink)
        {
            return URL.StartsWith("/") ? firstHalfOfLink+URL : URL;
        }

        public string ToJson(Object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
