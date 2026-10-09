using PadLab.App.Services;
using PadLab.Core.Model;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>
/// 基本入力画面: コントローラー図 + 両スティック (トレイル + 到達範囲の外形) + トリガーゲージ×2 + トリガー時系列。
/// 毎フレーム Hub.Latest を読むだけで、判定はすべて DeviceSession のアナライザに任せる。
/// </summary>
public sealed class BasicInputRenderer : IFrameRenderer
{
    private readonly DeviceSession _s;
    private readonly ControllerFigure _figure;
    private readonly Dictionary<ButtonId, Glow> _glows = new();
    private readonly StickTrail _leftTrail = new(), _rightTrail = new();
    private readonly TriggerGauge _lt = new() { Label = Strings.LeftTrigger }, _rt = new() { Label = Strings.RightTrigger };
    private readonly Glow _ltSnap = new(), _rtSnap = new();
    private readonly LineChart _ltChart = new(), _rtChart = new() { Color = Theme.Warn };
    private readonly SKPaint _ring = Theme.Stroke(Theme.Outline, 1f);
    private readonly SKPaint _range = new() { Color = Theme.Accent.WithAlpha(50), IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _rangeLine = Theme.Stroke(Theme.Accent.WithAlpha(120), 1f);
    private readonly SKPaint _text = Theme.Fill(Theme.TextDim);
    private readonly SKFont _font = Theme.JpFont(12);
    private readonly SKPathBuilder _path = new();
    private long _lastNs = -1;

    // ヒーロー演出: プロファイル順に 40ms ずつ Glow を点灯
    private readonly ButtonId[] _heroOrder;
    private int _heroIndex = -1;
    private double _heroClock;
    private const double HeroStepSeconds = 0.04;

    public BasicInputRenderer(DeviceSession session)
    {
        _s = session;
        _figure = new ControllerFigure(session.Source.Descriptor.Kind, session.Profile);
        _heroOrder = session.Profile.Buttons.Select(b => b.Id).ToArray();
    }

    /// <summary>初回接続時のヒーロー演出を開始する。motion=false のフレームでは何もせず終了する。</summary>
    public void StartHero() { _heroIndex = 0; _heroClock = 0; }

    public void Render(SKCanvas c, SKRect bounds, double dt, bool motion)
    {
        var s = _s.Hub.Latest;
        if (s.TimestampNs != _lastNs)
        {
            _lastNs = s.TimestampNs;
            _leftTrail.Push(s.LeftStick, s.TimestampNs);
            _rightTrail.Push(s.RightStick, s.TimestampNs);
        }

        if (_heroIndex >= 0)
        {
            if (!motion) _heroIndex = -1;
            else
            {
                _heroClock += dt;
                while (_heroIndex >= 0 && _heroClock >= HeroStepSeconds)
                {
                    _heroClock -= HeroStepSeconds;
                    if (!_glows.TryGetValue(_heroOrder[_heroIndex], out var g)) _glows[_heroOrder[_heroIndex]] = g = new Glow();
                    g.Trigger();
                    if (++_heroIndex >= _heroOrder.Length) _heroIndex = -1;
                }
            }
        }

        var pad = 12f;
        var figRect = new SKRect(bounds.Left + pad, bounds.Top + pad, bounds.Right - pad, bounds.Top + bounds.Height * 0.55f);
        _figure.Draw(c, figRect, s, _glows, _s.Coverage.State.UnregisteredSeen, motion, dt);

        _ring.Color = Theme.Outline; _text.Color = Theme.TextDim;
        var lower = new SKRect(bounds.Left + pad, figRect.Bottom + pad, bounds.Right - pad, bounds.Bottom - pad);
        var plotR = Math.Min(lower.Height * 0.36f, lower.Width * 0.11f);
        var plotY = lower.Top + 18 + plotR;

        var leftC = new SKPoint(lower.Left + plotR, plotY);
        DrawStick(c, leftC, plotR, Strings.LeftStick, _s.LeftRange.MaxRadiusBySector, _leftTrail, motion);
        var rightC = new SKPoint(lower.Left + plotR * 3 + pad * 2, plotY);
        DrawStick(c, rightC, plotR, Strings.RightStick, _s.RightRange.MaxRadiusBySector, _rightTrail, motion);

        var gaugeX = rightC.X + plotR + pad * 2;
        var gaugeTop = lower.Top + 18;
        var gaugeH = Math.Max(40f, lower.Bottom - 20 - gaugeTop);
        _lt.Draw(c, new SKRect(gaugeX, gaugeTop, gaugeX + 26, gaugeTop + gaugeH), s.LeftTrigger, _s.LeftTrigger.Max, _ltSnap, motion);
        _rt.Draw(c, new SKRect(gaugeX + 40, gaugeTop, gaugeX + 66, gaugeTop + gaugeH), s.RightTrigger, _s.RightTrigger.Max, _rtSnap, motion);

        var chartX = gaugeX + 84;
        if (bounds.Right - pad - chartX > 60)
        {
            var half = (gaugeH - 6) / 2;
            _ltChart.Draw(c, new SKRect(chartX, gaugeTop, bounds.Right - pad, gaugeTop + half), _s.LeftTrigger.History, 5_000_000_000L, 0f, 1f);
            _rtChart.Draw(c, new SKRect(chartX, gaugeTop + half + 6, bounds.Right - pad, gaugeTop + gaugeH), _s.RightTrigger.History, 5_000_000_000L, 0f, 1f);
            c.DrawText(Strings.LeftTrigger, chartX + 4, gaugeTop + 12, SKTextAlign.Left, _font, _text);
            c.DrawText(Strings.RightTrigger, chartX + 4, gaugeTop + half + 18, SKTextAlign.Left, _font, _text);
        }
    }

    private void DrawStick(SKCanvas c, SKPoint center, float r, string label, float[] sectors, StickTrail trail, bool motion)
    {
        c.DrawCircle(center, r, _ring);
        // 到達範囲の薄い外形
        if (sectors.Length > 0 && sectors.Any(v => v > 0))
        {
            _path.Reset();
            var step = 360f / sectors.Length;
            for (var i = 0; i < sectors.Length; i++)
            {
                var a = (i + 0.5f) * step * MathF.PI / 180f;
                var p = new SKPoint(center.X + MathF.Cos(a) * sectors[i] * r, center.Y - MathF.Sin(a) * sectors[i] * r);
                if (i == 0) _path.MoveTo(p); else _path.LineTo(p);
            }
            _path.Close();
            using var path = _path.Detach();
            c.DrawPath(path, _range);
            c.DrawPath(path, _rangeLine);
        }
        trail.Draw(c, center, r, motion);
        c.DrawText(label, center.X, center.Y - r - 4, SKTextAlign.Center, _font, _text);
    }
}
