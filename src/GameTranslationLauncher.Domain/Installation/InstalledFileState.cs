namespace GameTranslationLauncher.Domain.Installation;

/// <summary>
/// Snapshot chỉ đọc của một file đích tại thời điểm kiểm tra trạng thái.
/// </summary>
public sealed record InstalledFileState(bool Exists, long? SizeBytes, string? Sha256)
{
    public static InstalledFileState Missing { get; } = new(false, null, null);
}
