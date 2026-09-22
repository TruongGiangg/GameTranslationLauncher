# Quy chuẩn phát triển Game Translation Launcher

File này là nguồn chính cho các quy tắc bắt buộc khi viết và sửa code Launcher. Cách thực hiện một yêu cầu nằm tại [`WORKFLOW.md`](WORKFLOW.md); kiến trúc và vị trí class nằm tại [`DESIGN.md`](DESIGN.md).

Điều hướng: [`README.md`](README.md) | [`WORKFLOW.md`](WORKFLOW.md) | [`RULES.md`](RULES.md) | [`DESIGN.md`](DESIGN.md)

## 1. Thứ tự ưu tiên

Áp dụng theo thứ tự:

1. Yêu cầu rõ ràng của người dùng trong công việc hiện tại.
2. `AGENTS.md` gần nhất và quy tắc chung của repository.
3. `RULES.md` này.
4. Kiến trúc đã ghi trong [`DESIGN.md`](DESIGN.md).
5. Convention nhất quán của code gần đó.

Nếu có mâu thuẫn, làm theo nguồn có ưu tiên cao hơn và ghi rõ trong báo cáo cuối.

## 2. Nguyên tắc thay đổi

- Hiểu execution path trước khi sửa.
- Chỉ sửa file trực tiếp liên quan đến yêu cầu.
- Ưu tiên thay đổi nhỏ nhất nhưng hoàn chỉnh.
- Không refactor, đổi tên, format hoặc nâng package ngoài phạm vi.
- Không tạo abstraction chỉ để dự đoán một nhu cầu tương lai chưa tồn tại.
- Trước khi viết class mới, phải tìm class tương tự và đánh giá khả năng tái sử dụng.
- Giữ tương thích ngược cho manifest, receipt và user settings; thay đổi phá vỡ phải tăng `schemaVersion` và có kế hoạch migration.

## 3. Áp dụng SOLID theo hướng thực dụng

### Single Responsibility Principle

- Một class có một lý do chính để thay đổi.
- ViewModel không đọc JSON, hash file, copy file hoặc quyết định đường dẫn đích.
- JSON repository không chứa quy tắc cài đặt.
- Use case điều phối luồng nhưng không tự triển khai mọi thao tác file.
- Nếu một class vừa lập kế hoạch, vừa thực thi file, vừa hiển thị UI, phải tách theo layer.

### Open/Closed Principle

- Thêm game đã hoàn thành bằng package manifest, không thêm `if (gameName == ...)` trong code chung. Game chưa Việt hóa dùng `UntranslatedGameDefinition` presentation-only, tuyệt đối không tạo manifest placeholder hoặc đưa chúng vào luồng cài/gỡ. Artwork của mỗi game thuộc `images/<game-id>/`; không để asset game rời tại root `images/`.
- Khác biệt thực sự giữa các kiểu cài đặt được biểu diễn bằng dữ liệu hoặc strategy có contract rõ.
- Không tạo plugin system hoặc reflection trước khi có ít nhất hai hành vi cài đặt thực sự khác nhau.

### Liskov Substitution Principle

- Implementation phải giữ đúng contract của interface, bao gồm cancellation, lỗi và side effect.
- Fake/test implementation phải mô phỏng đúng semantics quan trọng, không trả kết quả thuận lợi một cách giả tạo.

### Interface Segregation Principle

- Interface nhỏ theo capability, ví dụ đọc hash không buộc phải cung cấp chức năng xóa file.
- Consumer chỉ phụ thuộc vào các operation nó thực sự dùng.
- Không tạo một interface `ILauncherService` chứa toàn bộ catalog, install, update, uninstall và settings.

### Dependency Inversion Principle

- Application định nghĩa abstraction cần thiết; Infrastructure triển khai abstraction đó.
- Domain không phụ thuộc WPF, JSON, Windows Registry hoặc `System.IO` để thực hiện side effect.
- WPF nhận use case/dependency qua composition root, không tự `new` repository hoặc file service trong ViewModel.

## 4. MVVM bắt buộc

### View

- XAML chịu trách nhiệm layout, binding, style và visual state.
- Code-behind chỉ được dùng cho hành vi thuần giao diện khó biểu diễn bằng binding, ví dụ focus hoặc window lifecycle.
- Code-behind không gọi `File`, `Directory`, repository hoặc use case cài đặt trực tiếp.
- Không dùng event handler để chứa nghiệp vụ install/update/uninstall.

### ViewModel

- Cung cấp state hiển thị và command cho View.
- Gọi Application use case và chuyển kết quả thành trạng thái UI.
- Không biết đường dẫn `%LOCALAPPDATA%`, cấu trúc JSON hoặc cách copy file.
- Không gọi `MessageBox` trực tiếp; dùng abstraction/dialog coordinator ở tầng UI.
- Mọi thao tác I/O dài phải bất đồng bộ, có trạng thái bận và hỗ trợ `CancellationToken` khi có ý nghĩa.
- Không cho chạy đồng thời các command xung đột trên cùng game.

### Model và use case

- Domain model không triển khai `INotifyPropertyChanged`.
- UI model chỉ dùng để trình bày; không thay thế domain model hoặc receipt.
- Mỗi use case đại diện một ý định rõ như load catalog, chọn game root, install, update hoặc uninstall.

## 5. Quy tắc chia class và hàm

Các con số dưới đây là mốc cảnh báo để xem lại thiết kế, không phải lý do tự động chia code vô nghĩa:

- Một file có một primary public type, trừ trường hợp type nhỏ gắn chặt và convention hiện tại cho phép.
- Class thông thường nên dưới khoảng 300 dòng logic. Khi vượt mốc này phải xem lại trách nhiệm trước khi thêm code.
- ViewModel nên dưới khoảng 250 dòng logic; state lặp lại hoặc workflow con nên được tách hợp lý.
- Hàm nên tập trung vào một mức trừu tượng và thường không quá 40 dòng logic.
- Hàm vượt khoảng 60 dòng phải được xem xét tách; nếu không tách vì cần giữ transaction/rollback liền mạch, phải có `NOTE` giải thích lý do và invariant.
- Constructor có quá 5 dependency là dấu hiệu class có thể đang giữ quá nhiều trách nhiệm.
- Tránh quá 4 tham số rời rạc; dùng request/options object khi các giá trị tạo thành một khái niệm ổn định.
- Không chia thành nhiều hàm một dòng chỉ để đạt giới hạn. Việc tách phải làm rõ ý nghĩa hoặc cho phép tái sử dụng/kiểm thử.
- Không tạo các class chung chung như `Manager`, `Helper`, `Utility`, `Processor` khi có thể đặt tên theo trách nhiệm cụ thể.

## 6. Quy tắc đặt tên

### Project, namespace và file

- Project/namespace/type dùng `PascalCase`.
- Namespace phải khớp project và folder, ví dụ `GameTranslationLauncher.Application.Installation`.
- Tên file khớp primary type, ví dụ `InstallTranslationUseCase.cs`.
- Interface dùng tiền tố `I`, ví dụ `IGameCatalogRepository`.
- Test project có hậu tố `.Tests` và phản chiếu cấu trúc source khi hợp lý.

### Type theo trách nhiệm

- Use case: `<ĐộngTừ><ĐốiTượng>UseCase`, ví dụ `InstallTranslationUseCase`.
- Repository interface: `I<ĐốiTượng>Repository`, ví dụ `IInstallationReceiptRepository`.
- Implementation theo công nghệ: `JsonGamePackageRepository`, `Sha256FileHashProvider`.
- ViewModel: `<MànHình>ViewModel`, ví dụ `GameDetailsViewModel`.
- View: `<MànHình>View` hoặc `<MànHình>Window` theo loại UI.
- Command: `<HànhĐộng>Command` nếu cần class riêng.
- Request/result: `<HànhĐộng>Request` và `<HànhĐộng>Result`.
- Exception riêng chỉ tạo khi caller thực sự cần phân biệt loại lỗi.

### Hàm và property

- Method/property/event dùng `PascalCase`; biến local và parameter dùng `camelCase`.
- Hàm bất đồng bộ có hậu tố `Async`.
- `Get...` yêu cầu dữ liệu phải tồn tại hoặc trả về contract rõ; `Find...`/`TryGet...` dùng khi không tìm thấy là bình thường.
- `Try...` trả `bool` và không dùng exception cho kết quả dự kiến.
- `Validate...` chỉ kiểm tra và trả kết quả/lỗi; không sửa file hoặc state.
- `Build...`/`Create...` tạo dữ liệu mới; `Ensure...` chỉ dùng khi hành vi idempotent được mô tả rõ.
- Tên hàm phải thể hiện side effect: dùng `Save`, `Copy`, `Delete`, `Restore`, không dùng tên mơ hồ như `Process` hoặc `Handle`.
- Boolean bắt đầu bằng `Is`, `Has`, `Can`, `Should` hoặc `Requires`.
- Collection dùng danh từ số nhiều.

## 7. Ghi chú cho logic khó

Comment phải giải thích **tại sao**, invariant hoặc ràng buộc bên ngoài; không lặp lại từng dòng code.

Bắt buộc thêm XML documentation cho:

- Public interface và public model contract.
- Hàm có quy tắc rollback, ownership, path safety hoặc compatibility không hiển nhiên.
- Thuật toán mà thay đổi thứ tự bước có thể làm hỏng dữ liệu.

Dùng `NOTE` tại đúng vị trí có ràng buộc khó nhận ra:

```csharp
// NOTE(install-rollback): Receipt chỉ được ghi sau khi tất cả file đích đã được
// xác minh hash; ghi sớm hơn sẽ khiến lần khởi động sau hiểu nhầm là đã cài xong.
```

Quy tắc cho `NOTE`:

- Có nhãn ngắn trong ngoặc để dễ tìm kiếm.
- Nêu điều kiện và hậu quả nếu vi phạm.
- Không dùng `NOTE` để biện minh cho code rối; vẫn phải tách class/hàm khi có thể.
- `TODO` phải có đầu ra cụ thể; không để placeholder không rõ trách nhiệm.

## 8. C# và xử lý bất đồng bộ

- Dùng phiên bản C# tương thích target framework đã chốt trong [`DESIGN.md`](DESIGN.md).
- Bật nullable reference types cho project mới.
- Dùng file-scoped namespace nếu project không có convention khác.
- Ưu tiên immutable model/value object khi dữ liệu không cần thay đổi.
- I/O dài dùng `async`/`await`; không dùng `.Result`, `.Wait()` hoặc fire-and-forget trong ViewModel.
- Truyền `CancellationToken` xuyên suốt use case và implementation có I/O dài.
- Không chạy song song các bước có thứ tự bắt buộc như backup → replace → verify → receipt.
- Không dùng background thread để cập nhật trực tiếp property bind với UI.
- Dispose stream và resource xác định bằng `using`/`await using`.
- Không bắt `Exception` ở tầng thấp trừ khi cần thêm context và vẫn giữ inner exception. Boundary UI có thể chuyển lỗi thành thông báo thân thiện và log chi tiết.
- Không swallow exception hoặc trả thành công khi thao tác chỉ hoàn thành một phần.

## 9. An toàn file, dữ liệu và bảo mật

- Tất cả destination trong manifest phải là đường dẫn tương đối.
- Chuẩn hóa đường dẫn tuyệt đối rồi xác minh nó vẫn nằm trong game root trước mọi thao tác ghi/xóa.
- Từ chối path traversal, destination trùng nhau và đường dẫn trỏ qua reparse point ngoài phạm vi nếu không chứng minh được an toàn.
- Không xóa directory theo recursive từ input chưa được xác minh.
- Uninstall dựa trên receipt, không dựa vào glob hoặc phỏng đoán tên file.
- Không xóa file bị người dùng hoặc công cụ khác thay đổi mà không cảnh báo/xử lý xung đột.
- Backup và receipt phải được ghi atomic khi có thể; file tạm có tên riêng và được dọn an toàn.
- Không lưu AES key, token, credential hoặc dữ liệu nhạy cảm trong log, receipt hay source của Launcher.
- Signature bypass là prerequisite nhạy cảm: chỉ xử lý khi manifest cho phép và người dùng xác nhận rõ. Không hỗ trợ né anti-cheat.
- Không yêu cầu chạy toàn bộ Launcher bằng quyền Administrator nếu chỉ một operation cụ thể cần quyền cao hơn.

## 10. Logging và thông báo lỗi

- Log phải có operation ID, game ID, bước đang chạy và lỗi kỹ thuật đủ chẩn đoán.
- UI hiển thị thông báo ngắn, hành động tiếp theo và đường dẫn log khi cần.
- Không log toàn bộ nội dung manifest nếu có field nhạy cảm trong tương lai.
- Phân biệt lỗi validation, conflict, quyền truy cập, file bị khóa, package hỏng và lỗi không mong đợi.
- Progress phản ánh các bước nghiệp vụ, không phát sự kiện cho từng byte gây quá tải UI.

## 11. Kiểm thử

- Domain rule và Application use case phải có unit test.
- Infrastructure thao tác file phải dùng thư mục test tạm, tuyệt đối không dùng thư mục game thật.
- Mỗi test tự tạo và tự dọn dữ liệu của mình; không phụ thuộc thứ tự chạy.
- Tên test theo mẫu `Method_Scenario_ExpectedResult` hoặc `Action_WhenCondition_ExpectedResult` và giữ nhất quán trong từng project.
- Phải kiểm tra cả happy path, validation failure, partial failure, cancellation, rollback và compatibility.
- Không sửa expected result chỉ để test xanh khi chưa xác minh hành vi.
- Ma trận tối thiểu nằm trong [`WORKFLOW.md`](WORKFLOW.md).

## 12. Dependency và thư viện ngoài

- Chỉ thêm package khi .NET/WPF không đáp ứng hợp lý và lợi ích đã được ghi rõ.
- Không thêm nhiều framework cho cùng một trách nhiệm.
- Dependency injection chỉ được cấu hình ở composition root của WPF.
- Domain không tham chiếu package UI, JSON serializer cụ thể hoặc file-system implementation.
- Trước khi nâng version package, đánh giá breaking change và chạy build/test liên quan.

## 13. Checklist review

Trước khi kết thúc một thay đổi, xác nhận:

- Class nằm đúng project/folder và chỉ có một trách nhiệm chính.
- Không có nghiệp vụ hoặc file I/O trong View/code-behind.
- Không hardcode game cụ thể trong logic dùng chung.
- Path, hash, backup, ownership và rollback được xử lý đúng với rủi ro của thay đổi.
- Hàm khó đã có tên rõ và `NOTE`/XML documentation phù hợp.
- Không tạo duplicate implementation thay cho việc tái sử dụng code hiện có.
- Diff nhỏ, sạch và không có file build output.
- Build/test và cập nhật tài liệu được thực hiện theo [`WORKFLOW.md`](WORKFLOW.md).
