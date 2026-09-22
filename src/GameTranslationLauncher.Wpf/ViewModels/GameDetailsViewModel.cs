using System.Windows;
using System.Windows.Media;
using GameTranslationLauncher.Domain.Installation;

namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Cung cấp thông tin và trạng thái presentation của game đang được chọn.
/// </summary>
public sealed class GameDetailsViewModel : ObservableObject
{
    private static readonly Brush EmptyHeroBrush = CreateEmptyHeroBrush();
    private GameLibraryItemViewModel? game;
    private LauncherApplicationText text = LauncherTextFactory.Create("vi").Application;
    private string actionMessage = string.Empty;

    public string DisplayName => game?.DisplayName.ToUpperInvariant() ?? text.EmptyLibraryTitle;

    public double DisplayNameFontSize => DisplayName.Length > 20 ? 38 : 46;

    public string Subtitle => game is null
        ? text.EmptyLibrarySubtitle
        : game.HasTranslationPackage
            ? string.Format(text.TranslationVersionFormat, game.CatalogItem!.AvailablePackage.Package.PackageVersion)
            : text.TranslationUnavailable;

    public string Description => game is null
        ? text.EmptyLibraryDescription
        : game.Presentation.GetDescription(text.LanguageCode);

    public string PrerequisiteNotice => game?.Presentation.GetPrerequisiteNotice(text.LanguageCode) ?? string.Empty;

    public bool HasPrerequisiteNotice => !string.IsNullOrWhiteSpace(PrerequisiteNotice);

    public string StatusGlyph => Status switch
    {
        InstallationStatus.Installed => "✓",
        InstallationStatus.UpdateAvailable => "↻",
        InstallationStatus.NotInstalled => "↓",
        InstallationStatus.Damaged => "!",
        InstallationStatus.Conflict => "!",
        _ => game?.HasTranslationPackage == false ? "⌛" : HasGameRoot ? "?" : "⌂"
    };

    public string StatusLabel => Status switch
    {
        InstallationStatus.Installed => text.StatusInstalled,
        InstallationStatus.UpdateAvailable => text.StatusUpdateAvailable,
        InstallationStatus.NotInstalled => text.StatusNotInstalled,
        InstallationStatus.Damaged => text.StatusDamaged,
        InstallationStatus.Conflict => text.StatusConflict,
        _ => game?.HasTranslationPackage == false ? text.StatusTranslationUnavailable : HasGameRoot ? text.StatusNotChecked : text.StatusFolderNotSelected
    };

    public string SupportText => game?.Presentation.SupportText ?? "—";

    public string LanguageText => game?.CatalogItem?.AvailablePackage.Package.TargetLanguage.Equals(
        "vi", StringComparison.OrdinalIgnoreCase) == true
            ? text.VietnameseLanguage
            : game?.CatalogItem?.AvailablePackage.Package.TargetLanguage ?? "—";

    public string VerifiedDateText => game?.CatalogItem?.AvailablePackage.Package.LastVerifiedWorkingInGame
        .ToString("dd/MM/yyyy") ?? "—";

    public string PackageSizeText => game is null || !game.HasTranslationPackage
        ? "—"
        : FormatSize(GetPackageSizeBytes(game));

    public string FilesText => game is null || !game.HasTranslationPackage
        ? "—"
        : string.Format(text.PackageFilesFormat, GetPackageFileCount(game));

    public string TranslationStatusText => game is null
        ? "—"
        : game.HasTranslationPackage ? text.TranslationComplete : text.TranslationUnavailable;

    public Brush HeroBrush => game?.HeroBrush ?? EmptyHeroBrush;

    public ImageSource? HeroImage => game?.HeroImage;

    public string PrimaryActionLabel => Status switch
    {
        InstallationStatus.Installed => text.PrimaryLaunchGame,
        InstallationStatus.UpdateAvailable => text.PrimaryUpdate,
        InstallationStatus.NotInstalled => text.PrimaryInstallTranslation,
        InstallationStatus.Damaged => text.PrimaryVerifyAgain,
        InstallationStatus.Conflict => text.PrimaryViewDetails,
        _ => HasGameRoot ? text.PrimaryCheckGameStatus : text.PrimaryChooseGameFolder
    };

    public string SecondaryActionLabel => Status switch
    {
        InstallationStatus.Installed => text.SecondaryCheckUpdates,
        InstallationStatus.UpdateAvailable => text.PrimaryLaunchGame,
        InstallationStatus.NotInstalled => text.SecondaryOpenGameFolder,
        InstallationStatus.Damaged => text.SecondaryUninstallTranslation,
        InstallationStatus.Conflict => text.SecondaryOpenFolder,
        _ => HasGameRoot ? text.SecondaryOpenGameFolder : string.Empty
    };

    public string TertiaryActionLabel => Status switch
    {
        InstallationStatus.Installed => text.SecondaryOpenFolder,
        InstallationStatus.UpdateAvailable => text.SecondaryOpenFolder,
        InstallationStatus.Damaged => text.SecondaryOpenFolder,
        _ => string.Empty
    };

    public bool HasSecondaryAction => !string.IsNullOrEmpty(SecondaryActionLabel);

    public bool HasTertiaryAction => !string.IsNullOrEmpty(TertiaryActionLabel);

    public bool HasTranslationActions => game?.HasTranslationPackage == true;

    public bool HasGameRoot => game?.CatalogItem?.GameRoot is not null;

    public string FolderActionLabel => text.FolderAction;

    public string ActionMessage
    {
        get => actionMessage;
        private set => SetProperty(ref actionMessage, value);
    }

    private InstallationStatus Status => game?.CatalogItem?.Status ?? InstallationStatus.Unknown;

    public void Show(GameLibraryItemViewModel? selectedGame)
    {
        game = selectedGame;
        ActionMessage = string.Empty;
        RefreshPresentation();
    }

    public void ApplyText(LauncherApplicationText value)
    {
        text = value ?? throw new ArgumentNullException(nameof(value));
        RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        foreach (var propertyName in new[]
                 {
                     nameof(DisplayName), nameof(Subtitle), nameof(Description), nameof(StatusGlyph),
                     nameof(DisplayNameFontSize), nameof(PrerequisiteNotice), nameof(HasPrerequisiteNotice),
                     nameof(StatusLabel), nameof(SupportText), nameof(LanguageText), nameof(VerifiedDateText),
                     nameof(PackageSizeText), nameof(FilesText), nameof(TranslationStatusText), nameof(HeroBrush), nameof(HeroImage), nameof(PrimaryActionLabel),
                     nameof(SecondaryActionLabel), nameof(TertiaryActionLabel), nameof(HasSecondaryAction),
                     nameof(HasTertiaryAction), nameof(HasTranslationActions), nameof(HasGameRoot), nameof(FolderActionLabel)
                 })
        {
            OnPropertyChanged(propertyName);
        }
    }

    private static long GetPackageSizeBytes(GameLibraryItemViewModel item) =>
        item.CatalogItem!.AvailablePackage.Package.PayloadFiles.Sum(file => file.SizeBytes)
        + item.CatalogItem.AvailablePackage.Package.Prerequisites.Sum(prerequisite =>
            prerequisite.Files.Sum(file => file.SizeBytes));

    private static int GetPackageFileCount(GameLibraryItemViewModel item) =>
        item.CatalogItem!.AvailablePackage.Package.PayloadFiles.Count
        + item.CatalogItem.AvailablePackage.Package.Prerequisites.Sum(prerequisite => prerequisite.Files.Count);

    private static string FormatSize(long sizeBytes) => sizeBytes switch
    {
        <= 0 => "—",
        < 1024 * 1024 => $"{Math.Ceiling(sizeBytes / 1024d):0} KB",
        < 1024L * 1024 * 1024 => $"{sizeBytes / (1024d * 1024):0.0} MB",
        _ => $"{sizeBytes / (1024d * 1024 * 1024):0.0} GB"
    };

    private static Brush CreateEmptyHeroBrush()
    {
        var brush = new LinearGradientBrush(
            Color.FromRgb(21, 22, 25),
            Color.FromRgb(8, 9, 10),
            new Point(0, 0),
            new Point(1, 1));
        brush.Freeze();
        return brush;
    }
}
