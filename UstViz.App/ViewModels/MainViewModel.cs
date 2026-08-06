using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UstViz.Core.Config;
using UstViz.Core.Parsing;
using UstViz.Rendering.Video;

namespace UstViz.App.ViewModels;

/// <summary>主窗口视图模型：配置绑定、文件选择、主题、颜色、视频导出。</summary>
public partial class MainViewModel : ViewModelBase
{
    // ---- UI 交互委托（由 View 注入，保持 VM 可测试、可替换）----
    public Func<Task<string?>>? PickUstFile { get; set; }
    public Func<Task<string?>>? PickOutputFolder { get; set; }
    public Func<Task<string?>>? PickFontFile { get; set; }
    public Func<Task<string?>>? PickSaveConfigPath { get; set; }
    public Func<Task<string?>>? PickLoadConfigPath { get; set; }
    public Func<string, Task<string?>>? PickColor { get; set; }
    public Action<string, string>? ShowMessage { get; set; }

    // ---- 文件 ----
    [ObservableProperty]
    public partial string UstFile { get; set; } = "";

    [ObservableProperty]
    public partial string OutputFolder { get; set; } = "";

    [ObservableProperty]
    public partial string FontFile { get; set; } = "";

    // ---- 基本设置 ----
    [ObservableProperty]
    public partial double Width { get; set; } = 1920;

    [ObservableProperty]
    public partial double Height { get; set; } = 1080;

    [ObservableProperty]
    public partial double Fps { get; set; } = 30;

    [ObservableProperty]
    public partial double FontSize { get; set; } = 24;

    // ---- 动画设置 ----
    [ObservableProperty]
    public partial double JudgmentLinePosition { get; set; } = 0.2;

    [ObservableProperty]
    public partial double ScrollSpeed { get; set; } = 500;

    [ObservableProperty]
    public partial double FadeDuration { get; set; } = 1.0;

    [ObservableProperty]
    public partial double VerticalOffset { get; set; }

    // ---- 样式设置 ----
    [ObservableProperty]
    public partial double NoteHeight { get; set; } = 20;

    [ObservableProperty]
    public partial double NoteCornerRadius { get; set; } = 5;

    [ObservableProperty]
    public partial bool NoteShadow { get; set; } = true;

    [ObservableProperty]
    public partial bool TransparentBackground { get; set; }

    [ObservableProperty]
    public partial double LyricOffset { get; set; } = 15;

    [ObservableProperty]
    public partial bool ShowLyric { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowPitchCurve { get; set; } = true;

    [ObservableProperty]
    public partial double PitchCurveWidth { get; set; } = 3;

    [ObservableProperty]
    public partial bool PitchCurveShadow { get; set; } = true;

    [ObservableProperty]
    public partial bool PitchCurveDots { get; set; } = true;

    [ObservableProperty]
    public partial double PitchCurveDotSize { get; set; } = 5;

    [ObservableProperty]
    public partial double PitchCurveSmoothness { get; set; } = 50;

    // ---- 颜色（hex）----
    [ObservableProperty]
    public partial string NoteColorHex { get; set; } = "#FF0000";

    [ObservableProperty]
    public partial string ActiveNoteColorHex { get; set; } = "#00FF00";

    [ObservableProperty]
    public partial string LyricColorHex { get; set; } = "#FFFFFF";

    [ObservableProperty]
    public partial string BackgroundColorHex { get; set; } = "#000000";

    [ObservableProperty]
    public partial string JudgmentLineColorHex { get; set; } = "#FFFF00";

    [ObservableProperty]
    public partial string PitchCurveColorHex { get; set; } = "#00FFFF";

    // ---- 状态 ----
    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; } = "就绪";

    public ObservableCollection<string> Logs { get; } = [];

    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
        ApplyConfig(AppConfig.CreateDefault());
        Log("程序已启动");
    }

    // ================= 命令 =================

    [RelayCommand]
    private async Task SelectUstFileAsync()
    {
        if (PickUstFile is null)
            return;
        var path = await PickUstFile();
        if (path is null)
            return;
        UstFile = path;
        Log($"已选择 UST 文件: {Path.GetFileName(path)}");
    }

    [RelayCommand]
    private async Task SelectOutputFolderAsync()
    {
        if (PickOutputFolder is null)
            return;
        var path = await PickOutputFolder();
        if (path is null)
            return;
        OutputFolder = path;
        Log($"已选择输出文件夹: {path}");
    }

    [RelayCommand]
    private async Task SelectFontFileAsync()
    {
        if (PickFontFile is null)
            return;
        var path = await PickFontFile();
        if (path is null)
            return;
        FontFile = path;
        Log($"已选择字体: {Path.GetFileName(path)}");
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        Application.Current!.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        Log(IsDarkTheme ? "已切换到深色主题" : "已切换到浅色主题");
    }

    [RelayCommand]
    private async Task SaveConfigAsync()
    {
        if (PickSaveConfigPath is null)
            return;
        var path = await PickSaveConfigPath();
        if (path is null)
            return;
        try
        {
            new ConfigFile().Save(path, BuildConfig());
            Log($"配置已保存: {path}");
            ShowMessage?.Invoke("成功", $"配置已保存到:\n{path}");
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke("错误", $"保存配置失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        if (PickLoadConfigPath is null)
            return;
        var path = await PickLoadConfigPath();
        if (path is null)
            return;
        try
        {
            var config = new ConfigFile().Load(path);
            ApplyConfig(config);
            Log($"配置已加载: {path}");
            ShowMessage?.Invoke("成功", "配置加载成功");
        }
        catch (Exception ex)
        {
            ShowMessage?.Invoke("错误", $"加载配置失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ChooseColorAsync(string key)
    {
        if (PickColor is null)
            return;
        var current = GetColorHex(key);
        var result = await PickColor(current);
        if (result is null)
            return;
        SetColorHex(key, result);
        Log($"{key} -> {result}");
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(UstFile) || string.IsNullOrWhiteSpace(OutputFolder))
        {
            ShowMessage?.Invoke("提示", "请先选择 UST 文件和输出文件夹。");
            return;
        }
        if (!File.Exists(UstFile))
        {
            ShowMessage?.Invoke("错误", "UST 文件不存在。");
            return;
        }

        try
        {
            Directory.CreateDirectory(OutputFolder);
            var config = BuildConfig();
            var project = new UstParser().ParseFile(UstFile);
            string outPath = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(UstFile) + ".avi");

            IsBusy = true;
            ProgressValue = 0;
            ProgressText = "开始生成...";
            _cts = new CancellationTokenSource();
            Log($"开始生成视频: {outPath}");

            using var writer = new MjpegAviVideoWriter(outPath, config.Width, config.Height, config.Fps);
            var service = new VideoExportService();
            var progress = new Progress<double>(p =>
            {
                ProgressValue = p * 100;
                ProgressText = $"生成进度: {p:P0}";
            });

            await Task.Run(() => service.ExportAsync(project, config, writer, progress, _cts.Token));

            ProgressText = "生成完成";
            Log($"视频已生成: {outPath}");
            ShowMessage?.Invoke("完成", $"视频已生成:\n{outPath}");
        }
        catch (OperationCanceledException)
        {
            ProgressText = "已停止";
            Log("生成已停止");
        }
        catch (Exception ex)
        {
            ProgressText = "生成失败";
            Log($"错误: {ex.Message}");
            ShowMessage?.Invoke("错误", $"生成失败:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanGenerate() => !IsBusy;

    [RelayCommand]
    private void StopGenerate()
    {
        _cts?.Cancel();
        Log("请求停止生成");
    }

    // ================= 配置转换 =================

    public AppConfig BuildConfig() => new()
    {
        Width = (int)Width,
        Height = (int)Height,
        Fps = (int)Fps,
        NoteColor = NoteColorHex,
        ActiveNoteColor = ActiveNoteColorHex,
        LyricColor = LyricColorHex,
        BackgroundColor = BackgroundColorHex,
        JudgmentLineColor = JudgmentLineColorHex,
        PitchCurveColor = PitchCurveColorHex,
        JudgmentLinePosition = JudgmentLinePosition,
        ScrollSpeed = ScrollSpeed,
        FadeDuration = FadeDuration,
        VerticalOffset = (int)VerticalOffset,
        FontPath = FontFile,
        FontSize = (int)FontSize,
        FallbackFont = "",
        NoteHeight = (int)NoteHeight,
        NoteCornerRadius = (int)NoteCornerRadius,
        NoteShadow = NoteShadow,
        TransparentBackground = TransparentBackground,
        LyricOffset = (int)LyricOffset,
        ShowLyric = ShowLyric,
        ShowPitchCurve = ShowPitchCurve,
        PitchCurveWidth = (int)PitchCurveWidth,
        PitchCurveShadow = PitchCurveShadow,
        PitchCurveDots = PitchCurveDots,
        PitchCurveDotSize = (int)PitchCurveDotSize,
        PitchCurveSmoothness = (int)PitchCurveSmoothness,
    };

    public void ApplyConfig(AppConfig c)
    {
        Width = c.Width;
        Height = c.Height;
        Fps = c.Fps;
        FontSize = c.FontSize;
        JudgmentLinePosition = c.JudgmentLinePosition;
        ScrollSpeed = c.ScrollSpeed;
        FadeDuration = c.FadeDuration;
        VerticalOffset = c.VerticalOffset;
        NoteHeight = c.NoteHeight;
        NoteCornerRadius = c.NoteCornerRadius;
        NoteShadow = c.NoteShadow;
        TransparentBackground = c.TransparentBackground;
        LyricOffset = c.LyricOffset;
        ShowLyric = c.ShowLyric;
        ShowPitchCurve = c.ShowPitchCurve;
        PitchCurveWidth = c.PitchCurveWidth;
        PitchCurveShadow = c.PitchCurveShadow;
        PitchCurveDots = c.PitchCurveDots;
        PitchCurveDotSize = c.PitchCurveDotSize;
        PitchCurveSmoothness = c.PitchCurveSmoothness;
        NoteColorHex = c.NoteColor;
        ActiveNoteColorHex = c.ActiveNoteColor;
        LyricColorHex = c.LyricColor;
        BackgroundColorHex = c.BackgroundColor;
        JudgmentLineColorHex = c.JudgmentLineColor;
        PitchCurveColorHex = c.PitchCurveColor;
        FontFile = c.FontPath;
    }

    private string GetColorHex(string key) => key switch
    {
        "NoteColorHex" => NoteColorHex,
        "ActiveNoteColorHex" => ActiveNoteColorHex,
        "LyricColorHex" => LyricColorHex,
        "BackgroundColorHex" => BackgroundColorHex,
        "JudgmentLineColorHex" => JudgmentLineColorHex,
        "PitchCurveColorHex" => PitchCurveColorHex,
        _ => "#FFFFFF",
    };

    private void SetColorHex(string key, string value)
    {
        switch (key)
        {
            case "NoteColorHex": NoteColorHex = value; break;
            case "ActiveNoteColorHex": ActiveNoteColorHex = value; break;
            case "LyricColorHex": LyricColorHex = value; break;
            case "BackgroundColorHex": BackgroundColorHex = value; break;
            case "JudgmentLineColorHex": JudgmentLineColorHex = value; break;
            case "PitchCurveColorHex": PitchCurveColorHex = value; break;
        }
    }

    private void Log(string message) =>
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
}
