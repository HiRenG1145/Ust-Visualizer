using System.Diagnostics;
using SkiaSharp;

namespace UstViz.Rendering.Video;

/// <summary>
/// 通过 FFmpeg 管道编码视频（高质量 H.264 MP4）。
/// 需要系统可用的 ffmpeg 可执行文件（由调用方提供路径或保证在 PATH 中）。
/// 帧以 RGBA rawvideo 写入 stdin。
/// </summary>
public sealed class FfmpegVideoWriter : IVideoWriter
{
    private readonly Process _process;
    private readonly Stream _stdin;
    private readonly byte[] _rowBuffer;
    private bool _finished;

    public int Width { get; }
    public int Height { get; }
    public int Fps { get; }

    public FfmpegVideoWriter(string outputPath, int width, int height, int fps,
        string ffmpegPath = "ffmpeg", string? extraArgs = null)
    {
        Width = width;
        Height = height;
        Fps = fps;

        var args =
            $"-y -f rawvideo -pix_fmt rgba -s {width}x{height} -r {fps} -i - " +
            $"-c:v libx264 -pix_fmt yuv420p -crf 18 -preset medium " +
            (extraArgs ?? "") +
            $"\"{outputPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _process = new Process { StartInfo = psi };
        if (!_process.Start())
            throw new InvalidOperationException($"无法启动 ffmpeg: {ffmpegPath}");

        // 避免 stderr 缓冲区阻塞
        _process.ErrorDataReceived += (_, e) => { };
        _process.BeginErrorReadLine();

        _stdin = _process.StandardInput.BaseStream;
        _rowBuffer = new byte[width * 4];
    }

    public void AddFrame(SKBitmap frame)
    {
        if (_finished)
            throw new InvalidOperationException("写入器已结束。");
        if (frame.Width != Width || frame.Height != Height)
            throw new ArgumentException($"帧尺寸 {frame.Width}x{frame.Height} 与视频尺寸 {Width}x{Height} 不一致。");

        // SKBitmap (Rgba8888) 内存布局即 RGBA，逐行写入
        var pixels = frame.GetPixelSpan();
        for (int y = 0; y < Height; y++)
        {
            pixels.Slice(y * Width * 4, Width * 4).CopyTo(_rowBuffer);
            _stdin.Write(_rowBuffer, 0, _rowBuffer.Length);
        }
        _stdin.Flush();
    }

    public void Finish()
    {
        if (_finished)
            return;

        _stdin.Close(); // 关闭 stdin 让 ffmpeg 结束
        if (!_process.WaitForExit(30_000))
        {
            try { _process.Kill(entireProcessTree: true); } catch { /* 忽略 */ }
            throw new InvalidOperationException("ffmpeg 编码超时。");
        }
        _process.Dispose();
        _finished = true;
    }

    public void Dispose() => Finish();
}
