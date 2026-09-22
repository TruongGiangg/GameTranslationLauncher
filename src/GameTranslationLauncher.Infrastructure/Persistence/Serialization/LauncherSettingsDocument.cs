namespace GameTranslationLauncher.Infrastructure.Persistence.Serialization;

internal sealed class LauncherSettingsDocument
{
    public required int SchemaVersion { get; init; }

    public required List<GameInstallationSettingDocument> Installations { get; init; }

    public string? DisplayLanguage { get; init; }

    public string? AccentTheme { get; init; }
}

internal sealed class GameInstallationSettingDocument
{
    public required string GameId { get; init; }

    public required Guid InstallationId { get; init; }

    public required string GameRoot { get; init; }

    public required DateTimeOffset LastUsedAtUtc { get; init; }
}
