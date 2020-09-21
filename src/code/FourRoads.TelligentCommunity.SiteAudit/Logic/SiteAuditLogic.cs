using FourRoads.TelligentCommunity.SiteAudit.Interfaces;
using FourRoads.TelligentCommunity.SiteAudit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Api.Content.Groups;
using Telligent.Evolution.Api.Content.Root;
using Telligent.Evolution.Api.Plugins.Administration.ContentFragmentManagement;
using Telligent.Evolution.Blogs.Internal;
using Telligent.Evolution.Blogs.Internal.Constants;
using Telligent.Evolution.Blogs.Plugins;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.MediaGalleries.Internal;
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
        private readonly IGroupService _groupService;
        private readonly IGalleryService _galleryService;
        private readonly IBlogService _blogService;
        private readonly IContentFragmentManagementUiExtensionService _managementUiExtensionService;

        private static readonly string _pageName = "fr-site-audit";
        
        public SiteAuditLogic(IUsers usersService,
            IThemeTypeService themeTypeService,
            IContentFragmentPageService pageService,
            IContentFragmentPageDataService pageDataService,
            IContentFragmentPageSampleUrlService sampleUrlService,
            IPageDefinitionManager pageDefinitionManager,
            IGroupService groupService,
            IGalleryService galleryService,
            IBlogService blogService,
            IContentFragmentManagementUiExtensionService managementUiExtensionService)
        {
            _usersService = usersService;
            _themeTypeService = themeTypeService;
            _pageService = pageService;
            _pageDateService = pageDataService;
            _sampleUrlService = sampleUrlService;
            _pageDefinitionManager = pageDefinitionManager;
            _groupService = groupService;
            _galleryService = galleryService;
            _blogService = blogService;
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
                var themePagesWidgets = new ThemePagesWidgets(theme);
                var themePageDefinitions = new Dictionary<string, IPageDefinition>();

                foreach (var pageDefinition in _pageDefinitionManager.GetPageDefinitions(theme.ThemeTypeId))
                {
                    themePageDefinitions[pageDefinition.PageName] = pageDefinition;
                }

                if (theme is RootApplicationType)
                {
                    themeApplicationId = Telligent.Evolution.Api.Content.ContentTypes.RootApplication;
                }
                else if (theme is BlogApplicationType)
                {
                    themeApplicationId = BlogContentTypes.Blog;

                    // get blogs
                    var blogsPage = _blogService.GetBlogs(new BlogsListOptions()
                    {
                        IncludeSubGroups = true,
                        PageIndex = 0,
                        PageSize = 1
                    });

                    var blogs = _blogService.GetBlogs(new BlogsListOptions()
                    {
                        IncludeSubGroups = true,
                        PageIndex = 0,
                        PageSize = blogsPage.TotalCount
                    });

                    foreach (var blog in blogs)
                    {
                        var blogThemeId = theme.GetThemeId(blog.ApplicationId);

                        if (blogThemeId != null)
                        {
                            var themeName = blogThemeId.ToString().Replace("-", "");
                            var groupPage = _pageService.GetAll(theme.ThemeTypeId, blog.ApplicationId, themeName).FirstOrDefault();
                            if (groupPage != null)
                            {
                                var themePage = new ThemePage(groupPage, themePageDefinitions, blog.ApplicationId);
                                themePage.Name = blog.Name;
                                themePage.Description = blog.Description;

                                themePagesWidgets.Pages.Add(themePage);
                            }
                        }
                    }
                }
                else if (theme is GroupContainerType)
                {
                    themeApplicationId = Telligent.Evolution.Components.ContentTypes.Group;

                    // get groups
                    var groupsPage = _groupService.GetGroups(new GroupQuery()
                    {
                        IncludeParentGroup = true,
                        IncludeAllSubGroups = true,
                        PageIndex = 0,
                        PageSize = 1
                    });

                    var groups = _groupService.GetGroups(new GroupQuery()
                    {
                        IncludeParentGroup = true,
                        IncludeAllSubGroups = true,
                        PageIndex = 0,
                        PageSize = groupsPage.TotalItems
                    });

                    foreach (var group in groups)
                    {
                        var groupThemeId = theme.GetThemeId(group.ApplicationId);
                        
                        if (groupThemeId != null)
                        {
                            var themeName = groupThemeId.ToString().Replace("-", "");
                            var groupPage = _pageService.GetAll(theme.ThemeTypeId, group.ApplicationId, themeName).FirstOrDefault();
                            if (groupPage != null)
                            {
                                var themePage = new ThemePage(groupPage, themePageDefinitions, group.ApplicationId);
                                themePage.Name = group.Name;
                                themePage.Description = group.Description;

                                themePagesWidgets.Pages.Add(themePage);
                            }
                        }
                    }

                    // get galleries
                    var galleriesPage = _galleryService.GetGalleries(new GalleriesListOptions()
                    {
                        IncludeSubGroups = true,
                        PageIndex = 0,
                        PageSize = 1
                    });

                    var galleries = _galleryService.GetGalleries(new GalleriesListOptions()
                    {
                        IncludeSubGroups = true,
                        PageIndex = 0,
                        PageSize = galleriesPage.TotalCount
                    });

                    foreach (var gallery in galleries)
                    {
                        var galleryThemeId = gallery.Group?.ThemeId;

                        if (galleryThemeId != null)
                        {
                            var themeName = galleryThemeId.ToString().Replace("-", "");
                            var groupPage = _pageService.GetAll(theme.ThemeTypeId, gallery.ApplicationId, themeName).FirstOrDefault();
                            if (groupPage != null)
                            {
                                var themePage = new ThemePage(groupPage, themePageDefinitions, gallery.ApplicationId);
                                themePage.Name = gallery.Name;
                                themePage.Description = gallery.Description;

                                themePagesWidgets.Pages.Add(themePage);
                            }
                        }
                    }

                    
                }
                else //(theme is Telligent.Evolution.Api.Content.Core.UserContentType)
                {
                    themeApplicationId = Telligent.Evolution.Components.ContentTypes.User;
                }

                var themeId = theme.DefaultThemeId.ToString().Replace("-", "");
                
                if (!forceDefault)
                {
                    var allPages = _pageService.GetAll(theme.ThemeTypeId, themeApplicationId, themeId);
                    foreach (var page in allPages)
                    {
                        var themePage = new ThemePage(page, themePageDefinitions, themeApplicationId);
                        if (string.IsNullOrWhiteSpace(themePage.Url))
                        {
                            themePage.Url = _sampleUrlService.GetSampleUrl(theme.ThemeTypeId, page.Name, page.IsCustom, themeApplicationId);
                        }

                        themePagesWidgets.Pages.Add(themePage);
                    }
                }

                var factoryDefault = _pageService.GetAllFactoryDefault(theme.ThemeTypeId, themeId);
                foreach (var page in factoryDefault)
                {
                    var themePage = new ThemePage(page, themePageDefinitions, themeApplicationId);
                    if (string.IsNullOrWhiteSpace(themePage.Url))
                    {
                        themePage.Url = _sampleUrlService.GetSampleUrl(theme.ThemeTypeId, page.Name, page.IsCustom, themeApplicationId);
                    }

                    themePagesWidgets.Pages.Add(themePage);
                }

                var allDefault = _pageService.GetAllDefault(theme.ThemeTypeId, themeId);
                foreach (var page in allDefault)
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
            var themePageWidgets = GetPages(false);

            var result = new List<Widget>();

            foreach (var fragment in fragmentList)
            {
                var defaultProvider = providers?.FirstOrDefault(p => p.Id == fragment.FactoryDefaultProvider);
                var widget = new Widget()
                {
                    InstanceIdentifier = fragment.InstanceIdentifier.ToString(),
                    Name = fragment.ProcessedName,
                    Description = fragment.ProcessedDescription,
                    FactoryDefaultProvider = fragment.FactoryDefaultProvider,
                    FactoryDefaultProviderName = defaultProvider?.Name ?? "-"
                };

                var instanceIdentifier = fragment.InstanceIdentifier.ToString().Replace("-", "");

                foreach(var themePage in themePageWidgets)
                {
                    var matchingPages = themePage.Pages.Where(p =>
                        p.Widgets.Any(w => !string.IsNullOrEmpty(w.InstanceIdentifier) && w.InstanceIdentifier.Equals(instanceIdentifier, StringComparison.InvariantCultureIgnoreCase))
                    ).ToList();

                    foreach(var page in matchingPages)
                    {
                        if (!widget.Pages.Any(w => w.Id == page.Id))
                        {
                            widget.Pages.Add(page);
                        }
                    }
                }

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
