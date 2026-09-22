using System.Windows.Input;
using GameTranslationLauncher.Application.Installation;
using GameTranslationLauncher.Application.Settings;
using GameTranslationLauncher.Domain.Installation;
using GameTranslationLauncher.Domain.Packages;
using GameTranslationLauncher.Domain.Settings;
using GameTranslationLauncher.Wpf.Commands;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Điều phối command UI cho game đang chọn; mọi thay đổi file đều đi qua Application use case.
/// </summary>
public sealed class GameOperationViewModel : ObservableObject
{
    private readonly SelectGameInstallationUseCase selectInstallationUseCase;
    private readonly PrepareTranslationPlanUseCase prepareTranslationPlanUseCase;
    private readonly InstallTranslationUseCase installTranslationUseCase;
    private readonly UpdateTranslationUseCase updateTranslationUseCase;
    private readonly UninstallTranslationUseCase uninstallTranslationUseCase;
    private readonly IGameFolderPicker folderPicker;
    private readonly WindowsGameShell gameShell;
    private LauncherOperationText text = LauncherTextFactory.Create("vi").Operation;
    private readonly ConflictDetailsViewModel conflictDetails = new();
    private GameLibraryItemViewModel? selectedGame;
    private PendingOperation? pendingOperation;
    private CancellationTokenSource? operationCancellation;
    private bool isOperationInProgress;
    private bool isConfirmationVisible;
    private string confirmationTitle = string.Empty;
    private string confirmationMessage = string.Empty;
    private string confirmationActionLabel = string.Empty;
    private string operationMessage = string.Empty;
    private bool isOperationError;
    private int operationProgress;

    public GameOperationViewModel(
        SelectGameInstallationUseCase selectInstallationUseCase,
        PrepareTranslationPlanUseCase prepareTranslationPlanUseCase,
        InstallTranslationUseCase installTranslationUseCase,
        UpdateTranslationUseCase updateTranslationUseCase,
        UninstallTranslationUseCase uninstallTranslationUseCase,
        IGameFolderPicker folderPicker,
        WindowsGameShell gameShell)
    {
        this.selectInstallationUseCase = selectInstallationUseCase;
        this.prepareTranslationPlanUseCase = prepareTranslationPlanUseCase;
        this.installTranslationUseCase = installTranslationUseCase;
        this.updateTranslationUseCase = updateTranslationUseCase;
        this.uninstallTranslationUseCase = uninstallTranslationUseCase;
        this.folderPicker = folderPicker;
        this.gameShell = gameShell;

        ChooseFolderCommand = new AsyncRelayCommand(ChooseFolderAsync, CanInteract);
        PrimaryActionCommand = new AsyncRelayCommand(ExecutePrimaryActionAsync, CanInteract);
        SecondaryActionCommand = new AsyncRelayCommand(ExecuteSecondaryActionAsync, CanInteract);
        TertiaryActionCommand = new AsyncRelayCommand(OpenFolderAsync, () => CanOpenFolder);
        UninstallCommand = new AsyncRelayCommand(PrepareUninstallAsync, () => CanUninstall);
        ConfirmCommand = new AsyncRelayCommand(ConfirmPendingOperationAsync, () => IsConfirmationVisible && !IsOperationInProgress);
        CancelConfirmationCommand = new RelayCommand(CancelConfirmation, () => IsConfirmationVisible && !IsOperationInProgress);
        CancelOperationCommand = new RelayCommand(CancelOperation, () => IsOperationInProgress);
    }

    public event Func<Task>? CatalogRefreshRequested;

    public event Func<GameLibraryItemViewModel, Task>? StatusCheckRequested;

    public ICommand ChooseFolderCommand { get; }

    public ICommand PrimaryActionCommand { get; }

    public ICommand SecondaryActionCommand { get; }

    public ICommand TertiaryActionCommand { get; }

    public ICommand UninstallCommand { get; }

    public ICommand ConfirmCommand { get; }

    public ICommand CancelConfirmationCommand { get; }

    public ICommand CancelOperationCommand { get; }

    public ConflictDetailsViewModel ConflictDetails => conflictDetails;

    public bool IsOperationInProgress
    {
        get => isOperationInProgress;
        private set
        {
            if (!SetProperty(ref isOperationInProgress, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanOpenFolder));
            OnPropertyChanged(nameof(CanUninstall));
            RaiseCommandStates();
        }
    }

    public bool IsConfirmationVisible
    {
        get => isConfirmationVisible;
        private set
        {
            if (!SetProperty(ref isConfirmationVisible, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanOpenFolder));
            OnPropertyChanged(nameof(CanUninstall));
            RaiseCommandStates();
        }
    }

    public string ConfirmationTitle
    {
        get => confirmationTitle;
        private set => SetProperty(ref confirmationTitle, value);
    }

    public string ConfirmationMessage
    {
        get => confirmationMessage;
        private set => SetProperty(ref confirmationMessage, value);
    }

    public string ConfirmationActionLabel
    {
        get => confirmationActionLabel;
        private set => SetProperty(ref confirmationActionLabel, value);
    }

    public string OperationMessage
    {
        get => operationMessage;
        private set => SetProperty(ref operationMessage, value);
    }

    public bool IsOperationError
    {
        get => isOperationError;
        private set => SetProperty(ref isOperationError, value);
    }

    public int OperationProgress
    {
        get => operationProgress;
        private set => SetProperty(ref operationProgress, value);
    }

    public bool HasOperationMessage => !string.IsNullOrWhiteSpace(OperationMessage);

    public bool CanOpenFolder => selectedGame?.CatalogItem?.GameRoot is not null && !IsOperationInProgress;

    public bool CanUninstall
    {
        get
        {
            var game = selectedGame;
            return game is not null
                && game.CatalogItem is { InstallationId: not null, GameRoot: not null }
                && (game.CatalogItem.Status is InstallationStatus.Installed
                    or InstallationStatus.UpdateAvailable
                    or InstallationStatus.Damaged
                    or InstallationStatus.Conflict)
                && !IsOperationInProgress;
        }
    }

    public void SelectGame(GameLibraryItemViewModel? game)
    {
        selectedGame = game;
        pendingOperation = null;
        conflictDetails.Hide();
        IsConfirmationVisible = false;
        OperationMessage = string.Empty;
        IsOperationError = false;
        OperationProgress = 0;
        OnPropertyChanged(nameof(HasOperationMessage));
        OnPropertyChanged(nameof(CanOpenFolder));
        OnPropertyChanged(nameof(CanUninstall));
        RaiseCommandStates();
    }

    public void ApplyText(LauncherOperationText value)
    {
        text = value ?? throw new ArgumentNullException(nameof(value));
        gameShell.ApplyText(text);
        conflictDetails.ApplyText(text);
    }

    public void RefreshSelectedGame()
    {
        OnPropertyChanged(nameof(CanOpenFolder));
        OnPropertyChanged(nameof(CanUninstall));
        RaiseCommandStates();
    }

    private async Task ExecutePrimaryActionAsync()
    {
        var game = selectedGame;
        if (game?.CatalogItem is not { } catalogItem)
        {
            return;
        }

        switch (catalogItem.Status)
        {
            case InstallationStatus.Installed:
                ShowShellResult(gameShell.TryLaunchGame(catalogItem, out var launchMessage), launchMessage);
                break;
            case InstallationStatus.UpdateAvailable:
                await PrepareApplyAsync(PendingOperationKind.Update);
                break;
            case InstallationStatus.NotInstalled:
                await PrepareApplyAsync(PendingOperationKind.Install);
                break;
            case InstallationStatus.Damaged:
                await CheckStatusAsync(game, text.StatusRefreshed);
                break;
            case InstallationStatus.Conflict:
                conflictDetails.Show(game);
                break;
            default:
                if (catalogItem.GameRoot is null)
                {
                    await ChooseFolderAsync();
                }
                else
                {
                    await CheckStatusAsync(game, text.StatusRefreshed);
                }
                break;
        }
    }

    private async Task ExecuteSecondaryActionAsync()
    {
        var game = selectedGame;
        if (game?.CatalogItem is not { } catalogItem)
        {
            return;
        }

        if (catalogItem.Status == InstallationStatus.Installed)
        {
            await CheckStatusAsync(game, text.UpdatesChecked);
            return;
        }

        if (catalogItem.Status == InstallationStatus.UpdateAvailable)
        {
            ShowShellResult(gameShell.TryLaunchGame(catalogItem, out var launchMessage), launchMessage);
            return;
        }

        await OpenFolderAsync();
    }

    private async Task ChooseFolderAsync()
    {
        var game = selectedGame;
        if (game?.CatalogItem is not { } catalogItem)
        {
            return;
        }

        var directory = folderPicker.PickFolder(
            string.Format(text.ChooseFolderDialogTitleFormat, game.DisplayName),
            catalogItem.GameRoot);
        if (string.IsNullOrWhiteSpace(directory))
        {
            ShowResult(false, text.FolderNotChanged);
            return;
        }

        try
        {
            await selectInstallationUseCase.ExecuteAsync(
                catalogItem.AvailablePackage,
                directory);
            await RefreshCatalogAsync(text.FolderSaved);
        }
        catch (InstallationOperationException exception)
        {
            ShowResult(true, exception.Message);
        }
        catch (Exception)
        {
            ShowResult(true, text.FolderVerificationFailed);
        }
    }

    private async Task PrepareApplyAsync(PendingOperationKind kind)
    {
        var game = selectedGame;
        if (game?.CatalogItem is not { } catalogItem)
        {
            return;
        }

        var installation = await EnsureInstallationAsync(game);
        if (installation is null)
        {
            return;
        }

        IsOperationInProgress = true;
        OperationProgress = 5;
        ShowResult(false, text.PreparingPlan);
        operationCancellation = new CancellationTokenSource();
        try
        {
            var package = catalogItem.AvailablePackage.Package;
            var acceptedPrerequisites = package.Prerequisites
                .Where(prerequisite => prerequisite.RequiresExplicitConsent)
                .Select(prerequisite => prerequisite.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var initialRequest = new ApplyTranslationRequest(
                catalogItem.AvailablePackage,
                installation,
                acceptedPrerequisites);
            var plan = kind == PendingOperationKind.Install
                ? await prepareTranslationPlanUseCase.PrepareInstallAsync(initialRequest, operationCancellation.Token)
                : await prepareTranslationPlanUseCase.PrepareUpdateAsync(initialRequest, operationCancellation.Token);
            var replacements = plan.Files
                .Where(file => file.Action == InstallFileAction.ReplaceExternal)
                .Select(file => file.PackageFile.Destination)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var request = initialRequest with { ApprovedReplacementDestinations = replacements };
            pendingOperation = new PendingOperation(kind, game, installation, request);
            ShowConfirmation(kind, package.Prerequisites, replacements, plan.Files.Count);
        }
        catch (InstallationOperationException exception)
        {
            ShowResult(true, exception.Message);
        }
        catch (OperationCanceledException)
        {
            ShowResult(false, text.CancelledBeforeFileOperation);
        }
        catch (Exception)
        {
            ShowResult(true, text.PlanPreparationFailed);
        }
        finally
        {
            operationCancellation.Dispose();
            operationCancellation = null;
            IsOperationInProgress = false;
            OperationProgress = 0;
        }
    }

    private async Task PrepareUninstallAsync()
    {
        var game = selectedGame;
        if (game?.CatalogItem is not { InstallationId: Guid installationId, GameRoot: not null } catalogItem)
        {
            return;
        }

        pendingOperation = new PendingOperation(
            PendingOperationKind.Uninstall,
            game,
            new GameInstallationSetting(
                game.Id,
                installationId,
                catalogItem.GameRoot,
                DateTimeOffset.UtcNow),
            null);
        ConfirmationTitle = text.UninstallConfirmationTitle;
        ConfirmationMessage = text.UninstallConfirmationMessage;
        ConfirmationActionLabel = text.UninstallConfirmationAction;
        IsConfirmationVisible = true;
    }

    private async Task ConfirmPendingOperationAsync()
    {
        var operation = pendingOperation;
        if (operation is null)
        {
            return;
        }

        pendingOperation = null;
        IsConfirmationVisible = false;
        operationCancellation = new CancellationTokenSource();
        IsOperationInProgress = true;
        OperationProgress = 0;
        ShowResult(false, text.PreparingFileOperation);
        var progress = new Progress<InstallationProgress>(UpdateProgress);
        try
        {
            var result = operation.Kind switch
            {
                PendingOperationKind.Install => await installTranslationUseCase.ExecuteAsync(
                    operation.Request!, progress, operationCancellation.Token),
                PendingOperationKind.Update => await updateTranslationUseCase.ExecuteAsync(
                    operation.Request!, progress, operationCancellation.Token),
                PendingOperationKind.Uninstall => await uninstallTranslationUseCase.ExecuteAsync(
                    operation.Game.Id,
                    operation.Installation.InstallationId,
                    progress,
                    operationCancellation.Token),
                _ => throw new ArgumentOutOfRangeException()
            };
            ShowResult(!result.Succeeded, result.Message);
            if (result.Succeeded)
            {
                await RefreshCatalogAsync(result.Message);
            }
        }
        catch (Exception)
        {
            ShowResult(true, text.OperationFailed);
        }
        finally
        {
            operationCancellation.Dispose();
            operationCancellation = null;
            IsOperationInProgress = false;
        }
    }

    private Task OpenFolderAsync()
    {
        var game = selectedGame;
        if (game?.CatalogItem is not { } catalogItem)
        {
            return Task.CompletedTask;
        }

        ShowShellResult(gameShell.TryOpenGameFolder(catalogItem.GameRoot, out var message), message);
        return Task.CompletedTask;
    }

    private async Task<GameInstallationSetting?> EnsureInstallationAsync(GameLibraryItemViewModel game)
    {
        if (game.CatalogItem is { InstallationId: Guid installationId, GameRoot: not null } catalogItem)
        {
            return new GameInstallationSetting(
                game.Id,
                installationId,
                catalogItem.GameRoot,
                DateTimeOffset.UtcNow);
        }

        var directory = folderPicker.PickFolder(
            string.Format(text.ChooseFolderDialogTitleFormat, game.DisplayName),
            null);
        if (string.IsNullOrWhiteSpace(directory))
        {
            ShowResult(false, text.ChooseFolderBeforeInstall);
            return null;
        }

        if (game.CatalogItem is not { } newInstallationCatalogItem)
        {
            return null;
        }

        try
        {
            return await selectInstallationUseCase.ExecuteAsync(
                newInstallationCatalogItem.AvailablePackage,
                directory);
        }
        catch (InstallationOperationException exception)
        {
            ShowResult(true, exception.Message);
            return null;
        }
        catch (Exception)
        {
            ShowResult(true, text.FolderVerificationFailed);
            return null;
        }
    }

    private void ShowConfirmation(
        PendingOperationKind kind,
        IReadOnlyList<GamePrerequisite> prerequisites,
        IReadOnlySet<string> replacements,
        int fileCount)
    {
        var requiresConsent = prerequisites
            .Where(prerequisite => prerequisite.RequiresExplicitConsent)
            .Select(prerequisite => $"• {prerequisite.DisplayName}");
        var replacementSummary = replacements.Count == 0
            ? string.Empty
            : $"\n\n{string.Format(text.ReplacementSummaryFormat, replacements.Count)}";
        var consentSummary = requiresConsent.Any()
            ? $"\n\n{string.Format(text.PrerequisiteSummaryFormat, Environment.NewLine + string.Join(Environment.NewLine, requiresConsent))}"
            : string.Empty;

        ConfirmationTitle = kind == PendingOperationKind.Install
            ? text.InstallConfirmationTitle
            : text.UpdateConfirmationTitle;
        ConfirmationMessage = string.Format(text.ConfirmationMessageFormat, fileCount) + replacementSummary + consentSummary;
        ConfirmationActionLabel = kind == PendingOperationKind.Install ? text.InstallConfirmationAction : text.UpdateConfirmationAction;
        IsConfirmationVisible = true;
    }

    private void CancelConfirmation()
    {
        pendingOperation = null;
        IsConfirmationVisible = false;
        ShowResult(false, text.CancelledBeforeChanges);
    }

    private void CancelOperation()
    {
        operationCancellation?.Cancel();
        OperationMessage = text.CancellingOperation;
        IsOperationError = false;
    }

    private async Task RefreshCatalogAsync(string message)
    {
        var refresh = CatalogRefreshRequested;
        if (refresh is not null)
        {
            await refresh();
        }

        ShowResult(false, message);
    }

    private async Task CheckStatusAsync(GameLibraryItemViewModel game, string message)
    {
        var checkStatus = StatusCheckRequested;
        if (checkStatus is null)
        {
            return;
        }

        try
        {
            await checkStatus(game);
            ShowResult(false, message);
        }
        catch (Exception)
        {
            ShowResult(true, text.StatusCheckFailed);
        }
    }

    private void UpdateProgress(InstallationProgress progress)
    {
        var total = Math.Max(progress.TotalItems, 1);
        OperationProgress = Math.Clamp((int)Math.Round(progress.CompletedItems * 100d / total), 0, 100);
        OperationMessage = progress.Message;
        IsOperationError = false;
    }

    private void ShowShellResult(bool succeeded, string message) => ShowResult(!succeeded, message);

    private void ShowResult(bool isError, string message)
    {
        IsOperationError = isError;
        OperationMessage = message;
        OnPropertyChanged(nameof(HasOperationMessage));
    }

    private bool CanInteract() => selectedGame?.HasTranslationPackage == true && !IsOperationInProgress && !IsConfirmationVisible;

    private void RaiseCommandStates()
    {
        foreach (var command in new[]
                 {
                     ChooseFolderCommand, PrimaryActionCommand, SecondaryActionCommand,
                     TertiaryActionCommand, UninstallCommand, ConfirmCommand,
                     CancelConfirmationCommand, CancelOperationCommand
                 })
        {
            switch (command)
            {
                case AsyncRelayCommand asyncCommand:
                    asyncCommand.RaiseCanExecuteChanged();
                    break;
                case RelayCommand relayCommand:
                    relayCommand.RaiseCanExecuteChanged();
                    break;
            }
        }
    }

    private sealed record PendingOperation(
        PendingOperationKind Kind,
        GameLibraryItemViewModel Game,
        GameInstallationSetting Installation,
        ApplyTranslationRequest? Request);

    private enum PendingOperationKind
    {
        Install,
        Update,
        Uninstall
    }
}
