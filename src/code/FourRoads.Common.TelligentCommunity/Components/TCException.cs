using System;
using System.Collections;
using FourRoads.Common.TelligentCommunity.Plugins.Base;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Exceptions.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Platform.Logging;
using ExceptionCategories = Telligent.Evolution.Extensibility.Exceptions.Version1.ExceptionCategories;

namespace FourRoads.Common.TelligentCommunity.Components
{
    [Serializable]
    public class TCException : Exception, IUserRenderableException, IExceptions
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

        private string _errorText = "Logged error";
        public void Log()
        {
            ExceptionHelper.Handle(this, _errorText, ExceptionCategories.UnknownError);
        }

        public AdditionalInfo Log(Exception exception)
        {
            ExceptionHelper.Handle(exception, _errorText, ExceptionCategories.UnknownError);
            return new AdditionalInfo();
        }

        public AdditionalInfo Log(Exception exception,
            [Documentation(Name = "CategoryId", Type = typeof(Guid), Description = "The exception category Id")]
            IDictionary options)
        {
            if (options["CategoryId"] != null && Guid.TryParse(options["CategoryId"].ToString(), out var exCategory))
            {
                ExceptionHelper.Handle(exception, _errorText, exCategory);
            }
            else
            {
                ExceptionHelper.Handle(exception, _errorText, ExceptionCategories.UnknownError);
            }

            return new AdditionalInfo();
        }
    }
}
