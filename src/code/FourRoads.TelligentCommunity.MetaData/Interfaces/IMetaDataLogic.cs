using System.Collections;
using FourRoads.TelligentCommunity.MetaData.Logic;

namespace FourRoads.TelligentCommunity.MetaData.Interfaces
{
    public interface IMetaDataLogic
    {
        void UpdateConfiguration(MetaDataConfiguration metaConfig);
        Logic.MetaData GetCurrentMetaData();
        string GetDynamicFormXml();
        string[] GetAvailableExtendedMetaTags();
        void SaveMetaDataConfiguration(string title, string description, string keywords,bool inherit, IDictionary extendedTags);
        bool CanEdit { get; }
        string FormatMetaString(string rawFieldValue, string seperator, IDictionary namedParameters);
        string GetBestImageUrlForCurrent();
    }
}