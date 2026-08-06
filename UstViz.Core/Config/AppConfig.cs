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
    public string FallbackFont { get; set; } = "simsun";

    // 音符样式
    public int NoteHeight { get; set; } = 20;
    public int NoteCornerRadius { get; set; } = 5;
    public bool NoteShadow { get; set; } = true;
    public bool TransparentBackground { get; set; } = false;
    public int LyricOffset { get; set; } = 15;

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
