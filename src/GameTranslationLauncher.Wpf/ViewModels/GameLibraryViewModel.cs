using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Data;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Wpf.Commands;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Tải catalog local và điều phối selection giữa navigation sidebar và detail panel.
/// </summary>
public sealed class GameLibraryViewModel : ObservableObject
{
    private readonly LoadGameCatalogUseCase loadCatalogUseCase;
    private readonly CheckGameStatusUseCase checkGameStatusUseCase;
    private readonly GameArtworkResolver artworkResolver;
    private readonly GamePresentationResolver presentationResolver;
    private readonly GameOperationViewModel operations;
    private readonly LauncherSettingsViewModel settings;
    private GameLibraryItemViewModel? selectedGame;
    private bool isLoading;
    private bool isSettingsVisible;
    private int catalogGameCount;
    private int catalogErrorCount;
    private bool catalogLoadFailed;
    private bool hasLoadedCatalog;
    private string catalogMessage = string.Empty;
    private string gameSearchQuery = string.Empty;

    public GameLibraryViewModel(
        LoadGameCatalogUseCase loadCatalogUseCase,
        CheckGameStatusUseCase checkGameStatusUseCase,
        GameArtworkResolver artworkResolver,
        GamePresentationResolver presentationResolver,
        GameOperationViewModel operations,
        LauncherSettingsViewModel settings)
    {
        this.loadCatalogUseCase = loadCatalogUseCase ?? throw new ArgumentNullException(nameof(loadCatalogUseCase));
        this.checkGameStatusUseCase = checkGameStatusUseCase ?? throw new ArgumentNullException(nameof(checkGameStatusUseCase));
        this.artworkResolver = artworkResolver ?? throw new ArgumentNullException(nameof(artworkResolver));
        this.presentationResolver = presentationResolver ?? throw new ArgumentNullException(nameof(presentationResolver));
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Details = new GameDetailsViewModel();
        FilteredGames = CollectionViewSource.GetDefaultView(Games);
        FilteredGames.Filter = MatchesSearch;
        FilteredGames.SortDescriptions.Add(new SortDescription(
            nameof(GameLibraryItemViewModel.DisplayName),
            ListSortDirection.Ascending));
        this.operations.CatalogRefreshRequested += LoadAsync;
        this.operations.StatusCheckRequested += CheckStatusAsync;
        this.settings.PresentationChanged += ApplyPresentationText;
        ApplyPresentationText(this.settings.Text);
        RefreshCatalogCommand = new AsyncRelayCommand(LoadAsync, () => !IsLoading);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        HideSettingsCommand = new RelayCommand(HideSettings);
    }

    public ObservableCollection<GameLibraryItemViewModel> Games { get; } = [];

    /// <summary>
    /// View chỉ phục vụ tìm kiếm tại sidebar; danh sách gốc vẫn là nguồn catalog duy nhất.
    /// </summary>
    public ICollectionView FilteredGames { get; }

    public GameDetailsViewModel Details { get; }

    public string GameSearchQuery
    {
        get => gameSearchQuery;
        set
        {
            if (SetProperty(ref gameSearchQuery, value))
            {
                FilteredGames.Refresh();
            }
        }
    }

    public string SearchGamesPlaceholder => settings.Text.Application.SearchGamesPlaceholder;

    public GameOperationViewModel Operations => operations;

    public LauncherSettingsViewModel Settings => settings;

    public ICommand RefreshCatalogCommand { get; }

    public ICommand ShowSettingsCommand { get; }

    public ICommand HideSettingsCommand { get; }

    public bool IsSettingsVisible
    {
        get => isSettingsVisible;
        private set => SetProperty(ref isSettingsVisible, value);
    }

    public GameLibraryItemViewModel? SelectedGame
    {
        get => selectedGame;
        set
        {
            if (!SetProperty(ref selectedGame, value))
            {
                return;
            }

            Details.Show(value);
            Operations.SelectGame(value);
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        private set
        {
            if (!SetProperty(ref isLoading, value))
            {
                return;
            }

            ((AsyncRelayCommand)RefreshCatalogCommand).RaiseCanExecuteChanged();
        }
    }

    public string CatalogMessage
    {
        get => catalogMessage;
        private set => SetProperty(ref catalogMessage, value);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var previousGameId = SelectedGame?.Id;
            var result = await loadCatalogUseCase.ExecuteAsync();
            ReplaceGames(result.Items);
            SelectedGame = Games.FirstOrDefault(game => string.Equals(
                game.Id,
                previousGameId,
                StringComparison.OrdinalIgnoreCase)) ?? Games.FirstOrDefault();
            catalogGameCount = result.Items.Count;
            catalogErrorCount = result.Errors.Count;
            hasLoadedCatalog = true;
            catalogLoadFailed = false;
            UpdateCatalogMessage();
        }
        catch (Exception)
        {
            Games.Clear();
            SelectedGame = null;
            catalogGameCount = 0;
            catalogErrorCount = 0;
            hasLoadedCatalog = true;
            catalogLoadFailed = true;
            UpdateCatalogMessage();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public Task InitializeAsync() => settings.InitializeAsync();

    private async Task CheckStatusAsync(GameLibraryItemViewModel game)
    {
        if (game.CatalogItem is not { } catalogItem)
        {
            return;
        }

        var updatedItem = await checkGameStatusUseCase.ExecuteAsync(catalogItem);
        game.UpdateCatalogItem(updatedItem);

        if (ReferenceEquals(SelectedGame, game))
        {
            Details.Show(game);
            Operations.RefreshSelectedGame();
        }
    }

    private void ReplaceGames(IReadOnlyList<GameCatalogItem> items)
    {
        Games.Clear();
        foreach (var item in items)
        {
            var gameId = item.AvailablePackage.Package.GameId;
            Games.Add(new GameLibraryItemViewModel(
                item,
                artworkResolver.Resolve(gameId),
                presentationResolver.Resolve(gameId)));
        }

        var packagedGameIds = items
            .Select(item => item.AvailablePackage.Package.GameId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in presentationResolver.GetUntranslatedGames()
                     .Where(definition => !packagedGameIds.Contains(definition.GameId)))
        {
            Games.Add(new GameLibraryItemViewModel(
                definition,
                artworkResolver.Resolve(definition.GameId)));
        }

        settings.SetAvailableGames(Games.Select(game => new ReportGameOption(game.Id, game.DisplayName)));
    }

    private void ShowSettings() => IsSettingsVisible = true;

    private void HideSettings() => IsSettingsVisible = false;

    private void ApplyPresentationText(LauncherSettingsText value)
    {
        Details.ApplyText(value.Application);
        operations.ApplyText(value.Operation);
        OnPropertyChanged(nameof(SearchGamesPlaceholder));
        UpdateCatalogMessage();
    }

    private bool MatchesSearch(object item)
    {
        if (item is not GameLibraryItemViewModel game)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(GameSearchQuery))
        {
            return true;
        }

        var query = GameSearchQuery.Trim();
        return game.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
               game.Id.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateCatalogMessage()
    {
        if (catalogLoadFailed)
        {
            CatalogMessage = settings.Text.Application.CatalogLoadFailed;
            return;
        }

        CatalogMessage = !hasLoadedCatalog
            ? settings.Text.Application.CatalogLoading
            : catalogGameCount == 0
                ? settings.Text.Application.CatalogEmpty
            : catalogErrorCount == 0
                ? string.Format(settings.Text.Application.CatalogLoadedFormat, catalogGameCount)
                : string.Format(settings.Text.Application.CatalogLoadedWithErrorsFormat, catalogGameCount, catalogErrorCount);
    }
}
