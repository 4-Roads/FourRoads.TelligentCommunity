using System;
using System.Collections.Generic;
using Telligent.Evolution.Components;

namespace FourRoads.TelligentCommunity.SiteAudit.Models
{
    public class Widget
    {
        public int Id { get; set; }

        public string InstanceIdentifier { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string RegionName { get; }

        public string CssClassAddition { get; set; }

        public int OrderNumber { get; set; }

        public Guid? FactoryDefaultProvider { get; set; }
        public string FactoryDefaultProviderName { get; set; }

        public string ThemeTitle { get; set; }

        public List<ThemePage> Pages { get; set; }

        public Widget()
        {
            Pages = new List<ThemePage>();
        }

        public Widget(ConfiguredContentFragment configuredContentFragment)
        {
            if (configuredContentFragment != null)
            {
                Id = configuredContentFragment.ID;
                Name = configuredContentFragment.ContentFragment?.FragmentName;
                Description = configuredContentFragment.ContentFragment?.FragmentDescription;
                RegionName = configuredContentFragment.RegionName;
                CssClassAddition = configuredContentFragment.CssClassAddition;
                OrderNumber = configuredContentFragment.OrderNumber;

                if(configuredContentFragment.ContentFragment is Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment)
                {
                    var instanceIdentifier = ((Telligent.Evolution.ScriptedContentFragments.ScriptedContentFragment)configuredContentFragment.ContentFragment)?.InstanceIdentifier;
                    InstanceIdentifier = instanceIdentifier.Replace("-", "");
                }
            }
        }
    }
}
