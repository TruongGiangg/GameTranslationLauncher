using System.Globalization;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Infrastructure.Catalog.Serialization;

namespace GameTranslationLauncher.Infrastructure.Catalog;

internal static class GamePackageMapper
{
    public static GamePackage Map(GamePackageDocument document)
    {
        var payloadFiles = document.PayloadFiles
            .Select(file => new GamePackageFile(
                file.Source,
                file.Destination,
                file.Sha256,
                file.SizeBytes,
                MapRole(file.Role)))
            .ToArray();

        var prerequisites = document.Prerequisites
            .Select(prerequisite => new GamePrerequisite(
                prerequisite.Id,
                prerequisite.DisplayName,
                prerequisite.Description,
                prerequisite.RequiresExplicitConsent,
                prerequisite.Files
                    .Select(file => new GamePackageFile(
                        file.Source,
                        file.Destination,
                        file.Sha256,
                        file.SizeBytes,
                        PackageFileRole.Prerequisite,
                        prerequisite.Id))
                    .ToArray()))
            .ToArray();

        var detection = new GameInstallDetection(
            document.InstallDetection.RequiredPaths
                .Select(marker => new GameDetectionMarker(marker.Path, MapMarkerKind(marker.Kind)))
                .ToArray(),
            document.InstallDetection.BlockedProcessNames.ToArray());

        return new GamePackage(
            document.SchemaVersion,
            document.GameId,
            document.DisplayName,
            document.PackageVersion,
            document.TargetLanguage,
            DateOnly.ParseExact(
                document.LastVerifiedWorkingInGame,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            detection,
            payloadFiles,
            prerequisites);
    }

    private static PackageFileRole MapRole(string role)
    {
        return role switch
        {
            "translation" => PackageFileRole.Translation,
            "companion" => PackageFileRole.Companion,
            _ => throw new InvalidOperationException($"Payload role '{role}' chưa được kiểm tra.")
        };
    }

    private static GameDetectionMarkerKind MapMarkerKind(string kind)
    {
        return kind switch
        {
            "file" => GameDetectionMarkerKind.File,
            "directory" => GameDetectionMarkerKind.Directory,
            _ => throw new InvalidOperationException($"Marker kind '{kind}' chưa được kiểm tra.")
        };
    }
}
