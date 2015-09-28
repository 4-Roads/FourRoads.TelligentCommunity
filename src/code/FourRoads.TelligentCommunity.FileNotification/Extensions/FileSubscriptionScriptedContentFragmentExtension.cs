using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FourRoads.Common;
using FourRoads.TelligentCommunity.FileNotification.Interfaces.Plugins;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.FileNotification.Extensions
{
    internal class FileSubscriptionScriptedContentFragmentExtension : IScriptedContentFragmentExtension
    {
        public object Extension
        {
            get { return Injector.Get<IFileSubscriptionScriptedContentFragment>(); }
        }

        public string ExtensionName
        {
            get { return "filenotifation_v1_subscription"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to access IFileCommentScriptedContentFragment"; }
        }

        public void Initialize()
        { }

        public string Name
        {
            get { return string.Format("FileNotification - SubscribeToFile Extension ({0})", ExtensionName); }
        }
    }
}
