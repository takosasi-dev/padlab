using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace PadLab.App.Rendering;

public interface IFrameRenderer
{
    void Render(SKCanvas canvas, SKRect bounds, double dtSeconds, bool motion);
}

/// <summary>CompositionTarget.Rendering ごとに Renderer を呼ぶ SKElement。IsActive=false で停止。</summary>
public sealed class SkiaCanvasView : SKElement
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastFrame;
    private int _frames;
    private double _fpsWindowStart;
    private bool _hooked;

    public IFrameRenderer? Renderer { get; set; }
    public bool Motion { get; set; } = true;
    public double MeasuredFps { get; private set; }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(SkiaCanvasView), new PropertyMetadata(true, (d, _) => ((SkiaCanvasView)d).Hook()));

    public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

    public SkiaCanvasView()
    {
        Loaded += (_, _) => Hook();
        Unloaded += (_, _) => Unhook();
        PaintSurface += OnPaint;
    }

    private void Hook()
    {
        var want = IsActive && IsLoaded;
        if (want && !_hooked) { CompositionTarget.Rendering += OnRendering; _hooked = true; }
        else if (!want) Unhook();
    }

    private void Unhook()
    {
        if (!_hooked) return;
        CompositionTarget.Rendering -= OnRendering;
        _hooked = false;
    }

    private void OnRendering(object? sender, EventArgs e) => InvalidateVisual();

    private void OnPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var now = _clock.Elapsed.TotalSeconds;
        var dt = _lastFrame == 0 ? 1.0 / 60 : Math.Min(now - _lastFrame, 0.1);
        _lastFrame = now;
        _frames++;
        if (now - _fpsWindowStart >= 1.0)
        {
            MeasuredFps = _frames / (now - _fpsWindowStart);
            _frames = 0;
            _fpsWindowStart = now;
        }

        var canvas = e.Surface.Canvas;
        canvas.Clear(Theme.Background);
        Renderer?.Render(canvas, SKRect.Create(e.Info.Width, e.Info.Height), dt, Motion);
    }
}
