# Bộ test case thủ công — TG Launcher

> Dùng để giao cho người/AI kiểm thử (ví dụ ChatGPT) chạy thử Launcher và báo bug.
> Mỗi case có mã, mức ưu tiên, bước làm và kết quả mong đợi. Báo bug theo mẫu ở cuối file.

## 1. Bối cảnh cho người kiểm thử

**TG Launcher** là ứng dụng Windows (WPF, .NET 10, self-contained `win-x64`) dùng để cài, cập nhật, kiểm tra và gỡ **bản Việt hóa** cho nhiều game PC. Mỗi game có một package khai báo trong `games/<slug>/dist/launcher/launcher-package.json`.

Nguyên tắc an toàn mà Launcher phải luôn giữ (vi phạm = bug nghiêm trọng):

- Không bao giờ ghi/xóa file ngoài thư mục game người dùng đã chọn và đã xác minh.
- Chỉ gỡ đúng file có trong receipt; file bị sửa ngoài Launcher thì báo xung đột, **không xóa âm thầm**.
- File gốc bị thay thế phải được backup và khôi phục được khi gỡ.
- Prerequisite nhạy cảm (ví dụ `dsound.dll` bypass chữ ký) chỉ được cài sau khi người dùng xác nhận.
- Lỗi giữa chừng phải rollback; không ghi receipt "thành công" giả.

Vị trí dữ liệu cục bộ:

| Dữ liệu | Đường dẫn |
|---|---|
| Settings | `%LOCALAPPDATA%\GameTranslationLauncher\settings.json` |
| Receipt cài đặt | `%LOCALAPPDATA%\GameTranslationLauncher\installations\<game-id>\<installation-id>\receipt.json` |
| Backup file gốc | `%LOCALAPPDATA%\GameTranslationLauncher\backups\...` |
| Log kỹ thuật | `%LOCALAPPDATA%\GameTranslationLauncher\logs\` |

## 2. Chuẩn bị môi trường

1. Windows 10/11 x64. Ghi lại phiên bản Windows, độ phân giải, mức scale (100% / 125% / 150%).
2. Chạy `Launcher\App\TGLauncher.exe` (bản build) hoặc cài bằng `Installer\TGLauncherSetup-<version>.exe`.
3. **Sao lưu** thư mục `%LOCALAPPDATA%\GameTranslationLauncher` trước khi test, để có thể khôi phục.
4. Để test cài/gỡ, **dùng bản sao thư mục game** (copy sang ổ khác) thay vì thư mục game thật nếu có thể. Game đang có package thật: *Tiny Eden*, *No Rest for the Wicked*, *Crimson Moon* (kiểm tra danh sách trong sidebar).
5. Trước mỗi nhóm test cài/gỡ, chụp danh sách file + SHA-256 của thư mục game để so sánh sau (PowerShell: `Get-ChildItem -Recurse <thư mục> | Get-FileHash`).

Quy ước: **P1** = phải chạy (an toàn dữ liệu, chức năng chính), **P2** = nên chạy, **P3** = hình thức/biên.

---

## 3. Khởi động và catalog

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| ST-01 | P1 | Xóa (sau khi đã backup) thư mục `%LOCALAPPDATA%\GameTranslationLauncher`, mở Launcher. | Mở được, không crash. Giao diện mặc định **Glassy**, ngôn ngữ **Tiếng Việt**, màu nhấn **Đỏ TG**. Sidebar có danh sách game. |
| ST-02 | P1 | Mở Launcher khi đã có settings với vài game đã chọn thư mục. | Game có thư mục đã lưu hiện **CHƯA KIỂM TRA**; game chưa có thư mục hiện **CHƯA CHỌN THƯ MỤC**. Khởi động không quét hash (mở nhanh). |
| ST-03 | P2 | Đo thời gian từ lúc bấm mở đến lúc thấy danh sách game. | Dưới ~3 giây trên máy bình thường; không treo UI. |
| ST-04 | P1 | Sửa tay một `launcher-package.json` thành JSON lỗi, mở Launcher. | Các game khác vẫn hiện bình thường; game lỗi bị bỏ qua và có thông báo dạng "bỏ qua N package lỗi". Không crash. (Khôi phục file sau test.) |
| ST-05 | P1 | Sửa `settings.json` thành JSON lỗi hoặc `schemaVersion: 2`, mở Launcher. | Không crash; không tự ghi đè/xóa settings lỗi một cách âm thầm. Ghi lại thông báo hiển thị. |
| ST-06 | P2 | Mở Launcher khi không có mạng. | Vẫn dùng được catalog local; kiểm tra cập nhật Launcher thất bại êm, không chặn UI. |

## 4. Sidebar và tìm kiếm

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| SB-01 | P2 | Gõ một phần tên game vào ô "Tìm game…". | Danh sách lọc theo tên, không phân biệt hoa thường. |
| SB-02 | P2 | Gõ game ID (ví dụ `tiny-eden`). | Game tương ứng hiện ra. |
| SB-03 | P3 | Gõ chuỗi không khớp game nào. | Danh sách rỗng, không lỗi; xóa ô tìm thì danh sách quay lại đủ. |
| SB-04 | P2 | Cuộn danh sách game bằng bánh xe chuột, touchpad, kéo trên màn hình cảm ứng (nếu có). | Bánh xe chuột: mỗi nấc trượt mượt khoảng một thumbnail (~108px), không nhảy cóc; lăn nhanh nhiều nấc thì cộng dồn. Touchpad: cuộn tỉ lệ theo lực vuốt, không giật. Màn hình cảm ứng: kéo dọc theo pixel, có quán tính. Cuộn được cả hai chiều tới đầu/cuối danh sách. Logo, ô tìm kiếm, nút Settings/phiên bản luôn cố định. Không bị "cuộn một chiều". |
| SB-05 | P3 | Rê chuột lên thumbnail game. | Thumbnail phóng nhẹ; viền sáng lên. Bỏ chuột ra trở lại bình thường. |
| SB-06 | P2 | Bấm lần lượt nhiều game. | Game được chọn có viền màu nhấn; khung chi tiết đổi đúng game. |
| SB-07 | P3 | Rê chuột lên nút "?" ở đáy sidebar. | Tooltip phiên bản hiện ngay phía trên. |

## 5. Chi tiết game

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| DT-01 | P2 | Chọn game đã Việt hóa. | Hiện tên game, mô tả, nhãn trạng thái, dòng thông tin (Hỗ trợ / Ngôn ngữ / Bản Việt hóa / Xác minh), 3 thẻ Trạng thái – Dung lượng – Package và nút hành động phù hợp. |
| DT-02 | P2 | Chọn game **chưa Việt hóa** (ví dụ ELDEN RING, Cyberpunk 2077). | Không có nút chọn thư mục/cài/cập nhật/gỡ. Không thể thao tác file. |
| DT-03 | P3 | Chọn game có tên rất dài. | Tiêu đề tự thu nhỏ, không tràn ra ngoài và không đè lên nhãn trạng thái. |
| DT-04 | P2 | Chọn *No Rest for the Wicked*. | Có ô cảnh báo prerequisite (MelonLoader/bypass) màu vàng. |
| DT-05 | P3 | Game không có ảnh hero. | Hiện nền gradient thay thế, không bị trống trắng/đen kịt, chữ vẫn đọc được. |

## 6. Chọn thư mục game

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| FD-01 | P1 | Bấm **CHỌN THƯ MỤC GAME**, chọn đúng thư mục cài game. | Được xác minh và lưu; trạng thái chuyển **CHƯA KIỂM TRA** hoặc trạng thái thật sau khi kiểm tra. |
| FD-02 | P1 | Chọn sai thư mục (thư mục bất kỳ, thư mục game khác). | Báo không xác nhận được; **không lưu** vào settings; không thay đổi file nào. |
| FD-03 | P2 | Mở hộp chọn thư mục rồi bấm Hủy. | Báo "Chưa thay đổi thư mục game"; không đổi gì. |
| FD-04 | P2 | Chọn thư mục có khoảng trắng và ký tự tiếng Việt (ví dụ `D:\Trò chơi\Tiny Eden`). | Hoạt động bình thường như đường dẫn thường. |
| FD-05 | P1 | Đã chọn thư mục, bấm **ĐỔI THƯ MỤC GAME** sang bản sao khác. | Lưu thư mục mới; **ngôn ngữ, màu nhấn, kiểu giao diện trong settings giữ nguyên** (mở `settings.json` kiểm tra). |
| FD-06 | P2 | Đóng và mở lại Launcher. | Thư mục đã chọn vẫn được nhớ. |

## 7. Kiểm tra trạng thái

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| CS-01 | P1 | Thư mục game sạch, bấm **KIỂM TRA TRẠNG THÁI**. | **CHƯA CÀI**. |
| CS-02 | P1 | Sau khi cài bằng Launcher, kiểm tra lại. | **ĐÃ CÀI**. |
| CS-03 | P1 | Sau khi cài, xóa tay 1 file Việt hóa trong thư mục game, kiểm tra. | **CẦN KIỂM TRA** (Damaged). Không tự sửa/xóa gì. |
| CS-04 | P1 | Sau khi cài, sửa nội dung 1 file Việt hóa (đổi vài byte), kiểm tra. | Trạng thái lỗi/xung đột; không tự ghi đè. |
| CS-05 | P1 | Thư mục game chưa từng cài bằng Launcher nhưng đã có sẵn file trùng tên file bản dịch (chép tay), kiểm tra. | **XUNG ĐỘT FILE**; mở được modal giải thích; modal chỉ hướng dẫn, **không có nút ghi đè/xóa**. |
| CS-06 | P2 | Xóa thư mục game đã lưu khỏi ổ đĩa, kiểm tra. | Báo không kiểm tra được, không crash. |

## 8. Cài đặt bản Việt hóa (an toàn dữ liệu — quan trọng nhất)

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| IN-01 | P1 | Thư mục sạch → **CÀI BẢN DỊCH** → xem hộp xác nhận. | Hộp xác nhận nêu số file sẽ xử lý, số file sẽ được backup, và prerequisite (nếu có). Chưa có file nào bị đổi trước khi bấm xác nhận. |
| IN-02 | P1 | Bấm **QUAY LẠI** ở hộp xác nhận. | Không file nào thay đổi (so hash với ảnh chụp ban đầu). |
| IN-03 | P1 | Xác nhận cài. | Thanh tiến trình chạy; cài xong trạng thái **ĐÃ CÀI**; có `receipt.json`; mọi file đích đúng hash package. |
| IN-04 | P1 | Kiểm tra backup: trước khi cài, thư mục có file gốc trùng đích (ví dụ file `.pak` gốc). | File gốc được backup vào `%LOCALAPPDATA%\...\backups`, hash backup = hash gốc. |
| IN-05 | P1 | Game có prerequisite (No Rest for the Wicked / Tiny Eden): kiểm tra có hỏi xác nhận riêng trước khi cài prerequisite. | Prerequisite chỉ được cài sau khi đồng ý. |
| IN-06 | P1 | Mở game (để game khóa file) rồi bấm cài. | Báo file đang bị dùng / game đang chạy; **không cài dở**; thư mục game giữ nguyên. |
| IN-07 | P1 | Đang cài, bấm **HỦY SAU ĐIỂM AN TOÀN**. | Hoặc dừng trước khi đổi file, hoặc rollback hoàn toàn; không có trạng thái cài dở; không có receipt thành công giả. |
| IN-08 | P2 | Bấm cài lần 2 khi đã cài đúng bản. | Không làm gì thừa (idempotent), trạng thái vẫn **ĐÃ CÀI**. |
| IN-09 | P1 | Thư mục game nằm trên ổ gần đầy (nếu dựng được). | Báo thiếu dung lượng trước khi đổi file; không cài dở. |
| IN-10 | P1 | Thư mục game chỉ đọc / không có quyền ghi (ví dụ `C:\Program Files\...` không chạy admin). | Báo thiếu quyền; không cài dở. |
| IN-11 | P2 | Trong lúc cài, thử bấm các nút khác (chọn game khác, cài lại, gỡ). | Các thao tác xung đột bị khóa; không chạy song song 2 thao tác trên cùng game. |
| IN-12 | P2 | Rút/đổi tên một file trong `games/<slug>/dist/launcher/payload` (package hỏng) rồi cài. | Báo package lỗi/sai hash trước khi đổi file game. (Khôi phục package sau test.) |

## 9. Cập nhật

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| UP-01 | P1 | Cài bản cũ, sau đó catalog có package version mới hơn → kiểm tra trạng thái. | **CÓ CẬP NHẬT**; nút **CẬP NHẬT** xuất hiện. |
| UP-02 | P1 | Bấm cập nhật và xác nhận. | Lên bản mới, đúng hash; backup file gốc ban đầu vẫn giữ (không backup đè bằng bản Việt hóa cũ). File không còn trong package mới được dọn đúng. |
| UP-03 | P1 | Sau cập nhật, gỡ bản Việt hóa. | Khôi phục được **file gốc ban đầu** (không phải bản Việt hóa cũ). |

## 10. Gỡ bản Việt hóa

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| UN-01 | P1 | Đã cài → **GỠ BẢN VIỆT HÓA** → xác nhận. | Chỉ file trong receipt bị xóa; file gốc được khôi phục; thư mục game trở về đúng như ảnh chụp ban đầu (so hash). |
| UN-02 | P1 | Trước khi gỡ, sửa tay 1 file Việt hóa. | Báo xung đột cho file đó; **không xóa âm thầm**. |
| UN-03 | P1 | Prerequisite đã có sẵn từ trước khi cài (ví dụ `dsound.dll` cùng hash). Gỡ. | Prerequisite có sẵn **không bị xóa**. |
| UN-04 | P2 | Thư mục rỗng do Launcher tạo. | Được xóa nếu rỗng; thư mục không do Launcher tạo thì giữ. |
| UN-05 | P2 | Bấm gỡ rồi bấm **QUAY LẠI**. | Không đổi gì. |

## 11. Chơi game và mở thư mục

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| PL-01 | P2 | Game đã cài → **CHƠI GAME**. | Game khởi chạy từ executable đã khai báo. |
| PL-02 | P2 | Xóa/đổi tên executable game rồi bấm chơi. | Báo không tìm thấy executable; không crash. |
| PL-03 | P3 | **MỞ THƯ MỤC GAME**. | Explorer mở đúng thư mục game. |

## 12. Settings — ngôn ngữ, màu nhấn, kiểu giao diện

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| SE-01 | P2 | Mở Settings bằng nút bánh răng. | Modal hiện giữa cửa sổ; nội dung phía sau mờ đi và không bấm được. Nút × đóng modal. |
| SE-02 | P2 | Chọn **Tiếng Anh**. | Toàn bộ chữ do Launcher sở hữu (Settings, sidebar, chi tiết, title bar, hộp thoại) đổi sang tiếng Anh ngay. Chọn lại Tiếng Việt thì đổi lại. |
| SE-03 | P2 | Đổi lần lượt 7 màu nhấn. | Nút chính, viền game đang chọn, lựa chọn đang bật đổi màu ngay. Màu **Trắng** và **Cyan**: chữ trên nút chính là màu tối, đọc được. |
| SE-04 | P1 | Trong "Kiểu giao diện" chọn **Tối**. | Đổi ngay sang giao diện tối cổ điển (không cần khởi động lại). Bố cục giống bản cũ: sidebar sát trái, nền đen, ảnh hero chỉ trong khung phải. |
| SE-05 | P1 | Chọn lại **Glassy**. | Đổi ngay sang kính: ảnh game phủ cả cửa sổ, sidebar nổi bo tròn, dock kính phía dưới. |
| SE-06 | P1 | Đặt Tối + Tiếng Anh + Cyan, đóng mở lại Launcher. | Cả ba lựa chọn được nhớ. `settings.json` có `"surfaceTheme": "dark"`. |
| SE-07 | P2 | Settings cũ không có `surfaceTheme` (xóa dòng đó trong `settings.json`). | Mở bình thường, mặc định **Glassy**. |
| SE-08 | P2 | Sửa `settings.json`: `"surfaceTheme": "light"`. | Không crash; xử lý như settings lỗi (ghi lại thông báo). |
| SE-09 | P3 | Thu nhỏ cửa sổ về cỡ tối thiểu (1100×720) rồi mở Settings. | Modal không bị cắt; nội dung cuộn được bên trong. |
| SE-10 | P3 | Mở danh sách màu nhấn và danh sách game trong Report ở cả hai kiểu giao diện. | Dropdown đọc rõ chữ, mục đang chọn được tô màu nhấn. |

## 13. Report và liên kết

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| RP-01 | P2 | Report: chọn **Báo lỗi**, chọn game, nhập nội dung, bấm **MỞ EMAIL ĐỂ GỬI**. | Mở trình email với thư nháp đúng địa chỉ, tiêu đề và nội dung. Launcher **không tự gửi**. |
| RP-02 | P2 | Bấm gửi khi chưa chọn game / chưa nhập nội dung. | Báo lỗi rõ ràng, không mở email. |
| RP-03 | P2 | Chọn **Yêu cầu Việt hóa**. | Ô chọn game đổi thành ô nhập tên game tự do. |
| RP-04 | P3 | Máy không có ứng dụng email mặc định. | Báo lỗi êm, không crash. |
| RP-05 | P3 | Bấm Facebook / Instagram / GitHub. | Mở đúng link trong trình duyệt mặc định. |

## 14. Giao diện Glassy (mới)

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| GL-01 | P2 | Ở Glassy, bấm qua nhiều game có ảnh khác nhau (tối: No Rest for the Wicked; sáng: The Witcher 3; xám: Black Myth: Wukong). | Phần kính (sidebar, dock, nhãn trạng thái) cho thấy ảnh game phía sau **mờ mịn**, nhuộm theo màu chủ đạo của từng game. Chữ trắng trên kính **luôn đọc được**, kể cả với ảnh sáng. |
| GL-02 | P2 | Đổi game. | Ảnh nền chuyển mờ dần (không giật/chớp đen); màu kính chuyển dần trong ~0,7 giây. |
| GL-03 | P3 | Để yên một game ~30 giây. | Ảnh nền zoom rất chậm (~6%); phần kính zoom khớp theo, không lệch hình so với ảnh phía sau. |
| GL-04 | P2 | Đổi game và quan sát phần chữ. | Tên game, mô tả, cảnh báo, dock trượt lên và hiện dần lệch nhau, không nhấp nháy hiện–mất–hiện. |
| GL-05 | P2 | Kéo đổi kích thước cửa sổ, phóng to/khôi phục. | Kính luôn khớp vị trí ảnh phía sau (không thấy ảnh mờ bị lệch so với ảnh rõ). Nhãn trạng thái bên phải cũng khớp khi cửa sổ rộng ra. |
| GL-06 | P2 | Mở Settings và các hộp thoại (xác nhận cài, xung đột, tiến trình, cập nhật Launcher) ở Glassy. | Đều là kính đọc rõ chữ; nút bấm được. |
| GL-07 | P2 | Tắt animation của Windows (Settings > Accessibility > Visual effects > Animation effects = Off), mở lại Launcher, đổi game. | Không còn chuyển động (không zoom, không trượt), nội dung hiện ngay đầy đủ. |
| GL-08 | P3 | Kéo cửa sổ bằng thanh tiêu đề ở Glassy (thanh trong suốt). | Kéo được; 3 nút thu nhỏ/phóng to/đóng hoạt động. |
| GL-09 | P2 | Đặt scale màn hình 125% và 150%, mở Launcher. | Kính khớp ảnh, chữ không mờ nhòe bất thường, không tràn. |
| GL-10 | P3 | Kiểm tra hiệu năng: chuyển game liên tục 20 lần, để Launcher mở 10 phút. | Không giật nặng, RAM không tăng liên tục (theo dõi Task Manager). |
| GL-11 | P2 | So sánh giao diện **Tối** với bản Launcher trước khi có Glassy (nếu có ảnh chụp cũ). | Không khác biệt về màu, khoảng cách, bố cục. |

## 15. Cập nhật Launcher và cài đặt

| ID | Ưu tiên | Bước | Kết quả mong đợi |
|---|---|---|---|
| LU-01 | P2 | Có bản Launcher mới trên feed. | Hiện hộp thoại bắt buộc cập nhật với ghi chú phát hành; tải có thanh tiến trình. |
| LU-02 | P2 | Ngắt mạng giữa lúc tải bản cập nhật. | Báo lỗi tải, thử lại được; không chạy file tải dở. |
| IS-01 | P2 | Cài bằng installer vào thư mục có khoảng trắng/tiếng Việt. | Cài xong có shortcut Desktop `TGLauncher.exe`, Start Menu `TGLauncher`; tìm được qua Windows Search. |
| IS-02 | P2 | Chạy trên máy sạch **không** có .NET runtime. | Launcher vẫn chạy (bản self-contained). |
| IS-03 | P1 | Gỡ Launcher bằng uninstaller. | Không xóa `%LOCALAPPDATA%\GameTranslationLauncher` (receipt/backup vẫn còn). Cài lại vẫn quản lý được game đã cài. |

---

## 16. Mẫu báo bug (vui lòng trả lời theo mẫu này)

```
### BUG-<số thứ tự>: <tiêu đề ngắn>
- Test case: <ID, ví dụ IN-07>
- Mức độ: Nghiêm trọng (mất/hỏng dữ liệu, crash) | Cao (chức năng chính sai) | Trung bình | Thấp (hình thức)
- Môi trường: Windows <phiên bản>, độ phân giải <...>, scale <...>%, kiểu giao diện <Glassy/Tối>, ngôn ngữ <vi/en>
- Bước tái hiện:
  1. ...
  2. ...
- Kết quả thực tế: ...
- Kết quả mong đợi: ...
- Tần suất: Luôn luôn | Thỉnh thoảng (x/y lần)
- Bằng chứng: ảnh chụp / đoạn log trong %LOCALAPPDATA%\GameTranslationLauncher\logs / nội dung settings.json hoặc receipt.json liên quan
```

Cuối báo cáo, thêm bảng tổng hợp: số case **Đạt / Lỗi / Bỏ qua (kèm lý do)**.
