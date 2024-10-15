using MakeSmoke.Data;
using MakeSmoke.Interfaces;
using static MakeSmoke.Utils.Constants;
using static MakeSmoke.Utils.StringUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NReco.Logging.File;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MakeSmoke.Utils;
using MakeSmoke.Models;
using System.Linq;

namespace MakeSmoke
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string URL = String.Empty;
            string filterURL = String.Empty;
            bool debugMode = false;
            bool recursive = false;
            byte threadsCount = 0;
            string logFileName = String.Empty;
            string blackListFileName = String.Empty;
            string verifyLinksFileName = String.Empty;
            List<string> excludedErrors = new();
            List<string> linksToVerify = new();

            try
            {
                Configuration settings = XmlLoader.LoadConfiguration();
                if (settings != null)
                {
                    if (settings.URL != String.Empty) URL = settings.URL;
                    if (settings.FilterURL != String.Empty) filterURL = settings.FilterURL;
                    if (settings.DebugMode) debugMode = true;
                    if (settings.Recursive) recursive = true;
                    if (settings.ThreadsCount != 0) threadsCount = settings.ThreadsCount;
                    if (settings.LogFileName != String.Empty) logFileName = settings.LogFileName;
                    if (settings.BlackList != String.Empty) blackListFileName = settings.BlackList;
                    if (settings.BlackList != String.Empty) verifyLinksFileName = settings.VerifyLinks;
                }
            }
            catch
            {
                Console.WriteLine("Failed to load parser configuration.");
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-r"))
                {
                    recursive = true;
                }
                else if (args[i].Equals("-d"))
                {
                    debugMode = true;
                }
                else if (args[i].StartsWith("--threads="))
                {
                    string threadsCountString = args[i].Substring(10);
                    bool isParsed = Byte.TryParse(threadsCountString, out threadsCount);
                    if (!isParsed)
                    {
                        Console.WriteLine($"Unknown number of threads.");
                    }
                }
                else if (args[i].StartsWith("--filter="))
                {
                    filterURL = args[i].Substring(9);
                }
                else if (args[i].StartsWith("--name="))
                {
                    logFileName = args[i].Substring(7);
                }
                else if (args[i].StartsWith("--blacklist="))
                {
                    excludedErrors.Add(args[i].Substring(12));
                }
                else if (args[i].StartsWith("--verify="))
                {
                    excludedErrors.Add(args[i].Substring(9));
                }
                else if (args[i].StartsWith("-"))
                {
                    Console.WriteLine($"Unknown argument: {args[i]}");
                }
                else
                {
                    URL = args[i];
                }
            }

            if (blackListFileName != String.Empty) FillArrayWithJson(excludedErrors, blackListFileName, "exclude");
            if (verifyLinksFileName != String.Empty) FillArrayWithJson(linksToVerify, verifyLinksFileName, "verify");

                if (URL == String.Empty)
            {
                Console.WriteLine("No URL is given. Not enough data is given. Closing program.");
                return;
            }

            if (threadsCount == 0)
            {
                threadsCount = THREADS_COUNT_BY_DEFAULT;
                Console.WriteLine($"No threads count is given. Using default value: { THREADS_COUNT_BY_DEFAULT }");
            }
            if (logFileName == String.Empty)
            {
                logFileName = LOG_FILE_NAME_BY_DEFAULT;
                Console.WriteLine($"No log file name is given. Using default value: { LOG_FILE_NAME_BY_DEFAULT }");
            }

            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            IServiceCollection services = new ServiceCollection();
            services.AddLogging(builder => GetLoggerOptions(builder, $"{logFileName}_log_{timeStamp}.txt", debugMode));
            services.AddSingleton<ILinkDictionary, LinkDictionary>();
            services.AddSingleton<IParserThreadDirector, ParserThreadDirector>();
            ServiceProvider app = services.BuildServiceProvider();
            ILogger<Program> logger = app.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
            logger.LogDebug("Logger initialized");
            logger.LogInformation("Debug mode: {debug}, Recursive mode: {recursive}", debugMode, recursive);

            ILinkDictionary linkDictionary = app.GetRequiredService<ILinkDictionary>();
            linkDictionary.FilterURL = RemoveFragment(filterURL);
            linkDictionary.ExcludedErrors = excludedErrors;

            IParserThreadDirector threadDirector = app.GetRequiredService<IParserThreadDirector>();
            threadDirector.TargetThreads = threadsCount;
            logger.LogDebug("Thread director initialized");

            threadDirector.StartParse(URL, recursive);

            File.WriteAllText($"{logFileName}_links_{timeStamp}.txt", ToJson(linkDictionary.Dictionary));
            File.WriteAllText($"{logFileName}_errors_{timeStamp}.txt", ToJson(linkDictionary.Errors));
            File.WriteAllText($"{logFileName}_redirects_{timeStamp}.txt", ToJson(linkDictionary.Redirects));
            logger.LogInformation("Parsing is ended.");

            if (linksToVerify.Count > 0)
            {
                bool allLinksVerified = true;
                Dictionary<string, LinkData> dictionary = linkDictionary.Dictionary; 
                foreach (var link in linksToVerify)
                {
                    if (dictionary.All(dict => !dict.Key.Contains(link)))
                    {
                        allLinksVerified = false;
                        logger.LogError("{link} was not found in parsed links.", link);
                    }
                }
                if (allLinksVerified)
                {
                    logger.LogInformation(
                        "{linksCount} link(-s) have been verified. Everything ok. Ending program.",
                        linksToVerify.Count
                    );
                }
            } else
            {
                logger.LogInformation("No links to verify. Ending program.");
            }
        }

        private static void GetLoggerOptions(ILoggingBuilder builder, string logFileName, bool debugMode = false) {
            builder.AddConsole();
            builder.AddFile(logFileName, append: true);
            if (debugMode)
            {
                builder.AddDebug();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            }
        }

        public static void FillArrayWithJson(List<string> arrayToFill, string filePath, string key)
        {
            JArray? array = JsonLoader.LoadJson(filePath, "exclude");
            if (array != null)
            {
                foreach (JToken value in array)
                {
                    arrayToFill.Add(value.ToString());
                }
            }
            else
            {
                Console.WriteLine($"{filePath} file parse failed");
            }
        }

        public static string ToJson(Object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
