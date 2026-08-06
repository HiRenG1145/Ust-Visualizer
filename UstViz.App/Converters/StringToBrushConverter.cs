using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace UstViz.App.Converters;

/// <summary>把 #RRGGBB 十六进制字符串转换为画刷（用于颜色预览块）。</summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public static readonly StringToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && TryParse(hex, out var color))
            return new SolidColorBrush(color);
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as SolidColorBrush)?.Color.ToString();

    private static bool TryParse(string hex, out Color color)
    {
        color = default;
        hex = hex.Trim().TrimStart('#');
        if (hex.Length is not (6 or 8))
            return false;
        try
        {
            byte r = byte.Parse(hex[..2], NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            byte a = hex.Length == 8 ? byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) : (byte)255;
            color = new Color(a, r, g, b);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
