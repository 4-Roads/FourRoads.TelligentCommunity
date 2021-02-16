using System;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Installer.Components.Utility
{
    [Serializable]
    public class TCException : Exception, IUserRenderableException, ILoggableException
    {
        private readonly Func<string> _getTranslatedMessage;

        public TCException(string internalMessage, Func<string> getTranslatedMessage = null) :
            base(internalMessage)
        {
            _getTranslatedMessage = getTranslatedMessage;
        }

        public TCException(string internalMessage, string resourceName, params string[] argv) :
            base(internalMessage)
        {
            _getTranslatedMessage += () => GetResourceMessage(resourceName, argv);
        }

        public TCException(string internalMessage, Exception ex, string resourceName=null, params string[] argv) :
            base(internalMessage, ex)
        {
            _getTranslatedMessage += () => GetResourceMessage(resourceName, argv);
        }

        protected ITranslatablePluginController PluginResourceController { get; set; }

        private string GetResourceMessage(string resourceName, string[] argv)
        {
            if (PluginResourceController != null)
            {
                return string.Format(PluginResourceController.GetLanguageResourceValue(resourceName), argv);
            }

            return "Translation controller not initialized:" + resourceName + ":" + String.Join(",",argv);
        }

        public TCException(string internalMessage, Func<string> getTranslatedMessage, Exception inner) :
            base(internalMessage, inner)
        {
            _getTranslatedMessage = getTranslatedMessage;
        }

        public string GetUserRenderableMessage()
        {
            if (_getTranslatedMessage != null)
            {
                return _getTranslatedMessage();
            }

            return "Internal Error Occured";
        }

        public string Category => "4 Roads";

        public void Log()
        {
            new Error(this);
        }
    }
}
