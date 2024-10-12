using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MakeSmoke.Interfaces
{
    public interface IParserThreadDirector
    {
        public ILogger _Logger { get; }

        public ILoggerFactory LogFactory { get; }

        public ILinkDictionary LinkDictionary { get; }

        public List<IParserAsync> Parsers { get; }
        public List<Task> ParserTasks { get; }

        public byte TargetThreads { get; set; }

        public void StartThreads();

        public void StartParse(string URLToParse, bool isRecursive);

        public void WaitForEnd();
    }
}
