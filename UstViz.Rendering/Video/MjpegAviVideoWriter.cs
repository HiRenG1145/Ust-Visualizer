using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace UstViz.Rendering.Video;

/// <summary>
/// Motion-JPEG AVI 视频写入器。
/// 使用 SkiaSharp 将帧编码为 JPEG，封装进标准 AVI（RIFF）容器。
/// 无外部依赖、跨平台；AVI/MJPEG 兼容主流播放器与剪辑软件。
/// </summary>
public sealed class MjpegAviVideoWriter : IVideoWriter
{
    private readonly Stream _stream;
    private readonly BinaryWriter _writer;
    private readonly int _jpegQuality;

    // 需要在结尾回填的偏移
    private long _riffSizePos;
    private long _moviSizePos;
    private long _frameCountPos;
    private long _moviDataStart;   // 'movi' 之后数据起始（用于 idx1 偏移）
    private long _strhDataStart;    // strh 数据起始（用于回填 dwLength）
    private readonly List<(uint Offset, uint Size)> _index = [];
    private int _frameCount;
    private bool _finished;

    public int Width { get; }
    public int Height { get; }
    public int Fps { get; }

    public MjpegAviVideoWriter(string path, int width, int height, int fps, int jpegQuality = 92)
    {
        Width = width;
        Height = height;
        Fps = fps;
        _jpegQuality = jpegQuality;
        _stream = File.Create(path);
        _writer = new BinaryWriter(_stream, Encoding.ASCII, leaveOpen: true);
        WriteHeader();
    }

    private void WriteHeader()
    {
        WriteFourCc("RIFF");
        _riffSizePos = _stream.Position;
        _writer.Write(0); // 稍后回填：文件大小 - 8
        WriteFourCc("AVI ");

        // ---- LIST hdrl ----
        WriteFourCc("LIST");
        _writer.Write(0); // 稍后回填
        long listStart = _stream.Position;
        WriteFourCc("hdrl");

        // avih (56 bytes)
        WriteFourCc("avih");
        _writer.Write(56);
        _writer.Write(1_000_000 / Fps);          // dwMicroSecPerFrame
        _writer.Write(0);                         // dwMaxBytesPerSec
        _writer.Write(0);                         // dwPaddingGranularity
        _writer.Write(0x10);                      // dwFlags: AVIF_HASINDEX
        _frameCountPos = _stream.Position;
        _writer.Write(0);                         // dwTotalFrames（稍后回填）
        _writer.Write(0);                         // dwInitialFrames
        _writer.Write(1);                         // dwStreams
        _writer.Write(0);                         // dwSuggestedBufferSize
        _writer.Write(Width);                     // dwWidth
        _writer.Write(Height);                    // dwHeight
        _writer.Write(new byte[16]);              // dwReserved[4]

        // ---- LIST strl ----
        WriteFourCc("LIST");
        _writer.Write(0);
        long strlStart = _stream.Position;
        WriteFourCc("strl");

        // strh (56 bytes)
        WriteFourCc("strh");
        _writer.Write(56);
        _strhDataStart = _stream.Position;
        WriteFourCc("vids");
        WriteFourCc("MJPG");
        _writer.Write(0);                         // dwFlags
        _writer.Write((ushort)0);                 // wPriority
        _writer.Write((ushort)0);                 // wLanguage
        _writer.Write(0);                         // dwInitialFrames
        _writer.Write(1);                         // dwScale
        _writer.Write(Fps);                       // dwRate
        _writer.Write(0);                         // dwStart
        _writer.Write(0);                         // dwLength（稍后回填）
        _writer.Write(0);                         // dwSuggestedBufferSize
        _writer.Write(0xFFFFFFFFu);               // dwQuality
        _writer.Write(0);                         // dwSampleSize
        _writer.Write((short)0); _writer.Write((short)0);   // rcFrame.left/top
        _writer.Write((short)Width); _writer.Write((short)Height); // rcFrame.right/bottom

        // strf (BITMAPINFOHEADER, 40 bytes)
        WriteFourCc("strf");
        _writer.Write(40);
        _writer.Write(40);                        // biSize
        _writer.Write(Width);                     // biWidth
        _writer.Write(Height);                    // biHeight
        _writer.Write((ushort)1);                 // biPlanes
        _writer.Write((ushort)24);                // biBitCount
        WriteFourCc("MJPG");                      // biCompression
        _writer.Write(0);                         // biSizeImage
        _writer.Write(0);                         // biXPelsPerMeter
        _writer.Write(0);                         // biYPelsPerMeter
        _writer.Write(0);                         // biClrUsed
        _writer.Write(0);                         // biClrImportant

        // 回填 strl LIST 大小
        PatchSize(strlStart);

        // ---- LIST movi ----
        WriteFourCc("LIST");
        _moviSizePos = _stream.Position;
        _writer.Write(0); // 稍后回填
        WriteFourCc("movi");
        _moviDataStart = _stream.Position;

        // 回填 hdrl LIST 大小
        PatchSize(listStart);
    }

    public void AddFrame(SKBitmap frame)
    {
        if (_finished)
            throw new InvalidOperationException("写入器已结束。");
        if (frame.Width != Width || frame.Height != Height)
            throw new ArgumentException($"帧尺寸 {frame.Width}x{frame.Height} 与视频尺寸 {Width}x{Height} 不一致。");

        using var image = SKImage.FromBitmap(frame);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, _jpegQuality);
        if (data is null)
            throw new InvalidOperationException("JPEG 编码失败。");

        uint chunkOffset = (uint)(_stream.Position - _moviDataStart);

        WriteFourCc("00dc");
        _writer.Write((int)data.Size); // 注意：SKData.Size 是 long，必须转 int（AVI size 字段 4 字节）
        _writer.Write(data.ToArray());

        // 2 字节对齐
        if ((data.Size & 1) == 1)
            _writer.Write((byte)0);

        _index.Add((chunkOffset, (uint)data.Size));
        _frameCount++;
    }

    public void Finish()
    {
        if (_finished)
            return;

        // 回填总帧数（avih.dwTotalFrames 与 strh.dwLength）
        _stream.Seek(_frameCountPos, SeekOrigin.Begin);
        _writer.Write(_frameCount);
        _stream.Seek(_strhDataStart + 32, SeekOrigin.Begin);
        _writer.Write(_frameCount);
        _stream.Seek(0, SeekOrigin.End);

        // 回填 movi LIST 大小（数据 + 4 字节 'movi' 标签）
        long moviEnd = _stream.Position;
        _stream.Seek(_moviSizePos, SeekOrigin.Begin);
        _writer.Write((int)(moviEnd - _moviDataStart + 4));
        _writer.Seek(0, SeekOrigin.End);

        // ---- idx1 ----
        long idx1Start = _stream.Position;
        WriteFourCc("idx1");
        _writer.Write(_index.Count * 16);
        foreach (var (offset, size) in _index)
        {
            WriteFourCc("00dc");
            _writer.Write(0x10);          // AVIIF_KEYFRAME
            _writer.Write(offset);
            _writer.Write(size);
        }

        // 回填 RIFF 大小
        long fileEnd = _stream.Position;
        _stream.Seek(_riffSizePos, SeekOrigin.Begin);
        _writer.Write((int)(fileEnd - 8));
        _writer.Seek(0, SeekOrigin.End);

        _writer.Flush();
        _stream.Dispose();
        _finished = true;
    }

    private void WriteFourCc(string fourCc)
    {
        Span<byte> buf = stackalloc byte[4];
        Encoding.ASCII.GetBytes(fourCc, buf);
        _stream.Write(buf);
    }

    /// <summary>回填某个 LIST 块的大小字段（size 字段位于 listStart - 4 处，值为当前偏移与 listStart 之差）。</summary>
    private void PatchSize(long listStart)
    {
        long sizeFieldPos = listStart - 4;
        long size = _stream.Position - listStart;

        _stream.Seek(sizeFieldPos, SeekOrigin.Begin);
        _writer.Write((int)size);
        _stream.Seek(0, SeekOrigin.End);
    }

    public void Dispose() => Finish();
}




