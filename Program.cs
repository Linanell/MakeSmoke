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
            string URL = "https://www.dunlop.eu/en_gb/motorcycle";
            bool debugMode = true;
            bool recursive = true;
            string logFileName = $"makesmoke_log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt";
            
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(builder => GetLoggerOptions(builder, logFileName, debugMode));
            services.AddSingleton<IWebDriver>(GetChromeDriver());
            services.AddTransient<IParser, Parser>();
            var app = services.BuildServiceProvider();

            ILoggerFactory loggerFactory = app.GetRequiredService<ILoggerFactory>();
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            logger.LogDebug("Logger initialized");
            logger.LogInformation("URL to parse {URL}, Debug mode: {debug}, Recursive mode: {recursive}", URL, debugMode, recursive);

            IParser parser = app.GetRequiredService<IParser>();
            logger.LogDebug("Parser initialized");
            parser.Parse(URL, recursive);
            logger.LogInformation("Parsing is ended.");
        }

        private static IWebDriver GetChromeDriver()
        {
            var options = new ChromeOptions();


            return new ChromeDriver(options);
        }

        private static void GetLoggerOptions(ILoggingBuilder builder, string logFileName, bool debugMode = false) {
            builder.AddConsole();
            builder.AddDebug();
            builder.AddFile(logFileName, append: true);
            if (debugMode)
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            }
        }
    }
}
