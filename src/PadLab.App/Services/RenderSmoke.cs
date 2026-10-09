using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PadLab.App.Rendering;
using PadLab.Core.Model;
using PadLab.Core.Profiles;
using SkiaSharp;

namespace PadLab.App.Services;

/// <summary>--render-smoke / --phase0 のオフスクリーン実行。GUI 無しで描画部品を確認する。</summary>
public static class RenderSmoke
{
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);

    private static void Out(string s)
    {
        try { Console.WriteLine(s); } catch (Exception) { }
    }

    public static int RunSmoke()
    {
        AttachConsole(-1);
        try
        {
            using var bmp = new SKBitmap(640, 480);
            using var canvas = new SKCanvas(bmp);
            var bounds = SKRect.Create(640, 480);
            var snap = GamepadSnapshot.Neutral with
            {
                TimestampNs = Clock.NowNs(),
                Buttons = GamepadSnapshot.Bit(ButtonId.A) | GamepadSnapshot.Bit(ButtonId.LB),
                LeftStick = new Vector2F(0.5f, 0.3f), LeftTrigger = 1f, ExtraButtons = new[] { 7 },
            };
            var glows = new Dictionary<ButtonId, Glow> { [ButtonId.A] = new() };
            foreach (var motion in new[] { true, false })
            {
                foreach (var kind in new[] { DeviceKind.XboxOne, DeviceKind.DualSense, DeviceKind.SwitchPro, DeviceKind.Generic })
                {
                    var fig = new ControllerFigure(kind, DemoScene.XboxProfile);
                    canvas.Clear(Theme.Background);
                    fig.Draw(canvas, bounds, snap, glows, new[] { 7, 9 }, motion);
                    fig.Draw(canvas, bounds, GamepadSnapshot.Neutral, glows, new[] { 7, 9 }, motion); // 解放エッジ → Glow 減衰
                    _ = fig.MotorPosition(true, bounds);
                }
                var trail = new StickTrail();
                for (var i = 0; i < 100; i++) trail.Push(new Vector2F(MathF.Cos(i * 0.1f), MathF.Sin(i * 0.1f)), i * 4_000_000L);
                trail.Draw(canvas, new SKPoint(100, 100), 80, motion);

                var pts = new List<Vector2F>();
                for (var i = 0; i < 3000; i++) pts.Add(new Vector2F((i % 50) / 500f, (i % 30) / 300f));
                new ScatterPlot { Zoom = 6f }.Draw(canvas, new SKPoint(300, 100), 80, pts, 0.03f, 0.08f);

                var snapGlow = new Glow();
                new TriggerGauge { Label = "LT" }.Draw(canvas, new SKRect(500, 20, 530, 180), 1f, 0.9f, snapGlow, motion);

                var ripple = new Ripple();
                ripple.Emit(new SKPoint(100, 300), 0.7f);
                ripple.Draw(canvas, 0.016, motion);

                var data = new List<(long, float)>();
                for (var i = 0; i < 500; i++) data.Add((i * 10_000_000L, (i % 100) / 100f));
                new LineChart().Draw(canvas, new SKRect(200, 250, 620, 400), data, 3_000_000_000L, 0, 1);

                using var scene = new DemoScene();
                scene.Render(canvas, SKRect.Create(1280, 720), 0.016, motion);
            }
            Out("render-smoke: OK");
            return 0;
        }
        catch (Exception ex)
        {
            Out("render-smoke: NG " + ex);
            return 1;
        }
    }

    /// <summary>10 秒 (60fps ペース 5 秒 + 上限なし 5 秒) を 1280×720 に描き、FPS と CPU% を出す。</summary>
    public static int RunPhase0(string? outFile)
    {
        AttachConsole(-1);
        try
        {
            using var bmp = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bmp);
            var bounds = SKRect.Create(1280, 720);
            using var scene = new DemoScene();
            scene.Start();
            var proc = Process.GetCurrentProcess();

            (double fps, double cpuTotal) Measure(double seconds, bool paced)
            {
                var sw = Stopwatch.StartNew();
                var cpu0 = proc.TotalProcessorTime;
                var frames = 0;
                var last = 0.0;
                while (sw.Elapsed.TotalSeconds < seconds)
                {
                    var now = sw.Elapsed.TotalSeconds;
                    canvas.Clear(Theme.Background);
                    scene.Render(canvas, bounds, frames == 0 ? 1.0 / 60 : now - last, true);
                    canvas.Flush();
                    last = now;
                    frames++;
                    if (paced)
                    {
                        var next = frames / 60.0;
                        var wait = next - sw.Elapsed.TotalSeconds;
                        if (wait > 0) Thread.Sleep(TimeSpan.FromSeconds(wait));
                    }
                }
                var wall = sw.Elapsed.TotalSeconds;
                var cpu = (proc.TotalProcessorTime - cpu0).TotalSeconds;
                return (frames / wall, cpu / wall * 100);
            }

            Measure(1, true); // ウォームアップ
            var paced = Measure(5, true);
            var free = Measure(5, false);
            scene.Stop();

            var sb = new StringBuilder();
            sb.AppendLine("# Phase 0 計測結果 (オフスクリーン 1280×720, SKBitmap)");
            sb.AppendLine();
            sb.AppendLine($"- 計測日時: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"- 環境: {Environment.MachineName}, {Environment.ProcessorCount} 論理コア, {RuntimeInformation.FrameworkDescription}");
            sb.AppendLine("- 内容: Xbox 図 + 左スティック円回しトレイル (300ms) + 右スティック散布図 3000 点 + トリガーゲージ×2 + 時系列。DemoDriver 250Hz");
            sb.AppendLine("- CPU% は Process.TotalProcessorTime / 実時間 (プロセス全体、DemoDriver スレッド込み)。100% = 1 コア相当");
            sb.AppendLine();
            sb.AppendLine("| 条件 | 描画 fps | CPU% (1 コア=100) | CPU% (全コア比) |");
            sb.AppendLine("|---|---|---|---|");
            sb.AppendLine($"| 60fps ペース 5 秒 | {paced.fps:0.0} | {paced.cpuTotal:0.0} | {paced.cpuTotal / Environment.ProcessorCount:0.0} |");
            sb.AppendLine($"| 上限なし 5 秒 | {free.fps:0.0} | {free.cpuTotal:0.0} | {free.cpuTotal / Environment.ProcessorCount:0.0} |");
            sb.AppendLine();
            sb.AppendLine("注: オフスクリーン (CPU ラスタ) の値。実ウィンドウでは SKElement の WriteableBitmap 転送が加わるため、DebugPage の MeasuredFps で再確認する。");
            var text = sb.ToString();
            Out(text);
            if (outFile is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
                File.WriteAllText(outFile, text, new UTF8Encoding(false));
            }
            return 0;
        }
        catch (Exception ex)
        {
            Out("phase0: NG " + ex);
            if (outFile is not null) File.WriteAllText(outFile, "phase0: NG " + ex);
            return 1;
        }
    }
}
