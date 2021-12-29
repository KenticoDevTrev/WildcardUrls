﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.WildcardUrls.Internal;

namespace XperienceCommunity.WildcardUrls
{
    public static class WildcardRoutingExtension
    {

        /// <summary>
        /// Adds the wildcard routing to the service collection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddWildcardPageBuilderRouting(this IServiceCollection services)
        {
            return services.AddSingleton<IWildcardRegisterPageRouteRetriever, WildcardRegisterPageRouteRetriever>()
                .AddSingleton<WildcardRoutingValueTransformer>();
        }

        /// <summary>
        /// Uses Wildcard page builder routing, should be applied at the end of your endpoint list.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IEndpointRouteBuilder UseWildcardPageBuilderRouting(this IEndpointRouteBuilder builder)
        {
            builder.MapDynamicControllerRoute<WildcardRoutingValueTransformer>("{**pagepath}");
            return builder;
        }
    }
}
