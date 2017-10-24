using System;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Platform.Logging;

namespace FourRoads.Common.TelligentCommunity.Components
{
    // ReSharper disable once InconsistentNaming
    public class TCException : Exception, IUserRenderableException, ILoggableException
    {
        private readonly Func<string> _getTranslatedMessage;

        public TCException(string internalMessage, Exception inner) :
         this(internalMessage , null , inner)
        {
        }

        public TCException(string internalMessage, Func<string> getTranslatedMessage = null):
            base(internalMessage)
        {
            _getTranslatedMessage = getTranslatedMessage;
        }

        public TCException(string internalMessage, Func<string> getTranslatedMessage, Exception inner):
               base(internalMessage , inner)
        {
            _getTranslatedMessage = getTranslatedMessage;
        }

        public string GetUserRenderableMessage()
        {
            if ( _getTranslatedMessage != null )
            {
                return _getTranslatedMessage();
            }

            return "Internal Error Occured";
        }

        public string Category => "4 Roads";

        protected class NotSafeCsException : CSException
        {
            public NotSafeCsException(CSExceptionType t, string internalMessage) : base(t, internalMessage)
            {
            }

            public NotSafeCsException(CSExceptionType t, string internalMessage, Exception inner) : base(t, internalMessage, inner)
            {
            }

            public void Log()
            {
                ExceptionHelper.Handle(this);
            }
        }

        public void Log()
        {
            new NotSafeCsException(CSExceptionType.UnknownError, "Logged Error", this).Log();
        }
    }
}
