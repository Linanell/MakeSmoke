using MakeSmoke.Enums;

namespace MakeSmoke.Models
{
    public class LinkData
    {
        public LinkType Type { get; set; }
        public bool Checked { get; set; }

        public LinkData(LinkType type) {
            Type = type;
            Checked = false;
        }
    }
}
