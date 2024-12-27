namespace NeeLaboratory.RealtimeSearch.Services
{
    public interface IAppSettings
    {
        string? CheckVersion { get; set; }
        string DateVersion { get; set; }
        string? DistributionUrl { get; set; }
        string? LogFile { get; set; }
        string PackageType { get; set; }
        string Revision { get; set; }
        bool SelfContained { get; set; }
        bool TrimSaveData { get; set; }
        bool UseLocalApplicationData { get; set; }
        bool Watermark { get; set; }
    }
}