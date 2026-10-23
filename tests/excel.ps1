# Usage (on the PC): powershell -ExecutionPolicy Bypass -File tests\excel.ps1
# Real Excel, multiple workbooks, against the RUNNING TrayHelper (Excel must be a managed app). Uses a separate automation
# instance of Excel with two blank workbooks; your own Excel windows are left alone. Briefly shows two Excel windows.
$ErrorActionPreference = 'Continue'; $here = Split-Path -Parent $MyInvocation.MyCommand.Path; . (Join-Path $here 'lib.ps1')
$dist = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist')
function Status { (Get-Content "$dist\status.txt" -ErrorAction SilentlyContinue | ? { $_.StartsWith('[Excel]') }) }
$before = Status; $hidBefore = @(Get-Content "$dist\hidden.txt" -ErrorAction SilentlyContinue)
$mine = @(Get-Process EXCEL -ErrorAction SilentlyContinue | % Id)
$xl = New-Object -ComObject Excel.Application; $xl.DisplayAlerts = $false; $xl.Visible = $true
$wb1 = $xl.Workbooks.Add(); $wb2 = $xl.Workbooks.Add(); Start-Sleep 1
$h1 = [IntPtr]$wb1.Windows.Item(1).Hwnd; $h2 = [IntPtr]$wb2.Windows.Item(1).Hwnd
$xpid = (Get-Process EXCEL | ? { $mine -notcontains $_.Id } | Select -First 1).Id
Rep 'test Excel is a separate process' ($xpid -and $mine -notcontains $xpid) "pid=$xpid yours=$($mine -join ',')"
Start-Sleep 2.5
$f = [H]::Focus($h1); if ($f) { Start-Sleep 0.5; [H]::Combo([byte[]](0x12), 0x73); Start-Sleep 2 }
Rep 'Excel: Alt+F4 on one of two workbooks closes it' ($f -and $xl.Workbooks.Count -eq 1 -and [H]::IsWindowVisible($h2)) "focus=$f workbooks=$($xl.Workbooks.Count)"
$f = [H]::Focus($h2); if ($f) { Start-Sleep 0.5; [H]::Combo([byte[]](0x12), 0x73); Start-Sleep 2 }
Rep 'Excel: Alt+F4 on the last workbook hides it (Excel keeps running)' ($f -and [H]::IsWindow($h2) -and -not [H]::IsWindowVisible($h2) -and $xl.Workbooks.Count -eq 1) "focus=$f $(Status)"
$yours = @($hidBefore | % { [IntPtr][long](($_ -split "`t")[0]) })
Rep 'your hidden Excel window untouched' (@($yours | ? { [H]::IsWindow($_) -and [H]::IsWindowVisible($_) }).Count -eq 0) ($hidBefore -join ' | ')
$xl.Quit(); [Runtime.InteropServices.Marshal]::FinalReleaseComObject($wb2) | Out-Null; [Runtime.InteropServices.Marshal]::FinalReleaseComObject($wb1) | Out-Null; [Runtime.InteropServices.Marshal]::FinalReleaseComObject($xl) | Out-Null; [GC]::Collect()
Start-Sleep 4; if ($xpid -and (Get-Process -Id $xpid -ErrorAction SilentlyContinue)) { Stop-Process -Id $xpid -Force }
Start-Sleep 2; $hidAfter = @(Get-Content "$dist\hidden.txt" -ErrorAction SilentlyContinue)
Rep 'after quitting test Excel, hidden.txt is back to your windows' (($hidAfter -join '|') -eq ($hidBefore -join '|')) ($hidAfter -join ' | ')
$script:results; "before: $before"; "after:  $(Status)"
