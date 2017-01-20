using System.Collections.Generic;

namespace FourRoads.TelligentCommunity.MetaData.Logic
{
    public class MetaDataConfiguration
    {
        public MetaDataConfiguration()
        {
            //Default extended entries
            ExtendedEntries = new List<string>();
        }

        public List<string> ExtendedEntries;
        public string GoogleTagHead;
        public string GoogleTagBody;
    }
}