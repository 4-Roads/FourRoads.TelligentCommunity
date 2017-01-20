using System;
using System.Collections.Generic;
using FourRoads.Common;

namespace FourRoads.TelligentCommunity.MetaData.Logic
{
    [Serializable]
    public class MetaData
    {
        public MetaData()
        {
            ExtendedMetaTags = new SerializableDictionary<string, string>();
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public SerializableDictionary<string,string> ExtendedMetaTags { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid ContainerId { get; set; }
        public bool InheritData { get; set; }
        public Guid ContainerTypeId { get; set; }
        public String GoogleTagHead { get; set; }
        public String GoogleTagBody { get; set; }
    }
}