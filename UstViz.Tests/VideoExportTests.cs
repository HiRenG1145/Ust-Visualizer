using SkiaSharp;
using UstViz.Core.Config;
using UstViz.Core.Parsing;
using UstViz.Rendering.Video;
using Xunit.Abstractions;

namespace UstViz.Tests;

/// <summary>视频写入与导出测试。</summary>
public class VideoExportTests
{
    private readonly ITestOutputHelper _output;

    public VideoExportTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void MjpegAviWriter_Produces_Valid_Avi()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ustviz_{Guid.NewGuid():N}.avi");
        try
        {
            using (var writer = new MjpegAviVideoWriter(path, 160, 90, 10))
            {
                using var frame = CreateFrame(160, 90, 255, 0, 0);
                writer.AddFrame(frame);
                using var frame2 = CreateFrame(160, 90, 0, 255, 0);
                writer.AddFrame(frame2);
            }

            var bytes = File.ReadAllBytes(path);

            Assert.True(Contains(bytes, "RIFF"), "缺少 RIFF 标记");
            Assert.True(Contains(bytes, "AVI "), "缺少 AVI 标记");
            Assert.True(Contains(bytes, "hdrl"), "缺少 hdrl");
            Assert.True(Contains(bytes, "strl"), "缺少 strl");
            Assert.True(Contains(bytes, "movi"), "缺少 movi");
            Assert.True(Contains(bytes, "idx1"), "缺少 idx1 索引");

            Assert.Equal(2, ReadFrameCount(bytes));

            _output.WriteLine($"AVI 大小: {bytes.Length} 字节");
        }
        finally
        {
            File.Delete(path);
        }
    }


    [Fact]
    public void MjpegAviWriter_Chunk_Layout_Is_Correct()
    {
        // 回归测试：每个 00dc 帧块必须是 4 字节 size + 紧跟 JPEG SOI (FFD8)。
        // 曾因 SKData.Size 是 long 而误写 8 字节 size，导致整段视频错位无法打开。
        var path = Path.Combine(Path.GetTempPath(), $"ustviz_{Guid.NewGuid():N}.avi");
        try
        {
            using (var writer = new MjpegAviVideoWriter(path, 64, 36, 5))
            {
                using var f1 = CreateFrame(64, 36, 255, 0, 0);
                using var f2 = CreateFrame(64, 36, 0, 255, 0);
                using var f3 = CreateFrame(64, 36, 0, 0, 255);
                writer.AddFrame(f1);
                writer.AddFrame(f2);
                writer.AddFrame(f3);
            }

            var bytes = File.ReadAllBytes(path);
            int moviPos = IndexOf(bytes, "movi");
            int idx1Pos = IndexOf(bytes, "idx1");
            Assert.True(moviPos >= 0, "缺少 movi");
            Assert.True(idx1Pos > moviPos, "缺少 idx1");

            // 只扫描 movi 数据区（movi 标签后 ~ idx1 前），避免把 idx1 索引条目误认为帧块
            int count = 0;
            int i = moviPos + 4;
            while (i < idx1Pos - 16)
            {
                if (bytes[i] == (byte)'0' && bytes[i + 1] == (byte)'0' &&
                    bytes[i + 2] == (byte)'d' && bytes[i + 3] == (byte)'c')
                {
                    int size = BitConverter.ToInt32(bytes, i + 4);
                    Assert.True(size > 0, "帧块 size 必须为正");
                    // size 字段后应直接是 JPEG SOI（曾误写 8 字节 size 导致错位）
                    Assert.Equal(0xFF, bytes[i + 8]);
                    Assert.Equal(0xD8, bytes[i + 9]);
                    count++;
                    i += 8 + size + ((size & 1) == 1 ? 1 : 0); // 帧块 + 2 字节对齐 padding
                }
                else
                {
                    i++;
                }
            }

            Assert.Equal(3, count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task VideoExportService_Exports_Frames()
    {
        var project = new UstParser().ParseFile(Path.Combine(AppContext.BaseDirectory, "Test", "热异常.ust"));
        var config = AppConfig.CreateDefault();
        config.Width = 320;
        config.Height = 180;
        config.Fps = 10;
        config.FallbackFont = "simsun";

        var path = Path.Combine(Path.GetTempPath(), $"ustviz_{Guid.NewGuid():N}.avi");
        try
        {
            using var writer = new MjpegAviVideoWriter(path, config.Width, config.Height, config.Fps);
            var service = new VideoExportService();
            await service.ExportAsync(project, config, writer, maxFrames: 10);

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(10, ReadFrameCount(bytes));
            Assert.True(bytes.Length > 10_000, "AVI 文件过小");
            _output.WriteLine($"导出 10 帧成功，大小 {bytes.Length} 字节");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SKBitmap CreateFrame(int width, int height, byte r, byte g, byte b)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(r, g, b));
        return bitmap;
    }

    private static int ReadFrameCount(byte[] bytes)
    {
        int idx = IndexOf(bytes, "avih");
        Assert.True(idx >= 0, "缺少 avih 块");
        return BitConverter.ToInt32(bytes, idx + 4 + 4 + 16); // fourcc(4) + size(4) + 前 4 个 DWORD(16)
    }

    private static bool Contains(byte[] bytes, string text)
    {
        var needle = System.Text.Encoding.ASCII.GetBytes(text);
        for (int i = 0; i <= bytes.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (bytes[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return true;
        }
        return false;
    }

    private static int IndexOf(byte[] bytes, string text)
    {
        var needle = System.Text.Encoding.ASCII.GetBytes(text);
        for (int i = 0; i <= bytes.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (bytes[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }
}





