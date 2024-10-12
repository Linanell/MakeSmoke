using MakeSmoke.Data;
using MakeSmoke.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NReco.Logging.File;
using System;
using System.IO;

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
            string threadsCountString = String.Empty;
            byte threadsCount = 0;
            
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
                    threadsCountString = args[i].Substring(10);
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
                else if (args[i].StartsWith("-"))
                {
                    Console.WriteLine($"Unknown argument: {args[i]}");
                }
                else
                {
                    URL = args[i];
                }
            }

            try
            {
                Configuration settings = ConfigurationLoader.LoadConfiguration();
                if (settings != null)
                {
                    if (URL == String.Empty && settings.URL != String.Empty) URL = settings.URL;
                    if (filterURL == String.Empty && settings.FilterURL != String.Empty) filterURL = settings.FilterURL;
                    if (settings.DebugMode == true) debugMode = true;
                    if (settings.Recursive == true) recursive = true;
                    if (threadsCount == 0 && settings.ThreadsCount != 0) threadsCount = settings.ThreadsCount;
                }
            }
            catch
            {
                Console.WriteLine("Failed to load parser configuration.");
            }

            bool isEnoughForParse = true;
            if (URL == String.Empty)
            {
                isEnoughForParse = false;
                Console.WriteLine("No URL is given.");
            }
            if (threadsCount == 0)
            {
                isEnoughForParse = false;
                Console.WriteLine("No threads count is given.");
            }
            if (!isEnoughForParse)
            {
                Console.WriteLine("Not enough data is given. Closing program.");
                return;
            }

            filterURL = GetFormattedURL(URL, filterURL);

            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string logFileName = $"makesmoke_log_{timeStamp}.txt";
            
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(builder => GetLoggerOptions(builder, logFileName, debugMode));
            services.AddSingleton<ILinkDictionary, LinkDictionary>();
            services.AddSingleton<IParserThreadDirector, ParserThreadDirector>();
            ServiceProvider app = services.BuildServiceProvider();
            ILogger<Program> logger = app.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
            logger.LogDebug("Logger initialized");
            logger.LogInformation("Debug mode: {debug}, Recursive mode: {recursive}", debugMode, recursive);

            ILinkDictionary linkDictionary = app.GetRequiredService<ILinkDictionary>();
            linkDictionary.SetFormattedURL(filterURL);

            IParserThreadDirector threadDirector = app.GetRequiredService<IParserThreadDirector>();
            threadDirector.TargetThreads = threadsCount;
            logger.LogDebug("Thread director initialized");

            threadDirector.StartParse(URL, recursive);

            File.WriteAllText($"makesmoke_links_{timeStamp}.txt", ToJson(linkDictionary.GetDictionary()));
            File.WriteAllText($"makesmoke_errors_{timeStamp}.txt", ToJson(linkDictionary.GetErrors()));
            File.WriteAllText($"makesmoke_redirects_{timeStamp}.txt", ToJson(linkDictionary.GetRedirects()));
            logger.LogInformation("Parsing is ended.");
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

        private static string GetFormattedURL(string URL, string baseURL)
        {
            if (baseURL == String.Empty)
            {
                int pos = URL.IndexOf("#/");
                if (pos != -1)
                {
                    baseURL = URL.Substring(0, pos);
                }
                else baseURL = URL;
            }
            return baseURL;
        }

        public static string ToJson(Object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
