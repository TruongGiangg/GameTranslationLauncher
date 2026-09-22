using System.Text.RegularExpressions;

namespace GameTranslationLauncher.Infrastructure.Serialization;

internal static class ContractValueValidator
{
    private static readonly Regex GameIdPattern = CreateRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$");
    private static readonly Regex SemanticVersionPattern = CreateRegex(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$");
    private static readonly Regex LanguagePattern = CreateRegex("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$");
    private static readonly Regex Sha256Pattern = CreateRegex("^[a-f0-9]{64}$");

    public static bool IsGameId(string? value)
    {
        return IsText(value, 80) && GameIdPattern.IsMatch(value!);
    }

    public static bool IsSemanticVersion(string? value)
    {
        return IsText(value, 80)
            && SemanticVersionPattern.IsMatch(value!)
            && value!.Split('.').All(part => int.TryParse(part, out _));
    }

    public static bool IsLanguage(string? value)
    {
        return IsText(value, 80) && LanguagePattern.IsMatch(value!);
    }

    public static bool IsSha256(string? value)
    {
        return value is not null && Sha256Pattern.IsMatch(value);
    }

    public static bool IsText(string? value, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value == value.Trim()
            && value.Length <= maxLength;
    }

    public static bool IsSafeRelativePath(string? value)
    {
        if (!IsText(value, 1024)
            || value!.Contains('\\')
            || value.Contains(':')
            || value.StartsWith('/')
            || value.EndsWith('/'))
        {
            return false;
        }

        return value.Split('/').All(segment =>
            !string.IsNullOrEmpty(segment)
            && segment is not ("." or "..")
            && segment.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
    }

    public static bool IsAbsolutePath(string? value)
    {
        return IsText(value, 32767) && Path.IsPathFullyQualified(value!);
    }

    private static Regex CreateRegex(string pattern)
    {
        return new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }
}
