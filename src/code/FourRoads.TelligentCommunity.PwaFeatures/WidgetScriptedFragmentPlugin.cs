using System;
using Telligent.Evolution.Extensibility.UI.Version1;

namespace FourRoads.TelligentCommunity.PwaFeatures
{
    public class WidgetScriptedFragmentPlugin : IScriptedContentFragmentFactoryDefaultProvider
    {
        public static Guid Id = new Guid("77ff006373644dc29aa39293e4ed8aed");
        public void Initialize()
        {

        }

        public string Name => "PWA Widget Provider";
        public string Description => "Provides the factory provider for these widgets";
        public Guid ScriptedContentFragmentFactoryDefaultIdentifier => Id;
    }
}