using System;
using Telligent.Evolution.Components;

namespace FourRoads.Common.TelligentCommunity.Components
{
    // ReSharper disable once InconsistentNaming
    public class TCException: CSException
    {
        public TCException(string internalMessage)
            : base(CSExceptionType.UnknownError, internalMessage, null)
        {
        }

        public TCException(CSExceptionType t, string internalMessage) : base(t, internalMessage, null)
        {
        }

        public TCException(string category, string internalMessage, Exception inner)
            : base(CSExceptionType.UnknownError, string.Format("{0}:{1}", category, internalMessage), null, inner)
        {
        }

        public TCException(CSExceptionType t, string internalMessage, Exception inner)
            : base(t, internalMessage, null, inner)
        {
        }
    }
}
