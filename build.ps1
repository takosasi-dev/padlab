# PadLab ビルド → テスト → 単体 exe publish → selftest
# PowerShell 5.1 互換 (&& は使わない)。実行: powershell -ExecutionPolicy Bypass -File .\build.ps1
$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$dist = Join-Path $root "dist"

Write-Host "== dotnet test"
dotnet test (Join-Path $root "PadLab.slnx") -c Release --nologo
if (-not $?) { Write-Host "テスト失敗"; exit 1 }

Write-Host "== dotnet publish"
dotnet publish (Join-Path $root "src\PadLab.App") -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:EnableCompressionInSingleFile=true `
  -o $dist --nologo
if (-not $?) { Write-Host "publish 失敗"; exit 1 }
Remove-Item (Join-Path $dist "*.pdb") -ErrorAction SilentlyContinue  # SkiaSharp のネイティブ pdb は不要

$exe = Join-Path $dist "PadLab.exe"
Write-Host ("== {0} ({1:N1} MB)" -f $exe, ((Get-Item $exe).Length / 1MB))

Write-Host "== selftest"
$p = Start-Process -FilePath $exe -ArgumentList "--selftest" -Wait -PassThru -NoNewWindow
Get-Content (Join-Path $env:LOCALAPPDATA "PadLab\selftest.txt")
if ($p.ExitCode -ne 0) { Write-Host "selftest 失敗 (exit $($p.ExitCode))"; exit 1 }
Write-Host "== 完了"
