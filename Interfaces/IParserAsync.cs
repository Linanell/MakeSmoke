using OpenQA.Selenium;
using System.Threading.Tasks;

namespace MakeSmoke.Interfaces
{
    public interface IParserAsync
    {
        public Task ParseAsync(IWebDriver driver, string URL, bool isRecursive);
    }
}
