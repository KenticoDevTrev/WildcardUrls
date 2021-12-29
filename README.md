# WildcardUrls
Adds Wildcard Url support for Kentico Xperience 13 (.net core).  This was available in [portal engine](https://docs.xperience.io/k12sp/configuring-kentico/configuring-page-urls/wildcard-urls) and in Kentico Xperience 12 MVC through the [RM.DynamicRoutingWildcards.Kentico.MVC](https://www.nuget.org/packages/RM.DynamicRoutingWildcards.Kentico.MVC/) package.

# Usage
You can add Alternate Urls to any Page (that uses Pages/Urls) within Kentico Xperience, and use any number of /{parameterName} at the end of your Url.  Pages that match these patterns will be routed to your given page (with the page builder context initialized) and the parameters added to the RouteValues.

Example:
If you have a page that is `/Locations`, you can add an Alternate Url of `/Locations/{state}/{city}` and both `/Locations` and `/Locations/Wisconsin/Green-Bay` will be routed to your Locations page.

# Requirements
Must be on Kentico Xperience 13 (.net core), hotfix 5 minimum (.net 5 support)

# Installation
1. Install the `XperienceCommunity.WildcardUrls` nuget package on the MVC Site
2. In your startup, add Wildcard Urls `services.AddWildcardUrls()` (XperienceCommunity.WildcardUrls namespace)
3. In your route endpoint configuration, use Wildcard Urls at the end of your endpoint configuration list:
``` csharp
app.UseEndpoints(endpoints =>
            {
                endpoints.Kentico().MapRoutes();

                endpoints.MapControllers();

                ...

                endpoints.UseAddWildcardUrls();
            });
```
4. In your Kentico Xperience Admin, go to Settings -> URLs and SEO -> Alternative URLs -> Alternative URLs Constraint and allow {} and optionally = with this regex: `^[\w\-\/\{\}\=]+$`

## defalut routing
By default, the wild card routing will honor all of Kentico Xperiences's basic methods of routing.
* Default View (~/Views/Shared/PageTypes/My_Class.cshtml)
* Controller/Action routing through `RegisterPageRoute` assembly attribute (with Path support)
* Page Templates through the `RegisterPageTemplate` assembly attribute

## Controller / action routing
Optionally, in the alternative Url you can specify a Controller and/or Action for your routing by adding the wildcard parameters {controller=ControllerName} and/or {action=ActionName}.

Example:
`/Locations/{state}/{controller=Location}/{action=LookupByState}`
`/Locations/{state}/{city}/{controller=Location}/{action=LookupByCity}`

***Caution using this***
In many cases you should probably just use standard MVC routing for advanced situations like this.  If you need page builder widget zones, you can either manually set the Page Context (`IPageDataContextInitializer.Initialize`), leverage a [Partial Widget Page](https://github.com/KenticoDevTrev/PartialWidgetPage) to render widget zones based on another page's context, or use [Shared Widget Pages](https://dev.to/seangwright/kentico-ems-mvc-widget-experiments-part-4-shared-widget-pages-2476)
