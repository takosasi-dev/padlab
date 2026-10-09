using PadLab.Core.Model;
using PadLab.Input.Native;
using Windows.Gaming.Input;

namespace PadLab.Input.Backends;

/// <summary>
/// Xbox 系 (XInput 互換) コントローラー。入力と振動は Windows.Gaming.Input、Guide ボタンだけ XInputGetStateEx。
/// 参照: https://learn.microsoft.com/uwp/api/windows.gaming.input.gamepad
///       https://learn.microsoft.com/uwp/api/windows.gaming.input.rawgamecontroller
/// </summary>
public sealed class XboxSource : IGamepadSource
{
    private readonly Gamepad _gamepad;
    private readonly int _xinputIndex;
    private readonly RawGameController? _raw;
    private readonly bool[]? _rawButtons;
    private readonly GameControllerSwitchPosition[]? _rawSwitches;
    private readonly double[]? _rawAxes;
    private ulong _lastTimestamp;
    private long _lastBatteryNs;
    private float? _battery;
    private Capabilities _caps = Capabilities.Rumble | Capabilities.ImpulseTriggers | Capabilities.Battery;

    /// <summary>WGI の標準ボタン数。RawGameController がこれより多く返す分を余剰 (Share 等) として扱う [要検証]。</summary>
    private const int WgiStandardButtonCount = 14;

    public XboxSource(Gamepad gamepad, int xinputUserIndex, DeviceDescriptor descriptor)
    {
        _gamepad = gamepad;
        _xinputIndex = xinputUserIndex;
        Descriptor = descriptor;
        try
        {
            _raw = RawGameController.FromGameController(gamepad);
            if (_raw is not null)
            {
                _rawButtons = new bool[_raw.ButtonCount];
                _rawSwitches = new GameControllerSwitchPosition[_raw.SwitchCount];
                _rawAxes = new double[_raw.AxisCount];
            }
        }
        catch (Exception) { _raw = null; }
    }

    public DeviceDescriptor Descriptor { get; }
    public Capabilities Capabilities => _caps;

    public bool TryRead(out GamepadSnapshot snapshot)
    {
        var r = _gamepad.GetCurrentReading();
        if (r.Timestamp == _lastTimestamp) { snapshot = GamepadSnapshot.Neutral; return false; }
        _lastTimestamp = r.Timestamp;
        var now = Clock.NowNs();

        var bits = MapButtons(r.Buttons);
        if (_xinputIndex >= 0 && ReadGuide()) bits |= GamepadSnapshot.Bit(ButtonId.Guide);

        List<int>? extra = null;
        if (_raw is not null && _rawButtons is not null && _rawButtons.Length > WgiStandardButtonCount)
        {
            try
            {
                _raw.GetCurrentReading(_rawButtons, _rawSwitches, _rawAxes);
                // [要検証] 余剰ボタンの index と Share の対応。最初の余剰を Share とみなし、残りは ExtraButtons。
                for (var i = WgiStandardButtonCount; i < _rawButtons.Length; i++)
                {
                    if (!_rawButtons[i]) continue;
                    if (i == WgiStandardButtonCount) bits |= GamepadSnapshot.Bit(ButtonId.Share);
                    else (extra ??= new List<int>()).Add(i);
                }
            }
            catch (Exception) { }
        }

        if (now - _lastBatteryNs > 5_000_000_000L) { _lastBatteryNs = now; _battery = ReadBattery(); }

        snapshot = new GamepadSnapshot(
            now, bits,
            new Vector2F((float)r.LeftThumbstickX, (float)r.LeftThumbstickY),
            new Vector2F((float)r.RightThumbstickX, (float)r.RightThumbstickY),
            (float)r.LeftTrigger, (float)r.RightTrigger,
            Battery: _battery, ExtraButtons: extra);
        return true;
    }

    internal static ulong MapButtons(GamepadButtons b)
    {
        ulong bits = 0;
        void Set(GamepadButtons flag, ButtonId id) { if ((b & flag) != 0) bits |= GamepadSnapshot.Bit(id); }
        Set(GamepadButtons.A, ButtonId.A); Set(GamepadButtons.B, ButtonId.B);
        Set(GamepadButtons.X, ButtonId.X); Set(GamepadButtons.Y, ButtonId.Y);
        Set(GamepadButtons.LeftShoulder, ButtonId.LB); Set(GamepadButtons.RightShoulder, ButtonId.RB);
        Set(GamepadButtons.DPadUp, ButtonId.DUp); Set(GamepadButtons.DPadDown, ButtonId.DDown);
        Set(GamepadButtons.DPadLeft, ButtonId.DLeft); Set(GamepadButtons.DPadRight, ButtonId.DRight);
        Set(GamepadButtons.LeftThumbstick, ButtonId.L3); Set(GamepadButtons.RightThumbstick, ButtonId.R3);
        Set(GamepadButtons.Menu, ButtonId.Start); Set(GamepadButtons.View, ButtonId.Select);
        // Elite 系のパドル。WGI が物理パドルを返すかは機種依存 [要検証]
        Set(GamepadButtons.Paddle1, ButtonId.P1); Set(GamepadButtons.Paddle2, ButtonId.P2);
        Set(GamepadButtons.Paddle3, ButtonId.P3); Set(GamepadButtons.Paddle4, ButtonId.P4);
        return bits;
    }

    private bool ReadGuide()
    {
        try
        {
            return XInput.XInputGetStateEx((uint)_xinputIndex, out var st) == XInput.ErrorSuccess
                && (st.Gamepad.wButtons & XInput.Guide) != 0;
        }
        catch (Exception) { return false; }
    }

    private float? ReadBattery()
    {
        try
        {
            var rep = _gamepad.TryGetBatteryReport();
            var full = rep?.FullChargeCapacityInMilliwattHours;
            var rem = rep?.RemainingCapacityInMilliwattHours;
            if (full is > 0 && rem is not null) return Math.Clamp((float)rem.Value / full.Value, 0, 1);
        }
        catch (Exception) { }
        _caps &= ~Capabilities.Battery;
        return null;
    }

    public void SetRumble(RumbleCommand c)
    {
        try
        {
            _gamepad.Vibration = new GamepadVibration
            {
                LeftMotor = c.LeftMotor, RightMotor = c.RightMotor,
                LeftTrigger = c.LeftTrigger, RightTrigger = c.RightTrigger,
            };
        }
        catch (Exception)
        {
            _caps &= ~(Capabilities.Rumble | Capabilities.ImpulseTriggers);
            if (_xinputIndex < 0) return;
            try
            {
                var v = new XInput.XINPUT_VIBRATION { wLeftMotorSpeed = (ushort)(c.LeftMotor * 65535), wRightMotorSpeed = (ushort)(c.RightMotor * 65535) };
                if (XInput.XInputSetState((uint)_xinputIndex, ref v) == XInput.ErrorSuccess) _caps |= Capabilities.Rumble;
            }
            catch (Exception) { }
        }
    }

    public void Dispose()
    {
        try { SetRumble(RumbleCommand.Stop); } catch (Exception) { }
    }
}
