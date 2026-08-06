# 架构与依赖规则（Architecture & Dependency Rules）

> 目标：各层职责单一、依赖只允许自上而下、与系统和第三方库解耦，便于测试与跨平台。

## 项目布局

| 项目 | 职责 | 允许的依赖 |
|---|---|---|
| `UstViz.Core` | 纯领域层：模型 / 解析 / 算法 / 配置 / 抽象契约 / 基础设施默认实现 | 仅 BCL + `System.Text.Json`（配置序列化）。**禁止** UI、渲染、音频库 |
| `UstViz.Rendering` | SkiaSharp 渲染引擎（帧渲染 / 字体 / 视频导出） | `UstViz.Core` + SkiaSharp |
| `UstViz.Cli` | 命令行导出工具（UST → 视频） | `UstViz.Core` + `UstViz.Rendering` |
| `UstViz.Audio` | 音频播放实现（NAudio，Windows） | `UstViz.Core`（实现 `IAudioPlayer`）+ NAudio |
| `UstViz.App` | Avalonia UI（**主题：FluentAvalonia**） | `UstViz.Core` / `Rendering` / `Audio` + Avalonia + FluentAvalonia |
| `UstViz.Tests` | xUnit 测试 | 各项目 + xUnit |

## 分层与依赖方向

```
Abstractions（接口契约：IFileSystem / IPlatformDefaults / IAudioPlayer）
        ↑
Models / Algorithms（纯数据与纯函数，零依赖）
        ↑
Parsing / Config / IO / Platform（领域服务，仅依赖接口与数据）
        ↑
Rendering / App（边缘层，可依赖第三方库）
```

## 规则（必须遵守）

1. **依赖单向**：只允许自上而下，禁止反向依赖与循环依赖（Core 不得引用 App）。
2. **依赖接口而非实现**：跨模块调用通过接口 + 构造注入（如 `IFileSystem`、`IAudioPlayer`）。
3. **第三方库隔离**：SkiaSharp / NAudio / Avalonia / FluentAvalonia 只允许出现在边缘层（Rendering / Audio / App），Core 不引用。
4. **平台差异封装**：操作系统相关逻辑（字体候选等）统一走 `IPlatformDefaults`，其余代码不感知平台。
5. **数据与行为分离**：`AppConfig` / `UstNote` 等为纯数据对象，不包含平台或 IO 行为。

## 已确认的技术选型

- UI 框架：Avalonia 12.x + CommunityToolkit.Mvvm
- **主题：FluentAvalonia**（含 ColorPicker 等控件）
- 渲染：SkiaSharp（`UstViz.Rendering`：`FrameRenderer` + `VideoExportService`）
- 视频输出：MJPEG AVI（自写写入器，无外部依赖）/ FFmpeg H.264 MP4（提供 ffmpeg 路径时）
- 音频：接口 `IAudioPlayer` + 方波合成 `SquareWaveSynthesizer`（Core），实现 `NaudioAudioPlayer`（UstViz.Audio，NAudio 混音）
- 配置序列化：System.Text.Json



