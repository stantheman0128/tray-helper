param([string]$Title, [switch]$Stubborn, [switch]$TopMost, [int]$X=120, [int]$Y=120)
Add-Type -AssemblyName System.Windows.Forms
if ($Stubborn) {
Add-Type -ReferencedAssemblies System.Windows.Forms -TypeDefinition @"
public class SForm : System.Windows.Forms.Form { protected override void WndProc(ref System.Windows.Forms.Message m){ if(m.Msg==0x46){ int off=System.IntPtr.Size==8?32:24; int f=System.Runtime.InteropServices.Marshal.ReadInt32(m.LParam,off); System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam,off,f & ~0x80);} base.WndProc(ref m);} }
"@
$f = New-Object SForm } else { $f = New-Object System.Windows.Forms.Form }
$f.Text = $Title; $f.Width = 420; $f.Height = 240; $f.StartPosition = 'Manual'; $f.Left = $X; $f.Top = $Y; if ($TopMost) { $f.TopMost = $true }
Add-Type -Namespace W -Name U -MemberDefinition '[DllImport("user32")] public static extern bool ShowWindow(System.IntPtr h,int c);'
# Started with -WindowStyle Hidden: the first ShowWindow inherits SW_HIDE, so show it again explicitly.
$f.Add_Shown({ [W.U]::ShowWindow($f.Handle, 5) | Out-Null })
[System.Windows.Forms.Application]::Run($f)
