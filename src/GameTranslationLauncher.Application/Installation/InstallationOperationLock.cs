using System.Collections.Concurrent;

namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Chặn hai operation thay đổi cùng một installation trong một tiến trình Launcher.
/// </summary>
public sealed class InstallationOperationLock
{
    private readonly ConcurrentDictionary<string, byte> activeOperations =
        new(StringComparer.OrdinalIgnoreCase);

    public IDisposable? TryAcquire(string gameId, Guid installationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        if (installationId == Guid.Empty)
        {
            throw new ArgumentException("Installation ID không được rỗng.", nameof(installationId));
        }

        var key = $"{gameId}:{installationId:N}";
        return activeOperations.TryAdd(key, 0)
            ? new Lease(activeOperations, key)
            : null;
    }

    private sealed class Lease(
        ConcurrentDictionary<string, byte> activeOperations,
        string key) : IDisposable
    {
        private int isDisposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) == 0)
            {
                activeOperations.TryRemove(key, out _);
            }
        }
    }
}
