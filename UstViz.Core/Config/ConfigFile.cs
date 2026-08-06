using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using UstViz.Core.Abstractions;
using UstViz.Core.IO;

namespace UstViz.Core.Config;

/// <summary>AppConfig 的 JSON 存取（对应 Python 版 save_config / load_config）。</summary>
public sealed class ConfigFile
{
    private readonly IFileSystem _fileSystem;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), // 不转义中文（字体路径等）
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>创建配置存取器。默认使用系统文件系统，可注入自定义实现。</summary>
    public ConfigFile(IFileSystem? fileSystem = null) => _fileSystem = fileSystem ?? new SystemFileSystem();

    public AppConfig Load(string path) =>
        JsonSerializer.Deserialize<AppConfig>(_fileSystem.ReadAllText(path, new UTF8Encoding(false)), Options)
        ?? AppConfig.CreateDefault();

    public void Save(string path, AppConfig config) =>
        _fileSystem.WriteAllText(path, JsonSerializer.Serialize(config, Options), new UTF8Encoding(false));
}

