using FourRoads.TelligentCommunity.SiteAudit.Interfaces;
using FourRoads.TelligentCommunity.SiteAudit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Api.Content.Groups;
using Telligent.Evolution.Api.Content.Root;
using Telligent.Evolution.Api.Plugins.Administration.ContentFragmentManagement;
using Telligent.Evolution.Blogs.Internal.Constants;
using Telligent.Evolution.Blogs.Plugins;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Urls.Routing;

namespace FourRoads.TelligentCommunity.SiteAudit.Logic
{
    public class SiteAuditLogic : ISiteAuditLogic
    {
        private readonly IUsers _usersService;
        private readonly IThemeTypeService _themeTypeService;
        private readonly IContentFragmentPageService _pageService;
        private readonly IContentFragmentPageDataService _pageDateService;
        private readonly IContentFragmentPageSampleUrlService _sampleUrlService;
        private readonly IPageDefinitionManager _pageDefinitionManager;
        private readonly IContentFragmentManagementUiExtensionService _managementUiExtensionService;

        private static readonly string _pageName = "fr-site-audit";
        
        public SiteAuditLogic(IUsers usersService,
            IThemeTypeService themeTypeService,
            IContentFragmentPageService pageService,
            IContentFragmentPageDataService pageDataService,
            IContentFragmentPageSampleUrlService sampleUrlService,
            IPageDefinitionManager pageDefinitionManager,
            IContentFragmentManagementUiExtensionService managementUiExtensionService)
        {
            _usersService = usersService;
            _themeTypeService = themeTypeService;
            _pageService = pageService;
            _pageDateService = pageDataService;
            _sampleUrlService = sampleUrlService;
            _pageDefinitionManager = pageDefinitionManager;
            _managementUiExtensionService = managementUiExtensionService;
        }

        public void Initialize()
        {
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_pageName, _pageName, new FourRoads.Common.TelligentCommunity.Routing.SiteRootRouteConstraint(), null, _pageName, new PageDefinitionOptions
            {
                DefaultPageXml = LoadPageResourceXml("site-audit"),
                Validate = (context, accessController) =>
                {
                    if (_usersService.AccessingUser != null)
                    {
                        if (_usersService.AnonymousUserName == _usersService.AccessingUser.Username)
                        {
                            accessController.AccessDenied("This page is not available to you", false);
                        }
                    }
                }
            });
        }

        public IList<ThemePagesWidgets> GetPages(bool forceDefault)
        {
            var result = new List<ThemePagesWidgets>();
            var themeApplicationId = Guid.Empty;

            var themes = _themeTypeService.GetThemeTypes();

            foreach(var theme in themes)
            {
                if (theme is RootApplicationType)
                {
                    themeApplicationId = Telligent.Evolution.Api.Content.ContentTypes.RootApplication;
                }
                else if (theme is BlogApplicationType)
                {
                    themeApplicationId = BlogContentTypes.Blog;
                }
                else if (theme is GroupContainerType)
                {
                    themeApplicationId = Telligent.Evolution.Components.ContentTypes.Group;
                }
                else //(theme is Telligent.Evolution.Api.Content.Core.UserContentType)
                {
                    themeApplicationId = Telligent.Evolution.Components.ContentTypes.User;
                }

                var themePagesWidgets = new ThemePagesWidgets(theme);
                var themeId = theme.DefaultThemeId.ToString().Replace("-", "");

                var themePageDefinitions = new Dictionary<string, IPageDefinition>();
                
                foreach (var pageDefinition in _pageDefinitionManager.GetPageDefinitions(theme.ThemeTypeId))
                {
                    themePageDefinitions[pageDefinition.PageName] = pageDefinition;
                }

                if (!forceDefault)
                {
                    foreach (var page in _pageService.GetAll(theme.ThemeTypeId, themeApplicationId, themeId))
                    {
                        var themePage = new ThemePage(page, themePageDefinitions, themeApplicationId);
                        if (string.IsNullOrWhiteSpace(themePage.Url))
                        {
                            themePage.Url = _sampleUrlService.GetSampleUrl(theme.ThemeTypeId, page.Name, page.IsCustom, themeApplicationId);
                        }

                        themePagesWidgets.Pages.Add(themePage);
                    }
                }
                
                foreach (var page in _pageService.GetAllFactoryDefault(theme.ThemeTypeId, themeId))
                {
                    var themePage = new ThemePage(page, themePageDefinitions, themeApplicationId);
                    if (string.IsNullOrWhiteSpace(themePage.Url))
                    {
                        themePage.Url = _sampleUrlService.GetSampleUrl(theme.ThemeTypeId, page.Name, page.IsCustom, themeApplicationId);
                    }

                    themePagesWidgets.Pages.Add(themePage);
                }
                foreach (var page in _pageService.GetAllDefault(theme.ThemeTypeId, themeId))
                {
                    var themePage = new ThemePage(page, themePageDefinitions, themeApplicationId);
                    if (string.IsNullOrWhiteSpace(themePage.Url))
                    {
                        themePage.Url = _sampleUrlService.GetSampleUrl(theme.ThemeTypeId, page.Name, page.IsCustom, themeApplicationId);
                    }

                    themePagesWidgets.Pages.Add(themePage);
                }
                
                result.Add(themePagesWidgets);
            }
            
            return result;
        }

        public IList<Widget> ListWidgets(System.Collections.IDictionary options)
        {
            var fragmentList = _managementUiExtensionService.ListFragments(options);

            var providers = _managementUiExtensionService.ListProviders();

            var result = new List<Widget>();

            foreach (var fragment in fragmentList)
            {
                var defaultProvider = providers?.FirstOrDefault(p => p.Id == fragment.FactoryDefaultProvider);
                var widget = new Widget()
                {
                    Name = fragment.ProcessedName,
                    Description = fragment.ProcessedDescription,
                    FactoryDefaultProvider = fragment.FactoryDefaultProvider,
                    FactoryDefaultProviderName = defaultProvider?.Name
                };

                result.Add(widget);
            }


            return result.OrderBy(r => r.FactoryDefaultProviderName).ToList();
        }

        private Dictionary<string, string> defaultPageLayouts = null;

        private string LoadPageResourceXml(string name)
        {
            if (defaultPageLayouts == null)
            {
                defaultPageLayouts = new Dictionary<string, string>();

                var namespacePath = "FourRoads.TelligentCommunity.SiteAudit.Resources.PageLayouts";
                var resources = new EmbeddedResources();
                resources.EnumerateResources(namespacePath, ".xml", resourceName =>
                {
                    // Get the page content:
                    var pageContent = resources.GetString(resourceName);
                    var pageName = resourceName.Replace(".xml", "").Replace(namespacePath + ".", "");
                    defaultPageLayouts[pageName.ToLower()] = pageContent;
                });

            }

            string content;
            defaultPageLayouts.TryGetValue(name.ToLower(), out content);
            return content;
        }
    }
}
