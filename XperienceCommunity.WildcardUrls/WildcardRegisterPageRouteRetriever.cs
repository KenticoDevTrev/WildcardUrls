using CMS.Core;
using Kentico.Content.Web.Mvc.Routing;
using System.Collections.Generic;
using System.Reflection;
namespace XperienceCommunity.WildcardUrls.Internal
{
    public class WildcardRegisterPageRouteRetriever : IWildcardRegisterPageRouteRetriever
    {
        public WildcardRegisterPageRouteRetriever()
        {
            var attributes = new List<RegisterPageRouteAttribute>();
            // Find filters that apply
            foreach (var assembly in AssemblyDiscoveryHelper.GetAssemblies(true))
            {
                attributes.AddRange(assembly.GetCustomAttributes<RegisterPageRouteAttribute>());
            }
            Attributes = attributes;
        }

        public List<RegisterPageRouteAttribute> Attributes { get; private set; }

        public IEnumerable<RegisterPageRouteAttribute> GetRegisterPageRouteAttributes()
        {
            return Attributes;
        }
    }
}
