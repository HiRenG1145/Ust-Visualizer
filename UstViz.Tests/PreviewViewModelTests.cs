using UstViz.App.ViewModels;
using UstViz.Core.Audio;
using UstViz.Core.Config;
using UstViz.Core.Models;
using UstViz.Core.Parsing;

namespace UstViz.Tests;

/// <summary>预览播放逻辑测试（判定线触发音效等）。</summary>
public class PreviewViewModelTests
{
    private sealed class StubAudioPlayer : IAudioPlayer
    {
        public List<int> Played { get; } = [];
        public List<int> Stopped { get; } = [];

        public void PlayNote(int noteNum, double durationSeconds) => Played.Add(noteNum);
        public void StopNote(int noteNum) => Stopped.Add(noteNum);
        public void Dispose() { }
    }

    private static (PreviewViewModel Vm, StubAudioPlayer Audio, UstProject Project, AppConfig Config) Create()
    {
        var project = new UstParser().ParseFile(Path.Combine(AppContext.BaseDirectory, "Test", "热异常.ust"));
        var config = AppConfig.CreateDefault();
        config.Width = 640;
        config.Height = 360;
        config.Fps = 30;
        config.ScrollSpeed = 500;
        var audio = new StubAudioPlayer();
        var vm = new PreviewViewModel(project, config, audio);
        return (vm, audio, project, config);
    }

    [Fact]
    public void Tick_Advances_Time_When_Playing()
    {
        var (vm, _, _, _) = Create();
        vm.IsPlaying = true;
        double t0 = vm.CurrentTime;
        vm.Tick();
        Assert.True(vm.CurrentTime > t0, "播放中 Tick 应推进时间");
    }

    [Fact]
    public void Tick_Does_Not_Advance_When_Paused()
    {
        var (vm, _, _, _) = Create();
        double t0 = vm.CurrentTime;
        vm.Tick();
        Assert.Equal(t0, vm.CurrentTime);
    }

    [Fact]
    public void StepForward_And_Back_Clamp()
    {
        var (vm, _, _, _) = Create();
        vm.StepForward();
        Assert.Equal(10.0 / 30.0, vm.CurrentTime, 6);
        vm.StepBack();
        Assert.Equal(0, vm.CurrentTime, 6);
    }

    [Fact]
    public void CheckNoteTriggers_Plays_Note_At_Judgment_Line()
    {
        var (vm, audio, project, config) = Create();
        vm.IsPlaying = true;

        double leadIn = config.Width / config.ScrollSpeed;
        double judgmentX = config.Width * config.JudgmentLinePosition;
        double pps = config.ScrollSpeed;

        bool found = false;
        foreach (var note in project.Notes)
        {
            if (note.IsRest || note.NoteNum <= 0)
                continue;

            // 找到音符经过判定线的时刻：判定线 x 处的绝对时间
            // note 左缘 startX = W + (start - t + leadIn)*pps = judgmentX  => t = start + leadIn + (W - judgmentX)/pps
            double t = note.StartTime + leadIn + (config.Width - judgmentX) / pps;
            if (t < 0 || t > vm.TotalDuration)
                continue;

            vm.CurrentTime = t;
            vm.CheckNoteTriggers();
            Assert.Contains(note.NoteNum, audio.Played);
            found = true;
            break;
        }

        Assert.True(found, "未找到可测试的判定线时刻");
    }
}

