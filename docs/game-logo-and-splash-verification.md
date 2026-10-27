# Kiểm tra logo game, splash và game xem cuối — 2026-10-27

Đã thực hiện hai phần trong [prompt giao việc](codex-prompts/codex-prompts-game-logo-and-splash.md). Không commit. Giữ nguyên các thay đổi có sẵn của dự án; không chỉnh sửa năm file được dành cho Claude trong prompt.

## Kết quả

- Detail ưu tiên logo PNG trong suốt, có shadow, giới hạn 560 × 140 và dành cột riêng cho nhãn trạng thái. Thiếu logo thì dùng tiêu đề chữ. Hero, Ken Burns và stagger vẫn hoạt động ở Glassy/Tối.
- 14 logo đã nhúng; tất cả là PNG RGBA có alpha từ 0 đến 255. Không chỉnh sửa pixel hoặc cắt ảnh nguồn.
- Splash dùng overlay trong MainWindow, timeline 800ms. Ảnh được chuẩn bị trên worker thread và Freeze trước khi đưa vào UI; cache chỉ được đọc trên UI sau khi preload hoàn thành. Preload bắt đầu cùng khởi tạo preference/cửa sổ, để giải mã artwork không chặn animation.
- `lastSelectedGameId` là field tùy chọn của settings schema v1. Lưu khi đóng, bỏ qua ghi khi ID không đổi, dùng `settings with` và tuần tự hóa với lưu preference. Đọc ID sau khởi tạo settings và chọn lại sau LoadAsync; thiếu ID hoặc ID đã bị xóa thì dùng game đầu tiên.
- Khi lưu selection lỗi, Settings hiện thông báo hiện có; lần đóng tiếp theo vẫn thoát được.

## Logo và nguồn

Người dùng đã đồng ý tải cả 18 URL trước khi tải. Bốn URL trả HTTP 404 được bỏ qua, không thay bằng URL khác. GTA VI dùng chữ theo prompt. Logo lưu tại `src/GameTranslationLauncher.Wpf/Resources/Artwork/<gameId>/<gameId>-logo.png` và có Build Action `Resource` trong project WPF. Vị trí này theo yêu cầu rõ trong prompt, là ngoại lệ của quy ước artwork nguồn trong `images/`.

| Game / gameId | AppID | URL đã duyệt | Kết quả |
| --- | ---: | --- | --- |
| Tiny Eden / `tiny-eden` | 3375110 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/3375110/logo.png) | Nhúng PNG 640 × 360 |
| No Rest for the Wicked / `no-rest-for-the-wicked` | 1371980 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1371980/logo.png) | Nhúng PNG 640 × 360 |
| Phantom Blade Zero / `phantom-blade-zero` | 4115450 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/4115450/logo.png) | 404, dùng chữ |
| DRAGON BALL: Sparking! ZERO / `dragon-ball-sparking-zero` | 1790600 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1790600/logo.png) | Nhúng PNG 640 × 360 |
| Borderlands 4 / `borderlands-4` | 1285190 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1285190/logo.png) | Nhúng PNG 640 × 360 |
| ARK: Survival Evolved / `ark-survival-evolved` | 346110 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/346110/logo.png) | Nhúng PNG 561 × 434 |
| Red Dead Redemption 2 / `red-dead-redemption-2` | 1174180 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1174180/logo.png) | Nhúng PNG 640 × 360 |
| ELDEN RING / `elden-ring` | 1245620 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1245620/logo.png) | Nhúng PNG 640 × 98 |
| The Witcher 3: Wild Hunt / `the-witcher-3-wild-hunt` | 292030 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/292030/logo.png) | Nhúng PNG 640 × 360 |
| Split Fiction / `split-fiction` | 2001120 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/2001120/logo.png) | Nhúng PNG 640 × 360 |
| TIEBREAK: Grand Slam Edition / `tiebreak-grand-slam-edition` | 2264340 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/2264340/logo.png) | Nhúng PNG 640 × 360 |
| Cyberpunk 2077 / `cyberpunk-2077` | 1091500 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1091500/logo.png) | Nhúng PNG 640 × 360 |
| DRAGON BALL XENOVERSE 2 / `dragon-ball-xenoverse-2` | 454650 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/454650/logo.png) | Nhúng PNG 640 × 360 |
| God of War / `god-of-war` | 1593500 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/1593500/logo.png) | Nhúng PNG 640 × 360 |
| Forza Horizon 6 / `forza-horizon-6` | 2483190 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/2483190/logo.png) | 404, dùng chữ |
| Black Myth: Wukong / `black-myth-wukong` | 2358720 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/2358720/logo.png) | Nhúng PNG 640 × 360 |
| Resonance: A Plague Tale Legacy / `resonance-a-plague-tale-legacy` | 2713000 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/2713000/logo.png) | 404, dùng chữ |
| Crimson Moon / `crimson-moon` | 4317690 | [logo.png](https://cdn.cloudflare.steamstatic.com/steam/apps/4317690/logo.png) | 404, dùng chữ |
| Grand Theft Auto VI / `grand-theft-auto-vi` | — | Bỏ qua theo prompt | Dùng chữ |

Catalog cạnh App trên máy kiểm tra có 16 game hiển thị và 12 logo khả dụng. Tiny Eden và No Rest for the Wicked có logo đã nhúng nhưng chưa có package trong catalog App đang dùng, nên không có tab để chụp trong lần này.

## Build và test

- `dotnet build GameTranslationLauncher.sln --no-restore`: thành công, **0 warning, 0 error**.
- `dotnet test GameTranslationLauncher.sln --no-restore`: **60/60 pass**, gồm Application 24 và Infrastructure 36.
- Thêm 5 test settings: file cũ thiếu field, roundtrip giữ field/preference, từ chối ID sai định dạng, lưu selection giữ preference/installation, ID không đổi không ghi. Mở rộng các test lưu preference/chọn installation để giữ selection.
- Diff check không có lỗi whitespace. Các lượt build bị khóa file khi một phiên Launcher khác đang mở đã được chạy lại sau khi đóng app; kết quả trên là lượt thành công cuối.

## Đo khởi động

Chạy executable thật `App/TGLauncher.exe`. Mốc trước là binary có sẵn trước khi build thay đổi. Mốc catalog bằng UI Automation không xét opacity của lớp phủ, nên tách khỏi mốc hiển thị đo bằng probe WPF tạm thời nạp vào cùng executable.

| Phép đo | Trước | Sau |
| --- | --- | --- |
| Catalog xuất hiện trong UI Automation, 3 lượt | 1614 / 1578 / 1552 ms | 1176 / 1042 / 1062 ms |
| Catalog và giao diện chính bắt đầu hiện rõ (`MainContent.Opacity > 0.2`), probe các tình huống hợp lệ | Không có probe baseline | Khoảng 1456–1508 ms |
| Overlay Collapsed, giao diện opacity 1 | Không có splash | Khoảng 1640–1664 ms |

Timeline splash là 800ms từ lúc bắt đầu animation. Không cộng thêm exit animation sau khi catalog tải xong. Số đo gồm khởi động process, chỉ phản ánh máy này; không phải benchmark máy yếu. Các JSON đo được lưu trong [thư mục bằng chứng](verification/game-logo-and-splash/).

## Kiểm tra runtime

- Đã chọn game thật qua UI Automation, chụp Witcher 3 trên nền sáng, Black Myth: Wukong trên nền xám tối và GTA VI không logo. Kiểm tra cả Glassy/Tối, 1100 × 720 và phóng to (ảnh native window 2574 × 1454, gồm mép cửa sổ); logo/tiêu đề và badge không chồng nhau.
- Probe trong executable thật chọn Black Myth: Wukong, đóng app, kiểm tra settings được ghi, mở lại và xác nhận `SelectedGame.Id = black-myth-wukong`.
- Thiếu `lastSelectedGameId` hoặc ID không còn trong catalog: chọn Witcher 3 là game đầu tiên của catalog này.
- Windows animation tắt: overlay luôn Collapsed, giao diện opacity 1, catalog sẵn sàng khoảng 973ms; đã khôi phục thiết lập Windows ngay sau lượt kiểm tra.
- Catalog báo lỗi do settings không hợp lệ: overlay vẫn Collapsed sau timeline, UI hiện lỗi và app đóng bình thường.
- Sau splash: `IsHitTestVisible=False`, `Visibility=Collapsed`, `MainContent.Opacity=1`.
- Đã sao lưu dữ liệu local trước QA và khôi phục lại **9 file, SHA-256 và đường dẫn khớp toàn bộ**. Bản state phát sinh trong QA được giữ ở thư mục tạm, không đưa vào repository.

### Ảnh kiểm tra

Witcher 3, Glassy, 1100 × 720:

![Witcher 3 Glassy](verification/game-logo-and-splash/glass-witcher-1100x720.png)

Black Myth: Wukong, Tối, 1100 × 720:

![Wukong Tối](verification/game-logo-and-splash/dark-wukong-1100x720.png)

GTA VI không logo, Glassy, 1100 × 720:

![GTA VI fallback](verification/game-logo-and-splash/glass-gta-1100x720.png)

Splash ở các khung 150 / 350 / 750 / 1000ms sau khi tìm thấy cửa sổ native (mốc này có sai số lấy mẫu):

![Splash 150ms](verification/game-logo-and-splash/glass-splash-150.png)
![Splash 350ms](verification/game-logo-and-splash/glass-splash-350.png)
![Splash 750ms](verification/game-logo-and-splash/glass-splash-750.png)
![Splash 1000ms](verification/game-logo-and-splash/glass-splash-1000.png)

Các ảnh phóng to và toàn bộ ảnh Tối/Glassy nằm trong [thư mục bằng chứng](verification/game-logo-and-splash/).

## File code/contract/test đã sửa hoặc thêm trong công việc này

- `src/GameTranslationLauncher.Domain/Settings/LauncherSettings.cs`
- `src/GameTranslationLauncher.Application/Settings/SaveLastSelectedGameUseCase.cs` (mới)
- `src/GameTranslationLauncher.Infrastructure/Persistence/Serialization/LauncherSettingsDocument.cs`
- `src/GameTranslationLauncher.Infrastructure/Persistence/LauncherSettingsMapper.cs`
- `src/GameTranslationLauncher.Infrastructure/Persistence/LauncherSettingsDocumentValidator.cs`
- `contracts/launcher-settings.schema.json`
- `src/GameTranslationLauncher.Wpf/Services/GameArtwork.cs`
- `src/GameTranslationLauncher.Wpf/Services/GameArtworkResolver.cs`
- `src/GameTranslationLauncher.Wpf/ViewModels/GameLibraryItemViewModel.cs`
- `src/GameTranslationLauncher.Wpf/ViewModels/GameDetailsViewModel.cs`
- `src/GameTranslationLauncher.Wpf/ViewModels/GameLibraryViewModel.cs` (chỉ thêm ID đã nhớ tại vị trí chọn game sau load)
- `src/GameTranslationLauncher.Wpf/ViewModels/LauncherSettingsViewModel.cs`
- `src/GameTranslationLauncher.Wpf/Views/GameDetailsView.xaml`
- `src/GameTranslationLauncher.Wpf/GameTranslationLauncher.Wpf.csproj` (nhúng resource logo)
- `src/GameTranslationLauncher.Wpf/Composition/LauncherCompositionRoot.cs`
- `src/GameTranslationLauncher.Wpf/App.xaml.cs`
- `src/GameTranslationLauncher.Wpf/MainWindow.xaml`
- `src/GameTranslationLauncher.Wpf/MainWindow.xaml.cs`
- `tests/GameTranslationLauncher.Application.Tests/SaveLastSelectedGameUseCaseTests.cs` (mới)
- `tests/GameTranslationLauncher.Application.Tests/SaveLauncherPreferencesUseCaseTests.cs`
- `tests/GameTranslationLauncher.Application.Tests/SelectGameInstallationUseCaseTests.cs`
- `tests/GameTranslationLauncher.Infrastructure.Tests/JsonLauncherSettingsRepositoryTests.cs`

Tài liệu: `DESIGN.md`, `WORKFLOW.md`, báo cáo này và folder bằng chứng. Asset thêm: 14 file logo theo gameId trong bảng trên. Các script/probe/build tạm nằm ngoài workspace, tại `%TEMP%/tg-logo-splash-20261027`.

## Giới hạn còn lại

- Bốn URL 404 vẫn dùng chữ; không tải nguồn thay thế chưa được duyệt.
- Chưa benchmark máy yếu, màn hình DPI cao hoặc xác minh trực quan toàn bộ 14 logo.
- Các tình huống WPF được kiểm tra bằng app thật và probe tạm; chưa bổ sung một project WPF UI test thường trực.
- Lưu selection và lưu preference được tuần tự hóa trong SettingsViewModel; không thay đổi cơ chế đồng thời của các use case installation hiện hữu.
