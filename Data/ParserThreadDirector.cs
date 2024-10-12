using MakeSmoke.Interfaces;
using MakeSmoke.Utils;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSmoke.Data
{
    public class ParserThreadDirector : IParserThreadDirector
    {
        public ILogger _Logger { get; }

        public ILoggerFactory LogFactory { get; }

        public ILinkDictionary LinkDictionary { get; }

        public List<IParserAsync> Parsers { get; } = new List<IParserAsync>();
        public List<Task> ParserTasks { get; } = new List<Task>();

        public byte TargetThreads { get; set; }


        public ParserThreadDirector(ILoggerFactory loggerFactory, ILinkDictionary linkDictionary)
        {
            LogFactory = loggerFactory;
            LinkDictionary = linkDictionary;
            
            _Logger = loggerFactory.CreateLogger<ParserThreadDirector>();
        }

        public void StartThreads()
        {
            _Logger.LogDebug("Starting threads.");
            while (Parsers.Count < TargetThreads)
            {
                IParserAsync newParser = CreateParser();
                Parsers.Add(newParser);
                _Logger.LogDebug("Created new parser instance.");
            }
        }

        public void StartParse(string URLToParse, bool isRecursive)
        {
            if (Parsers.Count == 0) 
            {
                StartThreads();
            }

            _Logger.LogDebug("Starting parsing process.");
            
            bool firstParser = true; 
            foreach (IParserAsync parser in Parsers)
            {
                IWebDriver driver = DriverGenerator.GetChromeDriver();
                Task newParseTask = parser.ParseAsync(driver, URLToParse, isRecursive);
                ParserTasks.Add(newParseTask);
                _Logger.LogDebug("Created new task to parse");

                // if it is first parser then generate time to parse first page and get links for other parsers to parse
                if (firstParser)
                {
                    Thread.Sleep(5000);
                    firstParser = false;
                }
            }
            while (true)
            {
                //if (!ParserTasks.All(task => !task.IsFaulted))
                //{
                //    RestartTasks(URLToParse, baseURL, isRecursive);
                //}
                if (ParserTasks.All(
                    task => 
                    task.IsCompleted || 
                    task.IsCompletedSuccessfully ||
                    task.IsFaulted ||
                    task.IsCanceled
                ))
                {
                    break;
                }
                
                Thread.Sleep(10000);
            }
        }

        public async void WaitForEnd()
        {
            if (ParserTasks.Any())
            {
                await Task.WhenAll(ParserTasks);
            }
        }

        public IParserAsync CreateParser()
        {
            return new ParserAsync(LogFactory, LinkDictionary);
        }

        // not working for now
        public void RestartTasks(string URLToParse, bool isRecursive)
        {
            for (int i = 0; i < ParserTasks.Count; i++)
            {
                if (ParserTasks[i].IsFaulted)
                {
                    ParserTasks[i] = Parsers[i].ParseAsync(DriverGenerator.GetChromeDriver(), URLToParse, isRecursive);
                }
            }
        }
    }
}
