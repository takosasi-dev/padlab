# Phase 0 計測結果 (オフスクリーン 1280×720, SKBitmap)

- 計測日時: 2026-09-03 17:43
- 環境: たこさしPC, 16 論理コア, .NET 10.0.11
- 内容: Xbox 図 + 左スティック円回しトレイル (300ms) + 右スティック散布図 3000 点 + トリガーゲージ×2 + 時系列。DemoDriver 250Hz
- CPU% は Process.TotalProcessorTime / 実時間 (プロセス全体、DemoDriver スレッド込み)。100% = 1 コア相当

| 条件 | 描画 fps | CPU% (1 コア=100) | CPU% (全コア比) |
|---|---|---|---|
| 60fps ペース 5 秒 | 60.0 | 31.9 | 2.0 |
| 上限なし 5 秒 | 209.7 | 99.4 | 6.2 |

注: オフスクリーン (CPU ラスタ) の値。実ウィンドウでは SKElement の WriteableBitmap 転送が加わるため、DebugPage の MeasuredFps で再確認する。

## 判断

- 目標「60fps 維持・CPU は 1 コアの数十%以下」はオフスクリーン計測では達成 (60.0fps / 約 32%。この値には DemoDriver 250Hz スレッドの Sleep(1) ループ分も含む)。
- 描画のみの上限は約 210fps (1 コア飽和) なので、1 フレームあたり約 4.8ms。描画簡略化や WinUI 3 再検討は現時点では不要と見ている。
- 実ウィンドウ (SKElement → WriteableBitmap 転送) での値は未確認。`計測 (Phase 0)` ナビ項目 (DebugPage) を開くと MeasuredFps が画面上部に出るので、目視で確認できる。
- 再計測: `PadLab.exe --phase0 docs\phase0-result.md` (この表の部分を上書きする)。
