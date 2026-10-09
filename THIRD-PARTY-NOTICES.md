# サードパーティ ライセンス表記

PadLab は以下のライブラリを利用しています。各ライセンスの全文はリンク先を参照してください。

## 配布物 (PadLab.exe) に含まれるもの

| ライブラリ | バージョン | ライセンス | URL |
|---|---|---|---|
| .NET Runtime / WPF | 10.0 | MIT | https://github.com/dotnet/runtime , https://github.com/dotnet/wpf |
| WPF-UI (Wpf.Ui) | 4.3.0 | MIT | https://github.com/lepoco/wpfui |
| SkiaSharp / SkiaSharp.Views.WPF | 4.151.2 | MIT | https://github.com/mono/SkiaSharp |
| Skia (SkiaSharp が同梱するネイティブ) | - | BSD-3-Clause | https://skia.org/ |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | https://github.com/CommunityToolkit/dotnet |
| Vortice.DirectInput (Vortice.Windows) | 3.8.3 | MIT | https://github.com/amerkoleci/Vortice.Windows |
| HidSharp | 2.6.4 | Apache-2.0 | https://www.zer7.com/software/hidsharp |

## 開発時のみ使用 (配布物には含まれない)

| ライブラリ | ライセンス | URL |
|---|---|---|
| xunit | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.runner.visualstudio | Apache-2.0 | https://github.com/xunit/visualstudio.xunit |
| Microsoft.NET.Test.Sdk | MIT | https://github.com/microsoft/vstest |
| coverlet.collector | MIT | https://github.com/coverlet-coverage/coverlet |

## 参照した資料 (コードの再利用ではなく HID レポートレイアウトの参考)

参照元は `src/PadLab.Input/Hid/*.cs` の先頭コメントに記載しています。
