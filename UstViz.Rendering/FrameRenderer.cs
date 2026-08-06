using SkiaSharp;
using UstViz.Core.Algorithms;
using UstViz.Core.Config;
using UstViz.Core.Models;

namespace UstViz.Rendering;

/// <summary>
/// 帧渲染器：把 UST 工程在某个时间点的画面绘制到 SKBitmap。
/// 渲染逻辑与 UstViz.py SequenceGenerator._draw_note / _draw_pitch_curves 保持一致。
/// </summary>
public sealed class FrameRenderer : IDisposable
{
    private readonly AppConfig _config;
    private readonly SKFont _font;
    private readonly SKPaint _textPaint;

    public FrameRenderer(AppConfig config, UstViz.Core.Abstractions.IPlatformDefaults? platform = null)
    {
        _config = config;
        using var fonts = new FontResolver(config, platform);
        _font = new SKFont(fonts.Typeface, config.FontSize);
        _textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
    }

    /// <summary>渲染某一时刻的帧。</summary>
    public SKBitmap Render(UstProject project, double currentTime, double totalDuration)
    {
        int width = _config.Width;
        int height = _config.Height;

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        DrawBackground(canvas, width, height);
        DrawJudgmentLine(canvas, width, height);

        double pps = _config.ScrollSpeed;
        double judgmentX = width * _config.JudgmentLinePosition;
        double leadIn = width / pps;

        foreach (var note in project.Notes)
            DrawNote(canvas, note, currentTime, totalDuration, pps, judgmentX, leadIn, width, height);

        if (_config.ShowPitchCurve)
            DrawPitchCurves(canvas, project.Notes, currentTime, totalDuration, pps, leadIn, width, height);

        return bitmap;
    }

    /// <summary>计算某一时刻所有可见音符的布局（供测试/调试对比，不涉及像素）。</summary>
    public List<NoteLayout> ComputeNoteLayouts(UstProject project, double currentTime, double totalDuration)
    {
        var result = new List<NoteLayout>();
        double pps = _config.ScrollSpeed;
        double judgmentX = _config.Width * _config.JudgmentLinePosition;
        double leadIn = _config.Width / pps;
        int fadeAlpha = ComputeFadeAlpha(currentTime, totalDuration);

        foreach (var note in project.Notes)
        {
            var (startX, endX) = NoteGeometry.GetNoteXRange(note, currentTime, pps, _config.Width, leadIn);
            if (endX < 0 || startX > _config.Width)
                continue;
            if (note.IsRest || note.NoteNum <= 0)
                continue;

            double noteWidth = Math.Max(10, endX - startX);
            if (noteWidth < 5)
                continue;

            double noteY = NoteGeometry.GetNoteYPosition(note.NoteNum, _config.Height, _config.VerticalOffset);
            bool isActive = startX <= judgmentX && judgmentX <= endX;

            var color = ColorUtil.ToSkColor(isActive ? _config.ActiveNoteColor : _config.NoteColor);
            color = ApplyFade(color, fadeAlpha);

            result.Add(new NoteLayout(
                note.Number,
                startX, endX, noteY,
                isActive,
                color.Alpha,
                color.Red, color.Green, color.Blue));
        }

        return result;
    }

    private void DrawBackground(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(_config.TransparentBackground ? SKColors.Transparent : ColorUtil.ToSkColor(_config.BackgroundColor));
    }

    private void DrawJudgmentLine(SKCanvas canvas, int width, int height)
    {
        double x = width * _config.JudgmentLinePosition;
        using var paint = new SKPaint
        {
            Color = ColorUtil.ToSkColor(_config.JudgmentLineColor),
            StrokeWidth = 2,
            IsAntialias = true,
        };
        canvas.DrawLine((float)x, 0, (float)x, height, paint);
    }

    private void DrawNote(SKCanvas canvas, UstNote note, double currentTime, double totalDuration,
        double pps, double judgmentX, double leadIn, int width, int height)
    {
        var (startX, endX) = NoteGeometry.GetNoteXRange(note, currentTime, pps, width, leadIn);

        // 完全在屏幕外则跳过
        if (endX < 0 || startX > width)
            return;
        // 跳过休止符或无效音高
        if (note.IsRest || note.NoteNum <= 0)
            return;

        double noteY = NoteGeometry.GetNoteYPosition(note.NoteNum, height, _config.VerticalOffset);
        double noteWidth = Math.Max(10, endX - startX);
        double noteHeight = _config.NoteHeight;

        if (noteWidth < 5)
            return;

        bool isActive = startX <= judgmentX && judgmentX <= endX;
        int fadeAlpha = ComputeFadeAlpha(currentTime, totalDuration);

        // 阴影（Python 版阴影不应用淡入淡出）
        if (_config.NoteShadow)
        {
            var shadowColor = _config.TransparentBackground
                ? new SKColor(0, 0, 0, 100)
                : new SKColor(30, 30, 30, 255);
            DrawRoundedRect(canvas,
                (float)(startX + 3), (float)(noteY - noteHeight / 2 + 3),
                (float)noteWidth, (float)noteHeight,
                _config.NoteCornerRadius, shadowColor);
        }

        // 音符主体
        var noteColor = ColorUtil.ToSkColor(isActive ? _config.ActiveNoteColor : _config.NoteColor);
        noteColor = ApplyFade(noteColor, fadeAlpha);
        DrawRoundedRect(canvas,
            (float)startX, (float)(noteY - noteHeight / 2),
            (float)noteWidth, (float)noteHeight,
            _config.NoteCornerRadius, noteColor);

        // 歌词（midbottom 对齐，位于音符头部上方）
        if (_config.ShowLyric && note.Lyric.Length > 0 && !note.IsRest)
        {
            var lyricColor = ApplyFade(ColorUtil.ToSkColor(_config.LyricColor), fadeAlpha);
            DrawLyric(canvas, note.Lyric,
                (float)(startX + Math.Min(20, noteWidth / 2)),
                (float)(noteY - noteHeight / 2 - _config.LyricOffset),
                lyricColor);
        }
    }

    private void DrawPitchCurves(SKCanvas canvas, IReadOnlyList<UstNote> notes, double currentTime,
        double totalDuration, double pps, double leadIn, int width, int height)
    {
        if (notes.Count == 0)
            return;

        int curveWidth = _config.PitchCurveWidth;
        bool showShadow = _config.PitchCurveShadow;
        bool showDots = _config.PitchCurveDots;
        int dotSize = _config.PitchCurveDotSize;
        int smoothness = _config.PitchCurveSmoothness;
        int fadeAlpha = ComputeFadeAlpha(currentTime, totalDuration);
        var curveColor = ApplyFade(ColorUtil.ToSkColor(_config.PitchCurveColor), fadeAlpha);

        foreach (var note in notes)
        {
            if (note.IsRest || note.NoteNum <= 0)
                continue;

            var (startX, endX) = NoteGeometry.GetNoteXRange(note, currentTime, pps, width, leadIn);
            if (endX < 0 || startX > width)
                continue;

            var pitchPoints = PitchCurveCalculator.Calculate(note, smoothness);
            if (pitchPoints.Count < 2)
                continue;

            var screenPoints = new List<SKPoint>(pitchPoints.Count);
            foreach (var pt in pitchPoints)
            {
                float x = (float)(startX + pt.Progress * (endX - startX));
                float y = (float)NoteGeometry.GetNoteYPosition(pt.Pitch, height, _config.VerticalOffset);
                screenPoints.Add(new SKPoint(x, y));
            }

            // 曲线阴影（不应用淡入淡出，与 Python 版一致）
            if (showShadow && curveWidth > 1)
            {
                var shadowColor = _config.TransparentBackground
                    ? new SKColor(0, 0, 0, 100)
                    : new SKColor(30, 30, 30, 255);
                DrawPolyline(canvas, screenPoints, curveWidth, shadowColor, 2, 2);
            }

            // 曲线主体
            DrawPolyline(canvas, screenPoints, curveWidth, curveColor, 0, 0);

            // 端点标记
            if (showDots && screenPoints.Count >= 2)
            {
                using var dotPaint = new SKPaint { Color = curveColor, IsAntialias = true, Style = SKPaintStyle.Fill };
                canvas.DrawCircle(screenPoints[0], dotSize, dotPaint);
                canvas.DrawCircle(screenPoints[^1], dotSize, dotPaint);
            }
        }
    }

    private static void DrawPolyline(SKCanvas canvas, IReadOnlyList<SKPoint> points, float strokeWidth,
        SKColor color, float offsetX, float offsetY)
    {
        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = strokeWidth,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };
        using var builder = new SKPathBuilder();
        builder.MoveTo(new SKPoint(points[0].X + offsetX, points[0].Y + offsetY));
        for (int i = 1; i < points.Count; i++)
            builder.LineTo(new SKPoint(points[i].X + offsetX, points[i].Y + offsetY));
        using var path = builder.Detach();
        canvas.DrawPath(path, paint);
    }

    private static void DrawRoundedRect(SKCanvas canvas, float x, float y, float w, float h,
        int radius, SKColor color)
    {
        using var paint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        if (radius > 0)
        {
            float r = Math.Min(radius, Math.Min(w, h) / 2);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + w, y + h), r), paint);
        }
        else
        {
            canvas.DrawRect(x, y, w, h, paint);
        }
    }

    private void DrawLyric(SKCanvas canvas, string text, float lyricX, float lyricY, SKColor color)
    {
        float textWidth = _font.MeasureText(text, _textPaint);
        float descent = _font.Metrics.Descent;
        _textPaint.Color = color;
        float x = lyricX - textWidth / 2;
        float baselineY = lyricY - descent; // midbottom 对齐
        canvas.DrawText(text, x, baselineY, SKTextAlign.Left, _font, _textPaint);
    }

    /// <summary>计算淡入淡出透明度（与 Python 版一致，不 clamp）。</summary>
    private int ComputeFadeAlpha(double currentTime, double totalDuration)
    {
        int alpha = 255;
        double fade = _config.FadeDuration;

        if (currentTime < fade)
            alpha = (int)(255 * (currentTime / fade));
        else if (currentTime > totalDuration - fade)
            alpha = (int)(255 * ((totalDuration - currentTime) / fade));

        return alpha;
    }

    private static SKColor ApplyFade(SKColor color, int fadeAlpha) =>
        color.WithAlpha((byte)(color.Alpha * fadeAlpha / 255));

    public void Dispose()
    {
        _font.Dispose();
        _textPaint.Dispose();
    }
}

/// <summary>某一时刻可见音符的布局信息（供测试/调试）。</summary>
public readonly record struct NoteLayout(
    int Number, double StartX, double EndX, double Y,
    bool IsActive, byte Alpha, byte R, byte G, byte B);

