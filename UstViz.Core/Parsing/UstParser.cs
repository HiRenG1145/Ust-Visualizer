using System.Globalization;
using System.Text.RegularExpressions;
using UstViz.Core.Abstractions;
using UstViz.Core.IO;
using UstViz.Core.Models;

namespace UstViz.Core.Parsing;

/// <summary>UST 文件解析器（与 UstViz.py 的 USTParser 行为一致）。</summary>
public sealed class UstParser
{
    private readonly IFileSystem _fileSystem;
    private readonly UstTextEncoding _textEncoding;

    /// <summary>
    /// 创建解析器。默认使用系统文件系统与默认编码列表（utf-8/shift_jis/gbk/big5），
    /// 可通过参数注入自定义实现，便于测试与替换。
    /// </summary>
    public UstParser(IFileSystem? fileSystem = null, UstTextEncoding? textEncoding = null)
    {
        _fileSystem = fileSystem ?? new SystemFileSystem();
        _textEncoding = textEncoding ?? new UstTextEncoding(_fileSystem);
    }

    private static readonly Regex NoteBlockRegex = new(
        @"\[#(\d+)\](.*?)(?=\[#\d+\]|$)", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TempoRegex = new(@"Tempo=([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex ProjectNameRegex = new(@"ProjectName=([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex LengthRegex = new(@"Length=(\d+)", RegexOptions.Compiled);
    private static readonly Regex LyricRegex = new(@"Lyric=([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex NoteNumRegex = new(@"NoteNum=(\d+)", RegexOptions.Compiled);
    private static readonly Regex PbsRegex = new(@"PBS=([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex PbwRegex = new(@"PBW=([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex PbyRegex = new(@"PBY=([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex PbmRegex = new(@"PBM=([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex PitchBendRegex = new(@"PitchBend=([^\r\n]+)", RegexOptions.Compiled);

    /// <summary>可选的日志回调（对应 Python 版的 print 输出）。</summary>
    public Action<string>? Log { get; set; }

    /// <summary>解析 UST 文件，返回工程模型。编码自动探测。</summary>
    public UstProject ParseFile(string path)
    {
        string content = _textEncoding.ReadAllText(path);
        var project = new UstProject();
        ParseMetadata(content, project);
        ParseNotes(content, project);
        CalculateTotalDuration(project);
        return project;
    }

    private void ParseMetadata(string content, UstProject project)
    {
        // 解析速度（BPM）
        var tempoMatch = TempoRegex.Match(content);
        if (tempoMatch.Success)
        {
            if (double.TryParse(tempoMatch.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var tempoValue))
            {
                if (tempoValue <= 0 || tempoValue > 1000) // 合理的速度范围检查
                {
                    Log?.Invoke($"警告: 速度值 {tempoValue} 超出合理范围，使用默认值120.0");
                    project.Tempo = 120.0;
                }
                else
                {
                    project.Tempo = tempoValue;
                    Log?.Invoke($"解析到速度: {project.Tempo} BPM");
                }
            }
            else
            {
                Log?.Invoke("警告: 速度值解析失败，使用默认值120.0");
                project.Tempo = 120.0;
            }
        }
        else
        {
            project.Tempo = 120.0;
            Log?.Invoke("未找到速度参数，使用默认值120.0 BPM");
        }

        // 解析项目名称
        var projectMatch = ProjectNameRegex.Match(content);
        if (projectMatch.Success)
        {
            project.ProjectName = projectMatch.Groups[1].Value;
            Log?.Invoke($"项目名称: {project.ProjectName}");
        }
    }

    private void ParseNotes(string content, UstProject project)
    {
        project.Notes.Clear();
        var matches = NoteBlockRegex.Matches(content);
        Log?.Invoke($"找到 {matches.Count} 个音符块");

        double currentTime = 0; // 当前时间（秒）

        foreach (Match match in matches)
        {
            string noteNumStr = match.Groups[1].Value;
            string noteContent = match.Groups[2].Value;

            // Python 版对 SETTING/TRACKEND/PREV/NEXT 做了防御性跳过（正则实际只匹配数字，不会命中）
            if (!int.TryParse(noteNumStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var noteNumber))
                continue;

            var note = new UstNote { Number = noteNumber };

            // 解析长度
            var lengthMatch = LengthRegex.Match(noteContent);
            if (lengthMatch.Success)
                note.Length = SafeInt(lengthMatch.Groups[1].Value, 480);

            // 解析歌词
            var lyricMatch = LyricRegex.Match(noteContent);
            if (lyricMatch.Success)
                note.Lyric = lyricMatch.Groups[1].Value.Trim();

            // 解析音高
            var noteNumMatch = NoteNumRegex.Match(noteContent);
            if (noteNumMatch.Success)
                note.NoteNum = SafeInt(noteNumMatch.Groups[1].Value, 60);

            // 解析 PBS (Pitch Bend Start)
            var pbsMatch = PbsRegex.Match(noteContent);
            if (pbsMatch.Success)
            {
                string pbsStr = pbsMatch.Groups[1].Value;
                if (!IsNullOrNull(pbsStr))
                {
                    if (pbsStr.Contains(';'))
                    {
                        var parts = pbsStr.Split(';');
                        note.Pbs =
                        [
                            SafeFloat(parts[0]),
                            SafeFloat(parts.Length > 1 ? parts[1] : null),
                        ];
                    }
                    else
                    {
                        note.Pbs = [SafeFloat(pbsStr), 0];
                    }
                }
            }

            // 解析 PBW (Pitch Bend Width)
            var pbwMatch = PbwRegex.Match(noteContent);
            if (pbwMatch.Success)
            {
                string pbwStr = pbwMatch.Groups[1].Value;
                if (!IsNullOrNull(pbwStr))
                {
                    note.Pbw = pbwStr.Split(',')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => SafeFloat(x))
                        .ToList();
                }
            }

            // 解析 PBY (Pitch Bend Y)
            var pbyMatch = PbyRegex.Match(noteContent);
            if (pbyMatch.Success)
            {
                string pbyStr = pbyMatch.Groups[1].Value;
                if (!IsNullOrNull(pbyStr))
                {
                    note.Pby = pbyStr.Split(',')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => SafeFloat(x))
                        .ToList();
                }
            }

            // 解析 PBM (Pitch Bend Mode) —— 注意：Python 版未过滤空项
            var pbmMatch = PbmRegex.Match(noteContent);
            if (pbmMatch.Success)
            {
                string pbmStr = pbmMatch.Groups[1].Value;
                if (!IsNullOrNull(pbmStr))
                    note.Pbm = pbmStr.Split(',').Select(x => x.Trim()).ToList();
            }

            // 解析 PitchBend
            var pitchBendMatch = PitchBendRegex.Match(noteContent);
            if (pitchBendMatch.Success)
            {
                string pitchBendStr = pitchBendMatch.Groups[1].Value;
                if (!IsNullOrNull(pitchBendStr))
                {
                    note.PitchBend = pitchBendStr.Split(',')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => SafeInt(x))
                        .ToList();
                }
            }

            // 计算时间信息（将 ticks 转换为秒）：480 ticks per quarter note
            double quarterNoteDuration = 60.0 / project.Tempo;
            double noteDurationSeconds = (note.Length / 480.0) * quarterNoteDuration;

            note.StartTime = currentTime;
            note.EndTime = currentTime + noteDurationSeconds;
            note.Duration = noteDurationSeconds;

            currentTime += noteDurationSeconds;

            // 只添加有意义音符（非休止符或音高 > 0）
            if (!note.IsRest || note.NoteNum > 0)
                project.Notes.Add(note);
            else
                Log?.Invoke($"跳过休止符: 音符 #{noteNumber}");
        }
    }

    private static void CalculateTotalDuration(UstProject project)
    {
        project.TotalDuration = project.Notes.Count > 0
            ? project.Notes.Max(n => n.EndTime)
            : 0;
    }

    private static bool IsNullOrNull(string s) =>
        string.IsNullOrWhiteSpace(s) || string.Equals(s.Trim(), "null", StringComparison.OrdinalIgnoreCase);

    private static double SafeFloat(string? value, double defaultValue = 0.0)
    {
        if (value is null || value.Trim().ToLowerInvariant() is "null" or "")
            return defaultValue;
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    private static int SafeInt(string? value, int defaultValue = 0)
    {
        if (value is null || value.Trim().ToLowerInvariant() is "null" or "")
            return defaultValue;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }
}


