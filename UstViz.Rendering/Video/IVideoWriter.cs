using SkiaSharp;

namespace UstViz.Rendering.Video;

/// <summary>
/// 视频写入抽象：接收渲染帧，编码并写入视频文件。
/// 实现可替换（如 MJPEG AVI、FFmpeg 管道等）。
/// </summary>
public interface IVideoWriter : IDisposable
{
    /// <summary>视频宽度（像素）。</summary>
    int Width { get; }

    /// <summary>视频高度（像素）。</summary>
    int Height { get; }

    /// <summary>帧率。</summary>
    int Fps { get; }

    /// <summary>写入一帧。</summary>
    void AddFrame(SKBitmap frame);

    /// <summary>结束写入并关闭文件（完成所有帧后调用一次）。</summary>
    void Finish();
}
