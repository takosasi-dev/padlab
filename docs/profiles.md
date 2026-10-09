# 機種プロファイル JSON

`PadLab.Core` に同梱される `src/PadLab.Core/Profiles/data/*.json` (EmbeddedResource) が既定のプロファイルです。
exe と同じフォルダに `profiles/` を作り、同名の JSON を置くとそちらが優先されます (`ProfileStore(overrideDirectory)`)。
機種単位で丸ごと置き換える方式で、部分的なマージは行いません。

## ファイル名と DeviceKind

| DeviceKind | ファイル名 | 備考 |
|---|---|---|
| XboxOne | `xbox_one.json` | Xbox One / Series 系 |
| XboxElite2 | `xbox_elite2.json` | paddleCount 4 |
| DualShock4 | `dualshock4.json` | |
| DualSense | `dualsense.json` | |
| DualSenseEdge | `dualsense_edge.json` | Fn1/Fn2、paddleCount 2 |
| SwitchPro | `switch_pro.json` | |
| Generic | `generic.json` | DirectInput ボタン 1〜16 と POV |
| Fake | `fake.json` | デモ用仮想コントローラー (Xbox 配置) |

## 形式

```json
{
  "kind": "DualSense",
  "name": "DualSense",
  "version": "1",
  "paddleCount": 0,
  "expectedCapabilities": ["Rumble", "Gyro", "Accel", "Touchpad", "Battery", "Led"],
  "buttons": [
    { "id": "A", "label": "×", "category": "Standard" },
    { "id": "Mute", "label": "ミュート", "category": "Extended" }
  ]
}
```

| キー | 型 | 意味 |
|---|---|---|
| `kind` | `DeviceKind` 名 | ファイル名と一致させる |
| `name` | 文字列 | 画面表示用の機種名 |
| `version` | 文字列 | プロファイルの版。セッション JSON の `device.profile` に `name/version` で記録される |
| `paddleCount` | 整数 | 背面パドルの数。0 ならパドル確認画面を出さない |
| `expectedCapabilities` | `Capabilities` 名の配列 | その機種で期待される機能。実際に取れなかった項目は「未確認」表示になる |
| `buttons[].id` | `ButtonId` 名 | 正規化後のボタン識別子 (南=A 規約。PS の × は `A`、Switch の B は `A`) |
| `buttons[].label` | 文字列 | 画面に出す表記 |
| `buttons[].category` | `Standard` / `Extended` / `Paddle` | 分類A/B/C |

`buttons` に無い `ButtonId` や生ボタン番号 (`ExtraButtons`) が入力に現れた場合は、`ButtonCoverage` が「未登録ボタン #n」として末尾に自動追加します。
プロファイルに載っていないからといって入力が捨てられることはありません。

## VID/PID → DeviceKind

`ProfileStore.KindFromVidPid` の対応表。表に無い組み合わせはすべて `Generic` です。

| VID | PID | DeviceKind |
|---|---|---|
| 045E (Microsoft) | 02D1, 02DD, 02E3, 02EA, 02FD, 0B12, 0B13, 0B20, 0B22 | XboxOne |
| 045E | 0B00 (USB), 0B05 (BT) | XboxElite2 |
| 054C (Sony) | 05C4, 09CC | DualShock4 |
| 054C | 0CE6 | DualSense |
| 054C | 0DF2 | DualSenseEdge |
| 057E (Nintendo) | 2009 | SwitchPro |

`ProfileStore.VendorName` は 045E→Microsoft、054C→Sony、057E→Nintendo、2DC8→8BitDo、それ以外は `VID 0x____` を返します。

## 上書きの用途

- 新しい PID の機種を既存プロファイルで扱いたい場合 (VID/PID 表はコード側なので、`Generic` として認識された機種には `generic.json` の上書きが効く)
- ラベルを自分の好みに変えたい場合 (例: × を「バツ」)
- 検証中に `expectedCapabilities` を減らして「未確認」表示を抑えたい場合

JSON が壊れていると該当機種の読み込み時に例外になります。`--selftest` で 8 機種の読み込みを確認できます。
