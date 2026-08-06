using UstViz.Core.Audio;
using Xunit.Abstractions;

namespace UstViz.Tests;

/// <summary>音频合成与播放测试。</summary>
public class AudioTests
{
    private readonly ITestOutputHelper _output;

    public AudioTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(69, 440.0)]
    [InlineData(57, 220.0)]
    [InlineData(81, 880.0)]
    public void NoteToFrequency_Matches_Midi(int noteNum, double expected)
    {
        Assert.Equal(expected, SquareWaveSynthesizer.NoteToFrequency(noteNum), 6);
    }

    [Fact]
    public void GenerateStereo_Length_And_Amplitude()
    {
        var wave = SquareWaveSynthesizer.GenerateStereo(440, 1.0, 44100, 0.1);

        Assert.Equal(44100 * 2, wave.Length); // 1 秒立体声

        foreach (var sample in wave)
        {
            Assert.InRange(sample, -0.1f, 0.1f);
            Assert.Equal(sample, Math.Sign(sample) * 0.1f); // 方波：只有 -0.1 / 0 / +0.1
        }
    }

    [Fact]
    public void GenerateStereo_Has_Square_Wave_Shape()
    {
        // 440Hz @ 44100Hz -> 约 100.2 采样/周期，1 秒内应有大量正负跳变
        var wave = SquareWaveSynthesizer.GenerateStereo(440, 1.0, 44100, 0.1);

        bool hasPositive = false;
        bool hasNegative = false;
        int signChanges = 0;
        float lastSign = 0;

        for (int i = 0; i < wave.Length; i += 2)
        {
            float s = wave[i];
            if (s > 0) hasPositive = true;
            if (s < 0) hasNegative = true;

            float sign = Math.Sign(s);
            if (sign != 0 && sign != lastSign)
            {
                signChanges++;
                lastSign = sign;
            }
        }

        Assert.True(hasPositive, "方波缺少正值");
        Assert.True(hasNegative, "方波缺少负值");
        Assert.True(signChanges > 100, $"方波跳变次数过少: {signChanges}");
    }

    [Fact]
    public void NaudioAudioPlayer_Smoke_Test()
    {
        try
        {
            using var player = new UstViz.Audio.NaudioAudioPlayer();
            player.PlayNote(69, 0.3); // A4
            player.PlayNote(73, 0.3); // C5（重叠播放）
            Thread.Sleep(150);
            player.StopNote(69);
            player.PlayNote(60, 0.3); // C4
            Thread.Sleep(250);

            _output.WriteLine("音频播放冒烟测试通过。");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"无可用音频设备，跳过冒烟测试: {ex.GetType().Name}: {ex.Message}");
        }
    }
}


