# Builds dist\TrayHelper.exe with the csc.exe from .NET Framework 4.x (no SDK needed). WPF UI.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path "$fw\csc.exe")) { $fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$wpf = Join-Path $fw 'WPF'
New-Item -ItemType Directory -Force (Join-Path $here 'dist') | Out-Null
$out = Join-Path $here 'dist\TrayHelper.exe'
if (-not (Test-Path (Join-Path $here 'dist\tray-helper.ini'))) { Copy-Item (Join-Path $here 'tray-helper.ini') (Join-Path $here 'dist') }
$refs = @('System.Windows.Forms.dll','System.Drawing.dll','System.Core.dll',"$fw\System.Xaml.dll","$wpf\WindowsBase.dll","$wpf\PresentationCore.dll","$wpf\PresentationFramework.dll","$wpf\WindowsFormsIntegration.dll") | % { "/r:$_" }
$ico = Join-Path $here 'assets\app.ico'; $png = Join-Path $here 'assets\logo.png'
& "$fw\csc.exe" /nologo /target:winexe /optimize+ "/out:$out" "/win32icon:$ico" "/resource:$ico,app.ico" "/resource:$png,logo.png" $refs (Join-Path $here 'TrayHelper.cs') (Join-Path $here 'Catalog.cs') (Join-Path $here 'Ui.cs') (Join-Path $here 'Scanner.cs')
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
Write-Host "Built $out"
