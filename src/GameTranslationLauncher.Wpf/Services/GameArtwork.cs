using System.Windows.Media;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Các asset presentation của một game, độc lập với package và thao tác cài đặt.
/// </summary>
public sealed record GameArtwork(ImageSource? HeroImage, ImageSource? CoverImage);
