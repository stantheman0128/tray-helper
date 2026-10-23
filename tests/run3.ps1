# Test windows are TopMost so Stan's maximized windows can't cover their X button.
# Usage (on the PC): powershell -ExecutionPolicy Bypass -File tests\run3.ps1   (0.9 multi-window; $env:TH_SRC to test a build outside dist\)
$ErrorActionPreference = 'Continue'
$TD = Join-Path $env:TEMP ('th-test-' + [DateTime]::Now.ToString('HHmmss')); $here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'lib.ps1')
$mainPid = (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { $_.Path -like '*\tray-helper\dist\*' -and -not $_.HasExited }).Id
$old = [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg'); if ($old -ne [IntPtr]::Zero) { [H]::PostMessage($old, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2 }
Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { $_.Path -like "$env:TEMP\th-test*" } | % { try { $_.Kill() } catch {} }
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? { $_.CommandLine -like '*win.ps1*-Title TH-*' } | % { Stop-Process -Id $_.ProcessId -Force }
Start-Sleep 1.5; New-Item -ItemType Directory $TD | Out-Null
$src = if($env:TH_SRC){$env:TH_SRC}else{(Join-Path (Split-Path -Parent $PSScriptRoot) 'dist')}
Copy-Item "$src\TrayHelper.exe","$src\VirtualDesktopAccessor.dll" $TD
@"
[General]
Notifications=false
HotkeyRestoreAll=Ctrl+Alt+Shift+F11
HotkeySendToTray=Ctrl+Alt+Shift+F12

[App:THMulti]
Enabled=true
ProcessName=powershell
WindowClass=
WindowTitle=TH-Multi-*
"@ | Set-Content "$TD\tray-helper.ini"
function StartTH { Start-Process "$TD\TrayHelper.exe" -ArgumentList '--test' -WorkingDirectory $TD; Start-Sleep 3 }
function THPid { (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { -not $_.HasExited -and $_.Path -like "$TD\*" }).Id }
function Status { (Get-Content "$TD\status.txt" -ErrorAction SilentlyContinue | ? { $_.StartsWith('[THMulti]') }) }
function Vis($h) { [H]::IsWindow($h) -and [H]::IsWindowVisible($h) }
function Hid($h) { [H]::IsWindow($h) -and -not [H]::IsWindowVisible($h) }
$KC=0x11; $KA=0x12; $KS=0x10
function SendTemp { [H]::Combo([byte[]]($KC,$KA,$KS), 0x7B) }
function RestoreAll { [H]::Combo([byte[]]($KC,$KA,$KS), 0x7A) }
function AltF4($h) { $f = [H]::Focus($h); if ($f) { Start-Sleep -Milliseconds 400; [H]::Combo([byte[]]($KA), 0x73); Start-Sleep 1.2 }; $f }
$win = Join-Path $here 'win.ps1'
Add-Type -Namespace P -Name K -MemberDefinition '[DllImport("user32.dll")] public static extern System.IntPtr WindowFromPoint(System.Drawing.Point p); [DllImport("user32.dll")] public static extern System.IntPtr GetAncestor(System.IntPtr h,uint f); [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(System.IntPtr h, System.Text.StringBuilder s, int n); [DllImport("user32.dll")] public static extern bool SetWindowPos(System.IntPtr h, System.IntPtr a, int x, int y, int cx, int cy, uint f);' -ReferencedAssemblies System.Drawing -ErrorAction SilentlyContinue
function ClickX($h) { $r = New-Object H+RECT; for ($i=0;$i -lt 3;$i++) { [H]::GetWindowRect($h, [ref]$r) | Out-Null; [H]::Focus($h) | Out-Null; Start-Sleep 1.2; $ok = [H]::ClickIfOn($h, $r.r - 25, $r.t + 18); if ($ok) { return $true }; $w=[P.K]::GetAncestor([P.K]::WindowFromPoint((New-Object System.Drawing.Point(($r.r-25),($r.t+18)))),2); $sb=New-Object System.Text.StringBuilder 200; [P.K]::GetWindowText($w,$sb,200)|Out-Null; Write-Host "diag: X point covered by hwnd=$w '$sb'" }; return $false }
function NewWin($title, $x=120, $y=120) { Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$win`" -Title $title -TopMost -X $x -Y $y" -WindowStyle Hidden; for ($i=0;$i -lt 120;$i++){ Start-Sleep -Milliseconds 250; $h=[H]::Find($title); if ($h -ne [IntPtr]::Zero -and [H]::IsWindowVisible($h)) { return $h } }; return [IntPtr]::Zero }

StartTH; Rep 'test instance started' ([bool](THPid))
$A = NewWin 'TH-Multi-A' 120; $B = NewWin 'TH-Multi-B' 640; Start-Sleep 2
Rep 'both windows tracked' ((Status) -match 'windows=2 open=2') (Status)
$f = AltF4 $A; Rep 'Alt+F4 with another window open closes this one' ($f -and -not [H]::IsWindow($A) -and (Vis $B)) "focus=$f"
$f = AltF4 $B; Rep 'Alt+F4 on the last window hides it' ($f -and (Hid $B))
$C = NewWin 'TH-Multi-C' 120; $D = NewWin 'TH-Multi-D' 640; Start-Sleep 2
$f = [H]::Focus($C); if ($f) { SendTemp; Start-Sleep 1 }
Rep 'send-to-tray hotkey hides one window of a managed app' ((Hid $C) -and (Vis $D) -and ((Status) -match 'hidden=2')) (Status)
$ck = ClickX $D; Start-Sleep 1.2
Rep 'X on the last open window hides it (others already hidden)' ($ck -and (Hid $D) -and ((Status) -match 'state=hidden') -and ((Status) -match 'hidden=3')) "clicked=$ck $(Status)"
$hf = @(Get-Content "$TD\hidden.txt" -ErrorAction SilentlyContinue)
Rep 'hidden.txt has one record per hidden window' ($hf.Count -eq 3) ($hf -join ' | ')
(Get-Process -Id (THPid)).Kill(); Start-Sleep 1; StartTH; Start-Sleep 1
Rep 'crash + restart: all 3 windows still hidden and re-adopted' ((Hid $B) -and (Hid $C) -and (Hid $D) -and ((Status) -match 'hidden=3') -and ((Status) -match 'state=hidden')) (Status)
RestoreAll; Start-Sleep 1.2
if (-not ((Status) -match 'hidden=0')) { Write-Output 'diag: retry restore hotkey'; Start-Sleep 1; RestoreAll; Start-Sleep 1.5 }
Rep 'restore-all brings every window back' ((Vis $B) -and (Vis $C) -and (Vis $D) -and ((Status) -match 'hidden=0')) (Status)
$ck = ClickX $C; Start-Sleep 1.5
Rep 'X with other windows open closes the window' ($ck -and -not [H]::IsWindow($C) -and (Vis $B) -and (Vis $D)) "clicked=$ck"
Start-Sleep 1; $f = AltF4 $B; $f2 = AltF4 $D
Rep 'closing down to the last window: B closed, D hidden' (-not [H]::IsWindow($B) -and (Hid $D)) "focus=$f/$f2"
$mw = [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg'); [H]::PostMessage($mw, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2
Rep 'Exit restores the hidden window' ((Vis $D) -and -not (THPid))
Rep 'main TrayHelper untouched' ($mainPid -and (Get-Process -Id $mainPid -ErrorAction SilentlyContinue)) "pid=$mainPid"
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? { $_.CommandLine -like '*win.ps1*-Title TH-*' } | % { Stop-Process -Id $_.ProcessId -Force }
$script:results; '--- test log ---'; Get-Content "$TD\tray-helper.log" | Select-Object -Last 16
