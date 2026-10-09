# 開発用: PadLab を起動して指定ページへ移動し、PrintWindow でウィンドウをキャプチャする (前面でなくても撮れる)
# 例: powershell -File tools\shot.ps1 -Exe dist\PadLab.exe -Page '基本入力' -Keys QE -Out shot.png
param([string]$Exe, [string]$Page, [string]$Keys, [string]$Out, [int]$Wait = 6)
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public class W {
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
 public struct RECT { public int L, T, R, B; } }
"@
$argv = @('--page', $Page); if ($Keys) { $argv += @('--keys', $Keys) }
$p = Start-Process $Exe -ArgumentList $argv -PassThru
Start-Sleep $Wait
$p.Refresh(); $h = $p.MainWindowHandle
$r = New-Object W+RECT; [W]::GetWindowRect($h, [ref]$r) | Out-Null
$bmp = New-Object System.Drawing.Bitmap(($r.R-$r.L), ($r.B-$r.T))
$g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
[W]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
$bmp.Save($Out); "saved $Out $($bmp.Width)x$($bmp.Height)"
Stop-Process -Id $p.Id -Force

