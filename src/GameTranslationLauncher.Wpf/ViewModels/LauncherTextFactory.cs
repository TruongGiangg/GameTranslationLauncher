namespace GameTranslationLauncher.Wpf.ViewModels;

/// <summary>
/// Tạo toàn bộ chuỗi presentation WPF cho ngôn ngữ đã lưu của Launcher.
/// </summary>
public static class LauncherTextFactory
{
    public static LauncherSettingsText Create(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? CreateEnglish()
            : CreateVietnamese();

    private static LauncherSettingsText CreateVietnamese() => new(
        "CÀI ĐẶT LAUNCHER",
        "Tùy chỉnh giao diện, gửi góp ý và kết nối với dự án.",
        "GIAO DIỆN",
        "Ngôn ngữ",
        "Tiếng Việt",
        "Tiếng Anh",
        "Màu nhấn",
        "Đỏ TG",
        "Xanh dương",
        "Xanh lục",
        "Tím",
        "Trắng",
        "Cam",
        "Cyan",
        "GỬI BÁO CÁO",
        "Báo lỗi, yêu cầu cập nhật bản dịch hoặc đề xuất game mới.",
        "Loại báo cáo",
        "Báo lỗi",
        "Cập nhật bản dịch",
        "Yêu cầu Việt hóa",
        "Tên game",
        "Chọn game trong thư viện",
        "Nhập tên game bạn muốn Việt hóa",
        "Nội dung chi tiết",
        "MỞ EMAIL ĐỂ GỬI",
        "Chọn hoặc nhập tên game cần báo cáo.",
        "Nhập nội dung report.",
        "Đã mở email nháp. Hãy kiểm tra rồi bấm gửi.",
        "Không thể lưu lựa chọn giao diện.",
        "KẾT NỐI DỰ ÁN",
        "TG Launcher V1.0.0",
        "Đóng",
        CreateVietnameseApplicationText(),
        CreateVietnameseOperationText(),
        CreateVietnameseUpdateText());

    private static LauncherSettingsText CreateEnglish() => new(
        "LAUNCHER SETTINGS",
        "Customize the interface, send feedback, and connect with the project.",
        "INTERFACE",
        "Language",
        "Vietnamese",
        "English",
        "Accent color",
        "TG Crimson",
        "Blue",
        "Emerald",
        "Violet",
        "White",
        "Orange",
        "Cyan",
        "SEND A REPORT",
        "Report an issue, request a translation update, or suggest a new game.",
        "Report type",
        "Bug report",
        "Translation update",
        "Translation request",
        "Game name",
        "Choose a game from the library",
        "Enter the game you want translated",
        "Details",
        "OPEN EMAIL TO SEND",
        "Choose or enter the game name.",
        "Enter the report details.",
        "A draft email is open. Review it, then send it.",
        "Unable to save the interface preference.",
        "PROJECT LINKS",
        "TG Launcher V1.0.0",
        "Close",
        CreateEnglishApplicationText(),
        CreateEnglishOperationText(),
        CreateEnglishUpdateText());

    private static LauncherApplicationText CreateVietnameseApplicationText() => new(
        "vi", "THƯ VIỆN GAME", "Chưa tìm thấy package bản Việt hóa trong catalog local.",
        "Thêm package hợp lệ vào thư mục games để bắt đầu quản lý bản Việt hóa.",
        "Bản việt hóa: {0}", "ĐÃ CÀI", "CÓ CẬP NHẬT", "CHƯA CÀI", "CẦN KIỂM TRA", "XUNG ĐỘT FILE", "CHƯA CÓ BẢN DỊCH", "CHƯA CHỌN THƯ MỤC", "CHƯA KIỂM TRA",
        "Tiếng Việt", "ĐÃ VIỆT HÓA", "CHƯA VIỆT HÓA", "{0} FILE TRONG GÓI", "CHƠI GAME", "CẬP NHẬT", "CÀI BẢN DỊCH", "KIỂM TRA LẠI", "XEM CHI TIẾT", "CHỌN THƯ MỤC GAME", "KIỂM TRA TRẠNG THÁI",
        "KIỂM TRA CẬP NHẬT", "MỞ THƯ MỤC GAME", "GỠ BẢN DỊCH", "MỞ THƯ MỤC", "ĐỔI THƯ MỤC GAME", "Hỗ trợ: ", "Ngôn ngữ: ", "Xác minh: ",
        "TRẠNG THÁI", "DUNG LƯỢNG", "PACKAGE", "GỠ BẢN VIỆT HÓA", "Đang tải catalog local…", "Chưa có package hợp lệ trong catalog local.", "Không thể tải catalog local. Hãy kiểm tra lại package và thử tải lại.",
        "Đã tải {0} game từ catalog local.", "Đã tải {0} game; bỏ qua {1} package lỗi.", "Đang xử lý bản Việt hóa", "HỦY SAU ĐIỂM AN TOÀN", "QUAY LẠI",
        "Thu nhỏ", "Phóng to / khôi phục", "Đóng", "TG Launcher V1.0.0", "Tìm game…");

    private static LauncherApplicationText CreateEnglishApplicationText() => new(
        "en", "GAME LIBRARY", "No translation package was found in the local catalog.",
        "Add a valid package to the games folder to manage its translation.",
        "Translation: {0}", "INSTALLED", "UPDATE AVAILABLE", "NOT INSTALLED", "NEEDS CHECKING", "FILE CONFLICT", "TRANSLATION NOT AVAILABLE", "NO FOLDER SELECTED", "NOT CHECKED",
        "Vietnamese", "TRANSLATED", "NOT TRANSLATED", "{0} FILES IN PACKAGE", "PLAY GAME", "UPDATE", "INSTALL TRANSLATION", "VERIFY AGAIN", "VIEW DETAILS", "CHOOSE GAME FOLDER", "CHECK STATUS",
        "CHECK FOR UPDATES", "OPEN GAME FOLDER", "UNINSTALL TRANSLATION", "OPEN FOLDER", "CHANGE GAME FOLDER", "Support: ", "Language: ", "Verified: ",
        "STATUS", "SIZE", "PACKAGE", "UNINSTALL TRANSLATION", "Loading local catalog…", "No valid package was found in the local catalog.", "Unable to load the local catalog. Check the packages and try again.",
        "Loaded {0} games from the local catalog.", "Loaded {0} games; skipped {1} invalid packages.", "Processing translation", "CANCEL AFTER SAFE POINT", "GO BACK",
        "Minimize", "Maximize / restore", "Close", "TG Launcher V1.0.0", "Search games…");

    private static LauncherOperationText CreateVietnameseOperationText() => new(
        "Đã kiểm tra trạng thái game.", "Đã kiểm tra bản dịch và trạng thái file.", "Không thể kiểm tra trạng thái game.", "Chưa thay đổi thư mục game.", "Đã xác nhận và lưu thư mục game.",
        "Không thể xác nhận thư mục game đã chọn.", "Đang kiểm tra package và lập kế hoạch an toàn.", "Đã hủy trước khi thao tác file.", "Không thể lập kế hoạch cài đặt an toàn.",
        "Gỡ bản Việt hóa?", "Launcher chỉ gỡ các file có trong receipt và khôi phục backup gốc nếu có. File đã bị chỉnh sửa ngoài Launcher sẽ không bị xóa âm thầm.",
        "GỠ BẢN DỊCH", "Đang chuẩn bị thao tác file.", "Không thể hoàn tất thao tác. Kiểm tra log Launcher để biết thêm chi tiết.",
        "Cần chọn thư mục game trước khi cài bản Việt hóa.", "Cài bản Việt hóa?", "Cập nhật bản Việt hóa?",
        "Launcher sẽ xử lý {0} file theo package đã xác minh. Mọi thay đổi có thể hoàn tác theo receipt.", "{0} file đã tồn tại sẽ được backup trước khi thay thế.",
        "Prerequisite sẽ được cài sau khi bạn xác nhận:{0}", "CÀI ĐẶT", "CẬP NHẬT", "Đã hủy thao tác trước khi thay đổi file.", "Đang hủy tại điểm an toàn…",
        "Chọn thư mục cài đặt {0}", "Chưa có thư mục game hợp lệ để mở.", "Đã mở thư mục game trong Explorer.", "Không thể mở thư mục game bằng Windows Explorer.",
        "Chưa có thư mục game hợp lệ để khởi chạy.", "Package chưa khai báo executable để khởi chạy game.", "Không tìm thấy executable game tại đường dẫn đã xác minh.",
        "Đang khởi chạy game.", "Windows không thể khởi chạy executable game.", "Executable path vượt thư mục game đã chọn.",
        "CHI TIẾT XUNG ĐỘT FILE",
        "Launcher phát hiện file trùng với package Việt hóa của {0}, nhưng không có biên nhận cài đặt hợp lệ hoặc package đã thay đổi kể từ lần cài. Để bảo vệ dữ liệu game, Launcher sẽ không tự ghi đè hoặc xóa file này.",
        "Mở thư mục game để kiểm tra bản dịch hoặc mod đã chép thủ công. Nếu muốn cài bằng Launcher, hãy sao lưu và dọn file xung đột, sau đó bấm Đổi thư mục game để kiểm tra lại.",
        "ĐÃ HIỂU");

    private static LauncherOperationText CreateEnglishOperationText() => new(
        "Game status was checked.", "Translation and file status were checked.", "Unable to check the game status.", "The game folder was not changed.", "The game folder was verified and saved.",
        "The selected game folder could not be verified.", "Checking the package and preparing a safe plan.", "Cancelled before changing any files.", "Unable to prepare a safe installation plan.",
        "Uninstall translation?", "Launcher only removes files in its receipt and restores original backups where available. Files changed outside Launcher are never removed silently.",
        "UNINSTALL", "Preparing the file operation.", "The operation could not be completed. Check the Launcher log for details.",
        "Choose a game folder before installing the translation.", "Install translation?", "Update translation?",
        "Launcher will process {0} verified package files. Every change can be reversed using the receipt.", "{0} existing files will be backed up before replacement.",
        "The following prerequisite will be installed after confirmation:{0}", "INSTALL", "UPDATE", "The operation was cancelled before files changed.", "Cancelling at a safe point…",
        "Choose the {0} installation folder", "There is no valid game folder to open.", "The game folder was opened in Explorer.", "Windows Explorer could not open the game folder.",
        "There is no valid game folder to launch.", "The package does not declare an executable to launch the game.", "The game executable was not found at its verified path.",
        "Launching the game.", "Windows could not launch the game executable.", "The executable path is outside the selected game folder.",
        "FILE CONFLICT DETAILS",
        "Launcher found files that overlap the {0} translation package, but there is no valid installation receipt or the package changed after installation. To protect your game files, Launcher will not overwrite or remove these files automatically.",
        "Open the game folder to inspect translations or mods copied manually. To install with Launcher, back up and remove the conflicting files, then choose the game folder again to recheck it.",
        "GOT IT");

    private static LauncherUpdateText CreateVietnameseUpdateText() => new(
        "Đã có bản cập nhật Launcher {0}",
        "TẢI VÀ CÀI ĐẶT",
        "Bạn cần cập nhật lên bản mới nhất để tiếp tục sử dụng Launcher.",
        "Không thể tải bản cập nhật. Kiểm tra kết nối mạng rồi thử lại.",
        "Windows không thể khởi chạy bộ cài đặt vừa tải về.",
        "File tải về không hợp lệ. Vui lòng thử lại.");

    private static LauncherUpdateText CreateEnglishUpdateText() => new(
        "Launcher update {0} is available",
        "DOWNLOAD AND INSTALL",
        "You must update to the latest version to keep using the Launcher.",
        "Unable to download the update. Check your connection and try again.",
        "Windows could not launch the downloaded installer.",
        "The downloaded file is invalid. Please try again.");
}
