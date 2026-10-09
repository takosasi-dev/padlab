# 実機検証チェックリスト (Input バックエンド)

開発機にコントローラー未接続のまま実装したため、以下は実機で確認して結果欄を埋める。
コード中の `[要検証]` コメントと対応する。結果は ✓ / ✗ / 未 で記入し、✗ の場合は備考に症状を書く。

## 共通

| 項目 | 確認方法 | 結果 | 備考 |
|---|---|---|---|
| 接続で 1 秒以内に一覧へ追加される | デバイス画面を開いたまま接続 | 未 | `WindowsDeviceWatcher.Scan` |
| 切断で一覧から消える (Removed が来る) | 接続中に抜く | 未 | |
| 同じ物理機が 2 つ以上に見えない | 接続後の一覧件数 | 未 | WGI + HID + DI の重複 |
| ポーリングレートが期待値付近 (USB 250Hz / BT 数十〜数百Hz) | 基本入力画面の Hz 表示 | 未 | |
| Faulted 後にアプリが落ちない | 動作中に抜く | 未 | `PollingLoop.Faulted` |

## Xbox One / Series (WGI + XInput)

| 項目 | USB | BT | 備考 |
|---|---|---|---|
| A/B/X/Y/LB/RB/十字/L3/R3/Menu/View が点く | 未 | 未 | |
| Guide (Xbox ボタン) が点く | 未 | 未 | `XInputGetStateEx` ordinal 100。XInput index と WGI の並び順の対応 [要検証] |
| Share ボタン (Series 型) が Share として点く | 未 | 未 | `RawGameController` の 15 番目以降を Share とみなす [要検証]。違う index なら `XboxSource.WgiStandardButtonCount` を調整 |
| 左右スティック Y 軸が上で + | 未 | 未 | |
| トリガー 0..1 | 未 | 未 | |
| 左右モーター振動 | 未 | 未 | `Gamepad.Vibration` |
| インパルストリガー振動 | 未 | 未 | BT では効かない機種あり |
| バッテリー % 表示 | 未 | 未 | `TryGetBatteryReport` (USB 給電中は null になりうる) |

## Xbox Elite Series 2

| 項目 | USB | BT | 備考 |
|---|---|---|---|
| 上記 Xbox 項目すべて | 未 | 未 | |
| パドル P1〜P4 が物理ビットとして見える | 未 | 未 | WGI `GamepadButtons.Paddle1..4` [要検証]。見えない場合は割り当て先ボタンだけ点く |

## DualShock 4 (生 HID)

| 項目 | USB (0x01) | BT (0x11) | 備考 |
|---|---|---|---|
| フルレポートに切り替わる | ― | 未 | BT はフィーチャー 0x02 の GetFeature 後 [要検証] |
| ×○□△ → A/B/X/Y | 未 | 未 | offset 5 bit4..7 |
| L1/R1/Share/Options/L3/R3 | 未 | 未 | offset 6 |
| PS / タッチパッドクリック | 未 | 未 | offset 7 bit0/1 |
| ハット → 十字 | 未 | 未 | offset 5 下位 4bit |
| L2/R2 アナログ 0..1 | 未 | 未 | offset 8/9 |
| スティック Y 軸が上で + | 未 | 未 | |
| ジャイロ / 加速度が動く | 未 | 未 | offset 13〜24 |
| タッチ座標 0..1 | 未 | 未 | offset 35〜。分解能 1920×942 |
| バッテリー | 未 | 未 | offset 30 下位 4bit |
| 振動 (左=大 / 右=小) | 未 | 未 | USB 0x05 / BT 0x11 + CRC32 |
| LED が水色になる | 未 | 未 | |
| BT 判定 (接続方式表示) | ― | 未 | DevicePath の "bth" [要検証] |

## DualSense / DualSense Edge (生 HID)

| 項目 | USB (0x01) | BT (0x31) | 備考 |
|---|---|---|---|
| フルレポートに切り替わる | ― | 未 | BT はフィーチャー 0x05 の GetFeature 後 [要検証] |
| ×○□△ / L1 R1 / Create Options / L3 R3 | 未 | 未 | offset 8/9 |
| PS / タッチパッド / ミュート | 未 | 未 | offset 10 bit0..2 |
| Edge: Fn1/Fn2 が Fn として点く | 未 | 未 | offset 10 bit4/5 [要検証] |
| Edge: 背面ボタンが P1/P2 として点く | 未 | 未 | offset 10 bit6/7 [要検証]。プロファイル割り当て先ボタンも同時に点くか |
| L2/R2 アナログ | 未 | 未 | offset 5/6 |
| ジャイロ / 加速度 | 未 | 未 | offset 16〜27 |
| タッチ座標 | 未 | 未 | offset 33〜。1920×1080 |
| バッテリー | 未 | 未 | offset 53 下位 4bit × 10% |
| 振動 (互換モード) | 未 | 未 | flag0 = 0x03 |
| LED 水色 | 未 | 未 | flag1 = 0x04 |

## Switch Pro Controller (生 HID)

| 項目 | USB | BT | 備考 |
|---|---|---|---|
| ハンドシェイク後に 0x30 が来る | 未 | 未 | USB は 0x80 0x02 / 0x80 0x04 → サブコマンド 0x03 0x30 |
| B/A/Y/X → A/B/X/Y (南=A) | 未 | 未 | offset 3 |
| −/+/Home/キャプチャ/L3/R3 | 未 | 未 | offset 4 |
| 十字 / L / ZL | 未 | 未 | offset 5 |
| ZR | 未 | 未 | offset 3 bit7 |
| スティック 12bit、Y 上が + (反転なしで正しいか) | 未 | 未 | `SwitchProReport.Stick12` [要検証] |
| スティック到達範囲がほぼ 1.0 (÷1700 の妥当性) | 未 | 未 | 足りなければキャリブレーション読み出しへ |
| IMU (加速度・ジャイロ) | 未 | 未 | offset 13〜 |
| バッテリー | 未 | 未 | offset 2 上位 4bit |
| 振動 (HD 振動の符号化) | 未 | 未 | `SwitchProReport.EncodeRumble` [要検証] |
| プレイヤー LED 1 が点く | 未 | 未 | サブコマンド 0x30 |

## 汎用パッド (DirectInput)

| 項目 | 結果 | 備考 |
|---|---|---|
| Vortice で Acquire できる (HWND あり / なし) | 未 | `DirectInputSource.WindowHandle` |
| ボタン 1..N が Generic1.. として点く | 未 | |
| 32 個超のボタンが「未登録 #n」で出る | 未 | |
| X/Y → 左スティック (Y 反転) | 未 | |
| RX/RY → 右スティック | 未 | |
| Z/RZ → 左右トリガー | 未 | |
| 合成 Z 軸機種で左右に振り分く (向きが逆でないか) | 未 | `SplitCombinedTrigger` [要検証] |
| POV → 十字 (斜め含む) | 未 | |
| 軸レンジ 0..65535 に設定できている | 未 | できない機種は中心ずれとして見える |
| 振動が「未対応」表示 | 未 | ForceFeedback 非対応 |
