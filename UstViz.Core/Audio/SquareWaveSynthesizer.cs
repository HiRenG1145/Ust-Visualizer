namespace UstViz.Core.Audio;

/// <summary>
/// 方波音频合成（纯算法，无第三方依赖）。
/// 与 UstViz.py AudioGenerator.generate_square_wave 的行为一致。
/// </summary>
public static class SquareWaveSynthesizer
{
    /// <summary>MIDI 音符编号转频率（A4=69=440Hz）。</summary>
    public static double NoteToFrequency(int noteNum) =>
        440.0 * Math.Pow(2.0, (noteNum - 69) / 12.0);

    /// <summary>
    /// 生成方波（立体声、IEEE float、左右声道相同），返回交错 float[]。
    /// </summary>
    public static float[] GenerateStereo(
        double frequency, double durationSeconds,
        int sampleRate = 44100, double amplitude = 0.1)
    {
        int samples = (int)(durationSeconds * sampleRate);
        var wave = new float[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / sampleRate;
            // 方波：sin 的符号（与 numpy.sign 一致，0 处为 0）
            float value = (float)(Math.Sign(Math.Sin(2 * Math.PI * frequency * t)) * amplitude);
            wave[i * 2] = value;
            wave[i * 2 + 1] = value;
        }
        return wave;
    }
}
