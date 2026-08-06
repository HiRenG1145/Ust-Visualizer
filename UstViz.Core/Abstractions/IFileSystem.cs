using System.Text;

namespace UstViz.Core.Abstractions;

/// <summary>
/// 文件系统抽象。Core 不直接依赖 System.IO，便于替换实现（内存文件系统、远程存储等）与单元测试。
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);

    /// <summary>按指定编码读取文本；编码的 DecoderFallback 决定是否抛出 DecoderFallbackException。</summary>
    string ReadAllText(string path, Encoding encoding);

    void WriteAllText(string path, string contents, Encoding encoding);

    void CreateDirectory(string path);
}
