using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MakeSmoke.Utils
{
    internal static class DriverGenerator
    {
        public static IWebDriver GetChromeDriver()
        {
            var options = new ChromeOptions();
            options.SetLoggingPreference(LogType.Browser, OpenQA.Selenium.LogLevel.Severe);
            options.AddArgument("--disable-gpu");
            options.AddArgument("--headless");
            options.AddArgument("--disable-extensions");
            return new ChromeDriver(options);
        }
    }
}
