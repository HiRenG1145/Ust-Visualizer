# UST Visualizer | UST 可视化工具

用 **C# + Avalonia** 重写的 UST 可视化工具：把 UTAU 的 `.ust` 工程渲染成**滚动音符视频**，支持实时预览与直接导出视频。

> 原 Python（pygame + tkinter）版本已迁移为 C#：`UstViz.Core`（解析/算法）+ `UstViz.Rendering`（SkiaSharp 渲染/视频）+ `UstViz.App`（Avalonia UI）。

## 功能特性

- 📁 解析 UST 工程：多编码自动探测（utf-8 / shift_jis / gbk / big5）、音符/歌词/音高曲线
- 🎬 直接生成视频（无需先导出 PNG 序列）：
  - **AVI**（MJPEG，无外部依赖，跨平台）
  - **MP4**（H.264，需要 ffmpeg，体积小兼容性好；自动检测或手动指定路径）
- 🎵 实时预览：空格播放/暂停、Z/X 前后 10 帧、滚轮 ±5 帧、判定线音效（方波）
- 🎨 可调参数：分辨率/帧率/滚动速度/判定线位置/音符样式/歌词/音高曲线/颜色/淡入淡出/透明背景
- 🌙 深/浅色主题（FluentAvalonia）
- ⚙️ 配置保存/加载（JSON）
- 🖥️ 图形界面（Avalonia 12 + FluentAvalonia）与命令行（`UstViz.Cli`）双入口

## 构建与运行

需要 [.NET 10 SDK](https://dotnet.microsoft.com/)。

```bash
# 构建
dotnet build Ust-Visualizer.slnx

# 运行图形界面
dotnet run --project UstViz.App

# 运行测试
dotnet test Ust-Visualizer.slnx
```

### 单文件发布

```bash
dotnet publish UstViz.App -c Release -o publish -r win-x64 --self-contained true -p:PublishSingleFile=true
```

发布产物为单个 `UstViz.App.exe`，可直接分发（Windows）。

## 命令行用法（UstViz.Cli）

```bash
# 导出 AVI（无依赖）
dotnet run --project UstViz.Cli -- --ust 歌曲.ust --out 视频.avi --width 1920 --height 1080 --fps 30

# 导出 MP4（自动检测 ffmpeg；也可 --ffmpeg 指定路径）
dotnet run --project UstViz.Cli -- --ust 歌曲.ust --out 视频.mp4 --ffmpeg C:\ffmpeg\bin\ffmpeg.exe

# 调试：只导出前 N 帧
dotnet run --project UstViz.Cli -- --ust 歌曲.ust --max-frames 300
```

## 使用方法（图形界面）

1. 选择 UST 文件（需先经 [UtaFormatix](https://utaformatix.tk/) 等工具转换为 UST 格式，转换时勾选“转换音高参数”）
2. 选择输出文件夹
3. 按需调整参数（右侧 4 个选项卡）
4. 点击 **🎵 实时预览** 预览效果，或 **🎬 生成视频** 导出
5. 输出 MP4 时可在“输出设置”中指定 ffmpeg 路径（留空则自动检测）

> 提示：AVI 格式无外部依赖，但单个文件超过约 2GB 时会提示改用 MP4；长视频/高分辨率建议使用 MP4。

## 项目结构

```
UstViz.Core      领域层：模型 / UST 解析 / 音高曲线算法 / 配置 / 抽象契约
UstViz.Rendering SkiaSharp 渲染引擎：帧渲染 / MJPEG AVI / FFmpeg MP4 / 视频导出服务
UstViz.Audio     音频播放实现（NAudio，实现 Core 的 IAudioPlayer）
UstViz.App       Avalonia 图形界面（FluentAvalonia 主题）
UstViz.Cli       命令行导出工具
UstViz.Tests     xUnit 测试（解析/渲染/视频/音频/预览逻辑）
```

架构与依赖规则见 [ARCHITECTURE.md](ARCHITECTURE.md)。

## 许可证

[GNU General Public License v3.0](LICENSE)

