# Per-user install: no admin, no UAC. Copies dist\ to %LOCALAPPDATA%\TrayHelper, keeps an existing ini,
# points HKCU Run at the new exe, and restarts the helper there. Usage: powershell -ExecutionPolicy Bypass -File installer\install.ps1
param([string]$Source = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist'))
$ErrorActionPreference = 'Stop'
$dest = Join-Path $env:LOCALAPPDATA 'TrayHelper'
New-Item -ItemType Directory -Force $dest | Out-Null
$ini = Join-Path $dest 'tray-helper.ini'
if (-not (Test-Path $ini)) { foreach ($c in @((Join-Path $Source 'tray-helper.ini'), (Join-Path $env:LOCALAPPDATA 'GrokBotTray\tray-helper.ini'))) { if (Test-Path $c) { Copy-Item $c $ini; break } } }
# Stop a running helper (hidden windows are re-adopted by the new one via hidden.txt).
Get-CimInstance Win32_Process -Filter "Name='TrayHelper.exe'" | % { Stop-Process -Id $_.ProcessId -Force }
Start-Sleep -Milliseconds 800
$hid = Get-ChildItem $Source -Filter hidden.txt -ErrorAction SilentlyContinue; if ($hid) { Copy-Item $hid.FullName $dest -Force }
foreach ($f in 'TrayHelper.exe','VirtualDesktopAccessor.dll') { $p = Join-Path $Source $f; if (Test-Path $p) { Copy-Item $p $dest -Force } }
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name TrayHelper -Value ('"' + (Join-Path $dest 'TrayHelper.exe') + '"')
Start-Process (Join-Path $dest 'TrayHelper.exe') -WorkingDirectory $dest
Write-Host "Installed to $dest"
