# Usage (on the PC): powershell -ExecutionPolicy Bypass -File tests\run.ps1   (expects dist\ built; uses a --test instance in %TEMP%)
# Isolated TrayHelper test run: second instance in --test mode with its own folder, its own hotkeys, managing only throwaway windows.
$ErrorActionPreference = 'Continue'
$TD = Join-Path $env:TEMP ('th-test-' + [DateTime]::Now.ToString('HHmmss')); $here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'lib.ps1')
$old = [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg'); if ($old -ne [IntPtr]::Zero) { [H]::PostMessage($old, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2 }
Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { $_.Path -like "$env:TEMP\th-test*" } | % { try { $_.Kill() } catch {} }
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? { $_.CommandLine -like '*win.ps1*-Title TH-*' } | % { Stop-Process -Id $_.ProcessId -Force }
Start-Sleep 1.5; Remove-Item $TD -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory $TD | Out-Null
$src = if($env:TH_SRC){$env:TH_SRC}else{(Join-Path (Split-Path -Parent $PSScriptRoot) 'dist')}
Copy-Item "$src\TrayHelper.exe","$src\VirtualDesktopAccessor.dll" $TD
@"
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

[App:THStubborn]
Enabled=true
ProcessName=powershell
WindowClass=
WindowTitle=TH-Stubborn-Window
"@ | Set-Content "$TD\tray-helper.ini"
function StartTH { Start-Process "$TD\TrayHelper.exe" -ArgumentList '--test' -WorkingDirectory $TD; Start-Sleep 3 }
function THPid { (Get-Process TrayHelper -ErrorAction SilentlyContinue | ? { -not $_.HasExited -and $_.Path -like "$TD*" }).Id }
function Status($name) { (Get-Content "$TD\status.txt" | ? { $_.StartsWith("[$name]") }) }
$KC=0x11; $KA=0x12; $KS=0x10
function SendTemp { [H]::Combo([byte[]]($KC,$KA,$KS), 0x7B) }   # Ctrl+Alt+Shift+F12
function RestoreAll { [H]::Combo([byte[]]($KC,$KA,$KS), 0x7A) } # Ctrl+Alt+Shift+F11
$win = Join-Path $here 'win.ps1'
function NewWin($title, [switch]$stub, $x=120, $y=120) { $a = "-NoProfile -ExecutionPolicy Bypass -File `"$win`" -Title $title -X $x -Y $y"; if ($stub) { $a += ' -Stubborn' }; Start-Process powershell -ArgumentList $a -WindowStyle Hidden; for ($i=0;$i -lt 120;$i++){ Start-Sleep -Milliseconds 250; $h=[H]::Find($title); if ($h -ne [IntPtr]::Zero -and [H]::IsWindowVisible($h)) { return $h } }; return [IntPtr]::Zero }

StartTH; Rep 'test instance started' ([bool](THPid)) ("pid=" + (THPid))
$wm = NewWin 'TH-Test-Window'; $wt = NewWin 'TH-Temp-Window' -x 600; $ws = NewWin 'TH-Stubborn-Window' -stub -x 1080
Rep 'throwaway windows' ($wm -ne [IntPtr]::Zero -and $wt -ne [IntPtr]::Zero -and $ws -ne [IntPtr]::Zero)
Start-Sleep 2

# 1. X click on a managed window -> hidden
$sx = Status 'THTest'; $r = New-Object H+RECT; [H]::GetWindowRect($wm, [ref]$r) | Out-Null; [H]::Focus($wm) | Out-Null; Start-Sleep 1.2
$clicked = [H]::ClickIfOn($wm, $r.r - 25, $r.t + 18); Start-Sleep 1
Rep 'X click hides managed window (window alive)' ($clicked -and [H]::IsWindow($wm) -and -not [H]::IsWindowVisible($wm)) "clicked=$clicked alive=$([H]::IsWindow($wm)) before=$sx"

# 2. Send-any-window shortcut
$fok = [H]::Focus($wt); if ($fok) { SendTemp; Start-Sleep 1 }
Rep 'send-to-tray shortcut hides any window' ($fok -and -not [H]::IsWindowVisible($wt)) "focus=$fok"
$hf = Get-Content "$TD\hidden.txt" -ErrorAction SilentlyContinue
Rep 'hidden.txt records both' (($hf | Measure).Count -eq 2 -and ($hf -match "`tt").Count -eq 1) (($hf -join ' | '))

# 3. Restore-all shortcut
RestoreAll; Start-Sleep 1.2
Rep 'restore-all shortcut brings both back' ([H]::IsWindowVisible($wm) -and [H]::IsWindowVisible($wt))
Start-Sleep 2; Rep 'temp tray entry gone after restore' (-not (Test-Path "$TD\hidden.txt"))

# 4. Alt+F4 on managed -> hidden
$fok = [H]::Focus($wm); if ($fok) { [H]::Combo([byte[]]($KA), 0x73); Start-Sleep 1 }
Rep 'Alt+F4 hides managed window' ($fok -and -not [H]::IsWindowVisible($wm))
$fok = [H]::Focus($wt); if ($fok) { SendTemp; Start-Sleep 1 }

# 5. Crash: kill -9, restart -> managed stays hidden (adopted), temp comes back
(Get-Process -Id (THPid)).Kill(); Start-Sleep 1
Rep 'after crash both still hidden' (-not [H]::IsWindowVisible($wm) -and -not [H]::IsWindowVisible($wt))
StartTH; Start-Sleep 2
Rep 'restart: temp window shown again' ([H]::IsWindowVisible($wt))
Rep 'restart: managed window re-adopted, still hidden' (-not [H]::IsWindowVisible($wm) -and ((Status 'THTest') -match 'state=hidden')) (Status 'THTest')

# 6. Pass-through: a window that refuses to hide -> close goes through
$fok = [H]::Focus($ws); if ($fok) { [H]::Combo([byte[]]($KA), 0x73); Start-Sleep 2 }
Rep 'unhideable window closes normally on Alt+F4' ($fok -and -not [H]::IsWindow($ws)) "focus=$fok"

# 7. Exit restores everything
$mw = [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg'); [H]::PostMessage($mw, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 2
Rep 'Exit restores hidden windows' ([H]::IsWindowVisible($wm) -and -not (THPid))

# 8. UWP via hidden virtual desktop (Calculator)
StartTH
$calcWasRunning = [bool](Get-Process CalculatorApp -ErrorAction SilentlyContinue)
$d0 = [V]::GetDesktopCount()
if (-not $calcWasRunning) { Start-Process 'calculator:' }
$wc = [IntPtr]::Zero; for ($i=0;$i -lt 40 -and $wc -eq [IntPtr]::Zero;$i++){ Start-Sleep -Milliseconds 300; $wc = [H]::FindFrameOf('CalculatorApp') }
Start-Sleep 1
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
function TaskbarNames { $tb = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ClassNameProperty, 'Shell_TrayWnd'))); if (-not $tb) { return @() }; $tb.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button))) | % { $_.Current.Name } }
$ctitle = [H]::Title($wc); $pre = @(TaskbarNames | ? { $_ -and $ctitle -and $_ -like "*$ctitle*" }).Count
$fok = $false; if ($wc -ne [IntPtr]::Zero) { $fok = [H]::Focus($wc); if ($fok) { SendTemp; Start-Sleep 2 } }
$ghost = @(TaskbarNames | ? { $_ -and $ctitle -and $_ -like "*$ctitle*" }).Count
$d1 = [V]::GetDesktopCount(); $on = [V]::IsWindowOnCurrentVirtualDesktop($wc)
$filter = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -ErrorAction SilentlyContinue).VirtualDesktopTaskbarFilter
Rep 'UWP moved to hidden desktop' ($fok -and $d1 -eq $d0 + 1 -and $on -eq 0) "frame=$wc focus=$fok desktops $d0->$d1 onCurrent=$on taskbarFilter=$filter"
Rep 'UWP: no taskbar ghost' ($pre -gt 0 -and $ghost -lt $pre) ("matching taskbar buttons before=$pre hidden=$ghost names: " + ((TaskbarNames | ? { $_ -like "*$ctitle*" }) -join ' / '))
RestoreAll; Start-Sleep 2
$back = @(TaskbarNames | ? { $_ -and $ctitle -and $_ -like "*$ctitle*" }).Count; $d2 = [V]::GetDesktopCount(); $on2 = [V]::IsWindowOnCurrentVirtualDesktop($wc)
Rep 'UWP restored, hidden desktop removed' ($on2 -eq 1 -and $d2 -eq $d0) "desktops=$d2 onCurrent=$on2 visible=$([H]::IsWindowVisible($wc)) taskbarButtonBack=$back"
$logs = Get-Content "$TD\tray-helper.log" | Select-String 'desktop|Calculator' | % Line
if (-not $calcWasRunning -and $wc -ne [IntPtr]::Zero) { [H]::PostMessage($wc, 0x10, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }

# cleanup
$mw = [H]::FindWindowEx([IntPtr](-3), [IntPtr]::Zero, [NullString]::Value, 'TrayHelper.test.msg'); [H]::PostMessage($mw, 0x8004, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null; Start-Sleep 1.5
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | ? { $_.CommandLine -like '*win.ps1*-Title TH-*' } | % { Stop-Process -Id $_.ProcessId -Force }
$script:results; '--- test log ---'; Get-Content "$TD\tray-helper.log" | Select-Object -Last 30
