namespace UstViz.Rendering.Video;

/// <summary>ffmpeg 可执行文件定位：显式路径 → PATH → 常见安装位置。</summary>
public static class FfmpegLocator
{
    /// <summary>查找 ffmpeg；找不到返回 null。</summary>
    public static string? Locate(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        // PATH 搜索
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // 忽略非法路径
            }
        }

        // 常见安装位置
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var candidates = new[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(localAppData, "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(localAppData, "Microsoft", "WinGet", "Packages", "Gyan.FFmpeg", "ffmpeg-*", "bin", "ffmpeg.exe"),
        };

        foreach (var pattern in candidates)
        {
            try
            {
                if (pattern.Contains('*'))
                {
                    var dir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(pattern)));
                    if (dir is null)
                        continue;
                    var match = Directory.GetDirectories(dir, Path.GetFileName(Path.GetDirectoryName(pattern))!)
                        .FirstOrDefault();
                    if (match is not null)
                    {
                        var exe = Path.Combine(match, "bin", "ffmpeg.exe");
                        if (File.Exists(exe))
                            return exe;
                    }
                }
                else if (File.Exists(pattern))
                {
                    return pattern;
                }
            }
            catch
            {
                // 忽略
            }
        }

        return null;
    }
}
