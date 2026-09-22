namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Định nghĩa presentation của game đã được lên kế hoạch nhưng chưa có package Việt hóa.
/// Không có dữ liệu package để Launcher không thể vô tình cho phép cài hoặc gỡ file game.
/// </summary>
public sealed record UntranslatedGameDefinition(
    string GameId,
    string DisplayName,
    GamePresentation Presentation);
