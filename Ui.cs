using System.Windows.Media.Animation;
// Modern WPF UI (Windows 11 style) built in code: tray flyout (right-click) + settings window (left-click).
using System; using System.Collections.Generic; using System.Diagnostics; using System.IO; using System.Linq; using System.Reflection; using System.Runtime.InteropServices; using System.Text;
using System.Windows; using System.Windows.Controls; using System.Windows.Controls.Primitives; using System.Windows.Interop; using System.Windows.Markup; using System.Windows.Media; using System.Windows.Media.Effects; using System.Windows.Media.Imaging; using System.Windows.Threading;
using Microsoft.Win32; using Ellipse=System.Windows.Shapes.Ellipse;

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
    if(exe.EndsWith(".png",StringComparison.OrdinalIgnoreCase)){ try{ var bi=new BitmapImage(); bi.BeginInit(); bi.UriSource=new Uri(exe); bi.CacheOption=BitmapCacheOption.OnLoad; bi.DecodePixelWidth=64; bi.EndInit(); bi.Freeze(); s=bi; }catch{ s=Logo; } iconCache[exe]=s; return s; } // Store app logo
    try{ using(var ic=System.Drawing.Icon.ExtractAssociatedIcon(exe)){ s=Imaging.CreateBitmapSourceFromHIcon(ic.Handle,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions()); s.Freeze(); } }catch{ s=Logo; }
    iconCache[exe]=s; return s; }
  static ImageSource logo;
  public static ImageSource Logo{ get{ if(logo==null){ try{ var bi=new BitmapImage(); bi.BeginInit(); bi.StreamSource=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"); bi.CacheOption=BitmapCacheOption.OnLoad; bi.EndInit(); bi.Freeze(); logo=bi; }catch{} } return logo; } }
  public static Image Img(ImageSource s,double size){ var i=new Image(); i.Source=s; i.Width=size; i.Height=size; i.VerticalAlignment=VerticalAlignment.Center; RenderOptions.SetBitmapScalingMode(i,BitmapScalingMode.HighQuality); return i; }
  [DllImport("dwmapi")] static extern int DwmSetWindowAttribute(IntPtr h,int a,ref int v,int s);
  public static void DarkTitle(Window w){ try{ var h=new WindowInteropHelper(w).EnsureHandle(); int v=Dark?1:0; DwmSetWindowAttribute(h,20,ref v,4); int c=2; DwmSetWindowAttribute(h,33,ref c,4); }catch{} }
  // Drag-to-reorder: drag starts from non-button areas after a few pixels.
  public static void Draggable(FrameworkElement el,AppTarget a,Action<AppTarget,AppTarget,bool> drop){
    Point start=new Point(); bool armed=false; el.AllowDrop=true;
    el.PreviewMouseLeftButtonDown+=(s,e)=>{ armed=!InButton(e.OriginalSource as DependencyObject); start=e.GetPosition(el); };
    el.PreviewMouseLeftButtonUp+=(s,e)=>armed=false;
    el.PreviewMouseMove+=(s,e)=>{ if(!armed||e.LeftButton!=System.Windows.Input.MouseButtonState.Pressed) return; var p=e.GetPosition(el); if(Math.Abs(p.Y-start.Y)<6&&Math.Abs(p.X-start.X)<6) return; armed=false; el.Opacity=0.5; try{ DragDrop.DoDragDrop(el,new DataObject("th-app",a),DragDropEffects.Move); } finally{ el.Opacity=1; } };
    el.DragOver+=(s,e)=>{ e.Effects=e.Data.GetDataPresent("th-app")?DragDropEffects.Move:DragDropEffects.None; e.Handled=true; };
    el.Drop+=(s,e)=>{ var src=e.Data.GetData("th-app") as AppTarget; if(src==null||src==a) return; bool after=e.GetPosition(el).Y>el.ActualHeight/2; drop(src,a,after); e.Handled=true; }; }
  static bool InButton(DependencyObject d){ while(d!=null){ if(d is System.Windows.Controls.Primitives.ButtonBase) return true; d=d is Visual?VisualTreeHelper.GetParent(d):LogicalTreeHelper.GetParent(d); } return false; }
  // Grip-based reorder with mouse capture (no OLE drag-drop). Rows in the panel carry Tag=AppTarget.
  public static void Reorderable(Panel panel,FrameworkElement row,FrameworkElement grip,Action<List<AppTarget>> commit){
    bool drag=false; double startY=0; int from=0, to=0; List<FrameworkElement> rows=null; grip.Cursor=System.Windows.Input.Cursors.SizeNS;
    Func<FrameworkElement,TranslateTransform> tt=fe=>{ var t=fe.RenderTransform as TranslateTransform; if(t==null){ t=new TranslateTransform(); fe.RenderTransform=t; } return t; };
    Action<FrameworkElement,double> anim=(fe,y)=>{ var t=tt(fe); var da=new DoubleAnimation(y,TimeSpan.FromMilliseconds(160)); da.EasingFunction=new QuadraticEase{EasingMode=EasingMode.EaseOut}; t.BeginAnimation(TranslateTransform.YProperty,da); };
    Action end=()=>{ drag=false; row.Opacity=1; Panel.SetZIndex(row,0); row.Effect=null; if(rows==null) return; foreach(var r in rows){ var t=tt(r); t.BeginAnimation(TranslateTransform.YProperty,null); t.Y=0; } };
    grip.MouseLeftButtonDown+=(s,e)=>{ rows=panel.Children.OfType<FrameworkElement>().Where(c=>c.Tag is AppTarget).ToList(); from=to=rows.IndexOf(row); if(from<0) return;
      drag=grip.CaptureMouse(); startY=e.GetPosition(panel).Y; row.Opacity=0.92; Panel.SetZIndex(row,10); row.Effect=new DropShadowEffect{BlurRadius=14,ShadowDepth=2,Opacity=0.35,Direction=270}; e.Handled=true; };
    grip.MouseMove+=(s,e)=>{ if(!drag) return; double dy=e.GetPosition(panel).Y-startY; var t=tt(row); t.BeginAnimation(TranslateTransform.YProperty,null); t.Y=dy;
      double h=row.ActualHeight+row.Margin.Top+row.Margin.Bottom; double center=row.TranslatePoint(new Point(0,0),panel).Y-t.Y+dy+row.ActualHeight/2; int n=from;
      for(int i=0;i<rows.Count;i++){ if(rows[i]==row) continue; var r=rows[i]; double rc=r.TranslatePoint(new Point(0,0),panel).Y-tt(r).Y+r.ActualHeight/2; if(i>from&&center>rc) n=Math.Max(n,i); if(i<from&&center<rc) n=Math.Min(n,i); }
      if(n!=to){ to=n; for(int i=0;i<rows.Count;i++){ if(rows[i]==row) continue; double y=(i>from&&i<=to)?-h:(i<from&&i>=to)?h:0; anim(rows[i],y); } } };
    grip.MouseLeftButtonUp+=(s,e)=>{ if(!drag) return; grip.ReleaseMouseCapture(); end(); if(to!=from){ var lst=rows.ToList(); lst.RemoveAt(from); lst.Insert(to,row); int baseIdx=panel.Children.IndexOf(rows.OrderBy(r=>panel.Children.IndexOf(r)).First());
        foreach(var r in rows) panel.Children.Remove(r); for(int i=0;i<lst.Count;i++) panel.Children.Insert(baseIdx+i,lst[i]); commit(lst.Select(r=>(AppTarget)r.Tag).ToList()); } e.Handled=true; };
    grip.LostMouseCapture+=(s,e)=>{ if(drag) end(); }; }
  // Status dot on the app icon: green = window open, grey = hidden in the tray.
  static readonly Brush DotOpen=MkFrozen(Color.FromRgb(0x3F,0xB9,0x50)), DotHidden=MkFrozen(Color.FromRgb(0x8A,0x8A,0x8A));
  static Brush MkFrozen(Color c){ var b=new SolidColorBrush(c); b.Freeze(); return b; }
  public static void SetDot(Ellipse d,AppTarget a){ var st=TrayHelper.StateOf(a); d.Visibility=st=="not running"?Visibility.Collapsed:Visibility.Visible; d.Fill=st=="hidden"?DotHidden:DotOpen; }
  public static Grid IconDot(ImageSource src,double size,AppTarget a,out Ellipse dot){ var g=new Grid(); g.VerticalAlignment=VerticalAlignment.Center; g.Children.Add(Img(src,size));
    dot=new System.Windows.Shapes.Ellipse(); double ds=Math.Max(8,size*0.32); dot.Width=ds; dot.Height=ds; dot.HorizontalAlignment=HorizontalAlignment.Right; dot.VerticalAlignment=VerticalAlignment.Bottom; dot.Margin=new Thickness(0,0,-2,-2); dot.Stroke=Bg; dot.StrokeThickness=1.5; SetDot(dot,a); g.Children.Add(dot); return g; }
  // Shortcut recorder: click, press the keys. Esc cancels, Backspace/Delete clears. No dropdowns.
  public static Border HotkeyBox(string cur,Func<string,bool> set,Action<bool> recording){ var b=new Border(); b.CornerRadius=new CornerRadius(4); b.BorderThickness=new Thickness(1); b.BorderBrush=Line; b.Background=Card; b.Padding=new Thickness(10,5,10,5); b.MinWidth=150; b.Focusable=true; b.Cursor=System.Windows.Input.Cursors.Hand;
    var t=T(string.IsNullOrEmpty(cur)?"None":cur,13,Text,FontWeights.Normal); t.HorizontalAlignment=HorizontalAlignment.Center; b.Child=t; string val=cur??"";
    Action show=()=>{ t.Text=val.Length==0?"None":val; t.Foreground=Text; b.BorderBrush=Line; };
    b.MouseLeftButtonDown+=(s,e)=>{ b.Focus(); System.Windows.Input.Keyboard.Focus(b); e.Handled=true; };
    b.GotKeyboardFocus+=(s,e)=>{ recording(true); t.Text="Press keys…"; t.Foreground=Sub; b.BorderBrush=Accent; };
    b.LostKeyboardFocus+=(s,e)=>{ show(); recording(false); };
    b.PreviewKeyDown+=(s,e)=>{ e.Handled=true; var k=e.Key==System.Windows.Input.Key.System?e.SystemKey:e.Key; var m=System.Windows.Input.Keyboard.Modifiers;
      if(k==System.Windows.Input.Key.Escape){ System.Windows.Input.Keyboard.ClearFocus(); return; }
      if(k==System.Windows.Input.Key.Back||k==System.Windows.Input.Key.Delete){ val=""; set(""); System.Windows.Input.Keyboard.ClearFocus(); return; }
      var parts=new List<string>(); if((m&System.Windows.Input.ModifierKeys.Control)!=0) parts.Add("Ctrl"); if((m&System.Windows.Input.ModifierKeys.Alt)!=0) parts.Add("Alt"); if((m&System.Windows.Input.ModifierKeys.Shift)!=0) parts.Add("Shift"); if((m&System.Windows.Input.ModifierKeys.Windows)!=0) parts.Add("Win");
      bool modOnly=k==System.Windows.Input.Key.LeftCtrl||k==System.Windows.Input.Key.RightCtrl||k==System.Windows.Input.Key.LeftAlt||k==System.Windows.Input.Key.RightAlt||k==System.Windows.Input.Key.LeftShift||k==System.Windows.Input.Key.RightShift||k==System.Windows.Input.Key.LWin||k==System.Windows.Input.Key.RWin;
      if(modOnly||parts.Count==0||(parts.Count==1&&parts[0]=="Shift")){ t.Text=parts.Count>0?string.Join(" + ",parts)+" + …":"Press keys…"; return; }
      parts.Add(((System.Windows.Forms.Keys)System.Windows.Input.KeyInterop.VirtualKeyFromKey(k)).ToString()); var spec=string.Join("+",parts);
      recording(false); bool ok=set(spec); val=spec; System.Windows.Input.Keyboard.ClearFocus(); show(); if(!ok){ t.Text=spec+"  (in use)"; t.Foreground=Brushes.IndianRed; b.BorderBrush=Brushes.IndianRed; } };
    return b; }
  public static Border Badge(string text){ var b=new Border(); b.CornerRadius=new CornerRadius(4); b.Background=new SolidColorBrush(Color.FromArgb(0x33,0xF5,0xA6,0x23)); b.Padding=new Thickness(6,1,6,1); b.Margin=new Thickness(8,0,0,0); b.VerticalAlignment=VerticalAlignment.Center;
    b.Child=T(text,10.5,new SolidColorBrush(Color.FromRgb(0xF5,0xA6,0x23)),FontWeights.SemiBold); return b; }
  public static UIElement NameLine(AppTarget a,double size){ var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; sp.Children.Add(T(a.Name,size,Text,FontWeights.Normal)); if(a.Note=="experimental") sp.Children.Add(Badge("Experimental")); return sp; }
  public static TextBlock Grip(){ var g=T("⋮⋮",14,Sub,FontWeights.Bold); g.Margin=new Thickness(0,0,10,0); g.Background=Brushes.Transparent; g.Padding=new Thickness(2,6,2,6); return g; }
  public static string StatusText(AppTarget a){ var s=TrayHelper.StateOf(a); int hn=a.HidAlive; string t=s=="hidden"?(hn>1?"Hidden in tray · "+hn+" windows":"Hidden in tray"):s=="visible"?(hn>0?"Running · "+hn+" hidden":"Running"):"Not running"; if(!string.IsNullOrEmpty(a.Note)&&a.Note!="experimental") t+=" · "+a.Note; return t; }
}

partial class TrayHelper{
  Window flyout; SettingsWindow settingsWin;
  static IEnumerable<AppTarget> Visible(){ return Apps.Where(a=>a.Detected||!a.Preset).ToList(); }
  public void ApplyOrder(List<AppTarget> order){ var idx=order.Select(x=>Apps.IndexOf(x)).Where(i=>i>=0).OrderBy(i=>i).ToList(); for(int k=0;k<idx.Count&&k<order.Count;k++) Apps[idx[k]]=order[k]; snapApps=Apps.ToArray(); SaveAll(); Log("reordered"); }
  public void Reorder(AppTarget src,AppTarget target,bool after){ AppTarget before=target; if(after){ int i=Apps.IndexOf(target); before=i+1<Apps.Count?Apps[i+1]:null; if(before==src){ before=i+2<Apps.Count?Apps[i+2]:null; } } Move(src,before); Log("reordered "+src.Name); }
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
    var rows=new StackPanel(); list.Child=rows; bool first=false;
    { var row=new Grid(); row.Margin=new Thickness(12,9,12,9); row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
      row.Children.Add(Theme.Img(Theme.Logo,24)); var tx=new StackPanel(); tx.Margin=new Thickness(12,0,8,0); tx.VerticalAlignment=VerticalAlignment.Center; tx.Children.Add(Theme.T("TrayHelper",13.5,Theme.Text,FontWeights.Normal)); tx.Children.Add(Theme.T("Start with Windows",11.5,Theme.Sub,FontWeights.Normal)); Grid.SetColumn(tx,1); row.Children.Add(tx);
      var sw=Theme.Switch(StartWithWindows); sw.Click+=(s,e)=>StartWithWindows=sw.IsChecked==true; Grid.SetColumn(sw,2); row.Children.Add(sw); rows.Children.Add(row); }
    foreach(var a0 in Apps.Where(x=>x.Enabled&&!x.Native&&x.Running).ToList()){ var a=a0;
      var row=new Grid(); row.Margin=new Thickness(6,9,12,9);
      row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
      var grip=Theme.Grip(); row.Children.Add(grip); Ellipse fdot; var ic=Theme.IconDot(Theme.AppIcon(a.IconExe),24,a,out fdot); Grid.SetColumn(ic,1); row.Children.Add(ic);
      var tx=new StackPanel(); tx.Margin=new Thickness(12,0,8,0); tx.VerticalAlignment=VerticalAlignment.Center; tx.Children.Add(Theme.NameLine(a,13.5)); tx.Children.Add(Theme.T(Theme.StatusText(a),11.5,Theme.Sub,FontWeights.Normal));
      Grid.SetColumn(tx,2); row.Children.Add(tx);
      if(a.Native){ var nl=Theme.T("Native",12,Theme.Sub,FontWeights.Normal); nl.ToolTip=a.Hint; Grid.SetColumn(nl,3); row.Children.Add(nl); } else { var sw=Theme.Switch(a.Enabled); sw.Click+=(s,e)=>SetEnabled(a,sw.IsChecked==true); Grid.SetColumn(sw,3); row.Children.Add(sw); }
      var wrap=new Border(); wrap.Child=row; wrap.Background=Brushes.Transparent; wrap.Cursor=System.Windows.Input.Cursors.Hand; wrap.ToolTip="Click to show "+a.Name+"";
      tx.MouseLeftButtonUp+=(s,e)=>{ flyout.Hide(); ShowApp(a); };
      wrap.BorderBrush=Theme.Line; wrap.BorderThickness=new Thickness(0,1,0,0);
      wrap.Tag=a; Theme.Reorderable(rows,wrap,grip,order=>{ ApplyOrder(order); if(settingsWin!=null&&settingsWin.IsVisible) settingsWin.Rebuild(); });
      rows.Children.Add(wrap); }
    if(rows.Children.Count<=1){ var none=Theme.T("No managed apps are running.",13,Theme.Sub,FontWeights.Normal); none.Margin=new Thickness(12); rows.Children.Add(none); }
    st.Children.Add(list);
    var foot=new DockPanel(); foot.Margin=new Thickness(0,8,0,0);
    var exit=Theme.MkBtn(Theme.T("Exit",12.5,Theme.Sub,FontWeights.Normal),"Link"); exit.ToolTip="Exit TrayHelper (hidden windows are restored)"; exit.Click+=(s,e)=>{ flyout.Hide(); ExitHelper(); }; DockPanel.SetDock(exit,Dock.Right); foot.Children.Add(exit);
    var more=Theme.MkBtn(Theme.T("Open settings",12.5,Theme.Accent,FontWeights.Normal),"Link"); more.Click+=(s,e)=>{ flyout.Hide(); ShowSettings(); }; more.HorizontalAlignment=HorizontalAlignment.Left; foot.Children.Add(more);
    if(UpdateTag!=null){ var ul=Theme.MkBtn(Theme.T("Update to v"+UpdateTag,12.5,Theme.Accent,FontWeights.Normal),"Link"); ul.Click+=(s,e)=>{ flyout.Hide(); InstallUpdate(); }; DockPanel.SetDock(ul,Dock.Right); foot.Children.Insert(0,ul); }
    st.Children.Add(foot);
    flyout.Content=outer;
    flyout.Show(); PlaceFlyout(); flyout.Activate();
  }
  void PlaceFlyout(){ if(flyout==null) return; var wa=SystemParameters.WorkArea; flyout.Left=wa.Right-flyout.ActualWidth; flyout.Top=wa.Bottom-flyout.ActualHeight; }
  public void ShowSettings(){
    Theme.Init(); if(flyout!=null) flyout.Hide();
    if(!TestMode){ try{ Catalog.Merge(Apps); }catch{} }
    if(settingsWin==null){ settingsWin=new SettingsWindow(this); Interop(settingsWin); }
    settingsWin.Rebuild(); bool wasVis=settingsWin.IsVisible; if(!wasVis){ settingsWin.PlaceForOpen(); } settingsWin.Show(); if(wasVis&&settingsWin.WindowState==WindowState.Minimized) settingsWin.WindowState=OpenMaximized||WinMax?WindowState.Maximized:WindowState.Normal; settingsWin.Activate();
  }
  Window mini;
  public void ShowAppMenu(AppTarget a){ Theme.Init();
    if(mini!=null){ try{ mini.Close(); }catch{} }
    mini=new Window(); var w=mini; w.WindowStyle=WindowStyle.None; w.AllowsTransparency=true; w.Background=Brushes.Transparent; w.Topmost=true; w.ShowInTaskbar=false; w.ResizeMode=ResizeMode.NoResize; w.SizeToContent=SizeToContent.WidthAndHeight; w.Resources.MergedDictionaries.Add(Theme.Res);
    var outer=new Border(); outer.Margin=new Thickness(10); outer.CornerRadius=new CornerRadius(8); outer.Background=Theme.Bg; outer.BorderBrush=Theme.Line; outer.BorderThickness=new Thickness(1); outer.Effect=new DropShadowEffect{ BlurRadius=14, ShadowDepth=2, Opacity=0.35, Direction=270 };
    var st=new StackPanel(); st.Margin=new Thickness(6); st.MinWidth=210; outer.Child=st;
    var hd=new StackPanel(); hd.Orientation=Orientation.Horizontal; hd.Margin=new Thickness(10,6,10,8); hd.Children.Add(Theme.Img(Theme.AppIcon(a.IconExe),18)); var ht=Theme.T(a.Name,13,Theme.Text,FontWeights.SemiBold); ht.Margin=new Thickness(8,0,0,0); hd.Children.Add(ht); st.Children.Add(hd);
    Action<string,string,Action> item=(g,t,act)=>{ var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; sp.Children.Add(Theme.Glyph(g,14,Theme.Text)); var tt=Theme.T(t,13,Theme.Text,FontWeights.Normal); tt.Margin=new Thickness(10,0,0,0); sp.Children.Add(tt);
      var b=Theme.MkBtn(sp,"Link"); b.HorizontalAlignment=HorizontalAlignment.Stretch; b.HorizontalContentAlignment=HorizontalAlignment.Left; b.Click+=(s,e)=>{ w.Close(); act(); }; st.Children.Add(b); };
    var hw=a.hid.Where(q=>IsWindow(q.H)).Select(q=>q.H).ToList();
    if(hw.Count>1){ foreach(var h0 in hw){ var h=h0; var tl=TitleOf(h); if(tl.Length==0) tl=a.Name; if(tl.Length>48) tl=tl.Substring(0,47)+"…"; item("\uE8A7",tl,()=>ShowOneWin(a,h)); } item("\uE8A7","Restore all",()=>ShowApp(a)); if(a.OpenCount>0) item("\uE738","Hide to tray",()=>Hide_(a)); }
    else item(a.weHid||a.Hidden?"\uE8A7":"\uE738",a.weHid||a.Hidden?"Show":"Hide to tray",()=>{ if(a.weHid||a.Hidden) ShowApp(a); else Hide_(a); });
    item("\uE711","Quit "+a.Name,()=>QuitApp(a));
    if(!a.Temp){ var sep=new Border(); sep.Height=1; sep.Background=Theme.Line; sep.Margin=new Thickness(6,4,6,4); st.Children.Add(sep);
    item("\uE7E8","Disable close-to-tray",()=>{ if(a.weHid||a.Hidden) ShowApp(a); SetEnabled(a,false); }); }
    item("\uE713","TrayHelper settings",()=>ShowSettings());
    w.Content=outer; w.Deactivated+=(s,e)=>{ try{ w.Close(); }catch{} };
    var cur=System.Windows.Forms.Cursor.Position; w.Show();
    var src=PresentationSource.FromVisual(w); double sx=1,sy=1; if(src!=null&&src.CompositionTarget!=null){ sx=src.CompositionTarget.TransformToDevice.M11; sy=src.CompositionTarget.TransformToDevice.M22; }
    var wa=SystemParameters.WorkArea; double x=cur.X/sx-w.ActualWidth+10, y=cur.Y/sy-w.ActualHeight+10;
    w.Left=Math.Max(wa.Left,Math.Min(x,wa.Right-w.ActualWidth)); w.Top=Math.Max(wa.Top,Math.Min(y,wa.Bottom-w.ActualHeight)); w.Activate(); }
  void CloseWpf(){ if(flyout!=null){ flyout.Close(); flyout=null; } if(settingsWin!=null){ settingsWin.ReallyClose(); settingsWin=null; } }
  const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
  public static bool StartWithWindows{
    get{ try{ using(var k=Registry.CurrentUser.OpenSubKey(RunKey)) return k!=null&&k.GetValue("TrayHelper")!=null; }catch{ return false; } }
    set{ if(TestMode) return; try{ using(var k=Registry.CurrentUser.CreateSubKey(RunKey)){ if(value) k.SetValue("TrayHelper","\""+System.Windows.Forms.Application.ExecutablePath+"\""+(StartDelay>0?" --delay "+StartDelay:"")); else k.DeleteValue("TrayHelper",false); } Log("startWithWindows="+value); }catch(Exception ex){ Log("run key "+ex.Message); } } }
}

sealed class SettingsWindow:Window{
  readonly TrayHelper th; bool reallyClose; ContentControl page; RadioButton navApps, navGeneral, navAbout;
  readonly Dictionary<AppTarget,TextBlock> statusLabels=new Dictionary<AppTarget,TextBlock>(); readonly Dictionary<AppTarget,Ellipse> dots=new Dictionary<AppTarget,Ellipse>(); DispatcherTimer timer;
  public SettingsWindow(TrayHelper th){ this.th=th; Title="TrayHelper"; Width=TrayHelper.WinW>=620?TrayHelper.WinW:900; Height=TrayHelper.WinH>=400?TrayHelper.WinH:600; MinWidth=620; MinHeight=400; WindowStartupLocation=WindowStartupLocation.CenterScreen;

    Background=Theme.Bg; FontFamily=Theme.Font; Resources.MergedDictionaries.Add(Theme.Res); Icon=Theme.Logo;
    SourceInitialized+=(s,e)=>Theme.DarkTitle(this);
    var root=new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(220)}); root.ColumnDefinitions.Add(new ColumnDefinition());
    var side=new StackPanel(); side.Margin=new Thickness(14,16,8,16);
    var brand=new StackPanel(); brand.Orientation=Orientation.Horizontal; brand.Margin=new Thickness(8,0,0,18); brand.Children.Add(Theme.Img(Theme.Logo,28)); var bt=Theme.T("TrayHelper",16,Theme.Text,FontWeights.SemiBold); bt.Margin=new Thickness(10,0,0,0); brand.Children.Add(bt); side.Children.Add(brand);
    navApps=Nav("\uE71D","Apps"); navGeneral=Nav("\uE713","Settings"); navAbout=Nav("\uE946","About");
    side.Children.Add(navApps); side.Children.Add(navGeneral); side.Children.Add(navAbout);
    root.Children.Add(side);
    var sv=new ScrollViewer(); sv.VerticalScrollBarVisibility=ScrollBarVisibility.Auto; page=new ContentControl(); page.Margin=new Thickness(20,18,28,24); sv.Content=page; Grid.SetColumn(sv,1); root.Children.Add(sv);
    Content=root; navApps.IsChecked=true;
    timer=new DispatcherTimer(); timer.Interval=TimeSpan.FromSeconds(1); timer.Tick+=(s,e)=>{ foreach(var kv in statusLabels) kv.Value.Text=Theme.StatusText(kv.Key); foreach(var kv in dots) Theme.SetDot(kv.Value,kv.Key); }; timer.Start();
  }
  RadioButton Nav(string glyph,string text){ var r=new RadioButton(); r.Style=(Style)Theme.Res["Nav"]; r.GroupName="nav";
    var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; sp.Children.Add(Theme.Glyph(glyph,16,Theme.Text)); var t=Theme.T(text,14,Theme.Text,FontWeights.Normal); t.Margin=new Thickness(12,0,0,0); sp.Children.Add(t); r.Content=sp;
    r.Checked+=(s,e)=>Rebuild(); return r; }
  public void Rebuild(){ statusLabels.Clear(); dots.Clear(); if(navGeneral.IsChecked==true) page.Content=GeneralPage(); else if(navAbout.IsChecked==true) page.Content=AboutPage(); else page.Content=AppsPage(); }
  static Border Card(UIElement child){ var b=new Border(); b.CornerRadius=new CornerRadius(6); b.Background=Theme.Card; b.BorderBrush=Theme.Line; b.BorderThickness=new Thickness(1); b.Padding=new Thickness(16,12,16,12); b.Margin=new Thickness(0,0,0,4); b.Child=child; return b; }
  static StackPanel Header(string title,string sub){ var s=new StackPanel(); s.Margin=new Thickness(0,0,0,16); s.Children.Add(Theme.T(title,26,Theme.Text,FontWeights.SemiBold)); if(sub==null) return s; var d=Theme.T(sub,13,Theme.Sub,FontWeights.Normal); d.TextWrapping=TextWrapping.Wrap; d.Margin=new Thickness(0,4,0,0); s.Children.Add(d); return s; }
  bool scanning;
  UIElement AppsPage(){
    var st=new StackPanel(); st.Children.Add(Header("Apps",null));
    var bar=new StackPanel(); bar.Orientation=Orientation.Horizontal; bar.Margin=new Thickness(0,0,0,12);
    var add=Theme.MkBtn("Add app…","Accent"); add.Click+=(s,e)=>AddApp(); bar.Children.Add(add);
    var spin=Theme.Glyph("\uE72C",14,Theme.Text); var rt=new RotateTransform(); spin.RenderTransform=rt; spin.RenderTransformOrigin=new Point(0.5,0.5);
    var scanLbl=Theme.T(scanning?"Scanning…":"Rescan",13,Theme.Text,FontWeights.Normal); scanLbl.Margin=new Thickness(8,0,0,0);
    var scp=new StackPanel(); scp.Orientation=Orientation.Horizontal; scp.Children.Add(spin); scp.Children.Add(scanLbl);
    var scan=Theme.MkBtn(scp,"Btn"); scan.Margin=new Thickness(8,0,0,0); bar.Children.Add(scan);
    scan.Click+=(s,e)=>{ if(scanning) return; scanning=true; scanLbl.Text="Scanning…"; scan.IsEnabled=false;
      var anim=new System.Windows.Media.Animation.DoubleAnimation(0,360,new Duration(TimeSpan.FromSeconds(0.9))); anim.RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever; rt.BeginAnimation(RotateTransform.AngleProperty,anim);
      var started=DateTime.Now;
      System.Threading.ThreadPool.QueueUserWorkItem(_=>{ List<Catalog.Found> res=null; try{ res=Catalog.DetectAll(); }catch{}
        var wait=700-(int)(DateTime.Now-started).TotalMilliseconds; if(wait>0) System.Threading.Thread.Sleep(wait);
        Dispatcher.BeginInvoke(new Action(()=>{ try{ if(res!=null){ Catalog.Apply(TrayHelper.Apps,res); TrayHelper.SaveAll(); } }catch{} scanning=false; Rebuild(); })); }); };
    var addAll=Theme.MkBtn("Add all open windows…","Btn"); addAll.Margin=new Thickness(8,0,0,0); addAll.Click+=(s,e)=>{ var w=new AddAllWindow(); w.Owner=this; if(w.ShowDialog()==true){ foreach(var a in w.Chosen) if(!TrayHelper.Apps.Any(x=>string.Equals(x.ProcName,a.ProcName,StringComparison.OrdinalIgnoreCase))) th.AddApp(a); Rebuild(); } }; bar.Children.Add(addAll);
    st.Children.Add(bar);
    { var g=new Grid(); foreach(var w in new[]{GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto}) g.ColumnDefinitions.Add(new ColumnDefinition{Width=w});
      g.Children.Add(Theme.Img(Theme.Logo,32)); var tx=new StackPanel(); tx.Margin=new Thickness(14,0,12,0); tx.VerticalAlignment=VerticalAlignment.Center; tx.Children.Add(Theme.T("TrayHelper",14,Theme.Text,FontWeights.Normal)); tx.Children.Add(Theme.T("This window always closes to the tray",12,Theme.Sub,FontWeights.Normal)); Grid.SetColumn(tx,1); g.Children.Add(tx);
      var sw=Labeled("Start with Windows",TrayHelper.StartWithWindows,v=>TrayHelper.StartWithWindows=v); Grid.SetColumn(sw,2); g.Children.Add(sw); st.Children.Add(Card(g)); }
    var all=TrayHelper.Apps.Where(x=>x.Detected||!x.Preset).ToList();
    var managed=all.Where(x=>!x.Native).ToList(); var native=all.Where(x=>x.Native).ToList();
    var mpanel=new StackPanel(); st.Children.Add(mpanel); HashSet<string> owners; try{ owners=Scanner.TrayOwners(); }catch{ owners=new HashSet<string>(); }
    foreach(var a0 in managed){ var a=a0;
      var outerG=new Grid(); outerG.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); outerG.ColumnDefinitions.Add(new ColumnDefinition());
      var gripA=Theme.Grip(); outerG.Children.Add(gripA);
      var g=new Grid(); Grid.SetColumn(g,1); outerG.Children.Add(g); foreach(var w in new[]{GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto,GridLength.Auto,GridLength.Auto}) g.ColumnDefinitions.Add(new ColumnDefinition{Width=w});
      Ellipse sdot; g.Children.Add(Theme.IconDot(Theme.AppIcon(a.IconExe),32,a,out sdot)); dots[a]=sdot;
      var tx=new StackPanel(); tx.Margin=new Thickness(14,0,12,0); tx.VerticalAlignment=VerticalAlignment.Center;
      tx.Children.Add(Theme.NameLine(a,14)); var stt=Theme.T(Theme.StatusText(a),12,Theme.Sub,FontWeights.Normal); statusLabels[a]=stt; tx.Children.Add(stt);
      Grid.SetColumn(tx,1); g.Children.Add(tx);
      int offer=TrayHelper.NativeOffer(a,owners);
      if(offer>0){ var nb=new StackPanel(); nb.Orientation=Orientation.Horizontal; nb.Margin=new Thickness(-8,2,0,0);
        if(offer==2){ var bdg=Theme.Badge("May have its own tray option"); bdg.Margin=new Thickness(8,0,2,0); nb.Children.Add(bdg); }
        var use=Theme.MkBtn(Theme.T("Use built-in",11.5,offer==2?Theme.Accent:Theme.Sub,FontWeights.Normal),"Link"); use.ToolTip="Turn off TrayHelper for "+a.Name+" and open its settings"; use.Click+=(s,e)=>{ th.UseNative(a); Rebuild(); }; nb.Children.Add(use);
        if(offer==2){ var dis=Theme.MkBtn(Theme.Glyph("\uE711",10,Theme.Sub),"Link"); dis.ToolTip="Hide this hint"; dis.Click+=(s,e)=>{ th.DismissNative(a); Rebuild(); }; nb.Children.Add(dis); }
        tx.Children.Add(nb); }
      var show=Theme.MkBtn("Show","Btn"); show.Click+=(s,e)=>th.Show_(a); show.Margin=new Thickness(0,0,16,0); Grid.SetColumn(show,2); g.Children.Add(show);

      var ct=Labeled("Close to tray",a.Enabled,v=>th.SetEnabled(a,v)); Grid.SetColumn(ct,4); g.Children.Add(ct);
      var card=Card(outerG); card.Tag=a; Theme.Reorderable(mpanel,card,gripA,order=>th.ApplyOrder(order));
      if(!a.Preset){ var cm=new ContextMenu(); var mi=new MenuItem(); mi.Header="Remove "+a.Name; mi.Click+=(s,e)=>{ if(MessageBox.Show(this,"Remove "+a.Name+" from TrayHelper?","TrayHelper",MessageBoxButton.YesNo)==MessageBoxResult.Yes){ th.RemoveApp(a); Rebuild(); } }; cm.Items.Add(mi); card.ContextMenu=cm;  }
      mpanel.Children.Add(card); }
    if(native.Count>0){ var nh=Theme.T("Natively supported",15,Theme.Text,FontWeights.SemiBold); nh.Margin=new Thickness(2,18,0,2); st.Children.Add(nh);
      var npanel=new StackPanel(); st.Children.Add(npanel);
      foreach(var a0 in native){ var a=a0;
        var g=new Grid(); foreach(var w in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto}) g.ColumnDefinitions.Add(new ColumnDefinition{Width=w});
        var gripN=Theme.Grip(); g.Children.Add(gripN); var nic=Theme.Img(Theme.AppIcon(a.IconExe),32); Grid.SetColumn(nic,1); g.Children.Add(nic);
        var tx=new StackPanel(); tx.Margin=new Thickness(14,0,12,0); tx.VerticalAlignment=VerticalAlignment.Center; tx.Children.Add(Theme.NameLine(a,14));
        var hint=Theme.T(a.Hint??(a.UserNative?"Uses its own close-to-tray":"Option is in app settings"),12,Theme.Sub,FontWeights.Normal); hint.TextWrapping=TextWrapping.Wrap; tx.Children.Add(hint); Grid.SetColumn(tx,2); g.Children.Add(tx);
        var obp=new StackPanel(); obp.Orientation=Orientation.Horizontal; obp.VerticalAlignment=VerticalAlignment.Center;
        if(a.UserNative){ var back=Theme.MkBtn(Theme.T("Use TrayHelper",12.5,Theme.Accent,FontWeights.Normal),"Link"); back.Margin=new Thickness(0,0,8,0); back.Click+=(s,e)=>{ th.UseOurs(a); Rebuild(); }; obp.Children.Add(back); }
        var ob=Theme.MkBtn("Open settings","Btn"); ob.Click+=(s,e)=>th.OpenAppSettings(a); obp.Children.Add(ob); Grid.SetColumn(obp,3); g.Children.Add(obp);
        var nc=Card(g); nc.Tag=a; Theme.Reorderable(npanel,nc,gripN,order=>th.ApplyOrder(order)); npanel.Children.Add(nc); } }
    return st; }
  static StackPanel Labeled(string label,bool on,Action<bool> set){ var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; sp.VerticalAlignment=VerticalAlignment.Center;
    var t=Theme.T(label,12.5,Theme.Sub,FontWeights.Normal); t.Margin=new Thickness(0,0,10,0); sp.Children.Add(t); var sw=Theme.Switch(on); sw.Click+=(s,e)=>set(sw.IsChecked==true); sp.Children.Add(sw); return sp; }
  UIElement GeneralPage(){ var st=new StackPanel(); st.Children.Add(Header("Settings",null));
    st.Children.Add(Row("Start with Windows",null,Labeled("",TrayHelper.StartWithWindows,v=>TrayHelper.StartWithWindows=v)));
    st.Children.Add(Row("Notifications",null,Labeled("",TrayHelper.NotifyFirstHide,v=>{ TrayHelper.NotifyFirstHide=v; TrayHelper.SaveAll(); })));
    st.Children.Add(Row("Show all hidden apps",null,Theme.HotkeyBox(TrayHelper.HkRestoreAll,v=>{ TrayHelper.HkRestoreAll=v; TrayHelper.SaveAll(); th.ApplyHotkeys(); bool ok; return !th.HotkeyOk.TryGetValue(1,out ok)||ok; },r=>th.SuspendHotkeys(r))));
    st.Children.Add(Row("Send window to tray",null,Theme.HotkeyBox(TrayHelper.HkSendToTray,v=>{ TrayHelper.HkSendToTray=v; TrayHelper.SaveAll(); th.ApplyHotkeys(); bool ok; return !th.HotkeyOk.TryGetValue(2,out ok)||ok; },r=>th.SuspendHotkeys(r))));
    var io=new StackPanel(); io.Orientation=Orientation.Horizontal;
    var ex=Theme.MkBtn("Export…","Btn"); ex.Click+=(s,e)=>{ var d=new SaveFileDialog(); d.FileName="tray-helper.ini"; d.Filter="TrayHelper config|*.ini"; if(d.ShowDialog(this)==true){ try{ TrayHelper.SaveAll(); File.Copy(TrayHelper.IniPath,d.FileName,true); }catch(Exception er){ MessageBox.Show(this,er.Message,"TrayHelper"); } } };
    var im=Theme.MkBtn("Import…","Btn"); im.Margin=new Thickness(8,0,0,0); im.Click+=(s,e)=>{ var d=new OpenFileDialog(); d.Filter="TrayHelper config|*.ini"; if(d.ShowDialog(this)==true){ try{ File.Copy(d.FileName,TrayHelper.IniPath,true); th.MergeReload(File.ReadAllText(TrayHelper.IniPath)); Rebuild(); }catch(Exception er){ MessageBox.Show(this,er.Message,"TrayHelper"); } } };
    io.Children.Add(ex); io.Children.Add(im);
    st.Children.Add(Row("Export / import",null,io));
    var lf=Theme.MkBtn("Open folder","Btn"); lf.Click+=(s,e)=>{ try{ Process.Start("explorer.exe","\""+TrayHelper.Dir+"\""); }catch{} };
    st.Children.Add(Row("Config folder",null,lf));
    st.Children.Add(Row("Updates",null,Segmented(new[]{"Auto","Notify","Off"},TrayHelper.UpdateMode,v=>{ TrayHelper.UpdateMode=v.ToLowerInvariant(); TrayHelper.SaveAll(); })));
    st.Children.Add(UpdateRow());
    var rs=Theme.MkBtn("Reset…","Btn"); rs.Click+=(s,e)=>{ if(MessageBox.Show(this,"Reset TrayHelper's app list and settings to defaults?","TrayHelper",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return; try{ File.Copy(TrayHelper.IniPath,TrayHelper.IniPath+".bak",true); File.Delete(TrayHelper.IniPath); th.ReloadConfig(); Rebuild(); }catch(Exception er){ MessageBox.Show(this,er.Message,"TrayHelper"); } };
    st.Children.Add(Row("Reset to defaults",null,rs));
    var ah=Theme.T("Advanced",15,Theme.Text,FontWeights.SemiBold); ah.Margin=new Thickness(2,18,0,6); st.Children.Add(ah);
    st.Children.Add(Row("Open this window maximized","Off: restore last size and position",Labeled("",TrayHelper.OpenMaximized,v=>{ TrayHelper.OpenMaximized=v; TrayHelper.SaveAll(); })));
    st.Children.Add(Row("Hide Store apps on a hidden desktop",null,Labeled("",TrayHelper.UwpVd,v=>{ TrayHelper.UwpVd=v; TrayHelper.SaveAll(); })));
    return st; }
  // Segmented buttons instead of a dropdown.
  static UIElement Segmented(string[] opts,string cur,Action<string> set){ var bd=new Border(); bd.CornerRadius=new CornerRadius(4); bd.BorderBrush=Theme.Line; bd.BorderThickness=new Thickness(1); var sp=new StackPanel(); sp.Orientation=Orientation.Horizontal; bd.Child=sp; var btns=new List<Border>();
    Action<string> paint=v=>{ foreach(var b in btns){ bool on=string.Equals((string)b.Tag,v,StringComparison.OrdinalIgnoreCase); b.Background=on?Theme.Accent:Brushes.Transparent; ((TextBlock)b.Child).Foreground=on?(Theme.Dark?Brushes.Black:Brushes.White):Theme.Text; } };
    foreach(var o in opts){ var b=new Border(); b.Tag=o; b.Padding=new Thickness(12,4,12,4); b.Cursor=System.Windows.Input.Cursors.Hand; b.CornerRadius=new CornerRadius(3); b.Child=Theme.T(o,12.5,Theme.Text,FontWeights.Normal); var oo=o; b.MouseLeftButtonUp+=(s,e)=>{ set(oo); paint(oo); }; btns.Add(b); sp.Children.Add(b); }
    paint(cur); return bd; }
  Border UpdateRow(){ var sub=TrayHelper.UpdateStatus!=null?"v"+TrayHelper.Version+" · "+TrayHelper.UpdateStatus:"v"+TrayHelper.Version; var right=new StackPanel(); right.Orientation=Orientation.Horizontal;
    if(TrayHelper.UpdateTag!=null){ var ins=Theme.MkBtn("Install v"+TrayHelper.UpdateTag,"Accent"); ins.Margin=new Thickness(0,0,8,0); ins.Click+=(s,e)=>{ th.InstallUpdate(); ins.IsEnabled=false; }; right.Children.Add(ins); }
    var up=Theme.MkBtn("Check now","Btn"); right.Children.Add(up); var row=Row("Check for updates",sub,right); var lbl=((StackPanel)((Grid)row.Child).Children[0]).Children[1] as TextBlock;
    up.Click+=(s,e)=>{ up.IsEnabled=false; if(lbl!=null) lbl.Text="v"+TrayHelper.Version+" · Checking…"; System.Threading.ThreadPool.QueueUserWorkItem(_=>{ TrayHelper.CheckUpdate(true); Dispatcher.BeginInvoke(new Action(()=>{ if(navGeneral.IsChecked==true) Rebuild(); })); }); };
    return row; }
  static Border Row(string title,string sub,UIElement right){ var g=new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
    var tx=new StackPanel(); tx.Children.Add(Theme.T(title,14,Theme.Text,FontWeights.Normal)); if(sub!=null){ var s=Theme.T(sub,12,Theme.Sub,FontWeights.Normal); tx.Children.Add(s); } tx.VerticalAlignment=VerticalAlignment.Center; g.Children.Add(tx); Grid.SetColumn(right,1); g.Children.Add(right); return Card(g); }
  UIElement AboutPage(){ var st=new StackPanel();
    var h=new StackPanel(); h.Orientation=Orientation.Horizontal; h.Margin=new Thickness(0,0,0,16); h.Children.Add(Theme.Img(Theme.Logo,64));
    var ht=new StackPanel(); ht.Margin=new Thickness(16,0,0,0); ht.VerticalAlignment=VerticalAlignment.Center; ht.Children.Add(Theme.T("TrayHelper",26,Theme.Text,FontWeights.SemiBold)); ht.Children.Add(Theme.T("Close the window, keep the app.  ·  v"+TrayHelper.Version+"  ·  MIT, unofficial",13,Theme.Sub,FontWeights.Normal)); h.Children.Add(ht); st.Children.Add(h);
    var rm=new StackPanel(); string cl=null; try{ using(var rs=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CHANGELOG.md")) if(rs!=null) using(var rd=new StreamReader(rs)) cl=rd.ReadToEnd(); }catch{}
    if(cl==null) cl="# Changelog\n\n## 0.5.0";
    foreach(var raw in cl.Replace("\r","").Split('\n')){ var line=raw.TrimEnd(); if(line.Length==0) continue; TextBlock t;
      if(line.StartsWith("# ")) t=Theme.T(line.Substring(2),15,Theme.Text,FontWeights.SemiBold); else if(line.StartsWith("## ")){ t=Theme.T(line.Substring(3),13.5,Theme.Text,FontWeights.SemiBold); t.Margin=new Thickness(0,12,0,0); }
      else{ t=Theme.T("•  "+line.TrimStart('-',' '),12.5,Theme.Sub,FontWeights.Normal); t.Margin=new Thickness(0,4,0,0); } t.TextWrapping=TextWrapping.Wrap; rm.Children.Add(t); }
    st.Children.Add(Card(rm));
    var gif=new[]{Path.Combine(TrayHelper.Dir,"demo.gif"),Path.Combine(TrayHelper.Dir,"assets","demo.gif"),Path.Combine(TrayHelper.Dir,"..","assets","demo.gif")}.FirstOrDefault(File.Exists);
    var demo=new StackPanel(); demo.Children.Add(Theme.T("Demo",15,Theme.Text,FontWeights.SemiBold));
    if(gif!=null){ try{ var pb=new System.Windows.Forms.PictureBox(); pb.SizeMode=System.Windows.Forms.PictureBoxSizeMode.Zoom; pb.Image=System.Drawing.Image.FromFile(gif); var host=new System.Windows.Forms.Integration.WindowsFormsHost(); host.Child=pb; host.Height=280; host.Margin=new Thickness(0,8,0,0); demo.Children.Add(host); }catch{ gif=null; } }
    if(gif==null){ var ph=new Border(); ph.Height=160; ph.Margin=new Thickness(0,8,0,0); ph.CornerRadius=new CornerRadius(6); ph.BorderBrush=Theme.Line; ph.BorderThickness=new Thickness(1); var pt=Theme.T("Demo GIF coming soon (assets\\demo.gif)",13,Theme.Sub,FontWeights.Normal); pt.HorizontalAlignment=HorizontalAlignment.Center; ph.Child=pt; demo.Children.Add(ph); }
    var dc=Card(demo); dc.Margin=new Thickness(0,8,0,0); st.Children.Add(dc);
    return st; }
  void AddApp(){ var p=new PickerWindow(); p.Owner=this; if(p.ShowDialog()==true&&p.Result!=null){
      if(TrayHelper.Apps.Any(x=>string.Equals(x.Name,p.Result.Name,StringComparison.OrdinalIgnoreCase))){ MessageBox.Show(this,p.Result.Name+" is already managed.","TrayHelper"); return; }
      th.AddApp(p.Result); Rebuild(); } }
  // Remember RestoreBounds (never the maximized Left/Top) plus state.
  void SaveBounds(){ try{ if(!IsLoaded) return; var r=WindowState==WindowState.Normal?new Rect(Left,Top,ActualWidth,ActualHeight):RestoreBounds; if(r.IsEmpty||r.Width<100) return;
      TrayHelper.WinL=r.Left; TrayHelper.WinT=r.Top; TrayHelper.WinW=r.Width; TrayHelper.WinH=r.Height; TrayHelper.WinMax=WindowState==WindowState.Maximized; TrayHelper.SaveAll(); }catch{} }
  public void PlaceForOpen(){ WindowState=WindowState.Normal; double w=TrayHelper.WinW>=MinWidth?TrayHelper.WinW:900, h=TrayHelper.WinH>=MinHeight?TrayHelper.WinH:600, l=TrayHelper.WinL, t=TrayHelper.WinT;
    var vs=new Rect(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenWidth,SystemParameters.VirtualScreenHeight); bool onScreen=false;
    if(!double.IsNaN(l)&&!double.IsNaN(t)){ var src=PresentationSource.FromVisual(this); double sx=1,sy=1; if(src!=null&&src.CompositionTarget!=null){ sx=src.CompositionTarget.TransformToDevice.M11; sy=src.CompositionTarget.TransformToDevice.M22; }
      var cap=new System.Drawing.Rectangle((int)((l+40)*sx),(int)((t+8)*sy),(int)(Math.Max(100,w-80)*sx),(int)(32*sy)); foreach(var sc in System.Windows.Forms.Screen.AllScreens) if(sc.WorkingArea.IntersectsWith(cap)){ onScreen=true; break; } }
    var wa=SystemParameters.WorkArea; if(!onScreen){ w=Math.Min(w,wa.Width); h=Math.Min(h,wa.Height); l=wa.Left+(wa.Width-w)/2; t=wa.Top+(wa.Height-h)/2; }
    WindowStartupLocation=WindowStartupLocation.Manual; Left=l; Top=t; Width=w; Height=h;
    if(TrayHelper.OpenMaximized||TrayHelper.WinMax) WindowState=WindowState.Maximized; }
  public void ReallyClose(){ reallyClose=true; timer.Stop(); Close(); }
  protected override void OnClosing(System.ComponentModel.CancelEventArgs e){ SaveBounds(); if(!reallyClose){ e.Cancel=true; Hide(); return; } base.OnClosing(e); }
}

sealed class PickerWindow:Window{
  public AppTarget Result; readonly ListBox runList;
  sealed class Cand{ public string Title, Proc, Cls, Exe, Launch, Name; }
  public PickerWindow(){ Title="Add app"; Width=560; Height=520; WindowStartupLocation=WindowStartupLocation.CenterOwner; Background=Theme.Bg; FontFamily=Theme.Font; Resources.MergedDictionaries.Add(Theme.Res); ShowInTaskbar=false;
    SourceInitialized+=(s,e)=>Theme.DarkTitle(this);
    var dp=new DockPanel(); dp.Margin=new Thickness(20);
    var hd=Theme.T("Add an app",20,Theme.Text,FontWeights.SemiBold); hd.Margin=new Thickness(0,0,0,12); DockPanel.SetDock(hd,Dock.Top); dp.Children.Add(hd);
    var btns=new DockPanel(); btns.Margin=new Thickness(0,12,0,0);
    var browse=Theme.MkBtn("Browse .exe…","Btn"); browse.Click+=(s,e)=>Browse(); DockPanel.SetDock(browse,Dock.Left); btns.Children.Add(browse);
    var right=new StackPanel(); right.Orientation=Orientation.Horizontal; right.HorizontalAlignment=HorizontalAlignment.Right;
    var ok=Theme.MkBtn("Add","Accent"); ok.Click+=(s,e)=>Pick(); var cancel=Theme.MkBtn("Cancel","Btn"); cancel.Margin=new Thickness(8,0,0,0); cancel.Click+=(s,e)=>{ DialogResult=false; };
    right.Children.Add(ok); right.Children.Add(cancel); btns.Children.Add(right); DockPanel.SetDock(btns,Dock.Bottom); dp.Children.Add(btns);
    runList=new ListBox(); runList.Background=Theme.Card; runList.BorderBrush=Theme.Line; runList.BorderThickness=new Thickness(1); runList.Foreground=Theme.Text; runList.MouseDoubleClick+=(s,e)=>Pick();
    dp.Children.Add(runList); Content=dp; FillRunning(); }
  void FillRunning(){ var managed=new HashSet<string>(TrayHelper.Apps.Where(a=>a.ProcName!=null).Select(a=>a.ProcName),StringComparer.OrdinalIgnoreCase); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach(var w in Scanner.AltTabWindows()){ bool uwp=w.Proc.Equals("ApplicationFrameHost",StringComparison.OrdinalIgnoreCase); if(!uwp&&(managed.Contains(w.Proc)||!seen.Add(w.Proc))) continue;
      var cd=new Cand{ Title=w.Title, Proc=w.Proc, Cls=w.Cls, Exe=w.Exe, Launch=w.Aumid!=null?@"shell:AppsFolder\"+w.Aumid:w.Exe };
      try{ if(w.Exe!=null){ var vi=FileVersionInfo.GetVersionInfo(w.Exe); cd.Name=!string.IsNullOrEmpty(vi.FileDescription)?vi.FileDescription:vi.ProductName; } }catch{} if(uwp||string.IsNullOrEmpty(cd.Name)) cd.Name=w.Title;
      var row=new StackPanel(); row.Orientation=Orientation.Horizontal; row.Margin=new Thickness(4,5,4,5); row.Children.Add(Theme.Img(Theme.AppIcon(cd.Exe),24));
      var tx=new StackPanel(); tx.Margin=new Thickness(10,0,0,0); tx.Children.Add(Theme.T(cd.Name,13.5,Theme.Text,FontWeights.Normal)); var tt=Theme.T(w.Title,11.5,Theme.Sub,FontWeights.Normal); tt.TextTrimming=TextTrimming.CharacterEllipsis; tt.MaxWidth=420; tx.Children.Add(tt); row.Children.Add(tx);
      var it=new ListBoxItem(); it.Content=row; it.Tag=cd; runList.Items.Add(it); } }
  static Cand FromExe(string exe){ var cd=new Cand(); cd.Exe=exe; cd.Launch=exe; cd.Proc=Path.GetFileNameWithoutExtension(exe); cd.Cls=""; cd.Title="";
    try{ var vi=FileVersionInfo.GetVersionInfo(exe); cd.Name=!string.IsNullOrEmpty(vi.FileDescription)?vi.FileDescription:vi.ProductName; }catch{} if(string.IsNullOrEmpty(cd.Name)) cd.Name=cd.Proc; return cd; }
  void Browse(){ var d=new OpenFileDialog(); d.Filter="Applications (*.exe)|*.exe"; d.Title="Choose an app"; if(d.ShowDialog(this)!=true) return; Done(FromExe(d.FileName)); }
  void Pick(){ var it=runList.SelectedItem as ListBoxItem; if(it==null) return; Done((Cand)it.Tag); }
  void Done(Cand cd){ var a=new AppTarget(); a.WinTitle=string.Equals(cd.Proc,"ApplicationFrameHost",StringComparison.OrdinalIgnoreCase)?cd.Title:""; a.Name=cd.Name??cd.Proc; a.ProcName=cd.Proc; a.WinClass=cd.Cls??""; a.ExePath=cd.Launch; a.IconExe=cd.Exe; a.Enabled=true; a.Preset=false; a.Detected=true;
    Result=a; DialogResult=true; }
}

sealed class AddAllWindow:Window{
  delegate bool EnumProc(IntPtr h,IntPtr l);
  [DllImport("user32")] static extern bool EnumWindows(EnumProc p,IntPtr l);
  [DllImport("user32")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32")] static extern IntPtr GetWindow(IntPtr h,uint c);
  [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder s,int n);
  [DllImport("dwmapi")] static extern int DwmGetWindowAttribute(IntPtr h,int a,out int v,int s);
  [DllImport("kernel32")] static extern IntPtr OpenProcess(uint a,bool i,uint pid);
  [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr h,int f,StringBuilder s,ref int n);
  [DllImport("kernel32",CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr h,ref int n,StringBuilder s);
  [DllImport("advapi32")] static extern bool OpenProcessToken(IntPtr p,uint a,out IntPtr t);
  [DllImport("advapi32")] static extern bool GetTokenInformation(IntPtr t,int c,out int v,int l,out int r);
  public List<AppTarget> Chosen=new List<AppTarget>(); readonly StackPanel list; readonly TextBox excl; readonly List<Tuple<CheckBox,AppTarget>> rows=new List<Tuple<CheckBox,AppTarget>>();
  static readonly Dictionary<string,string> Known=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"WINWORD","Word"},{"EXCEL","Excel"},{"POWERPNT","PowerPoint"},{"OUTLOOK","Outlook (classic)"},{"SystemSettings","Windows Settings"},{"Taskmgr","Task Manager"}};
  static bool Elevated(uint pid,out bool unknown){ unknown=false; var ph=OpenProcess(0x1000,false,pid); if(ph==IntPtr.Zero){ unknown=true; return false; }
    try{ IntPtr t; if(!OpenProcessToken(ph,0x0008,out t)){ return true; } try{ int v,r; if(GetTokenInformation(t,20,out v,4,out r)) return v!=0; }finally{ CloseHandle(t); } }finally{ CloseHandle(ph); } return false; }
  static bool SelfElevated(){ bool u; return Elevated((uint)Process.GetCurrentProcess().Id,out u); }
  public AddAllWindow(){ Title="Add all open windows"; Width=620; Height=600; WindowStartupLocation=WindowStartupLocation.CenterOwner; Background=Theme.Bg; FontFamily=Theme.Font; Resources.MergedDictionaries.Add(Theme.Res); ShowInTaskbar=false;
    SourceInitialized+=(s,e)=>Theme.DarkTitle(this);
    var dp=new DockPanel(); dp.Margin=new Thickness(20);
    var hd=new StackPanel(); hd.Children.Add(Theme.T("Add all open windows",20,Theme.Text,FontWeights.SemiBold)); var sb=Theme.T("Open app windows that TrayHelper doesn't manage yet, one per app. Uncheck anything you don't want.",12.5,Theme.Sub,FontWeights.Normal); sb.TextWrapping=TextWrapping.Wrap; hd.Children.Add(sb);
    var exl=Theme.T("Excluded processes (comma-separated):",12,Theme.Sub,FontWeights.Normal); exl.Margin=new Thickness(0,10,0,4); hd.Children.Add(exl);
    excl=new TextBox(); excl.Text=string.Join(", ",TrayHelper.Exclude); excl.Background=Theme.Card; excl.Foreground=Theme.Text; excl.BorderBrush=Theme.Line; excl.Padding=new Thickness(6,4,6,4); hd.Children.Add(excl);
    var ref_=Theme.MkBtn("Apply exclusions","Btn"); ref_.HorizontalAlignment=HorizontalAlignment.Left; ref_.Margin=new Thickness(0,6,0,10); ref_.Click+=(s,e)=>{ SaveExcl(); Fill(); }; hd.Children.Add(ref_);
    DockPanel.SetDock(hd,Dock.Top); dp.Children.Add(hd);
    var btns=new StackPanel(); btns.Orientation=Orientation.Horizontal; btns.HorizontalAlignment=HorizontalAlignment.Right; btns.Margin=new Thickness(0,12,0,0);
    var ok=Theme.MkBtn("Add selected","Accent"); ok.Click+=(s,e)=>{ SaveExcl(); foreach(var r in rows) if(r.Item1.IsChecked==true) Chosen.Add(r.Item2); DialogResult=true; };
    var cancel=Theme.MkBtn("Cancel","Btn"); cancel.Margin=new Thickness(8,0,0,0); cancel.Click+=(s,e)=>DialogResult=false; btns.Children.Add(ok); btns.Children.Add(cancel); DockPanel.SetDock(btns,Dock.Bottom); dp.Children.Add(btns);
    var sv=new ScrollViewer(); sv.VerticalScrollBarVisibility=ScrollBarVisibility.Auto; var bd=new Border(); bd.Background=Theme.Card; bd.BorderBrush=Theme.Line; bd.BorderThickness=new Thickness(1); bd.CornerRadius=new CornerRadius(6); list=new StackPanel(); list.Margin=new Thickness(8); sv.Content=list; bd.Child=sv; dp.Children.Add(bd);
    Content=dp; Fill(); }
  void SaveExcl(){ TrayHelper.Exclude=excl.Text.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList(); TrayHelper.SaveAll(); }
  void Fill(){ list.Children.Clear(); rows.Clear(); var exl=new List<string>();
    foreach(var cd in Scanner.Scan(exl)){ var a=cd.App;
      var cb=new CheckBox(); cb.IsChecked=cd.Suggest; cb.VerticalAlignment=VerticalAlignment.Center; cb.Margin=new Thickness(4,0,10,0);
      var row=new StackPanel(); row.Orientation=Orientation.Horizontal; row.Margin=new Thickness(2,5,2,5); row.Children.Add(cb); row.Children.Add(Theme.Img(Theme.AppIcon(a.IconExe),24));
      var tx=new StackPanel(); tx.Margin=new Thickness(10,0,0,0); tx.Children.Add(Theme.T(a.Name,13.5,Theme.Text,FontWeights.Normal)); tx.Children.Add(Theme.T(a.ProcName+"  ·  "+a.WinClass+(a.Note!=null?"  ·  "+a.Note:""),11.5,a.Note!=null?Theme.Accent:Theme.Sub,FontWeights.Normal)); row.Children.Add(tx);
      list.Children.Add(row); rows.Add(Tuple.Create(cb,a)); }
    if(rows.Count==0){ var n=Theme.T("Nothing new to add.",13,Theme.Sub,FontWeights.Normal); n.Margin=new Thickness(8); list.Children.Add(n); }
    if(exl.Count>0){ var e=Theme.T("Skipped: "+string.Join("; ",exl.Distinct()),11.5,Theme.Sub,FontWeights.Normal); e.TextWrapping=TextWrapping.Wrap; e.Margin=new Thickness(8,10,8,4); list.Children.Add(e); } }
}
