namespace GameTranslationLauncher.Application.Installation;

public enum InstallationErrorCode
{
    Validation,
    PackageCorrupted,
    Conflict,
    AccessDenied,
    InsufficientDiskSpace,
    FileInUse,
    Cancelled,
    RollbackFailed,
    ExplicitConsentRequired,
    OperationInProgress,
    Unexpected
}
