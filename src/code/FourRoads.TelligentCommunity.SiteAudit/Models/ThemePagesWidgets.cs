using System;
using System.Collections.Generic;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.SiteAudit.Models
{
    public class ThemePagesWidgets
    {
        public Guid ThemeTypeId { get; set; }

        public string ThemeTypeName { get; set; }

        public string ThemeTypeDescription { get; set; }

        public Guid DefaultThemeId { get; }

        public List<ThemePage> Pages { get; set; }

        public ThemePagesWidgets()
        {
            Pages = new List<ThemePage>();
        }

        public ThemePagesWidgets(IThemeableApplicationType themeableApplicationType)
        {
            ThemeTypeId = themeableApplicationType.ThemeTypeId;
            ThemeTypeName = themeableApplicationType.ThemeTypeName;
            ThemeTypeDescription = themeableApplicationType.ThemeTypeDescription;
            DefaultThemeId = themeableApplicationType.DefaultThemeId;
            Pages = new List<ThemePage>();
        }
    } 
}
