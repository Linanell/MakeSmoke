using MakeSmoke.Data;
using MakeSmoke.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;

namespace MakeSmoke
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string URL = String.Empty;
            string baseURL = String.Empty;
            bool debugMode = false;
            bool recursive = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-d"))
                {
                    recursive = true;
                }
                else if (args[i].Equals("-d"))
                {
                    debugMode = true;
                }
                else if (args[i].StartsWith("--base="))
                {
                    baseURL = args[i].Substring(7);
                } else if (args[i].StartsWith("-"))
                {
                    Console.WriteLine($"Unknown argument: {args[i]}");
                }
                else
                {
                    URL = args[i];
                }
            }

            //string URL = "https://www.dunlop.eu/en_gb/motorcycle#/";
            //string baseURL = String.Empty;
            //bool debugMode = false;
            //bool recursive = true;

            if (URL == String.Empty)
            {
                Console.WriteLine("No URL given for smoke. Ending program.");
                return;
            }

            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string logFileName = $"makesmoke_log_{timeStamp}.txt";
            
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(builder => GetLoggerOptions(builder, logFileName, debugMode));
            services.AddSingleton<IWebDriver>(GetChromeDriver());
            services.AddTransient<IParser, Parser>();
            var app = services.BuildServiceProvider();

            ILoggerFactory loggerFactory = app.GetRequiredService<ILoggerFactory>();
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            logger.LogDebug("Logger initialized");
            logger.LogInformation("Debug mode: {debug}, Recursive mode: {recursive}", debugMode, recursive);

            IParser parser = app.GetRequiredService<IParser>();
            logger.LogDebug("Parser initialized");
            parser.Parse(URL, baseURL, recursive);
            logger.LogInformation("Parsing is ended.");
        }

        private static IWebDriver GetChromeDriver()
        {
            var options = new ChromeOptions();
            options.SetLoggingPreference(LogType.Browser, OpenQA.Selenium.LogLevel.Severe);
            return new ChromeDriver(options);
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
    }
}
