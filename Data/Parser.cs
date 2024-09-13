using MakeSmoke.Enums;
using MakeSmoke.Interfaces;
using MakeSmoke.Models;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MakeSmoke.Data
{
    public class Parser : IParser
    {
        protected ILogger _Logger { get; }
        protected IWebDriver _Driver { get; }
        protected Dictionary<string, LinkData> _Dictionary { get; set; } = new Dictionary<string, LinkData>();
        protected string _originalURL = "";

        protected string[] staticFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".mp4", ".mp3",];

        public Parser(ILoggerFactory loggerFactory, IWebDriver driver)
        {
            _Logger = loggerFactory.CreateLogger<Parser>();
            _Driver = driver;
        }

        public void Parse(string URLToParse, bool isRecursive)
        {
            _originalURL = URLToParse;
            string? currentURL;
            AddToDictionary(_originalURL);

            try
            {
                if (isRecursive)
                {
                    _Logger.LogInformation("Entering recursive mode.");
                    while ((currentURL = GetNextLink()) != null)
                    {
                        ParseLinksOnLink(currentURL);
                    }
                    _Logger.LogInformation("Links to parse are over. Ends parsing.");
                } else
                {
                    _Logger.LogInformation("Start parsing URL: {URL}", _originalURL);
                    ParseLinksOnLink(_originalURL);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogCritical("Something gone wrong during parsing: {exception}", ex);
                throw;
            }
            finally
            {
                // write all links
                // write all errors
                _Logger.LogDebug("Shutdown driver.");
                _Driver.Quit();
                _Logger.LogDebug("Driver shutdowned.");
            }

        }


        public void ParseLinksOnLink(string URL)
        {
            _Logger.LogInformation("Going to URL: {URLToGo}", URL);
            _Driver.Navigate().GoToUrl(URL);
            Thread.Sleep(150);
            string? link;
            List<IWebElement> linksAsElements =
            [
                .. _Driver.FindElements(By.XPath("//a[@href]")),
                .. _Driver.FindElements(By.XPath("//img[@src]")),
            ];
            List<string?> listOfLinks = new List<string?>();
            foreach (IWebElement element in linksAsElements)
            {
                listOfLinks.Add(GetURLFromElement(element));
            }
            listOfLinks.Distinct();
            foreach (var linkOnPage in listOfLinks)
            {
                if (linkOnPage != null && !_Dictionary.ContainsKey(linkOnPage))
                {
                    AddToDictionary(linkOnPage);
                }
            }
            MarkAsChecked(URL);
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


        protected string? GetURLFromElement(IWebElement element)
        {
            try
            {
                if (element.GetAttribute("href") != null)
                {
                    return element.GetAttribute("href");
                }
                else return element.GetAttribute("src");
            }
            catch (StaleElementReferenceException ex)
            {
                _Logger.LogWarning("Stale element reference. Returning null: {element}", element.ToString());
                return null;
            }
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
    }
}
