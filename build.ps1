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
& "$fw\csc.exe" /nologo /codepage:65001 /target:winexe /optimize+ "/out:$out" "/win32icon:$ico" "/resource:$ico,app.ico" "/resource:$png,logo.png" "/resource:$(Join-Path $here 'CHANGELOG.md'),CHANGELOG.md" $refs (Join-Path $here 'TrayHelper.cs') (Join-Path $here 'Catalog.cs') (Join-Path $here 'Ui.cs') (Join-Path $here 'Scanner.cs') (Join-Path $here 'Extras.cs')
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
$vda = Join-Path $here 'assets\VirtualDesktopAccessor.dll'; if (Test-Path $vda) { Copy-Item $vda (Join-Path $here 'dist') -Force -ErrorAction SilentlyContinue }
$assetsDir = Join-Path $here 'dist\assets'; New-Item -ItemType Directory -Force $assetsDir | Out-Null
Get-ChildItem (Join-Path $here 'assets') -Filter '*.gif' -ErrorAction SilentlyContinue | % { Copy-Item $_.FullName $assetsDir -Force }

Write-Host "Built $out"
# Setup: one exe with TrayHelper.exe + the DLL embedded (per-user, no admin).
$setup = Join-Path $here 'dist\TrayHelper-Setup.exe'
& "$fw\csc.exe" /nologo /codepage:65001 /target:winexe /optimize+ "/out:$setup" "/win32icon:$ico" "/resource:$out,TrayHelper.exe" "/resource:$vda,VirtualDesktopAccessor.dll" /r:System.Windows.Forms.dll (Join-Path $here 'Setup.cs')
if ($LASTEXITCODE -ne 0) { throw "Setup build failed ($LASTEXITCODE)" }
Write-Host "Built $setup"
