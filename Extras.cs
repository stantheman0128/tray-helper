// 0.8 additions: Store-app identity + logos, ExePath self-heal, live ini reload, GitHub Releases updater, native-tray suggestions.
using System; using System.Linq; using System.Collections.Generic; using System.Diagnostics; using System.Drawing; using System.IO; using System.Net;
using System.Runtime.InteropServices; using System.Text; using System.Text.RegularExpressions; using System.Threading; using System.Windows.Forms; using System.Xml;

// Package identity of a window: AUMID, install root, the Start-menu logo (Square44x44, prefer the unplated 32 px variant).
static class Pkg{
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr p,IntPtr a,string c,string t);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("kernel32")] static extern IntPtr OpenProcess(uint a,bool i,uint pid);
  [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h,int f,StringBuilder s,ref int n);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr h,ref int n,StringBuilder s);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern int GetPackageFullName(IntPtr h,ref int n,StringBuilder s);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern int GetPackagePathByFullName(string full,ref int n,StringBuilder s);
  public sealed class Info{ public string Aumid, Root, Exe, Logo; }
  public static bool IsFrameHostExe(string p){ return p!=null&&p.EndsWith("ApplicationFrameHost.exe",StringComparison.OrdinalIgnoreCase); }
  // Store frames (ApplicationFrameHost) host the real app in a CoreWindow child owned by the app's own process.
  public static Info OfWindow(IntPtr h){ if(h==IntPtr.Zero) return null; uint pid; GetWindowThreadProcessId(h,out pid); var i=OfPid(pid);
    if(i!=null&&IsFrameHostExe(i.Exe)){ var cw=FindWindowEx(h,IntPtr.Zero,"Windows.UI.Core.CoreWindow",null); if(cw!=IntPtr.Zero){ uint p2; GetWindowThreadProcessId(cw,out p2); if(p2!=pid){ var j=OfPid(p2); if(j!=null) return j; } } i.Aumid=null; i.Root=null; i.Logo=null; }
    return i; }
  public static Info OfPid(uint pid){ var ph=OpenProcess(0x1000,false,pid); if(ph==IntPtr.Zero) return null; var r=new Info();
    try{ var sb=new StringBuilder(1024); int n=sb.Capacity; if(QueryFullProcessImageName(ph,0,sb,ref n)) r.Exe=sb.ToString();
      try{ var sa=new StringBuilder(512); int na=sa.Capacity; if(GetApplicationUserModelId(ph,ref na,sa)==0) r.Aumid=sa.ToString();
        var sf=new StringBuilder(512); int nf=sf.Capacity; if(GetPackageFullName(ph,ref nf,sf)==0){ var sp=new StringBuilder(1024); int np=sp.Capacity; if(GetPackagePathByFullName(sf.ToString(),ref np,sp)==0) r.Root=sp.ToString(); } }catch(EntryPointNotFoundException){} }
    finally{ CloseHandle(ph); }
    if(r.Aumid!=null&&r.Root!=null){ int b=r.Aumid.IndexOf('!'); r.Logo=Logo(r.Root,b>=0?r.Aumid.Substring(b+1):null); }
    return r; }
  static readonly Dictionary<string,string> logoCache=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  static XmlNode AppNode(XmlDocument d,string appId,string exeRel){ XmlNode first=null;
    foreach(XmlNode n in d.SelectNodes("//*[local-name()='Application']")){ if(first==null) first=n; var id=n.Attributes==null?null:n.Attributes["Id"]; var ex=n.Attributes==null?null:n.Attributes["Executable"];
      if(appId!=null&&id!=null&&string.Equals(id.Value,appId,StringComparison.OrdinalIgnoreCase)) return n; if(exeRel!=null&&ex!=null&&string.Equals(ex.Value.Replace('/','\\'),exeRel,StringComparison.OrdinalIgnoreCase)) return n; }
    return appId==null?first:null; }
  public static string Logo(string root,string appId){ var key=root+"!"+appId; string cached; lock(logoCache){ if(logoCache.TryGetValue(key,out cached)) return cached; }
    string res=null; try{ var d=new XmlDocument(); d.Load(Path.Combine(root,"AppxManifest.xml")); var app=AppNode(d,appId,null)??AppNode(d,null,null); string rel=null;
      if(app!=null) foreach(XmlNode ve in app.SelectNodes(".//*[local-name()='VisualElements']")){ foreach(var at in new[]{"Square44x44Logo","SmallLogo","Square150x150Logo","Logo"}){ var a=ve.Attributes[at]; if(a!=null&&a.Value.Length>0){ rel=a.Value; break; } } if(rel!=null) break; }
      if(rel==null){ var pl=d.SelectSingleNode("//*[local-name()='Properties']/*[local-name()='Logo']"); if(pl!=null) rel=pl.InnerText.Trim(); }
      if(rel!=null) res=BestAsset(root,rel); }catch{}
    lock(logoCache) logoCache[key]=res; return res; }
  // Logo files exist only as qualified variants: name.targetsize-32_altform-unplated.png, name.scale-200.png, ...
  static string BestAsset(string root,string rel){ rel=rel.Replace('/','\\'); var full=Path.Combine(root,rel); var dir=Path.GetDirectoryName(full); if(!Directory.Exists(dir)) return null;
    var stem=Path.GetFileNameWithoutExtension(full); var ext=Path.GetExtension(full); string best=File.Exists(full)?full:null; int bs=best!=null?30:-1;
    foreach(var f in Directory.GetFiles(dir,stem+".*"+ext)){ var q=Path.GetFileName(f).Substring(stem.Length).ToLowerInvariant(); if(q.Contains("contrast-")) continue; int s=40;
      var m=Regex.Match(q,@"targetsize-(\d+)"); if(m.Success){ int t=int.Parse(m.Groups[1].Value); s=q.Contains("altform-unplated")?100-Math.Abs(t-32):q.Contains("altform-lightunplated")?85-Math.Abs(t-32):80-Math.Abs(t-32); }
      else{ var sm=Regex.Match(q,@"scale-(\d+)"); if(sm.Success){ int sc=int.Parse(sm.Groups[1].Value); s=60-Math.Abs(sc-200)/10; } }
      if(s>bs){ bs=s; best=f; } }
    return best; }
  // ...\WindowsApps\Name_Version_Arch_Res_PublisherId\rel\app.exe  ->  current install of the same package (version-independent).
  public static bool FromWindowsAppsPath(string path,out string aumid,out string iconExe){ aumid=null; iconExe=null; if(string.IsNullOrEmpty(path)) return false;
    int i=path.IndexOf(@"\WindowsApps\",StringComparison.OrdinalIgnoreCase); if(i<0) return false; var rest=path.Substring(i+13); int sl=rest.IndexOf('\\'); if(sl<=0) return false;
    var seg=rest.Substring(0,sl); var exeRel=rest.Substring(sl+1); var parts=seg.Split('_'); if(parts.Length<3) return false; var name=parts[0]; var pub=parts[parts.Length-1]; if(name.Length==0||pub.Length==0) return false;
    var root=Catalog.AppxRoot(name+"_"); if(root==null) return false;
    try{ var d=new XmlDocument(); d.Load(Path.Combine(root,"AppxManifest.xml")); var app=AppNode(d,null,exeRel); if(app==null) return false; var id=app.Attributes["Id"]; if(id==null) return false;
      aumid=name+"_"+pub+"!"+id.Value; var ne=Path.Combine(root,exeRel); iconExe=File.Exists(ne)?ne:Logo(root,id.Value); return true; }catch{ return false; } }
  // shell:AppsFolder\Family!AppId -> logo of the installed package (for apps that aren't running yet).
  public static string LogoForAumid(string aumid){ if(string.IsNullOrEmpty(aumid)) return null; int b=aumid.IndexOf('!'); if(b<0) return null; var fam=aumid.Substring(0,b); int u=fam.LastIndexOf('_'); if(u<=0) return null;
    var root=Catalog.AppxRoot(fam.Substring(0,u)+"_"); return root==null?null:Logo(root,aumid.Substring(b+1)); }
  public static Icon PngIcon(string png){ try{ var sz=SystemInformation.SmallIconSize; using(var src=new Bitmap(png)) using(var bmp=new Bitmap(sz.Width,sz.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
      using(var g=Graphics.FromImage(bmp)){ g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality; g.DrawImage(src,0,0,sz.Width,sz.Height); }
      var hi=bmp.GetHicon(); using(var tmp=Icon.FromHandle(hi)){ var r=(Icon)tmp.Clone(); TrayHelper.DestroyIconPub(hi); return r; } } }catch{ return null; } }
}

partial class TrayHelper{
  // ---------- ExePath / icon self-heal: fill in or repair the launch path from the running window ----------
  static bool NeedsExe(string p,bool packaged){ if(string.IsNullOrEmpty(p)) return true; if(p.StartsWith("shell:",StringComparison.OrdinalIgnoreCase)) return false; if(Pkg.IsFrameHostExe(p)) return true;
    if(packaged&&p.IndexOf(@"\WindowsApps\",StringComparison.OrdinalIgnoreCase)>=0) return true; try{ return !File.Exists(p); }catch{ return false; } }
  static bool BadIcon(string p){ return string.IsNullOrEmpty(p)||Pkg.IsFrameHostExe(p)||!File.Exists(p); }
  void HealExe(AppTarget a,IntPtr h){ if(a.Temp||h==IntPtr.Zero) return; try{ var pi=Pkg.OfWindow(h); if(pi==null) return; bool ch=false, ic=false;
      if(pi.Aumid!=null){ if(NeedsExe(a.ExePath,true)){ Log(a.Name+": launch path set to shell:AppsFolder\\"+pi.Aumid+(string.IsNullOrEmpty(a.ExePath)?"":" (was "+a.ExePath+")")); a.ExePath=@"shell:AppsFolder\"+pi.Aumid; ch=true; }
        var want=pi.Logo??pi.Exe; if(want!=null&&!Pkg.IsFrameHostExe(want)&&BadIcon(a.IconExe)){ a.IconExe=want; ic=true; } }
      else if(pi.Exe!=null&&!Pkg.IsFrameHostExe(pi.Exe)){ if(NeedsExe(a.ExePath,false)){ Log(a.Name+": launch path set to "+pi.Exe+(string.IsNullOrEmpty(a.ExePath)?"":" (was "+a.ExePath+")")); a.ExePath=pi.Exe; ch=true; } if(BadIcon(a.IconExe)){ a.IconExe=pi.Exe; ic=true; } }
      if(ic){ a.TrayIcon=null; if(a.Tray!=null){ a.Tray.Visible=false; a.Tray.Dispose(); a.Tray=null; } }
      if(ch) SaveAll(); }catch(Exception ex){ Log(a.Name+": heal "+ex.Message); } }
  internal static Icon LoadIcon(string path){ if(path==null) return null; if(path.EndsWith(".png",StringComparison.OrdinalIgnoreCase)) return Pkg.PngIcon(path); return CleanIcon(Icon.ExtractAssociatedIcon(path)); }
  internal static void DestroyIconPub(IntPtr h){ DestroyIcon(h); }

  // ---------- live reload: hand edits to tray-helper.ini apply without a restart ----------
  internal static volatile string lastSaved; FileSystemWatcher iniW; System.Windows.Forms.Timer iniT;
  void StartIniWatch(){ try{ iniT=new System.Windows.Forms.Timer(); iniT.Interval=400; iniT.Tick+=(s,e)=>{ iniT.Stop(); OnIniChanged(); };
      iniW=new FileSystemWatcher(Dir,Path.GetFileName(IniPath)); iniW.NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.Size|NotifyFilters.FileName;
      FileSystemEventHandler on=(s,e)=>{ var sc=sync; if(sc!=null) sc.Post(delegate{ if(exiting) return; iniT.Stop(); iniT.Start(); },null); };
      iniW.Changed+=on; iniW.Created+=on; iniW.Renamed+=(s,e)=>on(s,e); iniW.EnableRaisingEvents=true; }catch(Exception ex){ Log("ini watch "+ex.Message); } }
  void OnIniChanged(){ string txt=null; for(int i=0;i<4&&txt==null;i++){ try{ if(!File.Exists(IniPath)) return; txt=File.ReadAllText(IniPath); }catch(IOException){ Thread.Sleep(120); } catch{ return; } }
    if(txt==null||txt==lastSaved) return; try{ MergeReload(txt); lastSaved=txt; Log("ini edited on disk, reloaded"); }catch(Exception ex){ Log("reload "+ex.Message); } }
  // Merge: keep each app's runtime state (window, hidden state, tray icon); only what changed is applied.
  public void MergeReload(string text){ var h1=HkRestoreAll; var h2=HkSendToTray; var fresh=ParseIni(text.Replace("\r","").Split('\n'));
    try{ if(TestMode) Catalog.Apply(fresh,new List<Catalog.Found>()); else Catalog.Merge(fresh); }catch{} var keep=new List<AppTarget>();
    foreach(var n in fresh){ var o=Apps.FirstOrDefault(x=>!keep.Contains(x)&&string.Equals(x.Name,n.Name,StringComparison.OrdinalIgnoreCase));
      if(o==null){ keep.Add(n); Log("ini: added "+n.Name); continue; }
      bool ident=o.ProcName!=n.ProcName||o.WinClass!=n.WinClass||o.WinTitle!=n.WinTitle, off=(o.Enabled&&!n.Enabled)||(!o.Native&&n.Native);
      if((ident||off)&&o.weHid) ShowWin(o); if(ident){ o.hwnd=IntPtr.Zero; o.hid.Clear(); o.wins=new IntPtr[0]; }
      if(o.Enabled!=n.Enabled) Log(o.Name+": enabled="+n.Enabled+" (ini)");
      o.Id=n.Id; o.ExePath=n.ExePath; o.ProcName=n.ProcName; o.WinClass=n.WinClass; o.WinTitle=n.WinTitle; o.Enabled=n.Enabled; o.CtrlQ=n.CtrlQ; o.CtrlW=n.CtrlW; o.Notified=n.Notified; o.Geometric=n.Geometric;
      o.UserNative=n.UserNative; o.Native=n.Native; o.NativeDismissed=n.NativeDismissed; o.NativeOffer=n.NativeOffer; o.Preset=n.Preset; o.Detected=n.Detected; o.Note=n.Note; o.Hint=n.Hint; o.SettingsKeys=n.SettingsKeys; o.NativeTray=n.NativeTray;
      if(BadIcon(o.IconExe)&&n.IconExe!=null){ o.IconExe=n.IconExe; o.TrayIcon=null; } o.snap=null; keep.Add(o); }
    foreach(var o in Apps) if(!keep.Contains(o)){ if(o.weHid) ShowWin(o); o.snap=null; if(o.Tray!=null){ o.Tray.Visible=false; o.Tray.Dispose(); o.Tray=null; } Log("ini: removed "+o.Name); }
    Apps.Clear(); Apps.AddRange(keep); snapApps=Apps.ToArray(); foreach(var a in Apps) SyncAppTray(a,a.Running);
    if(h1!=HkRestoreAll||h2!=HkSendToTray) ApplyHotkeys(); SaveHidden(); if(settingsWin!=null&&settingsWin.IsVisible) settingsWin.Rebuild(); }

  // ---------- updates from GitHub Releases (auto / notify / off). Never a popup. ----------
  internal static string UpdateMode="notify", UpdateRepo="stantheman0128/tray-helper"; internal static volatile string UpdateTag, UpdateStatus;
  static string updExe, updSha; static int updBusy; System.Threading.Timer updTimer;
  void StartUpdater(){ if(TestMode) return; updTimer=new System.Threading.Timer(_=>{ try{ if(UpdateMode!="off") CheckUpdate(false); }catch{} },null,120000,24*3600*1000); }
  static string Http(string url){ ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072; var rq=(HttpWebRequest)WebRequest.Create(url); rq.UserAgent="TrayHelper/"+Version; rq.Accept="application/vnd.github+json"; rq.Timeout=15000;
    using(var rs=rq.GetResponse()) using(var sr=new StreamReader(rs.GetResponseStream())) return sr.ReadToEnd(); }
  static System.Version V(string s){ System.Version v; return System.Version.TryParse((s??"").Trim().TrimStart('v','V'),out v)?v:new System.Version(0,0); }
  // Returns false when GitHub could not be reached (offline, private repo, rate limit).
  internal static TrayHelper Inst;
  public static bool CheckUpdate(bool manual){ if(Interlocked.Exchange(ref updBusy,1)==1) return true; try{
      string js; try{ js=Http("https://api.github.com/repos/"+UpdateRepo+"/releases/latest"); }
      catch(WebException wx){ var r=wx.Response as HttpWebResponse; UpdateStatus=r!=null&&(int)r.StatusCode==404?"No public releases found":"Couldn't reach GitHub"; if(manual||LogVerbose) Log("update check: "+UpdateStatus); return false; }
      var tm=Regex.Match(js,"\"tag_name\"\\s*:\\s*\"([^\"]+)\""); if(!tm.Success){ UpdateStatus="No releases yet"; return false; } var tag=tm.Groups[1].Value;
      updExe=null; updSha=null; foreach(Match m in Regex.Matches(js,"\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"")){ var u=m.Groups[1].Value; var f=u.Substring(u.LastIndexOf('/')+1);
        if(f.Equals("TrayHelper.exe",StringComparison.OrdinalIgnoreCase)) updExe=u; else if(f.Equals("TrayHelper.exe.sha256",StringComparison.OrdinalIgnoreCase)) updSha=u; }
      if(V(tag)<=V(Version)){ UpdateTag=null; UpdateStatus="Up to date"; return true; }
      if(updExe==null||updSha==null){ UpdateStatus="v"+tag.TrimStart('v')+" has no installable files"; return true; }
      UpdateTag=tag.TrimStart('v','V'); UpdateStatus="v"+UpdateTag+" available"; Log("update available: "+UpdateTag);
      if(UpdateMode=="auto"&&!manual){ var me=Inst; var sc=me==null?null:me.sync; if(sc!=null) sc.Post(delegate{ me.InstallUpdate(); },null); }
      return true; }catch(Exception ex){ UpdateStatus="Update check failed"; Log("update check "+ex.Message); return false; } finally{ updBusy=0; } }
  // Download, verify SHA-256 + file identity, swap the exe (the old one stays as TrayHelper.old.exe for rollback), restart; hidden windows are re-adopted.
  public void InstallUpdate(){ var exeUrl=updExe; var shaUrl=updSha; var tag=UpdateTag; if(exeUrl==null||shaUrl==null||tag==null) return; UpdateStatus="Downloading v"+tag+"…";
    ThreadPool.QueueUserWorkItem(_=>{ var tmp=Path.Combine(Dir,"TrayHelper.update.exe"); try{
        ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072; using(var wc=new WebClient()){ wc.Headers.Add("User-Agent","TrayHelper/"+Version); wc.DownloadFile(exeUrl,tmp); var want=wc.DownloadString(shaUrl).Trim().Split(' ','\t','\r','\n')[0].ToLowerInvariant();
          string got; using(var sh=System.Security.Cryptography.SHA256.Create()) using(var fs=File.OpenRead(tmp)) got=BitConverter.ToString(sh.ComputeHash(fs)).Replace("-","").ToLowerInvariant();
          if(want.Length!=64||got!=want){ File.Delete(tmp); UpdateStatus="Update download was corrupt, not installed"; Log("update: sha256 mismatch"); return; } }
        var vi=FileVersionInfo.GetVersionInfo(tmp); if(vi.ProductName!="TrayHelper"||V(vi.FileVersion)<V(tag)){ File.Delete(tmp); UpdateStatus="Update file didn't match, not installed"; Log("update: identity mismatch"); return; }
        var sc=sync; if(sc!=null) sc.Post(delegate{ SwapAndRestart(tmp,tag); },null); }catch(Exception ex){ try{ File.Delete(tmp); }catch{} UpdateStatus="Update failed"; Log("update "+ex.Message); } }); }
  void SwapAndRestart(string tmp,string tag){ var exe=Application.ExecutablePath; var old=Path.Combine(Dir,"TrayHelper.old.exe");
    try{ if(File.Exists(old)) File.Delete(old); }catch{} try{ File.Move(exe,old); }catch(Exception ex){ UpdateStatus="Update failed"; Log("update swap "+ex.Message); return; }
    try{ File.Move(tmp,exe); }catch(Exception ex){ try{ File.Move(old,exe); }catch{} UpdateStatus="Update failed"; Log("update swap "+ex.Message); return; }
    Log("updating to "+tag); ReplaceExit(exe); }
  // Hand over to a new instance without restoring windows: it re-adopts them from hidden.txt.
  void ReplaceExit(string startExe){ SaveHidden(); foreach(var a in Apps.Concat(Temps)){ if(a.Tray!=null){ a.Tray.Visible=false; a.Tray.Dispose(); a.Tray=null; } }
    if(startExe!=null){ try{ var psi=new ProcessStartInfo(startExe,(TestMode?"--test ":"")+"--replace"); psi.WorkingDirectory=Dir; psi.UseShellExecute=false; Process.Start(psi); }catch(Exception ex){ Log("restart "+ex.Message); } }
    Log("helper handover"); var later=sync??SynchronizationContext.Current; if(later!=null) later.Post(delegate{ Cleanup(); ExitThread(); },null); else{ Cleanup(); ExitThread(); } }

  // Test instances only: --test-update <exeUrl> <sha256Url> <tag> runs the real download/verify/swap/restart path (file:// URLs work).
  void TestUpdateArg(){ if(!TestMode) return; var ar=Environment.GetCommandLineArgs(); int i=Array.IndexOf(ar,"--test-update"); if(i<0||i+3>=ar.Length) return; updExe=ar[i+1]; updSha=ar[i+2]; UpdateTag=ar[i+3]; Log("test update to "+UpdateTag); InstallUpdate(); }
  // ---------- native close-to-tray: suggest retiring our toggle for apps that already own a tray icon ----------
  // The tray-icon registry only proves the app once showed a tray icon, not that X keeps it running: it's a hint.
  // 0 = nothing, 1 = small "Use built-in" link only, 2 = hint badge + link. The link stays once an app was suggested or used natively.
  public static int NativeOffer(AppTarget a,HashSet<string> owners){ if(a.Native||a.Temp||a.IsGrok||string.IsNullOrEmpty(a.ProcName)) return 0;
    bool owns=a.ProcName.Split('|').Any(p=>owners.Contains(p.Trim())); if(owns&&!a.NativeOffer){ a.NativeOffer=true; SaveAll(); }
    if(!a.NativeOffer) return 0; return owns&&!a.NativeDismissed?2:1; }
  public void UseNative(AppTarget a){ if(a.weHid) ShowWin(a); a.NativeOffer=true; a.UserNative=true; a.Native=true; a.Enabled=false; a.snap=null; SyncAppTray(a,a.Running); SaveAll(); Log(a.Name+": switched to the app's own close-to-tray"); OpenAppSettings(a); }
  public void UseOurs(AppTarget a){ a.UserNative=false; a.Native=Catalog.All.Any(p=>p.Id!=null&&p.Id==a.Id&&p.Native); a.NativeDismissed=true; a.NativeOffer=true; a.Enabled=!a.Native; SyncAppTray(a,a.Running); SaveAll(); Log(a.Name+": back to TrayHelper close-to-tray"); }
  public void DismissNative(AppTarget a){ a.NativeDismissed=true; SaveAll(); }
}
