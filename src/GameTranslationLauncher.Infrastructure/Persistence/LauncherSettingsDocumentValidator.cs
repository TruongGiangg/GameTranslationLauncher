using GameTranslationLauncher.Application.State;
using GameTranslationLauncher.Infrastructure.Persistence.Serialization;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

internal static class LauncherSettingsDocumentValidator
{
    public static void Validate(LauncherSettingsDocument document, string statePath)
    {
        var errors = new List<string>();
        if (document.SchemaVersion != 1)
        {
            errors.Add($"schemaVersion '{document.SchemaVersion}' chưa được hỗ trợ");
        }

        if (document.Installations is null)
        {
            errors.Add("installations là bắt buộc");
        }
        else
        {
            ValidateInstallations(document.Installations, errors);
        }

        if (document.DisplayLanguage is not null
            && document.DisplayLanguage is not ("vi" or "en"))
        {
            errors.Add($"displayLanguage không hợp lệ: '{document.DisplayLanguage}'");
        }

        if (document.AccentTheme is not null
            && document.AccentTheme is not ("crimson" or "blue" or "emerald" or "violet" or "white" or "orange" or "cyan"))
        {
            errors.Add($"accentTheme không hợp lệ: '{document.AccentTheme}'");
        }

        if (errors.Count > 0)
        {
            throw new LauncherStateContractException(statePath, errors);
        }
    }

    private static void ValidateInstallations(
        IEnumerable<GameInstallationSettingDocument> installations,
        List<string> errors)
    {
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var installation in installations)
        {
            if (installation is null)
            {
                errors.Add("installations không được chứa null");
                continue;
            }

            if (!ContractValueValidator.IsGameId(installation.GameId))
            {
                errors.Add($"gameId không hợp lệ: '{installation.GameId}'");
            }

            if (installation.InstallationId == Guid.Empty)
            {
                errors.Add("installationId không được rỗng");
            }

            if (!ContractValueValidator.IsAbsolutePath(installation.GameRoot))
            {
                errors.Add($"gameRoot phải là đường dẫn tuyệt đối: '{installation.GameRoot}'");
            }

            if (installation.LastUsedAtUtc.Offset != TimeSpan.Zero)
            {
                errors.Add("lastUsedAtUtc phải dùng UTC");
            }

            var identity = $"{installation.GameId}:{installation.InstallationId:N}";
            if (!identities.Add(identity))
            {
                errors.Add($"Installation bị trùng: '{identity}'");
            }
        }
    }
}
