using SkiaSharp;
using UstViz.Core.Abstractions;
using UstViz.Core.Config;
using UstViz.Core.Platform;

namespace UstViz.Rendering;

/// <summary>
/// 字体解析：优先用户指定的字体文件（FontPath），否则使用配置的 FallbackFont，
/// 再否则按平台候选字体（IPlatformDefaults）选择，最后回退到系统默认字体。
/// </summary>
public sealed class FontResolver : IDisposable
{
    private readonly SKTypeface? _custom;
    private readonly SKTypeface? _platform;

    public FontResolver(AppConfig config, IPlatformDefaults? platform = null)
    {
        var pf = platform ?? DefaultPlatform.Instance;

        if (!string.IsNullOrWhiteSpace(config.FontPath) && File.Exists(config.FontPath))
            _custom = SKTypeface.FromFile(config.FontPath);

        string family = !string.IsNullOrWhiteSpace(config.FallbackFont)
            ? config.FallbackFont
            : pf.PreferredFontFamilies.FirstOrDefault() ?? "";

        if (!string.IsNullOrEmpty(family))
            _platform = SKTypeface.FromFamilyName(family);

        Typeface = _custom ?? _platform ?? SKTypeface.Default;
    }

    /// <summary>最终选定的字体。</summary>
    public SKTypeface Typeface { get; }

    public void Dispose()
    {
        if (!ReferenceEquals(Typeface, SKTypeface.Default))
            Typeface.Dispose();
    }
}
