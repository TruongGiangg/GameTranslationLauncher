# Prompt giao Codex — ý 3 (logo game) và ý 6 (màn hình mở app)

Gửi hai prompt dưới đây thành **hai phiên Codex riêng**. Chạy lần lượt hoặc song song đều được; hai phần không sửa chung file nào.

Trong lúc Codex làm, Claude đang làm lại hộp thoại tiến trình cài đặt (ý 5). Các file sau thuộc phần việc của Claude, Codex không được sửa:

- `src/GameTranslationLauncher.Wpf/Views/GameLibraryView.xaml`
- `src/GameTranslationLauncher.Wpf/ViewModels/GameOperationViewModel.cs`
- `src/GameTranslationLauncher.Wpf/ViewModels/LauncherOperationText.cs`
- `src/GameTranslationLauncher.Wpf/ViewModels/LauncherTextFactory.cs`
- `src/GameTranslationLauncher.Wpf/Resources/LauncherTheme.xaml`

---

## Prompt 1 — Logo game thay chữ tiêu đề

```text
Bạn làm việc trong repo E:\SourceForDev\GameTranslationProject\Launcher. Đây là TG Launcher: app WPF .NET 10 (net10.0-windows), kiến trúc Clean (Domain/Application/Infrastructure/Wpf), MVVM. Trước khi sửa, đọc DESIGN.md, WORKFLOW.md và các file liên quan.

Mục tiêu: ở màn hình chi tiết game, thay dòng chữ tiêu đề lớn (ví dụ "THE WITCHER 3: WILD HUNT") bằng logo PNG nền trong suốt của game đó. Game nào không có logo thì giữ nguyên tiêu đề chữ như hiện tại.

Bối cảnh trong code:
- Ảnh game được nhúng trong app tại src/GameTranslationLauncher.Wpf/Resources/Artwork/<gameId>/ và ánh xạ trong Services/GameArtworkResolver.cs (record GameArtwork(HeroImage, CoverImage)).
- Tiêu đề hiển thị ở Views/GameDetailsView.xaml (TextBlock trong Grid "TitleRow", binding Details.DisplayName, cỡ chữ Details.DisplayNameFontSize). Code-behind GameDetailsView.xaml.cs có hiệu ứng hiện lệch nhau (stagger) dựa trên TitleTargetUpdated; logo phải tham gia hiệu ứng này giống tiêu đề chữ.
- Danh sách game và gameId: GamePresentationResolver.cs và các thư mục trong Resources/Artwork.

Yêu cầu:
1. Logo lấy từ Steam CDN, dạng https://cdn.cloudflare.steamstatic.com/steam/apps/<appid>/logo.png. Tự tra appid của từng game. PHẢI hỏi tôi và được đồng ý trước khi tải bất kỳ file nào. Khi hỏi, liệt kê tên game, appid và URL. Game không có trên Steam (ví dụ GTA VI) thì bỏ qua, dùng chữ.
2. Lưu logo vào Resources/Artwork/<gameId>/<gameId>-logo.png, nhúng vào app đúng cách đang dùng cho hero/cover (kiểm tra Build Action trong .csproj).
3. Mở rộng GameArtwork/GameArtworkResolver để có thêm LogoImage (null khi không có). Đưa giá trị này qua ViewModel (GameLibraryItemViewModel/GameDetailsViewModel) theo đúng cách HeroImage đang được đưa qua.
4. Trong GameDetailsView.xaml: khi có logo thì hiện Image (MaxHeight khoảng 140, MaxWidth khoảng 560, căn trái, Stretch=Uniform, có DropShadowEffect nhẹ để đọc được trên ảnh sáng) thay cho TextBlock tiêu đề; khi không có thì hiện TextBlock như cũ. Nhãn trạng thái bên phải tiêu đề không được bị đè. Đặt AutomationProperties.Name của Image bằng tên game.
5. Giữ nguyên hiệu ứng chuyển ảnh, Ken Burns và stagger hiện có. Áp dụng cho cả giao diện Glassy và Tối.
6. Không sửa các file sau (đang có người khác sửa): Views/GameLibraryView.xaml, ViewModels/GameOperationViewModel.cs, ViewModels/LauncherOperationText.cs, ViewModels/LauncherTextFactory.cs, Resources/LauncherTheme.xaml.

Kiểm tra:
- dotnet build GameTranslationLauncher.sln --no-restore: 0 warning, 0 error. Build xuất thẳng ra App\TGLauncher.exe; nếu Launcher đang chạy thì build sẽ lỗi khoá file, hãy đóng app trước.
- dotnet test GameTranslationLauncher.sln --no-restore: tất cả phải qua.
- Chạy App\TGLauncher.exe, chụp màn hình ít nhất 3 game: game có logo trên ảnh sáng (Witcher 3), game có logo trên ảnh tối, và game không có logo. Kiểm tra cửa sổ 1100x720 và phóng to.
- Cập nhật DESIGN.md (mục sidebar/detail) mô tả logo.

Kết thúc: báo danh sách file đã sửa/thêm, game nào có/không có logo, ảnh chụp kiểm tra, và các rủi ro còn lại. Không commit.
```

---

## Prompt 2 — Màn hình mở app (splash) và nhớ game xem lần cuối

```text
Bạn làm việc trong repo E:\SourceForDev\GameTranslationProject\Launcher. Đây là TG Launcher: app WPF .NET 10 (net10.0-windows), kiến trúc Clean (Domain/Application/Infrastructure/Wpf), MVVM, test bằng MSTest. Trước khi sửa, đọc DESIGN.md, WORKFLOW.md và các file liên quan.

Mục tiêu:
A. Khi mở Launcher, logo TG hiện ở giữa cửa sổ, sáng dần lên rồi thu nhỏ/mờ đi trong lúc giao diện chính hiện ra. Tổng thời gian khoảng 0,8–1,2 giây.
B. Launcher nhớ game người dùng đang xem khi đóng app và mở lại đúng game đó.

Bối cảnh trong code:
- Cửa sổ chính: src/GameTranslationLauncher.Wpf/MainWindow.xaml(.cs). Khởi động và composition: App.xaml.cs, Composition/LauncherCompositionRoot.cs.
- Logo: /Resources/tg-minimal-logo-red.png (đã dùng ở sidebar trong GameLibraryView.xaml).
- Catalog được tải bất đồng bộ (GameLibraryViewModel.LoadAsync). Hiện app mất khoảng 1,5 giây từ lúc mở tới lúc danh sách game hiện.
- Settings: Domain/Settings/LauncherSettings.cs (record, đã có DisplayLanguage, AccentTheme, SurfaceTheme); lưu qua Application/Settings/SaveLauncherPreferencesUseCase.cs; Infrastructure có LauncherSettingsDocument, Mapper, Validator; schema JSON ở contracts/launcher-settings.schema.json. Xem cách SurfaceTheme được thêm gần đây để làm theo đúng mẫu đó.
- Lỗi đã từng gặp: một use case ghi settings bằng object mới làm mất preference khác. Luôn dùng `settings with { ... }`.

Yêu cầu:
1. Splash là một lớp phủ trong chính MainWindow (không tạo cửa sổ mới, không dùng SplashScreen ảnh tĩnh). Không được làm thời gian tới lúc danh sách game hiện chậm thêm: chạy hiệu ứng song song với việc tải catalog, và ẩn splash khi đã đủ thời gian tối thiểu VÀ catalog đã tải xong (hoặc lỗi).
2. Tôn trọng SystemParameters.ClientAreaAnimation: khi Windows tắt animation thì không hiện splash.
3. Splash phải đẹp ở cả giao diện Glassy và Tối, không chặn thao tác sau khi đã ẩn (IsHitTestVisible=False, Visibility=Collapsed khi xong).
4. Nhớ game: thêm field tuỳ chọn lastSelectedGameId vào settings (Domain record, document, mapper, validator, schema). Thiếu field hoặc gameId không còn trong catalog thì chọn game đầu tiên như hiện tại. Ghi settings khi người dùng đổi game, nhưng không ghi liên tục mỗi lần đổi: debounce khoảng 500ms hoặc ghi khi đóng app. Không làm mất các preference khác.
5. Viết test MSTest cho phần settings: đọc file thiếu field, đọc/ghi lại giữ nguyên field, giữ các preference khác khi lưu lastSelectedGameId.
6. Không sửa các file sau (đang có người khác sửa): Views/GameLibraryView.xaml, ViewModels/GameOperationViewModel.cs, ViewModels/LauncherOperationText.cs, ViewModels/LauncherTextFactory.cs, Resources/LauncherTheme.xaml. Nếu bắt buộc phải sửa GameLibraryViewModel.cs để chọn game đã nhớ thì chỉ sửa đúng chỗ chọn SelectedGame sau LoadAsync.
7. Trước khi chạy thử, sao lưu %LOCALAPPDATA%\GameTranslationLauncher và khôi phục sau khi xong.

Kiểm tra:
- dotnet build GameTranslationLauncher.sln --no-restore: 0 warning, 0 error. Build xuất thẳng ra App\TGLauncher.exe; nếu Launcher đang chạy thì build sẽ lỗi khoá file, hãy đóng app trước.
- dotnet test GameTranslationLauncher.sln --no-restore: tất cả phải qua.
- Chạy App\TGLauncher.exe: chụp chuỗi khung hình của splash, đo thời gian tới khi danh sách game hiện (phải không chậm hơn khoảng 1,5 giây hiện tại), chọn một game, đóng và mở lại để xác nhận đúng game đó được chọn.
- Cập nhật DESIGN.md và WORKFLOW.md.

Kết thúc: báo danh sách file đã sửa/thêm, số test, số đo thời gian khởi động trước/sau, và các rủi ro còn lại. Không commit.
```
