using GameTranslationLauncher.Application.Catalog;

namespace GameTranslationLauncher.Application.Installation;

public interface IPackageIntegrityVerifier
{
    Task VerifyAsync(
        AvailableGamePackage availablePackage,
        CancellationToken cancellationToken = default);
}
