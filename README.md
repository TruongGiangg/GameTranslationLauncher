# Game Translation Launcher

Game Translation Launcher là ứng dụng Windows dùng để quản lý các bản Việt hóa đã hoàn thành trong repository này. Ứng dụng sẽ đọc các gói game theo một cấu trúc thống nhất, cho phép người dùng chọn thư mục game, kiểm tra trạng thái, cài đặt, cập nhật và gỡ bỏ bản Việt hóa một cách an toàn. Game đã có tab nhưng **chưa Việt hóa** chỉ hiện phần giới thiệu/trạng thái và không cho thao tác file. Artwork đặt tại `../images/<game-id>/`; logo riêng của Launcher đặt tại `../images/logo/`.

Launcher là một phần độc lập với pipeline tạo bản dịch:

- Pipeline ở thư mục [`../scripts/`](../scripts/) chịu trách nhiệm tạo các file phát hành.
- Dữ liệu từng game nằm trong [`../games/`](../games/).
- Launcher chỉ quản lý package đã được tạo; không thực hiện dịch, giải nén tài nguyên hoặc chỉnh sửa CSV.

## Thứ tự đọc bắt buộc

Trước khi phân tích hoặc chỉnh sửa Launcher, con người và AI phải đọc theo thứ tự sau:

1. [`README.md`](README.md): phạm vi, mục tiêu và cách sử dụng bộ tài liệu.
2. [`WORKFLOW.md`](WORKFLOW.md): quy trình thực hiện công việc, các giai đoạn và tiêu chí hoàn thành.
3. [`RULES.md`](RULES.md): quy tắc bắt buộc về SOLID, MVVM, đặt tên, chia class, viết hàm, kiểm thử và an toàn file.
4. [`DESIGN.md`](DESIGN.md): kiến trúc, cấu trúc solution, mô hình dữ liệu và luồng cài đặt/cập nhật/gỡ bỏ.

Các quy tắc chung của repository trong [`../README.md`](../README.md), [`../docs/system-architecture.md`](../docs/system-architecture.md) và [`../docs/code-standards.md`](../docs/code-standards.md) vẫn được giữ nguyên. Tài liệu trong thư mục này chỉ bổ sung quy định dành riêng cho Launcher.

## Mục tiêu của phiên bản đầu tiên

Phiên bản MVP phải cung cấp được các chức năng sau:

- Đọc danh sách game đã có package tương thích với Launcher.
- Cho phép chọn hoặc xác nhận thư mục cài game trên máy người dùng.
- Kiểm tra thư mục được chọn có đúng game hay không.
- Hiển thị một trong các trạng thái: chưa cài, đã cài, có bản cập nhật, thiếu file hoặc file đã bị thay đổi.
- Cài đặt bản Việt hóa từ package local.
- Cập nhật bản Việt hóa mà không làm mất file gốc đã backup.
- Gỡ đúng các file do Launcher quản lý và khôi phục file gốc khi cần.
- Hiển thị tiến độ, kết quả và lỗi có thể xử lý được.
- Lưu trạng thái cài đặt riêng cho từng máy trong `%LOCALAPPDATA%`.

Update qua Internet, tự cập nhật Launcher và tự động phát hiện mọi nền tảng phân phối game không thuộc MVP. Các phần đó chỉ được thực hiện sau khi quy trình local đã được kiểm thử đầy đủ.

## Nguyên tắc nền tảng

- **An toàn dữ liệu trước tiện lợi:** không xóa hoặc ghi đè file ngoài kế hoạch cài đặt đã xác minh.
- **Manifest điều khiển hành vi:** không hardcode tên game, đường dẫn game hoặc danh sách file trong ViewModel hay dịch vụ cài đặt.
- **Có thể hoàn tác:** thao tác thay đổi file phải có backup, xác minh và rollback phù hợp.
- **Một nguồn sự thật:** package manifest mô tả nội dung phát hành; installation receipt mô tả những gì đã được cài trên máy.
- **UI không chứa nghiệp vụ:** WPF dùng MVVM; ViewModel gọi use case thay vì tự đọc, copy hoặc xóa file.
- **Tái sử dụng có chủ đích:** logic dùng chung nằm ở Domain/Application/Infrastructure phù hợp, không sao chép giữa các game.
- **Class và hàm có trách nhiệm rõ:** không để một class tích lũy hàng nghìn dòng hoặc một hàm xử lý toàn bộ quy trình.

Chi tiết bắt buộc được quy định tại [`RULES.md`](RULES.md). Sơ đồ phụ thuộc và vị trí từng loại class nằm tại [`DESIGN.md`](DESIGN.md).

## Cấu trúc hiện tại

```text
Launcher/
├── README.md
├── WORKFLOW.md
├── RULES.md
├── DESIGN.md
├── contracts/
│   ├── launcher-package.schema.json
│   ├── installation-receipt.schema.json
│   └── launcher-settings.schema.json
├── build/
│   └── installer/
│       └── TGLauncher.aip
├── GameTranslationLauncher.sln
├── Build-Installer.ps1
├── Installer/                 # artifact tạo ra, không lưu source
├── src/
│   ├── GameTranslationLauncher.Domain/
│   ├── GameTranslationLauncher.Application/
│   ├── GameTranslationLauncher.Infrastructure/
│   └── GameTranslationLauncher.Wpf/
└── tests/
    ├── GameTranslationLauncher.Application.Tests/
    └── GameTranslationLauncher.Infrastructure.Tests/
```

Project test Domain chỉ được tạo khi có behavior cần kiểm thử tại layer đó; không tạo project hoặc file placeholder. Thư mục `App/` là đầu ra duy nhất của WPF cho cả build `Debug`, `Release` và publish; người dùng chỉ chạy Launcher từ đây. WPF build `Debug`/`Release` là self-contained `win-x64`, nên chạy hoặc debug không phụ thuộc .NET runtime cài trên máy. Vì hai configuration dùng chung một thư mục, lần build sau sẽ thay file đầu ra của lần build trước. Trạng thái triển khai thực tế được cập nhật trong [`WORKFLOW.md`](WORKFLOW.md).

Build và chạy test từ thư mục `Launcher/`:

```powershell
dotnet build GameTranslationLauncher.sln
dotnet test GameTranslationLauncher.sln --no-build
```

Để tạo bản phát hành Windows độc lập, chạy:

```powershell
.\Publish-App.ps1
```

Sau khi hoàn tất, chỉ cần mở `App/TGLauncher.exe`. Tên project nội bộ `GameTranslationLauncher.Wpf` chỉ mô tả layer giao diện; không xuất hiện trong tên ứng dụng phát hành.

Để tạo bộ cài Windows, cần có Advanced Installer 21.2 rồi chạy:

```powershell
.\Build-Installer.ps1
```

Script lấy version từ thuộc tính `<Version>` của project WPF, đồng bộ catalog vào project [`build/installer/TGLauncher.aip`](build/installer/TGLauncher.aip), rồi tạo file `Installer/TGLauncherSetup-<version>.exe`. Có thể mở file `.aip` trực tiếp bằng Advanced Installer để xem hoặc chỉnh các tùy chọn bộ cài. Bộ cài bao gồm bản self-contained `win-x64`, catalog package hiện có trong `../games/*/dist/launcher/`, shortcut Desktop tên `TGLauncher.exe`, shortcut Start Menu tên `TGLauncher` để Windows Search tìm thấy, và uninstaller; người nhận không cần cài .NET SDK hoặc runtime. Gỡ Launcher chỉ gỡ ứng dụng/package đã phát hành, không xóa `%LOCALAPPDATA%/GameTranslationLauncher` vì nơi này có receipt và backup cần thiết để khôi phục file game an toàn.

## Quy tắc duy trì tài liệu

Khi yêu cầu mới làm thay đổi phạm vi, dữ liệu, kiến trúc hoặc quy chuẩn:

1. Cập nhật file tài liệu chịu trách nhiệm cho nội dung đó.
2. Kiểm tra và cập nhật các liên kết hoặc nội dung liên quan trong ba file còn lại.
3. Sau khi tài liệu phản ánh đúng quyết định mới, mới áp dụng thay đổi vào code.
4. Khi hoàn thành, cập nhật trạng thái và bằng chứng kiểm thử trong [`WORKFLOW.md`](WORKFLOW.md).

Không sao chép cùng một quy tắc chi tiết vào nhiều file. Mỗi nội dung chỉ có một file làm nguồn chính; các file khác liên kết đến nguồn đó.

## Bắt đầu một công việc mới

Thực hiện checklist tại phần **Quy trình cho mỗi yêu cầu** trong [`WORKFLOW.md`](WORKFLOW.md). Nếu thay đổi cần tạo hoặc di chuyển class, phải đối chiếu cấu trúc trong [`DESIGN.md`](DESIGN.md) và quy tắc chia trách nhiệm trong [`RULES.md`](RULES.md) trước khi viết code.
