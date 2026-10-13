# Code-signing prep. Needs a code-signing certificate in the user store and signtool (Windows SDK).
# Usage: powershell -File installer\sign.ps1 -Thumbprint <cert thumbprint>
param([Parameter(Mandatory=$true)][string]$Thumbprint, [string]$Timestamp = 'http://timestamp.digicert.com')
$ErrorActionPreference = 'Stop'
$st = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $st) { throw 'signtool.exe not found (install the Windows SDK)' }
$exe = Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\TrayHelper.exe'
& $st.FullName sign /sha1 $Thumbprint /fd SHA256 /tr $Timestamp /td SHA256 $exe
& $st.FullName verify /pa $exe
