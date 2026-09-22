namespace GameTranslationLauncher.Domain.Installation;

/// <summary>
/// Trạng thái của một package đối với thư mục game đang được chọn.
/// </summary>
public enum InstallationStatus
{
    NotInstalled,
    Installed,
    UpdateAvailable,
    Damaged,
    Conflict,
    Unknown
}
