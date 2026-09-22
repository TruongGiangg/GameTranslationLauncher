using System.Windows;
using System.Windows.Media;
using GameTranslationLauncher.Application.Catalog;
using GameTranslationLauncher.Wpf.Services;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Biểu diễn presentation-only của một package trong thanh điều hướng game.
/// </summary>
public sealed class GameLibraryItemViewModel : ObservableObject
{
    private static readonly (Color Start, Color End)[] ArtworkPalettes =
    [
        (Color.FromRgb(69, 109, 151), Color.FromRgb(18, 29, 50)),
        (Color.FromRgb(142, 85, 64), Color.FromRgb(47, 26, 33)),
        (Color.FromRgb(46, 119, 107), Color.FromRgb(17, 45, 49)),
        (Color.FromRgb(128, 65, 85), Color.FromRgb(43, 18, 34)),
        (Color.FromRgb(120, 103, 62), Color.FromRgb(40, 33, 19))
    ];

    public GameLibraryItemViewModel(
        GameCatalogItem catalogItem,
        GameArtwork artwork,
        GamePresentation presentation)
    {
        this.catalogItem = catalogItem ?? throw new ArgumentNullException(nameof(catalogItem));
        id = catalogItem.AvailablePackage.Package.GameId;
        displayName = catalogItem.AvailablePackage.Package.DisplayName;
        HasTranslationPackage = true;
        InitializePresentation(artwork, presentation);
    }

    public GameLibraryItemViewModel(
        UntranslatedGameDefinition definition,
        GameArtwork artwork)
    {
        ArgumentNullException.ThrowIfNull(definition);
        catalogItem = null;
        id = definition.GameId;
        displayName = definition.DisplayName;
        HasTranslationPackage = false;
        InitializePresentation(artwork, definition.Presentation);
    }

    private GameCatalogItem? catalogItem;
    private readonly string id;
    private readonly string displayName;

    private void InitializePresentation(GameArtwork artwork, GamePresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(artwork);
        ArgumentNullException.ThrowIfNull(presentation);
        var palette = ArtworkPalettes[GetPaletteIndex(Id)];
        CoverBrush = CreateBrush(palette.Start, palette.End, new Point(0, 0), new Point(1, 1));
        HeroBrush = CreateBrush(palette.Start, palette.End, new Point(0.15, 0), new Point(0.9, 1));
        CoverImage = artwork.CoverImage;
        HeroImage = artwork.HeroImage;
        Presentation = presentation;
    }

    public GameCatalogItem? CatalogItem
    {
        get => catalogItem;
        private set => SetProperty(ref catalogItem, value);
    }

    public string Id => id;

    public string DisplayName => displayName;

    public bool HasTranslationPackage { get; }

    public string Initials => CreateInitials(DisplayName);

    public Brush CoverBrush { get; private set; } = null!;

    public Brush HeroBrush { get; private set; } = null!;

    public ImageSource? CoverImage { get; private set; }

    public ImageSource? HeroImage { get; private set; }

    public GamePresentation Presentation { get; private set; } = null!;

    public void UpdateCatalogItem(GameCatalogItem value)
    {
        if (!HasTranslationPackage)
        {
            throw new InvalidOperationException("Game chưa có package Việt hóa không thể cập nhật trạng thái cài đặt.");
        }

        CatalogItem = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static int GetPaletteIndex(string value)
    {
        var hash = 17;
        foreach (var character in value)
        {
            hash = (hash * 31) + character;
        }

        return Math.Abs(hash % ArtworkPalettes.Length);
    }

    private static string CreateInitials(string displayName)
    {
        var words = displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }

    private static LinearGradientBrush CreateBrush(
        Color start,
        Color end,
        Point startPoint,
        Point endPoint)
    {
        var brush = new LinearGradientBrush(start, end, 35)
        {
            StartPoint = startPoint,
            EndPoint = endPoint
        };
        brush.Freeze();
        return brush;
    }
}
