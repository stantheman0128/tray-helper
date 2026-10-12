// Built-in catalog of supported apps + detection (install paths, Store packages, running processes).
using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using Microsoft.Win32;

sealed class Preset{
  public string Name, ProcName, WinClass="Chrome_WidgetWin_1", WinTitle, AppxPrefix, AppxExe, Aumid, Note; public string[] Paths=new string[0]; public bool DefaultOn, NativeTray, AlwaysRestart;
}
static class Catalog{
  public static readonly Preset[] All=new Preset[]{
    new Preset{ Name="Grok Bot", ProcName="Grok Bot", WinTitle="Grok Bot", Paths=new[]{@"%LOCALAPPDATA%\Programs\Grok Bot\Grok Bot.exe"}, DefaultOn=true, AlwaysRestart=true },
    new Preset{ Name="Claude", ProcName="claude", WinTitle="Claude", AppxPrefix="Claude_", AppxExe=@"app\Claude.exe", Aumid="Claude_pzs8sxrjxfjjc!Claude", Paths=new[]{@"%LOCALAPPDATA%\AnthropicClaude\claude.exe"}, DefaultOn=true, NativeTray=true, Note="has its own tray option" },
    new Preset{ Name="ChatGPT / Codex", ProcName="ChatGPT", WinTitle="ChatGPT", AppxPrefix="OpenAI.Codex_", AppxExe=@"app\ChatGPT.exe", Aumid="OpenAI.Codex_2p2nqsd0c76g0!App" },
    new Preset{ Name="Visual Studio Code", ProcName="Code", WinTitle="*Visual Studio Code", Paths=new[]{@"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe",@"%ProgramFiles%\Microsoft VS Code\Code.exe"} },
    new Preset{ Name="Cursor", ProcName="Cursor", WinTitle="*Cursor", Paths=new[]{@"%LOCALAPPDATA%\Programs\cursor\Cursor.exe"} },
    new Preset{ Name="Windsurf", ProcName="Windsurf", WinTitle="*Windsurf", Paths=new[]{@"%LOCALAPPDATA%\Programs\Windsurf\Windsurf.exe"} },
    new Preset{ Name="Obsidian", ProcName="Obsidian", WinTitle="*Obsidian", Paths=new[]{@"%LOCALAPPDATA%\Programs\Obsidian\Obsidian.exe",@"%LOCALAPPDATA%\Obsidian\Obsidian.exe",@"%ProgramFiles%\Obsidian\Obsidian.exe"} },
    new Preset{ Name="Notion", ProcName="Notion", WinTitle="", Paths=new[]{@"%LOCALAPPDATA%\Programs\Notion\Notion.exe"} },
    new Preset{ Name="OpenCode", ProcName="OpenCode", WinTitle="*OpenCode", Paths=new[]{@"%LOCALAPPDATA%\Programs\OpenCode\OpenCode.exe",@"%LOCALAPPDATA%\OpenCode\OpenCode.exe",@"%ProgramFiles%\OpenCode\OpenCode.exe"} },
    new Preset{ Name="Hermes Agent", ProcName="Hermes Agent", WinTitle="*Hermes", Paths=new[]{@"%LOCALAPPDATA%\Programs\Hermes Agent\Hermes Agent.exe"} },
    new Preset{ Name="Outlook (new)", ProcName="olk", WinClass="", WinTitle="*Outlook", AppxPrefix="Microsoft.OutlookForWindows_", AppxExe="olk.exe", Aumid="Microsoft.OutlookForWindows_8wekyb3d8bbwe!Microsoft.OutlookforWindows", Note="experimental" },
    new Preset{ Name="Thunderbird", ProcName="thunderbird", WinClass="MozillaWindowClass", WinTitle="*Thunderbird", Paths=new[]{@"%ProgramFiles%\Mozilla Thunderbird\thunderbird.exe"}, Note="experimental" },
  };
  static string AppxRoot(string prefix){ try{
      using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages")){
        if(k==null) return null; string best=null, root=null;
        foreach(var n in k.GetSubKeyNames()) if(n.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&(best==null||string.CompareOrdinal(n,best)>0)){
          using(var s=k.OpenSubKey(n)){ var r=s==null?null:s.GetValue("PackageRootFolder") as string; if(r!=null&&Directory.Exists(r)){ best=n; root=r; } } }
        return root; } }catch{ return null; } }
  static string RunningExe(string proc){ Process[] ps=null; try{ ps=Process.GetProcessesByName(proc); foreach(var p in ps){ try{ var f=p.MainModule.FileName; if(!string.IsNullOrEmpty(f)) return f; }catch{} } }catch{} finally{ if(ps!=null) foreach(var p in ps) p.Dispose(); } return null; }
  // Returns launch path (exe or shell:AppsFolder\AUMID) and an exe to take the icon from; null if not installed.
  public static bool Detect(Preset p,out string launch,out string iconExe){
    launch=null; iconExe=null;
    foreach(var raw in p.Paths){ var f=Environment.ExpandEnvironmentVariables(raw); if(File.Exists(f)){ launch=f; iconExe=f; return true; } }
    if(p.AppxPrefix!=null){ var root=AppxRoot(p.AppxPrefix); if(root!=null){ launch=@"shell:AppsFolder\"+p.Aumid; var e=Path.Combine(root,p.AppxExe); iconExe=File.Exists(e)?e:RunningExe(p.ProcName); return true; } }
    var run=RunningExe(p.ProcName); if(run!=null){ launch=run; iconExe=run; return true; }
    return false; }
  public static void Merge(List<AppTarget> apps){
    foreach(var p in All){ string launch, icon; bool found=Detect(p,out launch,out icon);
      var a=apps.Find(x=>string.Equals(x.Name,p.Name,StringComparison.OrdinalIgnoreCase));
      if(a==null){ if(!found) continue; a=new AppTarget(); a.Name=p.Name; a.ProcName=p.ProcName; a.WinClass=p.WinClass; a.WinTitle=p.WinTitle; a.ExePath=launch; a.Enabled=p.DefaultOn; apps.Add(a); TrayHelper.Log("catalog: detected "+p.Name); }
      a.Preset=true; a.Detected=found; a.Note=p.Note; a.NativeTray=p.NativeTray; a.AlwaysRestart=a.AlwaysRestart||p.AlwaysRestart; a.ProcName=p.ProcName; a.WinClass=p.WinClass; a.WinTitle=p.WinTitle; if(icon!=null) a.IconExe=icon; if(string.IsNullOrEmpty(a.ExePath)&&launch!=null) a.ExePath=launch; }
    foreach(var a in apps) if(!a.Preset){ if(a.IconExe==null&&a.ExePath!=null&&File.Exists(a.ExePath)) a.IconExe=a.ExePath; if(a.IconExe==null) a.IconExe=RunningExe(a.ProcName); }
  }
}
