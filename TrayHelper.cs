// Tray Helper (POC) - close-to-tray + watchdog for multiple Windows apps. MIT License. Unofficial.
// Generalized from grokbot-background-helper (GrokBotTray.cs). C# 5 / .NET Framework csc.
using System; using System.Linq; using System.Collections.Generic; using System.Diagnostics; using System.Drawing; using System.IO; using System.Reflection;
using System.Runtime.InteropServices; using System.Text; using System.Threading; using System.Windows.Forms;

sealed class CloseSnap{ public readonly int l,t,r,b; public CloseSnap(int l,int t,int r,int b){ this.l=l; this.t=t; this.r=r; this.b=b; } }

sealed class AppTarget{ public bool wasMax, wasMin; public bool geoLogged;
  public string Name, ExePath, ProcName, WinClass, WinTitle, IconExe, Note; public bool Enabled=true, Watchdog=true, Preset, Detected=true, NativeTray, gaveUp, AlwaysRestart, RestartOnCrash, Native, Notified, Geometric;
  public string Id, Hint, SettingsKeys; public bool CtrlQ, CtrlW; public bool IsGrok{ get{ return Id=="grokbot"; } }
  public volatile bool moved; public NotifyIcon Tray; public Icon TrayIcon; public Process mainProc; public List<DateTime> relaunches=new List<DateTime>(); public DateTime nextTry=DateTime.MinValue;
  public IntPtr hwnd; public volatile CloseSnap snap;
  public bool userQuit, quitSawGone, seenRunning, hideNew, Temp, vd; public int launchedPid;
  public DateTime goneSince=DateTime.MaxValue, lastLaunch=DateTime.MinValue, hiddenAt=DateTime.MinValue;
  public int aL,aT,aR,aB;
  public bool Running{ get{ return hwnd!=IntPtr.Zero && TrayHelper.IsWindow(hwnd); } }
  public bool Hidden{ get{ return Running && (vd || !TrayHelper.IsWindowVisible(hwnd)); } }
}

partial class TrayHelper : ApplicationContext {
  internal const string Version="0.7.0";
  internal static string HkRestoreAll="Ctrl+Alt+Up", HkSendToTray="Ctrl+Alt+Down"; internal static bool UwpVd=true;
  internal static List<AppTarget> Temps=new List<AppTarget>();
  internal static bool OpenMaximized=true; internal static double WinW=0,WinH=0,WinL=double.NaN,WinT=double.NaN; internal static bool WinMax=true; internal static long MaxLogBytes=256*1024; internal static int StartDelay=0; internal static List<string> Exclude=new List<string>{"Raycast","chrome","msedge","firefox","explorer","TrayHelper","Grok Bot","ShellExperienceHost","SearchHost","StartMenuExperienceHost","TextInputHost","LockApp"}; internal static bool NotifyFirstHide=true, LogVerbose=false; const int WM_HIDE=0x8001;
  const int SM_CXSIZE=30, SM_CYSIZE=31, SM_CXFRAME=32, SM_CYFRAME=33, SM_CXPADDEDBORDER=92;
  internal static string Dir, IniPath, dpiMode="unaware"; static int MyPid;
  internal static List<AppTarget> Apps=new List<AppTarget>();
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
  [DllImport("user32")] static extern bool IsIconic(IntPtr h);
  [DllImport("user32")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32")] public static extern bool IsWindow(IntPtr h);
  [DllImport("user32")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32")] static extern bool EnumWindows(EnumProc p,IntPtr l);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
  [DllImport("user32")] static extern bool GetWindowRect(IntPtr h,out RECT r);
  [DllImport("user32")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32")] static extern IntPtr GetWindow(IntPtr h,uint c);
  [DllImport("user32")] static extern bool IsZoomed(IntPtr h);
  [DllImport("user32")] static extern short GetAsyncKeyState(int k);
  delegate void WinEventProc(IntPtr hook,uint ev,IntPtr h,int obj,int child,uint th,uint t);
  [DllImport("user32")] static extern IntPtr SetWinEventHook(uint a,uint b,IntPtr m,WinEventProc p,uint pid,uint tid,uint f);
  [DllImport("user32")] static extern bool UnhookWinEvent(IntPtr h);
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
    protected override void WndProc(ref Message m){ if(m.Msg==0x0312){ try{ o.OnHotkey(m.WParam.ToInt32()); }catch(Exception ex){ Log("hotkey "+ex.Message); } } if(m.Msg==0x8002){ try{ o.ShowSettings(); }catch(Exception ex){ Log("settings "+ex); } } if(m.Msg==0x8003){ try{ o.ShowFlyout(); }catch(Exception ex){ Log("flyout "+ex); } } if(m.Msg==WM_HIDE){ try{ o.OnUiHide(); }catch{ o.hookFault=true; } } base.WndProc(ref m); } }

  NotifyIcon tray; System.Windows.Forms.Timer moveTimer; WinEventProc weProc; IntPtr weHook, fgHook; System.Windows.Forms.Timer timer; UiWin ui; Icon ownedIcon;
  IntPtr uiHwnd, mHook, kHook; HookProc mProc,kProc; SynchronizationContext sync;
  volatile AppTarget pendApp; int pendKind, pendX, pendY, hideQueued, cleaned;
  int ctrlKeyHeld; bool swallowUp, altF4Held, booted, exiting, inTick, dpiWinMissing, metricDpiMissing; public bool hookFault; bool hooksSuspect;

  // ---------- config ----------
  static bool ParseBool(string v){ v=v.ToLowerInvariant(); return v=="1"||v=="true"||v=="yes"||v=="on"; }
  static void LoadConfig(){
    if(!File.Exists(IniPath)){ Log("no ini at "+IniPath); return; }
    AppTarget cur=null; bool inGeneral=false;
    foreach(var raw in File.ReadAllLines(IniPath)){ var line=raw.Trim(); if(line.Length==0||line[0]==';'||line[0]=='#') continue;
      if(line[0]=='['){ cur=null; var s=line.Trim('[',']').Trim(); inGeneral=s.Equals("General",StringComparison.OrdinalIgnoreCase);
        if(s.StartsWith("App:",StringComparison.OrdinalIgnoreCase)){ cur=new AppTarget(); cur.Name=s.Substring(4).Trim(); cur.ProcName=cur.Name; cur.WinTitle=cur.Name; cur.WinClass="Chrome_WidgetWin_1"; Apps.Add(cur); }
        continue; }
      if(cur==null&&inGeneral){ int gi=line.IndexOf('='); if(gi<1) continue; var gk=line.Substring(0,gi).Trim().ToLowerInvariant(); var gv=line.Substring(gi+1).Trim(); int n;
        if(gk=="hotkeyrestoreall") HkRestoreAll=gv; else if(gk=="hotkeysendtotray") HkSendToTray=gv; else if(gk=="uwpvirtualdesktop") UwpVd=ParseBool(gv); else if(gk=="exclude"){ Exclude=new List<string>(gv.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim())); } else if(gk=="startdelay"&&int.TryParse(gv,out n)) StartDelay=n; else if(gk=="notifyfirsthide"||gk=="notifications") NotifyFirstHide=ParseBool(gv); else if(gk=="settingsopen") OpenMaximized=!gv.Equals("last",StringComparison.OrdinalIgnoreCase); else if(gk=="settingsbounds"){ var pp=gv.Split(','); double[] bv=new double[4]; bool ok=pp.Length>=4; for(int q=0;ok&&q<4;q++) ok=double.TryParse(pp[q],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out bv[q]); if(ok){ WinL=bv[0]; WinT=bv[1]; WinW=bv[2]; WinH=bv[3]; } if(pp.Length>=5) WinMax=pp[4]=="max"; } else if(gk=="settingssize"){ var pp=gv.Split('x'); double w,hh; if(pp.Length==2&&double.TryParse(pp[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out w)&&double.TryParse(pp[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out hh)){ WinW=w; WinH=hh; } } else if(gk=="loglevel") LogVerbose=gv.Equals("verbose",StringComparison.OrdinalIgnoreCase); else if(gk=="maxlogkb"&&int.TryParse(gv,out n)) MaxLogBytes=Math.Max(32,n)*1024L; continue; }
      if(cur==null) continue;
      int i=line.IndexOf('='); if(i<1) continue; var k=line.Substring(0,i).Trim().ToLowerInvariant(); var v=Environment.ExpandEnvironmentVariables(line.Substring(i+1).Trim());
      if(k=="enabled") cur.Enabled=ParseBool(v); else if(k=="watchdog") cur.Watchdog=ParseBool(v); else if(k=="interceptctrlq") cur.CtrlQ=ParseBool(v); else if(k=="interceptctrlw") cur.CtrlW=ParseBool(v); else if(k=="order"){} else if(k=="notified") cur.Notified=ParseBool(v); else if(k=="id") cur.Id=v; else if(k=="geometric") cur.Geometric=ParseBool(v);
      else if(k=="windowtitle") cur.WinTitle=v; else if(k=="windowclass") cur.WinClass=v;
      else if(v.Length==0) continue;
      else if(k=="exepath") cur.ExePath=v; else if(k=="processname") cur.ProcName=v; else if(k=="windowclass") cur.WinClass=v; else if(k=="windowtitle") cur.WinTitle=v; }
  }
  internal static void Log(string s){ try{ if(string.IsNullOrEmpty(Dir)) return; var path=Path.Combine(Dir,"tray-helper.log"); var fi=new FileInfo(path);
    if(fi.Exists&&fi.Length>MaxLogBytes){ var bak=path+".old"; try{ if(File.Exists(bak)) File.Delete(bak); File.Move(path,bak); }catch{} }
    File.AppendAllText(path,DateTime.Now.ToString("s")+" "+s+"\r\n"); }catch{} }

  static bool TryCtx(int c,string n){ try{ if(SetProcessDpiAwarenessContext(new IntPtr(c))){ dpiMode=n; return true; } }catch(EntryPointNotFoundException){} return false; }
  static bool TryAw(int l,string n){ try{ if(SetProcessDpiAwareness(l)==0){ dpiMode=n; return true; } }catch(EntryPointNotFoundException){}catch(DllNotFoundException){} return false; }
  static void SetDpi(){ if(TryCtx(-4,"per-monitor-v2")||TryCtx(-3,"per-monitor")||TryAw(2,"per-monitor")||TryCtx(-2,"system")||TryAw(1,"system")) return;
    try{ if(SetProcessDPIAware()) dpiMode="system"; }catch(EntryPointNotFoundException){} }

  // ---------- ui ----------
  TrayHelper(){
    tray=new NotifyIcon();
    try{ using(var st=Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico")) ownedIcon=new Icon(st,SystemInformation.SmallIconSize); }catch{}
    tray.Icon=ownedIcon??SystemIcons.Application; tray.Text="TrayHelper";
    tray.MouseUp+=(s,e)=>{ if(e.Button==MouseButtons.Left) ShowSettings(); else if(e.Button==MouseButtons.Right) ShowFlyout(); }; tray.Visible=true;
    mProc=MouseHook; kProc=KeyHook; weProc=OnWinEvent;
    moveTimer=new System.Windows.Forms.Timer(); moveTimer.Interval=60; moveTimer.Tick+=(s,e)=>{ moveTimer.Stop(); foreach(var a in Apps){ if(a.moved){ a.moved=false; try{ if(a.Enabled&&!a.Native&&a.hwnd!=IntPtr.Zero&&IsWindowVisible(a.hwnd)&&!a.userQuit) UpdateCloseRect(a); else a.snap=null; }catch{} } } };
    timer=new System.Windows.Forms.Timer(); timer.Interval=1000; timer.Tick+=(s,e)=>Tick(); timer.Start();
    Application.ApplicationExit+=delegate{ Cleanup(); };
    AppDomain.CurrentDomain.ProcessExit+=delegate{ try{ UnhookOnly(); }catch{} };
  }
  static string State(AppTarget a){ if(!a.Running) return "not running"; return a.Hidden?"hidden":"visible"; }
  public List<AppTarget> AppList{ get{ return Apps; } }
  public void SetEnabled(AppTarget a,bool on){ a.Enabled=on; if(!on) a.snap=null; SyncAppTray(a,a.Running); SaveAll(); Log(a.Name+": enabled="+on); }
  
  public void Show_(AppTarget a){ ShowApp(a); }
  public void Quit_(AppTarget a){ QuitApp(a); }
  public void Move(AppTarget a,AppTarget before){ if(a==before) return; Apps.Remove(a); int i=before==null?Apps.Count:Apps.IndexOf(before); if(i<0) i=Apps.Count; Apps.Insert(i,a); snapApps=Apps.ToArray(); SaveAll(); }
  public void OpenAppSettings(AppTarget a){ ShowApp(a); if(string.IsNullOrEmpty(a.SettingsKeys)) return;
    var t=new System.Windows.Forms.Timer(); t.Interval=900; t.Tick+=(s,e)=>{ t.Stop(); t.Dispose(); try{ if(a.hwnd!=IntPtr.Zero&&GetForegroundWindow()==a.hwnd){ SendKeys.SendWait(a.SettingsKeys); Log(a.Name+": sent settings shortcut"); } else Log(a.Name+": not foreground, settings shortcut skipped"); }catch{} }; t.Start(); }
  // Keep maximized state across hide/show: SW_SHOWMAXIMIZED if it was zoomed, never SW_RESTORE/SHOWNORMAL on it.
  // Store/UWP windows (ApplicationFrameHost) go to a hidden virtual desktop instead of SW_HIDE: no taskbar ghost, no blank frame.
  static bool FrameHost(IntPtr h){ try{ uint pid; GetWindowThreadProcessId(h,out pid); using(var p=Process.GetProcessById((int)pid)) return p.ProcessName.Equals("ApplicationFrameHost",StringComparison.OrdinalIgnoreCase); }catch{ return false; } }
  static void HideWin(AppTarget a,IntPtr h){ if(h==IntPtr.Zero) return; if(IsWindowVisible(h)&&!a.vd){ a.wasMax=IsZoomed(h); a.wasMin=IsIconic(h); }
    if(UwpVd&&!a.vd&&FrameHost(h)&&Vd.Hide(h)){ a.vd=true; Log(a.Name+": moved to the hidden desktop"); } else if(!a.vd) ShowWindow(h,0); SaveHidden(); }
  static void ShowWin(AppTarget a){ var h=a.hwnd; if(h==IntPtr.Zero) return; if(a.vd){ Vd.Show(h); a.vd=false; Vd.CleanupIfUnused(); }
    bool max=a.wasMax||IsZoomed(h); ShowWindow(h,max?3:(a.wasMin||IsIconic(h))?9:5); SaveHidden(); }
  internal static bool AnyVd(){ return Apps.Concat(Temps).Any(x=>x.vd&&x.Running); }
  // ---------- crash safety: every hidden window is written to hidden.txt so nothing is lost if TrayHelper dies ----------
  static string HiddenPath{ get{ return Path.Combine(Dir??"","hidden.txt"); } }
  static readonly object hiddenLock=new object();
  internal static void SaveHidden(){ try{ var sb=new StringBuilder();
      foreach(var a in Apps.Concat(Temps).ToArray()){ var h=a.hwnd; if(h==IntPtr.Zero||!IsWindow(h)||(!a.vd&&IsWindowVisible(h))) continue; uint pid; GetWindowThreadProcessId(h,out pid); sb.AppendLine(h.ToInt64()+"\t"+pid+"\t"+(a.wasMax?1:0)+"\t"+a.ProcName+"\t"+(a.Temp?"t":"")+(a.vd?"v":"")); }
      lock(hiddenLock){ if(sb.Length==0){ if(File.Exists(HiddenPath)) File.Delete(HiddenPath); } else File.WriteAllText(HiddenPath,sb.ToString()); } }catch{} }
  sealed class HiddenRec{ public IntPtr H; public uint Pid; public bool Max, Temp, Vd; public string Proc; }
  static List<HiddenRec> ReadHidden(){ var r=new List<HiddenRec>(); try{ if(!File.Exists(HiddenPath)) return r; foreach(var ln in File.ReadAllLines(HiddenPath)){ var f=ln.Split('\t'); long hv; uint pid; if(f.Length<4||!long.TryParse(f[0],out hv)||!uint.TryParse(f[1],out pid)) continue;
        var h=new IntPtr(hv); uint now; if(!IsWindow(h)) continue; GetWindowThreadProcessId(h,out now); string fl=f.Length>4?f[4]:""; bool v=fl.Contains("v"); if(now!=pid||(!v&&IsWindowVisible(h))) continue; r.Add(new HiddenRec{ H=h, Pid=pid, Max=f[2]=="1", Proc=f[3], Temp=fl.Contains("t"), Vd=v }); } }catch{} return r; }
  // Startup: windows of managed apps stay hidden and are re-adopted (no popping windows); anything else is shown again.
  static void ShowRec(HiddenRec rec){ if(rec.Vd) Vd.Show(rec.H); ShowWindow(rec.H,rec.Max?3:5); }
  static void RecoverHidden(){ int adopted=0, shown=0; foreach(var rec in ReadHidden()){ var a=rec.Temp?null:Apps.FirstOrDefault(x=>x.Enabled&&!x.Native&&string.Equals(x.ProcName,rec.Proc,StringComparison.OrdinalIgnoreCase));
      if(a!=null&&(a.hwnd==IntPtr.Zero||!IsWindow(a.hwnd))){ a.hwnd=rec.H; a.wasMax=rec.Max; a.vd=rec.Vd; a.hiddenAt=DateTime.Now; adopted++; } else { ShowRec(rec); shown++; } }
    if(!AnyVd()) Vd.CleanupIfUnused();
    if(adopted+shown>0) Log("recovered hidden windows from last run: adopted="+adopted+" shown="+shown); SaveHidden(); }
  // Quit / fatal error: show everything we hid.
  static void RestoreAllHidden(){ foreach(var a in Apps.Concat(Temps).ToArray()){ try{ if(a.Hidden) ShowWin(a); }catch{} } foreach(var rec in ReadHidden()){ try{ ShowRec(rec); }catch{} } try{ Vd.CleanupIfUnused(); }catch{} try{ lock(hiddenLock){ if(File.Exists(HiddenPath)) File.Delete(HiddenPath); } }catch{} }
  public void Hide_(AppTarget a){ if(a.Running&&!a.Hidden){ HideWin(a,a.hwnd); a.snap=null; a.hiddenAt=DateTime.Now; } }
  public void Exit_(){ ExitHelper(); }
  public void AddApp(AppTarget a){ Apps.Add(a); snapApps=Apps.ToArray(); SaveAll(); Log("added "+a.Name); }
  public void RemoveApp(AppTarget a){ a.Enabled=false; a.snap=null; if(a.Hidden) ShowWin(a); Apps.Remove(a); snapApps=Apps.ToArray(); SaveAll(); Log("removed "+a.Name); }
  public static string StateOf(AppTarget a){ return State(a); }
  internal static void SaveAll(){ try{ var sb=new StringBuilder();
    sb.AppendLine("; TrayHelper config. One [App:Name] section per app. Edited by the settings window.");
    sb.AppendLine("; ExePath may be a normal exe or shell:AppsFolder\\<AUMID> for Store/MSIX apps.");
    sb.AppendLine(); sb.AppendLine("[General]"); sb.AppendLine("Exclude="+string.Join(",",Exclude)); sb.AppendLine("Notifications="+(NotifyFirstHide?"true":"false")); sb.AppendLine("HotkeyRestoreAll="+HkRestoreAll); sb.AppendLine("HotkeySendToTray="+HkSendToTray); sb.AppendLine("UwpVirtualDesktop="+(UwpVd?"true":"false")); sb.AppendLine("SettingsOpen="+(OpenMaximized?"maximized":"last")); if(WinW>0&&!double.IsNaN(WinL)) sb.AppendLine("SettingsBounds="+(int)WinL+","+(int)WinT+","+(int)WinW+","+(int)WinH+","+(WinMax?"max":"normal"));
    foreach(var a in Apps){ sb.AppendLine(); sb.AppendLine("[App:"+a.Name+"]"); if(a.Id!=null) sb.AppendLine("Id="+a.Id); if(a.CtrlQ) sb.AppendLine("InterceptCtrlQ=true"); if(a.CtrlW) sb.AppendLine("InterceptCtrlW=true"); if(a.Notified) sb.AppendLine("Notified=true"); if(a.Geometric) sb.AppendLine("Geometric=true"); sb.AppendLine("Enabled="+(a.Enabled?"true":"false"));
      sb.AppendLine("ExePath="+(a.ExePath??"")); sb.AppendLine("ProcessName="+a.ProcName); sb.AppendLine("WindowClass="+a.WinClass); sb.AppendLine("WindowTitle="+a.WinTitle); }
    File.WriteAllText(IniPath,sb.ToString()); }catch(Exception ex){ Log("save ini "+ex.Message); } }
  // ---------- window tracking ----------
    // Patterns: "a|b" = any of; "*x" = contains x; "x*" = starts with x; else exact. Empty = anything.
  static bool PatOk(string pat,string v,bool emptyNeedsText){ if(string.IsNullOrEmpty(pat)) return !emptyNeedsText||v.Length>0;
    foreach(var p0 in pat.Split('|')){ var p=p0.Trim(); if(p.Length==0) continue;
      if(p.Length>1&&p[0]=='*'){ if(v.IndexOf(p.Substring(1).TrimEnd('*'),StringComparison.OrdinalIgnoreCase)>=0) return true; }
      else if(p.Length>1&&p[p.Length-1]=='*'){ if(v.StartsWith(p.Substring(0,p.Length-1),StringComparison.OrdinalIgnoreCase)) return true; }
      else if(string.Equals(v,p,StringComparison.Ordinal)) return true; } return false; }
  public static bool TitleOk(AppTarget a,string t){ return PatOk(a.WinTitle,t,true); }
  static bool ClassOk(AppTarget a,string c){ return PatOk(a.WinClass,c,false); }
  static bool ProcOk(uint pid,string names){ try{ using(var p=Process.GetProcessById((int)pid)){ foreach(var n in (names??"").Split('|')) if(string.Equals(p.ProcessName,n.Trim(),StringComparison.OrdinalIgnoreCase)) return true; } }catch{} return false; }
  [DllImport("user32",EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr h,int i);
  // Main window = best-scoring top-level window of the app's process: process + class + title must match, then
  // prefer visible, captioned, non-tool, large windows. Splash/tooltip/helper windows of the same process lose.
  static IntPtr FindMain(AppTarget a){ IntPtr best=IntPtr.Zero; long bestScore=long.MinValue; var pidOk=new Dictionary<uint,bool>();
    EnumWindows((h,l)=>{
      if(GetWindow(h,4)!=IntPtr.Zero) return true;
      var c=new StringBuilder(256); GetClassName(h,c,256); if(!ClassOk(a,c.ToString())) return true;
      var t=new StringBuilder(256); GetWindowText(h,t,256); if(!TitleOk(a,t.ToString())) return true;
      uint pid; GetWindowThreadProcessId(h,out pid); bool ok; if(!pidOk.TryGetValue(pid,out ok)){ ok=ProcOk(pid,a.ProcName); pidOk[pid]=ok; } if(!ok) return true;
      int st=GetWindowLong(h,-16), ex=GetWindowLong(h,-20); if((ex&0x80)!=0&&(ex&0x40000)==0) return true; // tool window
      RECT rc; long area=GetWindowRect(h,out rc)?Math.Max(0L,(long)(rc.r-rc.l)*(rc.b-rc.t)):0;
      bool vis=IsWindowVisible(h); if(vis&&!IsIconic(h)&&area<200L*120) return true; // tiny visible = popup/splash
      long score=Math.Min(area,4000000L)/1000; if(vis) score+=100000; if(h==a.hwnd) score+=50000; if((st&0x00C00000)==0x00C00000) score+=20000; if((ex&0x40000)!=0) score+=10000; if(t.Length>0) score+=5000;
      if(score>bestScore){ bestScore=score; best=h; } return true; },IntPtr.Zero); return best; }
  static bool ClassWindowAlive(AppTarget a){ bool f=false; EnumWindows((h,l)=>{ var c=new StringBuilder(256); GetClassName(h,c,256); if(!ClassOk(a,c.ToString())) return true;
      uint pid; GetWindowThreadProcessId(h,out pid); if(ProcOk(pid,a.ProcName)){ f=true; return false; } return true; },IntPtr.Zero); return f; }
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
      if(!booted){ booted=true; try{ ui=new UiWin(this); ui.Open(); uiHwnd=ui.Handle; ApplyHotkeys(); }catch(Exception ex){ Log("ui "+ex.Message); } sync=SynchronizationContext.Current; try{ weHook=SetWinEventHook(0x800B,0x800B,IntPtr.Zero,weProc,0,0,0); fgHook=SetWinEventHook(0x0003,0x0003,IntPtr.Zero,weProc,0,0,0); }catch{} Log("started dpi="+dpiMode+" apps="+Apps.Count+" hooks: mouse="+(mHook!=IntPtr.Zero)+" key="+(kHook!=IntPtr.Zero));  }
      EnsureHooks();
      foreach(var a in Apps) TickApp(a); TickTemps();
      snapApps=Apps.ToArray();
      if(sw.ElapsedMilliseconds>250) hooksSuspect=true;
      if(hooksSuspect||hookFault) EnsureHooks();
      WriteStatus();
    }catch(Exception ex){ Log("tick "+ex.Message); hooksSuspect=true; }
    finally{ inTick=false; } }
  void TickApp(AppTarget a){
    if(a.hwnd==IntPtr.Zero||!IsWindow(a.hwnd)){ var h=FindMain(a); if(h!=a.hwnd){ a.hwnd=h; if(h==IntPtr.Zero) a.snap=null; else HoldProc(a); Log(a.Name+": attached hwnd="+h); } }
    bool running=a.hwnd!=IntPtr.Zero; if(running&&a.vd&&Vd.OnCurrent(a.hwnd)) a.vd=false; // brought back by hand (Task View)
    if(running){ a.seenRunning=true; a.goneSince=DateTime.MaxValue; if(a.userQuit&&a.quitSawGone&&IsWindowVisible(a.hwnd)){ a.userQuit=false; a.quitSawGone=false; } }
    else{ if(a.goneSince==DateTime.MaxValue&&a.seenRunning){ a.goneSince=DateTime.Now; CheckCleanExit(a); } if(a.userQuit) a.quitSawGone=true; }
    if(running&&a.hideNew&&IsWindowVisible(a.hwnd)){ HideWin(a,a.hwnd); a.hideNew=false; a.hiddenAt=DateTime.Now; a.snap=null; Log(a.Name+": hid relaunched window"); }
    if(!running&&a.Enabled&&!a.Native&&a.IsGrok&&!a.gaveUp&&a.seenRunning&&!a.userQuit&&(DateTime.Now-a.goneSince).TotalSeconds>=2&&DateTime.Now>=a.nextTry){
      if(BlocksRelaunch(a)) a.nextTry=DateTime.Now.AddSeconds(3);
      else{ a.relaunches.RemoveAll(t=>(DateTime.Now-t).TotalMinutes>5);
        if(a.relaunches.Count>=3){ a.gaveUp=true; Log(a.Name+": crash loop, watchdog gave up (3 relaunches in 5 min)"); }
        else{ a.relaunches.Add(DateTime.Now); a.nextTry=DateTime.Now.AddSeconds(15*(1<<a.relaunches.Count)); a.lastLaunch=DateTime.Now;
          try{ a.hideNew=StartApp(a); Log(a.Name+(a.hideNew?": watchdog relaunched (#"+a.relaunches.Count+")":": launch fail")); }catch(Exception ex){ a.hideNew=false; Log(a.Name+": launch fail "+ex.Message); } hooksSuspect=true; } } }
    SyncAppTray(a,running);
    if(running&&a.Enabled&&!a.Native&&!a.userQuit&&IsWindowVisible(a.hwnd)) UpdateCloseRect(a); else a.snap=null; }
  void WriteStatus(){ try{ var sb=new StringBuilder();
    sb.AppendFormat("time={0}\r\nhelperPid={1}\r\ndpi={2}\r\nmouseHook={3}\r\nkeyHook={4}\r\n",DateTime.Now.ToString("s"),MyPid,dpiMode,mHook,kHook);
    foreach(var a in Apps){ var s=a.snap; sb.AppendFormat("[{0}] enabled={1} watchdog={2} hwnd={3} state={4} closeRect={5}\r\n",a.Name,a.Enabled,(a.IsGrok?(a.gaveUp?"gave-up":"on"):"off"),a.hwnd,State(a),s==null?"none":(s.l+","+s.t+","+s.r+","+s.b)); }
    File.WriteAllText(Path.Combine(Dir,"status.txt"),sb.ToString()); }catch{} }

  uint WinDpi(IntPtr h){ if(dpiWinMissing) return 96; try{ uint d=GetDpiForWindow(h); return d==0?96:d; }catch(EntryPointNotFoundException){ dpiWinMissing=true; return 96; } }
  int Metric(int i,uint dpi){ if(!metricDpiMissing){ try{ return GetSystemMetricsForDpi(i,dpi); }catch(EntryPointNotFoundException){ metricDpiMissing=true; } } return GetSystemMetrics(i); }
  static bool Fits16(int v){ return v>=short.MinValue&&v<=short.MaxValue; }
  static IntPtr Pack(int x,int y){ uint v=((uint)x&0xFFFFu)|(((uint)y&0xFFFFu)<<16); unchecked{ return new IntPtr((int)v); } }
  static int Hit(IntPtr h,int x,int y){ IntPtr r; return SendMessageTimeout(h,0x84,IntPtr.Zero,Pack(x,y),2,15,out r)!=IntPtr.Zero?(int)r:-1; }
  void Publish(AppTarget a,int l,int t,int r,int b){
    // Only arm when the app's X button is really on top at that spot (another window, e.g. our own settings, may cover it).
    var cp=new POINT(); cp.x=(l+r)/2; cp.y=Math.Max(t,(t+b)/2); var top=WindowFromPoint(cp); if(top==IntPtr.Zero||GetAncestor(top,2)!=a.hwnd){ a.snap=null; return; }
    var p=a.snap; if(p!=null&&p.l==l&&p.t==t&&p.r==r&&p.b==b) return; a.snap=new CloseSnap(l,t,r,b); if(LogVerbose) Log(a.Name+": closeRect="+l+","+t+","+r+","+b); }
  static readonly string[] OfficeCls={"XLMAIN","OpusApp","PPTFrameClass","rctrl_renwnd32","OMain","Framework::CFrame"};
  static bool Office(AppTarget a){ return a.WinClass!=null&&Array.IndexOf(OfficeCls,a.WinClass)>=0; }
  [DllImport("dwmapi")] static extern int DwmGetWindowAttribute(IntPtr h,int a,out RECT v,int s);
  [DllImport("user32")] static extern IntPtr MonitorFromWindow(IntPtr h,uint f);
  [StructLayout(LayoutKind.Sequential)] struct MONINFO{ public int cb; public RECT mon, work; public uint flags; }
  [DllImport("user32")] static extern bool GetMonitorInfo(IntPtr m,ref MONINFO mi);
  static int MonLeftTop(IntPtr h,bool left){ var mi=new MONINFO(); mi.cb=Marshal.SizeOf(typeof(MONINFO)); if(!GetMonitorInfo(MonitorFromWindow(h,2),ref mi)) return int.MinValue; return left?mi.work.l:mi.work.t; }
  void UpdateCloseRect(AppTarget a){
    RECT rc; IntPtr h=a.hwnd; if(!GetWindowRect(h,out rc)||rc.r<=rc.l||rc.b<=rc.t){ a.snap=null; return; }
    uint dpi=WinDpi(h); int bw=Math.Max(1,Metric(SM_CXSIZE,dpi)), bh=Math.Max(1,Metric(SM_CYSIZE,dpi)), fx=Math.Max(0,Metric(SM_CXFRAME,dpi)), fy=Math.Max(0,Metric(SM_CYFRAME,dpi)), pad=Math.Max(0,Metric(SM_CXPADDEDBORDER,dpi));
    int step=Math.Max(2,bw/8), reach=Math.Max(bw*3+fx,step*4), yA=rc.t+Math.Max(1,bh/3), yB=rc.t+fy+pad+Math.Max(1,bh/2);
    int hL=int.MaxValue,hR=int.MinValue,hY=yA,probes=0; bool sawFit=false; var sw=Stopwatch.StartNew();
    for(int pass=0;pass<2&&hR==int.MinValue;pass++){ int y=pass==0?yA:yB; if(pass==1&&y==yA) break;
      for(int x=rc.r-2;x>rc.l&&x>rc.r-reach;x-=step){ if(probes>=18||sw.ElapsedMilliseconds>=40) break; if(!Fits16(x)||!Fits16(y)) continue;
        sawFit=true; probes++; if(Hit(h,x,y)==20){ if(x<hL) hL=x; if(x>hR) hR=x; hY=y; } else if(hR!=int.MinValue) break; } }
    if(hR!=int.MinValue){ int mid=(hL+hR)/2, btm=hY;
      int vEnd=Math.Max(hY+bh+fy+pad,rc.t+(int)(64*dpi/96)), vp=0; for(int yy=hY;yy<vEnd;yy+=step){ if(++vp>24||sw.ElapsedMilliseconds>=60||!Fits16(mid)||!Fits16(yy)) break; if(Hit(h,mid,yy)==20) btm=yy; else break; }
      a.aL=rc.l; a.aT=rc.t; a.aR=rc.r; a.aB=rc.b; Publish(a,hL-2,rc.t,rc.r,btm+3); return; }
    if(a.Geometric||Office(a)||sawFit){ RECT vf; if(DwmGetWindowAttribute(h,9,out vf,Marshal.SizeOf(typeof(RECT)))!=0) vf=rc; bool z=IsZoomed(h); int gw=(int)(46*dpi/96), gh=(int)((Office(a)?40:32)*dpi/96), top=z?Math.Max(vf.t,0):vf.t;
      if(z){ top=Math.Max(vf.t,MonLeftTop(h,false)); }
      a.aL=rc.l; a.aT=rc.t; a.aR=rc.r; a.aB=rc.b; if(LogVerbose||!a.geoLogged){ a.geoLogged=true; Log(a.Name+": no HTCLOSE from hit-test, using caption geometry"); } Publish(a,vf.r-gw,top,vf.r,top+gh); return; }
    if(false){ int gw=(int)(46*dpi/96), gh=(int)(32*dpi/96), top=Math.Max(rc.t,rc.t+fy+pad-(IsZoomed(h)?fy+pad:0)); a.aL=rc.l; a.aT=rc.t; a.aR=rc.r; a.aB=rc.b; Publish(a,rc.r-gw-(IsZoomed(h)?fx+pad:0),rc.t,rc.r,top+gh+(IsZoomed(h)?fy+pad:0)); return; }
    if(!sawFit){ int w2=Math.Max(bw,20*(int)dpi/96), h2=Math.Max(bh+fy+pad,16*(int)dpi/96); a.aL=rc.l; a.aT=rc.t; a.aR=rc.r; a.aB=rc.b; Publish(a,rc.r-w2-fx,rc.t,rc.r,rc.t+h2); return; }
    if(a.aR!=a.aL&&(rc.l!=a.aL||rc.t!=a.aT||rc.r!=a.aR||rc.b!=a.aB)) a.snap=null; }

  // ---------- hooks (minimal: no IO, defer to UI thread) ----------
  AppTarget HitApp(int x,int y){ var arr=snapApps; for(int i=0;i<arr.Length;i++){ var s=arr[i].snap; if(s!=null&&x>=s.l&&x<s.r&&y>=s.t&&y<s.b) return arr[i]; } return null; }
  void PostHide(AppTarget a,int kind,int x,int y){
    if(Interlocked.CompareExchange(ref hideQueued,1,0)!=0) return;
    pendApp=a; pendKind=kind; pendX=x; pendY=y;
    if(uiHwnd!=IntPtr.Zero&&PostMessage(uiHwnd,(uint)WM_HIDE,IntPtr.Zero,IntPtr.Zero)) return;
    if(sync!=null) sync.Post(delegate{ OnUiHide(); },null); else hideQueued=0; }
  const long ReplayTag=0x54484C50; // "THLP": our own re-injected click, never intercepted
  [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT{ public int dx,dy; public uint data,flags,time; public IntPtr extra; }
  [StructLayout(LayoutKind.Sequential)] struct INPUT{ public int type; public MOUSEINPUT mi; }
  [DllImport("user32",SetLastError=true)] static extern uint SendInput(uint n,INPUT[] i,int size);
  [DllImport("user32")] static extern bool GetCursorPos(out POINT p);
  // The X click was swallowed but we did not hide: give the click back so the app closes (or whatever was really under it gets it).
  static void ReplayClick(int x,int y){ try{ POINT c; if(!GetCursorPos(out c)||Math.Abs(c.x-x)>4||Math.Abs(c.y-y)>4) return; var a=new INPUT[2]; a[0].type=0; a[0].mi.flags=0x0002; a[0].mi.extra=new IntPtr(ReplayTag); a[1].type=0; a[1].mi.flags=0x0004; a[1].mi.extra=new IntPtr(ReplayTag); SendInput(2,a,Marshal.SizeOf(typeof(INPUT))); }catch{} }
  static void LetClose(AppTarget a,IntPtr h,string why){ Log(a.Name+": hide failed ("+why+"), letting the close through"); PostMessage(h,0x112,new IntPtr(0xF060),IntPtr.Zero); }
  IntPtr MouseHook(int n,IntPtr w,IntPtr l){
    try{ if(n>=0&&l!=IntPtr.Zero&&Marshal.ReadIntPtr(l,IntPtr.Size==8?24:20).ToInt64()==ReplayTag) return CallNextHookEx(mHook,n,w,l); }catch{}
    try{ if(n>=0&&l!=IntPtr.Zero){ int msg=(int)w;
        if(msg==0x201||msg==0x202){ int x=Marshal.ReadInt32(l,0), y=Marshal.ReadInt32(l,4);
          if(msg==0x201){ if(HitApp(x,y)!=null){ swallowUp=true; return (IntPtr)1; } }
          else if(swallowUp){ swallowUp=false; var a=HitApp(x,y); if(a!=null) PostHide(a,1,x,y); return (IntPtr)1; } } } }catch{ hookFault=true; }
    return CallNextHookEx(mHook,n,w,l); }
  IntPtr KeyHook(int n,IntPtr w,IntPtr l){
    try{ if(n>=0&&l!=IntPtr.Zero){ int msg=(int)w; int vk0=Marshal.ReadInt32(l,0);
        if((vk0==0x51||vk0==0x57)&&(msg==0x100||msg==0x101)){ bool up0=(Marshal.ReadInt32(l,8)&0x80)!=0;
          if(up0){ if(ctrlKeyHeld==vk0){ ctrlKeyHeld=0; return (IntPtr)1; } }
          else if((GetAsyncKeyState(0x11)&0x8000)!=0&&(GetAsyncKeyState(0x10)&0x8000)==0&&(GetAsyncKeyState(0x12)&0x8000)==0){ var fg=GetForegroundWindow(); var arr=snapApps;
            for(int i=0;i<arr.Length;i++){ var x=arr[i]; if(x.Enabled&&!x.Native&&!x.userQuit&&x.hwnd!=IntPtr.Zero&&x.hwnd==fg&&((vk0==0x51&&x.CtrlQ)||(vk0==0x57&&x.CtrlW))){ if(ctrlKeyHeld!=vk0){ ctrlKeyHeld=vk0; PostHide(x,2,0,0); } return (IntPtr)1; } } } }
        if((msg==0x100||msg==0x104||msg==0x101||msg==0x105)&&Marshal.ReadInt32(l,0)==0x73){
          int flags=Marshal.ReadInt32(l,8); bool up=(flags&0x80)!=0, alt=(flags&0x20)!=0;
          if(up){ if(altF4Held){ altF4Held=false; return (IntPtr)1; } }
          else if(alt){ var fg=GetForegroundWindow(); var arr=snapApps; AppTarget hitA=null;
            for(int i=0;i<arr.Length;i++) if(arr[i].Enabled&&!arr[i].userQuit&&arr[i].hwnd!=IntPtr.Zero&&arr[i].hwnd==fg){ hitA=arr[i]; break; }
            if(hitA!=null){ if(!altF4Held){ altF4Held=true; PostHide(hitA,2,0,0); } return (IntPtr)1; }
            if(altF4Held) altF4Held=false; } } } }catch{ hookFault=true; }
    return CallNextHookEx(kHook,n,w,l); }
  void OnWinEvent(IntPtr hook,uint ev,IntPtr h,int obj,int child,uint th,uint t){ if(ev==3){ foreach(var x in Apps){ x.moved=true; x.snap=null; } if(!moveTimer.Enabled) moveTimer.Start(); return; } if(obj!=0||h==IntPtr.Zero) return; foreach(var a in Apps) if(a.hwnd==h){ a.moved=true; a.snap=null; if(!moveTimer.Enabled) moveTimer.Start(); break; } }
  public void OnUiHide(){
    var a=pendApp; int kind=pendKind, x=pendX, y=pendY; pendApp=null; pendKind=0; Interlocked.Exchange(ref hideQueued,0);
    if(exiting||a==null||kind==0) return; IntPtr h=a.hwnd;
    if(h==IntPtr.Zero||!IsWindow(h)||!IsWindowVisible(h)){ if(kind==1) ReplayClick(x,y); return; }
    if(kind==1){ var s=a.snap; if(s==null||!(x>=s.l&&x<s.r&&y>=s.t&&y<s.b)){ Log(a.Name+": skip, rect moved; click passed through"); a.snap=null; ReplayClick(x,y); return; }
      var p=new POINT(); p.x=x; p.y=y; var under=WindowFromPoint(p); var root=under==IntPtr.Zero?IntPtr.Zero:GetAncestor(under,2);
      if(root!=h){ Log(a.Name+": skip, under="+under+" root="+root+" at "+x+","+y+"; click passed through"); a.snap=null; ReplayClick(x,y); return; } }
    HideWin(a,h); if(!a.vd&&IsWindowVisible(h)){ a.snap=null; LetClose(a,h,"window still visible"); return; } a.snap=null; a.hiddenAt=DateTime.Now; FirstHideNote(a); Log(a.Name+": hid ("+(kind==1?"X":"alt+f4")+")"); }

  static void HoldProc(AppTarget a){ try{ if(a.mainProc!=null){ a.mainProc.Dispose(); a.mainProc=null; } uint pid; GetWindowThreadProcessId(a.hwnd,out pid); var p=Process.GetProcessById((int)pid); var hh=p.Handle; a.mainProc=p; }catch{ a.mainProc=null; } }
  // App quit itself cleanly (its own Quit menu) -> exit code 0 -> treat like a user quit, no relaunch.
  static void CheckCleanExit(AppTarget a){ try{ if(a.IsGrok){ if(a.mainProc!=null&&a.mainProc.HasExited) Log(a.Name+": exited code "+a.mainProc.ExitCode+", AlwaysRestart -> will relaunch"); return; } if(a.mainProc!=null&&a.mainProc.WaitForExit(1500)&&a.mainProc.ExitCode==0&&!a.hideNew){ a.userQuit=true; a.quitSawGone=true; Log(a.Name+": exited cleanly (code 0), no relaunch"); } else if(a.mainProc!=null&&a.mainProc.HasExited) Log(a.Name+": exited code "+a.mainProc.ExitCode); }catch{} }
  void FirstHideNote(AppTarget a){ if(!NotifyFirstHide||a.Notified) return; a.Notified=true; SaveAll(); try{ var t=a.Tray??tray; t.ShowBalloonTip(4000,a.Name+" is still running","Closed to the tray. Click its tray icon to bring it back.",ToolTipIcon.Info); }catch{} }
  public void ReloadConfig(){ foreach(var a in Apps){ if(a.Hidden) ShowWin(a); a.snap=null; SyncAppTray(a,false); } Apps.Clear(); LoadConfig(); try{ Catalog.Merge(Apps); }catch{} snapApps=Apps.ToArray(); SaveAll(); Log("config reloaded"); }
  // ---------- global hotkeys ----------
  [DllImport("user32")] static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint vk);
  [DllImport("user32")] static extern bool UnregisterHotKey(IntPtr h,int id);
  internal static bool ParseHk(string s,out uint mods,out uint vk){ mods=0; vk=0; if(string.IsNullOrEmpty(s)) return false;
    foreach(var p0 in s.Split('+')){ var p=p0.Trim(); if(p.Equals("Ctrl",StringComparison.OrdinalIgnoreCase)) mods|=2; else if(p.Equals("Alt",StringComparison.OrdinalIgnoreCase)) mods|=1; else if(p.Equals("Shift",StringComparison.OrdinalIgnoreCase)) mods|=4; else if(p.Equals("Win",StringComparison.OrdinalIgnoreCase)) mods|=8;
      else{ Keys k; if(!Enum.TryParse(p,true,out k)) return false; vk=(uint)k; } } return vk!=0&&mods!=0; }
  internal bool hotkeysOff; public readonly Dictionary<int,bool> HotkeyOk=new Dictionary<int,bool>();
  bool Reg(int id,string spec){ UnregisterHotKey(uiHwnd,id); uint m,v; if(hotkeysOff||!ParseHk(spec,out m,out v)){ HotkeyOk[id]=true; return true; } bool ok=RegisterHotKey(uiHwnd,id,m|0x4000,v); HotkeyOk[id]=ok; if(!ok) Log("hotkey "+spec+" is taken by another app"); return ok; }
  public void ApplyHotkeys(){ if(uiHwnd==IntPtr.Zero) return; Reg(1,HkRestoreAll); Reg(2,HkSendToTray); }
  public void SuspendHotkeys(bool off){ hotkeysOff=off; ApplyHotkeys(); }
  public void OnHotkey(int id){ if(id==1) RestoreAll(); else if(id==2) SendForegroundToTray(); }
  public void RestoreAll(){ int n=0; foreach(var a in Apps.Concat(Temps).ToArray()){ if(a.Hidden){ ShowWin(a); n++; } } Log("restore all: "+n+" window(s)"); }
  // ---------- send any window to the tray (temporary, not saved; its icon goes away once it's shown again) ----------
  [DllImport("kernel32")] static extern IntPtr OpenProcess(uint a,bool i,uint pid);
  [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h,int f,StringBuilder s,ref int n);
  static string ExeOf(uint pid){ var ph=OpenProcess(0x1000,false,pid); if(ph==IntPtr.Zero) return null; try{ var sb=new StringBuilder(1024); int n=sb.Capacity; return QueryFullProcessImageName(ph,0,sb,ref n)?sb.ToString():null; }finally{ CloseHandle(ph); } }
  public void SendForegroundToTray(){ var fg=GetForegroundWindow(); if(fg==IntPtr.Zero) return; var h=GetAncestor(fg,2); if(h==IntPtr.Zero) h=fg;
    if(!IsWindowVisible(h)) return; uint pid; GetWindowThreadProcessId(h,out pid); if(pid==MyPid) return;
    var c=new StringBuilder(256); GetClassName(h,c,256); var cls=c.ToString(); if(cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd"||cls=="Progman"||cls=="WorkerW"||cls=="Windows.UI.Core.CoreWindow") return;
    var m=Apps.FirstOrDefault(x=>x.hwnd==h&&x.Running); if(m!=null){ Hide_(m); Log(m.Name+": hid (shortcut)"); return; }
    if(Temps.Any(x=>x.hwnd==h)) return;
    var t=new StringBuilder(256); GetWindowText(h,t,256); string pn=null; try{ using(var p=Process.GetProcessById((int)pid)) pn=p.ProcessName; }catch{} var exe=ExeOf(pid);
    var a=new AppTarget(); a.Temp=true; a.hwnd=h; a.ProcName=pn??""; a.IconExe=exe; a.Enabled=true; a.Preset=false; a.Detected=true; a.seenRunning=true; a.Name=t.Length>0?t.ToString():(pn??"Window");
    Temps.Add(a); HideWin(a,h); if(!a.vd&&IsWindowVisible(h)){ Temps.Remove(a); Log(a.Name+": could not hide (shortcut)"); return; } a.hiddenAt=DateTime.Now; SyncAppTray(a,true); Log(a.Name+": sent to tray (shortcut)"); }
  void TickTemps(){ foreach(var a in Temps.ToArray()){ bool alive=a.hwnd!=IntPtr.Zero&&IsWindow(a.hwnd); if(alive&&a.vd&&Vd.OnCurrent(a.hwnd)) a.vd=false;
      if(!alive||!a.Hidden){ Temps.Remove(a); if(a.Tray!=null){ a.Tray.Visible=false; a.Tray.Dispose(); a.Tray=null; } SaveHidden(); } } }
  // ---------- tray icons: exe icon, solid background made transparent, sized for the current DPI ----------
  [DllImport("user32")] static extern bool DestroyIcon(IntPtr h);
  static Icon CleanIcon(Icon src){ if(src==null) return null; try{ var sz=SystemInformation.SmallIconSize; using(var big=src.ToBitmap()){
        int w=big.Width, h=big.Height; var c0=big.GetPixel(0,0); bool solid=c0.A==255&&w>8;
        if(solid){ foreach(var pt in new[]{new Point(w-1,0),new Point(0,h-1),new Point(w-1,h-1)}){ var c=big.GetPixel(pt.X,pt.Y); if(c.A!=255||Math.Abs(c.R-c0.R)+Math.Abs(c.G-c0.G)+Math.Abs(c.B-c0.B)>24){ solid=false; break; } } }
        if(solid){ // flood fill from the edges: only background connected to the border goes transparent
          var seen=new bool[w,h]; var q=new Queue<Point>(); for(int x=0;x<w;x++){ q.Enqueue(new Point(x,0)); q.Enqueue(new Point(x,h-1)); } for(int y=0;y<h;y++){ q.Enqueue(new Point(0,y)); q.Enqueue(new Point(w-1,y)); }
          while(q.Count>0){ var pt=q.Dequeue(); if(pt.X<0||pt.Y<0||pt.X>=w||pt.Y>=h||seen[pt.X,pt.Y]) continue; seen[pt.X,pt.Y]=true; var c=big.GetPixel(pt.X,pt.Y); if(c.A<16||Math.Abs(c.R-c0.R)+Math.Abs(c.G-c0.G)+Math.Abs(c.B-c0.B)>36) continue;
            big.SetPixel(pt.X,pt.Y,Color.Transparent); q.Enqueue(new Point(pt.X+1,pt.Y)); q.Enqueue(new Point(pt.X-1,pt.Y)); q.Enqueue(new Point(pt.X,pt.Y+1)); q.Enqueue(new Point(pt.X,pt.Y-1)); } }
        using(var bmp=new Bitmap(sz.Width,sz.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){ using(var g=Graphics.FromImage(bmp)){ g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality; g.DrawImage(big,0,0,sz.Width,sz.Height); }
          var hi=bmp.GetHicon(); using(var tmp=Icon.FromHandle(hi)){ var r=(Icon)tmp.Clone(); DestroyIcon(hi); return r; } } } }catch{ return src; } }
  void SyncAppTray(AppTarget a,bool running){
    bool want=running&&a.Enabled&&!a.Native&&!a.NativeTray&&!exiting;
    if(!want){ if(a.Tray!=null){ a.Tray.Visible=false; a.Tray.Dispose(); a.Tray=null; } return; }
    if(a.Tray!=null) return;
    if(a.TrayIcon==null){ try{ if(a.IconExe!=null) a.TrayIcon=CleanIcon(Icon.ExtractAssociatedIcon(a.IconExe)); }catch{} }
    var t=new NotifyIcon(); t.Icon=a.TrayIcon??ownedIcon??SystemIcons.Application; t.Text=a.Name.Length>63?a.Name.Substring(0,63):a.Name;
    t.MouseUp+=(s,e)=>{ if(e.Button==MouseButtons.Right) ShowAppMenu(a); else if(e.Button==MouseButtons.Middle&&!a.IsGrok) QuitApp(a); }; // Grok Bot is the remote channel: never quit by a stray middle click
    t.MouseClick+=(s,e)=>{ if(e.Button!=MouseButtons.Left) return; if(a.Hidden) ShowApp(a); else if(a.Running){ HideWin(a,a.hwnd); a.snap=null; a.hiddenAt=DateTime.Now; Log(a.Name+": hid (tray click)"); } };
    t.Visible=true; a.Tray=t; }
  void ShowApp(AppTarget a){ a.gaveUp=false; a.relaunches.Clear(); a.userQuit=false; a.quitSawGone=false; a.hideNew=false;
    if(a.hwnd==IntPtr.Zero||!IsWindow(a.hwnd)) a.hwnd=FindMain(a);
    if(a.hwnd!=IntPtr.Zero){ ShowWin(a); SetForegroundWindow(a.hwnd); Log(a.Name+": shown"); }
    else{ try{ a.lastLaunch=DateTime.Now; if(!StartApp(a)) Log(a.Name+": launch fail"); }catch(Exception ex){ Log(a.Name+": launch fail "+ex.Message); } } }
  void QuitApp(AppTarget a){ a.userQuit=true; a.quitSawGone=false; a.snap=null; Log(a.Name+": user quit");
    // Close while hidden: the window is never shown. Interception is off for this app while userQuit is set.
    var h=a.hwnd; if(h==IntPtr.Zero||!IsWindow(h)) return; uint pid; GetWindowThreadProcessId(h,out pid); PostMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);
    var t=new System.Windows.Forms.Timer(); t.Interval=5000; t.Tick+=(s,e)=>{ t.Stop(); t.Dispose(); try{ if(!a.userQuit||!IsWindow(h)) return; using(var p=Process.GetProcessById((int)pid)){ if(p.HasExited) return;
        bool ok=p.CloseMainWindow(); if(!ok){ PostMessage(h,0x10,IntPtr.Zero,IntPtr.Zero); } Log(a.Name+": still running 5s after WM_CLOSE, graceful close "+(ok?"sent":"re-posted")+"; not force-killing"); } }catch{} }; t.Start(); }

  void EnsureHooks(){ if(exiting) return; if(!hookFault&&!hooksSuspect&&mHook!=IntPtr.Zero&&kHook!=IntPtr.Zero) return;
    hookFault=false; hooksSuspect=false; UnhookOnly(); var mod=GetModuleHandle(null);
    mHook=SetWindowsHookEx(14,mProc,mod,0); kHook=SetWindowsHookEx(13,kProc,mod,0);
    if(mHook==IntPtr.Zero||kHook==IntPtr.Zero) Log("hook install failed"); }
  void UnhookOnly(){ if(mHook!=IntPtr.Zero){ UnhookWindowsHookEx(mHook); mHook=IntPtr.Zero; } if(kHook!=IntPtr.Zero){ UnhookWindowsHookEx(kHook); kHook=IntPtr.Zero; } }
  void Cleanup(){ if(Interlocked.Exchange(ref cleaned,1)==1) return; exiting=true; foreach(var a in Apps.Concat(Temps)){ try{ if(a.Tray!=null){ a.Tray.Visible=false; a.Tray.Dispose(); a.Tray=null; } }catch{} }
    try{ if(timer!=null){ timer.Stop(); timer.Dispose(); timer=null; } }catch{} try{ UnhookOnly(); }catch{} try{ if(weHook!=IntPtr.Zero) UnhookWinEvent(weHook); if(fgHook!=IntPtr.Zero) UnhookWinEvent(fgHook); }catch{}
    try{ if(ui!=null){ ui.DestroyHandle(); ui=null; } }catch{} try{ CloseWpf(); }catch{}
    try{ if(tray!=null){ tray.Visible=false; tray.Icon=null; tray.Dispose(); tray=null; } if(ownedIcon!=null) ownedIcon.Dispose(); }catch{} }
  void ExitHelper(){ RestoreAllHidden(); foreach(var a in Apps.Concat(Temps)){ if(a.Tray!=null){ a.Tray.Visible=false; a.Tray.Dispose(); a.Tray=null; } } Log("helper exit");
    var later=sync??SynchronizationContext.Current; if(later!=null) later.Post(delegate{ Cleanup(); ExitThread(); },null); else{ Cleanup(); ExitThread(); } }

  [STAThread] static void Main(){
    SetDpi(); var args=Environment.GetCommandLineArgs(); for(int ai=0;ai+1<args.Length;ai++) if(args[ai]=="--delay"){ int dl; if(int.TryParse(args[ai+1],out dl)&&dl>0&&dl<600) Thread.Sleep(dl*1000); } MyPid=Process.GetCurrentProcess().Id;
    try{ Dir=Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); }catch{}
    if(string.IsNullOrEmpty(Dir)) Dir=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    IniPath=Path.Combine(Dir,"tray-helper.ini");
    if(Environment.CommandLine.Contains("--scan")){ LoadConfig(); try{ Catalog.Merge(Apps); }catch{} Scanner.WriteReport(Path.Combine(Dir,"scan.txt")); return; }
    bool created; var mx=new Mutex(true,"Local\\TrayHelper",out created); if(!created){ mx.Dispose(); var w=FindWindowEx(new IntPtr(-3),IntPtr.Zero,null,"TrayHelper.msg"); if(w!=IntPtr.Zero) PostMessage(w,Environment.CommandLine.Contains("--flyout")?0x8003u:0x8002u,IntPtr.Zero,IntPtr.Zero); return; } // 2nd launch opens settings
    LoadConfig(); try{ Catalog.Merge(Apps); }catch(Exception ex){ Log("catalog "+ex.Message); } SaveAll(); RecoverHidden();
    AppDomain.CurrentDomain.UnhandledException+=delegate(object s,UnhandledExceptionEventArgs e){ try{ Log("fatal "+e.ExceptionObject); RestoreAllHidden(); }catch{} };
    try{ var mark=Path.Combine(Dir,"autostart.init"); if(!File.Exists(mark)){ StartWithWindows=true; File.WriteAllText(mark,DateTime.Now.ToString("s")); } }catch{}
    try{ Application.EnableVisualStyles(); Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException+=delegate(object s,ThreadExceptionEventArgs e){ Log("ui "+e.Exception.Message); };
      Application.Run(new TrayHelper()); }finally{ mx.Dispose(); } }
}

// Wrapper over Ciantic's VirtualDesktopAccessor.dll (MIT, shipped next to the exe). Any failure turns the feature off for this run.
static class Vd{
  const string D="VirtualDesktopAccessor.dll"; const string Name="TrayHelper";
  [DllImport(D)] static extern int GetDesktopCount();
  [DllImport(D)] static extern int GetCurrentDesktopNumber();
  [DllImport(D)] static extern int GetWindowDesktopNumber(IntPtr h);
  [DllImport(D)] static extern int IsWindowOnCurrentVirtualDesktop(IntPtr h);
  [DllImport(D)] static extern int MoveWindowToDesktopNumber(IntPtr h,int n);
  [DllImport(D)] static extern int CreateDesktop();
  [DllImport(D)] static extern int RemoveDesktop(int n,int fallback);
  [DllImport(D)] static extern int SetDesktopName(int n,byte[] utf8);
  [DllImport(D)] static extern int GetDesktopName(int n,byte[] buf,UIntPtr len);
  static int state; // 0 unknown, 1 ok, -1 off
  public static bool Ok{ get{ if(state==0){ try{ state=File.Exists(Path.Combine(TrayHelper.Dir??"",D))&&GetDesktopCount()>0?1:-1; }catch(Exception ex){ state=-1; TrayHelper.Log("virtual desktop off: "+ex.Message); } } return state==1; } }
  static void Fail(Exception ex){ state=-1; TrayHelper.Log("virtual desktop off: "+ex.Message); }
  static int Find(){ int n=GetDesktopCount(); var buf=new byte[256]; for(int i=0;i<n;i++){ Array.Clear(buf,0,buf.Length); if(GetDesktopName(i,buf,new UIntPtr((uint)buf.Length))<0) continue; int z=Array.IndexOf(buf,(byte)0); if(z<0) z=buf.Length; if(Encoding.UTF8.GetString(buf,0,z)==Name) return i; } return -1; }
  public static bool Hide(IntPtr h){ if(!Ok) return false; try{ int d=Find(); if(d<0){ d=CreateDesktop(); if(d<0) return false; SetDesktopName(d,Encoding.UTF8.GetBytes(Name+"\0")); }
      if(d==GetCurrentDesktopNumber()) return false; return MoveWindowToDesktopNumber(h,d)>=0&&GetWindowDesktopNumber(h)==d; }catch(Exception ex){ Fail(ex); return false; } }
  public static void Show(IntPtr h){ if(!Ok) return; try{ MoveWindowToDesktopNumber(h,GetCurrentDesktopNumber()); }catch(Exception ex){ Fail(ex); } }
  public static bool OnCurrent(IntPtr h){ if(!Ok) return true; try{ return IsWindowOnCurrentVirtualDesktop(h)==1; }catch(Exception ex){ Fail(ex); return true; } }
  // Remove our hidden desktop once nothing of ours is on it (anything else left there moves to the current desktop).
  public static void CleanupIfUnused(){ if(state!=1||TrayHelper.AnyVd()) return; try{ int d=Find(); if(d>=0){ int cur=GetCurrentDesktopNumber(); if(d!=cur) RemoveDesktop(d,cur); } }catch(Exception ex){ Fail(ex); } }
}
