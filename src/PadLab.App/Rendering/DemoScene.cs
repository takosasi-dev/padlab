using System.Windows.Input;
using PadLab.App.Services;
using PadLab.Core.Fake;
using PadLab.Core.Model;
using PadLab.Core.Profiles;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>
/// Phase 0 計測用: Xbox 図 + 左スティックのトレイル + 右スティックの散布図 3000 点 + トリガーゲージ + 時系列。
/// Fake デバイスを DemoDriver で動かし、描画スレッドが直接 TryRead で吸い出す (PollingLoop は使わない)。
/// </summary>
public sealed class DemoScene : IFrameRenderer, IDisposable
{
    public const int ScatterCapacity = 3000;

    public static readonly DeviceProfile XboxProfile = new(DeviceKind.XboxOne, "Xbox (デモ)", "0", new ButtonSpec[]
    {
        new(ButtonId.A, "A", ButtonCategory.Standard), new(ButtonId.B, "B", ButtonCategory.Standard),
        new(ButtonId.X, "X", ButtonCategory.Standard), new(ButtonId.Y, "Y", ButtonCategory.Standard),
        new(ButtonId.LB, "LB", ButtonCategory.Standard), new(ButtonId.RB, "RB", ButtonCategory.Standard),
        new(ButtonId.DUp, "↑", ButtonCategory.Standard), new(ButtonId.DDown, "↓", ButtonCategory.Standard),
        new(ButtonId.DLeft, "←", ButtonCategory.Standard), new(ButtonId.DRight, "→", ButtonCategory.Standard),
        new(ButtonId.L3, "L3", ButtonCategory.Standard), new(ButtonId.R3, "R3", ButtonCategory.Standard),
        new(ButtonId.Start, "Menu", ButtonCategory.Standard), new(ButtonId.Select, "View", ButtonCategory.Standard),
        new(ButtonId.Guide, "Xbox", ButtonCategory.Standard), new(ButtonId.Share, "Share", ButtonCategory.Extended),
    }, 0, Capabilities.Rumble | Capabilities.ImpulseTriggers);

    private readonly FakeGamepad _fake = new(DeviceKind.XboxOne);
    private readonly DemoDriver _driver;
    private readonly ControllerFigure _figure = new(DeviceKind.XboxOne, XboxProfile);
    private readonly Dictionary<ButtonId, Glow> _glows = new();
    private readonly StickTrail _trail = new();
    private readonly ScatterPlot _scatter = new() { Zoom = 6f };
    private readonly Vector2F[] _scatterRing = new Vector2F[ScatterCapacity];
    private int _scatterHead;
    private readonly TriggerGauge _lt = new() { Label = Strings.LeftTrigger }, _rt = new() { Label = Strings.RightTrigger };
    private readonly Glow _ltSnap = new(), _rtSnap = new();
    private readonly LineChart _chart = new();
    private readonly List<(long ns, float v)> _ltHistory = new();
    private readonly int[] _extra = { 7, 9 };
    private readonly SKPaint _ring = Theme.Stroke(Theme.Outline, 1f);
    private readonly SKPaint _text = Theme.Fill(Theme.TextDim);
    private readonly SKFont _font = Theme.JpFont(12);
    private GamepadSnapshot _latest = GamepadSnapshot.Neutral;
    private float _maxLt, _maxRt;

    public DemoScene()
    {
        _driver = new DemoDriver(_fake);
        // 初回フレームから 3000 点ある状態にする
        foreach (var s in ScriptedPattern.Drift(ScatterCapacity / 250.0, 0.02f, 0.01f, stick: StickId.Right, seed: 3))
            PushScatter(s.RightStick);
    }

    public void Start() => _driver.Start();
    public void Stop() => _driver.Stop();
    public void KeyDown(Key k) => _driver.KeyDown(k);
    public void KeyUp(Key k) => _driver.KeyUp(k);

    private void PushScatter(Vector2F v)
    {
        _scatterRing[_scatterHead] = v;
        _scatterHead = (_scatterHead + 1) % ScatterCapacity;
    }

    public void Render(SKCanvas c, SKRect bounds, double dt, bool motion)
    {
        while (_fake.TryRead(out var s))
        {
            _latest = s;
            _trail.Push(s.LeftStick, s.TimestampNs);
            PushScatter(s.RightStick);
            _ltHistory.Add((s.TimestampNs, s.LeftTrigger));
            if (_ltHistory.Count > 2000) _ltHistory.RemoveRange(0, _ltHistory.Count - 2000);
            _maxLt = Math.Max(_maxLt, s.LeftTrigger);
            _maxRt = Math.Max(_maxRt, s.RightTrigger);
        }

        var pad = 16f;
        var figRect = new SKRect(bounds.Left + pad, bounds.Top + pad, bounds.Left + bounds.Width * 0.55f, bounds.Bottom - pad);
        _figure.Draw(c, figRect, _latest, _glows, _extra, motion, dt);

        var rightX = bounds.Left + bounds.Width * 0.57f;
        var plotR = Math.Min((bounds.Width * 0.43f - pad * 3) / 4, bounds.Height * 0.22f);
        var plotY = bounds.Top + pad + plotR + 16;
        _ring.Color = Theme.Outline; _text.Color = Theme.TextDim;

        var leftC = new SKPoint(rightX + plotR, plotY);
        c.DrawCircle(leftC, plotR, _ring);
        _trail.Draw(c, leftC, plotR, motion);
        c.DrawText(Strings.LeftStick, leftC.X, plotY - plotR - 4, SKTextAlign.Center, _font, _text);

        var rightC = new SKPoint(rightX + plotR * 3 + pad, plotY);
        _scatter.Draw(c, rightC, plotR, _scatterRing, 0.03f, 0.08f);
        c.DrawText(Strings.RightStick, rightC.X, plotY - plotR - 4, SKTextAlign.Center, _font, _text);

        var gaugeTop = plotY + plotR + 40;
        var gaugeH = Math.Max(40f, bounds.Bottom - pad - 24 - gaugeTop);
        _lt.Draw(c, new SKRect(rightX, gaugeTop, rightX + 28, gaugeTop + gaugeH), _latest.LeftTrigger, _maxLt, _ltSnap, motion);
        _rt.Draw(c, new SKRect(rightX + 44, gaugeTop, rightX + 72, gaugeTop + gaugeH), _latest.RightTrigger, _maxRt, _rtSnap, motion);
        _chart.Draw(c, new SKRect(rightX + 90, gaugeTop, bounds.Right - pad, gaugeTop + gaugeH), _ltHistory, 5_000_000_000L, 0f, 1f);
    }

    public void Dispose() => Stop();
}
