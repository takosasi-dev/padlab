using PadLab.Core.Model;
using PadLab.Core.Profiles;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>振動テスト画面: ControllerFigure + モーター位置からの波紋 (強度連動、100ms ごと)。</summary>
public sealed class VibrationRenderer : IFrameRenderer
{
    private readonly ControllerFigure _figure;
    private readonly Dictionary<ButtonId, Glow> _glows = new();
    private readonly Ripple _ripple = new();
    private readonly Func<GamepadSnapshot> _latest;
    private double _acc;

    /// <summary>現在送信中の強度 0..1。VM が更新する。</summary>
    public float LeftMotor, RightMotor, LeftTrigger, RightTrigger;

    public VibrationRenderer(DeviceKind kind, DeviceProfile profile, Func<GamepadSnapshot> latest)
    {
        _figure = new ControllerFigure(kind, profile);
        _latest = latest;
    }

    public void Render(SKCanvas c, SKRect bounds, double dt, bool motion)
    {
        var s = _latest();
        _figure.Draw(c, bounds, s, _glows, s.ExtraButtons ?? Array.Empty<int>(), motion, dt);
        _acc += dt;
        if (_acc >= 0.1)
        {
            _acc = 0;
            var l = _figure.MotorPosition(true, bounds);
            var r = _figure.MotorPosition(false, bounds);
            var up = bounds.Width * 0.22f;   // トリガーはグリップの上、肩の位置
            _ripple.Emit(l, LeftMotor);
            _ripple.Emit(r, RightMotor);
            _ripple.Emit(new SKPoint(l.X, l.Y - up), LeftTrigger);
            _ripple.Emit(new SKPoint(r.X, r.Y - up), RightTrigger);
        }
        _ripple.Draw(c, dt, motion);
    }
}
