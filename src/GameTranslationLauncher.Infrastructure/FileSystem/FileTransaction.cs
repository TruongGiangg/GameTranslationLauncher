using GameTranslationLauncher.Application.Installation;

namespace GameTranslationLauncher.Infrastructure.FileSystem;

/// <summary>
/// Theo dõi chính xác file tạm và thay đổi đã commit để rollback không cần glob.
/// </summary>
internal sealed class FileTransaction
{
    private readonly string gameRoot;
    private readonly Guid operationId;
    private readonly List<string> stagingPaths = [];
    private readonly List<CommittedFileChange> committedChanges = [];
    private readonly List<string> createdDirectories = [];

    public FileTransaction(string gameRoot, Guid operationId)
    {
        this.gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameRoot));
        this.operationId = operationId;
    }

    public IReadOnlyList<string> CreatedDirectories => createdDirectories;

    public async Task<string> StageCopyAsync(
        string sourcePath,
        string destinationPath,
        string expectedHash,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        EnsureParentDirectory(destinationPath);
        var stagingPath = GetTemporaryPath(destinationPath, "stage");
        if (File.Exists(stagingPath))
        {
            throw Conflict("Đã tồn tại staging file của operation hiện tại.");
        }

        stagingPaths.Add(stagingPath);
        await CopyFileAsync(sourcePath, stagingPath, cancellationToken);
        await EnsureFileMatchesAsync(
            stagingPath,
            expectedHash,
            expectedSize,
            cancellationToken);
        return stagingPath;
    }

    public void CommitCreate(string stagingPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            throw Conflict("Destination mới đã xuất hiện trong lúc commit.");
        }

        File.Move(stagingPath, destinationPath);
        stagingPaths.Remove(stagingPath);
        committedChanges.Add(new CommittedFileChange(destinationPath, null));
    }

    public void CommitReplace(string stagingPath, string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            throw Conflict("Destination cần thay thế đã biến mất trong lúc commit.");
        }

        var rollbackPath = MoveDestinationToRollback(destinationPath);
        committedChanges.Add(new CommittedFileChange(destinationPath, rollbackPath));
        File.Move(stagingPath, destinationPath);
        stagingPaths.Remove(stagingPath);
    }

    public void CommitRemove(string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            return;
        }

        var rollbackPath = MoveDestinationToRollback(destinationPath);
        committedChanges.Add(new CommittedFileChange(destinationPath, rollbackPath));
    }

    public void CommitRestore(string stagingPath, string destinationPath)
    {
        string? rollbackPath = null;
        if (File.Exists(destinationPath))
        {
            rollbackPath = MoveDestinationToRollback(destinationPath);
        }

        committedChanges.Add(new CommittedFileChange(destinationPath, rollbackPath));
        File.Move(stagingPath, destinationPath);
        stagingPaths.Remove(stagingPath);
    }

    public void Rollback()
    {
        var errors = new List<Exception>();
        for (var index = committedChanges.Count - 1; index >= 0; index--)
        {
            try
            {
                RestoreChange(committedChanges[index]);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(exception);
            }
        }

        CleanupStaging(errors);
        RemoveCreatedDirectories(errors);
        if (errors.Count > 0)
        {
            throw new AggregateException("Không thể rollback đầy đủ file transaction.", errors);
        }
    }

    public bool TryComplete()
    {
        var errors = new List<Exception>();
        CleanupStaging(errors);
        foreach (var change in committedChanges)
        {
            if (change.RollbackPath is null)
            {
                continue;
            }

            TryDeleteFile(change.RollbackPath, errors);
        }

        return errors.Count == 0;
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough);
        await source.CopyToAsync(destination, cancellationToken);
        await destination.FlushAsync(cancellationToken);
    }

    private static async Task EnsureFileMatchesAsync(
        string path,
        string expectedHash,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length != expectedSize)
        {
            throw new InstallationOperationException(
                InstallationErrorCode.PackageCorrupted,
                "Staging file không khớp kích thước dự kiến.");
        }

        var actualHash = await Sha256File.ComputeAsync(path, cancellationToken);
        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.PackageCorrupted,
                "Staging file không khớp hash dự kiến.");
        }
    }

    private string MoveDestinationToRollback(string destinationPath)
    {
        var rollbackPath = GetTemporaryPath(destinationPath, "rollback");
        if (File.Exists(rollbackPath))
        {
            throw Conflict("Đã tồn tại rollback file của operation hiện tại.");
        }

        File.Move(destinationPath, rollbackPath);
        return rollbackPath;
    }

    private string GetTemporaryPath(string destinationPath, string suffix)
    {
        var directory = Path.GetDirectoryName(destinationPath)
            ?? throw new ArgumentException("Destination không có thư mục cha.", nameof(destinationPath));
        return Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.{operationId:N}.{suffix}");
    }

    private void EnsureParentDirectory(string destinationPath)
    {
        var parent = Path.GetDirectoryName(destinationPath)
            ?? throw new ArgumentException("Destination không có thư mục cha.", nameof(destinationPath));
        var missing = new Stack<string>();
        var current = parent;
        while (!Directory.Exists(current)
               && !string.Equals(current, gameRoot, StringComparison.OrdinalIgnoreCase))
        {
            missing.Push(current);
            current = Path.GetDirectoryName(current)
                ?? throw new InstallationOperationException(
                    InstallationErrorCode.Validation,
                    "Không thể xác định thư mục cha an toàn.");
        }

        if (!Directory.Exists(current))
        {
            throw new InstallationOperationException(
                InstallationErrorCode.Validation,
                "Game root không còn tồn tại.");
        }

        while (missing.Count > 0)
        {
            var directory = missing.Pop();
            Directory.CreateDirectory(directory);
            createdDirectories.Add(ToGameRelativePath(directory));
        }
    }

    private string ToGameRelativePath(string directory) =>
        Path.GetRelativePath(gameRoot, directory).Replace(Path.DirectorySeparatorChar, '/');

    private static void RestoreChange(CommittedFileChange change)
    {
        if (File.Exists(change.DestinationPath))
        {
            File.Delete(change.DestinationPath);
        }

        if (change.RollbackPath is not null && File.Exists(change.RollbackPath))
        {
            File.Move(change.RollbackPath, change.DestinationPath);
        }
    }

    private void CleanupStaging(List<Exception> errors)
    {
        foreach (var path in stagingPaths)
        {
            TryDeleteFile(path, errors);
        }
    }

    private void RemoveCreatedDirectories(List<Exception> errors)
    {
        for (var index = createdDirectories.Count - 1; index >= 0; index--)
        {
            var directory = SafeGamePathResolver.Resolve(gameRoot, createdDirectories[index]);
            try
            {
                if (Directory.Exists(directory)
                    && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(exception);
            }
        }
    }

    private static void TryDeleteFile(string path, List<Exception> errors)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add(exception);
        }
    }

    private static InstallationOperationException Conflict(string message) =>
        new(InstallationErrorCode.Conflict, message);

    private sealed record CommittedFileChange(string DestinationPath, string? RollbackPath);
}
