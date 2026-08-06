using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UstViz.Core.Audio;

namespace UstViz.Audio;

/// <summary>
/// 基于 NAudio 的 IAudioPlayer 实现（Windows）。
/// 使用 MixingSampleProvider 混音，支持多个音符同时播放；
/// 音源播放完毕后自动移除。
/// </summary>
public sealed class NaudioAudioPlayer : IAudioPlayer
{
    private const int SampleRate = 44100;
    private const double Amplitude = 0.1;

    private readonly WaveOutEvent _output;
    private readonly MixingSampleProvider _mixer;
    private readonly Dictionary<int, List<ISampleProvider>> _activeNotes = new();
    private bool _disposed;

    public NaudioAudioPlayer()
    {
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2))
        {
            ReadFully = true,
        };
        _mixer.MixerInputEnded += OnMixerInputEnded;

        _output = new WaveOutEvent();
        _output.Init(_mixer);
        _output.Play();
    }

    /// <inheritdoc />
    public void PlayNote(int noteNum, double durationSeconds)
    {
        double frequency = SquareWaveSynthesizer.NoteToFrequency(noteNum);
        var samples = SquareWaveSynthesizer.GenerateStereo(frequency, durationSeconds, SampleRate, Amplitude);

        var bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

        var provider = new RawSourceWaveStream(
                new MemoryStream(bytes),
                WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2))
            .ToSampleProvider();

        _mixer.AddMixerInput(provider);

        if (!_activeNotes.TryGetValue(noteNum, out var list))
        {
            list = [];
            _activeNotes[noteNum] = list;
        }
        list.Add(provider);
    }

    /// <inheritdoc />
    public void StopNote(int noteNum)
    {
        if (!_activeNotes.TryGetValue(noteNum, out var list))
            return;

        foreach (var provider in list.ToArray())
            _mixer.RemoveMixerInput(provider);
        _activeNotes.Remove(noteNum);
    }

    private void OnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        _mixer.RemoveMixerInput(e.SampleProvider);
        foreach (var list in _activeNotes.Values)
            list.Remove(e.SampleProvider);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _mixer.MixerInputEnded -= OnMixerInputEnded;
        _output.Stop();
        _output.Dispose();
    }
}

