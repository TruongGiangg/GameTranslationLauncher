namespace GameTranslationLauncher.Application.Installation;

/// <summary>
/// Lỗi nghiệp vụ có mã ổn định để ViewModel không phải phân tích message.
/// </summary>
public sealed class InstallationOperationException : Exception
{
    public InstallationOperationException(
        InstallationErrorCode errorCode,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public InstallationErrorCode ErrorCode { get; }
}
