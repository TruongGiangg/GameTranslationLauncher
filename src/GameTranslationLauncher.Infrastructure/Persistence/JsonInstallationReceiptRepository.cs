using System.Text.Json;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Application.State;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Infrastructure.Persistence.Serialization;
using GameTranslationLauncher.Infrastructure.Serialization;

namespace GameTranslationLauncher.Infrastructure.Persistence;

/// <summary>
/// Đọc và ghi installation receipt JSON theo game và installation identity.
/// </summary>
public sealed class JsonInstallationReceiptRepository : IInstallationReceiptRepository
{
    private static readonly JsonSerializerOptions ReadOptions = ContractJsonOptions.Create();
    private static readonly JsonSerializerOptions WriteOptions = ContractJsonOptions.Create(writeIndented: true);
    private readonly LauncherStoragePaths storagePaths;

    public JsonInstallationReceiptRepository(LauncherStoragePaths storagePaths)
    {
        this.storagePaths = storagePaths;
    }

    public async Task<InstallationReceipt?> FindAsync(
        string gameId,
        Guid installationId,
        CancellationToken cancellationToken = default)
    {
        var receiptPath = storagePaths.GetReceiptPath(gameId, installationId);
        if (!File.Exists(receiptPath))
        {
            return null;
        }

        var document = await JsonStateFileReader.ReadAsync<InstallationReceiptDocument>(
            receiptPath,
            "Receipt",
            ReadOptions,
            cancellationToken);
        InstallationReceiptDocumentValidator.Validate(document, receiptPath);
        ValidateIdentity(document, gameId, installationId, receiptPath);
        return InstallationReceiptMapper.Map(document);
    }

    public async Task SaveAsync(
        InstallationReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var receiptPath = storagePaths.GetReceiptPath(receipt.GameId, receipt.InstallationId);
        var document = InstallationReceiptMapper.Map(receipt);
        InstallationReceiptDocumentValidator.Validate(document, receiptPath);
        await AtomicJsonFileWriter.WriteAsync(
            receiptPath,
            document,
            WriteOptions,
            cancellationToken);
    }

    public Task DeleteAsync(
        string gameId,
        Guid installationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var receiptPath = storagePaths.GetReceiptPath(gameId, installationId);
        if (File.Exists(receiptPath))
        {
            File.Delete(receiptPath);
        }

        return Task.CompletedTask;
    }

    private static void ValidateIdentity(
        InstallationReceiptDocument document,
        string expectedGameId,
        Guid expectedInstallationId,
        string receiptPath)
    {
        if (!string.Equals(document.GameId, expectedGameId, StringComparison.OrdinalIgnoreCase)
            || document.InstallationId != expectedInstallationId)
        {
            throw new LauncherStateContractException(
                receiptPath,
                ["Game ID hoặc installation ID trong receipt không khớp vị trí lưu"]);
        }
    }
}
