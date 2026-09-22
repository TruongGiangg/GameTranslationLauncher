namespace GameTranslationLauncher.Application.Updates;

/// <summary>
/// File cài đặt tải về không khớp checksum công bố trên bản phát hành.
/// </summary>
public sealed class UpdatePackageIntegrityException : Exception
{
    public UpdatePackageIntegrityException(string message)
        : base(message)
    {
    }
}
