namespace GameTranslationLauncher.Domain.Settings;

/// <summary>
/// Liên kết một game với thư mục cài đặt mà người dùng đã xác nhận trên máy hiện tại.
/// </summary>
public sealed record GameInstallationSetting(
    string GameId,
    Guid InstallationId,
    string GameRoot,
    DateTimeOffset LastUsedAtUtc);
