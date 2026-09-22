namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Mở dialog chọn thư mục game tại tầng WPF.
/// </summary>
public interface IGameFolderPicker
{
    string? PickFolder(string title, string? initialDirectory);
}
