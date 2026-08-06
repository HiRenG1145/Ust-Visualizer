using System.Text.Json;
using SkiaSharp;
using UstViz.Core.Config;
using UstViz.Core.Models;
using UstViz.Core.Parsing;
using UstViz.Rendering;
using Xunit.Abstractions;

namespace UstViz.Tests;

/// <summary>
/// 渲染引擎与 Python 版 UstViz.py 对比测试。
/// 基准数据由 tools/generate_python_frames.py 生成（Test/python_frames/*.png、python_frame_snapshot.json）。
/// </summary>
public class FrameRendererTests
{
    private readonly ITestOutputHelper _output;

    public FrameRendererTests(ITestOutputHelper output) => _output = output;

    private static string TestRoot => Path.Combine(AppContext.BaseDirectory, "Test");

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string OutputDir => Path.Combine(RepoRoot, "Test", "output");

    private static readonly int[] Frames = [30, 60, 120, 240];

    private static UstProject ParseSample()
    {
        var parser = new UstParser();
        return parser.ParseFile(Path.Combine(TestRoot, "热异常.ust"));
    }

    private static AppConfig BuildConfig(bool simple)
    {
        var c = AppConfig.CreateDefault();
        c.Width = 640;
        c.Height = 360;
        c.Fps = 30;
        c.ScrollSpeed = 500;
        c.FallbackFont = "simsun"; // 与 Python 基准一致

        if (simple)
        {
            c.NoteCornerRadius = 0;
            c.NoteShadow = false;
            c.ShowLyric = false;
            c.ShowPitchCurve = false;
        }

        return c;
    }

    private static double ComputeTotalDuration(UstProject project, AppConfig config)
    {
        double leadIn = config.Width / config.ScrollSpeed;
        return project.TotalDuration + leadIn + leadIn;
    }

    [Fact]
    public void Simple_Frames_Match_Python_Pixels()
    {
        var project = ParseSample();
        var config = BuildConfig(simple: true);
        using var renderer = new FrameRenderer(config);
        double total = ComputeTotalDuration(project, config);

        foreach (int frame in Frames)
        {
            double currentTime = frame / (double)config.Fps;
            using var cs = renderer.Render(project, currentTime, total);
            using var py = SKBitmap.Decode(Path.Combine(TestRoot, "python_frames", $"simple_{frame}.png"));

            Assert.Equal(py.Width, cs.Width);
            Assert.Equal(py.Height, cs.Height);

            var (diffRatio, diffPixels) = PixelDiff(cs, py);
            _output.WriteLine($"frame {frame}: 差异像素 {diffPixels} / {cs.Width * cs.Height} = {diffRatio:P2}");

            Assert.True(diffRatio < 0.03,
                $"frame {frame} 像素差异 {diffRatio:P2} 超过阈值 3%");
        }
    }

    [Fact]
    public void NoteLayouts_Match_Python_Snapshot()
    {
        var project = ParseSample();
        var config = BuildConfig(simple: false);
        using var renderer = new FrameRenderer(config);
        double total = ComputeTotalDuration(project, config);

        using var doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(TestRoot, "python_frame_snapshot.json")));
        var root = doc.RootElement;

        foreach (var frameProp in root.EnumerateObject())
        {
            int frame = int.Parse(frameProp.Name);
            double currentTime = frame / (double)config.Fps;

            var layouts = renderer.ComputeNoteLayouts(project, currentTime, total);
            var expected = frameProp.Value;

            Assert.Equal(expected.GetArrayLength(), layouts.Count);
            for (int i = 0; i < layouts.Count; i++)
            {
                var e = expected[i];
                var a = layouts[i];

                Assert.Equal(e.GetProperty("number").GetInt32(), a.Number);
                Assert.Equal(e.GetProperty("start_x").GetDouble(), a.StartX, 6);
                Assert.Equal(e.GetProperty("end_x").GetDouble(), a.EndX, 6);
                Assert.Equal(e.GetProperty("y").GetDouble(), a.Y, 6);
                Assert.Equal(e.GetProperty("is_active").GetBoolean(), a.IsActive);
                Assert.Equal(e.GetProperty("alpha").GetInt32(), a.Alpha);
                Assert.Equal(e.GetProperty("r").GetInt32(), a.R);
                Assert.Equal(e.GetProperty("g").GetInt32(), a.G);
                Assert.Equal(e.GetProperty("b").GetInt32(), a.B);
            }
        }
    }

    [Fact]
    public void Renders_Full_Frames_To_Output()
    {
        var project = ParseSample();
        var config = BuildConfig(simple: false);
        using var renderer = new FrameRenderer(config);
        double total = ComputeTotalDuration(project, config);

        Directory.CreateDirectory(OutputDir);
        foreach (int frame in Frames)
        {
            double currentTime = frame / (double)config.Fps;
            using var bitmap = renderer.Render(project, currentTime, total);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var path = Path.Combine(OutputDir, $"full_{frame}.png");
            File.WriteAllBytes(path, data.ToArray());
            _output.WriteLine($"已输出: {path}");
        }

        Assert.True(File.Exists(Path.Combine(OutputDir, "full_120.png")));
    }

    private static (double DiffRatio, long DiffPixels) PixelDiff(SKBitmap a, SKBitmap b)
    {
        long diff = 0;
        long total = (long)a.Width * a.Height;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                var pa = a.GetPixel(x, y);
                var pb = b.GetPixel(x, y);
                if (pa.Red != pb.Red || pa.Green != pb.Green || pa.Blue != pb.Blue)
                    diff++;
            }
        }
        return ((double)diff / total, diff);
    }
}
