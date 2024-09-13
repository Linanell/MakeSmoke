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

namespace MakeSmoke.Data
{
    public class Parser : IParser
    {
        protected ILogger _Logger { get; }
        protected IWebDriver _Driver { get; }
        protected Dictionary<string, LinkData> _Dictionary { get; } = new Dictionary<string, LinkData>();
        protected Dictionary<string, List<string>> _Errors { get; } = new Dictionary<string, List<string>>();
        protected string _originalURL = String.Empty;
        protected string _formattedURL = String.Empty;

        protected string[] staticFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".mp4", ".mp3",];

        public Parser(ILoggerFactory loggerFactory, IWebDriver driver)
        {
            _Logger = loggerFactory.CreateLogger<Parser>();
            _Driver = driver;
        }

        public void Parse(string URLToParse, string baseURL, bool isRecursive)
        {
            InitializeURLs(URLToParse, baseURL);
            AddToDictionary(_originalURL);

            string? currentURL;
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
            Thread.Sleep(1500);
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
                        _Logger.LogError("New error found: {error}", error.ToString());
                        _Errors[URL].Add(error.ToString());
                    }
                }
            }
        }

        public void ParseLinksOnPage(string URL)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(_Driver.PageSource);
            List<string> listOfLinks = new List<string>();
            try
            {
                listOfLinks.AddRange(
                html
                    .DocumentNode
                    .SelectNodes("//a[@href]")
                    .Select(node => node.GetAttributeValue("href", string.Empty))
                    .Where(href => !string.IsNullOrEmpty(href))
                    .ToList()
                );
            }
            catch (ArgumentNullException ex)
            {
                _Logger.LogCritical("Something gone wrong during page parsing. Looks like page have no links: {Exception}", ex);
            }

            foreach (var linkOnPage in listOfLinks)
            {
                string fullLink = CreateFullLink(linkOnPage, URL);
                if (!_Dictionary.ContainsKey(fullLink))
                {
                    AddToDictionary(fullLink);
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

        private void InitializeURLs(string URLToParse, string baseURL)
        {
            _originalURL = URLToParse;
            _Logger.LogDebug("URL to parse: {OriginalURL}", _originalURL);
            if (baseURL == String.Empty)
            {
                int pos = _originalURL.IndexOf("#/");
                if (pos != -1)
                {
                    _formattedURL = _originalURL.Substring(0, pos);
                }
                else _formattedURL = _originalURL;
            }
            else
            {
                _formattedURL = baseURL;
            }
            _Logger.LogDebug("Base URL: {BaseURL}", baseURL);
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
            return !URL.Contains(_formattedURL);
        }

        protected string CreateFullLink(string urlToCheck, string currentLink)
        {
            Uri absoluteUri = new Uri(new Uri(currentLink), urlToCheck);
            return absoluteUri.ToString();
        }

        public string ToJson(Object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
