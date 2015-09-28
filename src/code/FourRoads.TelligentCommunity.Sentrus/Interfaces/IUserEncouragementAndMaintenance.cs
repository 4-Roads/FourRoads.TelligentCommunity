namespace FourRoads.TelligentCommunity.Sentrus.Interfaces
{
    using Telligent.Evolution.Extensibility.Version1;

    public interface IUserEncouragementAndMaintenance : ISingletonPlugin
    {
        int InactivityPeriod { get; }
        void TestSettings();
    }
}