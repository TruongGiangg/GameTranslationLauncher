namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Nội dung presentation tĩnh của một game, không tham gia vào quy tắc package hay cài đặt.
/// </summary>
public sealed record GamePresentation(
    string SupportText,
    string VietnameseDescription,
    string EnglishDescription,
    string VietnamesePrerequisiteNotice,
    string EnglishPrerequisiteNotice)
{
    public string GetDescription(string languageCode) =>
        string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase)
            ? EnglishDescription
            : VietnameseDescription;

    public string GetPrerequisiteNotice(string languageCode) =>
        string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase)
            ? EnglishPrerequisiteNotice
            : VietnamesePrerequisiteNotice;
}
