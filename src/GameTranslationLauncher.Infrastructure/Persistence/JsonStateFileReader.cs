using System.Text.Json;
using GameTranslationLauncher.Application.State;

namespace GameTranslationLauncher.Infrastructure.Persistence;

internal static class JsonStateFileReader
{
    public static async Task<T> ReadAsync<T>(
        string statePath,
        string stateName,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            await using var stream = new FileStream(
                statePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken)
                ?? throw new LauncherStateContractException(
                    statePath,
                    [$"{stateName} không được là null"]);
        }
        catch (JsonException exception)
        {
            throw new LauncherStateContractException(
                statePath,
                $"JSON không đúng contract: {exception.Message}",
                exception);
        }
    }
}
