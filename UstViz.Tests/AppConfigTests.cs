using UstViz.Core.Config;

namespace UstViz.Tests;

public class AppConfigTests
{
    [Fact]
    public void Defaults_Match_Python_Config()
    {
        var c = AppConfig.CreateDefault();

        Assert.Equal(1920, c.Width);
        Assert.Equal(1080, c.Height);
        Assert.Equal(30, c.Fps);
        Assert.Equal("#FF0000", c.NoteColor);
        Assert.Equal("#00FF00", c.ActiveNoteColor);
        Assert.Equal("#FFFFFF", c.LyricColor);
        Assert.Equal("#000000", c.BackgroundColor);
        Assert.Equal("#FFFF00", c.JudgmentLineColor);
        Assert.Equal("#00FFFF", c.PitchCurveColor);
        Assert.Equal(0.2, c.JudgmentLinePosition, 6);
        Assert.Equal(500, c.ScrollSpeed, 6);
        Assert.Equal(1.0, c.FadeDuration, 6);
        Assert.Equal(24, c.FontSize);
        Assert.Equal("", c.FallbackFont); // 平台默认字体由 IPlatformDefaults 提供
        Assert.Equal(20, c.NoteHeight);
        Assert.Equal(5, c.NoteCornerRadius);
        Assert.True(c.NoteShadow);
        Assert.False(c.TransparentBackground);
        Assert.Equal(15, c.LyricOffset);
        Assert.True(c.ShowLyric);
        Assert.True(c.ShowPitchCurve);
        Assert.Equal(3, c.PitchCurveWidth);
        Assert.True(c.PitchCurveShadow);
        Assert.True(c.PitchCurveDots);
        Assert.Equal(5, c.PitchCurveDotSize);
        Assert.Equal(50, c.PitchCurveSmoothness);
        Assert.Equal(0, c.VerticalOffset);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ustviz-config-tests");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "config.json");
        try
        {
            var config = AppConfig.CreateDefault();
            config.Width = 1280;
            config.ScrollSpeed = 750;
            config.NoteColor = "#123456";
            config.FontPath = @"C:\fonts\my.ttf";

            new ConfigFile().Save(path, config);
            var loaded = new ConfigFile().Load(path);

            Assert.Equal(1280, loaded.Width);
            Assert.Equal(750, loaded.ScrollSpeed, 6);
            Assert.Equal("#123456", loaded.NoteColor);
            Assert.Equal(@"C:\fonts\my.ttf", loaded.FontPath);
        }
        finally
        {
            File.Delete(path);
        }
    }
}


