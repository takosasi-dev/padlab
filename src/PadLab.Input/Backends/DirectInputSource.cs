using PadLab.Core.Model;
using PadLab.Core.Profiles;
using Vortice.DirectInput;
using Capabilities = PadLab.Core.Model.Capabilities;

namespace PadLab.Input.Backends;

/// <summary>
/// 汎用パッド (DirectInput)。Buffered モードで 1 イベント = 1 スナップショット。
/// ForceFeedback は非対応 (Capabilities に Rumble を含めない)。
/// 参照: https://learn.microsoft.com/previous-versions/windows/desktop/ee416842(v=vs.85)
/// </summary>
public sealed class DirectInputSource : IGamepadSource
{
    /// <summary>App が MainWindow の HWND を設定する。0 なら hwnd 無しで Background|NonExclusive を試す。</summary>
    public static IntPtr WindowHandle { get; set; }

    private const int AxisMax = 65535;
    private const int AxisCenter = 32767;

    private readonly IDirectInputDevice8 _device;
    private readonly JoystickState _state = new();
    private readonly Queue<GamepadSnapshot> _pending = new();
    private readonly bool _combinedZ;
    private bool _acquired;

    public DirectInputSource(IDirectInput8 di, DeviceInstance instance, DeviceDescriptor descriptor, DeviceProfile profile)
    {
        Descriptor = descriptor;
        _device = di.CreateDevice(instance.InstanceGuid);
        _device.SetDataFormat<RawJoystickState>();
        var hwnd = WindowHandle;
        var level = CooperativeLevel.Background | CooperativeLevel.NonExclusive;
        try { _device.SetCooperativeLevel(hwnd, level); }
        catch (Exception) when (hwnd != IntPtr.Zero) { _device.SetCooperativeLevel(IntPtr.Zero, level); }
        _device.Properties.BufferSize = 256;

        var hasRz = false;
        try
        {
            foreach (var obj in _device.GetObjects(DeviceObjectTypeFlags.Axis))
            {
                if (obj.ObjectType == ObjectGuid.RzAxis) hasRz = true;
                try { _device.GetObjectPropertiesById(obj.ObjectId).Range = new InputRange(0, AxisMax); } catch (Exception) { }
            }
        }
        catch (Exception) { }
        // Generic で RZ 軸が無い機種は Z が左右トリガーの合成 (32767 中心) とみなす [要検証]
        _combinedZ = profile.Kind == DeviceKind.Generic && !hasRz;
        Acquire();
    }

    public DeviceDescriptor Descriptor { get; }
    public Capabilities Capabilities => Capabilities.None;

    private void Acquire()
    {
        try { _acquired = _device.Acquire().Success; }
        catch (Exception) { _acquired = false; }
    }

    public bool TryRead(out GamepadSnapshot snapshot)
    {
        if (_pending.Count == 0) Fetch();
        if (_pending.Count == 0) { snapshot = GamepadSnapshot.Neutral; return false; }
        snapshot = _pending.Dequeue();
        return true;
    }

    private void Fetch()
    {
        if (!_acquired) { Acquire(); if (!_acquired) return; }
        JoystickUpdate[] updates;
        try
        {
            _device.Poll();
            updates = _device.GetBufferedJoystickData();
        }
        catch (Exception)
        {
            _acquired = false;
            return;
        }
        foreach (var u in updates)
        {
            _state.Update(u);
            _pending.Enqueue(ToSnapshot(_state, Clock.NowNs(), _combinedZ));
        }
    }

    internal static GamepadSnapshot ToSnapshot(JoystickState s, long ns, bool combinedZ)
    {
        ulong bits = 0;
        List<int>? extra = null;
        var buttons = s.Buttons;
        for (var i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i]) continue;
            if (i < 32) bits |= 1UL << ((int)ButtonId.Generic1 + i);
            else (extra ??= new List<int>()).Add(i);
        }
        bits |= PovToBits(s.PointOfViewControllers.Length > 0 ? s.PointOfViewControllers[0] : -1);

        float lt, rt;
        if (combinedZ) (lt, rt) = SplitCombinedTrigger(s.Z);
        else (lt, rt) = (AxisToTrigger(s.Z), AxisToTrigger(s.RotationZ));

        return new GamepadSnapshot(ns, bits, AxisToStick(s.X, s.Y), AxisToStick(s.RotationX, s.RotationY), lt, rt, ExtraButtons: extra);
    }

    /// <summary>0..65535 → -1..1。DirectInput は Y 下が + なので反転する。</summary>
    internal static Vector2F AxisToStick(int x, int y) => new(AxisToUnit(x), -AxisToUnit(y));

    /// <summary>0..65535 → 0..1。</summary>
    internal static float AxisToTrigger(int z) => Math.Clamp(z / (float)AxisMax, 0f, 1f);

    /// <summary>合成 Z 軸 (32767 中心)。中心より下=左、上=右 [要検証: 機種により逆]。</summary>
    internal static (float Left, float Right) SplitCombinedTrigger(int z)
    {
        var d = (z - AxisCenter) / (float)AxisCenter;
        return d < 0 ? (Math.Clamp(-d, 0, 1), 0) : (0, Math.Clamp(d, 0, 1));
    }

    /// <summary>POV (1/100 度、-1 または 0xFFFF は中立) → 十字キー。</summary>
    internal static ulong PovToBits(int pov)
    {
        if (pov < 0 || pov == 0xFFFF) return 0;
        ulong bits = 0;
        if (pov > 27000 || pov < 9000) bits |= GamepadSnapshot.Bit(ButtonId.DUp);
        if (pov > 0 && pov < 18000) bits |= GamepadSnapshot.Bit(ButtonId.DRight);
        if (pov > 9000 && pov < 27000) bits |= GamepadSnapshot.Bit(ButtonId.DDown);
        if (pov > 18000) bits |= GamepadSnapshot.Bit(ButtonId.DLeft);
        return bits;
    }

    private static float AxisToUnit(int v) => Math.Clamp((v - AxisCenter) / (float)AxisCenter, -1f, 1f);

    public void SetRumble(RumbleCommand command) { /* ForceFeedback 非対応: 「未対応」表示 */ }

    public void Dispose()
    {
        try { _device.Unacquire(); } catch (Exception) { }
        try { _device.Dispose(); } catch (Exception) { }
    }
}
