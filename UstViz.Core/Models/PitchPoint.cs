namespace UstViz.Core.Models;

/// <summary>音高曲线上的一个采样点。Progress 为 0~1 的横向进度，Pitch 为半音值。</summary>
public readonly record struct PitchPoint(double Progress, double Pitch);
