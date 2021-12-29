using System.Collections.Generic;

namespace XperienceCommunity.WildcardUrls
{
    public class WildcardRoute
    {
        public int DocumentID { get; set; }
        public string BaseRelativeUrl { get; set; }
        public List<string> WildcardsKeys { get; set; } = new List<string>();
        public string Controller { get; internal set; }
        public string Action { get; internal set; }
    }
}
