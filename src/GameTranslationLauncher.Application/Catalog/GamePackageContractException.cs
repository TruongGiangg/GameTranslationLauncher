namespace GameTranslationLauncher.Application.Catalog;

/// <summary>
/// Lỗi contract chứa một hoặc nhiều vấn đề có thể hiển thị cho người tạo package.
/// </summary>
public sealed class GamePackageContractException : Exception
{
    public GamePackageContractException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    public GamePackageContractException(string error, Exception innerException)
        : this([error], innerException)
    {
    }

    private GamePackageContractException(IReadOnlyList<string> errors, Exception innerException)
        : base(BuildMessage(errors), innerException)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    private static string BuildMessage(IReadOnlyList<string> errors)
    {
        return $"Package manifest không hợp lệ: {string.Join("; ", errors)}";
    }
}
