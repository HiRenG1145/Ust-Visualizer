using SkiaSharp;
using UstViz.Core.Config;
using UstViz.Core.Models;

namespace UstViz.Rendering.Video;

/// <summary>
/// 视频导出服务：逐帧渲染并写入视频。
/// 进度通过 IProgress 回调（0~1），取消通过 CancellationToken。
/// </summary>
public sealed class VideoExportService
{
    /// <summary>导出视频。writer 决定封装格式（AVI/MP4 等）。</summary>
    public async Task ExportAsync(
        UstProject project,
        AppConfig config,
        IVideoWriter writer,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        int? maxFrames = null)
    {
        double leadIn = config.Width / config.ScrollSpeed;
        double totalDuration = project.TotalDuration + leadIn + leadIn;
        int totalFrames = (int)(totalDuration * config.Fps);
        if (maxFrames is > 0 && maxFrames < totalFrames)
            totalFrames = maxFrames.Value;

        using var renderer = new FrameRenderer(config);

        for (int i = 0; i < totalFrames; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double currentTime = i / (double)config.Fps;
            using var frame = renderer.Render(project, currentTime, totalDuration);
            writer.AddFrame(frame);

            progress?.Report((double)(i + 1) / totalFrames);

            // 让 UI 线程有机会处理进度/取消
            if (i % 10 == 0)
                await Task.Yield();
        }

        writer.Finish();
    }

    /// <summary>计算导出总帧数。</summary>
    public static int ComputeTotalFrames(UstProject project, AppConfig config)
    {
        double leadIn = config.Width / config.ScrollSpeed;
        double totalDuration = project.TotalDuration + leadIn + leadIn;
        return (int)(totalDuration * config.Fps);
    }
}

