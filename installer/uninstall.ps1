# Restores every hidden window (asks the helper to exit normally), removes autostart and the install folder. Keeps nothing else.
$ErrorActionPreference = 'Continue'
$dest = Join-Path $env:LOCALAPPDATA 'TrayHelper'
Add-Type -Namespace TH -Name U -MemberDefinition '[DllImport("user32",CharSet=CharSet.Unicode)] public static extern System.IntPtr FindWindowEx(System.IntPtr p,System.IntPtr a,string c,string t); [DllImport("user32")] public static extern bool PostMessage(System.IntPtr h,uint m,System.IntPtr w,System.IntPtr l);'
$w = [TH.U]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, $null, 'TrayHelper.msg')
if ($w -ne [IntPtr]::Zero) { [TH.U]::PostMessage($w, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2 }
Get-CimInstance Win32_Process -Filter "Name='TrayHelper.exe'" | % { Stop-Process -Id $_.ProcessId -Force }
Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name TrayHelper -ErrorAction SilentlyContinue
Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
Write-Host 'TrayHelper removed'
