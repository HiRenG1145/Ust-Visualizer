using System.Text;
using UstViz.Core.Abstractions;
using UstViz.Core.IO;

namespace UstViz.Core.Parsing;

/// <summary>
/// UST 文本读取与编码探测，与 UstViz.py USTParser.parse_file 保持一致：
/// 依次尝试 utf-8 / shift_jis / gbk / big5，全部失败后用 UTF-8 忽略错误解码兜底。
/// 文件访问通过 IFileSystem 完成，编码列表可通过构造参数替换。
/// </summary>
public sealed class UstTextEncoding
{
    private readonly IFileSystem _fileSystem;
    private readonly (string Name, Encoding Encoding)[] _encodings;

    /// <summary>
    /// 创建编码探测器。默认使用系统文件系统与默认编码列表；
    /// 可注入自定义 IFileSystem 或编码列表，便于测试与替换。
    /// </summary>
    public UstTextEncoding(IFileSystem? fileSystem = null, IReadOnlyList<Encoding>? encodings = null)
    {
        _fileSystem = fileSystem ?? new SystemFileSystem();
        _encodings = BuildEncodings(encodings);
    }

    private static (string, Encoding)[] BuildEncodings(IReadOnlyList<Encoding>? encodings)
    {
        // .NET Core 需要注册代码页提供程序才能使用 shift_jis/gbk/big5（跨平台可用）
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (encodings is { Count: > 0 })
            return encodings.Select(e => (e.WebName, e)).ToArray();

        return
        [
            ("utf-8", new UTF8Encoding(false, true)), // utf-8（严格解码）
            ("shift_jis", Strict(932)),               // shift_jis / cp932 同代码页
            ("gbk", Strict(936)),
            ("big5", Strict(950)),
        ];
    }

    private static Encoding Strict(int codePage) =>
        Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

    /// <summary>读取文本，自动探测编码；全部失败时使用 UTF-8 忽略错误解码。</summary>
    public string ReadAllText(string path)
    {
        foreach (var (_, encoding) in _encodings)
        {
            try
            {
                return _fileSystem.ReadAllText(path, encoding);
            }
            catch (DecoderFallbackException)
            {
                // 尝试下一种编码
            }
        }

        return _fileSystem.ReadAllText(path, new UTF8Encoding(false, false));
    }

    /// <summary>返回成功读取文件所用编码的名称（供调试/日志）。</summary>
    public string DetectEncoding(string path)
    {
        foreach (var (name, encoding) in _encodings)
        {
            try
            {
                _fileSystem.ReadAllText(path, encoding);
                return name;
            }
            catch (DecoderFallbackException)
            {
            }
        }

        return "utf-8 (ignore-errors fallback)";
    }
}

