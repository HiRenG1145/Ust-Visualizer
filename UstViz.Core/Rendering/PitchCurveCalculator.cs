using UstViz.Core.Models;

namespace UstViz.Core.Rendering;

/// <summary>
/// 音高曲线计算，与 UstViz.py USTParser.calculate_pitch_curve /
/// _calculate_pitch_curve_from_pb 的行为保持一致。
/// </summary>
public static class PitchCurveCalculator
{
    /// <summary>计算音符的音高曲线，返回 resolution+1 个采样点。</summary>
    public static List<PitchPoint> Calculate(UstNote note, int resolution = 100)
    {
        // 如果没有 PitchBend 数据，尝试使用 PBW 和 PBY 生成曲线
        if (note.PitchBend.Count == 0 && note.Pbw.Count > 0 && note.Pby.Count > 0)
            return CalculateFromPb(note, resolution);

        // 如果没有 PitchBend 数据，返回平坦曲线
        if (note.PitchBend.Count == 0)
            return Flat(note.NoteNum, resolution);

        // 使用 PitchBend 数据生成曲线
        var points = new List<PitchPoint>(note.PitchBend.Count);
        for (int i = 0; i < note.PitchBend.Count; i++)
        {
            double progress = note.PitchBend.Count > 1 ? (double)i / (note.PitchBend.Count - 1) : 0;
            double pitchOffset = note.PitchBend[i] / 100.0; // 简化转换，与 Python 版一致
            points.Add(new PitchPoint(progress, note.NoteNum + pitchOffset));
        }

        return points;
    }

    /// <summary>使用 PBW/PBY 数据计算音高曲线（分段线性插值）。</summary>
    private static List<PitchPoint> CalculateFromPb(UstNote note, int resolution)
    {
        double basePitch = note.NoteNum;
        double pbsY = note.Pbs.Length > 1 ? note.Pbs[1] : 0;
        var pbw = note.Pbw;
        var pby = note.Pby;

        // 如果没有 PBW 数据，返回平坦曲线
        if (pbw.Count == 0)
            return Flat(basePitch, resolution);

        // 计算总宽度
        double totalWidth = pbw.Sum();

        // 生成曲线点，起点为 PBS 的 Y 偏移
        var pitchPoints = new List<PitchPoint> { new(0, basePitch + pbsY) };
        double currentPos = 0;

        for (int i = 0; i < pbw.Count; i++)
        {
            double segmentWidth = pbw[i];
            double segmentPitch = basePitch + (i < pby.Count ? pby[i] : 0);

            // Python 版还计算了未使用的 start_pos，这里省略
            double endPos = (currentPos + segmentWidth) / totalWidth;

            pitchPoints.Add(new PitchPoint(endPos, segmentPitch));

            currentPos += segmentWidth;
        }

        // 如果点太少，进行插值
        if (pitchPoints.Count < 2)
            return Flat(basePitch, resolution);

        // 对曲线进行插值以获得更平滑的结果
        var interpolated = new List<PitchPoint>(resolution + 1);
        for (int i = 0; i <= resolution; i++)
        {
            double progress = (double)i / resolution;

            bool found = false;
            for (int j = 0; j < pitchPoints.Count - 1; j++)
            {
                if (pitchPoints[j].Progress <= progress && progress <= pitchPoints[j + 1].Progress)
                {
                    double segProgress = (progress - pitchPoints[j].Progress) /
                                         (pitchPoints[j + 1].Progress - pitchPoints[j].Progress);
                    double pitchValue = pitchPoints[j].Pitch +
                                        segProgress * (pitchPoints[j + 1].Pitch - pitchPoints[j].Pitch);
                    interpolated.Add(new PitchPoint(progress, pitchValue));
                    found = true;
                    break;
                }
            }

            if (!found)
                interpolated.Add(new PitchPoint(progress, pitchPoints[^1].Pitch));
        }

        return interpolated;
    }

    /// <summary>生成平坦曲线（resolution+1 个点）。</summary>
    private static List<PitchPoint> Flat(double basePitch, int resolution)
    {
        var list = new List<PitchPoint>(resolution + 1);
        for (int i = 0; i <= resolution; i++)
            list.Add(new PitchPoint((double)i / resolution, basePitch));
        return list;
    }
}
