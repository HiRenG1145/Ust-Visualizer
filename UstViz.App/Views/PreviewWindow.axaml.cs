using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using UstViz.App.ViewModels;
using UstViz.Core.Audio;
using UstViz.Core.Config;
using UstViz.Core.Models;
using UstViz.Rendering;

namespace UstViz.App.Views;

/// <summary>实时预览窗口：渲染帧、播放控制、判定线音效。</summary>
public partial class PreviewWindow : Window
{
    private readonly PreviewViewModel _vm;
    private readonly FrameRenderer _renderer;
    private readonly UstProject _project;
    private readonly DispatcherTimer _timer;
    private readonly IAudioPlayer _audio;
    private bool _closed;

    public PreviewWindow(UstProject project, AppConfig config, IAudioPlayer audio)
    {
        InitializeComponent();

        _project = project;
        _audio = audio;
        _vm = new PreviewViewModel(project, config, audio);
        _renderer = new FrameRenderer(config);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / config.Fps),
        };
        _timer.Tick += OnTick;
        _timer.Start();

        KeyDown += OnKeyDown;
        AddHandler(PointerWheelChangedEvent, OnPointerWheel, RoutingStrategies.Tunnel);
        Closed += OnClosed;

        RenderFrame();
    }

    private void OnTick(object? sender, EventArgs e) => RenderFrame();

    private void RenderFrame()
    {
        if (_closed)
            return;

        _vm.Tick(); // 播放中推进时间并触发音效

        using var frame = _renderer.Render(_project, _vm.CurrentTime, _vm.TotalDuration);
        PreviewImage.Source = ToWriteableBitmap(frame);

        FrameText.Text = $"帧: {_vm.CurrentFrame}/{_vm.TotalFrames}";
        TimeText.Text = $"时间: {_vm.CurrentTime:F2}/{_vm.TotalDuration:F2}s";
        StatusText.Text = $"状态: {(_vm.IsPlaying ? "播放中" : "暂停")} | FPS: {_vm.Config.Fps}";
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                _vm.TogglePlay();
                e.Handled = true;
                break;
            case Key.Z:
                _vm.StepBack();
                e.Handled = true;
                break;
            case Key.X:
                _vm.StepForward();
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        _vm.Scroll(e.Delta.Y > 0 ? -5 : 5); // 上滚后退、下滚前进（与 Python 一致）
        e.Handled = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _timer.Stop();
        _renderer.Dispose();
        _audio.Dispose();
    }

    /// <summary>SKBitmap (BGRA premul) → Avalonia WriteableBitmap（零转换复制）。</summary>
    private static WriteableBitmap ToWriteableBitmap(SKBitmap sk)
    {
        var size = new PixelSize(sk.Width, sk.Height);
        var bmp = new WriteableBitmap(size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var fb = bmp.Lock();

        var src = sk.GetPixelSpan();
        int stride = sk.Width * 4;
        var buffer = new byte[stride];
        for (int y = 0; y < sk.Height; y++)
        {
            src.Slice(y * stride, stride).CopyTo(buffer);
            Marshal.Copy(buffer, 0, IntPtr.Add(fb.Address, y * fb.RowBytes), stride);
        }
        return bmp;
    }
}

