namespace GameTranslationLauncher.Domain.Packages;

/// <summary>
/// Nhóm file hỗ trợ cần cài kèm package, có thể yêu cầu người dùng xác nhận riêng.
/// </summary>
public sealed record GamePrerequisite(
    string Id,
    string DisplayName,
    string Description,
    bool RequiresExplicitConsent,
    IReadOnlyList<GamePackageFile> Files);
