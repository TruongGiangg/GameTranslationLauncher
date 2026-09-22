namespace GameTranslationLauncher.Domain.Installation;

/// <summary>
/// Cho biết Launcher sở hữu file hay chỉ theo dõi một prerequisite có sẵn.
/// </summary>
public enum InstalledFileOwnership
{
    Created,
    Replaced,
    Preserved
}
