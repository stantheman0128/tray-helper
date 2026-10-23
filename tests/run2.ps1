# Usage (on the PC): powershell -ExecutionPolicy Bypass -File tests\run2.ps1  (0.8 features; set $env:TH_SRC to test a build outside dist\)
# 0.8 feature tests on an isolated --test instance: live ini reload, ExePath self-heal, Store logo, handover, updater swap, setup.exe install/uninstall.
$ErrorActionPreference = 'Continue'
$TD = Join-Path $env:TEMP ('th-test-' + [DateTime]::Now.ToString('HHmmss')); $TD2 = "$TD-setup"; $here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'lib.ps1')
$mainPid = (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { $_.Path -like '*\tray-helper\dist\*' -and -not $_.HasExited }).Id
$old = [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg'); if ($old -ne [IntPtr]::Zero) { [H]::PostMessage($old, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2 }
Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { $_.Path -like "$env:TEMP\th-test*" } | % { try { $_.Kill() } catch {} }
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? { $_.CommandLine -like '*win.ps1*-Title TH-*' } | % { Stop-Process -Id $_.ProcessId -Force }
Start-Sleep 1.5; New-Item -ItemType Directory $TD | Out-Null
$src = if($env:TH_SRC){$env:TH_SRC}else{(Join-Path (Split-Path -Parent $PSScriptRoot) 'dist')}
Copy-Item "$src\TrayHelper.exe","$src\VirtualDesktopAccessor.dll" $TD
$ini = @"
[General]
Notifications=false
HotkeyRestoreAll=Ctrl+Alt+Shift+F11
HotkeySendToTray=Ctrl+Alt+Shift+F12
UwpVirtualDesktop=true

[App:THTest]
Enabled=true
ProcessName=powershell
WindowClass=
WindowTitle=TH-Test-Window

[App:THGone]
Enabled=true
ProcessName=nothing-here
WindowTitle=nothing

[App:CalcOld]
Enabled=false
ExePath=C:\Program Files\WindowsApps\Microsoft.WindowsCalculator_1.0.0.0_x64__8wekyb3d8bbwe\CalculatorApp.exe
ProcessName=nothing-calc
WindowTitle=nothing
"@
$ini | Set-Content "$TD\tray-helper.ini"
$runBefore = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue).TrayHelper
function StartTH($extra='') { Start-Process "$TD\TrayHelper.exe" -ArgumentList ("--test " + $extra) -WorkingDirectory $TD; Start-Sleep 3 }
function THPid { (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { -not $_.HasExited -and $_.Path -like "$TD\*" }).Id }
function Status($name, $d=$TD) { (Get-Content "$d\status.txt" -ErrorAction SilentlyContinue | ? { $_.StartsWith("[$name]") }) }
function MsgWin { [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg') }
function LogHas($pat, $d=$TD) { [bool](Get-Content "$d\tray-helper.log" -ErrorAction SilentlyContinue | Select-String -SimpleMatch $pat) }
$KC=0x11; $KA=0x12; $KS=0x10
function SendTemp { [H]::Combo([byte[]]($KC,$KA,$KS), 0x7B) }
function RestoreAll { [H]::Combo([byte[]]($KC,$KA,$KS), 0x7A) }
$win = Join-Path $here 'win.ps1'
function NewWin($title, $x=120, $y=120) { Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$win`" -Title $title -X $x -Y $y" -WindowStyle Hidden; for ($i=0;$i -lt 120;$i++){ Start-Sleep -Milliseconds 250; $h=[H]::Find($title); if ($h -ne [IntPtr]::Zero -and [H]::IsWindowVisible($h)) { return $h } }; return [IntPtr]::Zero }

StartTH; $p1 = THPid; Rep 'test instance started' ([bool]$p1) "pid=$p1"
$t = Get-Content "$TD\tray-helper.ini" -Raw
Rep 'stale WindowsApps path -> version-independent AUMID' ($t -match [regex]::Escape('ExePath=shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App')) (($t -split "`n" | ? { $_ -like 'ExePath=*' }) -join ' ; ')
$wm = NewWin 'TH-Test-Window'; Start-Sleep 2.5
$t = Get-Content "$TD\tray-helper.ini" -Raw; $sec = ($t -split '(?m)^\[App:')[1]
Rep 'empty ExePath filled from the running window' ($sec -match 'ExePath=.*powershell\.exe') (($sec -split "`n" | ? { $_ -like 'ExePath=*' }) -join '')

# live reload
$fok = [H]::Focus($wm); if ($fok) { [H]::Combo([byte[]]($KA), 0x73); Start-Sleep 1 }
Rep 'Alt+F4 hides managed window' ($fok -and -not [H]::IsWindowVisible($wm))
$t = Get-Content "$TD\tray-helper.ini" -Raw
$t2 = $t -replace '(?s)(\[App:THTest\].*?)Enabled=true','$1Enabled=false' -replace 'HotkeyRestoreAll=Ctrl\+Alt\+Shift\+F11','HotkeyRestoreAll=Ctrl+Alt+Shift+F10' -replace '(?s)\[App:THGone\].*?(?=\[App:)',''
$t2 += "`r`n[App:THNew]`r`nEnabled=true`r`nProcessName=nothing-new`r`nWindowTitle=nothing`r`n"
[IO.File]::WriteAllText("$TD\tray-helper.ini", $t2); Start-Sleep 2
Rep 'ini edit picked up without restart' ((LogHas 'ini edited on disk, reloaded') -and (THPid) -eq $p1)
Rep 'disabling in ini shows the hidden window' ([H]::IsWindowVisible($wm)) (Status 'THTest')
Rep 'ini: app added / removed live' ((Status 'THNew') -and -not (Status 'THGone')) "new=$([bool](Status 'THNew')) gone=$(-not (Status 'THGone'))"
$t3 = (Get-Content "$TD\tray-helper.ini" -Raw) -replace '(?s)(\[App:THTest\].*?)Enabled=false','$1Enabled=true'; [IO.File]::WriteAllText("$TD\tray-helper.ini", $t3); Start-Sleep 2
Rep 're-enabled via ini' ((Status 'THTest') -match 'enabled=True') (Status 'THTest')
$fok = [H]::Focus($wm); if ($fok) { [H]::Combo([byte[]]($KA), 0x73); Start-Sleep 1 }
[H]::Combo([byte[]]($KC,$KA,$KS), 0x79); Start-Sleep 1
Rep 'new restore-all hotkey from ini works' ([H]::IsWindowVisible($wm))

# Store logo for a hotkey-hidden Calculator
$calcWas = [bool](Get-Process CalculatorApp -ErrorAction SilentlyContinue); if (-not $calcWas) { Start-Process 'calculator:' }
$wc = [IntPtr]::Zero; for ($i=0;$i -lt 40 -and $wc -eq [IntPtr]::Zero;$i++){ Start-Sleep -Milliseconds 300; $wc = [H]::FindFrameOf('CalculatorApp') }; Start-Sleep 1
$fok = $false; if ($wc -ne [IntPtr]::Zero) { $fok = [H]::Focus($wc); if ($fok) { SendTemp; Start-Sleep 2 } }
Rep 'hotkey-hidden Store app uses its own logo' ($fok -and (LogHas 'Store logo icon')) ((Get-Content "$TD\tray-helper.log" | Select-String 'shortcut' | Select -Last 1).Line)
[H]::Combo([byte[]]($KC,$KA,$KS), 0x79); Start-Sleep 2
if (-not $calcWas -and $wc -ne [IntPtr]::Zero) { [H]::PostMessage($wc, 0x10, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }

# handover (used by updater and setup): old instance leaves without showing windows, new one re-adopts
$fok = [H]::Focus($wm); if ($fok) { [H]::Combo([byte[]]($KA), 0x73); Start-Sleep 1 }
[H]::PostMessage((MsgWin), 0x8005, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2
Rep 'handover exit keeps windows hidden' (-not (THPid) -and -not [H]::IsWindowVisible($wm))
StartTH '--replace'; Rep 'new instance re-adopts hidden window' ((Status 'THTest') -match 'state=hidden') (Status 'THTest')

# updater: bad hash refused, good hash swaps + restarts
$pk = "$TD\pkg"; New-Item -ItemType Directory $pk | Out-Null; Copy-Item "$src\TrayHelper.exe" $pk
$h = (Get-FileHash "$pk\TrayHelper.exe" -Algorithm SHA256).Hash.ToLower(); "$h  TrayHelper.exe" | Set-Content "$pk\good.sha256"; ('0' * 64) | Set-Content "$pk\bad.sha256"
$u = 'file:///' + ($pk -replace '\\','/')
[H]::PostMessage((MsgWin), 0x8005, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2
StartTH "--replace --test-update $u/TrayHelper.exe $u/bad.sha256 0.8.0"; Start-Sleep 2; $pb = THPid
Rep 'update with wrong SHA-256 is refused' ((LogHas 'sha256 mismatch') -and $pb -and -not (Test-Path "$TD\TrayHelper.old.exe"))
[H]::PostMessage((MsgWin), 0x8005, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2
StartTH "--replace --test-update $u/TrayHelper.exe $u/good.sha256 0.8.0"; Start-Sleep 5; $pn = THPid
Rep 'verified update swaps exe and restarts' ((LogHas 'updating to 0.8.0') -and (Test-Path "$TD\TrayHelper.old.exe") -and $pn) "pid=$pn"
Rep 'after update: window still hidden, re-adopted' (-not [H]::IsWindowVisible($wm) -and ((Status 'THTest') -match 'state=hidden')) (Status 'THTest')
& "$pk\TrayHelper.exe" --check-update | Out-Null; Start-Sleep 6
Rep 'update check against the (private) repo fails quietly' ((Get-Content "$pk\tray-helper.log" -ErrorAction SilentlyContinue | Select-String 'update check:') -ne $null) ((Get-Content "$pk\tray-helper.log" -ErrorAction SilentlyContinue | Select-String 'update check:' | Select -Last 1).Line)

# setup.exe: migrate the running test instance into a new folder, then uninstall
$setup = "$src\TrayHelper-Setup.exe"
(Start-Process $setup -ArgumentList "--test --silent --no-run-key --dir `"$TD2`"" -PassThru).WaitForExit(30000) | Out-Null; Start-Sleep 4
$ps = (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { -not $_.HasExited -and $_.Path -like "$TD2*" }).Id
Rep 'setup installs files' ((Test-Path "$TD2\TrayHelper.exe") -and (Test-Path "$TD2\VirtualDesktopAccessor.dll") -and (Test-Path "$TD2\Uninstall.exe")) ((Get-ChildItem $TD2 -Name) -join ',')
Rep 'setup migrates settings and the running helper' ((Test-Path "$TD2\tray-helper.ini") -and $ps -and -not (THPid)) "newPid=$ps"
Rep 'setup: hidden window stays hidden and is re-adopted' (-not [H]::IsWindowVisible($wm) -and ((Status 'THTest' $TD2) -match 'state=hidden')) (Status 'THTest' $TD2)
(Start-Process "$TD2\Uninstall.exe" -ArgumentList '--uninstall --test --silent' -PassThru).WaitForExit(30000) | Out-Null; Start-Sleep 5
Rep 'uninstall restores windows and removes the folder' ([H]::IsWindowVisible($wm) -and -not (Test-Path $TD2) -and -not (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { -not $_.HasExited -and $_.Path -like "$TD2*" })) "folderLeft=$(Test-Path $TD2)"
$runAfter = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue).TrayHelper
Rep 'real autostart entry untouched' ($runAfter -eq $runBefore) "$runAfter"
Rep 'main TrayHelper untouched' ($mainPid -and (Get-Process -Id $mainPid -ErrorAction SilentlyContinue)) "pid=$mainPid"

# cleanup
$mw = MsgWin; if ($mw -ne [IntPtr]::Zero) { [H]::PostMessage($mw, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 1.5 }
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? { $_.CommandLine -like '*win.ps1*-Title TH-*' } | % { Stop-Process -Id $_.ProcessId -Force }
$script:results; '--- test log ---'; Get-Content "$TD\tray-helper.log" | Select-Object -Last 25; '--- setup log ---'; Get-Content "$env:TEMP\TrayHelper-Setup.log" -Tail 12
