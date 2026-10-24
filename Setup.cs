// TrayHelper Setup: per-user install (no admin, no UAC). Embeds TrayHelper.exe + VirtualDesktopAccessor.dll. MIT License.
//   TrayHelper-Setup.exe                 install to %LOCALAPPDATA%\TrayHelper (or upgrade in place), keep settings, start it
//   TrayHelper-Setup.exe --uninstall     restore hidden windows, remove autostart, Start menu entry and files
//   --silent  --dir <path>  --no-run-key  --no-start  --no-migrate  --test (talks to a --test instance, touches no registry)
using System; using System.Diagnostics; using System.IO; using System.Reflection; using System.Runtime.InteropServices; using System.Text; using System.Threading; using System.Windows.Forms; using Microsoft.Win32;
[assembly: AssemblyTitle("TrayHelper Setup")] [assembly: AssemblyProduct("TrayHelper Setup")] [assembly: AssemblyDescription("Installs TrayHelper for the current user")] [assembly: AssemblyCompany("Stan Shih")] [assembly: AssemblyCopyright("MIT License")] [assembly: AssemblyVersion("0.9.1.0")] [assembly: AssemblyFileVersion("0.9.1.0")]

static class Setup{
  const string Ver="0.9.1", RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run", UninstKey=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\TrayHelper";
  static bool silent, test, noRun, noStart, noMigrate; static string dir; static StreamWriter log;
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr p,IntPtr a,string c,string t);
  [DllImport("user32")] static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  static string Caption{ get{ return test?"TrayHelper.test.msg":"TrayHelper.msg"; } }
  static string Self{ get{ return Assembly.GetExecutingAssembly().Location; } }
  [STAThread] static int Main(string[] args){ bool uninst=false; string d=null;
    for(int i=0;i<args.Length;i++){ var a=args[i].ToLowerInvariant().TrimStart('-','/');
      if(a=="uninstall"||a=="u") uninst=true; else if(a=="silent"||a=="s"||a=="quiet") silent=true; else if(a=="test") test=true; else if(a=="no-run-key") noRun=true; else if(a=="no-start") noStart=true; else if(a=="no-migrate") noMigrate=true; else if(a=="dir"&&i+1<args.Length) d=args[++i]; }
    if(d==null&&uninst&&File.Exists(Path.Combine(Path.GetDirectoryName(Self),"TrayHelper.exe"))) d=Path.GetDirectoryName(Self);
    dir=Path.GetFullPath(d??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TrayHelper"));
    try{ log=new StreamWriter(Path.Combine(Path.GetTempPath(),"TrayHelper-Setup.log"),true); log.AutoFlush=true; }catch{}
    try{ int r=uninst?Uninstall():Install(); return r; }catch(Exception ex){ L("error "+ex); Say("Setup failed: "+ex.Message,true); return 1; } finally{ if(log!=null) log.Dispose(); } }
  static void L(string s){ try{ if(log!=null) log.WriteLine(DateTime.Now.ToString("s")+" "+s); }catch{} }
  static void Say(string s,bool err){ if(!silent) MessageBox.Show(s,"TrayHelper",MessageBoxButtons.OK,err?MessageBoxIcon.Warning:MessageBoxIcon.Information); }
  // The running helper, if any: its message window, process and folder.
  static IntPtr Running(out Process p,out string runDir){ p=null; runDir=null; var w=FindWindowEx(new IntPtr(-3),IntPtr.Zero,null,Caption); if(w==IntPtr.Zero) return w;
    uint pid; GetWindowThreadProcessId(w,out pid); try{ p=Process.GetProcessById((int)pid); runDir=Path.GetDirectoryName(p.MainModule.FileName); }catch{} return w; }
  static void WaitGone(Process p,int ms,bool killAfter){ if(p==null) return; try{ if(!p.WaitForExit(ms)&&killAfter){ L("helper still running, stopping it (hidden windows are re-adopted from hidden.txt)"); p.Kill(); p.WaitForExit(3000); } }catch{} }
  static void Extract(string res,string path){ using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(res)){ if(s==null) throw new Exception("missing "+res);
      if(File.Exists(path)){ try{ File.Delete(path); }catch{ var old=path+".old"; try{ if(File.Exists(old)) File.Delete(old); }catch{} File.Move(path,old); } }
      using(var f=File.Create(path)) s.CopyTo(f); } }
  static int Install(){ L("install "+Ver+" to "+dir+(test?" (test)":"")); Directory.CreateDirectory(dir);
    Process rp; string runDir; var w=Running(out rp,out runDir); bool same=runDir!=null&&string.Equals(Path.GetFullPath(runDir).TrimEnd('\\'),dir.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase);
    bool migrate=!noMigrate&&runDir!=null&&!same;
    if(migrate){ foreach(var f in new[]{"tray-helper.ini","autostart.init"}){ var src=Path.Combine(runDir,f); var dst=Path.Combine(dir,f); if(File.Exists(src)&&!File.Exists(dst)){ File.Copy(src,dst); L("copied "+f+" from "+runDir); } } }
    if(w!=IntPtr.Zero&&(same||migrate)){ PostMessage(w,0x8005,IntPtr.Zero,IntPtr.Zero); WaitGone(rp,4000,true); L("previous helper stopped"); // hands over without restoring windows
      if(migrate){ var hs=Path.Combine(runDir,"hidden.txt"); if(File.Exists(hs)){ File.Copy(hs,Path.Combine(dir,"hidden.txt"),true); try{ File.Delete(hs); }catch{} } } }
    Extract("TrayHelper.exe",Path.Combine(dir,"TrayHelper.exe")); Extract("VirtualDesktopAccessor.dll",Path.Combine(dir,"VirtualDesktopAccessor.dll"));
    var un=Path.Combine(dir,"Uninstall.exe"); if(!string.Equals(Path.GetFullPath(Self),un,StringComparison.OrdinalIgnoreCase)){ try{ if(File.Exists(un)) File.Delete(un); File.Copy(Self,un); }catch(Exception ex){ L("uninstaller copy "+ex.Message); } }
    var exe=Path.Combine(dir,"TrayHelper.exe");
    if(!test){
      if(!noRun){ using(var k=Registry.CurrentUser.CreateSubKey(RunKey)) k.SetValue("TrayHelper","\""+exe+"\""); try{ File.WriteAllText(Path.Combine(dir,"autostart.init"),DateTime.Now.ToString("s")); }catch{} }
      try{ Shortcut(StartMenuLnk,exe); }catch(Exception ex){ L("shortcut "+ex.Message); }
      using(var k=Registry.CurrentUser.CreateSubKey(UninstKey)){ k.SetValue("DisplayName","TrayHelper"); k.SetValue("DisplayVersion",Ver); k.SetValue("Publisher","Stan Shih"); k.SetValue("InstallLocation",dir); k.SetValue("DisplayIcon",exe);
        k.SetValue("UninstallString","\""+un+"\" --uninstall"); k.SetValue("QuietUninstallString","\""+un+"\" --uninstall --silent"); k.SetValue("NoModify",1,RegistryValueKind.DWord); k.SetValue("NoRepair",1,RegistryValueKind.DWord); k.SetValue("EstimatedSize",(int)(new FileInfo(exe).Length/1024)+400,RegistryValueKind.DWord); } }
    if(!noStart){ var psi=new ProcessStartInfo(exe,(test?"--test ":"")+"--replace"); psi.WorkingDirectory=dir; psi.UseShellExecute=false; Process.Start(psi); L("started "+exe); }
    L("install done"); Say("TrayHelper "+Ver+" is installed. It's running in the tray.",false); return 0; }
  static string StartMenuLnk{ get{ return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"TrayHelper.lnk"); } }
  static void Shortcut(string lnk,string target){ var t=Type.GetTypeFromProgID("WScript.Shell"); object sh=Activator.CreateInstance(t); try{ object sc=t.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,sh,new object[]{lnk});
      var st=sc.GetType(); st.InvokeMember("TargetPath",BindingFlags.SetProperty,null,sc,new object[]{target}); st.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,sc,new object[]{Path.GetDirectoryName(target)}); st.InvokeMember("Description",BindingFlags.SetProperty,null,sc,new object[]{"Close the window, keep the app"});
      st.InvokeMember("Save",BindingFlags.InvokeMethod,null,sc,null); Marshal.FinalReleaseComObject(sc); }finally{ Marshal.FinalReleaseComObject(sh); } }
  // Only folders that look like a TrayHelper install are ever deleted.
  static bool SafeDir(string d){ try{ var full=Path.GetFullPath(d).TrimEnd('\\'); if(full.Length<=3) return false; var bad=new[]{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.Desktop),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.Windows),Path.GetTempPath().TrimEnd('\\')};
      foreach(var b in bad) if(string.Equals(full,b.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)) return false; return File.Exists(Path.Combine(full,"TrayHelper.exe")); }catch{ return false; } }
  static int Uninstall(){ L("uninstall "+dir+(test?" (test)":"")); Process rp; string runDir; var w=Running(out rp,out runDir);
    if(w!=IntPtr.Zero&&runDir!=null&&string.Equals(Path.GetFullPath(runDir).TrimEnd('\\'),dir.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)){ PostMessage(w,0x8004,IntPtr.Zero,IntPtr.Zero); WaitGone(rp,8000,true); L("helper exited (hidden windows restored)"); }
    if(!test){ try{ using(var k=Registry.CurrentUser.OpenSubKey(RunKey,true)){ var v=k==null?null:k.GetValue("TrayHelper") as string; if(v!=null&&v.IndexOf(dir,StringComparison.OrdinalIgnoreCase)>=0) k.DeleteValue("TrayHelper",false); } }catch{}
      try{ if(File.Exists(StartMenuLnk)) File.Delete(StartMenuLnk); }catch{} try{ Registry.CurrentUser.DeleteSubKeyTree(UninstKey,false); }catch{} }
    if(!SafeDir(dir)){ L("not deleting "+dir); Say("TrayHelper was removed (folder kept: "+dir+").",false); return 0; }
    var self=Path.GetFullPath(Self); bool selfInside=self.StartsWith(dir.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase);
    foreach(var f in Directory.GetFiles(dir)){ if(string.Equals(f,self,StringComparison.OrdinalIgnoreCase)) continue; try{ File.Delete(f); }catch(Exception ex){ L("keep "+f+": "+ex.Message); } }
    foreach(var sd in Directory.GetDirectories(dir)){ try{ Directory.Delete(sd,true); }catch{} }
    if(selfInside){ var psi=new ProcessStartInfo("cmd.exe","/c ping 127.0.0.1 -n 3 >nul & rmdir /s /q \""+dir+"\""); psi.CreateNoWindow=true; psi.UseShellExecute=false; psi.WindowStyle=ProcessWindowStyle.Hidden; Process.Start(psi); }
    else{ try{ Directory.Delete(dir,true); }catch{} }
    L("uninstall done"); Say("TrayHelper was removed. Hidden windows were brought back.",false); return 0; }
}
