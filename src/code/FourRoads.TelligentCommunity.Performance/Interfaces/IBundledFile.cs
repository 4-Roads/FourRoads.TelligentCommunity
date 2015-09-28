namespace FourRoads.TelligentCommunity.Performance.Interfaces
{
    public interface IBundledFile
    {
        bool IsValid { get; }
        string Type { get; }
        string RelativeUri { get; }
        string OrignalUri { get;}
        string LocalPath { get;}
    }
}