# Intro hai rồng TG và icon đỏ satin — 2026-10-27

## Bản hiện hành — căn giữa và làm sạch viền

- `MediaElement` đổi từ `UniformToFill` sang `Uniform`, căn giữa ngang/dọc. Toàn bộ frame video giữ đúng tỉ lệ, không bị phóng tràn rồi cắt lệch phải.
- Tâm contour logo trong video được căn lại theo bounding box phần đỏ, thay vì tâm canvas bitmap có padding. Khung 1920×1080 có tâm logo đo được `(960; 541)`; ảnh chụp Launcher 1440×900 có tâm `(720; 450,5)`, lệch tối đa 1 pixel so với tâm khung.
- `LogoMark.tsx` dùng contour SVG bo nhẹ các góc pixel, có lớp đỏ sát mép và chỉ lấy bitmap satin trong vùng đã lùi 3,5px. Loại viền trắng từ nền cũ nhưng giữ chi tiết mắt trắng. PNG/ICO/sidebar dùng chung cách dựng. PNG 1024px có tâm alpha `(512; 512)`; kiểm tra pixel sáng gần trắng chỉ nằm trong vùng mắt `(817,377)–(855,404)`.
- Video nén CRF 14, H.264 1920×1080/30fps/90 frame/3 giây, có AAC hoàn toàn im lặng như bản trước. Kích thước **1.747.110 byte**, SHA-256 `579A40FB55FE734763E7635125E58F7A3A7CE35D0B38EB6B2248552CACA58114`. File active `images/logo/tg-dragon-intro.mp4`; bản xem riêng `images/logo/tg-dragon-intro-centered.mp4`. Bản `tg-dragon-intro-v2.mp4` giữ để so sánh lịch sử.
- Build solution: 0 warning, 0 error. Kiểm tra pixel/alpha, TypeScript, render MP4 và executable thật đã chạy. Overlay đóng ở 4.386ms; chụp màn hình xác nhận logo ở giữa. Chỉ đổi layout/media asset trong lần sửa này, không thêm unit test cho pixel/layout. 9 file state đã khôi phục, hash khớp trước QA.
- Source app lần sửa này chỉ đổi `MainWindow.xaml`; icon và MP4 được cập nhật qua resource/content link đã có. Source dựng ảnh/video nằm ở project Remotion đã ghi bên dưới. Không sửa luồng cài game.

![Logo căn giữa trong Launcher](verification/tg-dragon-centered/screen-2850.png)

Các số đo/evidence hiện hành ở `docs/verification/tg-dragon-centered/`; thông tin hash/kích thước bản 2 phía dưới là lịch sử.

## Bản 2 trước chỉnh tâm/viền — sửa rồng, đường bay và nền trắng

- Thay hai rồng ribbon bằng hình rồng Á Đông nguyên bản tạo bằng imagegen, có sừng/râu/vảy/bờm/bốn chân vuốt. Đầu đi theo cung bay, thân và đuôi đi theo quãng đường đã bay, có sóng uốn thân; hai rồng từ hai phía cuộn về nét T/G.
- Thân bắt đầu cuộn ở frame 22/24, hoàn tất ở 48; texture chuyển sang logo từ 44–54, logo gốc hiện từ 45–57 rồi giữ tĩnh 57–89 (1,10 giây). Nét ngang trái T và nét trong G được hoàn tất bằng chuyển ảnh. Đây là hoạt ảnh minh họa 2D có dissolve, chưa phải biến hình 3D liên tục; cổ/chân trước rồng G còn kéo giãn khi cuộn.
- Bỏ toàn bộ ô trắng/ngà quanh logo trong video, icon PNG/ICO và ảnh dự phòng. Giữ màu đỏ satin và chi tiết mắt từ logo gốc.
- File hiện hành: `E:/SourceForDev/GameTranslationProject/images/logo/tg-dragon-intro.mp4`; bản xem riêng `tg-dragon-intro-v2.mp4` cùng thư mục. MP4 H.264, 1920×1080, 30fps, 90 frame, đúng 3 giây; **1.345.749 byte**, SHA-256 `C0E2BC3B616E5361442DDD8D45F356092713A1594F50066FE9BEE4CB535CD264`.
- Giữ một track AAC stereo 48kHz hoàn toàn im lặng để WPF phát đúng nhịp. Đã giải mã và kiểm tra toàn bộ sample PCM int16: biên độ tuyệt đối lớn nhất bằng 0. Bản bỏ audio từng bị WPF tua hết trong khoảng 0,2 giây; bản giao không có âm thanh nghe được.

### Tích hợp và xác minh bản 2

- Chỉ sửa thêm `MainWindow.xaml.cs` trong source app: tạm đặt `HwndTarget.RenderMode=SoftwareOnly` trong startup rồi khôi phục mode cũ trong `finally`, có kiểm tra source đã dispose. Cần thay đổi này vì live QA thấy decoder GPU nhận video nhưng hiện màn hình đen. Không thay đổi render mode toàn process.
- `dotnet build GameTranslationLauncher.sln --no-restore -v quiet`: 0 warning, 0 error. `dotnet test GameTranslationLauncher.sln --no-build --no-restore -v quiet`: Application 24/24, Infrastructure 36/36 qua. TypeScript `tsc --noEmit` và render Remotion qua.
- Executable thật: video tiến đều theo thời gian; overlay đóng ở 4.409ms từ lúc tạo process, UI opacity 1, hit test overlay tắt. Probe ghi `SoftwareOnly` trong intro và `Default` khi vào UI. Screenshot màn hình thực xác nhận rồng và logo có hiển thị.
- Windows tắt animation: bỏ video, catalog/UI hiện khoảng 1.019ms. Tạm thiếu MP4: ảnh TG trong suốt dự phòng chạy, overlay đóng khoảng 1.831ms. MP4 được khôi phục; hash source, bản xem riêng và file cạnh executable giống nhau.
- Sao lưu mới trước QA, khôi phục 9 file state người dùng và đối chiếu toàn bộ đường dẫn tương đối/SHA-256. Thiết lập animation Windows đã được khôi phục.
- Review độc lập đã kiểm tra 15 frame từ MP4 đã encode, metadata và audio PCM; không còn lỗi chặn. [Báo cáo độc lập](verification/tg-dragon-intro-v2/independent-review-v2.md) ghi rõ giới hạn kéo giãn cổ/chân và chuyển ảnh giữa texture/logo.

![Rồng bản 2 trong Launcher](verification/tg-dragon-intro-v2/screen-1350.png)

![Kết bằng TG không ô trắng](verification/tg-dragon-intro-v2/screen-2850.png)

![Các frame bản 2](verification/tg-dragon-intro-v2/contact-sheet-v2.png)

### Source bản 2

Project vẫn ở `C:/Users/giang/.codex/visualizations/2026/10/27/01a17c93-c1d9-7651-b0d7-3788f76a5460/tg-dragon-intro`. Source active: `src/TGDragonIntroV2.tsx`, `src/DragonFlight.tsx`, `src/Icons.tsx`, `BRIEF-V2.md`; bản 1 giữ tại `v1/`. Texture rồng mới được chép vào `E:/SourceForDev/GameTranslationProject/images/logo/tg-dragon-flight-v2.png`.

`npm run render` dựng source. Trim bản giao bằng FFmpeg đi kèm Remotion: `ffmpeg -i out/tg-dragon-intro.mp4 -c:v libx264 -crf 18 -pix_fmt yuv420p -c:a aac -t 3 -movflags +faststart out/tg-dragon-intro-v2.mp4`. Không bỏ track AAC. Source Launcher không thêm dependency Node/Remotion.

Motion Workbench vẫn chưa mở được do lỗi symlink `EPERM` đã ghi ở bản 1. Source và MP4 đầy đủ; không tuyên bố đã kiểm tra Workbench. Chưa kiểm tra trên máy khác/DPI cao. Dữ liệu QA nằm tại `docs/verification/tg-dragon-intro-v2/`.

---

## Bản 1 — lịch sử đã được thay thế

## Kết quả

- Dùng mẫu số 2 đỏ satin đã được người dùng chọn.
- Hai rồng riêng biệt bay vào từ ngoài hai phía, uốn/morph thành T và G; phần cuối dùng lại bitmap logo gốc đã duyệt, giữ tĩnh 35 frame (khoảng 1,17 giây).
- MP4 H.264, **1920×1080, 30fps, 90 frame, đúng 3 giây**, không có audio stream; 530.774 byte. SHA-256 `9864660E7D539DE4542775285718B0AE26D4AC4BDCE00E92E7C64F37E6F11A91`.
- [Video](../../images/logo/tg-dragon-intro.mp4), [icon PNG](../../images/logo/tg-dragon-satin-icon.png), [icon ICO](../../images/logo/tg-dragon-satin.ico).
- Đã tích hợp trong `App/TGLauncher.exe`. Video và catalog chạy song song; do người dùng chọn intro dài hơn, UI chính hiện sau khoảng 4,3–4,4 giây tính từ lúc tạo process trên máy kiểm tra (gồm khởi tạo app, mở decoder, video 3 giây và fade 180ms). Catalog vẫn sẵn sàng khoảng 1 giây.

## Source và skill

Dựng bằng [video-shotcraft](https://github.com/Vincentwei1021/video-shotcraft), nhánh single-shot, với hiệu ứng hai rồng tùy biến theo yêu cầu. Đã đọc Gallery, card `ui-to-brand-morph · input-morph-assemble` và source `InputMorphsIntoLogo.tsx`; chỉ tham khảo ngữ pháp đối tượng tạo thành thương hiệu, không tuyên bố khớp mẫu Gallery. SVG đích được lấy từ contour đỏ của logo đã duyệt, không vẽ lại bitmap cuối.

Project Remotion có đầy đủ source, bitmap gốc và brief tại:

`C:/Users/giang/.codex/visualizations/2026/10/27/01a17c93-c1d9-7651-b0d7-3788f76a5460/tg-dragon-intro`

File chính: `src/TGDragonIntro.tsx`, `src/Icons.tsx`, `src/logo-paths.json`, `src/workbench.ts`, `BRIEF.md`, `trace-logo.py`. `npm run render` dựng lại bản nguồn; bản final bỏ audio, xuất 3 giây bằng FFmpeg đi kèm Remotion. Không thêm Node/Remotion vào dependency của Launcher.

## File Launcher đã sửa

- `src/GameTranslationLauncher.Wpf/MainWindow.xaml`: icon cửa sổ, MediaElement, ảnh dự phòng mới.
- `src/GameTranslationLauncher.Wpf/MainWindow.xaml.cs`: phát intro, hoàn tất theo catalog/video, timeout và fallback, đóng media khi xong.
- `src/GameTranslationLauncher.Wpf/GameTranslationLauncher.Wpf.csproj`: icon exe, Resource link PNG/ICO, Content MP4 copy cho build/publish.
- `DESIGN.md`, `WORKFLOW.md`, báo cáo này.
- Thêm asset tại `../images/logo/`: `tg-satin-approved.png`, `tg-dragon-satin-icon.png`, `tg-dragon-satin-sidebar.png`, `tg-dragon-satin.ico`, `tg-dragon-intro.mp4`.

Không sửa `GameLibraryView.xaml` hoặc các file Claude đang giữ. Sidebar đổi ảnh thông qua Resource alias hiện có. Không commit.

## Verification

- `dotnet build GameTranslationLauncher.sln --no-restore`: **0 warning, 0 error**.
- `dotnet test GameTranslationLauncher.sln --no-restore`: **60/60 pass** (Application 24, Infrastructure 36).
- Project Remotion `tsc --noEmit`: qua; ffprobe xác nhận metadata video ở trên.
- Review độc lập đã xem frame mẫu và tự trích thêm f20/22/24/27/30/33/36/55/89: không còn lỗi chặn. Báo cáo trong [independent-review.md](verification/tg-dragon-intro/independent-review.md).
- Executable thật: `HasVideo=True`, vị trí phát vượt 3.000ms, kết thúc overlay `Collapsed`, `IsHitTestVisible=False`, giao diện opacity 1. Đã chụp hình video đang chạy và UI sau intro.
- Windows animation tắt: không mở media, catalog/giao diện hiện khoảng 969ms. Đã khôi phục thiết lập Windows sau kiểm tra.
- Tạm thiếu MP4: MediaFailed chuyển sang ảnh dự phòng; UI hiện và overlay đóng khoảng 1,8 giây từ khi tạo process. MP4 đã được khôi phục, hash output khớp source.
- Dữ liệu `%LOCALAPPDATA%/GameTranslationLauncher` được sao lưu trước runtime QA và khôi phục sau, kiểm tra hash từng file.

![Hai rồng trong Launcher](verification/tg-dragon-intro/runtime-850.png)

![Launcher sau intro, logo sidebar mới](verification/tg-dragon-intro/runtime-3650.png)

![Các frame kiểm tra](verification/tg-dragon-intro/contact-sheet.png)

## Giới hạn

- Đây là motion graphic 2D với SVG morph, không phải rồng 3D tả thực. Review còn ghi nhận gai mảnh ở f22 và vết gấp nhỏ ở f27; không làm mất nội dung chính.
- Chưa kiểm tra trên máy thiếu codec Windows, máy yếu hoặc DPI cao; có fallback khi MediaFailed/timeout.
- Motion Workbench của skill không mở được vì Windows trả `EPERM` khi tạo symlink. Source Remotion và MP4 vẫn đầy đủ; không đổi thiết lập quyền Windows hoặc sửa source skill để vượt lỗi này. Chưa kiểm tra parity của Workbench.

Khi muốn đăng phim công khai, việc ghi nguồn tác giả skill là tùy chọn: [X](https://x.com/VincentWei93), [Douyin](https://www.douyin.com/user/MS4wLjABAAAAK1pkjBxilk2Oi_9h_vFyD-lTAu9CTlvhmOtkosDvvxg), [Xiaohongshu](https://xhslink.cn/m/At9iP2d5C1V). Có thể chia sẻ tại [trang showcase](https://vincentwei1021.github.io/video-shotcraft/showcase.html) nếu muốn; chưa đăng hay gửi gì ra ngoài. Skill cũng hỗ trợ xuất project JianYing/CapCut khi có yêu cầu riêng.
