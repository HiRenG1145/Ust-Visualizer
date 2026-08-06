using Avalonia;
using Avalonia.Media;

namespace UstViz.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        // 默认字体按平台选择（含中文），避免 Inter 不含中文导致显示问题
        var defaultFamily = OperatingSystem.IsWindows() ? "Microsoft YaHei UI"
            : OperatingSystem.IsMacOS() ? "PingFang SC"
            : "Noto Sans CJK SC";

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .With(new FontManagerOptions { DefaultFamilyName = defaultFamily })
            .LogToTrace();
    }
}
