# Builds to dist\TrayHelper.new.exe, then swaps it in for the running helper (no windows shown).
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$tmp = Join-Path $env:TEMP 'th-build-new.ps1'
(Get-Content (Join-Path $here 'build.ps1') -Raw).Replace("'dist\TrayHelper.exe'","'dist\TrayHelper.new.exe'").Replace('$here = Split-Path -Parent $MyInvocation.MyCommand.Path',"`$here = '$here'") | Set-Content $tmp
& powershell -NoProfile -ExecutionPolicy Bypass -File $tmp
if ($LASTEXITCODE -ne 0) { throw 'build failed' }
$d = Join-Path $here 'dist'
Get-ChildItem (Join-Path $here 'assets') -Filter *.dll -ErrorAction SilentlyContinue | % { Copy-Item $_.FullName $d -Force -ErrorAction SilentlyContinue }
$assetsDir = Join-Path $d 'assets'; New-Item -ItemType Directory -Force $assetsDir | Out-Null
Get-ChildItem (Join-Path $here 'assets') -Filter '*.gif' -ErrorAction SilentlyContinue | % { Copy-Item $_.FullName $assetsDir -Force }
$old = Get-CimInstance Win32_Process -Filter "Name='TrayHelper.exe'" | ? { $_.ExecutablePath -eq (Join-Path $d 'TrayHelper.exe') }
Remove-Item (Join-Path $d 'TrayHelper.old.exe') -ErrorAction SilentlyContinue
Rename-Item (Join-Path $d 'TrayHelper.exe') 'TrayHelper.old.exe'
Rename-Item (Join-Path $d 'TrayHelper.new.exe') 'TrayHelper.exe'
if ($old) { Stop-Process -Id $old.ProcessId -Force; Start-Sleep -Milliseconds 800 }
Start-Process (Join-Path $d 'TrayHelper.exe') -WorkingDirectory $d
Start-Sleep 4
Get-Content (Join-Path $d 'status.txt')
Get-Content (Join-Path $d 'tray-helper.log') -Tail 6
