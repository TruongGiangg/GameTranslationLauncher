using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameTranslationLauncher.Wpf.Converters;

/// <summary>
/// Ẩn phần tử khi chuỗi rỗng hoặc chỉ có khoảng trắng — dùng cho các khối nội dung tùy chọn
/// như release notes, không nên chiếm chỗ khi không có dữ liệu.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
