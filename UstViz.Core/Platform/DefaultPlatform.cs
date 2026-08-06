namespace UstViz.Core.Platform;

using UstViz.Core.Abstractions;

/// <summary>按当前操作系统提供平台默认值。</summary>
public sealed class DefaultPlatform : IPlatformDefaults
{
    public static DefaultPlatform Instance { get; } = new();

    public IReadOnlyList<string> PreferredFontFamilies { get; }

    public DefaultPlatform()
    {
        if (OperatingSystem.IsWindows())
            PreferredFontFamilies = ["Microsoft YaHei", "SimSun", "SimHei", "DengXian"];
        else if (OperatingSystem.IsMacOS())
            PreferredFontFamilies = ["PingFang SC", "Heiti SC", "STHeiti"];
        else
            PreferredFontFamilies = ["Noto Sans CJK SC", "WenQuanYi Micro Hei", "Droid Sans Fallback"];
    }
}
