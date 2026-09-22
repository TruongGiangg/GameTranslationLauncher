using System.Text.Json;
using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Infrastructure.Persistence.Serialization;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

/// <summary>
/// Đọc và ghi settings JSON bằng replace trong cùng thư mục để tránh state dở dang.
/// </summary>
public sealed class JsonLauncherSettingsRepository : ILauncherSettingsRepository
{
    private static readonly JsonSerializerOptions ReadOptions = ContractJsonOptions.Create();
    private static readonly JsonSerializerOptions WriteOptions = ContractJsonOptions.Create(writeIndented: true);
    private readonly LauncherStoragePaths storagePaths;

    public JsonLauncherSettingsRepository(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths;
    }

    public async Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settingsPath = storagePaths.SettingsPath;
        if (!File.Exists(settingsPath))
        {
            return LauncherSettings.Empty;
        }

        var document = await JsonStateFileReader.ReadAsync<LauncherSettingsDocument>(
            settingsPath,
            "Settings",
            ReadOptions,
            cancellationToken);
        LauncherSettingsDocumentValidator.Validate(document, settingsPath);
        return LauncherSettingsMapper.Map(document);
    }

    public async Task SaveAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var document = LauncherSettingsMapper.Map(settings);
        LauncherSettingsDocumentValidator.Validate(document, storagePaths.SettingsPath);
        await AtomicJsonFileWriter.WriteAsync(
            storagePaths.SettingsPath,
            document,
            WriteOptions,
            cancellationToken);
    }

}
