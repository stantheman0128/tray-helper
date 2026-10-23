Add-Type -TypeDefinition @"
using System; using System.Text; using System.Runtime.InteropServices; using System.Diagnostics; using System.Threading;
public static class H {
  public delegate bool EP(IntPtr h, IntPtr l);
  [DllImport("user32")] public static extern bool EnumWindows(EP p, IntPtr l);
  [DllImport("user32")] public static extern bool EnumChildWindows(IntPtr p, EP f, IntPtr l);
  [DllImport("user32")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32")] public static extern bool IsWindow(IntPtr h);
  [DllImport("user32")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
  [DllImport("user32",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32")] public static extern void keybd_event(byte vk, byte sc, uint f, UIntPtr e);
  [DllImport("user32")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32")] public static extern bool GetCursorPos(out POINT p);
  [DllImport("user32")] public static extern void mouse_event(uint f, int x, int y, uint d, UIntPtr e);
  [DllImport("user32")] public static extern IntPtr WindowFromPoint(POINT p);
  [DllImport("user32")] public static extern IntPtr GetAncestor(IntPtr h, uint f);
  [DllImport("user32")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr p, IntPtr a, string c, string t);
  [DllImport("user32")] public static extern bool SetProcessDpiAwarenessContext(IntPtr c);
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x, y; }
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int l, t, r, b; }
  public static string Title(IntPtr h){ var s=new StringBuilder(256); GetWindowText(h,s,256); return s.ToString(); }
  public static string Cls(IntPtr h){ var s=new StringBuilder(256); GetClassName(h,s,256); return s.ToString(); }
  public static IntPtr Find(string title){ IntPtr r=IntPtr.Zero; EnumWindows((h,l)=>{ if(Title(h)==title){ r=h; return false; } return true; },IntPtr.Zero); return r; }
  public static string Proc(IntPtr h){ uint p; GetWindowThreadProcessId(h,out p); try{ return Process.GetProcessById((int)p).ProcessName; }catch{ return ""; } }
  public static IntPtr FindFrameOf(string childProc){ IntPtr r=IntPtr.Zero; EnumWindows((h,l)=>{ if(Cls(h)!="ApplicationFrameWindow") return true; bool hit=false; EnumChildWindows(h,(c,l2)=>{ if(Cls(c)=="Windows.UI.Core.CoreWindow"&&Proc(c)==childProc){ hit=true; return false; } return true; },IntPtr.Zero); if(hit){ r=h; return false; } return true; },IntPtr.Zero); return r; }
  public static bool Focus(IntPtr h){ for(int i=0;i<5;i++){ keybd_event(0x12,0,0,UIntPtr.Zero); keybd_event(0x12,0,2,UIntPtr.Zero); SetForegroundWindow(h); Thread.Sleep(250); if(GetForegroundWindow()==h) return true; } return false; }
  public static void Combo(byte[] mods, byte key){ foreach(var m in mods) keybd_event(m,0,0,UIntPtr.Zero); keybd_event(key,0,0,UIntPtr.Zero); keybd_event(key,0,2,UIntPtr.Zero); for(int i=mods.Length-1;i>=0;i--) keybd_event(mods[i],0,2,UIntPtr.Zero); }
  public static bool ClickIfOn(IntPtr h, int x, int y){ var p=new POINT(); p.x=x; p.y=y; var w=WindowFromPoint(p); if(GetAncestor(w,2)!=h) return false; POINT old; GetCursorPos(out old); SetCursorPos(x,y); Thread.Sleep(80); mouse_event(2,0,0,0,UIntPtr.Zero); Thread.Sleep(60); mouse_event(4,0,0,0,UIntPtr.Zero); Thread.Sleep(150); SetCursorPos(old.x,old.y); return true; }
}
public static class V {
  const string D=@"$(Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\VirtualDesktopAccessor.dll')";
  [DllImport(D)] public static extern int GetDesktopCount();
  [DllImport(D)] public static extern int GetCurrentDesktopNumber();
  [DllImport(D)] public static extern int GetWindowDesktopNumber(IntPtr h);
  [DllImport(D)] public static extern int IsWindowOnCurrentVirtualDesktop(IntPtr h);
}
"@
[H]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$script:results = @()
function Rep($name, $ok, $info='') { $script:results += ('{0} {1} {2}' -f ($(if($ok){'PASS'}else{'FAIL'})), $name, $info) }
