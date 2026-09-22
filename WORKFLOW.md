# Quy trình phát triển Game Translation Launcher

File này là nguồn chính cho trình tự thực hiện công việc, lộ trình, trạng thái và tiêu chí hoàn thành. Phạm vi tổng quát nằm tại [`README.md`](README.md), quy tắc code tại [`RULES.md`](RULES.md), còn quyết định kiến trúc tại [`DESIGN.md`](DESIGN.md).

Điều hướng: [`README.md`](README.md) | [`WORKFLOW.md`](WORKFLOW.md) | [`RULES.md`](RULES.md) | [`DESIGN.md`](DESIGN.md)

## Trạng thái hiện tại

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Nền tảng tài liệu | Hoàn thành | Đã tạo và liên kết bốn file hướng dẫn |
| Hợp đồng package manifest | Hoàn thành | Schema v1, package Tiny Eden tự chứa và contract test đã hoàn thành |
| Solution và project skeleton | Hoàn thành | Đã tạo bốn project đúng dependency direction và test Infrastructure |
| Catalog và kiểm tra trạng thái | Hoàn thành | Catalog local tải nhanh chỉ đọc manifest/settings; trạng thái receipt/file/hash được kiểm tra theo yêu cầu cho từng game |
| Install/update/uninstall engine | Hoàn thành | Install plan, preflight, transaction, rollback và uninstall theo receipt đã có test |
| Giao diện WPF/MVVM | Đang thực hiện | Shell, catalog, thao tác file, modal Settings, preference giao diện, report mailto, liên kết dự án, tab game chưa Việt hóa và modal giải thích trạng thái xung đột file đã được nối qua use case/dịch vụ UI; artwork được quản lý theo `images/<game-id>/`; còn thiếu test ViewModel và kiểm thử thao tác file thật |
| Update online | Chưa lên lịch | Ngoài phạm vi MVP local |

Quy ước trạng thái: `Chưa bắt đầu` → `Đang thực hiện` → `Bị chặn` hoặc `Hoàn thành`. Chỉ đánh dấu `Hoàn thành` khi đáp ứng tiêu chí và có bằng chứng kiểm thử tương ứng.

## Quy trình cho mỗi yêu cầu

### Bước 1 — Đọc và xác định phạm vi

- Đọc bốn file theo thứ tự trong [`README.md`](README.md).
- Viết lại yêu cầu thành hành vi có thể kiểm chứng.
- Xác định rõ phần thuộc Domain, Application, Infrastructure hay WPF theo [`DESIGN.md`](DESIGN.md).
- Liệt kê file dự kiến phải sửa và phần không được thay đổi.
- Nếu yêu cầu chưa rõ, chọn giả định nhỏ nhất và ghi rõ; không tự mở rộng thành một đợt refactor.

### Bước 2 — Khảo sát code và khả năng tái sử dụng

- Tìm class, interface, use case và test đang có chức năng gần nhất.
- Đọc execution path hiện tại trước khi chỉnh sửa.
- Ưu tiên mở rộng điểm phù hợp thay vì tạo một implementation song song.
- Không tái sử dụng một class chỉ vì tên tương tự; phải kiểm tra trách nhiệm và dependency.
- Kiểm tra framework, nullable, package references và convention của project bị ảnh hưởng.

### Bước 3 — Lập kế hoạch thay đổi nhỏ nhất

Kế hoạch phải nêu:

- Hành vi trước và sau thay đổi.
- Layer/project chịu trách nhiệm.
- Class được tạo mới, class được sửa và lý do.
- Dữ liệu hoặc compatibility có bị ảnh hưởng hay không.
- Cách kiểm thử thành công, thất bại và rollback.

Nếu cần di chuyển class, trình bày trước theo mẫu:

```text
Vị trí hiện tại -> Vị trí đề xuất -> Trách nhiệm chính -> Lý do -> Dependency bị ảnh hưởng
```

### Bước 4 — Cập nhật tài liệu trước khi code

Chỉ cập nhật tài liệu khi quyết định mới làm thay đổi nguồn sự thật hiện tại:

- Thay đổi mục tiêu/phạm vi/cách bắt đầu: cập nhật [`README.md`](README.md).
- Thay đổi thứ tự triển khai, trạng thái hoặc Definition of Done: cập nhật `WORKFLOW.md`.
- Thay đổi quy chuẩn code, naming, MVVM, giới hạn class/hàm: cập nhật [`RULES.md`](RULES.md).
- Thay đổi kiến trúc, dependency, schema, state hoặc luồng nghiệp vụ: cập nhật [`DESIGN.md`](DESIGN.md).

Sau khi sửa một file, kiểm tra liên kết chéo và tránh tạo hai quy tắc mâu thuẫn.

### Bước 5 — Triển khai

- Thực hiện thay đổi nhỏ nhất theo kế hoạch.
- Giữ View, ViewModel, use case và hạ tầng đúng ranh giới.
- Tách class/hàm khi trách nhiệm bắt đầu khác nhau, không chờ đến khi file quá lớn.
- Với logic khó, thêm XML documentation và `NOTE` theo [`RULES.md`](RULES.md).
- Không sửa file ngoài phạm vi để “tiện tay làm sạch”.

### Bước 6 — Kiểm tra

Thực hiện từ hẹp đến rộng:

1. Xem toàn bộ diff và loại bỏ thay đổi ngoài phạm vi.
2. Build project bị ảnh hưởng.
3. Chạy unit test trực tiếp liên quan.
4. Chạy integration test file system nếu thay đổi Infrastructure.
5. Build solution khi thay đổi dependency, contract hoặc nhiều project.
6. Chạy thử luồng UI nếu thay đổi binding, command hoặc trạng thái hiển thị.

Không được ghi “đã kiểm tra thành công” nếu lệnh tương ứng chưa chạy hoặc không hoàn tất.

### Bước 7 — Kết thúc và cập nhật trạng thái

- Cập nhật bảng trạng thái hoặc phase liên quan trong file này.
- Ghi ngắn gọn kết quả build/test và phần chưa thể kiểm tra.
- Nếu phát hiện quyết định kiến trúc mới trong lúc code, cập nhật [`DESIGN.md`](DESIGN.md).
- Nếu công việc chưa hoàn thành, ghi rõ bước tiếp theo và điều kiện đang chặn.

## Lộ trình triển khai

### Giai đoạn 0 — Nền tảng tài liệu

**Phụ trách:** toàn bộ dự án.

**Đầu ra:** `README.md`, `WORKFLOW.md`, `RULES.md`, `DESIGN.md` liên kết với nhau.

**Tiêu chí hoàn thành:** phạm vi, quy trình, kiến trúc, SOLID, MVVM, naming, giới hạn class/hàm và quy tắc cập nhật tài liệu được mô tả rõ.

### Giai đoạn 1 — Hợp đồng package và local state

**Trạng thái:** Hoàn thành ngày 2026-09-12. Package Tiny Eden gồm đủ 5 file và đã xác minh size/SHA-256. Bộ test contract có manifest hợp lệ, thiếu field, destination trùng không phân biệt hoa thường, source/destination traversal và path tuyệt đối.

**Phụ trách:** Domain định nghĩa model/invariant; Application định nghĩa nhu cầu đọc; Infrastructure xử lý JSON và file local.

**Công việc:**

1. Chốt schema `launcher-package.json` theo [`DESIGN.md`](DESIGN.md).
2. Chốt schema installation receipt và quy tắc tương thích `schemaVersion`.
3. Xác định cách sinh SHA-256 cho file phát hành.
4. Tạo package mẫu cho Tiny Eden mà không thay đổi pipeline dịch hiện tại.
5. Viết test cho manifest hợp lệ, thiếu field, trùng destination và path vượt ra ngoài game root.

**Tiêu chí hoàn thành:** package mẫu đọc được, lỗi schema có thông báo cụ thể, không chấp nhận đường dẫn tuyệt đối hoặc path traversal.

### Giai đoạn 2 — Solution skeleton và catalog local

**Trạng thái:** Hoàn thành ngày 2026-09-12, cập nhật ngày 2026-09-14. Catalog cô lập lỗi từng manifest; settings/receipt được ghi atomic trong storage root. `LoadGameCatalogUseCase` chỉ ghép package với installation gần nhất để khởi động nhanh; `CheckGameStatusUseCase` xác định sáu trạng thái cho đúng game người dùng yêu cầu, không phụ thuộc WPF. Test bao phủ catalog rỗng/lỗi, state schema cũ, roundtrip persistence, marker game, SHA-256 và status rules.

**Phụ trách:** bốn project theo [`DESIGN.md`](DESIGN.md).

**Công việc:**

1. Tạo solution và project đúng dependency direction.
2. Đọc các package local từ `../games/*/dist/launcher/launcher-package.json`.
3. Đọc/ghi user settings và installation receipt trong `%LOCALAPPDATA%`.
4. Khi người dùng yêu cầu kiểm tra, xác định trạng thái cài đặt từ receipt và hash thực tế cho đúng game đã chọn; không quét toàn bộ game khi khởi động.
5. Thêm test cho catalog rỗng, package lỗi và state cũ.

**Tiêu chí hoàn thành:** Application có thể trả về danh sách game nhanh không phụ thuộc WPF và kiểm tra trạng thái riêng từng game theo yêu cầu.

### Giai đoạn 3 — Install/update/uninstall engine

**Phụ trách:** Application điều phối use case; Infrastructure thực thi file system; Domain giữ rule và trạng thái.

**Công việc:**

1. Xây dựng install plan không thay đổi file.
2. Preflight: game root, source package, hash, quyền ghi, dung lượng và file đang bị khóa.
3. Backup file có sẵn và staging file mới.
4. Commit, verify và ghi receipt theo cách atomic trong phạm vi thực tế của Windows.
5. Rollback khi bất kỳ bước nào thất bại.
6. Update dựa trên receipt và phiên bản hiện tại.
7. Uninstall chỉ xử lý file đã ghi trong receipt; khôi phục backup khi có.
8. Thêm progress, cancellation và log không chứa dữ liệu nhạy cảm.

**Tiêu chí hoàn thành:** vượt qua đầy đủ ma trận kiểm thử an toàn ở phần dưới.

**Trạng thái:** Hoàn thành ngày 2026-09-12. Engine có plan chỉ đọc, consent/replacement approval rõ ràng, preflight, backup bền vững, staging cùng volume, commit receipt atomic, rollback ngược thứ tự, update theo ownership và uninstall chỉ theo receipt. Bộ 44 test hiện tại bao phủ cài sạch, game root sai, package sai hash, idempotency, update/thêm-bớt file, lỗi giữa commit và rollback, restore backup kể cả khi destination bị thiếu, conflict file đã sửa, prerequisite `preserved`, cancellation, path traversal, lock operation và vòng cài/gỡ package Tiny Eden thật trên game root tạm.

### Giai đoạn 4 — Giao diện WPF theo MVVM

**Trạng thái:** Đang thực hiện ngày 2026-09-14. Đã có shell WPF, ResourceDictionary theo TG Launcher Design Handoff, sidebar chọn game, detail panel theo trạng thái catalog và composition root cho `LoadGameCatalogUseCase`. Mỗi View có code-behind tối thiểu để gọi `InitializeComponent`; không đặt nghiệp vụ vào code-behind. UI đã nối chọn thư mục qua `SelectGameInstallationUseCase`, preview replacement trước confirmation, command cài/cập nhật/gỡ, progress/cancellation, mở thư mục/chạy executable đã khai báo và artwork local. Artwork resolver dùng pack URI tuyệt đối và có fallback theo từng game, nên asset lỗi không thể làm rỗng catalog. WPF cũng có resolver metadata presentation tĩnh theo game ID để hiển thị mô tả ngắn và engine, tách biệt manifest/rule cài đặt. Sidebar đã chuẩn hóa logo TG trên đúng nền sidebar, thumbnail ngang kèm tên game, icon Settings vector và icon phiên bản cùng kích thước, tooltip phiên bản hiện ngay phía trên, và title bar có ba nút cùng style; nhãn chữ dưới logo đã được bỏ. Settings là modal căn giữa cửa sổ, làm mờ và khóa nội dung Launcher phía sau thay vì thay thế màn Detail. Detail đưa phiên bản bản dịch vào dòng metadata cạnh ngày xác minh, hiển thị engine, mô tả game và thẻ trạng thái `ĐÃ VIỆT HÓA`; giá trị của ba thẻ thông tin nhỏ hơn nút `Chọn thư mục game` 2px. Bản phát hành WPF dùng tên `TGLauncher.exe` và nhúng icon TG. Chưa có test ViewModel riêng hoặc kiểm thử thao tác thật trên thư mục game do người dùng chọn, nên chưa đóng giai đoạn.

**Cập nhật mới nhất ngày 2026-09-14:** Settings dùng layout co giãn có vùng cuộn dự phòng để không cắt form ở cửa sổ nhỏ; liên kết hiển thị Facebook, Instagram rồi GitHub qua `MaterialDesignThemes` `PackIcon`, với `Foreground` tường minh theo `BrushTextPrimary` và padding ngang bằng 0 để icon không bị nén trong button rộng 48px, có tooltip và mở link qua thao tác click rõ ràng. Màu nhấn được chọn từ combobox, dùng chung control mũi tên tam giác với combobox game để tránh phụ thuộc glyph của font; template truyền `SelectionBoxItemTemplate` và `DataTemplate` chữ sáng cho `DisplayName`, nên luôn hiện tên màu hoặc game đã chọn. Nút đóng modal là button 34px độc lập, cân đối với header. Detail đã có metadata, hero/cover Steam CDN và cảnh báo MelonLoader riêng cho `no-rest-for-the-wicked`; tiêu đề dài tự giảm xuống 38px thay vì bị cắt sớm. Nút mở email cao 40px để đồng nhất với thao tác phụ. Preference `displayLanguage` (`vi`/`en`) nay cập nhật toàn bộ chuỗi presentation do WPF sở hữu (Settings, Library, Detail, title-bar, dialog chọn folder và phản hồi thao tác), còn `accentTheme` hỗ trợ `crimson`/`blue`/`emerald`/`violet`/`white`/`orange`/`cyan`; theme trắng dùng chữ đen trên button nhấn. Report dùng danh sách catalog local động cho báo lỗi/cập nhật, còn yêu cầu Việt hóa cho nhập tên game mới; report vẫn chỉ mở email nháp `mailto:` tới địa chỉ cấu hình và không lưu SMTP credential. Sidebar có ô tìm kiếm cố định để lọc tên/game ID; danh sách dùng cuộn pixel, panning và quán tính dọc, nhưng vẫn để cơ chế `ScrollViewer` gốc xử lý wheel để tránh lỗi cuộn một chiều. Khởi động/tải lại catalog chỉ đọc manifest và settings; receipt, marker game và hash chỉ được kiểm tra cho game người dùng bấm `Kiểm tra trạng thái`, nên thêm nhiều game không làm tăng thời gian mở Launcher. Bản phát hành hiện dùng `App/TGLauncher.exe`; shortcut Desktop hiển thị `TGLauncher.exe` và Start Menu hiển thị `TGLauncher` để Windows Search lập chỉ mục. `dotnet test Launcher/GameTranslationLauncher.sln --no-restore` đã qua 51 test và build WPF không warning/lỗi. Chưa có test ViewModel riêng hoặc kiểm thử thao tác thật trên thư mục game do người dùng chọn, nên chưa đóng giai đoạn.

**Phụ trách:** project WPF; mọi thay đổi dữ liệu đi qua Application use case.

**Công việc:**

1. Màn hình danh sách game và trạng thái.
2. Màn hình chi tiết package, phiên bản và lần xác minh gần nhất.
3. Chọn/xác nhận thư mục game.
4. Command cài đặt, cập nhật và gỡ bỏ.
5. Progress, cancellation, confirmation và kết quả lỗi.
6. Khóa command xung đột trong khi một thao tác đang chạy.
7. Hoàn thiện nhận diện sidebar, Settings presentation và nhãn thao tác theo trạng thái để người dùng không gặp hành động trùng nghĩa.

**Tiêu chí hoàn thành:** code-behind không chứa nghiệp vụ; ViewModel được kiểm thử độc lập; UI không bị treo trong thao tác file dài.

### Giai đoạn 5 — Đóng gói và phát hành local

**Trạng thái:** Đang thực hiện ngày 2026-09-15. Cả WPF build `Debug` và `Release`, cũng như publish self-contained Windows 64-bit qua `Publish-App.ps1`, đều đặt đầu ra tại `App/TGLauncher.exe`, không dùng hậu tố `Wpf`. Thuộc tính WPF `<Version>` là nguồn version duy nhất cho Assembly/File Version, Product Version và tên setup. Đóng gói dùng project Advanced Installer 21.2 [`build/installer/TGLauncher.aip`](build/installer/TGLauncher.aip); `Build-Installer.ps1` publish vào staging, đồng bộ catalog package, đặt Product Version theo WPF rồi tạo setup `.exe` tại `Installer/TGLauncherSetup-<version>.exe`. Build Advanced Installer hiện tại tạo `Installer/TGLauncherSetup-2.0.0.exe` (90,809,434 bytes, SHA-256 `A235413E0FE89559611D240CA42F50A230F9429298035CD8C7CD20D6E7A8E486`). Project dùng bộ cài `x64`, cài ứng dụng và catalog `games` cùng thư mục, tạo shortcut Desktop `TGLauncher.exe`, shortcut Start Menu `TGLauncher` để Windows Search nhận diện, và uninstaller. `Build-Installer.ps1` được lưu UTF-8 có BOM để Windows PowerShell 5.1 đọc an toàn các thông báo tiếng Việt. Đã đối chiếu 143 file package trong staging với source, không có sai khác; 51 test Application/Infrastructure đã qua. Do Debug/Release dùng chung thư mục, artifact của lần build mới nhất là artifact được chạy. Chưa kiểm tra installer trên máy sạch không có .NET runtime, đường dẫn tiếng Việt/khoảng trắng và chưa viết hướng dẫn xử lý sự cố.

**Cập nhật Debug/Release ngày 2026-09-14:** WPF đặt `RuntimeIdentifier=win-x64` và `SelfContained=true`, nên `dotnet build` Debug hoặc Release đưa .NET Core/WindowsDesktop runtime riêng vào `App`; không còn tạo executable phụ thuộc runtime hệ thống. Build Debug/Release đều qua không warning/lỗi; output có `hostfxr.dll`, `hostpolicy.dll`, runtimeconfig `includedFrameworks` và 253 file (khoảng 150 MB). Chưa chạy xác nhận trực tiếp trên máy sạch không có .NET runtime.

**Phụ trách:** WPF/composition root và quy trình build.

**Công việc:**

1. Publish self-contained cho Windows.
2. Tạo installer cài/gỡ chuẩn, shortcut Start Menu/Desktop và mang theo catalog package local.
3. Kiểm tra chạy trên máy không có sẵn .NET runtime.
4. Kiểm tra đường dẫn có khoảng trắng và ký tự tiếng Việt.
5. Viết hướng dẫn sử dụng và xử lý sự cố.

**Tiêu chí hoàn thành:** người dùng có thể chạy Launcher, quản lý Tiny Eden và gỡ sạch mà không cần môi trường phát triển.

### Giai đoạn 6 — Update online và mở rộng nền tảng

Chỉ bắt đầu sau khi MVP local ổn định. Mọi endpoint, chữ ký package, cơ chế cache, update channel, Steam/Epic detection và self-update phải có thiết kế riêng bổ sung vào [`DESIGN.md`](DESIGN.md).

## Ma trận kiểm thử an toàn bắt buộc

| Tình huống | Kết quả mong đợi |
| --- | --- |
| Thư mục game đúng và chưa cài | Cài thành công, hash khớp, receipt được ghi |
| Thư mục game sai | Dừng trước khi tạo/copy/xóa file |
| Package thiếu hoặc sai hash | Dừng ở preflight, game không thay đổi |
| Cài lại cùng phiên bản | Không tạo file thừa; trạng thái vẫn nhất quán |
| Update từ phiên bản cũ | Chỉ thay đổi file cần thiết; receipt chuyển sang phiên bản mới |
| Lỗi giữa quá trình update | Rollback về trạng thái trước update |
| Uninstall file chưa bị người dùng sửa | Xóa file owned và khôi phục backup |
| Uninstall gặp file đã bị sửa ngoài Launcher | Không xóa âm thầm; báo xung đột để người dùng quyết định |
| File prerequisite đã tồn tại trước Launcher | Không nhận quyền sở hữu và không xóa khi uninstall |
| Destination chứa `..` hoặc vượt game root | Manifest bị từ chối |
| Hai thao tác cùng lúc trên một game | Chỉ một thao tác được chạy; thao tác còn lại bị khóa rõ ràng |

## Definition of Done

Một yêu cầu chỉ hoàn thành khi:

- Hành vi đáp ứng đúng phạm vi đã thống nhất.
- Code tuân thủ [`RULES.md`](RULES.md) và dependency trong [`DESIGN.md`](DESIGN.md).
- Không có class/hàm phình lớn mà chưa được xem xét trách nhiệm.
- Đã bổ sung hoặc cập nhật test phù hợp với rủi ro.
- Build/test liên quan đã chạy thành công, hoặc giới hạn môi trường được ghi chính xác.
- Diff không chứa refactor, format hoặc file ngoài phạm vi.
- Tài liệu và trạng thái đã được cập nhật nếu quyết định nguồn sự thật thay đổi.
