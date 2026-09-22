using GameTranslationLauncher.Application.State;
using GameTranslationLauncher.Infrastructure.Persistence.Serialization;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

internal static class InstallationReceiptDocumentValidator
{
    public static void Validate(InstallationReceiptDocument document, string statePath)
    {
        var errors = new List<string>();
        ValidateHeader(document, errors);
        ValidateCreatedDirectories(document.CreatedDirectories, errors);
        ValidateFiles(document.Files, errors);

        if (errors.Count > 0)
        {
            throw new LauncherStateContractException(statePath, errors);
        }
    }

    private static void ValidateHeader(InstallationReceiptDocument document, List<string> errors)
    {
        if (document.SchemaVersion != 1)
        {
            errors.Add($"schemaVersion '{document.SchemaVersion}' chưa được hỗ trợ");
        }

        if (document.InstallationId == Guid.Empty)
        {
            errors.Add("installationId không được rỗng");
        }

        if (!ContractValueValidator.IsGameId(document.GameId))
        {
            errors.Add($"gameId không hợp lệ: '{document.GameId}'");
        }

        if (!ContractValueValidator.IsAbsolutePath(document.GameRoot))
        {
            errors.Add($"gameRoot phải là đường dẫn tuyệt đối: '{document.GameRoot}'");
        }

        if (!ContractValueValidator.IsSemanticVersion(document.PackageVersion))
        {
            errors.Add($"packageVersion không hợp lệ: '{document.PackageVersion}'");
        }

        if (!ContractValueValidator.IsSha256(document.PackageManifestSha256))
        {
            errors.Add("packageManifestSha256 không hợp lệ");
        }

        if (!ContractValueValidator.IsSemanticVersion(document.LauncherVersion))
        {
            errors.Add($"launcherVersion không hợp lệ: '{document.LauncherVersion}'");
        }

        if (document.CompletedAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add("completedAtUtc phải dùng UTC");
        }
    }

    private static void ValidateCreatedDirectories(
        List<string>? directories,
        List<string> errors)
    {
        if (directories is null)
        {
            errors.Add("createdDirectories là bắt buộc");
            return;
        }

        var uniqueDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in directories)
        {
            if (!ContractValueValidator.IsSafeRelativePath(directory))
            {
                errors.Add($"createdDirectories chứa đường dẫn không hợp lệ: '{directory}'");
            }

            if (!uniqueDirectories.Add(directory))
            {
                errors.Add($"createdDirectories bị trùng: '{directory}'");
            }
        }
    }

    private static void ValidateFiles(
        List<InstalledFileReceiptDocument>? files,
        List<string> errors)
    {
        if (files is null || files.Count == 0)
        {
            errors.Add("files phải có ít nhất một phần tử");
            return;
        }

        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (file is null)
            {
                errors.Add("files không được chứa null");
                continue;
            }

            ValidateFile(file, errors);
            if (!destinations.Add(file.Destination))
            {
                errors.Add($"Receipt destination bị trùng: '{file.Destination}'");
            }
        }
    }

    private static void ValidateFile(InstalledFileReceiptDocument file, List<string> errors)
    {
        if (!ContractValueValidator.IsSafeRelativePath(file.Destination))
        {
            errors.Add($"Receipt destination không hợp lệ: '{file.Destination}'");
        }

        if (!ContractValueValidator.IsSha256(file.InstalledSha256))
        {
            errors.Add($"installedSha256 không hợp lệ cho '{file.Destination}'");
        }

        if (file.InstalledSizeBytes < 0)
        {
            errors.Add($"installedSizeBytes không được âm cho '{file.Destination}'");
        }

        if (file.Role is not ("translation" or "companion" or "prerequisite"))
        {
            errors.Add($"Receipt role không hợp lệ: '{file.Role}'");
        }

        ValidatePrerequisiteLink(file, errors);
        ValidateOwnership(file, errors);
    }

    private static void ValidatePrerequisiteLink(
        InstalledFileReceiptDocument file,
        List<string> errors)
    {
        if (file.Role == "prerequisite")
        {
            if (!ContractValueValidator.IsGameId(file.PrerequisiteId))
            {
                errors.Add($"prerequisiteId không hợp lệ cho '{file.Destination}'");
            }
        }
        else if (file.PrerequisiteId is not null)
        {
            errors.Add($"Chỉ file prerequisite mới được có prerequisiteId: '{file.Destination}'");
        }
    }

    private static void ValidateOwnership(InstalledFileReceiptDocument file, List<string> errors)
    {
        if (file.Ownership is "created" or "preserved")
        {
            if (file.BackupRelativePath is not null
                || file.OriginalSha256 is not null
                || file.OriginalSizeBytes is not null)
            {
                errors.Add($"File {file.Ownership} không được có thông tin backup: '{file.Destination}'");
            }

            return;
        }

        if (file.Ownership != "replaced")
        {
            errors.Add($"Receipt ownership không hợp lệ: '{file.Ownership}'");
            return;
        }

        if (!ContractValueValidator.IsSafeRelativePath(file.BackupRelativePath)
            || !ContractValueValidator.IsSha256(file.OriginalSha256)
            || file.OriginalSizeBytes is null or < 0)
        {
            errors.Add($"File replaced thiếu thông tin backup hợp lệ: '{file.Destination}'");
        }
    }
}
