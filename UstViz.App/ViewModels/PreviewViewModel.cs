using CommunityToolkit.Mvvm.ComponentModel;
using UstViz.Core.Audio;
using UstViz.Core.Config;
using UstViz.Core.Models;
using UstViz.Rendering;

namespace UstViz.App.ViewModels;

/// <summary>
/// 预览窗口视图模型：播放/暂停、前后跳转、时间推进、判定线音效触发。
/// 逻辑与 UstViz.py PreviewWindow 一致。
/// </summary>
public partial class PreviewViewModel : ViewModelBase
{
    private readonly UstProject _project;
    private readonly IAudioPlayer _audio;
    private readonly double _fps;

    /// <summary>判定线已触发的音符编号。</summary>
    public HashSet<int> TriggeredNotes { get; } = [];

    public AppConfig Config { get; }
    public double TotalDuration { get; }
    public int TotalFrames { get; }
    public double PlaybackSpeed { get; } = 1.0;

    [ObservableProperty]
    public partial double CurrentTime { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    public PreviewViewModel(UstProject project, AppConfig config, IAudioPlayer audio)
    {
        _project = project;
        Config = config;
        _audio = audio;
        _fps = config.Fps;

        double leadIn = config.Width / config.ScrollSpeed;
        TotalDuration = project.TotalDuration + leadIn + leadIn;
        TotalFrames = (int)(TotalDuration * config.Fps);
    }

    public void TogglePlay() => IsPlaying = !IsPlaying;

    /// <summary>后退 10 帧（与 Python 版 Z 键一致）。</summary>
    public void StepBack()
    {
        CurrentTime = Math.Max(0, CurrentTime - 10.0 / _fps);
        TriggeredNotes.Clear();
    }

    /// <summary>前进 10 帧（与 Python 版 X 键一致）。</summary>
    public void StepForward()
    {
        CurrentTime = Math.Min(TotalDuration, CurrentTime + 10.0 / _fps);
        TriggeredNotes.Clear();
    }

    /// <summary>滚轮滚动 ±5 帧。</summary>
    public void Scroll(double frames)
    {
        CurrentTime = Math.Clamp(CurrentTime + frames / _fps, 0, TotalDuration);
        TriggeredNotes.Clear();
    }

    /// <summary>播放中推进一帧时间；返回是否需要重绘。</summary>
    public bool Tick()
    {
        if (!IsPlaying)
            return false;

        CurrentTime += 1.0 / _fps * PlaybackSpeed;
        if (CurrentTime >= TotalDuration)
        {
            CurrentTime = 0;
            TriggeredNotes.Clear();
        }

        CheckNoteTriggers();
        return true;
    }

    /// <summary>检查音符是否经过判定线，触发/停止音效（与 Python check_note_triggers 一致）。</summary>
    public void CheckNoteTriggers()
    {
        if (!IsPlaying)
            return;

        double judgmentX = Config.Width * Config.JudgmentLinePosition;
        double pps = Config.ScrollSpeed;
        double leadIn = Config.Width / pps;

        foreach (var note in _project.Notes)
        {
            if (note.IsRest || note.NoteNum <= 0)
                continue;

            var (startX, endX) = NoteGeometry.GetNoteXRange(note, CurrentTime, pps, Config.Width, leadIn);

            if (startX <= judgmentX && judgmentX <= endX)
            {
                if (TriggeredNotes.Add(note.Number))
                    _audio.PlayNote(note.NoteNum, note.Duration);
            }
            else
            {
                TriggeredNotes.Remove(note.Number);
            }
        }
    }

    public int CurrentFrame => (int)(CurrentTime * _fps);
}
