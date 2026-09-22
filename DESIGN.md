# Thiết kế Game Translation Launcher

File này là nguồn chính cho kiến trúc, dependency, cấu trúc project, mô hình dữ liệu và luồng nghiệp vụ của Launcher. Quy trình triển khai nằm tại [`WORKFLOW.md`](WORKFLOW.md); quy chuẩn code bắt buộc nằm tại [`RULES.md`](RULES.md).

Điều hướng: [`README.md`](README.md) | [`WORKFLOW.md`](WORKFLOW.md) | [`RULES.md`](RULES.md) | [`DESIGN.md`](DESIGN.md)

## 1. Mục tiêu thiết kế

- Quản lý nhiều game mà không viết lại logic install/update/uninstall cho từng game.
- Tách nghiệp vụ khỏi WPF để có thể kiểm thử độc lập và tái sử dụng.
- Mọi thay đổi file đều được lập kế hoạch, xác minh ownership và có khả năng rollback.
- Package phát hành không phụ thuộc đường dẫn cài game trên máy người tạo package.
- Có thể bổ sung nguồn catalog online sau này mà không thay đổi Domain hoặc ViewModel chính.
- Giữ thiết kế đủ rõ để bảo trì lâu dài nhưng không tạo abstraction chưa có nhu cầu thực tế.

## 2. Phạm vi và phần không thuộc Launcher

Launcher quản lý package đã build. Các công việc sau vẫn thuộc pipeline hiện tại của repository và không được đưa vào Launcher MVP:

- Trích xuất `.locres`, `.pak`, `.utoc` hoặc `.ucas` từ game.
- Dịch hoặc merge CSV.
- Tìm AES key.
- Build bản Việt hóa từ source translation.
- Thay đổi nội dung game-specific ngoài những file đã khai báo trong package.

Kiến trúc pipeline nguồn được mô tả tại [`../docs/system-architecture.md`](../docs/system-architecture.md).

## 3. Nền tảng kỹ thuật

- Ứng dụng desktop Windows dùng WPF và target `net10.0-windows` trên .NET 10 LTS.
- Ngôn ngữ chính là C#; bật nullable reference types và implicit usings cho project mới nếu không gây xung đột convention.
- JSON dùng `System.Text.Json`; hash package dùng SHA-256 từ thư viện chuẩn .NET.
- MVP lưu settings/receipt bằng file JSON, chưa dùng database.
- Bản phát hành mục tiêu đầu tiên là self-contained `win-x64`; yêu cầu kiến trúc khác chỉ bổ sung khi có nhu cầu thực tế.
- MSTest 4 được dùng cho test project vì tích hợp trực tiếp với .NET SDK và analyzer của Microsoft.
- Chưa chốt framework MVVM, DI hoặc logging bên thứ ba. `MaterialDesignThemes` 5.3.2 chỉ được dùng ở WPF Settings cho `PackIcon` của liên kết dự án; theme và control style vẫn do Launcher sở hữu. Các dependency khác chỉ được thêm khi có lý do rõ theo [`RULES.md`](RULES.md).

## 4. Kiến trúc tổng thể

```text
GameTranslationLauncher.Wpf
        │
        ├──────────────┐
        ▼              ▼
GameTranslationLauncher.Application
        │              ▲
        ▼              │ implements ports
GameTranslationLauncher.Domain
                       │
GameTranslationLauncher.Infrastructure
        ├── JSON/catalog
        ├── local state/receipt
        ├── file system/backup/staging
        └── SHA-256/logging
```

Dependency direction:

```text
Domain <- Application <- Wpf
   ^            ^          |
   └──────── Infrastructure┘  (Wpf là composition root)
```

Quy tắc bắt buộc:

- Domain không tham chiếu Application, Infrastructure hoặc WPF.
- Application chỉ tham chiếu Domain và abstraction do Application/Domain sở hữu.
- Infrastructure tham chiếu Domain/Application để triển khai abstraction.
- WPF tham chiếu Application và nối implementation Infrastructure tại startup.
- Không tạo dependency vòng.

## 5. Cấu trúc solution mục tiêu

```text
Launcher/
├── GameTranslationLauncher.sln
├── src/
│   ├── GameTranslationLauncher.Domain/
│   │   ├── Games/
│   │   ├── Installation/
│   │   ├── Packages/
│   │   └── ValueObjects/
│   ├── GameTranslationLauncher.Application/
│   │   ├── Abstractions/
│   │   ├── Catalog/
│   │   ├── Installation/
│   │   └── Settings/
│   ├── GameTranslationLauncher.Infrastructure/
│   │   ├── Catalog/
│   │   ├── FileSystem/
│   │   ├── Hashing/
│   │   ├── Persistence/
│   │   └── Logging/
│   └── GameTranslationLauncher.Wpf/
│       ├── Views/
│       ├── ViewModels/
│       ├── Commands/
│       ├── Dialogs/
│       ├── Converters/
│       ├── Resources/
│       └── Composition/
└── tests/
    ├── GameTranslationLauncher.Domain.Tests/
    ├── GameTranslationLauncher.Application.Tests/
    └── GameTranslationLauncher.Infrastructure.Tests/
```

Chỉ tạo folder khi có class thực sự thuộc trách nhiệm đó. Không tạo file placeholder chỉ để giống sơ đồ.

### Domain

Chứa model và invariant không phụ thuộc UI hay file system:

- `GamePackage`: metadata package của một game.
- `GamePackageFile`: file nguồn, destination tương đối, hash và vai trò.
- `GameInstallationSetting` và `InstalledFileState`: installation được chọn và snapshot file chỉ đọc.
- `InstallationStatus`: `NotInstalled`, `Installed`, `UpdateAvailable`, `Damaged`, `Conflict`, `Unknown`.
- `LauncherSettings`, `InstallationReceipt` và `InstalledFileReceipt`: local state độc lập với JSON serializer.
- Value object như `GameId`, `PackageVersion`, `RelativeGamePath` khi chúng giúp giữ invariant thực tế.

Không đưa DTO JSON hoặc attribute serializer vào Domain.

### Application

Chứa use case và port cần thiết:

- Use case đã triển khai: `LoadGameCatalogUseCase`, `CheckGameStatusUseCase` và `DetectInstallationStatusUseCase`. `LoadGameCatalogUseCase` chỉ đọc manifest/settings để khởi động nhanh, không gọi detector, đọc receipt hay hash. `CheckGameStatusUseCase` chỉ gọi `DetectInstallationStatusUseCase` cho game người dùng chủ động yêu cầu kiểm tra.
- Contract đã triển khai: `IGameCatalogRepository`, `IGamePackageRepository`, `ILauncherSettingsRepository`, `IInstallationReceiptRepository`, `IGameInstallationDetector` và `IInstalledFileStateReader`.
- Use case Giai đoạn 3: `BuildInstallPlanUseCase`, `InstallTranslationUseCase`, `UpdateTranslationUseCase` và `UninstallTranslationUseCase`.
- `InstallPlan` và `UninstallPlan`: operation bất biến đã được kiểm tra nhưng chưa thực thi.
- `IPackageIntegrityVerifier` chỉ xác minh package; `IInstallPlanExecutor` và `IUninstallPlanExecutor` chỉ thực thi transaction file tương ứng. Không mở rộng `IInstalledFileStateReader` thành interface đa trách nhiệm.
- `InstallationOperationLock` ở Application giữ lock không chờ theo `gameId + installationId`; operation thứ hai nhận lỗi `OperationInProgress` thay vì cùng thay đổi một installation.

Danh sách trên là định hướng, không bắt buộc tạo toàn bộ ngay từ đầu. Mỗi interface chỉ được tạo khi có consumer và contract cụ thể.

### Infrastructure

Chứa side effect và implementation theo công nghệ:

- Đọc package JSON từ repository local.
- Đọc/ghi settings và receipt trong `%LOCALAPPDATA%`.
- Chuẩn hóa/kiểm tra đường dẫn Windows.
- Tạo backup, staging, copy, replace, restore và cleanup.
- Tính SHA-256.
- Ghi log kỹ thuật.

Catalog local chỉ kiểm tra đúng đường dẫn `games/<slug>/dist/launcher/launcher-package.json`; không quét đệ quy toàn repository. Một manifest lỗi được trả thành `GameCatalogError` và không làm ẩn các package hợp lệ khác.

Infrastructure không quyết định một game có thể update hay không; nó cung cấp dữ liệu và operation để Application/Domain quyết định.

### WPF

Chứa UI theo MVVM:

- `MainWindow` chỉ làm shell.
- `GameLibraryViewModel` hiển thị danh sách game.
- `GameDetailsViewModel` hiển thị chi tiết; `GameOperationViewModel` điều phối command presentation và progress.
- WPF có màn hình thư viện/chi tiết game và modal Settings ở giữa cửa sổ. Khi modal mở, nội dung Launcher phía sau bị làm mờ và không nhận thao tác; modal co giãn theo cửa sổ và có vùng cuộn dự phòng, không cắt nội dung. Settings đổi toàn bộ chuỗi presentation do WPF sở hữu giữa `vi`/`en` và màu nhấn (`crimson`/`blue`/`emerald`/`violet`/`white`/`orange`/`cyan`) qua use case lưu preference; nền tối, logo và dữ liệu package không đổi. Mục Report mở email nháp bằng `mailto:` tới địa chỉ do Launcher cấu hình, để người dùng tự xác nhận gửi; Launcher không lưu thông tin SMTP hay tự gửi mail. Báo lỗi và yêu cầu cập nhật dùng danh sách game hiện có từ catalog local, còn yêu cầu Việt hóa nhận tên game tự do. Các liên kết mạng xã hội chỉ được mở bằng shell sau thao tác click rõ ràng của người dùng.
- ViewModel gọi use case, quản lý trạng thái bận/progress và chuyển kết quả thành nội dung hiển thị. Khi trạng thái là `Conflict`, WPF mở modal giải thích rằng Launcher không có đủ bằng chứng ownership để tự ghi đè hoặc xóa; modal chỉ hướng dẫn người dùng mở thư mục hoặc kiểm tra lại, không thay đổi file.
- Dialog chọn folder, confirmation và error presentation là dịch vụ UI, không nằm trong Application.
- `SelectGameInstallationUseCase` thuộc Application: nó xác minh marker game và lưu installation được chọn; WPF chỉ lấy đường dẫn từ dialog rồi gọi use case này.
- `PrepareTranslationPlanUseCase` chỉ đọc file state để tạo preview replacement chính xác; sau khi người dùng đồng ý, install/update vẫn lập plan lại trước transaction.
- Artwork và metadata presentation tĩnh của game (mô tả ngắn, engine hỗ trợ) được đóng gói/tra theo `gameId` qua các resolver WPF; chúng không thay đổi package manifest hay quy tắc cài đặt. Source artwork của mỗi game được giữ riêng tại `images/<game-id>/`, còn logo Launcher tại `images/logo/`; file `ARTWORK-SOURCES.md` ghi nguồn và mục đích của từng ảnh. Game đã lên kế hoạch nhưng **chưa Việt hóa** (hiện gồm Phantom Blade Zero, Grand Theft Auto VI, DRAGON BALL: Sparking! ZERO, Borderlands 4, ARK: Survival Evolved, Red Dead Redemption 2, ELDEN RING, The Witcher 3: Wild Hunt, Split Fiction, TIEBREAK: Grand Slam Edition, Cyberpunk 2077, DRAGON BALL XENOVERSE 2, God of War, Forza Horizon 6 và Black Myth: Wukong) được khai báo riêng qua `UntranslatedGameDefinition`; chúng hiện trong sidebar và Report, nhưng không có `GameCatalogItem`, không chọn game folder và không hiển thị thao tác install/update/uninstall. Khi package thật xuất hiện đúng `games/<slug>/dist/launcher/launcher-package.json`, package thay thế tab chờ này theo cùng `gameId`.
- Sidebar dùng thumbnail kèm tên game để nhận diện nhanh, có ô tìm kiếm cố định ngay dưới logo để lọc theo tên hoặc `gameId` trong catalog local. Khi danh sách dài, chỉ phần danh sách game cuộn bằng `ScrollViewer` mặc định của WPF; scrollbar được ẩn nhưng không bị disable, cuộn theo pixel và bật panning/quán tính dọc cho touchpad hoặc màn hình cảm ứng. Logo, tìm kiếm và nút tiện ích vẫn cố định. Khi chưa liên kết game root, chỉ một hành động rõ nghĩa là `Chọn thư mục game`; các hành động cài/cập nhật chỉ xuất hiện sau khi đường dẫn đã được xác minh. Sau khi mở Launcher hoặc tải lại catalog, game có game root đã lưu hiển thị `Chưa kiểm tra` và chỉ kiểm tra receipt/file/hash khi người dùng bấm `Kiểm tra trạng thái`; do đó số lượng game không làm chậm khởi động.
- Startup/composition root đăng ký dependency và tạo cửa sổ chính.

### Phân phối Windows

Bản phát hành và đầu ra WPF Debug/Release đều self-contained `win-x64`, đặt executable cùng runtime riêng và thư mục catalog cạnh nhau:

```text
TG Launcher/
├── TGLauncher.exe
└── games/
    └── <slug>/dist/launcher/
```

`FindGamesRoot` tìm thư mục `games` từ `AppContext.BaseDirectory`, nên installer phải luôn cài cả catalog cùng ứng dụng; không được chỉ phát hành riêng `.exe`. Thuộc tính `<Version>` trong project WPF là nguồn version duy nhất: `AssemblyVersion`, `FileVersion`, Product Version của Advanced Installer và tên setup đều lấy từ nó. Project [`build/installer/TGLauncher.aip`](build/installer/TGLauncher.aip) là nguồn cấu hình Advanced Installer 21.2. `Build-Installer.ps1` publish vào staging riêng, reset/sync thư mục `APPDIR` của project để phản ánh catalog hiện tại, rồi build installer per-machine `x64` tại `Installer/TGLauncherSetup-<version>.exe`. Installer tạo shortcut Desktop tên `TGLauncher.exe` và shortcut Start Menu tên `TGLauncher`; vì shortcut Start Menu là entry của ứng dụng đã cài, Windows Search có thể tìm bằng `TGLauncher`. Uninstaller không xóa `%LOCALAPPDATA%/GameTranslationLauncher`: receipt/backup ở đó cần được giữ lại để lần cài Launcher sau vẫn có thể quản lý hoặc khôi phục các file game đã từng được cài.

## 6. Nguồn dữ liệu và một nguồn sự thật

### Authoring config

`../games/<slug>/config/game-config.json` tiếp tục phục vụ pipeline build hiện tại. Launcher không dùng `steamInstallPath` trong file này làm lựa chọn cuối cùng trên máy người dùng.

Khi triển khai contract package, config hiện tại có thể được bổ sung metadata cần cho bước sinh manifest, nhưng không đổi tên hoặc xóa field đang được scripts sử dụng.

### Package manifest

Mỗi bản phát hành tương thích Launcher có:

```text
games/<slug>/dist/launcher/
├── launcher-package.json
├── payload/
└── prerequisites/
```

Thư mục `launcher/` là package tự chứa: tất cả `source` trong manifest phải nằm bên trong thư mục này. Cách đóng gói này giúp catalog local và catalog online sau này dùng cùng một contract, không phụ thuộc vào `tools/` hoặc cấu trúc repository trên máy phát hành.

File manifest được sinh từ config và nội dung thật trong `dist/`. Khi có generator, không chỉnh tay file đã sinh. Manifest chỉ chứa dữ liệu cần để Launcher cài package, không chứa AES key hoặc đường dẫn tuyệt đối trên máy build.

Schema chính thức nằm tại [`contracts/launcher-package.schema.json`](contracts/launcher-package.schema.json). Schema receipt nằm tại [`contracts/installation-receipt.schema.json`](contracts/installation-receipt.schema.json); schema user settings nằm tại [`contracts/launcher-settings.schema.json`](contracts/launcher-settings.schema.json).

Schema khởi đầu dự kiến:

```json
{
  "schemaVersion": 1,
  "gameId": "tiny-eden",
  "displayName": "Tiny Eden",
  "packageVersion": "1.0.0",
  "targetLanguage": "vi",
  "lastVerifiedWorkingInGame": "2026-09-12",
  "installDetection": {
    "requiredPaths": [
      {
        "path": "CGH/Binaries/Win64/CGH-Win64-Shipping.exe",
        "kind": "file"
      },
      {
        "path": "CGH/Content/Paks",
        "kind": "directory"
      }
    ],
    "blockedProcessNames": [
      "CGH-Win64-Shipping"
    ]
  },
  "payloadFiles": [
    {
      "source": "payload/VI_Translation_P.pak",
      "destination": "CGH/Content/Paks/~mods/VI_Translation_P.pak",
      "sha256": "2ca8db07e031981b4fa3102371b56fdbf2e45108cb09b4032e0063f897432b6e",
      "sizeBytes": 547629,
      "role": "translation"
    }
  ],
  "prerequisites": [
    {
      "id": "universal-signature-bypass",
      "displayName": "Universal Signature Bypass",
      "description": "Cho phép game nạp package Việt hóa chưa có chữ ký gốc.",
      "requiresExplicitConsent": true,
      "files": [
        {
          "source": "prerequisites/dsound.dll",
          "destination": "CGH/Binaries/Win64/dsound.dll",
          "sha256": "f4abc8a2371978e4114f267fc77fb8bb2ae94c0143f755eb5e6f113ce1bc187d",
          "sizeBytes": 545792
        }
      ]
    }
  ]
}
```

Ví dụ trên chỉ rút gọn danh sách file để dễ đọc. Package Tiny Eden thật phải khai báo đủ `.pak`, `.utoc`, `.ucas`, `dsound.dll` và `UniversalSigBypasser.asi`.

Mọi `source` phải dùng dấu `/`, tương đối với thư mục chứa manifest và không được thoát khỏi package root. Mọi `destination` phải dùng dấu `/`, tương đối với game root và không được thoát khỏi game root. Không chấp nhận path tuyệt đối, segment `.`/`..`, dấu `\\` hoặc hai destination trùng nhau khi so sánh không phân biệt hoa thường trên Windows.

`packageVersion` v1 dùng ba số nguyên không âm dạng `major.minor.patch`; chưa nhận prerelease hoặc build metadata để việc so sánh update không mơ hồ. SHA-256 được tính trực tiếp từ bytes của file sau khi package đã được tập hợp; ghi bằng 64 ký tự hex viết thường. `sizeBytes` và hash đều phải được Launcher xác minh trước khi thay đổi thư mục game.

Tương thích schema tuân theo các quy tắc sau:

- `schemaVersion` là số nguyên tăng theo phiên bản contract, bắt đầu từ `1`.
- Reader chỉ đọc các version nó hỗ trợ; version mới chưa biết phải bị từ chối với lỗi rõ ràng.
- Thêm field tùy chọn không đổi ý nghĩa có thể giữ nguyên version.
- Xóa/đổi tên field, thay đổi ý nghĩa hoặc đổi invariant bắt buộc phải tăng version và có migration/reader tương ứng.
- Manifest và receipt dùng `additionalProperties: false` để phát hiện lỗi chính tả thay vì bỏ qua âm thầm.

### User settings

Lưu tại:

```text
%LOCALAPPDATA%/GameTranslationLauncher/settings.json
```

Chứa preference presentation (`displayLanguage` là `vi` hoặc `en`, `accentTheme` là `crimson`, `blue`, `emerald`, `violet`, `white`, `orange` hoặc `cyan`) và game root người dùng đã chọn. Preference không chứa SMTP credential, nội dung report hay dữ liệu package. Ghi bằng temp file rồi replace để giảm nguy cơ file state bị dở dang.

Mỗi game có thể có nhiều installation. MVP chọn installation có `lastUsedAtUtc` mới nhất để hiển thị; UI chọn/chuyển installation sẽ được bổ sung ở giai đoạn WPF.

### Installation receipt

Lưu riêng theo game và installation identity:

```text
%LOCALAPPDATA%/GameTranslationLauncher/installations/<game-id>/<installation-id>/receipt.json
```

Receipt tối thiểu phải ghi:

- `schemaVersion`.
- Game ID, game root đã chuẩn hóa và package version.
- Thời điểm cài/update hoàn tất.
- Danh sách file Launcher sở hữu.
- Hash package và hash đã xác minh tại destination.
- File nào đã tồn tại trước khi cài và vị trí backup tương ứng.
- Prerequisite nào do Launcher tạo, prerequisite nào đã tồn tại trước đó.

Receipt chỉ được commit sau khi tất cả file đã được thay thế và xác minh thành công.

Schema receipt v1 còn ghi `packageManifestSha256`, `launcherVersion`, `createdDirectories` và ownership từng file. File có ownership `replaced` bắt buộc phải có thông tin backup và hash file gốc; file có ownership `created` hoặc `preserved` không được giả vờ có backup. `preserved` là file prerequisite đã tồn tại với đúng hash trước khi Launcher cài; Launcher theo dõi để phát hiện hỏng nhưng không sở hữu và không xóa file đó.

### Backup và log

```text
%LOCALAPPDATA%/GameTranslationLauncher/backups/<game-id>/<installation-id>/
%LOCALAPPDATA%/GameTranslationLauncher/logs/
```

Backup không nằm trong thư mục game hoặc repository. Chính sách dọn backup chỉ được bổ sung sau khi xác định rõ khả năng rollback qua nhiều phiên bản.

## 7. Nhận diện game và lựa chọn đường dẫn

Luồng MVP:

1. Launcher dùng game root đã lưu nếu còn tồn tại.
2. Nếu chưa có, người dùng chọn folder.
3. `installDetection.requiredRelativePaths` được kiểm tra.
4. Nếu marker thiếu hoặc không đúng loại, Application trả validation error và không lập install plan.
5. Chỉ sau khi xác minh thành công mới lưu game root vào user settings.

Tự dò Steam/Epic là adapter Infrastructure trong giai đoạn sau. Nó chỉ đề xuất đường dẫn; vẫn phải chạy cùng validation trước khi cài.

## 8. Luồng xác định trạng thái

Khi khởi động hoặc tải lại catalog, `LoadGameCatalogUseCase` chỉ ghép package với game root được dùng gần nhất và trả `Unknown`. Đây là trạng thái trung tính, không có nghĩa game bị lỗi hay chưa cài. WPF hiển thị `Chưa kiểm tra` nếu game root đã lưu, hoặc `Chưa chọn thư mục` nếu chưa có game root.

Chỉ sau thao tác rõ ràng `Kiểm tra trạng thái` của người dùng (hoặc preflight cài/cập nhật/gỡ) mới chạy luồng dưới đây cho đúng một game:

```text
Package + game root + receipt + file/hash thực tế
                       │
                       ▼
            DetectInstallationStatusUseCase
                       │
     ┌─────────────────┼──────────────────┐
     ▼                 ▼                  ▼
NotInstalled       Installed       UpdateAvailable/Damaged/Conflict
```

Quy tắc khởi đầu:

- Không có receipt và không có file package tại destination: `NotInstalled`.
- Chưa chọn game root hoặc marker không khớp: `Unknown`.
- Không có receipt nhưng đã có ít nhất một destination: `Conflict`, vì Launcher chưa có bằng chứng ownership.
- Receipt cùng version và mọi hash khớp: `Installed`.
- Receipt version thấp hơn package khả dụng và state hiện tại hợp lệ: `UpdateAvailable`.
- Receipt tồn tại nhưng owned file thiếu/sai hash: `Damaged`.
- Receipt cùng version nhưng hash manifest khác: `Conflict`, vì package đã bị thay đổi mà không tăng version.
- File destination tồn tại trước cài hoặc đã bị sửa và không thể quyết định ownership an toàn: `Conflict`.
- Dữ liệu receipt/schema không đọc được: `Unknown`; không tự động sửa hoặc xóa.

## 9. Luồng cài đặt

### Lập kế hoạch — không side effect

1. Validate manifest/schema.
2. Chuẩn hóa game root và xác minh marker.
3. Resolve tất cả source/destination.
4. Xác minh mọi destination nằm trong game root và không trùng nhau.
5. Kiểm tra source tồn tại và SHA-256 khớp manifest.
6. Phân loại destination: mới, đã owned, file ngoài Launcher hoặc conflict.
7. Tạo `InstallPlan` và danh sách cảnh báo/xác nhận cần thiết.

`InstallPlan` phân biệt `Create`, `ReplaceExternal`, `ReplaceOwned` và `Preserve`, đồng thời liệt kê file receipt cũ cần `DeleteOwned`, `RestoreBackup` hoặc `Preserve`. MVP áp dụng các quy tắc cụ thể sau:

- Payload đã tồn tại nhưng không có receipt mặc định là `Conflict`; chỉ được thay thế khi request chứa đúng destination đã được người dùng xác nhận riêng. Khi đó Launcher backup file gốc và ghi ownership `replaced`.
- Prerequisite đã tồn tại với đúng hash package là `Preserve`; khác hash là `Conflict`.
- Khi update, ownership và backup gốc của file đã owned được giữ xuyên phiên bản; không backup bản Việt hóa cũ thành file gốc.
- Cài lại đúng version và manifest, khi toàn bộ receipt còn nguyên vẹn, là no-op có tính idempotent.
- Prerequisite yêu cầu xác nhận chỉ được đưa vào plan khi request chứa đúng prerequisite ID đã được người dùng chấp thuận.

### Thực thi — có side effect

1. Giữ lock theo installation để ngăn thao tác đồng thời.
2. Chạy preflight cuối: quyền ghi, dung lượng, file bị khóa và game đang chạy nếu kiểm tra được an toàn.
3. Backup file ngoài Launcher cần được thay thế.
4. Copy source sang staging file cùng volume với destination khi có thể.
5. Xác minh hash staging.
6. Replace/move staging vào destination theo đúng thứ tự plan.
7. Xác minh hash toàn bộ destination.
8. Ghi receipt atomic.
9. Dọn staging và trả kết quả thành công.

Nếu lỗi trước bước 8, rollback các operation đã commit theo thứ tự ngược. Nếu rollback không hoàn tất, giữ backup/log và trả trạng thái lỗi nghiêm trọng; không ghi receipt giả như đã thành công.

Staging và file rollback tạm được đặt cạnh destination để thao tác commit nằm cùng volume. Backup gốc bền vững vẫn đặt trong `%LOCALAPPDATA%`; executor xác minh hash backup trước khi dùng. Executor chịu trách nhiệm cả thay đổi file và commit receipt để không có trạng thái "file mới nhưng receipt cũ". Tên file tạm chứa operation ID, không dùng glob để cleanup.

## 10. Luồng cập nhật

1. Yêu cầu receipt hiện tại hợp lệ hoặc xử lý conflict rõ ràng.
2. So sánh package version và hash từng destination.
3. Lập plan gồm file thêm, thay, giữ nguyên và file cũ cần loại bỏ.
4. Không backup lại bản Việt hóa cũ như thể đó là file gốc.
5. Giữ backup file gốc xuyên suốt update nếu vẫn cần cho uninstall.
6. Thực thi staging/replace/verify/rollback giống install.
7. Chỉ thay receipt cũ khi package mới được xác minh hoàn chỉnh.

Không xem ngày file là bằng chứng duy nhất cho update; dùng version và hash.

## 11. Luồng gỡ bỏ

1. Đọc và validate receipt.
2. Resolve từng owned path và xác minh vẫn nằm trong game root đã ghi.
3. So sánh hash hiện tại với hash Launcher đã cài.
4. Nếu file đã bị sửa ngoài Launcher, trả conflict; không xóa âm thầm.
5. Xóa đúng owned file hoặc khôi phục backup tương ứng.
6. Chỉ xóa directory do Launcher tạo nếu directory đang rỗng.
7. Không xóa prerequisite đã tồn tại trước lần cài.
8. Xác minh hậu điều kiện rồi mới xóa/đóng receipt.

Không dùng glob, suy luận theo tên mod hoặc recursive delete để gỡ cài đặt.

## 12. Prerequisite và signature bypass

Một số game Unreal có thể cần file hỗ trợ để nạp package. Đây là hành vi nhạy cảm:

- Manifest phải khai báo rõ từng prerequisite, source, destination và mục đích.
- UI phải hiển thị cảnh báo trước lần cài đầu tiên và yêu cầu xác nhận.
- Receipt phải phân biệt file có sẵn với file do Launcher tạo.
- Uninstall không xóa file có sẵn trước Launcher.
- Không cài hoặc quảng bá bypass cho game có anti-cheat; game đó phải bị đánh dấu không được hỗ trợ cho đến khi có phương thức an toàn.
- Không giấu prerequisite bên trong thao tác chung mà người dùng không biết.

## 13. Tiến độ, hủy và đồng thời

- Application phát progress theo các stage ổn định: validating, backing up, staging, installing, verifying, rolling back và completed.
- Cancellation được chấp nhận ở điểm an toàn. Sau khi bắt đầu commit, use case phải hoàn tất commit hoặc rollback trước khi trả về cancelled.
- Mỗi installation chỉ có một operation thay đổi state tại một thời điểm.
- Catalog/status read có thể chạy song song nếu không đọc file tạm chưa commit.
- UI disable command xung đột nhưng correctness vẫn phải được bảo vệ ở Application/Infrastructure.
- Log kỹ thuật ghi theo operation ID vào `%LOCALAPPDATA%/GameTranslationLauncher/logs/`; chỉ ghi game ID, installation ID, stage và path tương đối, không ghi game root tuyệt đối hoặc dữ liệu nhạy cảm.

## 14. Error model

Application trả lỗi có cấu trúc thay vì bắt ViewModel phân tích message string:

- `Validation`: manifest hoặc game root không hợp lệ.
- `PackageCorrupted`: source thiếu hoặc sai hash.
- `Conflict`: ownership/file thực tế không an toàn để tự xử lý.
- `AccessDenied`: thiếu quyền.
- `InsufficientDiskSpace`: không đủ dung lượng cho staging hoặc backup.
- `FileInUse`: game hoặc process khác đang khóa file.
- `Cancelled`: người dùng hủy tại điểm an toàn.
- `RollbackFailed`: operation thất bại và không thể hoàn tác đầy đủ.
- `ExplicitConsentRequired`: prerequisite nhạy cảm chưa được người dùng xác nhận.
- `OperationInProgress`: installation đang có operation thay đổi state khác.
- `Unexpected`: lỗi không dự kiến, có operation ID để tra log.

Các mã trên được trả qua `InstallationOperationResult`; `InstallationOperationException` chỉ chuyển lỗi có cấu trúc giữa Application và Infrastructure, không tạo hierarchy exception theo từng lỗi.

## 15. Quy tắc thay đổi thiết kế

Khi thêm store mới, kiểu package mới hoặc cơ chế update online:

1. Mô tả use case và khác biệt so với luồng hiện tại.
2. Xác định đây là dữ liệu mới, implementation mới hay domain rule mới.
3. Kiểm tra có thể mở rộng contract hiện tại mà không phá schema hay không.
4. Cập nhật file này và phase tương ứng trong [`WORKFLOW.md`](WORKFLOW.md).
5. Chỉ sau đó mới tạo interface/class mới theo [`RULES.md`](RULES.md).

Không thay đổi kiến trúc chỉ để dùng một thư viện hoặc pattern mới. Kiến trúc phục vụ hành vi, an toàn dữ liệu và khả năng bảo trì của Launcher.
