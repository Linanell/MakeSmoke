﻿using MakeSmoke.Data;
using MakeSmoke.Interfaces;
using static MakeSmoke.Utils.Constants;
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
            byte threadsCount = 0;
            string logFileName = String.Empty;

            try
            {
                Configuration settings = ConfigurationLoader.LoadConfiguration();
                if (settings != null)
                {
                    if (settings.URL != String.Empty) URL = settings.URL;
                    if (settings.FilterURL != String.Empty) filterURL = settings.FilterURL;
                    if (settings.DebugMode) debugMode = true;
                    if (settings.Recursive) recursive = true;
                    if (settings.ThreadsCount != 0) threadsCount = settings.ThreadsCount;
                    if (settings.LogFileName != String.Empty) logFileName = settings.LogFileName;
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
                else if (args[i].StartsWith("-"))
                {
                    Console.WriteLine($"Unknown argument: {args[i]}");
                }
                else
                {
                    URL = args[i];
                }
            }

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

            filterURL = GetFormattedURL(URL, filterURL);

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
            linkDictionary.SetFormattedURL(filterURL);

            IParserThreadDirector threadDirector = app.GetRequiredService<IParserThreadDirector>();
            threadDirector.TargetThreads = threadsCount;
            logger.LogDebug("Thread director initialized");

            threadDirector.StartParse(URL, recursive);

            File.WriteAllText($"{logFileName}_links_{timeStamp}.txt", ToJson(linkDictionary.Dictionary));
            File.WriteAllText($"{logFileName}_errors_{timeStamp}.txt", ToJson(linkDictionary.Errors));
            File.WriteAllText($"{logFileName}_redirects_{timeStamp}.txt", ToJson(linkDictionary.Redirects));
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
