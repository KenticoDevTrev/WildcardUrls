using Kentico.Content.Web.Mvc.Routing;
using System.Collections.Generic;

namespace XperienceCommunity.WildcardUrls
{
    public interface IWildcardRegisterPageRouteRetriever
    {
        IEnumerable<RegisterPageRouteAttribute> GetRegisterPageRouteAttributes();
    }
}
