// Finds user-facing app windows for "Add all open windows", and apps that already own a tray icon.
using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using System.Linq; using System.Runtime.InteropServices; using System.Text; using Microsoft.Win32;

static class Scanner{
  delegate bool EnumProc(IntPtr h,IntPtr l);
  [DllImport("user32")] static extern bool EnumWindows(EnumProc p,IntPtr l);
  [DllImport("user32")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32")] static extern IntPtr GetWindow(IntPtr h,uint c);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
  [DllImport("user32",EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr h,int i);
  [DllImport("user32")] static extern bool GetWindowRect(IntPtr h,out TrayHelper.RECT r);
  [DllImport("user32")] static extern bool IsIconic(IntPtr h);
  [DllImport("user32")] static extern IntPtr GetAncestor(IntPtr h,uint f);
  [DllImport("user32")] static extern IntPtr GetLastActivePopup(IntPtr h);
  // Raymond Chen's Alt+Tab rule + Win10/11 extras (cloaked, tool, no-activate).
  public static bool AltTab(IntPtr h){ if(!IsWindowVisible(h)) return false;
    IntPtr walk=GetAncestor(h,3), tr; int guard=0; while((tr=GetLastActivePopup(walk))!=walk&&guard++<50){ if(IsWindowVisible(tr)) break; walk=tr; } if(walk!=h) return false;
    int ex=GetWindowLong(h,-20); bool appw=(ex&0x40000)!=0; if(!appw&&((ex&0x80)!=0||(ex&0x08000000)!=0)) return false;
    int cl; if(DwmGetWindowAttribute(h,14,out cl,4)==0&&cl!=0) return false;
    var t=new StringBuilder(4); return GetWindowText(h,t,4)>0; }
  public sealed class Win{ public IntPtr H; public string Title, Proc, Cls, Exe, Aumid; }
  public static List<Win> AltTabWindows(){ var res=new List<Win>(); int me=Process.GetCurrentProcess().Id; var excl=new HashSet<string>(TrayHelper.Exclude,StringComparer.OrdinalIgnoreCase);
    EnumWindows((h,l)=>{ if(!AltTab(h)) return true; uint pid; GetWindowThreadProcessId(h,out pid); if(pid==me) return true;
      var c=new StringBuilder(256); GetClassName(h,c,256); var cls=c.ToString(); if(cls=="Progman"||cls=="Shell_TrayWnd"||cls=="WorkerW") return true;
      string pn=null; try{ using(var p=Process.GetProcessById((int)pid)) pn=p.ProcessName; }catch{} if(pn==null||excl.Contains(pn)) return true;
      var t=new StringBuilder(256); GetWindowText(h,t,256); string exe=null, aumid=null; var ph=OpenProcess(0x1000,false,pid);
      if(ph!=IntPtr.Zero){ try{ var sb=new StringBuilder(1024); int n=sb.Capacity; if(QueryFullProcessImageName(ph,0,sb,ref n)) exe=sb.ToString(); var sa=new StringBuilder(256); int na=sa.Capacity; try{ if(GetApplicationUserModelId(ph,ref na,sa)==0) aumid=sa.ToString(); }catch(EntryPointNotFoundException){} }finally{ CloseHandle(ph); } }
      res.Add(new Win{ H=h, Title=t.ToString(), Proc=pn, Cls=cls, Exe=exe, Aumid=aumid }); return true; },IntPtr.Zero); return res; }
  [DllImport("dwmapi")] static extern int DwmGetWindowAttribute(IntPtr h,int a,out int v,int s);
  [DllImport("kernel32")] static extern IntPtr OpenProcess(uint a,bool i,uint pid);
  [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h,int f,StringBuilder s,ref int n);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr h,ref int n,StringBuilder s);
  [DllImport("advapi32")] static extern bool OpenProcessToken(IntPtr p,uint a,out IntPtr t);
  [DllImport("advapi32")] static extern bool GetTokenInformation(IntPtr t,int c,out int v,int l,out int r);
  static readonly Dictionary<string,string> Known=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"WINWORD","Word"},{"EXCEL","Excel"},{"POWERPNT","PowerPoint"},{"OUTLOOK","Outlook (classic)"},{"SystemSettings","Windows Settings"},{"Taskmgr","Task Manager"}};

  // Win11 records every app that ever showed a tray icon under HKCU\Control Panel\NotifyIconSettings (ExecutablePath).
  public static HashSet<string> TrayOwners(){ var set=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    try{ using(var k=Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings")){ if(k!=null) foreach(var n in k.GetSubKeyNames()){ using(var s=k.OpenSubKey(n)){ var p=s==null?null:s.GetValue("ExecutablePath") as string; if(string.IsNullOrEmpty(p)) continue;
            var f=Path.GetFileNameWithoutExtension(p.Replace('/','\\')); if(!string.IsNullOrEmpty(f)) set.Add(f); } } } }catch{}
    set.Remove("TrayHelper"); foreach(var q in Catalog.TrayButQuits) set.Remove(q); foreach(var pr in Catalog.All) if(!pr.Native&&pr.ProcName!=null) set.Remove(pr.ProcName); return set; }
  static bool Elevated(uint pid){ var ph=OpenProcess(0x1000,false,pid); if(ph==IntPtr.Zero) return false;
    try{ IntPtr t; if(!OpenProcessToken(ph,0x0008,out t)) return true; try{ int v,r; if(GetTokenInformation(t,20,out v,4,out r)) return v!=0; }finally{ CloseHandle(t); } }finally{ CloseHandle(ph); } return false; }
  public sealed class Cand{ public AppTarget App; public bool Suggest; }
  // User-facing = visible, titled, not cloaked, not a tool window, app-window or unowned, reasonably large.
  public static List<Cand> Scan(List<string> excludedLog){
    var res=new List<Cand>(); int me=Process.GetCurrentProcess().Id; bool selfAdmin=Elevated((uint)me);
    var managed=new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach(var a in TrayHelper.Apps) if(a.ProcName!=null) managed.Add(a.ProcName);
    var excl=new HashSet<string>(TrayHelper.Exclude,StringComparer.OrdinalIgnoreCase); var tray=TrayOwners(); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var nativeProcs=new HashSet<string>(Catalog.All.Where(p=>p.Native).Select(p=>p.ProcName),StringComparer.OrdinalIgnoreCase);
    EnumWindows((h,l)=>{ if(!IsWindowVisible(h)) return true;
      var t=new StringBuilder(256); GetWindowText(h,t,256); if(t.Length==0) return true;
      if(!AltTab(h)) return true;
      TrayHelper.RECT rc; if(!IsIconic(h)&&(!GetWindowRect(h,out rc)||rc.r-rc.l<200||rc.b-rc.t<120)) return true;
      var c=new StringBuilder(256); GetClassName(h,c,256); var cls=c.ToString(); if(cls=="Progman"||cls=="Shell_TrayWnd"||cls=="WorkerW") return true;
      uint pid; GetWindowThreadProcessId(h,out pid); if(pid==me) return true;
      string pn=null; try{ using(var p=Process.GetProcessById((int)pid)) pn=p.ProcessName; }catch{} if(pn==null) return true;
      bool uwp=pn.Equals("ApplicationFrameHost",StringComparison.OrdinalIgnoreCase); string key=uwp?"uwp:"+t:pn; if(!seen.Add(key)) return true;
      if(!uwp){ if(managed.Contains(pn)){ excludedLog.Add(pn+": already managed"); return true; } if(excl.Contains(pn)){ excludedLog.Add(pn+": exclusion list"); return true; }
        if(nativeProcs.Contains(pn)){ excludedLog.Add(pn+": natively supported (built-in list)"); return true; } if(tray.Contains(pn)){ excludedLog.Add(pn+": has its own tray icon (NotifyIconSettings)"); return true; } }
      string exe=null, aumid=null; var ph=OpenProcess(0x1000,false,pid);
      if(ph!=IntPtr.Zero){ try{ var sb=new StringBuilder(1024); int n=sb.Capacity; if(QueryFullProcessImageName(ph,0,sb,ref n)) exe=sb.ToString(); var sa=new StringBuilder(256); int na=sa.Capacity; try{ if(GetApplicationUserModelId(ph,ref na,sa)==0) aumid=sa.ToString(); }catch(EntryPointNotFoundException){} }finally{ CloseHandle(ph); } }
      bool elev=!selfAdmin&&Elevated(pid);
      var a=new AppTarget(); a.ProcName=pn; a.WinClass=cls; a.WinTitle=uwp?t.ToString():""; a.ExePath=aumid!=null?@"shell:AppsFolder\"+aumid:exe; a.IconExe=exe; a.Enabled=true; a.Preset=false; a.Detected=true;
      string nm; if(!Known.TryGetValue(pn,out nm)){ nm=null; try{ if(exe!=null){ var vi=FileVersionInfo.GetVersionInfo(exe); nm=!string.IsNullOrEmpty(vi.FileDescription)?vi.FileDescription:vi.ProductName; } }catch{} } if(uwp) nm=t.ToString(); a.Name=string.IsNullOrEmpty(nm)?pn:nm;
      a.Note=elev?"needs TrayHelper run as admin (app is elevated)":uwp?"Store/UWP window, hiding may be unreliable":null;
      res.Add(new Cand{ App=a, Suggest=!elev&&!uwp }); return true; },IntPtr.Zero);
    return res; }
  public static void WriteReport(string path){ var ex=new List<string>(); var r=Scan(ex); var sb=new StringBuilder();
    sb.AppendLine("Would list:"); foreach(var c in r) sb.AppendLine("  "+(c.Suggest?"[x] ":"[ ] ")+c.App.Name+"  ("+c.App.ProcName+", "+c.App.WinClass+")"+(c.App.Note!=null?"  - "+c.App.Note:""));
    sb.AppendLine("Excluded:"); foreach(var e in ex.Distinct()) sb.AppendLine("  "+e);
    sb.AppendLine("Tray owners (NotifyIconSettings): "+string.Join(", ",TrayOwners().OrderBy(x=>x)));
    File.WriteAllText(path,sb.ToString()); }
}
