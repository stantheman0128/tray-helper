# Builds TrayHelper.exe with the csc.exe from .NET Framework 4.x (no SDK needed).
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$out = Join-Path $here 'TrayHelper.exe'
& $csc /nologo /target:winexe /optimize+ "/out:$out" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $here 'TrayHelper.cs')
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
Write-Host "Built $out"
