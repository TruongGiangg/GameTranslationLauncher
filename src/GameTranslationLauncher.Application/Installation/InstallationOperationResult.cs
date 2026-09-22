using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Application.Installation;

public sealed record InstallationOperationResult(
    Guid OperationId,
    bool Succeeded,
    InstallationErrorCode? ErrorCode,
    string Message,
    InstallationReceipt? Receipt)
{
    public static InstallationOperationResult Success(
        Guid operationId,
        string message,
        InstallationReceipt? receipt = null) =>
        new(operationId, true, null, message, receipt);

    public static InstallationOperationResult Failure(
        Guid operationId,
        InstallationErrorCode errorCode,
        string message) =>
        new(operationId, false, errorCode, message, null);
}
