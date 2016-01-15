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
    }
}