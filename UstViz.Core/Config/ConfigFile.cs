using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace UstViz.Core.Config;

/// <summary>AppConfig 的 JSON 存取（对应 Python 版 save_config / load_config）。</summary>
public static class ConfigFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), // 不转义中文（字体路径等）
        PropertyNameCaseInsensitive = true,
    };

    public static AppConfig Load(string path) =>
        JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), Options) ?? AppConfig.CreateDefault();

    public static void Save(string path, AppConfig config) =>
        File.WriteAllText(path, JsonSerializer.Serialize(config, Options), new UTF8Encoding(false));
}
