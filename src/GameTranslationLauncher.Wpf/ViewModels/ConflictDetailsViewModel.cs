using System.Windows.Input;
using GameTranslationLauncher.Wpf.Commands;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Cung cấp nội dung thuần presentation cho modal giải thích trạng thái xung đột file.
/// </summary>
public sealed class ConflictDetailsViewModel : ObservableObject
{
    private LauncherOperationText text = LauncherTextFactory.Create("vi").Operation;
    private bool isVisible;
    private string title = string.Empty;
    private string message = string.Empty;
    private string recommendation = string.Empty;

    public ConflictDetailsViewModel()
    {
        CloseCommand = new RelayCommand(Hide);
    }

    public ICommand CloseCommand { get; }

    public bool IsVisible
    {
        get => isVisible;
        private set => SetProperty(ref isVisible, value);
    }

    public string Title
    {
        get => title;
        private set => SetProperty(ref title, value);
    }

    public string Message
    {
        get => message;
        private set => SetProperty(ref message, value);
    }

    public string Recommendation
    {
        get => recommendation;
        private set => SetProperty(ref recommendation, value);
    }

    public string AcknowledgeAction => text.ConflictDetailsAcknowledgeAction;

    public void ApplyText(LauncherOperationText value)
    {
        text = value ?? throw new ArgumentNullException(nameof(value));
        OnPropertyChanged(nameof(AcknowledgeAction));
    }

    public void Show(GameLibraryItemViewModel game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Title = text.ConflictDetailsTitle;
        Message = string.Format(text.ConflictDetailsMessageFormat, game.DisplayName);
        Recommendation = text.ConflictDetailsRecommendation;
        IsVisible = true;
    }

    public void Hide() => IsVisible = false;
}
