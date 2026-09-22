using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Ánh xạ game ID sang artwork đã được đóng gói cùng Launcher.
/// </summary>
public sealed class GameArtworkResolver
{
    private static readonly IReadOnlyDictionary<string, (string Hero, string Cover)> AssetPathsByGameId =
        new Dictionary<string, (string Hero, string Cover)>(StringComparer.OrdinalIgnoreCase)
        {
            ["tiny-eden"] = (
                "Resources/Artwork/tiny-eden/tiny-eden-hero-v2.png",
                "Resources/Artwork/tiny-eden/tiny-eden-hero-logo.jpg"),
            ["no-rest-for-the-wicked"] = (
                "Resources/Artwork/no-rest-for-the-wicked/no-rest-for-the-wicked-hero-v2.png",
                "Resources/Artwork/no-rest-for-the-wicked/no-rest-for-the-wicked-cover.jpg"),
            ["phantom-blade-zero"] = (
                "Resources/Artwork/phantom-blade-zero/phantom-blade-zero-hero-v2.png",
                "Resources/Artwork/phantom-blade-zero/phantom-blade-zero-cover.jpg"),
            ["grand-theft-auto-vi"] = (
                "Resources/Artwork/grand-theft-auto-vi/grand-theft-auto-vi-hero-v2.png",
                "Resources/Artwork/grand-theft-auto-vi/grand-theft-auto-vi-cover.jpg"),
            ["dragon-ball-sparking-zero"] = (
                "Resources/Artwork/dragon-ball-sparking-zero/dragon-ball-sparking-zero-hero-v2.png",
                "Resources/Artwork/dragon-ball-sparking-zero/dragon-ball-sparking-zero-cover.jpg"),
            ["borderlands-4"] = (
                "Resources/Artwork/borderlands-4/borderlands-4-hero-v2.png",
                "Resources/Artwork/borderlands-4/borderlands-4-cover.jpg"),
            ["ark-survival-evolved"] = (
                "Resources/Artwork/ark-survival-evolved/ark-survival-evolved-hero-v2.png",
                "Resources/Artwork/ark-survival-evolved/ark-survival-evolved-cover.png"),
            ["red-dead-redemption-2"] = (
                "Resources/Artwork/red-dead-redemption-2/red-dead-redemption-2-hero-v2.png",
                "Resources/Artwork/red-dead-redemption-2/red-dead-redemption-2-cover.jpg"),
            ["elden-ring"] = (
                "Resources/Artwork/elden-ring/elden-ring-hero-v2.png",
                "Resources/Artwork/elden-ring/elden-ring-hero.jpg"),
            ["the-witcher-3-wild-hunt"] = (
                "Resources/Artwork/the-witcher-3-wild-hunt/the-witcher-3-wild-hunt-hero-v2.png",
                "Resources/Artwork/the-witcher-3-wild-hunt/the-witcher-3-wild-hunt-cover.jpg"),
            ["split-fiction"] = (
                "Resources/Artwork/split-fiction/split-fiction-hero-v2.png",
                "Resources/Artwork/split-fiction/split-fiction-cover-thumbnail.png"),
            ["tiebreak-grand-slam-edition"] = (
                "Resources/Artwork/tiebreak-grand-slam-edition/tiebreak-grand-slam-edition-hero-v2.png",
                "Resources/Artwork/tiebreak-grand-slam-edition/tiebreak-grand-slam-edition-cover.jpg"),
            ["cyberpunk-2077"] = (
                "Resources/Artwork/cyberpunk-2077/cyberpunk-2077-hero-v2.png",
                "Resources/Artwork/cyberpunk-2077/cyberpunk-2077-cover.jpg"),
            ["dragon-ball-xenoverse-2"] = (
                "Resources/Artwork/dragon-ball-xenoverse-2/dragon-ball-xenoverse-2-hero-v2.png",
                "Resources/Artwork/dragon-ball-xenoverse-2/dragon-ball-xenoverse-2-cover.jpg"),
            ["god-of-war"] = (
                "Resources/Artwork/god-of-war/god-of-war-hero-v2.png",
                "Resources/Artwork/god-of-war/god-of-war-cover.jpg")
        };
    private readonly Dictionary<string, GameArtwork> artworkCache =
        new(StringComparer.OrdinalIgnoreCase);

    public GameArtwork Resolve(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        if (artworkCache.TryGetValue(gameId, out var cachedArtwork))
        {
            return cachedArtwork;
        }

        if (!AssetPathsByGameId.TryGetValue(gameId, out var paths))
        {
            return new GameArtwork(null, null);
        }

        try
        {
            var heroImage = CreateImage(paths.Hero);
            var coverImage = string.Equals(paths.Hero, paths.Cover, StringComparison.Ordinal)
                ? heroImage
                : CreateImage(paths.Cover);
            var artwork = new GameArtwork(heroImage, coverImage);
            artworkCache[gameId] = artwork;
            return artwork;
        }
        catch (Exception)
        {
            // NOTE(artwork-fallback): artwork chỉ là presentation; catalog và thao tác file
            // phải tiếp tục dùng được nếu một resource đóng gói bị thiếu hoặc lỗi đọc.
            return new GameArtwork(null, null);
        }
    }

    private static ImageSource CreateImage(string resourcePath)
    {
        var assemblyName = typeof(GameArtworkResolver).Assembly.GetName().Name
            ?? throw new InvalidOperationException("Không xác định được assembly chứa artwork.");
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(
            $"pack://application:,,,/{assemblyName};component/{resourcePath}",
            UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
