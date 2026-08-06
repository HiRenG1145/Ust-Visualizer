namespace UstViz.Core.Models;

/// <summary>解析后的 UST 工程（对应 UstViz.py 中 USTParser 的实例字段）。</summary>
public sealed class UstProject
{
    /// <summary>有效音符列表（不含休止符）</summary>
    public List<UstNote> Notes { get; set; } = [];

    /// <summary>速度（BPM），默认 120.0</summary>
    public double Tempo { get; set; } = 120.0;

    /// <summary>项目名称</summary>
    public string ProjectName { get; set; } = "";

    /// <summary>总时长（秒）= 最后一个音符的结束时间</summary>
    public double TotalDuration { get; set; }
}
