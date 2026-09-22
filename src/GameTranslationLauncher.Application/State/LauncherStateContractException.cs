namespace GameTranslationLauncher.Application.State;

/// <summary>
/// Cho biết settings hoặc receipt cục bộ không thể đọc an toàn theo schema được hỗ trợ.
/// </summary>
public sealed class LauncherStateContractException : Exception
{
    public LauncherStateContractException(string statePath, IReadOnlyList<string> errors)
        : base(BuildMessage(statePath, errors))
    {
        StatePath = statePath;
        Errors = errors;
    }

    public LauncherStateContractException(string statePath, string error, Exception innerException)
        : base(BuildMessage(statePath, [error]), innerException)
    {
        StatePath = statePath;
        Errors = [error];
    }

    public string StatePath { get; }

    public IReadOnlyList<string> Errors { get; }

    private static string BuildMessage(string statePath, IReadOnlyList<string> errors)
    {
        return $"Local state không hợp lệ tại '{statePath}': {string.Join("; ", errors)}";
    }
}
