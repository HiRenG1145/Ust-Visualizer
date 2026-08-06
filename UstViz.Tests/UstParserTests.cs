using System.Text.Json;
using UstViz.Core.Config;
using UstViz.Core.Models;
using UstViz.Core.Parsing;
using UstViz.Core.Rendering;

namespace UstViz.Tests;

/// <summary>
/// 与 Python 版 UstViz.py 解析结果对比的测试。
/// 基准数据由 tools/generate_python_baseline.py 生成（Test/python_baseline.json）。
/// </summary>
public class UstParserTests
{
    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "Test", "热异常.ust");

    private static readonly JsonDocument Baseline = LoadBaseline();

    private static JsonDocument LoadBaseline()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Test", "python_baseline.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static UstProject ParseSample()
    {
        var parser = new UstParser();
        return parser.ParseFile(SamplePath);
    }

    [Fact]
    public void Parses_Sample_Metadata()
    {
        var project = ParseSample();

        Assert.Equal(91.5, project.Tempo, 6);
        Assert.Equal("热异常", project.ProjectName);
        Assert.Equal(1239, project.Notes.Count);
        Assert.Equal(228.57, project.TotalDuration, 2);
    }

    [Fact]
    public void Encoding_Detection_Returns_Gbk()
    {
        Assert.Equal("gbk", UstTextEncoding.DetectEncoding(SamplePath));
    }

    [Fact]
    public void Notes_Match_Python_Baseline()
    {
        var project = ParseSample();
        var baselineNotes = Baseline.RootElement.GetProperty("notes");

        Assert.Equal(baselineNotes.GetArrayLength(), project.Notes.Count);

        for (int i = 0; i < project.Notes.Count; i++)
        {
            var b = baselineNotes[i];
            var n = project.Notes[i];

            Assert.Equal(b.GetProperty("number").GetInt32(), n.Number);
            Assert.Equal(b.GetProperty("length").GetInt32(), n.Length);
            Assert.Equal(b.GetProperty("lyric").GetString(), n.Lyric);
            Assert.Equal(b.GetProperty("note_num").GetInt32(), n.NoteNum);
            Assert.Equal(b.GetProperty("start_time").GetDouble(), n.StartTime, 6);
            Assert.Equal(b.GetProperty("end_time").GetDouble(), n.EndTime, 6);
            Assert.Equal(b.GetProperty("duration").GetDouble(), n.Duration, 6);

            AssertPbs(b.GetProperty("pbs"), n.Pbs);
            AssertDoubleList(b.GetProperty("pbw"), n.Pbw);
            AssertDoubleList(b.GetProperty("pby"), n.Pby);
            AssertStringList(b.GetProperty("pbm"), n.Pbm);
            AssertIntList(b.GetProperty("pitch_bend"), n.PitchBend);
        }
    }

    [Fact]
    public void PitchCurves_Match_Python_Baseline()
    {
        var project = ParseSample();
        var baselineCurves = Baseline.RootElement.GetProperty("pitch_curves");

        // 确保基准里有曲线数据
        Assert.True(baselineCurves.EnumerateObject().Any(), "基准数据中没有音高曲线");

        foreach (var prop in baselineCurves.EnumerateObject())
        {
            int index = int.Parse(prop.Name, System.Globalization.CultureInfo.InvariantCulture);
            var expected = prop.Value;
            var actual = PitchCurveCalculator.Calculate(project.Notes[index], resolution: 50);

            Assert.Equal(expected.GetArrayLength(), actual.Count);

            for (int i = 0; i < actual.Count; i++)
            {
                Assert.Equal(expected[i][0].GetDouble(), actual[i].Progress, 6);
                Assert.Equal(expected[i][1].GetDouble(), actual[i].Pitch, 6);
            }
        }
    }

    private static void AssertPbs(JsonElement element, double[] actual)
    {
        Assert.Equal(2, actual.Length);
        Assert.Equal(2, element.GetArrayLength());
        Assert.Equal(element[0].GetDouble(), actual[0], 6);
        Assert.Equal(element[1].GetDouble(), actual[1], 6);
    }

    private static void AssertDoubleList(JsonElement element, List<double> actual)
    {
        Assert.Equal(element.GetArrayLength(), actual.Count);
        for (int i = 0; i < actual.Count; i++)
            Assert.Equal(element[i].GetDouble(), actual[i], 6);
    }

    private static void AssertIntList(JsonElement element, List<int> actual)
    {
        Assert.Equal(element.GetArrayLength(), actual.Count);
        for (int i = 0; i < actual.Count; i++)
            Assert.Equal(element[i].GetInt32(), actual[i]);
    }

    private static void AssertStringList(JsonElement element, List<string> actual)
    {
        Assert.Equal(element.GetArrayLength(), actual.Count);
        for (int i = 0; i < actual.Count; i++)
            Assert.Equal(element[i].GetString(), actual[i]);
    }
}
