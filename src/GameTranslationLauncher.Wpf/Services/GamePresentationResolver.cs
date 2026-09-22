namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Ánh xạ game ID sang nội dung presentation tĩnh đã được đóng gói cùng Launcher.
/// </summary>
public sealed class GamePresentationResolver
{
    private static readonly GamePresentation DefaultPresentation = new(
        "—",
        "Bản Việt hóa được quản lý an toàn qua package local.",
        "This Vietnamese translation is safely managed through a local package.",
        string.Empty,
        string.Empty);

    private static readonly IReadOnlyDictionary<string, GamePresentation> PresentationsByGameId =
        new Dictionary<string, GamePresentation>(StringComparer.OrdinalIgnoreCase)
        {
            ["tiny-eden"] = new(
                "Unreal Engine",
                "Tiny Eden là game mô phỏng làm vườn góc nhìn thứ nhất giữa thành phố tương lai. " +
                "Trồng cây, nấu ăn, bán nông sản và mở rộng căn hộ thành khu vườn trên sân thượng.",
                "Tiny Eden is a first-person gardening simulation set in a futuristic city. " +
                "Grow crops, cook, sell produce, and turn your apartment into a rooftop garden.",
                string.Empty,
                string.Empty),
            ["no-rest-for-the-wicked"] = new(
                "Unity IL2CPP · MelonLoader 0.7.2",
                "No Rest for the Wicked là action RPG của Moon Studios, đưa bạn đến Isola Sacra — " +
                "một thế giới được thiết kế thủ công với chiến đấu chính xác, xây dựng nhân vật và co-op tối đa bốn người chơi.",
                "No Rest for the Wicked is Moon Studios' action RPG set in Isola Sacra, a handcrafted world " +
                "with precise combat, character building, and co-op for up to four players.",
                "Yêu cầu MelonLoader 0.7.2. Lần chạy đầu tiên sau khi cài có thể lâu hơn bình thường và hiện cửa sổ console để tạo dữ liệu interop.",
                "Requires MelonLoader 0.7.2. The first launch after installation may take longer and show a console window while interop data is generated.")
        };

    private static readonly IReadOnlyList<UntranslatedGameDefinition> UntranslatedGames =
    [
        new(
            "phantom-blade-zero",
            "Phantom Blade Zero",
            new GamePresentation(
                "Đang chuẩn bị",
                "Phantom Blade Zero là action RPG võ hiệp của S-GAME, kết hợp phong cách Wuxia, fantasy u tối và những trận chiến tốc độ cao. Bản Việt hóa chưa được phát hành.",
                "Phantom Blade Zero is S-GAME's Wuxia action RPG, combining dark fantasy with fast-paced martial-arts combat. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "grand-theft-auto-vi",
            "Grand Theft Auto VI",
            new GamePresentation(
                "Đang chuẩn bị",
                "Grand Theft Auto VI đưa người chơi đến Leonida và Vice City, theo chân Jason và Lucia trong một câu chuyện tội phạm quy mô lớn. Bản Việt hóa chưa được phát hành.",
                "Grand Theft Auto VI takes players to Leonida and Vice City, following Jason and Lucia in a large-scale crime story. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "dragon-ball-sparking-zero",
            "DRAGON BALL: Sparking! ZERO",
            new GamePresentation(
                "Đang chuẩn bị",
                "DRAGON BALL: Sparking! ZERO đưa lối chơi Budokai Tenkaichi trở lại với các trận chiến 3D tốc độ cao và dàn chiến binh phong phú. Bản Việt hóa chưa được phát hành.",
                "DRAGON BALL: Sparking! ZERO revives Budokai Tenkaichi gameplay with fast 3D battles and a broad roster of fighters. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "borderlands-4",
            "Borderlands 4",
            new GamePresentation(
                "Đang chuẩn bị",
                "Borderlands 4 đưa người chơi đến Kairos trong vai Vault Hunter, chiến đấu với hàng tỷ vũ khí, kỹ năng đặc biệt và khả năng di chuyển linh hoạt để chống lại Timekeeper. Bản Việt hóa chưa được phát hành.",
                "Borderlands 4 sends players to Kairos as a Vault Hunter, fighting with billions of weapons, Action Skills, and dynamic movement against the Timekeeper. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "ark-survival-evolved",
            "ARK: Survival Evolved",
            new GamePresentation(
                "Đang chuẩn bị",
                "ARK: Survival Evolved là game sinh tồn thế giới mở, nơi người chơi chế tạo, xây dựng căn cứ và thuần hóa sinh vật tiền sử để tồn tại trên hòn đảo bí ẩn. Bản Việt hóa chưa được phát hành.",
                "ARK: Survival Evolved is an open-world survival game where players craft, build bases, and tame prehistoric creatures to survive on a mysterious island. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "red-dead-redemption-2",
            "Red Dead Redemption 2",
            new GamePresentation(
                "Đang chuẩn bị",
                "Red Dead Redemption 2 là chuyến phiêu lưu miền viễn Tây của Rockstar Games, theo chân Arthur Morgan và băng Van der Linde trong những năm cuối của thời đại cao bồi. Bản Việt hóa chưa được phát hành.",
                "Red Dead Redemption 2 is Rockstar Games' western epic following Arthur Morgan and the Van der Linde gang during the final years of the outlaw era. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "elden-ring",
            "ELDEN RING",
            new GamePresentation(
                "Đang chuẩn bị",
                "ELDEN RING là action RPG fantasy của FromSoftware, đưa Tarnished bước vào Lands Between để khám phá thế giới rộng lớn, chiến đấu và trở thành Elden Lord. Bản Việt hóa chưa được phát hành.",
                "ELDEN RING is FromSoftware's fantasy action RPG, sending the Tarnished across the Lands Between to explore, fight, and become Elden Lord. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "the-witcher-3-wild-hunt",
            "The Witcher 3: Wild Hunt",
            new GamePresentation(
                "Đang chuẩn bị",
                "The Witcher 3: Wild Hunt là action RPG thế giới mở của CD PROJEKT RED, theo chân thợ săn quái vật Geralt of Rivia trong hành trình tìm Ciri. Bản Việt hóa chưa được phát hành.",
                "The Witcher 3: Wild Hunt is CD PROJEKT RED's open-world action RPG following monster slayer Geralt of Rivia on his search for Ciri. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "split-fiction",
            "Split Fiction",
            new GamePresentation(
                "Đang chuẩn bị",
                "Split Fiction là phiêu lưu co-op của Hazelight, nơi Mio và Zoe phải hợp tác để thoát khỏi những thế giới khoa học viễn tưởng và fantasy do chính họ tạo ra. Bản Việt hóa chưa được phát hành.",
                "Split Fiction is Hazelight's co-op adventure where Mio and Zoe must work together to escape sci-fi and fantasy worlds born from their own stories. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "tiebreak-grand-slam-edition",
            "TIEBREAK: Grand Slam Edition",
            new GamePresentation(
                "Đang chuẩn bị",
                "TIEBREAK: Grand Slam Edition là game tennis chính thức của ATP và WTA, cho phép người chơi thi đấu qua mùa giải cùng những tay vợt chuyên nghiệp nổi tiếng. Bản Việt hóa chưa được phát hành.",
                "TIEBREAK: Grand Slam Edition is the official ATP and WTA tennis game, letting players compete through a season with well-known professional athletes. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "cyberpunk-2077",
            "Cyberpunk 2077",
            new GamePresentation(
                "Đang chuẩn bị",
                "Cyberpunk 2077 là action RPG thế giới mở của CD PROJEKT RED, đưa người chơi đến Night City trong vai V, một lính đánh thuê theo đuổi bí mật của sự bất tử. Bản Việt hóa chưa được phát hành.",
                "Cyberpunk 2077 is CD PROJEKT RED's open-world action RPG set in Night City, where players take the role of V, a mercenary pursuing the secret to immortality. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "dragon-ball-xenoverse-2",
            "DRAGON BALL XENOVERSE 2",
            new GamePresentation(
                "Đang chuẩn bị",
                "DRAGON BALL XENOVERSE 2 là game hành động nhập vai, đưa người chơi du hành qua các thời điểm then chốt để bảo vệ dòng thời gian của thế giới Dragon Ball. Bản Việt hóa chưa được phát hành.",
                "DRAGON BALL XENOVERSE 2 is an action RPG that sends players across pivotal moments to protect the Dragon Ball timeline. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "god-of-war",
            "God of War",
            new GamePresentation(
                "Đang chuẩn bị",
                "God of War là game hành động phiêu lưu của Santa Monica Studio, theo chân Kratos và Atreus trong hành trình qua thế giới thần thoại Bắc Âu. Bản Việt hóa chưa được phát hành.",
                "God of War is Santa Monica Studio's action-adventure following Kratos and Atreus on their journey through the world of Norse mythology. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty)),
        new(
            "forza-horizon-6",
            "Forza Horizon 6",
            new GamePresentation(
                "Đang chuẩn bị",
                "Forza Horizon 6 là game đua xe thế giới mở của Playground Games, cho phép người chơi tự do khám phá bản đồ, sưu tầm hàng trăm mẫu xe và tham gia các sự kiện đua đa dạng. Bản Việt hóa chưa được phát hành.",
                "Forza Horizon 6 is Playground Games' open-world racing game, letting players freely explore the map, collect hundreds of cars, and take part in a wide variety of racing events. A Vietnamese translation has not been released yet.",
                string.Empty,
                string.Empty))
    ];

    public GamePresentation Resolve(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        return PresentationsByGameId.TryGetValue(gameId, out var presentation)
            ? presentation
            : DefaultPresentation;
    }

    /// <summary>
    /// Trả về các game đã có tab giới thiệu nhưng chưa đủ dữ liệu để tạo package cài đặt.
    /// </summary>
    public IReadOnlyList<UntranslatedGameDefinition> GetUntranslatedGames() => UntranslatedGames;
}
