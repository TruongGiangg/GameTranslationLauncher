using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Domain.Settings;

namespace GameTranslationLauncher.Application.Installation;

public sealed record ApplyTranslationRequest(
    AvailableGamePackage AvailablePackage,
    GameInstallationSetting Installation,
    IReadOnlySet<string> AcceptedPrerequisiteIds,
    IReadOnlySet<string>? ApprovedReplacementDestinations = null);
