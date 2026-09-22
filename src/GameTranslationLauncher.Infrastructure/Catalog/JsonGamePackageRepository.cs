using System.Text.Json;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Infrastructure.Catalog.Serialization;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Catalog;

/// <summary>
/// Đọc package manifest JSON v1, từ chối field lạ và chuyển sang model domain.
/// </summary>
public sealed class JsonGamePackageRepository : IGamePackageRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = ContractJsonOptions.Create();

    /// <inheritdoc />
    public async Task<GamePackage> LoadAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        await using var stream = new FileStream(
            manifestPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        GamePackageDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<GamePackageDocument>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new GamePackageContractException(
                $"JSON không đúng contract tại '{manifestPath}': {exception.Message}",
                exception);
        }

        if (document is null)
        {
            throw new GamePackageContractException(["Manifest không được là null"]);
        }

        GamePackageDocumentValidator.Validate(document);
        return GamePackageMapper.Map(document);
    }
}
