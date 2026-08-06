namespace UstViz.Core.Models;

/// <summary>单个 UST 音符（对应 UstViz.py 中 USTParser 解析出的 note dict）。</summary>
public sealed class UstNote
{
    /// <summary>音符编号（[#N] 中的 N）</summary>
    public int Number { get; set; }

    /// <summary>音符长度（ticks），默认 480</summary>
    public int Length { get; set; } = 480;

    /// <summary>歌词，默认休止符 "R"</summary>
    public string Lyric { get; set; } = "R";

    /// <summary>音高（MIDI 编号），默认 C5=60</summary>
    public int NoteNum { get; set; } = 60;

    /// <summary>PBS（Pitch Bend Start），格式 PBS=X;Y 或 PBS=X</summary>
    public double[] Pbs { get; set; } = [0, 0];

    /// <summary>PBW 各段宽度</summary>
    public List<double> Pbw { get; set; } = [];

    /// <summary>PBY 各点音高偏移</summary>
    public List<double> Pby { get; set; } = [];

    /// <summary>PBM 曲线类型</summary>
    public List<string> Pbm { get; set; } = [];

    /// <summary>PitchBend 数据</summary>
    public List<int> PitchBend { get; set; } = [];

    /// <summary>开始时间（秒）</summary>
    public double StartTime { get; set; }

    /// <summary>结束时间（秒）</summary>
    public double EndTime { get; set; }

    /// <summary>持续时间（秒）</summary>
    public double Duration { get; set; }

    /// <summary>是否为休止符（歌词 R，不区分大小写）</summary>
    public bool IsRest => string.Equals(Lyric, "R", StringComparison.OrdinalIgnoreCase);
}
