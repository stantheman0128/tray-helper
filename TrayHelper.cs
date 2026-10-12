// Tray Helper (POC) - close-to-tray + watchdog for multiple Windows apps. MIT License. Unofficial.
// Generalized from grokbot-background-helper (GrokBotTray.cs). C# 5 / .NET Framework csc.
using System; using System.Collections.Generic; using System.Diagnostics; using System.Drawing; using System.IO; using System.Reflection;
using System.Runtime.InteropServices; using System.Text; using System.Threading; using System.Windows.Forms;

sealed class CloseSnap{ public readonly int l,t,r,b; public CloseSnap(int l,int t,int r,int b){ this.l=l; this.t=t; this.r=r; this.b=b; } }

sealed class AppTarget{
  public string Name, ExePath, ProcName, WinClass, WinTitle; public bool Enabled=true, Watchdog=false;
  public IntPtr hwnd; public volatile CloseSnap snap;
  public bool userQuit, quitSawGone, seenRunning, hideNew; public int launchedPid;
  public DateTime goneSince=DateTime.MaxValue, lastLaunch=DateTime.MinValue, hiddenAt=DateTime.MinValue;
  public int aL,aT,aR,aB;
  public bool Running{ get{ return hwnd!=IntPtr.Zero && TrayHelper.IsWindow(hwnd); } }
  public bool Hidden{ get{ return Running && !TrayHelper.IsWindowVisible(hwnd); } }
}

class TrayHelper : ApplicationContext {
  const long MaxLogBytes=256*1024; const int WM_HIDE=0x8001;
  const int SM_CXSIZE=30, SM_CYSIZE=31, SM_CXFRAME=32, SM_CYFRAME=33, SM_CXPADDEDBORDER=92;
  static string Dir, IniPath, dpiMode="unaware"; static int MyPid;
  static List<AppTarget> Apps=new List<AppTarget>();
  volatile AppTarget[] snapApps=new AppTarget[0];

  delegate IntPtr HookProc(int n, IntPtr w, IntPtr l); delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32",SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id, HookProc p, IntPtr m, uint t);
  [DllImport("user32")] static extern IntPtr CallNextHookEx(IntPtr h,int n,IntPtr w,IntPtr l);
  [DllImport("user32")] static extern bool UnhookWindowsHookEx(IntPtr h);
  [DllImport("kernel32")] static extern IntPtr GetModuleHandle(string n);
  [DllImport("user32")] static extern IntPtr WindowFromPoint(POINT p);
  [DllImport("user32")] static extern IntPtr GetAncestor(IntPtr h,uint f);
  [DllImport("user32")] static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint f,uint t,out IntPtr r);
  [DllImport("user32")] static extern bool ShowWindow(IntPtr h,int c);
  [DllImport("user32")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32")] public static extern bool IsWindow(IntPtr h);
  [DllImport("user32")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32")] static extern bool EnumWindows(EnumProc p,IntPtr l);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
  [DllImport("user32")] static extern bool GetWindowRect(IntPtr h,out RECT r);
  [DllImport("user32")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr p,IntPtr a,string c,string t);
  [DllImport("user32")] static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("user32")] static extern bool SetProcessDpiAwarenessContext(IntPtr c);
  [DllImport("user32")] static extern bool SetProcessDPIAware();
  [DllImport("user32")] static extern uint GetDpiForWindow(IntPtr h);
  [DllImport("user32")] static extern int GetSystemMetrics(int n);
  [DllImport("user32")] static extern int GetSystemMetricsForDpi(int n,uint dpi);
  [DllImport("shcore")] static extern int SetProcessDpiAwareness(int awareness);
  [StructLayout(LayoutKind.Sequential)] struct POINT{public int x,y;}
  [StructLayout(LayoutKind.Sequential)] public struct RECT{public int l,t,r,b;}

  sealed class UiWin:NativeWindow{ readonly TrayHelper o; public UiWin(TrayHelper o){ this.o=o; }
    public void Open(){ var cp=new CreateParams(); cp.Caption="TrayHelper.msg"; cp.Parent=new IntPtr(-3); CreateHandle(cp); }
    protected override void WndProc(ref Message m){ if(m.Msg==0x8002){ try{ o.ShowSettings(); }catch{} } if(m.Msg==WM_HIDE){ try{ o.OnUiHide(); }catch{ o.hookFault=true; } } base.WndProc(ref m); } }

  NotifyIcon tray; ContextMenuStrip menu; System.Windows.Forms.Timer timer; UiWin ui; Icon ownedIcon;
  IntPtr uiHwnd, mHook, kHook; HookProc mProc,kProc; SynchronizationContext sync;
  volatile AppTarget pendApp; int pendKind, pendX, pendY, hideQueued, cleaned;
  bool swallowUp, altF4Held, booted, exiting, inTick, dpiWinMissing, metricDpiMissing; public bool hookFault; bool hooksSuspect;

  // ---------- config ----------
  static bool ParseBool(string v){ v=v.ToLowerInvariant(); return v=="1"||v=="true"||v=="yes"||v=="on"; }
  static void LoadConfig(){
    if(!File.Exists(IniPath)){ Log("no ini at "+IniPath); return; }
    AppTarget cur=null;
    foreach(var raw in File.ReadAllLines(IniPath)){ var line=raw.Trim(); if(line.Length==0||line[0]==';'||line[0]=='#') continue;
      if(line[0]=='['){ cur=null; var s=line.Trim('[',']').Trim();
        if(s.StartsWith("App:",StringComparison.OrdinalIgnoreCase)){ cur=new AppTarget(); cur.Name=s.Substring(4).Trim(); cur.ProcName=cur.Name; cur.WinTitle=cur.Name; cur.WinClass="Chrome_WidgetWin_1"; Apps.Add(cur); }
        continue; }
      if(cur==null) continue;
      int i=line.IndexOf('='); if(i<1) continue; var k=line.Substring(0,i).Trim().ToLowerInvariant(); var v=Environment.ExpandEnvironmentVariables(line.Substring(i+1).Trim());
      if(k=="enabled") cur.Enabled=ParseBool(v); else if(k=="watchdog") cur.Watchdog=ParseBool(v);
      else if(v.Length==0) continue;
      else if(k=="exepath") cur.ExePath=v; else if(k=="processname") cur.ProcName=v; else if(k=="windowclass") cur.WinClass=v; else if(k=="windowtitle") cur.WinTitle=v; }
  }
  static void Log(string s){ try{ if(string.IsNullOrEmpty(Dir)) return; var path=Path.Combine(Dir,"tray-helper.log"); var fi=new FileInfo(path);
    if(fi.Exists&&fi.Length>MaxLogBytes){ var bak=path+".old"; try{ if(File.Exists(bak)) File.Delete(bak); File.Move(path,bak); }catch{} }
    File.AppendAllText(path,DateTime.Now.ToString("s")+" "+s+"\r\n"); }catch{} }

  static bool TryCtx(int c,string n){ try{ if(SetProcessDpiAwarenessContext(new IntPtr(c))){ dpiMode=n; return true; } }catch(EntryPointNotFoundException){} return false; }
  static bool TryAw(int l,string n){ try{ if(SetProcessDpiAwareness(l)==0){ dpiMode=n; return true; } }catch(EntryPointNotFoundException){}catch(DllNotFoundException){} return false; }
  static void SetDpi(){ if(TryCtx(-4,"per-monitor-v2")||TryCtx(-3,"per-monitor")||TryAw(2,"per-monitor")||TryCtx(-2,"system")||TryAw(1,"system")) return;
    try{ if(SetProcessDPIAware()) dpiMode="system"; }catch(EntryPointNotFoundException){} }

  // ---------- ui ----------
  TrayHelper(){
    tray=new NotifyIcon();
    foreach(var a in Apps){ if(a.ExePath!=null&&File.Exists(a.ExePath)){ try{ ownedIcon=Icon.ExtractAssociatedIcon(a.ExePath); }catch{} if(ownedIcon!=null) break; } }
    tray.Icon=ownedIcon??SystemIcons.Application; tray.Text="TrayHelper";
    menu=new ContextMenuStrip(); menu.Opening+=(s,e)=>BuildMenu(); BuildMenu();
    tray.ContextMenuStrip=menu; tray.MouseClick+=(s,e)=>{ if(e.Button==MouseButtons.Left) ShowSettings(); }; tray.Visible=true;
    mProc=MouseHook; kProc=KeyHook;
    timer=new System.Windows.Forms.Timer(); timer.Interval=1000; timer.Tick+=(s,e)=>Tick(); timer.Start();
    Application.ApplicationExit+=delegate{ Cleanup(); };
    AppDomain.CurrentDomain.ProcessExit+=delegate{ try{ UnhookOnly(); }catch{} };
  }
  static string State(AppTarget a){ if(!a.Running) return "not running"; return a.Hidden?"hidden":"visible"; }
  void BuildMenu(){
    menu.Items.Clear();
    menu.Items.Add("Settings...",null,(s,e)=>ShowSettings()); menu.Items.Add(new ToolStripSeparator());
    foreach(var a0 in Apps){ var a=a0; var it=new ToolStripMenuItem("Show "+a.Name+"  ["+State(a)+(a.Enabled?"":", off")+"]",null,(s,e)=>ShowApp(a)); menu.Items.Add(it); }
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Exit (restore windows)",null,(s,e)=>ExitHelper());
  }
  SettingsForm settings;
  public void ShowSettings(){ if(settings==null||settings.IsDisposed) settings=new SettingsForm(this); settings.Show(); settings.WindowState=FormWindowState.Normal; settings.Activate(); settings.RefreshRows(); }
  // ---- API used by SettingsForm ----
  public List<AppTarget> AppList{ get{ return Apps; } }
  public void SetEnabled(AppTarget a,bool on){ a.Enabled=on; if(!on) a.snap=null; SaveAll(); Log(a.Name+": enabled="+on); }
  public void SetWatchdog(AppTarget a,bool on){ a.Watchdog=on; SaveAll(); Log(a.Name+": watchdog="+on); }
  public void Show_(AppTarget a){ ShowApp(a); }
  public void AddApp(AppTarget a){ Apps.Add(a); snapApps=Apps.ToArray(); SaveAll(); Log("added "+a.Name); }
  public void RemoveApp(AppTarget a){ a.Enabled=false; a.snap=null; if(a.Hidden) ShowWindow(a.hwnd,5); Apps.Remove(a); snapApps=Apps.ToArray(); SaveAll(); Log("removed "+a.Name); }
  public static string StateOf(AppTarget a){ return State(a); }
  static void SaveAll(){ try{ var sb=new StringBuilder();
    sb.AppendLine("; TrayHelper config. One [App:Name] section per app. Edited by the settings window.");
    sb.AppendLine("; ExePath may be a normal exe or shell:AppsFolder\\<AUMID> for Store/MSIX apps.");
    foreach(var a in Apps){ sb.AppendLine(); sb.AppendLine("[App:"+a.Name+"]"); sb.AppendLine("Enabled="+(a.Enabled?"true":"false"));
      sb.AppendLine("ExePath="+(a.ExePath??"")); sb.AppendLine("ProcessName="+a.ProcName); sb.AppendLine("WindowClass="+a.WinClass); sb.AppendLine("WindowTitle="+a.WinTitle); sb.AppendLine("Watchdog="+(a.Watchdog?"true":"false")); }
    File.WriteAllText(IniPath,sb.ToString()); }catch(Exception ex){ Log("save ini "+ex.Message); } }
  // ---------- window tracking ----------
  static bool PidIs(uint pid,string name){ try{ using(var p=Process.GetProcessById((int)pid)) return string.Equals(p.ProcessName,name,StringComparison.OrdinalIgnoreCase); }catch{ return false; } }
  static IntPtr FindMain(AppTarget a){ IntPtr found=IntPtr.Zero; EnumWindows((h,l)=>{
      var c=new StringBuilder(256); GetClassName(h,c,256); if(c.ToString()!=a.WinClass) return true;
      var t=new StringBuilder(256); GetWindowText(h,t,256); if(t.ToString()!=a.WinTitle) return true;
      uint pid; GetWindowThreadProcessId(h,out pid); if(PidIs(pid,a.ProcName)){ found=h; return false; } return true; },IntPtr.Zero); return found; }
  static bool ClassWindowAlive(AppTarget a){ bool f=false; EnumWindows((h,l)=>{ var c=new StringBuilder(256); GetClassName(h,c,256); if(c.ToString()!=a.WinClass) return true;
      uint pid; GetWindowThreadProcessId(h,out pid); if(PidIs(pid,a.ProcName)){ f=true; return false; } return true; },IntPtr.Zero); return f; }
  static bool BlocksRelaunch(AppTarget a){
    if(a.launchedPid>0){ try{ using(var p=Process.GetProcessById(a.launchedPid)) if(!p.HasExited) return true; }catch(ArgumentException){ a.launchedPid=0; }catch{ return true; } }
    if(ClassWindowAlive(a)) return true;
    Process[] ps=null; try{ ps=Process.GetProcessesByName(a.ProcName); foreach(var p in ps){ try{ if(p.Id!=MyPid&&!p.HasExited&&(DateTime.Now-p.StartTime).TotalSeconds<20) return true; }catch{} } }
    catch{} finally{ if(ps!=null) foreach(var p in ps) try{ p.Dispose(); }catch{} }
    return false; }
  static bool StartApp(AppTarget a){
    if(string.IsNullOrEmpty(a.ExePath)) return false;
    var psi=new ProcessStartInfo(); psi.UseShellExecute=true;
    if(a.ExePath.StartsWith("shell:",StringComparison.OrdinalIgnoreCase)){ psi.FileName="explorer.exe"; psi.Arguments="\""+a.ExePath+"\""; }
    else{ psi.FileName=a.ExePath; var wd=Path.GetDirectoryName(a.ExePath); if(!string.IsNullOrEmpty(wd)) psi.WorkingDirectory=wd; }
    Process p=null; try{ p=Process.Start(psi); if(p!=null&&psi.FileName!="explorer.exe"){ try{ a.launchedPid=p.Id; }catch{} } return true; }
    finally{ if(p!=null) p.Dispose(); } }

  void Tick(){
    if(exiting||inTick) return; inTick=true; var sw=Stopwatch.StartNew();
    try{
      if(!booted){ booted=true; try{ ui=new UiWin(this); ui.Open(); uiHwnd=ui.Handle; }catch(Exception ex){ Log("ui "+ex.Message); } sync=SynchronizationContext.Current; Log("started dpi="+dpiMode+" apps="+Apps.Count); }
      EnsureHooks();
      foreach(var a in Apps) TickApp(a);
      snapApps=Apps.ToArray();
      if(sw.ElapsedMilliseconds>250) hooksSuspect=true;
      if(hooksSuspect||hookFault) EnsureHooks();
      WriteStatus();
    }catch(Exception ex){ Log("tick "+ex.Message); hooksSuspect=true; }
    finally{ inTick=false; } }
  void TickApp(AppTarget a){
    if(a.hwnd==IntPtr.Zero||!IsWindow(a.hwnd)){ var h=FindMain(a); if(h!=a.hwnd){ a.hwnd=h; if(h==IntPtr.Zero) a.snap=null; Log(a.Name+": attached hwnd="+h); } }
    bool running=a.hwnd!=IntPtr.Zero;
    if(running){ a.seenRunning=true; a.goneSince=DateTime.MaxValue; if(a.userQuit&&a.quitSawGone&&IsWindowVisible(a.hwnd)){ a.userQuit=false; a.quitSawGone=false; } }
    else{ if(a.goneSince==DateTime.MaxValue&&a.seenRunning) a.goneSince=DateTime.Now; if(a.userQuit) a.quitSawGone=true; }
    if(running&&a.hideNew&&IsWindowVisible(a.hwnd)){ ShowWindow(a.hwnd,0); a.hideNew=false; a.hiddenAt=DateTime.Now; a.snap=null; Log(a.Name+": hid relaunched window"); }
    if(!running&&a.Watchdog&&a.Enabled&&a.seenRunning&&!a.userQuit&&(DateTime.Now-a.goneSince).TotalSeconds>=2&&(DateTime.Now-a.lastLaunch).TotalSeconds>15){
      if(BlocksRelaunch(a)) a.lastLaunch=DateTime.Now.AddSeconds(-12);
      else{ a.lastLaunch=DateTime.Now; try{ a.hideNew=StartApp(a); Log(a.Name+(a.hideNew?": watchdog relaunched":": launch fail")); }catch(Exception ex){ a.hideNew=false; Log(a.Name+": launch fail "+ex.Message); } hooksSuspect=true; } }
    if(running&&a.Enabled&&!a.userQuit&&IsWindowVisible(a.hwnd)) UpdateCloseRect(a); else a.snap=null; }
  void WriteStatus(){ try{ var sb=new StringBuilder();
    sb.AppendFormat("time={0}\r\nhelperPid={1}\r\ndpi={2}\r\nmouseHook={3}\r\nkeyHook={4}\r\n",DateTime.Now.ToString("s"),MyPid,dpiMode,mHook,kHook);
    foreach(var a in Apps){ var s=a.snap; sb.AppendFormat("[{0}] enabled={1} watchdog={2} hwnd={3} state={4} closeRect={5}\r\n",a.Name,a.Enabled,a.Watchdog,a.hwnd,State(a),s==null?"none":(s.l+","+s.t+","+s.r+","+s.b)); }
    File.WriteAllText(Path.Combine(Dir,"status.txt"),sb.ToString()); }catch{} }

  uint WinDpi(IntPtr h){ if(dpiWinMissing) return 96; try{ uint d=GetDpiForWindow(h); return d==0?96:d; }catch(EntryPointNotFoundException){ dpiWinMissing=true; return 96; } }
  int Metric(int i,uint dpi){ if(!metricDpiMissing){ try{ return GetSystemMetricsForDpi(i,dpi); }catch(EntryPointNotFoundException){ metricDpiMissing=true; } } return GetSystemMetrics(i); }
  static bool Fits16(int v){ return v>=short.MinValue&&v<=short.MaxValue; }
  static IntPtr Pack(int x,int y){ uint v=((uint)x&0xFFFFu)|(((uint)y&0xFFFFu)<<16); unchecked{ return new IntPtr((int)v); } }
  static int Hit(IntPtr h,int x,int y){ IntPtr r; return SendMessageTimeout(h,0x84,IntPtr.Zero,Pack(x,y),2,15,out r)!=IntPtr.Zero?(int)r:-1; }
  void Publish(AppTarget a,int l,int t,int r,int b){ var p=a.snap; if(p!=null&&p.l==l&&p.t==t&&p.r==r&&p.b==b) return; a.snap=new CloseSnap(l,t,r,b); Log(a.Name+": closeRect="+l+","+t+","+r+","+b); }
  void UpdateCloseRect(AppTarget a){
    RECT rc; IntPtr h=a.hwnd; if(!GetWindowRect(h,out rc)||rc.r<=rc.l||rc.b<=rc.t){ a.snap=null; return; }
    uint dpi=WinDpi(h); int bw=Math.Max(1,Metric(SM_CXSIZE,dpi)), bh=Math.Max(1,Metric(SM_CYSIZE,dpi)), fx=Math.Max(0,Metric(SM_CXFRAME,dpi)), fy=Math.Max(0,Metric(SM_CYFRAME,dpi)), pad=Math.Max(0,Metric(SM_CXPADDEDBORDER,dpi));
    int step=Math.Max(2,bw/8), reach=Math.Max(bw*3+fx,step*4), yA=rc.t+Math.Max(1,bh/3), yB=rc.t+fy+pad+Math.Max(1,bh/2);
    int hL=int.MaxValue,hR=int.MinValue,hY=yA,probes=0; bool sawFit=false; var sw=Stopwatch.StartNew();
    for(int pass=0;pass<2&&hR==int.MinValue;pass++){ int y=pass==0?yA:yB; if(pass==1&&y==yA) break;
      for(int x=rc.r-2;x>rc.l&&x>rc.r-reach;x-=step){ if(probes>=18||sw.ElapsedMilliseconds>=40) break; if(!Fits16(x)||!Fits16(y)) continue;
        sawFit=true; probes++; if(Hit(h,x,y)==20){ if(x<hL) hL=x; if(x>hR) hR=x; hY=y; } else if(hR!=int.MinValue) break; } }
    if(hR!=int.MinValue){ int mid=(hL+hR)/2, btm=hY;
      for(int yy=hY;yy<hY+bh+fy+pad;yy+=step){ if(probes>=18||sw.ElapsedMilliseconds>=40||!Fits16(mid)||!Fits16(yy)) break; probes++; if(Hit(h,mid,yy)==20) btm=yy; else break; }
      a.aL=rc.l; a.aT=rc.t; a.aR=rc.r; a.aB=rc.b; Publish(a,hL-2,rc.t,rc.r,btm+3); return; }
    if(!sawFit){ int w2=Math.Max(bw,20*(int)dpi/96), h2=Math.Max(bh+fy+pad,16*(int)dpi/96); a.aL=rc.l; a.aT=rc.t; a.aR=rc.r; a.aB=rc.b; Publish(a,rc.r-w2-fx,rc.t,rc.r,rc.t+h2); return; }
    if(a.aR!=a.aL&&(rc.l!=a.aL||rc.t!=a.aT||rc.r!=a.aR||rc.b!=a.aB)) a.snap=null; }

  // ---------- hooks (minimal: no IO, defer to UI thread) ----------
  AppTarget HitApp(int x,int y){ var arr=snapApps; for(int i=0;i<arr.Length;i++){ var s=arr[i].snap; if(s!=null&&x>=s.l&&x<s.r&&y>=s.t&&y<s.b) return arr[i]; } return null; }
  void PostHide(AppTarget a,int kind,int x,int y){
    if(Interlocked.CompareExchange(ref hideQueued,1,0)!=0) return;
    pendApp=a; pendKind=kind; pendX=x; pendY=y;
    if(uiHwnd!=IntPtr.Zero&&PostMessage(uiHwnd,(uint)WM_HIDE,IntPtr.Zero,IntPtr.Zero)) return;
    if(sync!=null) sync.Post(delegate{ OnUiHide(); },null); else hideQueued=0; }
  IntPtr MouseHook(int n,IntPtr w,IntPtr l){
    try{ if(n>=0&&l!=IntPtr.Zero){ int msg=(int)w;
        if(msg==0x201||msg==0x202){ int x=Marshal.ReadInt32(l,0), y=Marshal.ReadInt32(l,4);
          if(msg==0x201){ if(HitApp(x,y)!=null){ swallowUp=true; return (IntPtr)1; } }
          else if(swallowUp){ swallowUp=false; var a=HitApp(x,y); if(a!=null) PostHide(a,1,x,y); return (IntPtr)1; } } } }catch{ hookFault=true; }
    return CallNextHookEx(mHook,n,w,l); }
  IntPtr KeyHook(int n,IntPtr w,IntPtr l){
    try{ if(n>=0&&l!=IntPtr.Zero){ int msg=(int)w;
        if((msg==0x100||msg==0x104||msg==0x101||msg==0x105)&&Marshal.ReadInt32(l,0)==0x73){
          int flags=Marshal.ReadInt32(l,8); bool up=(flags&0x80)!=0, alt=(flags&0x20)!=0;
          if(up){ if(altF4Held){ altF4Held=false; return (IntPtr)1; } }
          else if(alt){ var fg=GetForegroundWindow(); var arr=snapApps; AppTarget hitA=null;
            for(int i=0;i<arr.Length;i++) if(arr[i].Enabled&&!arr[i].userQuit&&arr[i].hwnd!=IntPtr.Zero&&arr[i].hwnd==fg){ hitA=arr[i]; break; }
            if(hitA!=null){ if(!altF4Held){ altF4Held=true; PostHide(hitA,2,0,0); } return (IntPtr)1; }
            if(altF4Held) altF4Held=false; } } } }catch{ hookFault=true; }
    return CallNextHookEx(kHook,n,w,l); }
  public void OnUiHide(){
    var a=pendApp; int kind=pendKind, x=pendX, y=pendY; pendApp=null; pendKind=0; Interlocked.Exchange(ref hideQueued,0);
    if(exiting||a==null||kind==0) return; IntPtr h=a.hwnd;
    if(h==IntPtr.Zero||!IsWindow(h)||!IsWindowVisible(h)) return;
    if(kind==1){ var s=a.snap; if(s==null||!(x>=s.l&&x<s.r&&y>=s.t&&y<s.b)){ Log(a.Name+": skip, rect moved"); return; }
      var p=new POINT(); p.x=x; p.y=y; var under=WindowFromPoint(p); var root=under==IntPtr.Zero?IntPtr.Zero:GetAncestor(under,2);
      if(root!=h){ Log(a.Name+": skip, under="+under+" root="+root+" at "+x+","+y); return; } }
    ShowWindow(h,0); a.snap=null; a.hiddenAt=DateTime.Now; Log(a.Name+": hid ("+(kind==1?"X":"alt+f4")+")"); }

  void ShowApp(AppTarget a){ a.userQuit=false; a.quitSawGone=false; a.hideNew=false;
    if(a.hwnd==IntPtr.Zero||!IsWindow(a.hwnd)) a.hwnd=FindMain(a);
    if(a.hwnd!=IntPtr.Zero){ ShowWindow(a.hwnd,5); ShowWindow(a.hwnd,9); SetForegroundWindow(a.hwnd); Log(a.Name+": shown"); }
    else{ try{ a.lastLaunch=DateTime.Now; if(!StartApp(a)) Log(a.Name+": launch fail"); }catch(Exception ex){ Log(a.Name+": launch fail "+ex.Message); } } }
  void QuitApp(AppTarget a){ a.userQuit=true; a.quitSawGone=false; a.snap=null; Log(a.Name+": user quit");
    if(a.hwnd!=IntPtr.Zero&&IsWindow(a.hwnd)){ ShowWindow(a.hwnd,5); PostMessage(a.hwnd,0x10,IntPtr.Zero,IntPtr.Zero); } }

  void EnsureHooks(){ if(exiting) return; if(!hookFault&&!hooksSuspect&&mHook!=IntPtr.Zero&&kHook!=IntPtr.Zero) return;
    hookFault=false; hooksSuspect=false; UnhookOnly(); var mod=GetModuleHandle(null);
    mHook=SetWindowsHookEx(14,mProc,mod,0); kHook=SetWindowsHookEx(13,kProc,mod,0);
    if(mHook==IntPtr.Zero||kHook==IntPtr.Zero) Log("hook install failed"); }
  void UnhookOnly(){ if(mHook!=IntPtr.Zero){ UnhookWindowsHookEx(mHook); mHook=IntPtr.Zero; } if(kHook!=IntPtr.Zero){ UnhookWindowsHookEx(kHook); kHook=IntPtr.Zero; } }
  void Cleanup(){ if(Interlocked.Exchange(ref cleaned,1)==1) return; exiting=true;
    try{ if(timer!=null){ timer.Stop(); timer.Dispose(); timer=null; } }catch{} try{ UnhookOnly(); }catch{}
    try{ if(ui!=null){ ui.DestroyHandle(); ui=null; } }catch{}
    try{ if(tray!=null){ tray.Visible=false; tray.Icon=null; tray.ContextMenuStrip=null; tray.Dispose(); tray=null; } if(menu!=null){ menu.Dispose(); menu=null; } if(ownedIcon!=null) ownedIcon.Dispose(); }catch{} }
  void ExitHelper(){ foreach(var a in Apps) if(a.Hidden) ShowWindow(a.hwnd,5); Log("helper exit");
    var later=sync??SynchronizationContext.Current; if(later!=null) later.Post(delegate{ Cleanup(); ExitThread(); },null); else{ Cleanup(); ExitThread(); } }

  [STAThread] static void Main(){
    SetDpi(); MyPid=Process.GetCurrentProcess().Id;
    try{ Dir=Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); }catch{}
    if(string.IsNullOrEmpty(Dir)) Dir=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    IniPath=Path.Combine(Dir,"tray-helper.ini"); if(!File.Exists(IniPath)){ var g=new AppTarget(); g.Name="Grok Bot"; g.ExePath=Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Programs\\Grok Bot\\Grok Bot.exe"); g.ProcName="Grok Bot"; g.WinTitle="Grok Bot"; g.WinClass="Chrome_WidgetWin_1"; g.Watchdog=true; Apps.Add(g); SaveAll(); } LoadConfig();
    bool created; var mx=new Mutex(true,"Local\\TrayHelper",out created); if(!created){ mx.Dispose(); var w=FindWindowEx(new IntPtr(-3),IntPtr.Zero,null,"TrayHelper.msg"); if(w!=IntPtr.Zero) PostMessage(w,0x8002,IntPtr.Zero,IntPtr.Zero); return; } // 2nd launch opens settings
    try{ Application.EnableVisualStyles(); Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException+=delegate(object s,ThreadExceptionEventArgs e){ Log("ui "+e.Exception.Message); };
      Application.Run(new TrayHelper()); }finally{ mx.Dispose(); } }
}

sealed class SettingsForm:Form{
  readonly TrayHelper th; DataGridView grid; System.Windows.Forms.Timer t; bool filling;
  public SettingsForm(TrayHelper th){ this.th=th; Text="TrayHelper"; Width=640; Height=360; StartPosition=FormStartPosition.CenterScreen; ShowInTaskbar=true; AutoScaleMode=AutoScaleMode.Dpi;
    try{ Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); }catch{}
    grid=new DataGridView(); grid.Dock=DockStyle.Fill; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; grid.RowHeadersVisible=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect=false; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
    var c0=new DataGridViewCheckBoxColumn(); c0.HeaderText="Close to tray"; c0.FillWeight=22;
    var c1=new DataGridViewTextBoxColumn(); c1.HeaderText="App"; c1.ReadOnly=true; c1.FillWeight=40;
    var c2=new DataGridViewTextBoxColumn(); c2.HeaderText="Status"; c2.ReadOnly=true; c2.FillWeight=22;
    var c3=new DataGridViewCheckBoxColumn(); c3.HeaderText="Watchdog"; c3.FillWeight=18;
    grid.Columns.AddRange(new DataGridViewColumn[]{c0,c1,c2,c3});
    grid.CurrentCellDirtyStateChanged+=(s,e)=>{ if(grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
    grid.CellValueChanged+=(s,e)=>{ if(filling||e.RowIndex<0) return; var a=grid.Rows[e.RowIndex].Tag as AppTarget; if(a==null) return; bool v=Convert.ToBoolean(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
      if(e.ColumnIndex==0) th.SetEnabled(a,v); else if(e.ColumnIndex==3) th.SetWatchdog(a,v); };
    var bar=new FlowLayoutPanel(); bar.Dock=DockStyle.Bottom; bar.AutoSize=true; bar.Padding=new Padding(4);
    bar.Controls.Add(Btn("Show",(s,e)=>{ var a=Sel(); if(a!=null) th.Show_(a); }));
    bar.Controls.Add(Btn("Add app...",(s,e)=>AddApp()));
    bar.Controls.Add(Btn("Remove",(s,e)=>{ var a=Sel(); if(a!=null&&MessageBox.Show(this,"Remove "+a.Name+"?","TrayHelper",MessageBoxButtons.YesNo)==DialogResult.Yes){ th.RemoveApp(a); RefreshRows(); } }));
    Controls.Add(grid); Controls.Add(bar);
    t=new System.Windows.Forms.Timer(); t.Interval=1000; t.Tick+=(s,e)=>{ if(Visible) RefreshRows(); }; t.Start(); }
  static Button Btn(string txt,EventHandler h){ var b=new Button(); b.Text=txt; b.AutoSize=true; b.Click+=h; return b; }
  AppTarget Sel(){ return grid.SelectedRows.Count>0?grid.SelectedRows[0].Tag as AppTarget:null; }
  public void RefreshRows(){ filling=true; try{ var apps=th.AppList;
      bool same=grid.Rows.Count==apps.Count; for(int i=0;same&&i<apps.Count;i++) if(grid.Rows[i].Tag!=apps[i]) same=false;
      if(!same){ grid.Rows.Clear(); foreach(var a in apps){ int r=grid.Rows.Add(a.Enabled,a.Name,TrayHelper.StateOf(a),a.Watchdog); grid.Rows[r].Tag=a; } }
      else for(int i=0;i<apps.Count;i++){ var row=grid.Rows[i]; var a=apps[i]; row.Cells[2].Value=TrayHelper.StateOf(a);
        if(!grid.IsCurrentCellInEditMode){ if(!Equals(row.Cells[0].Value,a.Enabled)) row.Cells[0].Value=a.Enabled; if(!Equals(row.Cells[3].Value,a.Watchdog)) row.Cells[3].Value=a.Watchdog; } }
    }finally{ filling=false; } }
  void AddApp(){ using(var p=new PickerForm()){ if(p.ShowDialog(this)==DialogResult.OK&&p.Result!=null){
        foreach(var x in th.AppList) if(string.Equals(x.Name,p.Result.Name,StringComparison.OrdinalIgnoreCase)){ MessageBox.Show(this,x.Name+" is already managed.","TrayHelper"); return; }
        th.AddApp(p.Result); RefreshRows(); } } }
  protected override void OnFormClosing(FormClosingEventArgs e){ if(e.CloseReason==CloseReason.UserClosing){ e.Cancel=true; Hide(); return; } base.OnFormClosing(e); }
}

sealed class PickerForm:Form{
  delegate bool EnumProc(IntPtr h,IntPtr l);
  [DllImport("user32")] static extern bool EnumWindows(EnumProc p,IntPtr l);
  [DllImport("user32")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32")] static extern IntPtr GetWindow(IntPtr h,uint c);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
  [DllImport("kernel32")] static extern IntPtr OpenProcess(uint a,bool i,uint pid);
  [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h,int f,StringBuilder s,ref int n);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr h,ref int n,StringBuilder s);
  public AppTarget Result; ListView lv;
  public PickerForm(){ Text="Add app - pick a window"; Width=760; Height=420; StartPosition=FormStartPosition.CenterParent; AutoScaleMode=AutoScaleMode.Dpi;
    lv=new ListView(); lv.Dock=DockStyle.Fill; lv.View=View.Details; lv.FullRowSelect=true; lv.MultiSelect=false;
    lv.Columns.Add("Title",220); lv.Columns.Add("Process",110); lv.Columns.Add("Class",160); lv.Columns.Add("Exe / AUMID",250);
    var ok=new Button(); ok.Text="Add"; ok.Dock=DockStyle.Bottom; ok.Click+=(s,e)=>Pick(); lv.DoubleClick+=(s,e)=>Pick();
    Controls.Add(lv); Controls.Add(ok); Fill(); }
  void Fill(){ int me=Process.GetCurrentProcess().Id;
    EnumWindows((h,l)=>{ if(!IsWindowVisible(h)||GetWindow(h,4)!=IntPtr.Zero) return true;
      var t=new StringBuilder(256); GetWindowText(h,t,256); if(t.Length==0) return true;
      var c=new StringBuilder(256); GetClassName(h,c,256); uint pid; GetWindowThreadProcessId(h,out pid); if(pid==me) return true;
      string pn=null; try{ using(var p=Process.GetProcessById((int)pid)) pn=p.ProcessName; }catch{} if(pn==null) return true;
      string exe=null, aumid=null; var ph=OpenProcess(0x1000,false,pid);
      if(ph!=IntPtr.Zero){ try{ var sb=new StringBuilder(1024); int n=sb.Capacity; if(QueryFullProcessImageName(ph,0,sb,ref n)) exe=sb.ToString();
          var sa=new StringBuilder(256); int na=sa.Capacity; try{ if(GetApplicationUserModelId(ph,ref na,sa)==0) aumid=sa.ToString(); }catch(EntryPointNotFoundException){} } finally{ CloseHandle(ph); } }
      string path=aumid!=null?"shell:AppsFolder\\"+aumid:exe;
      var it=new ListViewItem(new string[]{t.ToString(),pn,c.ToString(),path??""}); lv.Items.Add(it); return true; },IntPtr.Zero); }
  void Pick(){ if(lv.SelectedItems.Count==0) return; var it=lv.SelectedItems[0];
    var a=new AppTarget(); a.WinTitle=it.SubItems[0].Text; a.Name=a.WinTitle; a.ProcName=it.SubItems[1].Text; a.WinClass=it.SubItems[2].Text; a.ExePath=it.SubItems[3].Text; a.Enabled=true; a.Watchdog=false;
    Result=a; DialogResult=DialogResult.OK; Close(); }
}
