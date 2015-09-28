using FourRoads.Common.TelligentCommunity.Plugins.Interfaces;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Emoticons.Interfaces
{
    public interface IEmoticonPlugin : IBindingsLoader, IHtmlHeaderExtension, IPluginGroup, IConfigurablePlugin
    {
    }
}
