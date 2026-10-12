# Builds TrayHelper.exe with the csc.exe from .NET Framework 4.x (no SDK needed).
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
New-Item -ItemType Directory -Force (Join-Path $here 'dist') | Out-Null
$out = Join-Path $here 'dist\TrayHelper.exe'
if (-not (Test-Path (Join-Path $here 'dist\tray-helper.ini'))) { Copy-Item (Join-Path $here 'tray-helper.ini') (Join-Path $here 'dist') }
& $csc /nologo /target:winexe /optimize+ "/out:$out" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $here 'TrayHelper.cs')
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
Write-Host "Built $out"
