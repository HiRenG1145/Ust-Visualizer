namespace UstViz.Core.Abstractions;

/// <summary>
/// 平台相关默认值的抽象。把操作系统差异（如默认中文字体）隔离在实现中，
/// Core 其余部分不感知具体平台。
/// </summary>
public interface IPlatformDefaults
{
    /// <summary>按优先级排列的中文字体族候选（渲染回退用，与具体平台相关）。</summary>
    IReadOnlyList<string> PreferredFontFamilies { get; }
}
