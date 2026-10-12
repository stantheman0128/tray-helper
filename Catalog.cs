// Built-in catalog of supported apps + detection (install paths, Store packages, running processes).
using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using Microsoft.Win32;

sealed class Preset{
  public string Id, OldName, Hint, SettingsKeys; public bool Native, CtrlQ;
  public string Name, ProcName, WinClass="Chrome_WidgetWin_1", WinTitle, AppxPrefix, AppxExe, Aumid, Note; public string[] Paths=new string[0]; public bool DefaultOn, NativeTray;
}
static class Catalog{
  public static readonly Preset[] All=new Preset[]{
    new Preset{ Id="grokbot", Name="Grok Bot", ProcName="Grok Bot", WinTitle="Grok Bot", Paths=new[]{@"%LOCALAPPDATA%\Programs\Grok Bot\Grok Bot.exe"}, DefaultOn=true },
    new Preset{ Id="vscode", Name="Visual Studio Code", ProcName="Code", WinTitle="*Visual Studio Code", Paths=new[]{@"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe",@"%ProgramFiles%\Microsoft VS Code\Code.exe"} },
    new Preset{ Id="cursor", Name="Cursor", ProcName="Cursor", WinTitle="*Cursor", Paths=new[]{@"%LOCALAPPDATA%\Programs\cursor\Cursor.exe"} },
    new Preset{ Id="windsurf", Name="Windsurf", ProcName="Windsurf", WinTitle="*Windsurf", Paths=new[]{@"%LOCALAPPDATA%\Programs\Windsurf\Windsurf.exe"} },
    new Preset{ Id="obsidian", Name="Obsidian", ProcName="Obsidian", WinTitle="*Obsidian", Paths=new[]{@"%LOCALAPPDATA%\Programs\Obsidian\Obsidian.exe",@"%LOCALAPPDATA%\Obsidian\Obsidian.exe",@"%ProgramFiles%\Obsidian\Obsidian.exe"} },
    new Preset{ Id="notion", Name="Notion", ProcName="Notion", WinTitle="", Paths=new[]{@"%LOCALAPPDATA%\Programs\Notion\Notion.exe"} },
    new Preset{ Id="opencode", Name="OpenCode", ProcName="OpenCode", WinTitle="*OpenCode", Paths=new[]{@"%LOCALAPPDATA%\Programs\OpenCode\OpenCode.exe",@"%LOCALAPPDATA%\OpenCode\OpenCode.exe",@"%ProgramFiles%\OpenCode\OpenCode.exe"} },
    new Preset{ Id="hermes", Name="Hermes Agent", ProcName="Hermes Agent", WinTitle="*Hermes", Paths=new[]{@"%LOCALAPPDATA%\Programs\Hermes Agent\Hermes Agent.exe"} },
    new Preset{ Id="outlook-new", Name="Outlook", OldName="Outlook (new)", ProcName="olk", WinClass="", WinTitle="*Outlook", AppxPrefix="Microsoft.OutlookForWindows_", AppxExe="olk.exe", Aumid="Microsoft.OutlookForWindows_8wekyb3d8bbwe!Microsoft.OutlookforWindows", Note="experimental" },
    new Preset{ Id="thunderbird", Name="Thunderbird", ProcName="thunderbird", WinClass="MozillaWindowClass", WinTitle="*Thunderbird", CtrlQ=true, Paths=new[]{@"%ProgramFiles%\Mozilla Thunderbird\thunderbird.exe"}, Note="experimental" },
    // Apps with their own close-to-tray/background option: shown as natively supported, no hook.
    new Preset{ Id="claude", Name="Claude", ProcName="claude", WinTitle="Claude", AppxPrefix="Claude_", AppxExe=@"app\Claude.exe", Aumid="Claude_pzs8sxrjxfjjc!Claude", Paths=new[]{@"%LOCALAPPDATA%\AnthropicClaude\claude.exe"}, SettingsKeys="^,", Native=true, Hint="Claude: Settings → General → \"Show Claude in menu bar\"" },
    new Preset{ Id="chatgpt", Name="ChatGPT / Codex", ProcName="ChatGPT", WinTitle="ChatGPT", AppxPrefix="OpenAI.Codex_", AppxExe=@"app\ChatGPT.exe", Aumid="OpenAI.Codex_2p2nqsd0c76g0!App", SettingsKeys="^,", Native=true, Hint="Has its own background/tray option in app settings" },
    new Preset{ Id="typeless", Name="Typeless", ProcName="Typeless", WinTitle="", Paths=new[]{@"%LOCALAPPDATA%\Programs\Typeless\Typeless.exe"}, Native=true, Hint="Runs in the tray by itself; options are in app settings" },
    new Preset{ Id="spotify", Name="Spotify", ProcName="Spotify", WinTitle="", Paths=new[]{@"%APPDATA%\Spotify\Spotify.exe"}, AppxPrefix="SpotifyAB.SpotifyMusic_", AppxExe="Spotify.exe", Aumid="SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", SettingsKeys="^p", Native=true, Hint="Settings → Startup and window behaviour → close button minimizes to tray" },
    new Preset{ Id="line", Name="LINE", ProcName="LINE", WinTitle="LINE", Paths=new[]{@"%LOCALAPPDATA%\LINE\bin\current\LINE.exe",@"%LOCALAPPDATA%\LINE\bin\LineLauncher.exe"}, Native=true, Hint="Already stays in the tray when closed" },
    new Preset{ Id="lmstudio", Name="LM Studio", ProcName="LM Studio", WinTitle="", Paths=new[]{@"%LOCALAPPDATA%\Programs\LM Studio\LM Studio.exe",@"%LOCALAPPDATA%\Programs\lm-studio\LM Studio.exe"}, Native=true, Hint="Has a tray option in app settings" },
  };
  public sealed class Found{ public Preset P; public bool Ok; public string Launch, Icon; }
  public static List<Found> DetectAll(){ var r=new List<Found>(); foreach(var p in All){ var f=new Found(); f.P=p; try{ f.Ok=Detect(p,out f.Launch,out f.Icon); }catch{} r.Add(f); } return r; }
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
  public static void Merge(List<AppTarget> apps){ Apply(apps,DetectAll()); }
  public static void Apply(List<AppTarget> apps,List<Found> found){
    foreach(var f in found){ var p=f.P;
      var a=apps.Find(x=>(x.Id!=null&&x.Id==p.Id)||string.Equals(x.Name,p.Name,StringComparison.OrdinalIgnoreCase)||(p.OldName!=null&&string.Equals(x.Name,p.OldName,StringComparison.OrdinalIgnoreCase)));
      if(a==null){ if(!f.Ok) continue; a=new AppTarget(); a.ExePath=f.Launch; a.Enabled=p.DefaultOn; apps.Add(a); TrayHelper.Log("catalog: detected "+p.Name); }
      a.Id=p.Id; a.Name=p.Name; a.Preset=true; a.Detected=f.Ok; a.Note=p.Note; a.NativeTray=p.NativeTray; a.Native=p.Native; a.Hint=p.Hint; a.SettingsKeys=p.SettingsKeys; a.CtrlQ=a.CtrlQ||p.CtrlQ;
      a.ProcName=p.ProcName; a.WinClass=p.WinClass; a.WinTitle=p.WinTitle; if(a.Native) a.Enabled=false;
      if(f.Icon!=null) a.IconExe=f.Icon; if(string.IsNullOrEmpty(a.ExePath)&&f.Launch!=null) a.ExePath=f.Launch; }
    foreach(var a in apps) if(!a.Preset){ if(a.IconExe==null&&a.ExePath!=null&&File.Exists(a.ExePath)) a.IconExe=a.ExePath; if(a.IconExe==null) a.IconExe=RunningExe(a.ProcName); }
  }
}
