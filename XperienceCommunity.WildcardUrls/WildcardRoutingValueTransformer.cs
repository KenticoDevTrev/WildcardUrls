using CMS.Base;
using CMS.Base.Internal;
using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing.Internal;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace XperienceCommunity.WildcardUrls
{
    public class WildcardRoutingValueTransformer : DynamicRouteValueTransformer
    {
        private IPageDataContextRetriever _pageDataContextRetriever;
        private IProgressiveCache _progressiveCache;
        private IPageDataContextInitializer _pageDataContextInitializer;
        private readonly IAlternativeUrlInfoProvider _alternativeUrlInfoProvider;
        private readonly IWildcardRegisterPageRouteRetriever _wildcardRegisterPageRouteRetriever;
        private readonly ISiteService _siteService;
        private readonly IEventLogService _eventLogService;

        public WildcardRoutingValueTransformer(IPageDataContextRetriever pageDataContextRetriever,
            IProgressiveCache progressiveCache,
            IPageDataContextInitializer pageDataContextInitializer,
            IAlternativeUrlInfoProvider alternativeUrlInfoProvider,
            IWildcardRegisterPageRouteRetriever wildcardRegisterPageRouteRetriever,
            ISiteService siteService,
            IEventLogService eventLogService)
        {
            _pageDataContextRetriever = pageDataContextRetriever;
            _progressiveCache = progressiveCache;
            _pageDataContextInitializer = pageDataContextInitializer;
            _alternativeUrlInfoProvider = alternativeUrlInfoProvider;
            _wildcardRegisterPageRouteRetriever = wildcardRegisterPageRouteRetriever;
            _siteService = siteService;
            _eventLogService = eventLogService;
        }

        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            try
            {
                if (httpContext.Kentico().PageBuilder().EditMode)
                {
                    return values;
                }

                if (!_pageDataContextRetriever.TryRetrieve<TreeNode>(out var dataContext))
                {

                    // Look for alternative urls with {} that start with the current request
                    var wildcardRoutes = await _progressiveCache.LoadAsync(cs =>
                    {

                        if (cs.Cached)
                        {
                            cs.CacheDependency = CacheHelper.GetCacheDependency($"node|{_siteService.CurrentSite.SiteName}|/|childnodes");
                        }
                        var altUrlsWithMacros = _alternativeUrlInfoProvider.Get()
                        .Source(x => x.Join(new CMS.DataEngine.QuerySourceTable("CMS_Document"), nameof(AlternativeUrlInfo.AlternativeUrlDocumentID), nameof(TreeNode.DocumentID)))
                        .WhereLike(nameof(AlternativeUrlInfo.AlternativeUrlUrl), "%{%}%")
                        .OnSite(_siteService.CurrentSite.SiteName)
                        .Result;


                        var routes = new List<WildcardRoute>();

                        foreach (DataRow altUrlRow in altUrlsWithMacros.Tables[0].Rows)
                        {
                            var altUrl = new AlternativeUrlInfo(altUrlRow);
                            var route = new WildcardRoute()
                            {
                                DocumentID = altUrl.AlternativeUrlDocumentID,
                            };
                            string url = "/" + altUrl.AlternativeUrlUrl.NormalizedUrl.Trim('~').Trim('/');
                            string baseUrl = url.Substring(0, url.IndexOf('{') - 1);
                            var wildcards = GetWildcardSections(url, baseUrl);
                            route.BaseRelativeUrl = baseUrl;

                            // Handle controller/action specifically in sections
                            var sections = new List<string>();

                            for (int k = 0; k < wildcards.Count; k++)
                            {
                                if (wildcards[k].StartsWith("controller=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    route.Controller = wildcards[k].Substring(11);
                                }
                                else if (wildcards[k].StartsWith("action=", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    route.Action = wildcards[k].Substring(7);
                                }
                                else
                                {
                                    route.WildcardsKeys.Add(wildcards[k]);
                                }
                            }

                            routes.Add(route);
                        }
                        return Task.FromResult(routes.GroupBy(x => x.BaseRelativeUrl)
                            .ToDictionary(key => key.Key, value => value.ToList()));

                    }, new CacheSettings(1440, "GetWildcardRoutes"));

                    string baseRequestUrl = httpContext.Request.Path.Value;
                    var matchingRoutes = wildcardRoutes.Where(x => (baseRequestUrl + "/").StartsWith((x.Key + "/"), StringComparison.InvariantCultureIgnoreCase));
                    if (matchingRoutes.Any())
                    {
                        var matchingRouteDictionary = matchingRoutes.FirstOrDefault();

                        // must match on number of sections
                        var requestWildcardSections = GetWildcardSections(baseRequestUrl, matchingRouteDictionary.Key);
                        var match = matchingRouteDictionary.Value.Where(x => x.WildcardsKeys.Count() == requestWildcardSections.Count()).FirstOrDefault();

                        if (match != null)
                        {
                            // Initialize page biulder
                            _pageDataContextInitializer.Initialize(match.DocumentID);
                            var page = _pageDataContextRetriever.Retrieve<TreeNode>().Page;

                            values ??= new RouteValueDictionary();

                            // Add wildcard values to route
                            for (int k = 0; k < match.WildcardsKeys.Count; k++)
                            {
                                values.AddOrUpdate(match.WildcardsKeys[k], requestWildcardSections[k]);
                            }

                            // Handle routing either to wildcard controller/action, to a RegisterPageRouteAttribute controller/action, or default Kentico page builder routing
                            bool controllerActionSet = false;

                            // Wildcard controller/action
                            if (!controllerActionSet && !string.IsNullOrWhiteSpace(match.Controller))
                            {
                                values.AddOrUpdate("controller", match.Controller);
                                values.AddOrUpdate("action", !string.IsNullOrWhiteSpace(match.Action) ? match.Action : "Index");
                                controllerActionSet = true;
                            }

                            // RegisterPageRouteAttribute routing
                            if (!controllerActionSet)
                            {
                                var attributes = _wildcardRegisterPageRouteRetriever.GetRegisterPageRouteAttributes();
                                var matchingAttributes = attributes.Where(x => x.ClassName.Equals(page.ClassName) && (string.IsNullOrWhiteSpace(x.Path) || page.NodeAliasPath.StartsWith(x.Path.Replace("%", ""), StringComparison.InvariantCultureIgnoreCase)));
                                var bestMatch = matchingAttributes.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.Path) ? x.Path.Length : 0).FirstOrDefault();
                                if (bestMatch != null)
                                {
                                    string controllerName = bestMatch.MarkedType.Name;
                                    controllerName = controllerName.Substring(0, controllerName.Length - ("Controller".Length));
                                    values.AddOrUpdate("controller", controllerName);
                                    values.AddOrUpdate("action", !string.IsNullOrWhiteSpace(bestMatch.ActionName) ? bestMatch.ActionName : "Index");
                                    controllerActionSet = true;
                                }
                            }

                            // Normal Kentico page builder handling, this handles page templates too
                            if (!controllerActionSet)
                            {
                                values.AddOrUpdate("controller", "KenticoPageBuilderPage");
                                values.AddOrUpdate("action", "Index");
                            }

                            return values;
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // ignore this, usually on page builder request
            }
            catch (Exception ex)
            {
                _eventLogService.LogException("WildcardUrl", "Error Parsing", ex, additionalMessage: "For " + httpContext.Request.Path.Value);
            }
            return values;
        }

        private List<string> GetWildcardSections(string url, string baseUrl)
        {
            return url.Substring(baseUrl.Length)
                           .Trim('/')
                           .Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                           .Select(x => x.Replace("{", "").Replace("}", "").Trim()).ToList();
        }
    }
}
