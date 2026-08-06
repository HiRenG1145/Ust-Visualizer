using System.Text;
using UstViz.Core.Abstractions;

namespace UstViz.Core.IO;

/// <summary>基于 System.IO 的默认文件系统实现。</summary>
public sealed class SystemFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string ReadAllText(string path, Encoding encoding) => File.ReadAllText(path, encoding);

    public void WriteAllText(string path, string contents, Encoding encoding) =>
        File.WriteAllText(path, contents, encoding);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
}


