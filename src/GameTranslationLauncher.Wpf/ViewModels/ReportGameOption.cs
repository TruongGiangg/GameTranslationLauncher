namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Game từ catalog được chọn khi gửi báo lỗi hoặc yêu cầu cập nhật.
/// </summary>
public sealed record ReportGameOption(string GameId, string DisplayName);
