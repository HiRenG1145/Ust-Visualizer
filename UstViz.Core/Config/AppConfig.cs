namespace UstViz.Core.Config;

/// <summary>
/// 应用配置（对应 UstViz.py ModernGUI.default_config）。
/// 颜色以 #RRGGBB 十六进制字符串存储，与 Python 版 JSON 保存格式一致。
/// </summary>
public sealed class AppConfig
{
    // 分辨率与帧率
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps { get; set; } = 30;

    // 颜色
    public string NoteColor { get; set; } = "#FF0000";
    public string ActiveNoteColor { get; set; } = "#00FF00";
    public string LyricColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#000000";
    public string JudgmentLineColor { get; set; } = "#FFFF00";
    public string PitchCurveColor { get; set; } = "#00FFFF";

    // 动画
    public double JudgmentLinePosition { get; set; } = 0.2;
    public double ScrollSpeed { get; set; } = 500;
    public double FadeDuration { get; set; } = 1.0;
    public int VerticalOffset { get; set; } = 0;

    // 字体
    public string FontPath { get; set; } = "";
    public int FontSize { get; set; } = 24;

    /// <summary>
    /// 首选回退字体族名。默认空字符串表示"由平台默认值决定"
    /// （见 <see cref="UstViz.Core.Abstractions.IPlatformDefaults.PreferredFontFamilies"/>），
    /// 避免把 Windows 专用字体名（如 simsun）写死进配置。
    /// </summary>
    public string FallbackFont { get; set; } = "";

    // 音符样式
    public int NoteHeight { get; set; } = 20;
    public int NoteCornerRadius { get; set; } = 5;
    public bool NoteShadow { get; set; } = true;
    public bool TransparentBackground { get; set; } = false;
    public int LyricOffset { get; set; } = 15;

    // 输出
    /// <summary>输出格式："avi"（默认，无依赖）或 "mp4"（需要 ffmpeg）。</summary>
    public string OutputFormat { get; set; } = "avi";

    /// <summary>ffmpeg 可执行文件路径；为空时自动检测（PATH / 常见安装位置）。</summary>
    public string FfmpegPath { get; set; } = "";
    // 歌词与音高曲线
    public bool ShowLyric { get; set; } = true;
    public bool ShowPitchCurve { get; set; } = true;
    public int PitchCurveWidth { get; set; } = 3;
    public bool PitchCurveShadow { get; set; } = true;
    public bool PitchCurveDots { get; set; } = true;
    public int PitchCurveDotSize { get; set; } = 5;
    public int PitchCurveSmoothness { get; set; } = 50;

    public static AppConfig CreateDefault() => new();
}


