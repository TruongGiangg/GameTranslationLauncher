using System.Text;
using System.Text.Json;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Infrastructure.Persistence;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Logging;

/// <summary>
/// Ghi một JSON object mỗi dòng; contract không nhận game root nên log không lộ đường dẫn tuyệt đối.
/// </summary>
public sealed class JsonLinesInstallationOperationLogger : IInstallationOperationLogger, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = ContractJsonOptions.Create();
    private readonly LauncherStoragePaths storagePaths;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public JsonLinesInstallationOperationLogger(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths;
    }

    public async Task<bool> TryWriteAsync(
        InstallationLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(storagePaths.LogsDirectory);
            var logPath = Path.Combine(
                storagePaths.LogsDirectory,
                $"{entry.TimestampUtc:yyyy-MM-dd}.jsonl");
            var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
            await File.AppendAllTextAsync(logPath, line, Encoding.UTF8, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            writeLock.Release();
        }
    }

    public void Dispose()
    {
        writeLock.Dispose();
    }
}
