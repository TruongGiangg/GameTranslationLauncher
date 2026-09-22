using Microsoft.Win32;
using System.IO;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Hiện Windows folder dialog, không xác minh hay lưu đường dẫn đã chọn.
/// </summary>
public sealed class WindowsGameFolderPicker : IGameFolderPicker
{
    public string? PickFolder(string title, string? initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : null
        };

        return dialog.ShowDialog() == true
            ? dialog.FolderName
            : null;
    }
}
