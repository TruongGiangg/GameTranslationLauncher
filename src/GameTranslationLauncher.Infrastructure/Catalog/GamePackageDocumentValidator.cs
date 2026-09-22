using System.Globalization;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Infrastructure.Catalog.Serialization;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Catalog;

internal static class GamePackageDocumentValidator
{
    public static void Validate(GamePackageDocument document)
    {
        var errors = new List<string>();
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ValidateHeader(document, errors);
        ValidateDetection(document.InstallDetection, errors);
        ValidatePayloadFiles(document.PayloadFiles, destinations, errors);
        ValidatePrerequisites(document.Prerequisites, destinations, errors);

        if (errors.Count > 0)
        {
            throw new GamePackageContractException(errors);
        }
    }

    private static void ValidateHeader(GamePackageDocument document, List<string> errors)
    {
        if (document.SchemaVersion != 1)
        {
            errors.Add($"schemaVersion '{document.SchemaVersion}' chưa được hỗ trợ");
        }

        ValidatePattern(document.GameId, "gameId", ContractValueValidator.IsGameId, errors);
        ValidateText(document.DisplayName, "displayName", 120, errors);
        ValidatePattern(document.PackageVersion, "packageVersion", ContractValueValidator.IsSemanticVersion, errors);
        ValidatePattern(document.TargetLanguage, "targetLanguage", ContractValueValidator.IsLanguage, errors);

        if (!DateOnly.TryParseExact(
                document.LastVerifiedWorkingInGame,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            errors.Add("lastVerifiedWorkingInGame phải có định dạng yyyy-MM-dd");
        }
    }

    private static void ValidateDetection(InstallDetectionDocument? detection, List<string> errors)
    {
        if (detection is null)
        {
            errors.Add("installDetection là bắt buộc");
            return;
        }

        if (detection.RequiredPaths is null || detection.RequiredPaths.Count == 0)
        {
            errors.Add("installDetection.requiredPaths phải có ít nhất một marker");
        }
        else
        {
            var markerPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var marker in detection.RequiredPaths)
            {
                if (marker is null)
                {
                    errors.Add("installDetection.requiredPaths không được chứa null");
                    continue;
                }

                ValidateRelativePath(marker.Path, "installDetection.requiredPaths.path", errors);
                if (!markerPaths.Add(marker.Path))
                {
                    errors.Add($"Marker bị trùng: '{marker.Path}'");
                }

                if (marker.Kind is not ("file" or "directory"))
                {
                    errors.Add($"Loại marker không hợp lệ: '{marker.Kind}'");
                }
            }
        }

        if (detection.BlockedProcessNames is null)
        {
            errors.Add("installDetection.blockedProcessNames là bắt buộc");
            return;
        }

        var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var processName in detection.BlockedProcessNames)
        {
            ValidateText(processName, "blockedProcessNames", 260, errors);
            if (!string.IsNullOrWhiteSpace(processName)
                && processName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                errors.Add($"Tên process không hợp lệ: '{processName}'");
            }

            if (!processNames.Add(processName))
            {
                errors.Add($"Tên process bị trùng: '{processName}'");
            }
        }
    }

    private static void ValidatePayloadFiles(
        List<PayloadFileDocument>? files,
        HashSet<string> destinations,
        List<string> errors)
    {
        if (files is null || files.Count == 0)
        {
            errors.Add("payloadFiles phải có ít nhất một file");
            return;
        }

        foreach (var file in files)
        {
            if (file is null)
            {
                errors.Add("payloadFiles không được chứa null");
                continue;
            }

            ValidatePackageFile(file.Source, file.Destination, file.Sha256, file.SizeBytes, destinations, errors);
            if (file.Role is not ("translation" or "companion"))
            {
                errors.Add($"Role của payload không hợp lệ: '{file.Role}'");
            }
        }
    }

    private static void ValidatePrerequisites(
        List<PrerequisiteDocument>? prerequisites,
        HashSet<string> destinations,
        List<string> errors)
    {
        if (prerequisites is null)
        {
            errors.Add("prerequisites là bắt buộc; dùng mảng rỗng nếu không có");
            return;
        }

        var prerequisiteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prerequisite in prerequisites)
        {
            if (prerequisite is null)
            {
                errors.Add("prerequisites không được chứa null");
                continue;
            }

            ValidatePattern(prerequisite.Id, "prerequisite.id", ContractValueValidator.IsGameId, errors);
            ValidateText(prerequisite.DisplayName, "prerequisite.displayName", 120, errors);
            ValidateText(prerequisite.Description, "prerequisite.description", 1000, errors);
            if (!prerequisiteIds.Add(prerequisite.Id))
            {
                errors.Add($"Prerequisite ID bị trùng: '{prerequisite.Id}'");
            }

            if (prerequisite.Files is null || prerequisite.Files.Count == 0)
            {
                errors.Add($"Prerequisite '{prerequisite.Id}' phải có ít nhất một file");
                continue;
            }

            foreach (var file in prerequisite.Files)
            {
                if (file is null)
                {
                    errors.Add($"Prerequisite '{prerequisite.Id}' không được chứa file null");
                    continue;
                }

                ValidatePackageFile(file.Source, file.Destination, file.Sha256, file.SizeBytes, destinations, errors);
            }
        }
    }

    private static void ValidatePackageFile(
        string source,
        string destination,
        string sha256,
        long sizeBytes,
        HashSet<string> destinations,
        List<string> errors)
    {
        ValidateRelativePath(source, "source", errors);
        ValidateRelativePath(destination, "destination", errors);
        ValidatePattern(sha256, "sha256", ContractValueValidator.IsSha256, errors);

        if (sizeBytes < 0)
        {
            errors.Add($"sizeBytes của '{source}' không được âm");
        }

        if (!destinations.Add(destination))
        {
            errors.Add($"Destination bị trùng: '{destination}'");
        }
    }

    private static void ValidateRelativePath(string value, string fieldName, List<string> errors)
    {
        if (!ContractValueValidator.IsSafeRelativePath(value))
        {
            errors.Add($"Đường dẫn tương đối không hợp lệ: '{value}'");
        }
    }

    private static void ValidatePattern(
        string value,
        string fieldName,
        Func<string?, bool> isValid,
        List<string> errors)
    {
        if (!isValid(value))
        {
            errors.Add($"{fieldName} có định dạng không hợp lệ: '{value}'");
        }
    }

    private static void ValidateText(string value, string fieldName, int maxLength, List<string> errors)
    {
        if (!ContractValueValidator.IsText(value, maxLength))
        {
            errors.Add($"{fieldName} phải có từ 1 đến {maxLength} ký tự và không có khoảng trắng thừa");
        }
    }
}
