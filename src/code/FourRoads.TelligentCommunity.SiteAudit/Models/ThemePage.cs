using System;
using System.Collections.Generic;
using Telligent.Evolution.Components;
using Telligent.Evolution.Urls.Routing;

namespace FourRoads.TelligentCommunity.SiteAudit.Models
{
    public class ThemePage
    {
        public int Id { get; set; }

        public Guid ContentId { get; set; }

        public string ContainerName { get; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public bool WasImported { get; set; }

        public DateTime LastModified { get; set; }

        public Guid ThemeContextID { get; }
        public Guid ThemeTypeID { get; }

        public LayoutType Layout { get; set; }

        public bool IsCustom { get; set; }

        string CustomPageName { get; set; }
        string CustomPageTitle { get; set; }
        string CustomPageDescription { get; set; }

        public List<Widget> Widgets { get; set; }

        public ThemePage()
        {
            Widgets = new List<Widget>();
        }

        public ThemePage(ContentFragmentPage page, Dictionary<string, IPageDefinition> themePageDefinitions, Guid themeApplicationId)
        {
            if (page != null)
            {
                IPageDefinition pageDefinition = null;
                if (themePageDefinitions.TryGetValue(page.Name, out pageDefinition))
                {
                    Url = pageDefinition.GetSampleUrl(themeApplicationId);
                }

                Id = page.ID;
                ContentId = page.ContentId;
                ContainerName = page.ContainerName;
                Name = pageDefinition?.PageName ?? page.Name;
                Title = pageDefinition?.PageTitle ?? page.Title;
                Description = pageDefinition?.Description;
                IsCustom = page.IsCustom;
                WasImported = page.WasImported;
                LastModified = page.LastModified;
                ThemeContextID = page.ThemeContextID;
                ThemeTypeID = page.ThemeTypeID;
                Layout = page.Layout;

                Widgets = new List<Widget>();

                var widgets = page.GetAllConfiguredContentFragments();

                if (widgets != null)
                {
                    foreach (var widget in widgets)
                    {
                        Widgets.Add(new Widget(widget));
                    }
                }
            }
        }
    }
}
