using SkiaSharp;

namespace UstViz.Rendering;

/// <summary>颜色工具：#RRGGBB / #RRGGBBAA 十六进制字符串与 SKColor 互转。</summary>
public static class ColorUtil
{
    /// <summary>解析 #RRGGBB 或 #RRGGBBAA；非法输入返回洋红色以便发现配置问题。</summary>
    public static SKColor ToSkColor(string hex)
    {
        hex = hex.Trim().TrimStart('#');
        if (hex.Length is not (6 or 8) ||
            !byte.TryParse(hex.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) ||
            !byte.TryParse(hex.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) ||
            !byte.TryParse(hex.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return new SKColor(255, 0, 255);
        }

        byte a = 255;
        if (hex.Length == 8)
            byte.TryParse(hex.AsSpan(6, 2), System.Globalization.NumberStyles.HexNumber, null, out a);

        return new SKColor(r, g, b, a);
    }

    /// <summary>把 SKColor 转回 #RRGGBB。</summary>
    public static string ToHex(SKColor color) => $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
}
