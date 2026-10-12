// Modern WPF UI (Windows 11 style) built in code: tray flyout (right-click) + settings window (left-click).
using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using System.Linq; using System.Reflection; using System.Runtime.InteropServices; using System.Text;
using System.Windows; using System.Windows.Controls; using System.Windows.Controls.Primitives; using System.Windows.Interop; using System.Windows.Markup; using System.Windows.Media; using System.Windows.Media.Effects; using System.Windows.Media.Imaging; using System.Windows.Threading;
using Microsoft.Win32;

static class Theme{
  public static bool Dark;
  public static ResourceDictionary Res;
  public static Brush Bg, Card, Line, Text, Sub, Accent, Hover;
  static Brush B(string hex){ var b=(Brush)new BrushConverter().ConvertFromString(hex); b.Freeze(); return b; }
  public static void Init(){
    if(Res!=null) return;
    try{ using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")){ var v=k==null?null:k.GetValue("AppsUseLightTheme"); Dark=v is int&&(int)v==0; } }catch{}
    string bg=Dark?"#202020":"#F3F3F3", card=Dark?"#2B2B2B":"#FFFFFF", border=Dark?"#3A3A3A":"#E3E3E3", text=Dark?"#FFFFFF":"#1B1B1B", sub=Dark?"#A8A8A8":"#616161", acc=Dark?"#7B8CFF":"#4F46E5", knobOn=Dark?"#101010":"#FFFFFF", hover=Dark?"#353535":"#F0F0F0", btn=Dark?"#373737":"#FBFBFB";
    Bg=B(bg); Card=B(card); Line=B(border); Text=B(text); Sub=B(sub); Accent=B(acc); Hover=B(hover);
    var x=@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style x:Key='Switch' TargetType='ToggleButton'>
    <Setter Property='Cursor' Value='Hand'/><Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ToggleButton'>
      <Grid Width='40' Height='20' Background='Transparent'>
        <Border x:Name='T' CornerRadius='10' BorderThickness='1' BorderBrush='$SUB' Background='Transparent'/>
        <Ellipse x:Name='K' Width='12' Height='12' HorizontalAlignment='Left' Margin='4,0' Fill='$SUB'/>
      </Grid>
      <ControlTemplate.Triggers>
        <Trigger Property='IsChecked' Value='True'><Setter TargetName='T' Property='Background' Value='$ACC'/><Setter TargetName='T' Property='BorderBrush' Value='$ACC'/><Setter TargetName='K' Property='HorizontalAlignment' Value='Right'/><Setter TargetName='K' Property='Fill' Value='$KNOBON'/></Trigger>
        <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='K' Property='Width' Value='14'/><Setter TargetName='K' Property='Height' Value='14'/><Setter TargetName='K' Property='Margin' Value='3,0'/></Trigger>
        <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.4'/></Trigger>
      </ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style x:Key='Btn' TargetType='Button'>
    <Setter Property='Foreground' Value='$TEXT'/><Setter Property='Cursor' Value='Hand'/><Setter Property='FontSize' Value='13'/><Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
      <Border x:Name='B' CornerRadius='4' Background='$BTN' BorderBrush='$BORDER' BorderThickness='1' Padding='12,5'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border>
      <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='B' Property='Background' Value='$HOVER'/></Trigger></ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style x:Key='Accent' TargetType='Button' BasedOn='{StaticResource Btn}'>
    <Setter Property='Foreground' Value='$KNOBON'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
      <Border x:Name='B' CornerRadius='4' Background='$ACC' Padding='12,5'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border>
      <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='B' Property='Opacity' Value='0.9'/></Trigger></ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style x:Key='Nav' TargetType='RadioButton'>
    <Setter Property='Foreground' Value='$TEXT'/><Setter Property='FontSize' Value='14'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='RadioButton'>
      <Border x:Name='B' CornerRadius='4' Background='Transparent' Padding='12,8' Margin='0,2'>
        <Grid><Border x:Name='I' Width='3' Height='16' CornerRadius='2' Background='$ACC' HorizontalAlignment='Left' Margin='-8,0,0,0' Visibility='Collapsed'/><ContentPresenter/></Grid></Border>
      <ControlTemplate.Triggers>
        <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='B' Property='Background' Value='$HOVER'/></Trigger>
        <Trigger Property='IsChecked' Value='True'><Setter TargetName='B' Property='Background' Value='$HOVER'/><Setter TargetName='I' Property='Visibility' Value='Visible'/></Trigger>
      </ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
  <Style x:Key='Link' TargetType='Button'>
    <Setter Property='Foreground' Value='$SUB'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
      <Border x:Name='B' CornerRadius='4' Background='Transparent' Padding='8,4'><ContentPresenter VerticalAlignment='Center'/></Border>
      <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='B' Property='Background' Value='$HOVER'/></Trigger></ControlTemplate.Triggers>
    </ControlTemplate></Setter.Value></Setter>
  </Style>
</ResourceDictionary>";
    x=x.Replace("$SUB",sub).Replace("$ACC",acc).Replace("$KNOBON",knobOn).Replace("$TEXT",text).Replace("$BTN",btn).Replace("$BORDER",border).Replace("$HOVER",hover);
    Res=(ResourceDictionary)XamlReader.Parse(x);
  }
  public static readonly FontFamily Font=new FontFamily("Segoe UI Variable Text, Segoe UI");
  public static readonly FontFamily Icons=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
  public static TextBlock T(string s,double size,Brush fg,FontWeight w){ var t=new TextBlock(); t.Text=s; t.FontSize=size; t.Foreground=fg; t.FontWeight=w; t.FontFamily=Font; t.TextTrimming=TextTrimming.CharacterEllipsis; t.VerticalAlignment=VerticalAlignment.Center; return t; }
  public static TextBlock Glyph(string g,double size,Brush fg){ var t=new TextBlock(); t.Text=g; t.FontFamily=Icons; t.FontSize=size; t.Foreground=fg; t.VerticalAlignment=VerticalAlignment.Center; return t; }
  public static ToggleButton Switch(bool on){ var t=new ToggleButton(); t.Style=(Style)Res["Switch"]; t.IsChecked=on; t.VerticalAlignment=VerticalAlignment.Center; return t; }
  public static Button MkBtn(object content,string style){ var b=new Button(); b.Content=content; b.Style=(Style)Res[style]; b.VerticalAlignment=VerticalAlignment.Center; return b; }
  static readonly Dictionary<string,ImageSource> iconCache=new Dictionary<string,ImageSource>(StringComparer.OrdinalIgnoreCase);
  public static ImageSource AppIcon(string exe){
    if(string.IsNullOrEmpty(exe)) return Logo;
    ImageSource s; if(iconCache.TryGetValue(exe,out s)) return s;
    try{ using(var ic=System.Drawing.Icon.ExtractAssociatedIcon(exe)){ s=Imaging.CreateBitmapSourceFromHIcon(ic.Handle,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions()); s.Freeze(); } }catch{ s=Logo; }
    iconCache[exe]=s; return s; }
  static ImageSource logo;
  public static ImageSource Logo{ get{ if(logo==null){ try{ var bi=new BitmapImage(); bi.BeginInit(); bi.StreamSource=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"); bi.CacheOption=BitmapCacheOption.OnLoad; bi.EndInit(); bi.Freeze(); logo=bi; }catch{} } return logo; } }
  public static Image Img(ImageSource s,double size){ var i=new Image(); i.Source=s; i.Width=size; i.Height=size; i.VerticalAlignment=VerticalAlignment.Center; RenderOptions.SetBitmapScalingMode(i,BitmapScalingMode.HighQuality); return i; }
  [DllImport("dwmapi")] static extern int DwmSetWindowAttribute(IntPtr h,int a,ref int v,int s);
  public static void DarkTitle(Window w){ try{ var h=new WindowInteropHelper(w).EnsureHandle(); int v=Dark?1:0; DwmSetWindowAttribute(h,20,ref v,4); int c=2; DwmSetWindowAttribute(h,33,ref c,4); }catch{} }
  public static string StatusText(AppTarget a){ var s=TrayHelper.StateOf(a); string t=s=="hidden"?"Hidden in tray":s=="visible"?"Running":"Not running"; if(!string.IsNullOrEmpty(a.Note)) t+=" · "+a.Note; return t; }
}

partial class TrayHelper{
  Window flyout; SettingsWindow settingsWin;
  static IEnumerable<AppTarget> Visible(){ return Apps.Where(a=>a.Detected||!a.Preset).ToList(); }
  static void Interop(Window w){ try{ System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(w); }catch{} }
  public void ShowFlyout(){
    Theme.Init();
    if(flyout==null){
      flyout=new Window(); flyout.WindowStyle=WindowStyle.None; flyout.AllowsTransparency=true; flyout.Background=Brushes.Transparent; flyout.Topmost=true; flyout.ShowInTaskbar=false; flyout.ResizeMode=ResizeMode.NoResize;
      flyout.Width=340; flyout.SizeToContent=SizeToContent.Height; flyout.Resources.MergedDictionaries.Add(Theme.Res); flyout.Title="TrayHelper flyout";
      flyout.Deactivated+=(s,e)=>flyout.Hide();
      flyout.SizeChanged+=(s,e)=>PlaceFlyout();
      Interop(flyout);
    }
    var outer=new Border(); outer.Margin=new Thickness(12); outer.CornerRadius=new CornerRadius(8); outer.Background=Theme.Bg; outer.BorderBrush=Theme.Line; outer.BorderThickness=new Thickness(1);
    outer.Effect=new DropShadowEffect{ BlurRadius=16, ShadowDepth=2, Opacity=0.35, Direction=270 };
    var st=new StackPanel(); st.Margin=new Thickness(16,14,16,10); outer.Child=st;
    var head=new DockPanel(); head.Margin=new Thickness(0,0,0,10);
    var gear=Theme.MkBtn(Theme.Glyph("\uE713",15,Theme.Sub),"Link"); gear.ToolTip="Settings"; gear.Click+=(s,e)=>{ flyout.Hide(); ShowSettings(); }; DockPanel.SetDock(gear,Dock.Right); head.Children.Add(gear);
    var hl=new StackPanel(); hl.Orientation=Orientation.Horizontal; hl.Children.Add(Theme.Img(Theme.Logo,20)); var ht=Theme.T("TrayHelper",15,Theme.Text,FontWeights.SemiBold); ht.Margin=new Thickness(8,0,0,0); hl.Children.Add(ht); head.Children.Add(hl);
    st.Children.Add(head);
    var cap=Theme.T("Close to tray",12,Theme.Sub,FontWeights.Normal); cap.Margin=new Thickness(2,0,0,6); st.Children.Add(cap);
    var list=new Border(); list.CornerRadius=new CornerRadius(6); list.Background=Theme.Card; list.BorderBrush=Theme.Line; list.BorderThickness=new Thickness(1);
    var rows=new StackPanel(); list.Child=rows; bool first=true;
    foreach(var a0 in Visible()){ var a=a0;
      var row=new Grid(); row.Margin=new Thickness(12,9,12,9);
      row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
      row.Children.Add(Theme.Img(Theme.AppIcon(a.IconExe),24));
      var tx=new StackPanel(); tx.Margin=new Thickness(12,0,8,0); tx.VerticalAlignment=VerticalAlignment.Center; tx.Children.Add(Theme.T(a.Name,13.5,Theme.Text,FontWeights.Normal)); tx.Children.Add(Theme.T(Theme.StatusText(a),11.5,Theme.Sub,FontWeights.Normal));
      Grid.SetColumn(tx,1); row.Children.Add(tx);
      var sw=Theme.Switch(a.Enabled); sw.Click+=(s,e)=>SetEnabled(a,sw.IsChecked==true); Grid.SetColumn(sw,2); row.Children.Add(sw);
      var wrap=new Border(); wrap.Child=row; wrap.Background=Brushes.Transparent; wrap.Cursor=System.Windows.Input.Cursors.Hand; wrap.ToolTip="Click to show "+a.Name;
      tx.MouseLeftButtonUp+=(s,e)=>{ flyout.Hide(); ShowApp(a); };
      if(!first){ wrap.BorderBrush=Theme.Line; wrap.BorderThickness=new Thickness(0,1,0,0); } first=false;
      rows.Children.Add(wrap); }
    if(first){ var none=Theme.T("No supported apps found.",13,Theme.Sub,FontWeights.Normal); none.Margin=new Thickness(12); rows.Children.Add(none); }
    st.Children.Add(list);
    var foot=new DockPanel(); foot.Margin=new Thickness(0,8,0,0);
    var exit=Theme.MkBtn(Theme.T("Exit",12.5,Theme.Sub,FontWeights.Normal),"Link"); exit.ToolTip="Exit TrayHelper (hidden windows are restored)"; exit.Click+=(s,e)=>{ flyout.Hide(); ExitHelper(); }; DockPanel.SetDock(exit,Dock.Right); foot.Children.Add(exit);
    var more=Theme.MkBtn(Theme.T("Open settings",12.5,Theme.Accent,FontWeights.Normal),"Link"); more.Click+=(s,e)=>{ flyout.Hide(); ShowSettings(); }; more.HorizontalAlignment=HorizontalAlignment.Left; foot.Children.Add(more);
    st.Children.Add(foot);
    flyout.Content=outer;
    flyout.Show(); PlaceFlyout(); flyout.Activate();
  }
  void PlaceFlyout(){ if(flyout==null) return; var wa=SystemParameters.WorkArea; flyout.Left=wa.Right-flyout.ActualWidth; flyout.Top=wa.Bottom-flyout.ActualHeight; }
  public void ShowSettings(){
    Theme.Init(); if(flyout!=null) flyout.Hide();
    try{ Catalog.Merge(Apps); }catch{}
    if(settingsWin==null){ settingsWin=new SettingsWindow(this); Interop(settingsWin); }
    settingsWin.Rebuild(); settingsWin.Show(); if(settingsWin.WindowState==WindowState.Minimized) settingsWin.WindowState=WindowState.Normal; settingsWin.Activate();
  }
  void CloseWpf(){ if(flyout!=null){ flyout.Close(); flyout=null; } if(settingsWin!=null){ settingsWin.ReallyClose(); settingsWin=null; } }
  const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
  public static bool StartWithWindows{
    get{ try{ using(var k=Registry.CurrentUser.OpenSubKey(RunKey)) return k!=null&&k.GetValue("TrayHelper")!=null; }catch{ return false; } }
    set{ try{ using(var k=Registry.CurrentUser.CreateSubKey(RunKey)){ if(value) k.SetValue("TrayHelper","\""+System.Windows.Forms.Application.ExecutablePath+"\""); else k.DeleteValue("TrayHelper",false); } Log("startWithWindows="+value); }catch(Exception ex){ Log("run key "+ex.Message); } } }
}

sealed class SettingsWindow:Window{
  readonly TrayHelper th; bool reallyClose; ContentControl page; RadioButton navApps, navGeneral, navAbout;
  readonly Dictionary<AppTarget,TextBlock> statusLabels=new Dictionary<AppTarget,TextBlock>(); DispatcherTimer timer;
  public SettingsWindow(TrayHelper th){ this.th=th; Title="TrayHelper"; Width=780; Height=540; MinWidth=620; MinHeight=400; WindowStartupLocation=WindowStartupLocation.CenterScreen;
    Background=Theme.Bg; FontFamily=Theme.Font; Resources.MergedDictionaries.Add(Theme.Res); Icon=Theme.Logo;
    SourceInitialized+=(s,e)=>Theme.DarkTitle(this);
    var root=new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(220)}); root.ColumnDefinitions.Add(new ColumnDefinition());
    var side=new StackPanel(); side.Margin=new Thickness(14,16,8,16);
    var brand=new StackPanel(); brand.Orientation=Orientation.Horizontal; brand.Margin=new Thickness(8,0,0,18); brand.Children.Add(Theme.Img(Theme.Logo,28)); var bt=Theme.T("TrayHelper",16,Theme.Text,FontWeights.SemiBold); bt.Margin=new Thickness(10,0,0,0); brand.Children.Add(bt); side.Children.Add(brand);
    navApps=Nav("\uE71D","Apps"); navGeneral=Nav("\uE713","General"); navAbout=Nav("\uE946","About");
    side.Children.Add(navApps); side.Children.Add(navGeneral); side.Children.Add(navAbout);
    root.Children.Add(side);
    var sv=new ScrollViewer(); sv.VerticalScrollBarVisibility=ScrollBarVisibility.Auto; page=new ContentControl(); page.Margin=new Thickness(20,18,28,24); sv.Content=page; Grid.SetColumn(sv,1); root.Children.Add(sv);
    Content=root; navApps.IsChecked=true;
    timer=new DispatcherTimer(); timer.Interval=TimeSpan.FromSeconds(1); timer.Tick+=(s,e)=>{ foreach(var kv in statusLabels) kv.Value.Text=Theme.StatusText(kv.Key); }; timer.Start();
  }
  RadioButton Nav(string glyph,string text){ var r=new RadioButton(); r.Style=(Style)Theme.Res["Nav"]; r.GroupName="nav";
    var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; sp.Children.Add(Theme.Glyph(glyph,16,Theme.Text)); var t=Theme.T(text,14,Theme.Text,FontWeights.Normal); t.Margin=new Thickness(12,0,0,0); sp.Children.Add(t); r.Content=sp;
    r.Checked+=(s,e)=>Rebuild(); return r; }
  public void Rebuild(){ statusLabels.Clear(); if(navGeneral.IsChecked==true) page.Content=GeneralPage(); else if(navAbout.IsChecked==true) page.Content=AboutPage(); else page.Content=AppsPage(); }
  static Border Card(UIElement child){ var b=new Border(); b.CornerRadius=new CornerRadius(6); b.Background=Theme.Card; b.BorderBrush=Theme.Line; b.BorderThickness=new Thickness(1); b.Padding=new Thickness(16,12,16,12); b.Margin=new Thickness(0,0,0,4); b.Child=child; return b; }
  static StackPanel Header(string title,string sub){ var s=new StackPanel(); s.Margin=new Thickness(0,0,0,16); s.Children.Add(Theme.T(title,26,Theme.Text,FontWeights.SemiBold)); var d=Theme.T(sub,13,Theme.Sub,FontWeights.Normal); d.TextWrapping=TextWrapping.Wrap; d.Margin=new Thickness(0,4,0,0); s.Children.Add(d); return s; }
  UIElement AppsPage(){
    var st=new StackPanel(); st.Children.Add(Header("Apps","Supported apps found on this PC. Turn on \"Close to tray\" and the X button / Alt+F4 hides the window instead of quitting, so the app keeps running in the background."));
    var bar=new StackPanel(); bar.Orientation=Orientation.Horizontal; bar.Margin=new Thickness(0,0,0,12);
    var add=Theme.MkBtn("Add app…","Accent"); add.Click+=(s,e)=>AddApp(); bar.Children.Add(add);
    var scan=Theme.MkBtn("Rescan","Btn"); scan.Margin=new Thickness(8,0,0,0); scan.Click+=(s,e)=>{ try{ Catalog.Merge(TrayHelper.Apps); TrayHelper.SaveAll(); }catch{} Rebuild(); }; bar.Children.Add(scan);
    st.Children.Add(bar);
    foreach(var a0 in TrayHelper.Apps.Where(x=>x.Detected||!x.Preset).ToList()){ var a=a0;
      var g=new Grid(); foreach(var w in new[]{GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto,GridLength.Auto,GridLength.Auto}) g.ColumnDefinitions.Add(new ColumnDefinition{Width=w});
      g.Children.Add(Theme.Img(Theme.AppIcon(a.IconExe),32));
      var tx=new StackPanel(); tx.Margin=new Thickness(14,0,12,0); tx.VerticalAlignment=VerticalAlignment.Center;
      tx.Children.Add(Theme.T(a.Name+(a.Preset?"":"  (custom)"),14,Theme.Text,FontWeights.Normal)); var stt=Theme.T(Theme.StatusText(a),12,Theme.Sub,FontWeights.Normal); statusLabels[a]=stt; tx.Children.Add(stt);
      Grid.SetColumn(tx,1); g.Children.Add(tx);
      var show=Theme.MkBtn("Show","Btn"); show.Click+=(s,e)=>th.Show_(a); show.Margin=new Thickness(0,0,16,0); Grid.SetColumn(show,2); g.Children.Add(show);
      var wd=Labeled("Watchdog",a.Watchdog,v=>th.SetWatchdog(a,v)); wd.ToolTip="Relaunch hidden if the app closes unexpectedly"; wd.Margin=new Thickness(0,0,18,0); Grid.SetColumn(wd,3); g.Children.Add(wd);
      var ct=Labeled("Close to tray",a.Enabled,v=>th.SetEnabled(a,v)); Grid.SetColumn(ct,4); g.Children.Add(ct);
      var card=Card(g);
      if(!a.Preset){ var cm=new ContextMenu(); var mi=new MenuItem(); mi.Header="Remove "+a.Name; mi.Click+=(s,e)=>{ if(MessageBox.Show(this,"Remove "+a.Name+" from TrayHelper?","TrayHelper",MessageBoxButton.YesNo)==MessageBoxResult.Yes){ th.RemoveApp(a); Rebuild(); } }; cm.Items.Add(mi); card.ContextMenu=cm; card.ToolTip="Right-click to remove"; }
      st.Children.Add(card); }
    var hint=Theme.T("Custom apps: right-click a card to remove it.",12,Theme.Sub,FontWeights.Normal); hint.Margin=new Thickness(2,10,0,0); st.Children.Add(hint);
    return st; }
  static StackPanel Labeled(string label,bool on,Action<bool> set){ var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; sp.VerticalAlignment=VerticalAlignment.Center;
    var t=Theme.T(label,12.5,Theme.Sub,FontWeights.Normal); t.Margin=new Thickness(0,0,10,0); sp.Children.Add(t); var sw=Theme.Switch(on); sw.Click+=(s,e)=>set(sw.IsChecked==true); sp.Children.Add(sw); return sp; }
  UIElement GeneralPage(){ var st=new StackPanel(); st.Children.Add(Header("General","Startup and configuration."));
    st.Children.Add(Row("Start with Windows","Launch TrayHelper in the tray when you sign in.",Labeled("",TrayHelper.StartWithWindows,v=>TrayHelper.StartWithWindows=v)));
    var open=Theme.MkBtn("Open folder","Btn"); open.Click+=(s,e)=>{ try{ Process.Start("explorer.exe","/select,\""+TrayHelper.IniPath+"\""); }catch{} };
    st.Children.Add(Row("Config file",TrayHelper.IniPath,open));
    var log=Theme.MkBtn("Open log","Btn"); log.Click+=(s,e)=>{ try{ Process.Start(Path.Combine(TrayHelper.Dir,"tray-helper.log")); }catch{} };
    st.Children.Add(Row("Log","tray-helper.log next to the exe",log));
    return st; }
  static Border Row(string title,string sub,UIElement right){ var g=new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
    var tx=new StackPanel(); tx.Children.Add(Theme.T(title,14,Theme.Text,FontWeights.Normal)); var s=Theme.T(sub,12,Theme.Sub,FontWeights.Normal); tx.Children.Add(s); g.Children.Add(tx); Grid.SetColumn(right,1); g.Children.Add(right); return Card(g); }
  UIElement AboutPage(){ var st=new StackPanel();
    var h=new StackPanel(); h.Orientation=Orientation.Horizontal; h.Margin=new Thickness(0,0,0,16); h.Children.Add(Theme.Img(Theme.Logo,64));
    var ht=new StackPanel(); ht.Margin=new Thickness(16,0,0,0); ht.VerticalAlignment=VerticalAlignment.Center; ht.Children.Add(Theme.T("TrayHelper",26,Theme.Text,FontWeights.SemiBold)); ht.Children.Add(Theme.T("Close the window, keep the app.  ·  MVP 0.2  ·  MIT, unofficial",13,Theme.Sub,FontWeights.Normal)); h.Children.Add(ht); st.Children.Add(h);
    var rm=new StackPanel(); rm.Children.Add(Theme.T("Roadmap",15,Theme.Text,FontWeights.SemiBold));
    foreach(var line in new[]{"Background auto-update of TrayHelper","Detect when an app ships its own close-to-tray option, then retire our toggle and guide you to the official setting","Watchdog crash-loop backoff","Installer, README, Store-app auto-detect for custom apps"}){ var t=Theme.T("•  "+line,13,Theme.Sub,FontWeights.Normal); t.TextWrapping=TextWrapping.Wrap; t.Margin=new Thickness(0,6,0,0); rm.Children.Add(t); }
    st.Children.Add(Card(rm)); return st; }
  void AddApp(){ var p=new PickerWindow(); p.Owner=this; if(p.ShowDialog()==true&&p.Result!=null){
      if(TrayHelper.Apps.Any(x=>string.Equals(x.Name,p.Result.Name,StringComparison.OrdinalIgnoreCase))){ MessageBox.Show(this,p.Result.Name+" is already managed.","TrayHelper"); return; }
      th.AddApp(p.Result); Rebuild(); } }
  public void ReallyClose(){ reallyClose=true; timer.Stop(); Close(); }
  protected override void OnClosing(System.ComponentModel.CancelEventArgs e){ if(!reallyClose){ e.Cancel=true; Hide(); return; } base.OnClosing(e); }
}

sealed class PickerWindow:Window{
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
  public AppTarget Result; readonly ListBox list;
  sealed class Cand{ public string Title, Proc, Cls, Exe, Launch; }
  public PickerWindow(){ Title="Add app"; Width=560; Height=480; WindowStartupLocation=WindowStartupLocation.CenterOwner; Background=Theme.Bg; FontFamily=Theme.Font; Resources.MergedDictionaries.Add(Theme.Res); ShowInTaskbar=false;
    SourceInitialized+=(s,e)=>Theme.DarkTitle(this);
    var dp=new DockPanel(); dp.Margin=new Thickness(20);
    var hd=new StackPanel(); hd.Margin=new Thickness(0,0,0,12); hd.Children.Add(Theme.T("Pick a running window",20,Theme.Text,FontWeights.SemiBold)); var sub=Theme.T("TrayHelper fills in process, window class, title and launch path automatically.",12.5,Theme.Sub,FontWeights.Normal); sub.TextWrapping=TextWrapping.Wrap; hd.Children.Add(sub);
    DockPanel.SetDock(hd,Dock.Top); dp.Children.Add(hd);
    var btns=new StackPanel(); btns.Orientation=Orientation.Horizontal; btns.HorizontalAlignment=HorizontalAlignment.Right; btns.Margin=new Thickness(0,12,0,0);
    var ok=Theme.MkBtn("Add","Accent"); ok.Click+=(s,e)=>Pick(); var cancel=Theme.MkBtn("Cancel","Btn"); cancel.Margin=new Thickness(8,0,0,0); cancel.Click+=(s,e)=>{ DialogResult=false; };
    btns.Children.Add(ok); btns.Children.Add(cancel); DockPanel.SetDock(btns,Dock.Bottom); dp.Children.Add(btns);
    list=new ListBox(); list.Background=Theme.Card; list.BorderBrush=Theme.Line; list.Foreground=Theme.Text; list.MouseDoubleClick+=(s,e)=>Pick(); dp.Children.Add(list);
    Content=dp; Fill(); }
  void Fill(){ int me=Process.GetCurrentProcess().Id;
    EnumWindows((h,l)=>{ if(!IsWindowVisible(h)||GetWindow(h,4)!=IntPtr.Zero) return true;
      var t=new StringBuilder(256); GetWindowText(h,t,256); if(t.Length==0) return true;
      var c=new StringBuilder(256); GetClassName(h,c,256); uint pid; GetWindowThreadProcessId(h,out pid); if(pid==me) return true;
      if(c.ToString()=="Progman"||c.ToString()=="Shell_TrayWnd"||c.ToString()=="Windows.UI.Core.CoreWindow") return true;
      string pn=null; try{ using(var p=Process.GetProcessById((int)pid)) pn=p.ProcessName; }catch{} if(pn==null) return true;
      string exe=null, aumid=null; var ph=OpenProcess(0x1000,false,pid);
      if(ph!=IntPtr.Zero){ try{ var sb=new StringBuilder(1024); int n=sb.Capacity; if(QueryFullProcessImageName(ph,0,sb,ref n)) exe=sb.ToString();
          var sa=new StringBuilder(256); int na=sa.Capacity; try{ if(GetApplicationUserModelId(ph,ref na,sa)==0) aumid=sa.ToString(); }catch(EntryPointNotFoundException){} } finally{ CloseHandle(ph); } }
      var cd=new Cand{ Title=t.ToString(), Proc=pn, Cls=c.ToString(), Exe=exe, Launch=aumid!=null?@"shell:AppsFolder\"+aumid:exe };
      var row=new StackPanel(); row.Orientation=Orientation.Horizontal; row.Margin=new Thickness(4,5,4,5); row.Children.Add(Theme.Img(Theme.AppIcon(exe),24));
      var tx=new StackPanel(); tx.Margin=new Thickness(10,0,0,0); tx.Children.Add(Theme.T(cd.Title,13.5,Theme.Text,FontWeights.Normal)); tx.Children.Add(Theme.T(pn+"  ·  "+cd.Cls+(aumid!=null?"  ·  Store app":""),11.5,Theme.Sub,FontWeights.Normal)); row.Children.Add(tx);
      var it=new ListBoxItem(); it.Content=row; it.Tag=cd; list.Items.Add(it); return true; },IntPtr.Zero); }
  void Pick(){ var it=list.SelectedItem as ListBoxItem; if(it==null) return; var cd=(Cand)it.Tag;
    var a=new AppTarget(); a.WinTitle=cd.Title; a.Name=cd.Title.Length>40?cd.Proc:cd.Title; a.ProcName=cd.Proc; a.WinClass=cd.Cls; a.ExePath=cd.Launch; a.IconExe=cd.Exe; a.Enabled=true; a.Watchdog=false; a.Preset=false; a.Detected=true;
    Result=a; DialogResult=true; }
}
