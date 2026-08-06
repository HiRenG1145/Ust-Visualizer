using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace UstViz.App.Views;

/// <summary>简单的 RGB 颜色选择对话框，返回 #RRGGBB 字符串。</summary>
public partial class ColorPickerDialog : Window
{
    private Color _color;

    public ColorPickerDialog(string initialHex)
    {
        InitializeComponent();
        _color = TryParse(initialHex) ?? Colors.White;
        RedSlider.Value = _color.R;
        GreenSlider.Value = _color.G;
        BlueSlider.Value = _color.B;
        UpdatePreview();
    }

    private void OnRgbChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _color = Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
        UpdatePreview();
    }

    private void OnHexChanged(object? sender, TextChangedEventArgs e)
    {
        if (TryParse(HexBox.Text) is { } c)
        {
            _color = c;
            RedSlider.Value = c.R;
            GreenSlider.Value = c.G;
            BlueSlider.Value = c.B;
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        Preview.Background = new SolidColorBrush(_color);
        RedValue.Text = _color.R.ToString();
        GreenValue.Text = _color.G.ToString();
        BlueValue.Text = _color.B.ToString();
        HexBox.Text = $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}";
    }

    private void OnOk(object? sender, RoutedEventArgs e) =>
        Close($"#{_color.R:X2}{_color.G:X2}{_color.B:X2}");

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private static Color? TryParse(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        hex = hex.Trim().TrimStart('#');
        if (hex.Length is not (6 or 8))
            return null;
        try
        {
            byte r = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromRgb(r, g, b);
        }
        catch
        {
            return null;
        }
    }
}
