using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using UstViz.App.ViewModels;
using UstViz.Audio;
using UstViz.Core.Config;
using UstViz.Core.Models;
using UstViz.Core.Parsing;

namespace UstViz.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.PickUstFile = PickUstFileAsync;
            vm.PickOutputFolder = PickOutputFolderAsync;
            vm.PickFontFile = PickFontFileAsync;
            vm.PickSaveConfigPath = PickSaveConfigPathAsync;
            vm.PickLoadConfigPath = PickLoadConfigPathAsync;
            vm.PickColor = PickColorAsync;
            vm.PickFfmpegFile = PickFfmpegFileAsync;
            vm.ShowMessage = ShowMessage;
            vm.OpenPreviewRequested = OpenPreview;
        }
    }

    // ================= 侧边栏导航 =================

    private void OnNavSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is FANavigationViewItem item && item.Tag is string tag)
            ShowPage(tag);
    }


    private void ShowPage(string tag)
    {
        var page = tag switch
        {
            "file" => (Control)PageFile,
            "generate" => PageGenerate,
            "basic" => PageBasic,
            "animation" => PageAnimation,
            "style" => PageStyle,
            "color" => PageColor,
            _ => null,
        };

        PageFile.IsVisible = tag == "file";
        PageGenerate.IsVisible = tag == "generate";
        PageBasic.IsVisible = tag == "basic";
        PageAnimation.IsVisible = tag == "animation";
        PageStyle.IsVisible = tag == "style";
        PageColor.IsVisible = tag == "color";

        if (page is null)
            return;

        // 页面切换过渡：淡入 + 轻微右滑（Entrance 风格）
        const double slideOffset = 24;
        var duration = TimeSpan.FromMilliseconds(200);

        page.RenderTransform = new TranslateTransform(slideOffset, 0);
        page.Opacity = 0;
        page.Transitions =
        [
            new DoubleTransition { Property = OpacityProperty, Duration = duration },
            new DoubleTransition { Property = TranslateTransform.XProperty, Duration = duration },
        ];
        page.Opacity = 1;
        ((TranslateTransform)page.RenderTransform).X = 0;
    }

    // ================= 对话框（遮罩式 ContentDialog） =================

    private async void ShowMessage(string title, string message)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
        };
        await dialog.ShowAsync(this);
    }

    // ================= 文件选择 =================

    private async Task<string?> PickUstFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 UST 文件",
            FileTypeFilter =
            [
                new FilePickerFileType("UST 文件") { Patterns = ["*.ust"] },
                new FilePickerFileType("文本文件") { Patterns = ["*.txt"] },
                FilePickerFileTypes.All,
            ],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickOutputFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择输出文件夹",
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickFontFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择字体文件",
            FileTypeFilter =
            [
                new FilePickerFileType("字体文件") { Patterns = ["*.ttf", "*.otf", "*.ttc"] },
                FilePickerFileTypes.All,
            ],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickSaveConfigPathAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存配置",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON 文件") { Patterns = ["*.json"] }],
        });
        return file?.TryGetLocalPath();
    }

    private async Task<string?> PickLoadConfigPathAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "加载配置",
            FileTypeFilter =
            [
                new FilePickerFileType("JSON 文件") { Patterns = ["*.json"] },
                FilePickerFileTypes.All,
            ],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickFfmpegFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 ffmpeg 可执行文件",
            FileTypeFilter =
            [
                new FilePickerFileType("ffmpeg") { Patterns = ["ffmpeg.exe"] },
                FilePickerFileTypes.All,
            ],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickColorAsync(string currentHex)
    {
        var dialog = new ColorPickerDialog(currentHex) { Icon = Icon };
        return await dialog.ShowDialog<string?>(this);
    }

    // ================= 预览 =================

    private void OpenPreview(AppConfig config, string ustFile)
    {
        try
        {
            var project = new UstParser().ParseFile(ustFile);
            var audio = new NaudioAudioPlayer();
            var window = new PreviewWindow(project, config, audio) { Icon = Icon };
            window.Show(this);
        }
        catch (Exception ex)
        {
            ShowMessage("预览错误", $"无法打开预览窗口:\n{ex.Message}");
        }
    }
}


