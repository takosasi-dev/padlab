namespace PadLab.App;

/// <summary>UI 文言はすべてここに置く。断定表現 (故障・壊れている) は使わない。</summary>
public static class Strings
{
    public const string AppTitle = "PadLab";

    // ナビゲーション
    public const string NavDevice = "デバイス";
    public const string NavBasic = "基本入力";
    public const string NavDrift = "ドリフト";
    public const string NavPaddle = "パドル";
    public const string NavVibration = "振動";
    public const string NavIdle = "放置監視";
    public const string NavPrecision = "精度テスト";
    public const string NavHistory = "履歴・レポート";
    public const string NavSettings = "設定";
    public const string NavAbout = "PadLab について";
    public const string NavDebug = "計測 (Phase 0)";

    // ヘッダー
    public const string NoDevice = "デバイス未選択";
    public const string PollingFormat = "{0,5:0} Hz";
    public const string PollingUnknown = "―― Hz";
    public const string ConnectionUsb = "USB";
    public const string ConnectionBluetooth = "Bluetooth";
    public const string ConnectionDongle = "ドングル";
    public const string ConnectionUnknown = "接続方式 未確認";

    // 共通
    public const string Unconfirmed = "未確認";
    public const string Unsupported = "未対応";
    public const string PlaceholderPage = "この画面は準備中です。";
    public const string ReferenceOnly = "測定値は参考値であり、故障の確定診断ではありません。";
    public const string NoNetwork = "PadLab は既定で一切ネットワーク通信を行いません。";

    // 判定
    public const string VerdictNone = "● 基準内";
    public const string VerdictMinor = "▲ 軽微なズレが見られます";
    public const string VerdictNotable = "■ 基準より大きなズレが見られます";
    public const string IdleNoEvents = "監視中に検出された入力はありません";

    // 描画部品
    public const string UnregisteredButtonFormat = "未登録 #{0}";
    public const string LeftStick = "左スティック";
    public const string RightStick = "右スティック";
    public const string LeftTrigger = "LT";
    public const string RightTrigger = "RT";
    public const string MaxMark = "最大";

    // Phase 0 計測
    public const string FpsFormat = "描画 {0,5:0.0} fps";
    public const string Phase0Hint = "Xbox 図 + 円回しトレイル + 散布図 3000 点 を描画中。WASD / 矢印 / JKLI / QE で上書きできます。";

    // 設定
    public const string SettingReduceMotion = "アニメーションを減らす";
    public const string SettingLightTheme = "ライトテーマ";
    public const string SettingShowPolling = "ポーリングレートを表示";
    public const string SettingShowFake = "デモ用仮想コントローラーを表示";
    public const string SettingFakeKind = "デモ用仮想コントローラーの機種 (パドル画面など機種別の表示を確認する用)";
    public const string SettingSound = "効果音 (既定 OFF)";
    public const string SettingAllowNetwork = "外部データの更新 (既定 OFF。今バージョンでは通信機能自体を実装していません)";

    // デバイス画面
    public const string DeviceListTitle = "接続中のコントローラー";
    public const string DeviceListEmpty = "接続中のコントローラーは見つかっていません。USB または Bluetooth で接続すると一覧に表示されます。";
    public const string DeviceSelected = "選択中";
    public const string DeviceSelect = "このコントローラーを診断する";
    public const string PollingShortFormat = "{0,4:0} Hz";

    // 基本入力画面
    public const string ChecklistTitle = "ボタン確認リスト";
    public const string ChecklistDone = "✓";
    public const string ChecklistPending = "－";
    public const string ChecklistProgressFormat = "押済み {0,2} / {1,2}";
    public const string AllButtonsDone = "● 全ボタン確認完了";
    public const string MaxReachTitle = "最大到達値";
    public const string StickMaxFormat = "{0} 半径 {1,5:0.000}";
    public const string TriggerMaxFormat = "{0} {1,5:0.000}";
    public const string CategoryStandard = "標準";
    public const string CategoryExtended = "拡張";
    public const string CategoryPaddle = "パドル";
    public const string BasicHint = "すべてのボタンを一度ずつ押し、両スティックを一周させ、トリガーを押し切ってください。";

    // ドリフト画面
    public const string DriftGuide = "スティックに触れないでください。「測定開始」を押すと 3 秒間ニュートラル位置を記録し、そのあと基準点からのズレを監視します。";
    public const string DriftStart = "測定開始";
    public const string DriftRestart = "測定をやり直す";
    public const string DriftSave = "この記録を保存";
    public const string DriftCalibratingFormat = "基準点を記録中… あと {0:0} 秒。スティックに触れないでください。";
    public const string DriftMonitoringFormat = "監視中 {0,6:0} 秒";
    public const string DriftMaxFormat = "最大距離 {0,6:0.000}";
    public const string DriftMeanFormat = "平均距離 {0,6:0.000}";
    public const string DriftAngleFormat = "方向     {0,6:0}°";
    public const string DriftExceedFormat = "超過回数 {0,6}";
    public const string DriftCenterFormat = "基準点  ({0,6:0.000}, {1,6:0.000})";
    public const string DriftThresholdFormat = "判定基準: ▲ {0:0.000} 以上 / ■ {1:0.000} 以上";
    public const string CompareTitle = "過去記録と比較";
    public const string CompareNone = "保存済みの記録はありません。";
    public const string ComparePick = "比較する記録を選択";
    public const string CompareRowFormat = "{0}  最大 {1,6:0.000}  {2}";
    public const string CompareNow = "今回";
    public const string DeadzoneTitle = "デッドゾーン推奨値";
    public const string DeadzoneFormat = "{0,3} % 程度";
    public const string DeadzoneTradeoff = "デッドゾーンを広げるほど誤入力は減りますが、微小な入力も効かなくなります。適用先はゲーム側や Steam Input などの設定で、PadLab 自身は何も書き込みません。";
    public const string DeadzoneNeedMonitoring = "監視データがまだありません。測定開始後に表示されます。";
    public const string DeadzoneIdleUsed = "放置監視の最大ズレも参照しています。";

    // 放置監視画面
    public const string IdleGuide = "コントローラーに触れずに、そのまま置いておいてください。監視中に到着したすべての入力を記録します。PadLab を背面にしても監視は続きます。";
    public const string IdleMinutesLabel = "監視時間 (分)";
    public const string IdleStart = "監視開始";
    public const string IdleStop = "今すぐ終了";
    public const string IdleAgain = "もう一度監視する";
    public const string IdleWaiting = "監視中";
    public const string IdleElapsedFormat = "{0:00}:{1:00} / {2:00}:00";
    public const string IdleEventCountFormat = "検出イベント {0,4} 件";
    public const string IdleResultTitle = "監視結果";
    public const string IdleSummaryFormat = "この {0:0} 分間で、左スティックが基準から最大 {1:0.00}、右スティックが最大 {2:0.00} ずれました。スティックのズレ {3} 回、ボタン入力 {4} 回、トリガー {5} 回、ポーリング空き {6} 回、切断・再接続 {7} 回。";
    public const string IdleEventsTitle = "イベント一覧";
    public const string IdleColTime = "時刻";
    public const string IdleColKind = "種類";
    public const string IdleColDetail = "詳細";
    public const string IdleKindStick = "スティック";
    public const string IdleKindButton = "ボタン";
    public const string IdleKindTrigger = "トリガー";
    public const string IdleKindGap = "ポーリング空き";
    public const string IdleKindDisconnected = "切断";
    public const string IdleKindReconnected = "再接続";
    public const string IdleDisconnectedStopped = "監視対象のコントローラーが切断されたため、監視を終了しました。";

    // ---- D3 ----
    // 共通
    public const string NoDeviceHint = "デバイス画面でコントローラーを選択してください。";

    // 振動画面
    public const string VibHint = "各行の ON / スライダー / スイープで振動を送り、手に伝わったかを自己申告してください。判定は体感が前提で、PadLab 側では自動判定しません。";
    public const string VibRowLeftMotor = "左モーター (低周波)";
    public const string VibRowRightMotor = "右モーター (高周波)";
    public const string VibRowLeftTrigger = "左トリガー (インパルス)";
    public const string VibRowRightTrigger = "右トリガー (インパルス)";
    public const string VibUnsupported = "この機種/接続では未対応";
    public const string VibSweep = "スイープ (0→100% 5 秒)";
    public const string VibFelt = "感じた";
    public const string VibNotFelt = "感じなかった";
    public const string VibReportFelt = "申告: 体感あり";
    public const string VibReportNotFelt = "申告: 体感なし";
    public const string VibReportNone = "申告: 未回答";
    public const string VibStrengthFormat = "{0,3:0} %";
    public const string VibCommandFormat = "送信中の指示  左 {0,3:0}%  右 {1,3:0}%  LT {2,3:0}%  RT {3,3:0}%";
    public const string VibStopAll = "すべて停止";

    // パドル画面
    public const string PaddleIntro = "背面パドルが、どの標準ボタンとして送られてくるかを 1 つずつ確認します。";
    public const string PaddleUnavailable = "この機種のプロファイルには背面パドルが登録されていません。";
    public const string PaddleGuideFormat = "背面パドル P{0} だけを押してください (他のボタンには触れないでください)";
    public const string PaddleWaitingFormat = "待機中 {0,4:0} 秒 / 10 秒";
    public const string PaddleBegin = "確認を開始";
    public const string PaddleNext = "次へ";
    public const string PaddleRetry = "やり直す";
    public const string PaddleCapturedFormat = "P{0} → {1} に割り当て中";
    public const string PaddleNoResponse = "反応なし。パドル自体の不調とプロファイルで未割り当ての区別は PadLab からはできません。メーカー公式アプリで割り当てをご確認ください。";
    public const string PaddleInvalid = "複数の入力が重なりました。もう一度お試しください。";
    public const string PaddleDone = "すべてのパドルを確認しました。";
    public const string PaddleListTitle = "割り当て一覧";
    public const string PaddleRowFormat = "P{0} → {1}";
    public const string PaddleNone = "反応なし";
    public const string PaddlePhysical = " (物理ビット検出)";
    public const string PaddleDiffTitle = "前回記録との差分";
    public const string PaddleDiffFormat = "前回 P{0}→{1}、今回 P{0}→{2}。設定変更の可能性があります。";
    public const string PaddleDiffNone = "前回記録と同じ割り当てです。";
    public const string PaddleNoPrevious = "比較できる過去記録はありません。";

    // 精度テスト画面
    public const string PrecStepStick = "スティック選択";
    public const string PrecStepCircle = "サークル";
    public const string PrecStepSnapback = "スナップバック";
    public const string PrecStepLinearity = "応答カーブ";
    public const string PrecStepResolution = "分解能";
    public const string PrecStepChatter = "チャタリング";
    public const string PrecStepResults = "結果一覧";
    public const string PrecStepFormat = "ステップ {0} / {1}: {2}";
    public const string PrecGuideStick = "精度テストを行うスティックを選んでください。";
    public const string PrecGuideCircle = "スティックを外周に沿ってゆっくり 1 周回してください。";
    public const string PrecGuideSnapback = "スティックをいっぱいに倒して手を離してください。5 回繰り返します。";
    public const string PrecGuideLinearity = "中央からゆっくり真横に倒してください。倒し切ると次へ進みます。";
    public const string PrecGuideResolution = "できるだけ小さくスティックを動かし続けてください (5 秒)。";
    public const string PrecGuideChatterFormat = "{0} ボタンを {1} 回、普通に押してください。";
    public const string PrecGuideResults = "各テストの数値です。前回セッションがあれば横に並べています。";
    public const string PrecRemainingFormat = "あと {0} 回";
    public const string PrecElapsedFormat = "あと {0:0} 秒";
    public const string PrecElapsedLabel = "経過";
    public const string PrecStart = "開始";
    public const string PrecNext = "次へ";
    public const string PrecBack = "戻る";
    public const string PrecSkip = "スキップ";
    public const string PrecRedo = "やり直す";
    public const string PrecSave = "セッションを保存";
    public const string PrecSavedFormat = "保存しました: {0}";
    public const string PrecComplete = "● 完了";
    public const string PrecRunning = "測定中…";
    public const string PrecIdle = "「開始」を押すと測定を始めます。";
    public const string PrecNotDone = "未実施";
    public const string PrecColItem = "項目";
    public const string PrecColNow = "今回";
    public const string PrecColPrev = "前回";
    public const string PrecChatterLimitFormat = "この接続では {0:0.0} ms 未満のチャタリングは検出できません";
    public const string PrecChatterSuspectsFormat = "疑い {0} 件 / 押下 {1} 回";
    public const string PrecNotesLabel = "メモ (任意。ファイル名には使われません)";
    public const string PrecRoundness = "真円度";
    public const string PrecMaxRadius = "最大半径";
    public const string PrecAsymX = "左右差";
    public const string PrecAsymY = "上下差";
    public const string PrecReturnMs = "戻り時間 (平均)";
    public const string PrecMaxReturnMs = "戻り時間 (最大)";
    public const string PrecOvershoot = "オーバーシュート";
    public const string PrecJumps = "飛び";
    public const string PrecReversals = "戻り";
    public const string PrecMaxJump = "最大の飛び";
    public const string PrecMinStep = "最小ステップ";
    public const string PrecNoise = "ノイズ";
    public const string PrecLevels = "観測レベル数";
    public const string PrecChatterSuspects = "チャタリング疑い";

    // 履歴画面
    public const string HistEmpty = "保存済みのセッションはありません。ドリフト画面の「この記録を保存」か精度テスト画面の「セッションを保存」で作成できます。";
    public const string HistRefresh = "一覧を更新";
    public const string HistExportJson = "JSON をエクスポート";
    public const string HistExportText = "テキストをエクスポート";
    public const string HistReportPng = "レポートカード (PNG) を保存";
    public const string HistScreenshot = "スクリーンショット";
    public const string HistSavedFormat = "保存しました: {0}";
    public const string HistFailedFormat = "保存できませんでした: {0}";
    public const string HistTabSummary = "要約";
    public const string HistTabJson = "JSON";
    public const string HistRowFormat = "{0:yyyy-MM-dd HH:mm}  {1}  {2}";
    public const string HistSelectHint = "左の一覧からセッションを選んでください。";
    public const string HistFolderFormat = "保存先: {0}";

    // 設定画面
    public const string SettingsDisplay = "表示";
    public const string SettingsThresholds = "判定の閾値";
    public const string SettingsOther = "その他";
    public const string SettingsDriftMinor = "ドリフト ▲ 軽微なズレ (距離)";
    public const string SettingsDriftNotable = "ドリフト ■ 大きなズレ (距離)";
    public const string SettingsChatterGap = "チャタリング疑いとする間隔 (ms 未満)";
    public const string SettingsIdleMinutes = "放置監視の既定時間 (分)";
    public const string SettingsRestartNote = "次回起動時に反映されます。";
    public const string SettingsAppliedNote = "変更は保存され、選択中のコントローラーにすぐ反映されます。";
    public const string SettingsNetworkNote = "ON にしても、このバージョンでは通信先が存在せず、実際の通信は一切行いません。";
    public const string SettingsSoundNote = "音源は同梱していません (設定のみ)。";
    public const string SettingsFileFormat = "設定ファイル: {0}";

    // PadLab について
    public const string AboutVersionFormat = "バージョン {0}";
    public const string AboutDescription = "PadLab は Windows 向けのゲームコントローラー診断ツールです。インストール・登録・ログインは不要です。";
    public const string AboutThirdParty = "第三者ライセンス (THIRD-PARTY-NOTICES.md)";
    public const string AboutThirdPartyMissing = "THIRD-PARTY-NOTICES.md が見つかりません。";
    public const string AboutSessionsDirFormat = "セッション保存先: {0}";
    public const string AboutOpenFolder = "保存フォルダーを開く";
}
