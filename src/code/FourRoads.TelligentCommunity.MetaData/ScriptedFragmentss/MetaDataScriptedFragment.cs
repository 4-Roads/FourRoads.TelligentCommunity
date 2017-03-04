using System;
using System.Collections;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.MetaData.Api;
using FourRoads.TelligentCommunity.MetaData.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.MetaData.ScriptedFragmentss
{
    public class MetaDataScriptedFragment : IMetaDataScriptedFragment
    {
        private IMetaDataLogic _metaDataLogic;

        public MetaDataScriptedFragment(IMetaDataLogic metaDataLogic)
        {
            _metaDataLogic = metaDataLogic;
        }

        protected IMetaDataLogic MetaDataLogic
        {
            get
            {
                return _metaDataLogic;
            }
        }

        public bool CanEdit
        {
            get { return MetaDataLogic.CanEdit; }
        }

        public string[] GetAvailableExtendedMetaTags()
        {
            return MetaDataLogic.GetAvailableExtendedMetaTags();
        }

        public string GetDynamicFormXml()
        {
            return MetaDataLogic.GetDynamicFormXml();
        }

        public string GetBestImageUrlForCurrent()
        {
            return MetaDataLogic.GetBestImageUrlForCurrent();
        }

        public string GetBestImageUrlForContent(Guid contentId, Guid contentTypeId)
        {
            return MetaDataLogic.GetBestImageUrlForContent(contentId, contentTypeId);
        }

        public string SaveMetaDataConfiguration(string title, string description, string keywords, bool ignore , IDictionary extendedTags )
        {
            try
            {
                MetaDataLogic.SaveMetaDataConfiguration(title, description, keywords, ignore, extendedTags);
            }
            catch (Exception ex)
            {
                new TCException("Save Failed" , ex).Log();

                return "Save Meta Data failed";
            }

            return string.Empty;
        }

        public string FormatMetaString(string rawFieldValue, string seperator, IDictionary namedParameters)
        {
            return MetaDataLogic.FormatMetaString(rawFieldValue, seperator, namedParameters);
        }

        public ApiMetaData GetCurrentMetaData()
        {
            Logic.MetaData metaData;

            try
            {
                metaData = MetaDataLogic.GetCurrentMetaData();

                if (metaData == null)
                    return null;
            }
            catch (Exception ex)
            {
                return new ApiMetaData(new AdditionalInfo(new Error("Exception", ex.Message)));
            }

            return new ApiMetaData(metaData);
        }
    }
}
