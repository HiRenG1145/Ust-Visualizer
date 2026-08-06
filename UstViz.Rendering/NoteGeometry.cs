using UstViz.Core.Models;

namespace UstViz.Rendering;

/// <summary>音符几何计算（与 UstViz.py NoteRenderer 一致）。</summary>
public static class NoteGeometry
{
    public const double MaxPitch = 108; // C8
    public const double MinPitch = 0;   // C0

    /// <summary>
    /// 根据半音值获取 Y 坐标（屏幕坐标，0 在顶部）。
    /// noteNum 允许为小数（音高曲线插值会产生非整数半音）。
    /// </summary>
    public static double GetNoteYPosition(double noteNum, double totalHeight, double verticalOffset)
    {
        double normalized = (noteNum - MinPitch) / (MaxPitch - MinPitch);
        double baseY = totalHeight * (1 - normalized);
        return baseY + verticalOffset;
    }

    /// <summary>计算音符在屏幕上的 X 范围（与 Python 版公式一致，音符从屏幕最右侧进入）。</summary>
    public static (double StartX, double EndX) GetNoteXRange(
        UstNote note, double currentTime, double pixelsPerSecond, double width, double leadInTime)
    {
        double startX = width + (note.StartTime - currentTime + leadInTime) * pixelsPerSecond;
        double endX = width + (note.EndTime - currentTime + leadInTime) * pixelsPerSecond;
        return (startX, endX);
    }
}
