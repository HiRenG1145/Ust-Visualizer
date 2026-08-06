using System.Globalization;
using UstViz.Core.Config;
using UstViz.Core.Parsing;
using UstViz.Rendering.Video;

// UstViz.Cli —— 把 UST 工程直接导出为视频
// 用法示例:
//   UstViz.Cli --ust 热异常.ust --out out.avi --width 1280 --height 720 --fps 30
//   UstViz.Cli --ust 热异常.ust --out out.mp4 --ffmpeg C:\ffmpeg\bin\ffmpeg.exe
//   UstViz.Cli --ust 热异常.ust --max-frames 300   (预览前 10 秒)

static void PrintUsage()
{
    Console.WriteLine("""
        UstViz.Cli - UST 可视化导出工具
        用法:
          UstViz.Cli --ust <文件.ust> [选项]

        选项:
          --ust <path>          UST 文件（必填）
          --out <path>          输出视频路径（默认与 UST 同名）
          --width <int>         宽度，默认 1920
          --height <int>        高度，默认 1080
          --fps <int>           帧率，默认 30
          --quality <int>       JPEG 质量 1-100（仅 AVI），默认 92
          --ffmpeg <path>       ffmpeg 可执行文件路径；提供则输出 H.264 MP4
          --max-frames <int>    只导出前 N 帧（调试/预览用）
          --help                显示帮助
        """);
}

static string? GetArg(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return null;
}

static bool HasFlag(string[] args, string name) =>
    args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

if (HasFlag(args, "--help") || args.Length == 0 || GetArg(args, "--ust") is null)
{
    PrintUsage();
    return;
}

var ustPath = GetArg(args, "--ust")!;
if (!File.Exists(ustPath))
{
    Console.Error.WriteLine($"找不到 UST 文件: {ustPath}");
    return;
}

var config = AppConfig.CreateDefault();
if (int.TryParse(GetArg(args, "--width"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var w)) config.Width = w;
if (int.TryParse(GetArg(args, "--height"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)) config.Height = h;
if (int.TryParse(GetArg(args, "--fps"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fps)) config.Fps = fps;
int quality = int.TryParse(GetArg(args, "--quality"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ? q : 92;
int? maxFrames = int.TryParse(GetArg(args, "--max-frames"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var mf) ? mf : null;
var ffmpegPath = GetArg(args, "--ffmpeg");

Console.WriteLine($"解析 UST: {ustPath}");
var parser = new UstParser { Log = Console.WriteLine };
var project = parser.ParseFile(ustPath);
Console.WriteLine($"音符数: {project.Notes.Count}, 总时长: {project.TotalDuration:F2}s, 速度: {project.Tempo} BPM");

string outputPath = GetArg(args, "--out") ?? Path.ChangeExtension(ustPath, ffmpegPath is null ? ".avi" : ".mp4");

int totalFrames = VideoExportService.ComputeTotalFrames(project, config);
Console.WriteLine($"输出: {outputPath} | {config.Width}x{config.Height} @ {config.Fps}fps | 共 {totalFrames} 帧");

using IVideoWriter writer = ffmpegPath is null
    ? new MjpegAviVideoWriter(outputPath, config.Width, config.Height, config.Fps, quality)
    : new FfmpegVideoWriter(outputPath, config.Width, config.Height, config.Fps, ffmpegPath);

var service = new VideoExportService();
var progress = new Progress<double>(p =>
{
    if (p % 0.05 < 0.001)
        Console.Write($"\r进度: {p:P0}   ");
});

var sw = System.Diagnostics.Stopwatch.StartNew();
await service.ExportAsync(project, config, writer, progress, maxFrames: maxFrames);
sw.Stop();

Console.WriteLine($"\r完成: {outputPath} | 用时 {sw.Elapsed.TotalSeconds:F1}s");
