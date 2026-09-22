namespace GameTranslationLauncher.Application.Installation;

public interface IInstallationOperationLogger
{
    /// <summary>
    /// Ghi log nếu có thể; trả false nếu nơi lưu log tạm thời không khả dụng.
    /// </summary>
    Task<bool> TryWriteAsync(
        InstallationLogEntry entry,
        CancellationToken cancellationToken = default);
}
