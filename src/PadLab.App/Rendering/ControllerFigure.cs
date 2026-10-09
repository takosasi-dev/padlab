using PadLab.Core.Model;
using PadLab.Core.Profiles;
using SkiaSharp;

namespace PadLab.App.Rendering;

/// <summary>
/// 機種別の簡易コントローラー図。すべて SKPath でコード描画。
/// Xbox/Switch: スティック左上・右下、面ボタン右上。PS/Generic: スティック左右下、十字キー左上。
/// プロファイルにあるが図に位置の無いボタンと、ExtraButtons (未登録 #n) は図の下段に自動配置する。
/// </summary>
public sealed class ControllerFigure
{
    private enum Shape { Circle, Pill, Rect, Square }
    private readonly record struct Slot(float X, float Y, float R, Shape Shape);

    private const float UnitH = 0.62f;            // 図の高さ (幅 1 に対して)
    private readonly DeviceProfile _profile;
    private readonly Dictionary<ButtonId, Slot> _slots = new();
    private readonly (float X, float Y) _left, _right;
    private readonly bool _psLayout;

    private readonly SKPaint _body = Theme.Fill(Theme.Panel);
    private readonly SKPaint _bodyLine = Theme.Stroke(Theme.Outline, 2f);
    private readonly SKPaint _btn = Theme.Fill(Theme.Background);
    private readonly SKPaint _btnLine = Theme.Stroke(Theme.Outline, 1.2f);
    private readonly SKPaint _halo = new() { Color = Theme.Accent, IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6f) };
    private readonly SKPaint _label = Theme.Fill(Theme.TextDim);
    private readonly SKPaint _labelOn = Theme.Fill(Theme.Background);
    private readonly SKFont _font = Theme.JpFont(11);
    private readonly SKPathBuilder _path = new();

    public ControllerFigure(DeviceKind kind, DeviceProfile profile)
    {
        _profile = profile;
        _psLayout = kind is DeviceKind.DualShock4 or DeviceKind.DualSense or DeviceKind.DualSenseEdge or DeviceKind.Generic;

        // 肩
        _slots[ButtonId.LB] = new(0.2f, 0.09f, 0.075f, Shape.Pill);
        _slots[ButtonId.RB] = new(0.8f, 0.09f, 0.075f, Shape.Pill);

        if (_psLayout)
        {
            AddDpad(0.2f, 0.33f);
            AddFace(0.8f, 0.33f);
            _left = (0.36f, 0.5f); _right = (0.64f, 0.5f);
            _slots[ButtonId.Select] = new(0.34f, 0.2f, 0.028f, Shape.Pill);
            _slots[ButtonId.Start] = new(0.66f, 0.2f, 0.028f, Shape.Pill);
            _slots[ButtonId.TouchpadClick] = new(0.5f, 0.25f, 0.1f, Shape.Rect);
            _slots[ButtonId.Guide] = new(0.5f, 0.41f, 0.028f, Shape.Circle);
            _slots[ButtonId.Mute] = new(0.5f, 0.49f, 0.02f, Shape.Pill);
            _slots[ButtonId.Fn1] = new(0.43f, 0.57f, 0.018f, Shape.Circle);
            _slots[ButtonId.Fn2] = new(0.57f, 0.57f, 0.018f, Shape.Circle);
        }
        else
        {
            _left = (0.2f, 0.33f); _right = (0.66f, 0.5f);
            AddDpad(0.34f, 0.48f);
            AddFace(0.8f, 0.33f);
            _slots[ButtonId.Select] = new(0.4f, 0.28f, 0.028f, Shape.Circle);
            _slots[ButtonId.Start] = new(0.6f, 0.28f, 0.028f, Shape.Circle);
            _slots[ButtonId.Guide] = new(0.5f, 0.21f, 0.035f, Shape.Circle);
            if (kind == DeviceKind.SwitchPro)
            {
                _slots[ButtonId.Capture] = new(0.43f, 0.4f, 0.022f, Shape.Square);
                _slots[ButtonId.Guide] = new(0.57f, 0.4f, 0.022f, Shape.Circle);
                _slots[ButtonId.Select] = new(0.4f, 0.24f, 0.022f, Shape.Circle);
                _slots[ButtonId.Start] = new(0.6f, 0.24f, 0.022f, Shape.Circle);
            }
            else
            {
                _slots[ButtonId.Share] = new(0.5f, 0.36f, 0.022f, Shape.Square);
            }
        }
        // 背面パドル (グリップ下端)
        _slots[ButtonId.P1] = new(0.22f, 0.58f, 0.03f, Shape.Pill);
        _slots[ButtonId.P2] = new(0.3f, 0.6f, 0.03f, Shape.Pill);
        _slots[ButtonId.P3] = new(0.7f, 0.6f, 0.03f, Shape.Pill);
        _slots[ButtonId.P4] = new(0.78f, 0.58f, 0.03f, Shape.Pill);
        // スティック押し込みはスティック自体の点灯で表す
        _slots[ButtonId.L3] = new(_left.X, _left.Y, 0.05f, Shape.Circle);
        _slots[ButtonId.R3] = new(_right.X, _right.Y, 0.05f, Shape.Circle);
    }

    private void AddDpad(float cx, float cy)
    {
        const float d = 0.055f, r = 0.024f;
        _slots[ButtonId.DUp] = new(cx, cy - d, r, Shape.Square);
        _slots[ButtonId.DDown] = new(cx, cy + d, r, Shape.Square);
        _slots[ButtonId.DLeft] = new(cx - d, cy, r, Shape.Square);
        _slots[ButtonId.DRight] = new(cx + d, cy, r, Shape.Square);
    }

    private void AddFace(float cx, float cy)
    {
        const float d = 0.06f, r = 0.028f;
        _slots[ButtonId.Y] = new(cx, cy - d, r, Shape.Circle);
        _slots[ButtonId.A] = new(cx, cy + d, r, Shape.Circle);
        _slots[ButtonId.X] = new(cx - d, cy, r, Shape.Circle);
        _slots[ButtonId.B] = new(cx + d, cy, r, Shape.Circle);
    }

    /// <summary>bounds 内で図が占める矩形 (下段の余白を除く)。</summary>
    private SKRect FigureRect(SKRect bounds, bool hasOverflow)
    {
        var avail = hasOverflow ? bounds.Height * 0.82f : bounds.Height;
        var w = Math.Min(bounds.Width, avail / UnitH);
        var h = w * UnitH;
        var x = bounds.MidX - w / 2;
        var y = bounds.Top + (avail - h) / 2;
        return new SKRect(x, y, x + w, y + h);
    }

    public SKPoint MotorPosition(bool left, SKRect bounds)
    {
        var r = FigureRect(bounds, false);
        return new SKPoint(r.Left + r.Width * (left ? 0.17f : 0.83f), r.Top + r.Width * 0.5f);
    }

    public void Draw(SKCanvas c, SKRect bounds, GamepadSnapshot s, IReadOnlyDictionary<ButtonId, Glow> glows,
        IReadOnlyList<int> extraButtons, bool motion, double dt = 1.0 / 60)
    {
        var overflow = _profile.Buttons.Where(b => !_slots.ContainsKey(b.Id)).ToList();
        var hasOverflow = overflow.Count > 0 || extraButtons.Count > 0;
        var r = FigureRect(bounds, hasOverflow);
        var w = r.Width;
        _font.Size = Math.Max(9f, w * 0.028f);
        _body.Color = Theme.Panel; _bodyLine.Color = Theme.Outline; _btnLine.Color = Theme.Outline;
        _label.Color = Theme.TextDim; _labelOn.Color = Theme.Background;

        DrawBody(c, r);
        DrawTrigger(c, r, 0.2f, s.LeftTrigger);
        DrawTrigger(c, r, 0.8f, s.RightTrigger);

        // スティック台座
        DrawStickBase(c, r, _left, s.LeftStick);
        DrawStickBase(c, r, _right, s.RightStick);

        foreach (var (id, slot) in _slots)
        {
            if (!HasInProfile(id) && id is not (ButtonId.L3 or ButtonId.R3)) continue;
            var down = s.IsDown(id);
            var glow = GlowFor(id, glows);
            glow.Update(down);
            var g = glow.Value(dt, motion);
            var (cx, cy) = (r.Left + slot.X * w, r.Top + slot.Y * w);
            var rad = slot.R * w;
            if (id is ButtonId.L3 or ButtonId.R3)
            {
                var stick = id == ButtonId.L3 ? s.LeftStick : s.RightStick;
                cx += stick.X * rad * 0.7f; cy -= stick.Y * rad * 0.7f;
            }
            DrawButton(c, cx, cy, rad, slot.Shape, down, g, ShortLabel(id));
        }

        if (!hasOverflow) return;
        // 下段: 図に無いボタン + 未登録ボタン
        var items = overflow.Select(b => (Label: b.Label, Down: s.IsDown(b.Id), Key: b.Id, Extra: -1)).ToList();
        foreach (var n in extraButtons)
            items.Add((string.Format(Strings.UnregisteredButtonFormat, n), s.ExtraButtons?.Contains(n) == true, (ButtonId)(32 + n % 32), n));
        var rad2 = Math.Max(10f, w * 0.03f);
        var gap = rad2 * 2.6f;
        var perRow = Math.Max(1, (int)(bounds.Width / gap));
        var y0 = r.Bottom + rad2 * 1.8f;
        for (var i = 0; i < items.Count; i++)
        {
            var row = i / perRow; var col = i % perRow;
            var count = Math.Min(perRow, items.Count - row * perRow);
            var x = bounds.MidX - (count - 1) * gap / 2 + col * gap;
            var y = y0 + row * gap * 1.4f;
            if (y + rad2 > bounds.Bottom) break;
            var glow = items[i].Extra >= 0 ? OwnGlow(("extra", items[i].Extra)) : GlowFor(items[i].Key, glows);
            glow.Update(items[i].Down);
            DrawButton(c, x, y, rad2, Shape.Circle, items[i].Down, glow.Value(dt, motion), "");
            c.DrawText(items[i].Label, x, y + rad2 + _font.Size + 2, SKTextAlign.Center, _font, _label);
        }
    }

    /// <summary>図の中に収まる短い表記。十字キーは矢印、スティック押し込みは無し、それ以外はプロファイルのラベル。</summary>
    private string ShortLabel(ButtonId id) => id switch
    {
        ButtonId.L3 or ButtonId.R3 => "",
        ButtonId.DUp => "↑", ButtonId.DDown => "↓", ButtonId.DLeft => "←", ButtonId.DRight => "→",
        _ => _profile.LabelOf(id),
    };

    private bool HasInProfile(ButtonId id) => _profile.Buttons.Any(b => b.Id == id);

    private Glow GlowFor(ButtonId id, IReadOnlyDictionary<ButtonId, Glow> glows) =>
        glows.TryGetValue(id, out var g) ? g : OwnGlow(("btn", (int)id));

    private readonly Dictionary<(string, int), Glow> _misc = new();
    private Glow OwnGlow((string, int) key)
    {
        if (!_misc.TryGetValue(key, out var g)) _misc[key] = g = new Glow();
        return g;
    }

    private void DrawBody(SKCanvas c, SKRect r)
    {
        var w = r.Width; var x = r.Left; var y = r.Top;
        SKPoint P(float ux, float uy) => new(x + ux * w, y + uy * w);
        var gripBottom = _psLayout ? 0.62f : 0.58f;
        _path.Reset();
        _path.MoveTo(P(0.14f, 0.12f));
        _path.CubicTo(P(0.3f, 0.09f), P(0.7f, 0.09f), P(0.86f, 0.12f));      // 上辺
        _path.CubicTo(P(0.97f, 0.16f), P(1.0f, 0.35f), P(0.93f, gripBottom - 0.02f)); // 右肩→右グリップ外側
        _path.CubicTo(P(0.9f, gripBottom + 0.02f), P(0.78f, gripBottom + 0.02f), P(0.72f, 0.5f));   // 右グリップ下
        _path.CubicTo(P(0.68f, 0.44f), P(0.58f, 0.44f), P(0.5f, 0.44f));        // 中央くぼみ
        _path.CubicTo(P(0.42f, 0.44f), P(0.32f, 0.44f), P(0.28f, 0.5f));
        _path.CubicTo(P(0.22f, gripBottom + 0.02f), P(0.1f, gripBottom + 0.02f), P(0.07f, gripBottom - 0.02f));  // 左グリップ下
        _path.CubicTo(P(0.0f, 0.35f), P(0.03f, 0.16f), P(0.14f, 0.12f));      // 左肩
        _path.Close();
        using var path = _path.Detach();
        c.DrawPath(path, _body);
        c.DrawPath(path, _bodyLine);
    }

    private void DrawTrigger(SKCanvas c, SKRect r, float ux, float value)
    {
        var w = r.Width;
        var rect = new SKRect(r.Left + (ux - 0.06f) * w, r.Top + 0.005f * w, r.Left + (ux + 0.06f) * w, r.Top + 0.045f * w);
        _btn.Color = Theme.Background;
        c.DrawRoundRect(rect, 4, 4, _btn);
        if (value > 0.01f)
        {
            var fill = new SKRect(rect.Left, rect.Top, rect.Left + rect.Width * Math.Clamp(value, 0, 1), rect.Bottom);
            _btn.Color = Theme.Accent;
            c.DrawRoundRect(fill, 4, 4, _btn);
        }
        c.DrawRoundRect(rect, 4, 4, _btnLine);
    }

    private void DrawStickBase(SKCanvas c, SKRect r, (float X, float Y) u, Vector2F v)
    {
        var w = r.Width;
        var cx = r.Left + u.X * w; var cy = r.Top + u.Y * w;
        var rad = 0.085f * w;
        _btn.Color = Theme.Background;
        c.DrawCircle(cx, cy, rad, _btn);
        c.DrawCircle(cx, cy, rad, _btnLine);
    }

    private void DrawButton(SKCanvas c, float cx, float cy, float rad, Shape shape, bool down, float glow, string label)
    {
        if (glow > 0.01f)
        {
            _halo.Color = Theme.Accent.WithAlpha((byte)(160 * glow));
            c.DrawCircle(cx, cy, rad * (1.3f + 0.5f * glow), _halo);
        }
        _btn.Color = down ? Theme.Accent : Theme.Background;
        switch (shape)
        {
            case Shape.Circle: c.DrawCircle(cx, cy, rad, _btn); c.DrawCircle(cx, cy, rad, _btnLine); break;
            case Shape.Pill:
                var pr = new SKRect(cx - rad, cy - rad * 0.4f, cx + rad, cy + rad * 0.4f);
                c.DrawRoundRect(pr, rad * 0.4f, rad * 0.4f, _btn); c.DrawRoundRect(pr, rad * 0.4f, rad * 0.4f, _btnLine); break;
            case Shape.Rect:
                var rr = new SKRect(cx - rad, cy - rad * 0.6f, cx + rad, cy + rad * 0.6f);
                c.DrawRoundRect(rr, 6, 6, _btn); c.DrawRoundRect(rr, 6, 6, _btnLine); break;
            case Shape.Square:
                var sq = new SKRect(cx - rad, cy - rad, cx + rad, cy + rad);
                c.DrawRoundRect(sq, 3, 3, _btn); c.DrawRoundRect(sq, 3, 3, _btnLine); break;
        }
        if (label.Length == 0) return;
        var fits = _font.MeasureText(label) <= rad * 2.2f;
        var ty = fits ? cy + _font.Size * 0.36f : cy + rad + _font.Size + 1;
        c.DrawText(label, cx, ty, SKTextAlign.Center, _font, fits && down ? _labelOn : _label);
    }
}
