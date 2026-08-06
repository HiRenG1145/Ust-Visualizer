using System.Text;
using UstViz.Core.Abstractions;
using UstViz.Core.Config;
using UstViz.Core.Parsing;
using UstViz.Core.Platform;

namespace UstViz.Tests;

/// <summary>验证 Core 与系统解耦：通过注入自定义实现即可运行，不依赖具体平台。</summary>
public class DecouplingTests
{
    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "Test", "热异常.ust");

    [Fact]
    public void Parser_Works_With_InMemory_FileSystem()
    {
        var fs = new MemoryFileSystem();
        fs.Add(SamplePath, File.ReadAllBytes(SamplePath));

        var parser = new UstParser(fs);
        var project = parser.ParseFile(SamplePath);

        Assert.Equal(1239, project.Notes.Count);
        Assert.Equal(91.5, project.Tempo, 6);
    }

    [Fact]
    public void ConfigFile_Works_With_InMemory_FileSystem()
    {
        var fs = new MemoryFileSystem();
        var configFile = new ConfigFile(fs);
        var config = AppConfig.CreateDefault();
        config.Width = 640;

        configFile.Save("/virtual/config.json", config);
        var loaded = configFile.Load("/virtual/config.json");

        Assert.Equal(640, loaded.Width);
    }

    [Fact]
    public void PlatformDefaults_Provide_Font_Candidates()
    {
        var platform = DefaultPlatform.Instance;
        Assert.NotEmpty(platform.PreferredFontFamilies);
    }

    /// <summary>基于内存字节存储的 IFileSystem 实现，用于验证解耦。</summary>
    private sealed class MemoryFileSystem : IFileSystem
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string path, byte[] content) => _files[path] = content;

        public bool FileExists(string path) => _files.ContainsKey(path);

        public string ReadAllText(string path, Encoding encoding) => encoding.GetString(_files[path]);

        public void WriteAllText(string path, string contents, Encoding encoding) =>
            _files[path] = encoding.GetBytes(contents);

        public void CreateDirectory(string path)
        {
        }
    }
}
