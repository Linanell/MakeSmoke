using HtmlAgilityPack;
using MakeSmoke.Interfaces;
using static MakeSmoke.Utils.StringUtils;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSmoke.Data
{
    public class ParserAsync : IParserAsync
    {
        protected ILogger _Logger { get; }
        protected ILinkDictionary _LinkDictionary { get; }
        protected IWebDriver _Driver { get; set; }

        public ParserAsync(ILoggerFactory loggerFactory, ILinkDictionary linkDictionary)
        {
            _Logger = loggerFactory.CreateLogger<ParserAsync>();
            _LinkDictionary = linkDictionary;
        }

        public async Task ParseAsync(IWebDriver driver,string URLToParse, bool isRecursive)
        {
            await Task.Run(() =>
            {
                _LinkDictionary.AddToDictionaryAsync(URLToParse, String.Empty);

                string currentURL = String.Empty;
                try
                {
                    if (_Driver == null)
                    {
                        _Driver = driver;
                    }
                    if (isRecursive)
                    {
                        _Logger.LogInformation("Entering recursive mode.");
                        while ((currentURL = _LinkDictionary.GetNextLinkAsync().Result) != String.Empty && currentURL != null)
                        {
                            CheckLink(currentURL);
                        }
                        _Logger.LogInformation("Links to parse are over. Ends parsing.");
                    }
                    else
                    {
                        _Logger.LogInformation("Start parsing URL: {URL}", URLToParse);
                        CheckLink(URLToParse);
                    }
                }
                finally
                {
                    _Logger.LogDebug("Shutdown driver.");
                    _Driver.Quit();
                    _Logger.LogDebug("Driver shutdowned.");
                }
            }).ConfigureAwait(false);
        }

        public void CheckLink(string URL)
        {
            try
            {
                _Logger.LogInformation("Going to URL: {URLToGo}", URL);
                _Driver.Navigate().GoToUrlAsync(URL);
                WaitForPageLoad();
                Thread.Sleep(1000);
                CheckErrorsOnPage(URL);
                ParseLinksOnPage(URL);
                CheckForRedirect(URL);
            }
            catch (Exception ex)
            {
                _Logger.LogCritical("Something gone wrong during parsing: {exception}", ex);
            }
        }

        public void CheckForRedirect(string URL)
        {
            if (_Driver.Url != URL) _LinkDictionary.AddToRedirects(URL, _Driver.Url);
        }

        public void CheckErrorsOnPage(string URL)
        {
            var errors = _Driver.Manage().Logs.GetLog(LogType.Browser);
            if (errors.Count > 0)
            {
                _LinkDictionary.AddToErrorsAsync(URL, errors);
            }
        }

        public void ParseLinksOnPage(string URL)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(_Driver.PageSource);
            List<string> listOfLinks = new List<string>();
            try
            {
                _Logger.LogDebug("Fetching links from page: {page}", URL);
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
            if (listOfLinks.Count > 0)
            {
                listOfLinks = listOfLinks.Select(link => WebUtility.HtmlDecode(link)).ToList();

                foreach (var linkOnPage in listOfLinks)
                {
                    string fullLink = CreateFullLink(linkOnPage, URL);

                    if (fullLink != null && fullLink != String.Empty)
                    {
                        _LinkDictionary.AddToDictionaryAsync(fullLink, URL);
                    }
                }
            }
        }

        protected string CreateFullLink(string urlToCheck, string currentLink)
        {
            Uri absoluteUri = new Uri(new Uri(currentLink), urlToCheck);
            return RemoveFragment(absoluteUri);
        }

        public void WaitForPageLoad()
        {
            WebDriverWait wait = new WebDriverWait(_Driver, TimeSpan.FromSeconds(30));
            wait.Until(driver =>
            {
                try
                {
                    Thread.Sleep(100);

                    string readyState = ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").ToString();


                    return readyState == "complete";
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });
        }
    }
}
