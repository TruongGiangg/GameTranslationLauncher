namespace GameTranslationLauncher.Application.Installation;

public sealed record InstallationLogEntry(
    DateTimeOffset TimestampUtc,
    Guid OperationId,
    string GameId,
    Guid InstallationId,
    InstallationProgressStage Stage,
    string Message,
    InstallationErrorCode? ErrorCode = null);
