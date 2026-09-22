namespace GameTranslationLauncher.Application.Installation;

public sealed class NullInstallationOperationLogger : IInstallationOperationLogger
{
    public Task<bool> TryWriteAsync(
        InstallationLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }
}
