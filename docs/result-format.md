# 結果 JSON 形式 (DiagnosticSession)

`SessionStore.Save` が書き出すセッション JSON の仕様です。C# の DTO は `src/PadLab.Core/Recording/DiagnosticSession.cs`。
`System.Text.Json` で `snake_case`、インデント付き、日本語はエスケープせずそのまま (`UnsafeRelaxedJsonEscaping`) で保存します。

## 保存先とファイル名

- 保存先: `%LOCALAPPDATA%\PadLab\sessions\`。exe と同じフォルダに `portable.txt` があれば `exe隣\sessions\` (`SessionStore.DefaultDirectory`)
- ファイル名: `SessionStore.MakeFileName` の 1 箇所だけが生成する
  - `yyyyMMdd-HHmmss_{DeviceKind}_{ConnectionType}_{sessionId先頭8文字}.json`
  - 例: `20260903-171500_DualSense_Bluetooth_ab12cd34.json`
  - sessionId は英数字以外を除去してから先頭 8 文字。空になったら `session`
  - メモや自由記述はファイル名に一切使わない (パス注入防止)
- `device.type` が `DeviceKind` 名として読めない場合は `Generic`、`device.connection` が読めない場合は `Unknown` としてファイル名を作る (中身はそのまま)

## トップレベル

```json
{
  "session_id": "ab12cd34ef",
  "measured_at": "2026-09-03T17:15:00+09:00",
  "app_version": "0.1.0",
  "device": { ... },
  "results": { ... },
  "button_aliases": { "A": "決定" },
  "notes": "自由記述"
}
```

| キー | 型 | 意味 |
|---|---|---|
| `session_id` | 文字列 | セッション識別子 (GUID 等) |
| `measured_at` | ISO 8601 (オフセット付き) | 測定日時 |
| `app_version` | 文字列 | PadLab のバージョン |
| `button_aliases` | 辞書 | ユーザーが付けたボタン別名 (`ButtonId` 名 → 表示名)。表示専用でファイル名には使わない |
| `notes` | 文字列 | 自由記述 |

## device

| キー | 型 | 意味 |
|---|---|---|
| `type` | 文字列 | `DeviceKind` 名 (`XboxOne`, `DualSense`, ...) |
| `connection` | 文字列 | `ConnectionType` 名 (`Usb`, `Bluetooth`, `Dongle`, `Unknown`) |
| `raw_identifiers` | 文字列 | VID/PID やバックエンド名など識別に使った生情報 |
| `profile` | 文字列 | 使用したプロファイル `name/version` |
| `serial` | 文字列 or null | 取得できた場合のみ |

## results

測定していない項目は代用値を入れず `null` にします (配列項目は空配列)。

### buttons[]

| キー | 型 | 意味 |
|---|---|---|
| `id` | 文字列 | `ButtonId` 名。未登録ボタンは `Generic{n}` |
| `label` | 文字列 | プロファイルの表記 |
| `category` | 文字列 | `Standard` / `Extended` / `Paddle` |
| `pressed` | bool | セッション中に一度でも反応したか |

### paddles[] (パドル対応機種のみ、それ以外は null)

| キー | 型 | 意味 |
|---|---|---|
| `paddle` | 整数 | 1 始まり |
| `mapped_to` | 文字列配列 | 反応した `ButtonId` 名。空なら「反応なし」 |
| `physical_seen` | bool | 生 HID で P1〜P4 の物理ビットが見えたか |

### sticks[]

| キー | 型 | 意味 |
|---|---|---|
| `stick` | 文字列 | `Left` / `Right` |
| `center_x`, `center_y` | float | キャリブレーションで得た基準点 |
| `max_drift`, `mean_drift` | float | 基準点からの最大距離 / 平均距離 |
| `dominant_angle` | float | 最大距離を記録した方向 (度、+X が 0、上が 90) |
| `verdict` | 文字列 | `None` / `Minor` / `Notable` (参考判定) |
| `max_radius` | float | 到達半径の最大 |
| `roundness`, `asymmetry_x`, `asymmetry_y` | float or null | サークルテスト |
| `mean_return_ms`, `max_overshoot` | 数値 or null | スナップバックテスト |
| `jump_count` | 整数 or null | 応答カーブテスト |
| `min_step`, `noise_level` | float or null | 分解能テスト |

### triggers[]

`trigger` (`Left` / `Right`) と `max_reached` (0..1)。

### vibration[]

| キー | 型 | 意味 |
|---|---|---|
| `motor` | 文字列 | `LeftMotor` / `RightMotor` / `LeftTrigger` / `RightTrigger` |
| `tested` | bool | 実施したか |
| `user_confirmed_felt` | bool or null | 自己申告。未回答は null |

### sensors

Phase 3 まで常に `null`。

### latency_reference

`hz`、`jitter_ms`、`is_reference_only` (常に `true`)。ポーリング周期から推定した参考値で、入力遅延そのものではありません。

### chatter[]

`button`、`presses`、`suspects`、`detection_limit_ms` (この値未満の間隔はポーリング周期の制約で検出できない)。

### idle (未実施なら null)

`duration_seconds`、`drift_events`、`button_events`、`trigger_events`、`gap_events`、`disconnect_events`。

### deadzone_recommended_percent / deadzone_rationale

推奨デッドゾーン (%) と根拠文。未算出なら null。

## 例

```json
{
  "session_id": "ab12cd34ef",
  "measured_at": "2026-09-03T17:15:00+09:00",
  "app_version": "0.1.0",
  "device": {
    "type": "DualSense",
    "connection": "Bluetooth",
    "raw_identifiers": "VID 0x054C PID 0x0CE6",
    "profile": "dualsense/1",
    "serial": null
  },
  "results": {
    "buttons": [ { "id": "A", "label": "×", "category": "Standard", "pressed": true } ],
    "paddles": null,
    "sticks": [
      { "stick": "Left", "center_x": 0.01, "center_y": 0.01, "max_drift": 0.06, "mean_drift": 0.03, "dominant_angle": 12,
        "verdict": "Minor", "max_radius": 0.98, "roundness": 0.96, "asymmetry_x": 0.01, "asymmetry_y": -0.02,
        "mean_return_ms": 8.5, "max_overshoot": 0.05, "jump_count": 0, "min_step": 0.001, "noise_level": 0.004 }
    ],
    "triggers": [ { "trigger": "Left", "max_reached": 1 } ],
    "vibration": [ { "motor": "LeftMotor", "tested": true, "user_confirmed_felt": true } ],
    "sensors": null,
    "latency_reference": { "hz": 250.3, "jitter_ms": 0.4, "is_reference_only": true },
    "chatter": [ { "button": "A", "presses": 50, "suspects": 0, "detection_limit_ms": 8 } ],
    "idle": { "duration_seconds": 600, "drift_events": 1, "button_events": 0, "trigger_events": 0, "gap_events": 0, "disconnect_events": 0 },
    "deadzone_recommended_percent": 9,
    "deadzone_rationale": "最大ズレ 0.06 + ノイズ 0.004×2 + 余裕 0.02 = 0.09 → 9%"
  },
  "button_aliases": { "A": "決定" },
  "notes": ""
}
```

## 他の出力

- `SessionStore.ExportText`: 同じ内容のプレーンテキスト。末尾に「本レポートの値は参考値であり、故障の確定診断ではありません。」
- `ReportCard.RenderPng`: PNG 1 枚 (幅 1200px 既定、高さは内容で決まる)。同じ注記を必ず含む

値はすべて参考値であり、故障の確定診断ではありません。
