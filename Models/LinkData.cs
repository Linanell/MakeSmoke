using MakeSmoke.Enums;
using System.Collections.Generic;

namespace MakeSmoke.Models
{
    public class LinkData
    {
        public LinkType Type { get; set; }
        public bool Checked { get; set; } = false;
        public List<string> WhereFound { get; set; } = new List<string>();

        public LinkData(LinkType type, string whereFound) {
            Type = type;
            WhereFound.Add(whereFound);
        }
    }
}
