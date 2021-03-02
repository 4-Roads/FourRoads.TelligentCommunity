using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.SocialProfileControls
{
    public interface IProfilePlugin : IPlugin
    {
        string FieldName { get; }
        string FieldType { get; }
        string Template { get;}
    }
}