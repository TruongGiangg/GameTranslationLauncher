using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

/// <summary>
/// Cung cấp toàn bộ đường dẫn local state từ một storage root đã được chuẩn hóa.
/// </summary>
public sealed class LauncherStoragePaths
{
    public LauncherStoragePaths(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        RootDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
    }

    public string RootDirectory { get; }

    public string SettingsPath => Path.Combine(RootDirectory, "settings.json");

    public string LogsDirectory => Path.Combine(RootDirectory, "logs");

    public static LauncherStoragePaths CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new LauncherStoragePaths(Path.Combine(localAppData, "GameTranslationLauncher"));
    }

    public string GetReceiptPath(string gameId, Guid installationId)
    {
        if (!ContractValueValidator.IsGameId(gameId))
        {
            throw new ArgumentException("Game ID không hợp lệ.", nameof(gameId));
        }

        if (installationId == Guid.Empty)
        {
            throw new ArgumentException("Installation ID không được rỗng.", nameof(installationId));
        }

        return Path.Combine(
            RootDirectory,
            "installations",
            gameId,
            installationId.ToString("N"),
            "receipt.json");
    }

    public string GetBackupRoot(string gameId, Guid installationId)
    {
        _ = GetReceiptPath(gameId, installationId);
        return Path.Combine(
            RootDirectory,
            "backups",
            gameId,
            installationId.ToString("N"));
    }

    public string GetBackupPath(
        string gameId,
        Guid installationId,
        string backupRelativePath)
    {
        if (!ContractValueValidator.IsSafeRelativePath(backupRelativePath))
        {
            throw new ArgumentException("Đường dẫn backup không hợp lệ.", nameof(backupRelativePath));
        }

        var backupRoot = GetBackupRoot(gameId, installationId);
        var relativePath = backupRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var backupPath = Path.GetFullPath(Path.Combine(backupRoot, relativePath));
        var rootPrefix = Path.TrimEndingDirectorySeparator(backupRoot) + Path.DirectorySeparatorChar;
        if (!backupPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Đường dẫn backup vượt storage root.", nameof(backupRelativePath));
        }

        return backupPath;
    }
}
