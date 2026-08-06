using System.Text;

namespace UstViz.Core.Parsing;

/// <summary>
/// UST 文本读取与编码探测，与 UstViz.py USTParser.parse_file 保持一致：
/// 依次尝试 utf-8 / shift_jis / gbk / big5 / cp932，全部失败后用 UTF-8 忽略错误解码兜底。
/// </summary>
public static class UstTextEncoding
{
    private static readonly (string Name, Encoding Encoding)[] Encodings = BuildEncodings();

    private static (string, Encoding)[] BuildEncodings()
    {
        // .NET Core 需要注册代码页提供程序才能使用 shift_jis/gbk/big5/cp932
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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
    public static string ReadAllText(string path)
    {
        foreach (var (_, encoding) in Encodings)
        {
            try
            {
                return File.ReadAllText(path, encoding);
            }
            catch (DecoderFallbackException)
            {
                // 尝试下一种编码
            }
        }

        return File.ReadAllText(path, new UTF8Encoding(false, false));
    }

    /// <summary>返回成功读取文件所用编码的名称（供调试/日志）。</summary>
    public static string DetectEncoding(string path)
    {
        foreach (var (name, encoding) in Encodings)
        {
            try
            {
                File.ReadAllText(path, encoding);
                return name;
            }
            catch (DecoderFallbackException)
            {
            }
        }

        return "utf-8 (ignore-errors fallback)";
    }
}


