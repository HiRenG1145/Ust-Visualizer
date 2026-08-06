namespace UstViz.Core.Audio;

/// <summary>
/// 音频播放抽象（Core 只定义契约，不依赖任何具体音频库）。
/// 具体实现（如 NAudio、ManagedBass、OpenAL）位于 App 层或独立音频项目。
/// </summary>
public interface IAudioPlayer
{
    /// <summary>播放一个 MIDI 音符（方块波），支持多音符重叠。</summary>
    void PlayNote(int noteNum, double durationSeconds);

    /// <summary>停止正在播放的指定音符。</summary>
    void StopNote(int noteNum);

    /// <summary>清理资源。</summary>
    void Dispose();
}
