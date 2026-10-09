# PadLab

Windows 向けのゲームコントローラー診断ツールです。Xbox / DualShock 4 / DualSense (Edge) / Switch Pro / 汎用パッドの入力を可視化し、ボタン反応・スティックのドリフト・チャタリング・放置監視・精度テストの結果を記録します。
単一 exe (自己完結、.NET ランタイム不要) で動作し、**既定で一切ネットワーク通信を行いません**。
表示・記録される値は**参考値であり、故障の確定診断ではありません**。

## 主な機能

- デバイス: 接続中のコントローラーの一覧と選択
- 基本入力: ボタン・十字キー・スティック・トリガーの表示、ポーリングレート (Hz)
- ドリフト: 触っていないときのスティックのずれを測り、デッドゾーンの推奨値を出す
- パドル: 背面パドルの割り当て確認 (パドルのある機種のみ)
- 振動: 左右モーター・インパルストリガーの振動テスト
- 放置監視: 放置中に起きた入力・切断・ポーリングの途切れを記録する
- 精度テスト: サークル (外周を回したときの角度ごとの到達半径)・スナップバック・応答カーブ・分解能・チャタリング
- 履歴・レポート: 診断の記録を JSON で保存し、1 枚の PNG レポートにまとめる
- デモ用の仮想コントローラー: 実機が無くてもキーボード (WASD / 矢印 / JKLI / QE) で全画面を試せる

対応機種のプロファイル: Xbox One / Series、Xbox Elite Series 2、DualShock 4、DualSense、DualSense Edge、Switch Pro コントローラー、汎用 (DirectInput)。プロファイルは exe の隣の `profiles/` に JSON を置くと上書きできます (形式は `docs/profiles.md`)。

## 開発状況

- 開発中 (v0.1.0)。上の画面はすべて実装済みですが、**コントローラーの実機ではまだ確認していません**。動作確認はデモ用の仮想コントローラーでしか行っていません
- 特に HID レポートの読み取り位置 (DualShock 4 / DualSense / Switch Pro の USB と Bluetooth)、Xbox の Share ボタンとパドル、汎用パッドのトリガー軸の向き、チャタリングの判定値は資料からの推定で書いてあり、実機では違う可能性があります。確認項目は `docs/hardware-checklist.md` にまとめています (コード中の `[要検証]` と対応)
- 未実装: センサー (ジャイロ・加速度) の画面、レイテンシの参考計測、複数台の比較。設定の「外部データの更新」は項目だけで、通信機能はありません

## 必要環境

- Windows 10 1903 (19041) 以降 / x64
- ビルドする場合: .NET SDK 10.0 (10.0.400 で確認)、Windows 10 SDK 19041

## ビルド / テスト / publish

リポジトリの根で実行します。

```powershell
dotnet build PadLab.slnx
dotnet test  PadLab.slnx
```

配布用の単体 exe は `build.ps1` で作ります (PowerShell 5.1 / 7 どちらでも可)。テスト → publish → `dist\PadLab.exe --selftest` まで通して実行します。

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

出力: `dist\PadLab.exe` (約 74 MB。自己完結・win-x64・PublishSingleFile・圧縮あり)。初回起動時にネイティブ DLL を `%TEMP%\.net` に展開するため、初回だけ起動が数秒遅くなります。

publish だけを手で行う場合:

```powershell
dotnet publish src\PadLab.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:EnableCompressionInSingleFile=true -o dist
```

## 起動オプション

| オプション | 動作 |
|---|---|
| (なし) | 通常起動 (GUI) |
| `--selftest` | SkiaSharp / HidSharp / XInput / Windows.Gaming.Input / DirectInput / プロファイル 8 件 / セッション保存先の書込 / デバイス列挙 を順に確認。結果を stdout と `%LOCALAPPDATA%\PadLab\selftest.txt` に出力。全部 OK なら終了コード 0、失敗があれば 1 |
| `--render-smoke` | 描画部品を 640×480 のオフスクリーンに 1 フレーム描いて例外なく終わることを確認。stdout に `render-smoke: OK`、終了コード 0/1 |
| `--phase0 [出力ファイル]` | オフスクリーン 1280×720 で 10 秒描画し FPS と CPU% を計測。省略時は stdout のみ |

WPF アプリのため、コンソールから実行しても出力がプロンプトと混ざることがあります。終了コードか `selftest.txt` で判定してください。

## データの保存先

- 設定: `%LOCALAPPDATA%\PadLab\settings.json`
- 診断記録: `%LOCALAPPDATA%\PadLab\sessions\*.json` (形式は `docs/result-format.md`)
- exe と同じフォルダに `portable.txt` を置くと、上記を exe の隣に保存します

## ライセンス

MIT License (`LICENSE`)。使用ライブラリのライセンスは `THIRD-PARTY-NOTICES.md` を参照してください。

Xbox・DualShock・DualSense・Nintendo Switch は各社の商標です。PadLab は非公式のツールで、各社とは関係ありません。
