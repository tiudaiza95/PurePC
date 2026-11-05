using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

partial class DesktopApp {
 string lastState="●  Sẵn sàng",sort="Title";bool descending,dark,collapsed;Grid proHost;ComboBox cleanupSources;TextBlock progressText;TextBox toolOutput;Button toolCheck,toolUpdate;List<FileItem> duplicateFiles;string diskReport="Chưa phân tích thư mục.";List<Control> proControls=new List<Control>();
 TextBlock Label(string text,int size=13,bool bold=false){var t=new TextBlock{Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal};t.SetResourceReference(TextBlock.ForegroundProperty,"Ink");return t;}
 Button ActionButton(string caption,Action action){var b=new Button{Content=caption,Margin=new Thickness(0,0,8,8),Padding=new Thickness(14,10,14,10)};b.Click+=(s,e)=>{try{action();}catch(Exception ex){Error(ex);}};proControls.Add(b);return b;}
 Button WorkButton(string caption,Func<Task> action){var b=new Button{Content=caption,Margin=new Thickness(0,0,8,8),Padding=new Thickness(14,10,14,10)};b.Click+=async(s,e)=>await Work(action);proControls.Add(b);return b;}
 Border Card(UIElement child){return new Border{Style=(Style)window.Resources["Card"],Child=child,Margin=new Thickness(0,0,0,12)};}
 void EnableEscapeClose(Window w){if(w!=null)w.PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape){w.Close();e.Handled=true;}};}
 void BuildPro(){
  proHost=new Grid{Visibility=Visibility.Collapsed};((Grid)C<Grid>("AppsPage").Parent).Children.Add(proHost);
  foreach(string n in new[]{"Health","Duplicates","Tools"}){string name=n;C<RadioButton>("Nav"+name).Checked+=(s,e)=>{if(!busy)NavigatePro(name);};}
  Click("ToggleTheme",()=>SetTheme(!dark));Click("CollapseSidebar",OpenSettingsDialog);C<Button>("CollapseSidebar").MouseRightButtonUp+=(s,e)=>Collapse();Click("AppInfo",()=>ShowInfo(target));Click("ShowGroups",ShowGroups);Click("ForceUninstall",()=>RunDetached(ForceUninstallSelected));Click("BatchUninstall",BatchUninstall);Click("SetupMonitorBtn",OpenSetupMonitorDialog);Click("RegistryCleanBtn",OpenRegistryCleanerDialog);Click("ManageServices",OpenServicesDialog);
  var navStack=(StackPanel)C<RadioButton>("NavApps").Parent;var navDock=(DockPanel)navStack.Parent;navDock.Children.Remove(navStack);navDock.Children.Add(new ScrollViewer{Content=navStack,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
  var filter=C<ComboBox>("AppFilter");filter.ItemsSource=new[]{I18n.T("Tất cả ứng dụng"),I18n.T("Mới cài · 30 ngày"),I18n.T("Dung lượng lớn · ≥ 1 GB"),I18n.T("Ít sử dụng · chưa có dữ liệu"),I18n.T("Ứng dụng Microsoft / Windows"),I18n.T("Ứng dụng bên thứ ba"),I18n.T("Hệ thống / driver"),I18n.T("Cân nhắc nếu không dùng"),I18n.T("Cần thận trọng"),I18n.T("Ứng dụng Windows Store (UWP)"),I18n.T("Ứng dụng máy tính (Win32)")};filter.SelectedIndex=0;filter.SelectionChanged+=(s,e)=>Filter();
  var grid=C<DataGrid>("AppsGrid");grid.Columns.Clear();grid.RowHeight=Double.NaN;grid.MinRowHeight=54;grid.FrozenColumnCount=3;
  AddColumn(grid,"STT","Number",40);var iconTemplate=new DataTemplate();var icon=new FrameworkElementFactory(typeof(Image));icon.SetBinding(Image.SourceProperty,new Binding("Icon"));icon.SetValue(Image.WidthProperty,26.0);icon.SetValue(Image.HeightProperty,26.0);iconTemplate.VisualTree=icon;grid.Columns.Add(new DataGridTemplateColumn{Header="",Width=36,CellTemplate=iconTemplate,CanUserSort=false});
  AddColumn(grid,"ỨNG DỤNG","Title",160);AddColumn(grid,"NHÀ PHÁT HÀNH","Vendor",100);AddColumn(grid,"PHIÊN BẢN","Release",90);AddColumn(grid,"DUNG LƯỢNG¹","SizeLabel",90,"InstallBytes");AddColumn(grid,"NGÀY CÀI²","InstalledDate",100);AddColumn(grid,"PHÂN LOẠI","Category",100);AddColumn(grid,"TRẠNG THÁI","StatusLabel",125);
  AddActionColumn(grid,"Thông tin",a=>ShowInfo(a));AddActionColumn(grid,"Gỡ cài đặt",a=>{target=a;grid.SelectedItems.Clear();grid.SelectedItem=a;RunDetached(UninstallSelected);});
  grid.Sorting+=(s,e)=>{e.Handled=true;string key=e.Column.SortMemberPath;if(String.IsNullOrEmpty(key))return;if(sort==key)descending=!descending;else{sort=key;descending=false;}foreach(var col in grid.Columns)col.SortDirection=null;e.Column.SortDirection=descending?ListSortDirection.Descending:ListSortDirection.Ascending;Filter();};
  grid.MouseDoubleClick+=(s,e)=>{if(grid.SelectedItem is AppEntry)ShowInfo((AppEntry)grid.SelectedItem);};
  grid.ContextMenu=new ContextMenu();AddMenu(grid.ContextMenu,"Xem thông tin",()=>ShowInfo(grid.SelectedItem as AppEntry));AddMenu(grid.ContextMenu,"Mở thư mục cài đặt",()=>OpenInstall(grid.SelectedItem as AppEntry));AddMenu(grid.ContextMenu,"Gỡ mục đã chọn",()=>RunDetached(UninstallSelected));AddMenu(grid.ContextMenu,"⚡ Gỡ cưỡng chế",()=>RunDetached(ForceUninstallSelected));AddMenu(grid.ContextMenu,"⚡ Gỡ hàng loạt các mục đã chọn",BatchUninstall);AddMenu(grid.ContextMenu,"Xem nguồn và nhóm trùng",ShowGroups);
  grid.PreviewMouseRightButtonDown+=(s,e)=>{DependencyObject obj=e.OriginalSource as DependencyObject;while(obj!=null&&!(obj is DataGridRow))obj=VisualTreeHelper.GetParent(obj);if(obj is DataGridRow){var row=(DataGridRow)obj;if(!row.IsSelected){grid.SelectedItems.Clear();row.IsSelected=true;}}};
  grid.SelectionChanged+=(s,e)=>{var a=grid.SelectedItem as AppEntry;if(a!=null){Text("SelectedHint",a.StatusLabel+" · "+a.Scope+" · "+a.SizeSource);C<Button>("Uninstall").IsEnabled=!busy&&!a.Protected&&!String.IsNullOrWhiteSpace(a.Command);C<Button>("ForceUninstall").IsEnabled=!busy&&!a.Protected;}};
  var filesTitle=C<TextBlock>("FilesTitle");cleanupSources=new ComboBox{ItemsSource=AnalysisTools.Roots(),SelectedIndex=0,Margin=new Thickness(0,0,0,12),MinWidth=300,HorizontalAlignment=HorizontalAlignment.Left};((StackPanel)filesTitle.Parent).Children.Insert(2,cleanupSources);
  C<DataGrid>("FilesGrid").ContextMenu=new ContextMenu();AddMenu(C<DataGrid>("FilesGrid").ContextMenu,"Mở vị trí",()=>C<Button>("OpenFileLocation").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
  var rg=C<DataGrid>("RemnantsGrid");AddColumn(rg,"TIN CẬY","Confidence",210);AddColumn(rg,"DUNG LƯỢNG","SizeLabel",120);rg.Height=150;
  Click("HistoryRemnants",ChooseRemovalHistory);
  BuildV04();
  try{SetTheme(File.Exists(Path.Combine(Engine.Data,"dark-mode")));}catch{SetTheme(false);}
 }
 Window currentSettingsWindow=null;
 void OpenSettingsDialog(){
  if(currentSettingsWindow!=null&&currentSettingsWindow.IsLoaded){
   currentSettingsWindow.Activate();
   return;
  }
  var dialog=new Window{
   Title=I18n.T("Cài đặt & Tiện ích chuyên sâu · Dọn Dẹp PC 1.2"),
   Owner=window,
   Width=800,
   Height=540,
   MinWidth=720,
   MinHeight=460,
   WindowStartupLocation=WindowStartupLocation.CenterOwner,
   Resources=window.Resources,
   FontFamily=window.FontFamily,
   FontSize=13
  };
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  currentSettingsWindow=dialog;
  dialog.Closed+=(s,e)=>{currentSettingsWindow=null;};

  var root=new DockPanel{Margin=new Thickness(20)};
  dialog.Content=root;

  // Header
  var header=new StackPanel{Margin=new Thickness(0,0,0,12)};
  DockPanel.SetDock(header,Dock.Top);
  root.Children.Add(header);
  header.Children.Add(Label(I18n.T("CÀI ĐẶT & TIỆN ÍCH CHUYÊN SÂU"),19,true));
  header.Children.Add(Label(I18n.T("Tùy biến giao diện, ngôn ngữ, an toàn dữ liệu và trung tâm lối tắt hệ thống"),12));

  // TabControl phân loại khoa học, không bị cuộn
  var tabs=new TabControl{Margin=new Thickness(0,4,0,0)};
  root.Children.Add(tabs);

  // TAB 1: Giao diện & An toàn
  var tab1=new TabItem{Header=I18n.T("⚙️ Cài đặt & An toàn")};
  tabs.Items.Add(tab1);
  var t1Panel=new StackPanel{Margin=new Thickness(12)};
  tab1.Content=t1Panel;

  var uiCard=new StackPanel();
  uiCard.Children.Add(Label(I18n.T("Giao diện & Ngôn ngữ"),14,true));
  var langRow=new WrapPanel{Margin=new Thickness(0,6,0,10)};
  var rbVi=new RadioButton{Content=I18n.T("Tiếng Việt (Mặc định)"),IsChecked=(I18n.Lang=="vi"),Margin=new Thickness(0,0,16,0)};
  rbVi.SetResourceReference(Control.ForegroundProperty,"Ink");
  var rbEn=new RadioButton{Content="English (US)",IsChecked=(I18n.Lang=="en"),Margin=new Thickness(0,0,16,0)};
  rbEn.SetResourceReference(Control.ForegroundProperty,"Ink");
  rbVi.Checked+=(s,e)=>{
   if(I18n.Lang!="vi"){
    I18n.Save("vi");
    ApplyLanguage();
    dialog.Close();
   }
  };
  rbEn.Checked+=(s,e)=>{
   if(I18n.Lang!="en"){
    I18n.Save("en");
    ApplyLanguage();
    dialog.Close();
   }
  };
  langRow.Children.Add(rbVi);langRow.Children.Add(rbEn);
  uiCard.Children.Add(langRow);

  var themeBtns=new WrapPanel();
  themeBtns.Children.Add(ActionButton(dark?I18n.T("☀ Chuyển sang Chế độ sáng"):I18n.T("◐ Chuyển sang Chế độ tối"),()=>{SetTheme(!dark);dialog.Close();}));
  themeBtns.Children.Add(ActionButton(collapsed?I18n.T("Mở rộng thanh bên"):I18n.T("Thu gọn thanh bên"),()=>{Collapse();dialog.Close();}));
  uiCard.Children.Add(themeBtns);
  t1Panel.Children.Add(Card(uiCard));

  var safeCard=new StackPanel{Margin=new Thickness(0,10,0,0)};
  safeCard.Children.Add(Label(I18n.T("Chính sách an toàn dữ liệu 100%"),14,true));
  var c1=new CheckBox{Content="Ưu tiên Thùng rác (Recycle Bin) khi dọn tệp để có thể khôi phục lại",IsChecked=true,IsEnabled=false,Margin=new Thickness(0,4,0,4)};
  c1.SetResourceReference(Control.ForegroundProperty,"Ink");
  var c2=new CheckBox{Content="Tự động sao lưu Registry (.reg) vào thư mục tạm trước khi xử lý",IsChecked=true,IsEnabled=false,Margin=new Thickness(0,4,0,4)};
  c2.SetResourceReference(Control.ForegroundProperty,"Ink");
  var c3=new CheckBox{Content="Tự động sao lưu Driver bên thứ ba trước khi cài đặt gói driver mới",IsChecked=true,IsEnabled=false,Margin=new Thickness(0,4,0,4)};
  c3.SetResourceReference(Control.ForegroundProperty,"Ink");
  safeCard.Children.Add(c1);safeCard.Children.Add(c2);safeCard.Children.Add(c3);
  t1Panel.Children.Add(Card(safeCard));

  // TAB 2: Trung tâm Tiện ích chuyên sâu
  var tab2=new TabItem{Header=I18n.T("🛠️ Lối tắt Tiện ích")};
  tabs.Items.Add(tab2);
  var t2Panel=new StackPanel{Margin=new Thickness(12)};
  tab2.Content=t2Panel;

  var toolCard=new StackPanel();
  toolCard.Children.Add(Label(I18n.T("Trung tâm công cụ nâng cao"),14,true));
  toolCard.Children.Add(Label(I18n.T("Truy cập nhanh các tính năng dọn dẹp và bảo trì hệ thống 1-chạm:"),11));

  var toolBtns=new WrapPanel{Margin=new Thickness(0,8,0,0)};
  toolBtns.Children.Add(ActionButton(I18n.T("🧹 Dọn dẹp Registry"),()=>{dialog.Close();OpenRegistryCleanerDialog();}));
  toolBtns.Children.Add(ActionButton(I18n.T("🧩 Tiện ích trình duyệt"),()=>{dialog.Close();OpenBrowserExtensionsDialog();}));
  toolBtns.Children.Add(ActionButton(I18n.T("⚙️ Dịch vụ bên thứ ba"),()=>{dialog.Close();OpenServicesDialog();}));
  toolBtns.Children.Add(ActionButton(I18n.T("📁 Bản sao lưu Registry"),()=>{dialog.Close();C<Button>("OpenBackups").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}));
  toolBtns.Children.Add(ActionButton(I18n.T("📁 Bản sao lưu Driver"),()=>{
   dialog.Close();
   string path=Path.Combine(Engine.Data,"Driver-backups");
   Directory.CreateDirectory(path);
   Process.Start(new ProcessStartInfo("explorer.exe",Maintenance.Quote(path)){UseShellExecute=true});
  }));
  toolBtns.Children.Add(ActionButton(I18n.T("⚡ Tối ưu Database SQLite"),()=>{dialog.Close();OpenSqliteOptimizerDialog();}));
   toolBtns.Children.Add(ActionButton(I18n.T("🔥 Hủy tệp an toàn (Shredder)"),()=>{dialog.Close();OpenFileShredderDialog();}));
   toolBtns.Children.Add(ActionButton(I18n.T("⚡ Làm sạch DNS Cache"),()=>{
    SystemPerformance.FlushDnsCache();
    MessageBox.Show(I18n.T("Đã làm sạch DNS Resolver Cache thành công!"),I18n.T("Làm sạch DNS Cache"),MessageBoxButton.OK,MessageBoxImage.Information);
   }));
   toolBtns.Children.Add(ActionButton(I18n.T("🗑️ Dọn sạch Thùng rác"),()=>{
    if(MessageBox.Show(I18n.T("Bạn có chắc chắn muốn dọn sạch toàn bộ Thùng rác (Recycle Bin)?"),I18n.T("Dọn Thùng rác"),MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes){
     SystemPerformance.EmptyRecycleBin();
     MessageBox.Show(I18n.T("Đã làm sạch Thùng rác thành công!"),I18n.T("Dọn Thùng rác"),MessageBoxButton.OK,MessageBoxImage.Information);
    }
   }));
   toolBtns.Children.Add(ActionButton(I18n.T("🗑️ Mở Thùng rác Windows"),()=>C<Button>("OpenRecycle").RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
  toolBtns.Children.Add(ActionButton(I18n.T("📝 Mở tệp Nhật ký"),()=>C<Button>("OpenLog").RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
  toolBtns.Children.Add(ActionButton(I18n.T("💾 Công cụ Disk Cleanup"),()=>OpenNative("cleanmgr.exe")));
  toolBtns.Children.Add(ActionButton(I18n.T("⚡ Tối ưu ổ đĩa Defrag"),()=>OpenNative("dfrgui.exe")));
  toolCard.Children.Add(toolBtns);
  t2Panel.Children.Add(Card(toolCard));

  // TAB 3: Thông tin phiên bản
  var tab3=new TabItem{Header=I18n.T("ℹ️ Giới thiệu")};
  tabs.Items.Add(tab3);
  var t3Panel=new StackPanel{Margin=new Thickness(12)};
  tab3.Content=t3Panel;

  var aboutCard=new StackPanel();
  aboutCard.Children.Add(Label(I18n.T("Dọn Dẹp PC Pro Max · Phiên bản 1.2 Chính thức"),15,true));
  aboutCard.Children.Add(Label("Mô hình kết hợp hoàn hảo giữa:\n• CCleaner: Dọn tệp tạm, tối ưu RAM, dọn Registry và quản lý tiện ích trình duyệt.\n• IObit Uninstaller: Gỡ cài đặt chuyên sâu, gỡ hàng loạt và dọn sạch tàn dư rác 1-click.\n• Driver & System Tools: Giám sát phần cứng và bảo trì ổ đĩa chuyên sâu.\n\nCam kết an toàn tuyệt đối, minh bạch 100% dữ liệu và bảo vệ quyền riêng tư.",12));
  
  var githubRow=new WrapPanel{Margin=new Thickness(0,10,0,0)};
  githubRow.Children.Add(ActionButton(I18n.T("Kiểm tra bản cập nhật trên GitHub"),()=>OpenUrl("https://github.com/tiudaiza95/DonDepPC/releases")));
  githubRow.Children.Add(ActionButton(I18n.T("Mở Kho mã nguồn GitHub"),()=>OpenUrl("https://github.com/tiudaiza95/DonDepPC")));
  aboutCard.Children.Add(githubRow);
  t3Panel.Children.Add(Card(aboutCard));

  dialog.ShowDialog();
 }
 void AddMenu(ContextMenu menu,string title,Action action){var item=new MenuItem{Header=title};item.Click+=(s,e)=>{if(busy)return;try{action();}catch(Exception ex){Error(ex);}};menu.Items.Add(item);}
 void AddColumn(DataGrid grid,string title,string property,double width,string sortMember=null){var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));style.Setters.Add(new Setter(TextBlock.ToolTipProperty,new Binding(property)));grid.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(property),SortMemberPath=sortMember??property,Width=width,ElementStyle=style,IsReadOnly=true});}
 void AddActionColumn(DataGrid grid,string caption,Action<AppEntry> action){var template=new DataTemplate();var button=new FrameworkElementFactory(typeof(Button));button.SetValue(Button.ContentProperty,caption);if(caption=="Gỡ cài đặt")button.SetBinding(Button.IsEnabledProperty,new Binding("CanUninstall"));button.SetValue(Button.PaddingProperty,new Thickness(9,7,9,7));button.AddHandler(Button.ClickEvent,new RoutedEventHandler((s,e)=>{if(busy)return;var a=((FrameworkElement)s).DataContext as AppEntry;if(a!=null){e.Handled=true;action(a);}}));template.VisualTree=button;grid.Columns.Add(new DataGridTemplateColumn{Header="",Width=90,CellTemplate=template,CanUserSort=false});}
 async void RunDetached(Func<Task> action){await Work(action);}
 void FilterPro(){if(window==null)return;var grid=C<DataGrid>("AppsGrid");if(grid==null)return;string query=C<TextBox>("Search").Text??"";int filter=C<ComboBox>("AppFilter")==null?0:C<ComboBox>("AppFilter").SelectedIndex;var selected=grid.SelectedItems.Cast<AppEntry>().Select(x=>x.Id).ToArray();var shown=apps.Where(x=>(x.Name??"").IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0||(x.Publisher??"").IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0).Where(x=>MatchesFilter(x,filter)).ToList();
  Func<AppEntry,IComparable> key=a=>SortValue(a,sort);shown=(descending?shown.OrderByDescending(key):shown.OrderBy(key)).ToList();for(int i=0;i<shown.Count;i++)shown[i].Number=i+1;grid.ItemsSource=shown;foreach(var a in shown.Where(x=>selected.Contains(x.Id)))grid.SelectedItems.Add(a);Text("ListTitle","Ứng dụng · "+shown.Count);if(filter==3)Text("Status","Không thu thập lịch sử sử dụng; chưa có bằng chứng để gắn nhãn ít sử dụng.");
 }
 internal static IComparable SortValue(AppEntry a,string key){if(key=="InstallBytes")return a.EstimatedBytes;if(key=="InstalledDate")return a.InstalledOn??"";if(key=="Number")return a.Number;var prop=typeof(AppEntry).GetProperty(key);return prop==null?(a.Name??""):(IComparable)(prop.GetValue(a,null)??"");}
 internal static bool MatchesFilter(AppEntry a,int filter){switch(filter){case 1:DateTime d;return DateTime.TryParseExact(a.InstalledOn,"yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out d)&&d>=DateTime.Today.AddDays(-30)&&d<=DateTime.Today;case 2:return a.EstimatedBytes>=1024L*1024*1024;case 3:return false;case 4:return (a.Publisher??"").IndexOf("Microsoft",StringComparison.OrdinalIgnoreCase)>=0;case 5:return !String.IsNullOrEmpty(a.Publisher)&&(a.Publisher??"").IndexOf("Microsoft",StringComparison.OrdinalIgnoreCase)<0;case 6:return a.SystemComponent||a.Protected;case 7:return a.Advice!=null&&!a.Caution;case 8:return a.Caution;case 9:return a.IsUwp;case 10:return !a.IsUwp;default:return true;}}
 void HidePro(){if(proHost==null)return;proHost.Visibility=Visibility.Collapsed;C<FrameworkElement>("GlobalStats").Visibility=Visibility.Collapsed;C<RowDefinition>("StatsRow").Height=new GridLength(0);cleanupSources.Visibility=page=="Temp"?Visibility.Visible:Visibility.Collapsed;}
 void NavigatePro(string name){if(proHost==null)return;page=name;foreach(string panel in new[]{"AppsPage","FilesPage","StartupPage","HistoryPage"})C<FrameworkElement>(panel).Visibility=Visibility.Collapsed;HidePro();proHost.Visibility=Visibility.Visible;proHost.Children.Clear();proControls.RemoveAll(x=>x.Parent==null);if(name=="Health")BuildHealth();if(name=="Duplicates")BuildDuplicatePage();if(name=="Tools")BuildTools();C<RadioButton>("Nav"+name).IsChecked=true;}
 void StatePro(){if(proHost==null)return;foreach(var control in proControls)control.IsEnabled=!busy;foreach(string n in new[]{"AppInfo","ShowGroups","AppFilter","ToggleTheme","CollapseSidebar","NavHealth","NavDuplicates","NavTools"})C<Control>(n).IsEnabled=!busy;cleanupSources.IsEnabled=!busy;StateV05();}
 void SetTheme(bool value){dark=value;string[] names={"Canvas","Ink","Muted","Panel","Soft","Header","Line","Stripe","Accent","AccentInk","Good","GoodSoft"};string[] values=value?new[]{"#111823","#E8EEF8","#A6B5CC","#1B2636","#243D60","#223044","#344457","#1E2B3E","#337BF0","#9CC4FF","#70DBB4","#183C34"}:new[]{"#F4F6FA","#1C2940","#64748B","#FFFFFF","#E9F0FF","#F1F4F9","#DFE5EF","#FAFBFD","#2875F5","#2563D9","#168569","#E5F6EF"};for(int i=0;i<names.Length;i++)window.Resources[names[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[i]));C<Button>("ToggleTheme").Content=value?I18n.T("☀ Sáng"):I18n.T("◐ Tối");if(!Engine.Testing){Directory.CreateDirectory(Engine.Data);string setting=Path.Combine(Engine.Data,"dark-mode");if(value)File.WriteAllText(setting,"dark");else if(File.Exists(setting))File.Delete(setting);}}
 void Collapse(){collapsed=!collapsed;foreach(string n in new[]{"Deep","DiskTools","Drivers"})C<RadioButton>("Nav"+n).Content=collapsed?(n=="Deep"?"⌕":n=="DiskTools"?"▣":"⚙"):(n=="Deep"?I18n.T("Quét chuyên sâu"):n=="DiskTools"?I18n.T("Công cụ ổ đĩa"):I18n.T("Driver & thiết bị"));C<ColumnDefinition>("SidebarColumn").Width=new GridLength(collapsed?80:220);C<FrameworkElement>("BrandName").Visibility=collapsed?Visibility.Collapsed:Visibility.Visible;C<FrameworkElement>("BrandCaption").Visibility=collapsed?Visibility.Collapsed:Visibility.Visible;C<FrameworkElement>("SidebarFoot").Visibility=collapsed?Visibility.Collapsed:Visibility.Visible;C<Image>("BrandImage").Width=collapsed?32:54;foreach(string n in new[]{"Apps","Temp","Large","Startup","Tools","History"}){var nav=C<RadioButton>("Nav"+n);if(nav!=null){var stack=nav.Content as StackPanel;if(stack!=null&&stack.Children.Count>1)((UIElement)stack.Children[1]).Visibility=collapsed?Visibility.Collapsed:Visibility.Visible;nav.ToolTip=I18n.T(n);}}foreach(string n in new[]{"Health","Duplicates"})C<RadioButton>("Nav"+n).Content=collapsed?(n=="Health"?"⌂":"≡"):(n=="Health"?I18n.T("Tổng quan"):I18n.T("Tệp trùng nội dung"));C<Button>("ToggleTheme").Content=collapsed?"◐":(dark?I18n.T("☀ Sáng"):I18n.T("◐ Tối"));if(C<Button>("ToggleLang")!=null)C<Button>("ToggleLang").Visibility=collapsed?Visibility.Collapsed:Visibility.Visible;}
 void OpenInstall(AppEntry a){if(a==null)return;if(String.IsNullOrWhiteSpace(a.Location)||!Directory.Exists(a.Location))throw new Exception("Không có đường dẫn cài đặt đã đăng ký còn tồn tại. Xem tệp tham chiếu trong Thông tin.");Process.Start(new ProcessStartInfo(a.Location){UseShellExecute=true});}
 string DetailText(AppEntry a){var d=a.Advice??new Advisory();return a.Name+"\n\nTHÔNG TIN TẠI MÁY\nNhà phát hành: "+a.Vendor+"\nQuốc gia: "+d.Country+"\nPhiên bản: "+a.Release+"\nDanh mục: "+d.Category+"\nNgày: "+a.InstalledDate+" (Registry InstallDate; có thể là ngày cập nhật)\nDung lượng: "+a.SizeLabel+"\nNguồn/cách tính: "+a.SizeSource+"\nVị trí cài: "+(String.IsNullOrWhiteSpace(a.Location)?"Chưa được trình cài khai báo":a.Location)+"\nTệp tham chiếu: "+Engine.KnownExecutable(a)+"\nPhạm vi: "+a.Scope+"\n"+a.Architecture+"\n\nCHỨC NĂNG\n"+d.Description+"\n\nĐÁNH GIÁ\n"+d.Level+"\nLý do: "+d.Reason+"\nTin cậy: "+d.Confidence+"\nẢnh hưởng: "+d.Impact+"\nSao lưu: "+d.Backup+"\n\nNGUỒN\n"+d.Source+"\nNgày xác minh nguồn: "+d.Checked+"\nNguồn cục bộ: "+a.Hive+" / "+a.View+" / "+a.Key+"\nURL do trình cài khai báo (chưa xác minh): "+(a.Website??"");}
 void ShowInfo(AppEntry a){if(a==null){Text("Status","Chọn một ứng dụng trước.");return;}var dialog=new Window{Title="Thông tin · "+a.Name,Owner=window,Width=820,Height=780,MinWidth=650,MinHeight=500,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13};dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);var root=new DockPanel();var frame=new Border{Padding=new Thickness(24),Child=root};frame.SetResourceReference(Border.BackgroundProperty,"Canvas");dialog.Content=frame;var title=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,18)};title.Children.Add(new Image{Source=a.Icon,Width=40,Height=40,Margin=new Thickness(0,0,14,0)});title.Children.Add(Label(a.Name,22,true));DockPanel.SetDock(title,Dock.Top);root.Children.Add(title);
  var actions=new WrapPanel{Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(actions,Dock.Bottom);root.Children.Add(actions);var body=new StackPanel();root.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled});var d=a.Advice??new Advisory();
  var basic=new StackPanel();basic.Children.Add(Label("Thông tin tại máy",17,true));
  var facts=new Grid();facts.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(150)});facts.ColumnDefinitions.Add(new ColumnDefinition());
  string[,] pairs={{"Nhà phát hành",a.Vendor},{"Quốc gia",d.Country},{"Phiên bản",a.Release},{"Danh mục",a.Category},{"Ngày đăng ký",a.InstalledDate+" · InstallDate có thể là ngày cập nhật"},{"Dung lượng",a.SizeLabel+" — "+a.SizeSource},{"Vị trí",String.IsNullOrWhiteSpace(a.Location)?"Trình cài chưa khai báo":a.Location},{"Phạm vi",a.Scope+" · "+a.Architecture}};
  for(int i=0;i<pairs.GetLength(0);i++){facts.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});var key=Label(pairs[i,0],12);key.SetResourceReference(TextBlock.ForegroundProperty,"Muted");var value=Label(pairs[i,1],13);Grid.SetRow(key,i);Grid.SetRow(value,i);Grid.SetColumn(value,1);facts.Children.Add(key);facts.Children.Add(value);}basic.Children.Add(facts);body.Children.Add(Card(basic));
  var description=new StackPanel();description.Children.Add(Label("Phần mềm này làm gì?",17,true));description.Children.Add(Label(d.Description));body.Children.Add(Card(description));
  var advisory=new StackPanel();advisory.Children.Add(Label(d.Level,17,true));advisory.Children.Add(Label("Vì sao: "+d.Reason));advisory.Children.Add(Label("Độ tin cậy: "+d.Confidence));advisory.Children.Add(Label("Có thể ảnh hưởng: "+d.Impact));advisory.Children.Add(Label("Trước khi gỡ: "+d.Backup));body.Children.Add(Card(advisory));
  var sources=new StackPanel();sources.Children.Add(Label("Nguồn và thời điểm",17,true));sources.Children.Add(Label(d.Source+"\n"+d.Checked+"\nRegistry: "+a.Hive+" / "+a.View+" / "+a.Key+"\nWebsite do bộ cài khai báo, chưa xác minh: "+(a.Website??"")));body.Children.Add(Card(sources));
  var signature=Label("Chữ ký số: chưa kiểm tra. Kiểm tra áp dụng cho tệp tham chiếu tại máy, không đại diện mọi tệp của ứng dụng.");body.Children.Add(signature);var sig=ActionButton("Kiểm tra chữ ký",()=>{});sig.Click+=async(s,e)=>{sig.IsEnabled=false;try{signature.Text=await Task.Run(()=>SignatureCheck.Check(Engine.KnownExecutable(a)));}finally{sig.IsEnabled=true;}};body.Children.Add(sig);
  var uninstall=ActionButton("Gỡ cài đặt",()=>{dialog.Close();target=a;C<DataGrid>("AppsGrid").SelectedItems.Clear();C<DataGrid>("AppsGrid").SelectedItem=a;RunDetached(UninstallSelected);});uninstall.IsEnabled=!a.Protected&&!String.IsNullOrWhiteSpace(a.Command);actions.Children.Add(uninstall);var forceUninstall=ActionButton("⚡ Gỡ cưỡng chế",()=>{dialog.Close();target=a;C<DataGrid>("AppsGrid").SelectedItems.Clear();C<DataGrid>("AppsGrid").SelectedItem=a;RunDetached(ForceUninstallSelected);});forceUninstall.IsEnabled=!a.Protected;actions.Children.Add(forceUninstall);actions.Children.Add(ActionButton("Mở thư mục",()=>OpenInstall(a)));actions.Children.Add(ActionButton("Sao chép thông tin",()=>Clipboard.SetText(DetailText(a)+"\n\n"+signature.Text)));
  var official=ActionButton("Trang chính thức",()=>OpenUrl(a.Advice.Url));official.IsEnabled=a.Advice!=null&&!String.IsNullOrEmpty(a.Advice.Url);actions.Children.Add(official);actions.Children.Add(ActionButton("Tra cứu trực tuyến",()=>{if(!Confirm("Mở trình duyệt để tìm tên và nhà phát hành của ứng dụng này?\nChỉ gửi truy vấn về ứng dụng bạn chọn, không gửi danh sách đã cài. Kết quả web cần tự đối chiếu."))return;string url="https://www.google.com/search?q="+Uri.EscapeDataString(a.Name+" "+a.Publisher+" official documentation");OpenUrl(url);Engine.Log("LOOKUP_OPENED "+DateTime.Now.ToString("s")+" | "+a.Name+" | "+url);signature.Text+="\nĐã mở tìm kiếm: "+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+". Chưa xác minh nội dung kết quả web.";}));actions.Children.Add(ActionButton("Cài đặt Windows",()=>Process.Start(new ProcessStartInfo("ms-settings:appsfeatures"){UseShellExecute=true})));actions.Children.Add(ActionButton("Quay lại",()=>dialog.Close()));if(Engine.Testing){dialog.Show();dialog.UpdateLayout();SaveWindow(dialog,"Detail-"+(dark?"Dark":"Light"));dialog.Close();}else dialog.ShowDialog();}
 static void OpenUrl(string url){Uri uri;if(!Uri.TryCreate(url,UriKind.Absolute,out uri)||(uri.Scheme!="https"&&uri.Scheme!="http"))throw new Exception("URL không hợp lệ.");Process.Start(new ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});}
 void ShowGroups(){var dialog=new Window{Title="Nguồn đọc & các mục cùng tên",Owner=window,Width=960,Height=660,Resources=window.Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");EnableEscapeClose(dialog);var panel=new DockPanel{Margin=new Thickness(22)};dialog.Content=panel;var note=Label("Chỉ gộp cùng danh tính HKCU và metadata từ hai chế độ đọc. Cùng tên khác khóa/phiên bản/phạm vi vẫn giữ riêng. Không xóa mục nào. Mở từng nhóm để đối chiếu.");DockPanel.SetDock(note,Dock.Top);panel.Children.Add(note);var tree=new TreeView{Background=Brushes.Transparent,BorderThickness=new Thickness(0)};tree.SetResourceReference(Control.ForegroundProperty,"Ink");panel.Children.Add(tree);
  foreach(var group in apps.GroupBy(x=>Engine.AppNames(x).LastOrDefault()??Engine.Norm(x.Name)).Where(g=>g.Count()>1||g.Any(x=>x.Sources.Count>1))){var item=new TreeViewItem{Header=group.First().Name+" — "+group.Count()+" mục cài đặt, "+group.Sum(x=>x.Sources.Count)+" nguồn đọc",Margin=new Thickness(0,6,0,6)};foreach(var a in group){var child=new TreeViewItem{Header=a.Name+" | "+a.Release+" | "+a.Scope+" | "+a.View+" — "+(a.Sources.Count>1?"cùng mục đọc lặp":"giữ độc lập")};foreach(var source in a.Sources)child.Items.Add(Label(source.Hive+" / "+source.View+"\n"+source.Key+"\nLệnh: "+source.Command+"\nVị trí: "+source.Location,12));item.Items.Add(child);}tree.Items.Add(item);}if(tree.Items.Count==0)tree.Items.Add(new TreeViewItem{Header="Không có nhóm cùng tên hoặc nguồn đọc lặp."});dialog.ShowDialog();}
 async Task UninstallSelected(){var selected=C<DataGrid>("AppsGrid").SelectedItems.Cast<AppEntry>().ToList();if(selected.Count==0&&target!=null)selected.Add(target);if(selected.Count==0)return;if(selected.Any(a=>a.Protected||String.IsNullOrWhiteSpace(a.Command)))throw new Exception("Có mục hệ thống/driver/bảo mật hoặc không có lệnh gỡ. Bỏ chọn mục đó; không ép gỡ.");if(selected.Count>1&&selected.Any(a=>a.Caution))throw new Exception("Nhóm có thành phần cần thận trọng hoặc chưa rõ. Hãy xem thông tin và gỡ từng mục riêng nếu đã xác định được nhu cầu.");
  if(!Confirm("Gỡ "+selected.Count+" ứng dụng sau bằng trình gỡ chính thức?\n\n"+String.Join("\n",selected.Select(x=>x.Name+" · "+x.Release))+"\n\nSao lưu dữ liệu cần giữ. Trình gỡ có thể yêu cầu quyền qua hộp thoại Windows; ứng dụng này không tự nâng quyền."))return;
  var reports=new List<string>();foreach(var a in selected){target=a;Engine.Remember(a);Text("Status","Đang chờ trình gỡ: "+a.Name);try{if(a.IsUwp){
    bool ok=UwpManager.RemovePackage(a.PackageFullName);
    reports.Add(a.Name+" (UWP Store) | "+(ok?"Đã gỡ cài đặt thành công":"Không thể gỡ bỏ"));
    Engine.Log("UNINSTALL_UWP "+a.Name+" | ok="+ok);
   }else{
    var start=Engine.Uninstaller(a);int exit=await Task.Run(()=>{using(var proc=Process.Start(start)){if(proc==null)return -1;proc.WaitForExit();return proc.ExitCode;}});bool present=Engine.Present(a);string result=a.Name+" | exit="+exit+" | "+(present?"Registry vẫn còn: chưa xác nhận gỡ xong":"không còn khóa cài đặt")+(exit==3010?" | yêu cầu khởi động lại":"");reports.Add(result);Engine.Log("UNINSTALL_RESULT "+result);if(present){reports.Add("Dừng chuỗi để tránh chạy song song trình gỡ con. Hoàn tất cửa sổ trình gỡ, rồi làm mới.");break;}
   }}catch(Exception ex){reports.Add(a.Name+": "+ex.Message);Engine.Log("UNINSTALL_ERROR "+a.Name+" | "+ex.Message);break;}}
  await Reload();if(target!=null&&!Engine.Present(target)){Text("Status","Đang tự động quét phần dư của "+target.Name+"…");try{var found=await Task.Run(()=>Engine.Scan(target));if(found.Count>0){remnants=found;BindRemnants();var cleanable=found.Where(x=>x.CanClean).ToList();long bytes=cleanable.Sum(x=>x.Bytes);if(cleanable.Count>0&&Confirm("ĐÃ GỠ CÀI ĐẶT THÀNH CÔNG!\n\nPhát hiện "+cleanable.Count+" mục phần dư rác còn sót lại ("+Features.Size(bytes)+") của "+target.Name+".\n\nBạn có muốn tự động chọn và dọn dẹp sạch sẽ ngay bây giờ không?")){int done=0;foreach(var item in cleanable){try{Engine.Clean(target,item);remnants.Remove(item);done++;}catch{}}BindRemnants();reports.Add("Tự động dọn phần dư: Đã xóa sạch "+done+" mục ("+Features.Size(bytes)+")!");}}}catch{}}Text("Status","Đã hoàn tất quy trình gỡ cài đặt.");MessageBox.Show(window,String.Join("\n\n",reports),"Kết quả thực tế");
 }
 async Task ScanFilesPro(){bool temp=page=="Temp";string root="";CleanupRoot scope=null;if(temp){scope=cleanupSources.SelectedItem as CleanupRoot;if(scope==null)throw new Exception("Chọn nguồn dọn trước.");root=scope.Path;}else{root=PickFolder("Chọn thư mục để phân tích và tìm tệp lớn. Chỉ đọc dữ liệu.");if(root==null)return;}await WithScan(async token=>{var p=Progress();if(temp){var result=await Task.Run(()=>AnalysisTools.Cleanup(scope,token,p));files=result.Items;Text("FilesSummary",files.Count+" tệp · "+Features.Size(files.Sum(x=>x.Size))+" · bỏ qua "+result.Skipped+(result.Limited?" · giới hạn kết quả":""));}else{int skipped=0;bool limited=false;var all=await Task.Run(()=>AnalysisTools.Walk(root,token,p,out skipped,out limited));diskReport=AnalysisTools.DiskSummary(all);files=all.Where(x=>x.Size>=100L*1024*1024).OrderByDescending(x=>x.Size).Take(500).ToList();Text("FilesSummary",all.Count+" tệp đã đọc · "+Features.Size(all.Sum(x=>x.Size))+" · "+files.Count+" tệp lớn · bỏ qua "+skipped+(limited?" · quét chưa đầy đủ":""));ShowChart(all);}C<DataGrid>("FilesGrid").ItemsSource=files;Text("Status","Quét hoàn tất: "+root+". Chưa thay đổi dữ liệu.");});}
 string PickFolder(string description){using(var picker=new System.Windows.Forms.FolderBrowserDialog()){picker.Description=description;picker.ShowNewFolderButton=false;return picker.ShowDialog(new Owner(new WindowInteropHelper(window).Handle))==System.Windows.Forms.DialogResult.OK?picker.SelectedPath:null;}}
 IProgress<ScanProgress> Progress(){return new Progress<ScanProgress>(p=>{Text("Status",p.ToString());if(progressText!=null)progressText.Text=p.ToString();});}
 async Task WithScan(Func<CancellationToken,Task> action){cancellation=new CancellationTokenSource();C<Button>("CancelScan").Visibility=Visibility.Visible;try{await action(cancellation.Token);}finally{C<Button>("CancelScan").Visibility=Visibility.Collapsed;cancellation.Dispose();cancellation=null;}}
 async Task CleanFilesPro(){if(page=="Large"){await DeleteLargeFiles();return;}if(page!="Temp")return;var chosen=files.Where(x=>x.Selected).ToArray();if(chosen.Length==0){Text("Status","Chưa chọn tệp.");return;}if(!Confirm("Chuyển "+chosen.Length+" tệp vào Thùng rác?\nKhông dọn cookie, phiên đăng nhập hay mật khẩu. Đóng ứng dụng liên quan trước.\nNếu Windows yêu cầu xóa vĩnh viễn, hãy Hủy. Chuyển vào Thùng rác chưa đồng nghĩa giải phóng dung lượng."))return;int done=0,skip=0;long bytes=0;var deltas=new Dictionary<string,long>();foreach(var f in chosen){string volume=Path.GetPathRoot(f.Path);if(!deltas.ContainsKey(volume))try{deltas[volume]=new DriveInfo(volume).AvailableFreeSpace;}catch{}}
  foreach(var f in chosen){try{AnalysisTools.Clean(f);files.Remove(f);bytes+=f.Size;done++;}catch(Exception ex){skip++;Engine.Log("CLEAN_SKIPPED "+f.Path+" | "+ex.Message);}Text("Status","Đã xử lý "+(done+skip)+" / "+chosen.Length+" tệp");await Task.Delay(1);}long change=0;foreach(var d in deltas)try{change+=new DriveInfo(d.Key).AvailableFreeSpace-d.Value;}catch{}Engine.Log("CLEAN_SUMMARY recycledBytes="+bytes+"; observedFreeSpaceDelta="+change+"; completed="+done+"; skipped="+skip+"; delta may include other processes");C<DataGrid>("FilesGrid").ItemsSource=null;C<DataGrid>("FilesGrid").ItemsSource=files;Text("FilesSummary",done+" tệp chuyển vào Thùng rác · "+Features.Size(bytes)+" · giữ lại "+skip);Text("Status","Biến động dung lượng trống đo được: "+(change<0?"−":"+")+Features.Size(Math.Abs(change))+"; có thể chịu ảnh hưởng tác vụ khác. Không coi byte tái chế là byte giải phóng.");}
 void ShowChart(List<FileItem> all){var dialog=new Window{Title="Phân tích dung lượng · chỉ phạm vi đã quét",Owner=window,Width=720,Height=650,Resources=window.Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");EnableEscapeClose(dialog);var panel=new StackPanel{Margin=new Thickness(24)};dialog.Content=new ScrollViewer{Content=panel};panel.Children.Add(Label("Dung lượng theo loại tệp",24,true));panel.Children.Add(Label("Tổng kích thước logic của các tệp đọc được; không phải dung lượng vật lý. Hard link có thể được tính nhiều lần."));long total=Math.Max(1,all.Sum(x=>x.Size));foreach(var group in all.GroupBy(x=>Path.GetExtension(x.Path).ToLowerInvariant()).OrderByDescending(x=>x.Sum(f=>f.Size)).Take(12)){long bytes=group.Sum(x=>x.Size);panel.Children.Add(Label((group.Key==""?"Không đuôi":group.Key)+" · "+Features.Size(bytes)+" · "+(100.0*bytes/total).ToString("0.0")+"%"));panel.Children.Add(new ProgressBar{Value=100.0*bytes/total,Maximum=100,Height=12,Margin=new Thickness(0,0,0,14)});}dialog.ShowDialog();}
 void BuildDuplicatePage(){proHost.Children.Clear();Text("Heading","Trùng nội dung, không chỉ trùng tên.");Text("Subtitle","So nội dung trong thư mục đã chọn · giữ một bản, xóa các bản không cần");var dock=new DockPanel();proHost.Children.Add(dock);var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);top.Children.Add(Label("Giới hạn 100.000 mục / 50.000 tệp / 20 GB đọc hash. Bỏ qua liên kết và tệp thay đổi. Bạn chọn bản giữ lại và các bản chuyển vào Thùng rác; không ghép nội dung tệp."));top.Children.Add(WorkButton("Chọn thư mục & tìm tệp trùng",async()=>{string root=PickFolder("Chọn thư mục kiểm tra nội dung trùng. Không xóa tệp.");if(root==null)return;await WithScan(async token=>{string summary="";var progress=Progress();duplicateFiles=await Task.Run(()=>AnalysisTools.Duplicates(root,token,progress,out summary));BuildDuplicatePage();Text("Status",summary);progressText.Text=summary;});}));progressText=Label(duplicateFiles==null?"Chưa quét.":duplicateFiles.Count+" đường dẫn có SHA-256 trùng.");top.Children.Add(progressText);var grid=new DataGrid{IsReadOnly=true,ItemsSource=duplicateFiles};proControls.Add(grid);AddColumn(grid,"NHÓM","Group",80);AddColumn(grid,"DUNG LƯỢNG","SizeLabel",110);AddColumn(grid,"ĐƯỜNG DẪN","Path",520);AddColumn(grid,"SHA-256","Evidence",440);grid.ContextMenu=new ContextMenu();AddMenu(grid.ContextMenu,"Mở vị trí",()=>{var f=grid.SelectedItem as FileItem;if(f!=null)Process.Start(new ProcessStartInfo("explorer.exe","/select,\""+f.Path+"\""){UseShellExecute=true});});dock.Children.Add(grid);AddDuplicateActions(top,grid);}

 async Task ForceUninstallSelected(){
  var a=target??(C<DataGrid>("AppsGrid").SelectedItem as AppEntry);
  if(a==null){Text("Status","Chưa chọn ứng dụng cần gỡ cưỡng chế.");return;}
  if(a.Protected){MessageBox.Show(window,"Ứng dụng này là thành phần cốt lõi của hệ điều hành hoặc driver bảo vệ; không được phép gỡ cưỡng chế.","Bảo vệ hệ thống",MessageBoxButton.OK,MessageBoxImage.Warning);return;}
  string locText=String.IsNullOrWhiteSpace(a.Location)?"(Không rõ thư mục cài đặt)":a.Location;
  if(!Confirm("⚡ CẢNH BÁO: GỠ CÀI ĐẶT CƯỠNG CHẾ (FORCE UNINSTALL)\n\nỨng dụng: "+a.Name+" ("+a.Release+")\nThư mục: "+locText+"\n\nTính năng này chuyên dùng khi:\n• Trình gỡ cài đặt chính thức bị lỗi, mất file uninstaller hoặc crash\n• Cần dọn sạch phần mềm mà không chạy bộ gỡ trung gian\n\nHệ thống an toàn sẽ thực hiện:\n1. Tự động sao lưu khóa Registry của ứng dụng ra file .reg an toàn\n2. Xóa mục đăng ký gỡ cài đặt khỏi Registry\n3. Chuyển toàn bộ thư mục cài đặt vào Thùng rác (Recycle Bin)\n4. Quét sâu và dọn sạch các tệp tàn dư cấu hình (AppData/Shortcuts)\n\nBạn có chắc chắn muốn tiến hành không?"))return;
  var reports=new List<string>();
  reports.Add("=== KẾT QUẢ GỠ CƯỠNG CHẾ: "+a.Name+" ===");
  try{
   string backupDir=Path.Combine(Path.GetTempPath(),"DonDepPC_Backup");
   Directory.CreateDirectory(backupDir);
   string safeName=string.Join("_",a.Name.Split(Path.GetInvalidFileNameChars()));
   string backupFile=Path.Combine(backupDir,"Backup_"+safeName+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".reg");
   string rootKey=(a.Hive==Microsoft.Win32.RegistryHive.LocalMachine?"HKLM":"HKCU")+"\\"+a.Key;
   var p=Process.Start(new ProcessStartInfo("reg.exe","export \""+rootKey+"\" \""+backupFile+"\" /y"){CreateNoWindow=true,UseShellExecute=false});
   if(p!=null){p.WaitForExit(3000);if(File.Exists(backupFile))reports.Add("✓ Đã sao lưu Registry an toàn tại:\n"+backupFile);}
  }catch(Exception ex){reports.Add("⚠ Không thể sao lưu Registry: "+ex.Message);}
  try{
   using(var baseKey=Microsoft.Win32.RegistryKey.OpenBaseKey(a.Hive,a.View)){
    string parentPath=@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    using(var unKey=baseKey.OpenSubKey(parentPath,true)){
     if(unKey!=null){
      string subName=a.Key.Contains("\\")?a.Key.Substring(a.Key.LastIndexOf('\\')+1):a.Key;
      unKey.DeleteSubKeyTree(subName,false);
      reports.Add("✓ Đã xóa mục đăng ký khỏi Registry Uninstall.");
     }
    }
   }
  }catch(Exception ex){reports.Add("⚠ Lỗi xóa Registry: "+ex.Message+" (Có thể cần quyền Quản trị viên)");}
  if(!String.IsNullOrWhiteSpace(a.Location)&&Directory.Exists(a.Location)){
   if(!FileActions.ProtectedPath(a.Location)&&a.Location.TrimEnd('\\').Length>3){
    try{
     Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(a.Location,Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
     reports.Add("✓ Đã chuyển thư mục cài đặt vào Thùng rác:\n"+a.Location);
    }catch(Exception ex){reports.Add("⚠ Không thể xóa thư mục cài đặt: "+ex.Message);}
   }else{reports.Add("i Bỏ qua xóa thư mục cài đặt vì là đường dẫn được bảo vệ.");}
  }
  try{
   Text("Status","Đang quét phần dư của "+a.Name+"…");
   var found=await Task.Run(()=>Engine.Scan(a));
   if(found.Count>0){
    remnants=found;BindRemnants();
    var cleanable=found.Where(x=>x.CanClean).ToList();
    long bytes=cleanable.Sum(x=>x.Bytes);
    if(cleanable.Count>0&&Confirm("ĐÃ XÓA CÀI ĐẶT CƯỠNG CHẾ!\n\nPhát hiện "+cleanable.Count+" mục phần dư cấu hình/rác ("+Features.Size(bytes)+") của "+a.Name+".\n\nBạn có muốn tự động chuyển sạch sẽ vào Thùng rác ngay bây giờ không?")){
     int done=0;foreach(var item in cleanable){try{Engine.Clean(a,item);remnants.Remove(item);done++;}catch{}}
     BindRemnants();
     reports.Add("✓ Đã tự động dọn phần dư: Xóa sạch "+done+" mục ("+Features.Size(bytes)+") vào Thùng rác.");
    }
   }
  }catch{}
  await Reload();
  Text("Status","Đã hoàn tất gỡ cưỡng chế: "+a.Name);
  MessageBox.Show(window,String.Join("\n\n",reports),"Kết quả Gỡ cài đặt cưỡng chế");
 }

 void OpenServicesDialog(){
  var dialog=new Window{Title=I18n.T("Quản lý Dịch vụ bên thứ ba (Third-party Services) · Dọn Dẹp PC 1.2"),Owner=window,Width=920,Height=640,MinWidth=750,MinHeight=480,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13};
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  var root=new DockPanel{Margin=new Thickness(20)};dialog.Content=root;
  var header=new StackPanel{Margin=new Thickness(0,0,0,14)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(Label("QUẢN LÝ DỊCH VỤ BÊN THỨ BA (SERVICES)",19,true));
  header.Children.Add(Label("Chuyển dịch vụ không cần thiết sang Thủ công (Manual) để tăng tốc khởi động và giải phóng RAM, hoặc Vô hiệu hóa (Disabled) nếu không dùng.",12));
  var filterRow=new Grid{Margin=new Thickness(0,12,0,0)};
  filterRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(260)});
  filterRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(200)});
  filterRow.ColumnDefinitions.Add(new ColumnDefinition());
  header.Children.Add(filterRow);
  var searchBox=new TextBox{Margin=new Thickness(0,0,10,0)};Grid.SetColumn(searchBox,0);filterRow.Children.Add(searchBox);
  var filterCombo=new ComboBox{ItemsSource=new[]{"Tất cả dịch vụ","Khởi động Tự động (Auto)","Khởi động Thủ công (Manual)","Đã vô hiệu hóa (Disabled)"},SelectedIndex=0};
  Grid.SetColumn(filterCombo,1);filterRow.Children.Add(filterCombo);
  var statusText=Label("Đang đọc dịch vụ…",12);statusText.VerticalAlignment=VerticalAlignment.Center;statusText.Margin=new Thickness(12,0,0,0);Grid.SetColumn(statusText,2);filterRow.Children.Add(statusText);
  var bottom=new StackPanel{Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  var btnRow=new WrapPanel();bottom.Children.Add(btnRow);
  var grid=new DataGrid{IsReadOnly=true,SelectionMode=DataGridSelectionMode.Single,RowHeight=Double.NaN,MinRowHeight=44,AutoGenerateColumns=false};
  root.Children.Add(grid);
  AddColumn(grid,"DỊCH VỤ","DisplayName",260);AddColumn(grid,"TRẠNG THÁI","StatusLabel",140);AddColumn(grid,"NHÀ PHÁT TRIỂN","Vendor",160);AddColumn(grid,"MÃ DỊCH VỤ","Name",130);AddColumn(grid,"ĐƯỜNG DẪN THỰC THI","CleanPath",320);
  List<ThirdPartyService> allServices=new List<ThirdPartyService>();
  Action refreshList=()=>{
   string q=(searchBox.Text??"").ToLowerInvariant().Trim();int f=filterCombo.SelectedIndex;
   var filtered=allServices.Where(s=>{
    if(!String.IsNullOrEmpty(q)){
     bool match=(s.DisplayName??"").ToLowerInvariant().Contains(q)||(s.Name??"").ToLowerInvariant().Contains(q)||(s.Vendor??"").ToLowerInvariant().Contains(q);
     if(!match)return false;
    }
    if(f==1&&s.StartType!=2)return false;
    if(f==2&&s.StartType!=3)return false;
    if(f==3&&s.StartType!=4)return false;
    return true;
   }).ToList();
   grid.ItemsSource=filtered;
   statusText.Text="Hiển thị "+filtered.Count+" / "+allServices.Count+" dịch vụ bên thứ ba.";
  };
  Action loadServices=()=>{allServices=ServiceTools.GetServices();refreshList();};
  searchBox.TextChanged+=(s,e)=>refreshList();
  filterCombo.SelectionChanged+=(s,e)=>refreshList();
  Action<int,string> changeState=(newType,actionName)=>{
   var s=grid.SelectedItem as ThirdPartyService;
   if(s==null){MessageBox.Show(dialog,"Vui lòng chọn một dịch vụ trong danh sách trước.","Thông báo");return;}
   if(s.StartType==newType){MessageBox.Show(dialog,"Dịch vụ đã ở trạng thái này.","Thông báo");return;}
   string msg;
   if(ServiceTools.SetStartType(s.Name,newType,out msg)){
    s.StartType=newType;refreshList();
    MessageBox.Show(dialog,"Đã chuyển dịch vụ '"+s.DisplayName+"' sang: "+s.StatusLabel,"Thành công",MessageBoxButton.OK,MessageBoxImage.Information);
   }else{
    MessageBox.Show(dialog,msg+"\n\n(Nếu gặp lỗi quyền, hãy đóng ứng dụng và chọn 'Run as administrator')","Không thể thay đổi",MessageBoxButton.OK,MessageBoxImage.Warning);
   }
  };
  var btnManual=ActionButton("⚡ Chuyển sang Thủ công (Manual)",()=>changeState(3,"Thủ công"));
  btnManual.ToolTip="Khuyên dùng: Ứng dụng chỉ chạy dịch vụ khi cần, không tự khởi động ngầm cùng Windows giúp tiết kiệm RAM.";
  btnRow.Children.Add(btnManual);
  var btnDisable=ActionButton("⛔ Vô hiệu hóa (Tắt hẳn)",()=>changeState(4,"Vô hiệu hóa"));
  btnDisable.ToolTip="Ngăn chặn dịch vụ chạy hoàn toàn.";
  btnRow.Children.Add(btnDisable);
  var btnAuto=ActionButton("▶️ Bật Tự động (Auto)",()=>changeState(2,"Tự động"));
  btnAuto.ToolTip="Cho phép dịch vụ tự động khởi động cùng Windows.";
  btnRow.Children.Add(btnAuto);
  var btnRefresh=ActionButton("🔄 Làm mới",loadServices);
  btnRow.Children.Add(btnRefresh);
  var btnOpenLoc=ActionButton("📁 Mở thư mục",()=>{
   var s=grid.SelectedItem as ThirdPartyService;
   if(s!=null&&!String.IsNullOrEmpty(s.CleanPath)&&File.Exists(s.CleanPath)){
    Process.Start(new ProcessStartInfo("explorer.exe","/select,\""+s.CleanPath+"\""){UseShellExecute=true});
   }
  });
  btnRow.Children.Add(btnOpenLoc);
  loadServices();
  dialog.ShowDialog();
 }


 class BatchQueueItem {
  public string Name { get; set; }
  public string Release { get; set; }
  public string Status { get; set; }
  public AppEntry App { get; set; }
 }

 void BatchUninstall(){
  var selected=C<DataGrid>("AppsGrid").SelectedItems.Cast<AppEntry>().ToList();
  if(selected.Count<2){
   MessageBox.Show(window,"Vui lòng chọn từ 2 ứng dụng trở lên trong bảng (dùng phím Ctrl hoặc Shift khi chọn) để dùng tính năng Gỡ hàng loạt.","Hướng dẫn gỡ hàng loạt",MessageBoxButton.OK,MessageBoxImage.Information);
   return;
  }
  var safeList=selected.Where(a=>!a.Protected).ToList();
  if(safeList.Count==0){
   MessageBox.Show(window,"Tất cả các mục bạn chọn đều là thành phần bảo vệ của hệ thống; không thể gỡ bỏ.","Cảnh báo",MessageBoxButton.OK,MessageBoxImage.Warning);
   return;
  }
  var queueDialog=new Window{Title=I18n.T("⚡ Hàng đợi Gỡ cài đặt hàng loạt (Batch Uninstall) · Dọn Dẹp PC 1.2"),Owner=window,Width=780,Height=540,MinWidth=650,MinHeight=420,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13};
  queueDialog.SetResourceReference(Window.BackgroundProperty,"Canvas");queueDialog.SetResourceReference(Window.ForegroundProperty,"Ink");
  var root=new DockPanel{Margin=new Thickness(20)};queueDialog.Content=root;
  var top=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
  top.Children.Add(Label("HÀNG ĐỢI GỠ CÀI ĐẶT HÀNG LOẠT ("+safeList.Count+" ỨNG DỤNG)",18,true));
  top.Children.Add(Label("Hệ thống sẽ thực hiện gỡ bỏ tuần tự từng ứng dụng theo chuỗi. Bạn có thể bật tự động dọn sạch tàn dư rác sau mỗi lần gỡ.",12));
  var chkAutoClean=new CheckBox{Content="☑ Tự động quét và dọn sạch tàn dư rác sau khi gỡ từng phần mềm (Auto Deep Clean)",IsChecked=true,Margin=new Thickness(0,8,0,4)};
  chkAutoClean.SetResourceReference(Control.ForegroundProperty,"Ink");top.Children.Add(chkAutoClean);
  var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  var bBtns=new WrapPanel();bottom.Children.Add(bBtns);
  var qGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=false,RowHeight=Double.NaN,MinRowHeight=40};
  root.Children.Add(qGrid);
  AddColumn(qGrid,"ỨNG DỤNG","Name",260);
  AddColumn(qGrid,"PHIÊN BẢN","Release",110);
  AddColumn(qGrid,"TRẠNG THÁI HIỆN TẠI","Status",320);
  var items=safeList.Select(a=>new BatchQueueItem{Name=a.Name,Release=a.Release,Status="Đang chờ trong hàng đợi…",App=a}).ToList();
  qGrid.ItemsSource=items;
  bool isRunning=false;
  var btnStart=ActionButton("⚡ Bắt đầu gỡ hàng loạt",async()=>{
   if(isRunning)return;
   if(!Confirm("Bắt đầu quy trình gỡ cài đặt tự động cho "+safeList.Count+" ứng dụng?\nTrình gỡ có thể mở các cửa sổ xác nhận của phần mềm. Hãy lưu công việc đang làm trước."))return;
   isRunning=true;
   int doneCount=0;int cleanCount=0;long totalCleanBytes=0;
   for(int i=0;i<items.Count;i++){
    var item=items[i];
    item.Status="▶ Đang chạy trình gỡ cài đặt…";qGrid.Items.Refresh();
    try{
     var start=Engine.Uninstaller(item.App);
     int exit=await Task.Run(()=>{using(var proc=Process.Start(start)){if(proc==null)return -1;proc.WaitForExit();return proc.ExitCode;}});
     doneCount++;
     item.Status="✓ Đã hoàn tất gỡ cài đặt.";
     if(chkAutoClean.IsChecked==true&&!Engine.Present(item.App)){
      item.Status="Đang dọn tàn dư rác…";qGrid.Items.Refresh();
      try{
       var found=await Task.Run(()=>Engine.Scan(item.App));
       var cleanable=found.Where(x=>x.CanClean).ToList();
       foreach(var r in cleanable){try{Engine.Clean(item.App,r);cleanCount++;totalCleanBytes+=r.Bytes;}catch{}}
       if(cleanable.Count>0)item.Status="✓ Đã gỡ & dọn "+cleanable.Count+" mục tàn dư ("+Features.Size(cleanable.Sum(x=>x.Bytes))+")!";
      }catch{}
     }
    }catch(Exception ex){item.Status="⚠ Lỗi: "+ex.Message;}
    qGrid.Items.Refresh();
   }
   await Reload();
   MessageBox.Show(queueDialog,"ĐÃ HOÀN TẤT GỠ CÀI ĐẶT HÀNG LOẠT!\n\nThành công: "+doneCount+" / "+items.Count+" ứng dụng.\nTự động dọn rác: Đã dọn sạch "+cleanCount+" mục tàn dư ("+Features.Size(totalCleanBytes)+").","Kết quả",MessageBoxButton.OK,MessageBoxImage.Information);
   queueDialog.Close();
  });
  btnStart.Style=(Style)window.Resources["Primary"];
  bBtns.Children.Add(btnStart);
  bBtns.Children.Add(ActionButton("Đóng",()=>queueDialog.Close()));
  queueDialog.ShowDialog();
 }


 void OpenSetupMonitorDialog(){
  var dialog=new Window{
   Title=I18n.T("Giám sát & Theo dõi Cài đặt mới (Setup Monitor) · Dọn Dẹp PC 1.2"),
   Owner=window,Width=920,Height=650,MinWidth=750,MinHeight=500,
   WindowStartupLocation=WindowStartupLocation.CenterOwner,
   Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13
  };
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  var dock=new DockPanel{Margin=new Thickness(20)};dialog.Content=dock;

  var header=new StackPanel{Margin=new Thickness(0,0,0,14)};DockPanel.SetDock(header,Dock.Top);dock.Children.Add(header);
  header.Children.Add(Label(I18n.T("GIÁM SÁT CÀI ĐẶT MỚI & GỠ BỎ TRIỆT ĐỂ (SETUP MONITOR)"),19,true));
  header.Children.Add(Label(I18n.T("Tự động chụp ảnh trạng thái trước & sau khi cài đặt phần mềm. Ghi nhận chính xác 100% mọi tệp tin và khóa Registry mới phát sinh để gỡ bỏ sạch sẽ không còn một tàn dư nào."),12));

  var tabButtons=new WrapPanel{Margin=new Thickness(0,0,0,12)};header.Children.Add(tabButtons);
  var btnTab1=ActionButton("📸 "+I18n.T("Giám sát cài đặt mới"),null);
  var btnTab2=ActionButton("📦 "+I18n.T("Phần mềm đã giám sát"),null);
  tabButtons.Children.Add(btnTab1);tabButtons.Children.Add(btnTab2);

  var contentPanel=new Grid();dock.Children.Add(contentPanel);

  var monitorView=new StackPanel();contentPanel.Children.Add(monitorView);
  var listView=new DockPanel{Visibility=Visibility.Collapsed};contentPanel.Children.Add(listView);

  SnapshotSession activeSession=null;
  var card1=new StackPanel();
  card1.Children.Add(Label("1. BƯỚC 1: BẮT ĐẦU CHỤP ẢNH HỆ THỐNG",14,true));
  card1.Children.Add(Label("Ghi lại trạng thái thư mục Program Files, AppData và Registry hiện tại trước khi cài đặt. Quá trình diễn ra trong tích tắc.",12));
  var step1Status=Label("Trạng thái: Chưa bắt đầu",12);step1Status.SetResourceReference(TextBlock.ForegroundProperty,"Muted");
  var btnStart=ActionButton("▶ Bắt đầu chụp ảnh trước (Start Snapshot)",()=>{
   step1Status.Text="Đang ghi nhận ảnh hệ thống ban đầu…";
   activeSession=SetupMonitor.StartSession();
   step1Status.Text="✓ Đã chụp ảnh xong lúc "+DateTime.Now.ToString("HH:mm:ss")+". Bây giờ bạn hãy chạy file cài đặt phần mềm!";
   step1Status.SetResourceReference(TextBlock.ForegroundProperty,"Good");
  });
  card1.Children.Add(btnStart);card1.Children.Add(step1Status);
  monitorView.Children.Add(Card(card1));

  var card2=new StackPanel();
  card2.Children.Add(Label("2. BƯỚC 2: TIẾN HÀNH CÀI ĐẶT PHẦN MỀM",14,true));
  card2.Children.Add(Label("Hãy mở tệp cài đặt (Setup.exe / Installer.msi) của phần mềm bạn muốn cài đặt và hoàn tất quá trình cài đặt của phần mềm đó như bình thường.",12));
  monitorView.Children.Add(Card(card2));

  var card3=new StackPanel();
  card3.Children.Add(Label("3. BƯỚC 3: HOÀN TẤT VÀ TẠO NHẬT KÝ GIÁM SÁT",14,true));
  card3.Children.Add(Label("Nhập tên phần mềm và bấm nút hoàn tất để hệ thống đối chiếu diff và lưu nhật ký.",12));
  var txtName=new TextBox{Margin=new Thickness(0,0,0,8),Padding=new Thickness(8,6,8,6),MinWidth=250};
  txtName.Text="Phần mềm mới";
  card3.Children.Add(txtName);
  var step3Status=Label("Chưa tạo nhật ký",12);step3Status.SetResourceReference(TextBlock.ForegroundProperty,"Muted");
  var btnFinish=ActionButton("✔ Hoàn tất cài đặt & Tạo nhật ký",()=>{
   if(activeSession==null){MessageBox.Show(dialog,"Vui lòng thực hiện Bước 1 trước khi hoàn tất!","Chưa chụp ảnh");return;}
   step3Status.Text="Đang đối chiếu thay đổi hệ thống…";
   var rec=SetupMonitor.CompleteSession(activeSession,txtName.Text);
   activeSession=null;
   step3Status.Text="✓ Đã lưu nhật ký giám sát '"+rec.AppName+"': "+rec.CreatedFiles.Count+" tệp và "+rec.CreatedRegistryKeys.Count+" khóa Registry!";
   step3Status.SetResourceReference(TextBlock.ForegroundProperty,"Good");
   MessageBox.Show(dialog,"ĐÃ GIÁM SÁT THÀNH CÔNG!\n\nPhần mềm: "+rec.AppName+"\n• Tệp mới tạo: "+rec.CreatedFiles.Count+"\n• Thư mục mới: "+rec.CreatedFolders.Count+"\n• Khóa Registry mới: "+rec.CreatedRegistryKeys.Count+"\n\nNhật ký đã được lưu. Bạn có thể gỡ bỏ triệt để bất cứ lúc nào trong tab 'Phần mềm đã giám sát'!","Hoàn tất",MessageBoxButton.OK,MessageBoxImage.Information);
  });
  card3.Children.Add(btnFinish);card3.Children.Add(step3Status);
  monitorView.Children.Add(Card(card3));

  // Build Tab 2 (List View)
  var listBottom=new WrapPanel{Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(listBottom,Dock.Bottom);listView.Children.Add(listBottom);
  var snapGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=false,RowHeight=Double.NaN,MinRowHeight=42};
  listView.Children.Add(snapGrid);
  AddColumn(snapGrid,"TÊN PHẦN MỀM","AppName",220);
  AddColumn(snapGrid,"NGÀY GIÁM SÁT","CreatedAt",160);

  List<SnapshotRecord> records=new List<SnapshotRecord>();
  Action RefreshSnapshots=()=>{
   records=SetupMonitor.LoadAllRecords();
   snapGrid.ItemsSource=null;snapGrid.ItemsSource=records;
  };

  btnTab1.Click+=(s,e)=>{monitorView.Visibility=Visibility.Visible;listView.Visibility=Visibility.Collapsed;};
  btnTab2.Click+=(s,e)=>{monitorView.Visibility=Visibility.Collapsed;listView.Visibility=Visibility.Visible;RefreshSnapshots();};

  listBottom.Children.Add(ActionButton("🗑️ Gỡ bỏ triệt để 100%",()=>{
   var rec=snapGrid.SelectedItem as SnapshotRecord;
   if(rec==null){MessageBox.Show(dialog,"Vui lòng chọn 1 phần mềm trong danh sách để gỡ bỏ!","Thông báo");return;}
   if(!Confirm("Bạn có chắc chắn muốn gỡ bỏ hoàn toàn '"+rec.AppName+"'?\n\nHệ thống sẽ:\n• Chuyển "+rec.CreatedFiles.Count+" tệp và "+rec.CreatedFolders.Count+" thư mục vào Thùng rác (Recycle Bin)\n• Xóa bỏ "+(rec.CreatedRegistryKeys.Count+rec.CreatedRegistryValues.Count)+" mục Registry đã ghi nhận\n\nToàn bộ tàn dư sẽ được dọn sạch 100%."))return;
   int fDel,rDel;List<string> errs;
   SetupMonitor.UninstallMonitoredApp(rec,out fDel,out rDel,out errs);
   RefreshSnapshots();
   MessageBox.Show(dialog,"ĐÃ GỠ BỎ TRIỆT ĐỂ!\n\n• Tệp đã dọn: "+fDel+"\n• Mục Registry đã dọn: "+rDel+(errs.Count>0?"\n• Mục cần quyền Administrator: "+errs.Count:""),"Hoàn tất",MessageBoxButton.OK,MessageBoxImage.Information);
  }));

  listBottom.Children.Add(ActionButton("📄 Xem chi tiết tệp & Registry",()=>{
   var rec=snapGrid.SelectedItem as SnapshotRecord;
   if(rec==null){MessageBox.Show(dialog,"Vui lòng chọn 1 phần mềm để xem chi tiết!","Thông báo");return;}
   var sb=new StringBuilder();
   sb.AppendLine("CHI TIẾT NHẬT KÝ GIÁM SÁT: "+rec.AppName);
   sb.AppendLine("Thời gian tạo: "+rec.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"));
   sb.AppendLine("\n--- DANH SÁCH THƯ MỤC MỚI ("+rec.CreatedFolders.Count+") ---");
   foreach(var d in rec.CreatedFolders)sb.AppendLine(d);
   sb.AppendLine("\n--- DANH SÁCH TỆP MỚI ("+rec.CreatedFiles.Count+") ---");
   foreach(var f in rec.CreatedFiles)sb.AppendLine(f);
   sb.AppendLine("\n--- KHÓA REGISTRY MỚI ("+rec.CreatedRegistryKeys.Count+") ---");
   foreach(var k in rec.CreatedRegistryKeys)sb.AppendLine(k);
   ShowLongReport("Chi tiết giám sát · "+rec.AppName,sb.ToString());
  }));

  listBottom.Children.Add(ActionButton("❌ Xóa bản ghi",()=>{
   var rec=snapGrid.SelectedItem as SnapshotRecord;
   if(rec==null)return;
   if(Confirm("Xóa bản ghi nhật ký này khỏi danh sách? (Phần mềm và tệp trên máy tính sẽ không bị xóa)")) {
    SetupMonitor.DeleteRecord(rec);
    RefreshSnapshots();
   }
  }));

  dialog.ShowDialog();
 }

 void OpenRegistryCleanerDialog(){
  var dialog=new Window{Title=I18n.T("Dọn dẹp Registry thông minh & An toàn · Dọn Dẹp PC 1.2"),Owner=window,Width=960,Height=650,MinWidth=750,MinHeight=480,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13};
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  var root=new DockPanel{Margin=new Thickness(20)};dialog.Content=root;
  var header=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(Label("DỌN DẸP REGISTRY HỆ THỐNG AN TOÀN",19,true));
  header.Children.Add(Label("Tự động quét và loại bỏ các mục đăng ký lỗi thời: Trình gỡ cài đặt mồ côi, Lối tắt khởi động đã xóa, MuiCache rác, App Paths hỏng.\n★ Tự động sao lưu khóa Registry (.reg) vào thư mục tạm trước khi sửa lỗi để có thể khôi phục 100%.",12));
  var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  var btnRow=new WrapPanel();bottom.Children.Add(btnRow);
  var statusText=Label("Bấm 'Quét Registry' để bắt đầu kiểm tra lỗi hệ thống.",12);
  statusText.Margin=new Thickness(0,0,0,6);
  bottom.Children.Insert(0,statusText);
  var grid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=false,RowHeight=Double.NaN,MinRowHeight=42};
  root.Children.Add(grid);
  AddChecks(grid,"Selected");
  AddColumn(grid,"VẤN ĐỀ","Category",180);
  AddColumn(grid,"VỊ TRÍ REGISTRY","DisplayKey",360);
  AddColumn(grid,"CHI TIẾT LỖI","Reason",320);
  List<RegistryIssue> currentIssues=new List<RegistryIssue>();
  var btnScan=ActionButton("🔍 1. Quét lỗi Registry",async()=>{
   statusText.Text="Đang quét toàn diện Registry hệ thống…";btnRow.IsEnabled=false;
   try{
    currentIssues=await Task.Run(()=>RegistryTools.ScanIssues());
    grid.ItemsSource=null;grid.ItemsSource=currentIssues;
    statusText.Text="Đã phát hiện "+currentIssues.Count+" mục đăng ký Registry lỗi thời hoặc mồ côi có thể xử lý an toàn.";
   }finally{btnRow.IsEnabled=true;}
  });
  btnScan.Style=(Style)window.Resources["Primary"];btnRow.Children.Add(btnScan);
  var btnFix=ActionButton("🧹 2. Sửa các mục đã chọn",()=>{
   var chosen=currentIssues.Where(x=>x.Selected).ToList();
   if(chosen.Count==0){MessageBox.Show(dialog,"Vui lòng đánh dấu chọn ít nhất 1 mục lỗi để sửa.","Thông báo");return;}
   if(!Confirm("Sửa chữa và dọn "+chosen.Count+" mục Registry đã chọn?\n\nHệ thống sẽ tự động xuất một bản sao lưu (.reg) trước khi thực hiện để đảm bảo an toàn tuyệt đối."))return;
   string backupFile;List<string> errors;
   int cleaned=RegistryTools.CleanIssues(chosen,out backupFile,out errors);
   foreach(var item in chosen)currentIssues.Remove(item);
   grid.ItemsSource=null;grid.ItemsSource=currentIssues;
   statusText.Text="Đã dọn thành công "+cleaned+" mục Registry. Bản sao lưu được lưu an toàn tại: "+backupFile;
   MessageBox.Show(dialog,"ĐÃ HOÀN TẤT DỌN DẸP REGISTRY!\n\nSố mục đã xử lý: "+cleaned+" / "+chosen.Count+"\n\nBản sao lưu hoàn tác (.reg):\n"+backupFile+(errors.Count>0?"\n\n(Có "+errors.Count+" mục cần quyền Administrator)":""),"Thành công",MessageBoxButton.OK,MessageBoxImage.Information);
  });
  btnRow.Children.Add(btnFix);
  btnRow.Children.Add(ActionButton("Chọn tất cả",()=>{foreach(var item in currentIssues)item.Selected=true;grid.Items.Refresh();}));
  btnRow.Children.Add(ActionButton("Bỏ chọn",()=>{foreach(var item in currentIssues)item.Selected=false;grid.Items.Refresh();}));
  btnRow.Children.Add(ActionButton("📁 Mở thư mục sao lưu",()=>{
   string dir=Path.Combine(Path.GetTempPath(),"DonDepPC_Backup");
   Directory.CreateDirectory(dir);
   Process.Start(new ProcessStartInfo("explorer.exe",dir){UseShellExecute=true});
  }));
  dialog.ShowDialog();
 }

 void OpenBrowserExtensionsDialog(){
  var dialog=new Window{Title=I18n.T("Quản lý Tiện ích mở rộng Trình duyệt (Browser Extensions) · Dọn Dẹp PC 1.2"),Owner=window,Width=920,Height=620,MinWidth=750,MinHeight=460,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13};
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  var root=new DockPanel{Margin=new Thickness(20)};dialog.Content=root;
  var header=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(Label("QUẢN LÝ TIỆN ÍCH MỞ RỘNG TRÌNH DUYỆT",19,true));
  header.Children.Add(Label("Xem và gỡ bỏ các extension/add-on cài cắm trên Google Chrome, Microsoft Edge, Brave, Cốc Cốc giúp tăng tốc duyệt web và bảo mật.",12));
  var filterRow=new Grid{Margin=new Thickness(0,10,0,0)};
  filterRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(260)});
  filterRow.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(190)});
  filterRow.ColumnDefinitions.Add(new ColumnDefinition());
  header.Children.Add(filterRow);
  var searchBox=new TextBox{Margin=new Thickness(0,0,10,0)};Grid.SetColumn(searchBox,0);filterRow.Children.Add(searchBox);
  var browserCombo=new ComboBox{ItemsSource=new[]{"Tất cả trình duyệt","Google Chrome","Microsoft Edge","Brave Browser","Cốc Cốc"},SelectedIndex=0};
  Grid.SetColumn(browserCombo,1);filterRow.Children.Add(browserCombo);
  var statusText=Label("Đang quét tiện ích…",12);statusText.VerticalAlignment=VerticalAlignment.Center;statusText.Margin=new Thickness(12,0,0,0);Grid.SetColumn(statusText,2);filterRow.Children.Add(statusText);
  var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  var btnRow=new WrapPanel();bottom.Children.Add(btnRow);
  var grid=new DataGrid{IsReadOnly=true,SelectionMode=DataGridSelectionMode.Single,AutoGenerateColumns=false,RowHeight=Double.NaN,MinRowHeight=42};
  root.Children.Add(grid);
  AddColumn(grid,"TRÌNH DUYỆT","Browser",140);
  AddColumn(grid,"TÊN TIỆN ÍCH","Name",240);
  AddColumn(grid,"PHIÊN BẢN","Version",90);
  AddColumn(grid,"ID TIỆN ÍCH","Id",160);
  AddColumn(grid,"MÔ TẢ","Description",280);
  List<BrowserExtensionItem> allExts=new List<BrowserExtensionItem>();
  Action refreshList=()=>{
   string q=(searchBox.Text??"").ToLowerInvariant().Trim();
   int b=browserCombo.SelectedIndex;
   var filtered=allExts.Where(x=>{
    if(!String.IsNullOrEmpty(q)&&!(x.Name??"").ToLowerInvariant().Contains(q)&&!(x.Description??"").ToLowerInvariant().Contains(q))return false;
    if(b==1&&x.Browser!="Google Chrome")return false;
    if(b==2&&x.Browser!="Microsoft Edge")return false;
    if(b==3&&x.Browser!="Brave Browser")return false;
    if(b==4&&x.Browser!="Cốc Cốc")return false;
    return true;
   }).ToList();
   grid.ItemsSource=filtered;
   statusText.Text="Hiển thị "+filtered.Count+" / "+allExts.Count+" tiện ích mở rộng.";
  };
  Action loadExts=()=>{allExts=BrowserExtensionTools.ScanExtensions();refreshList();};
  searchBox.TextChanged+=(s,e)=>refreshList();
  browserCombo.SelectionChanged+=(s,e)=>refreshList();
  var btnRemove=ActionButton("🗑️ Gỡ bỏ tiện ích",()=>{
   var item=grid.SelectedItem as BrowserExtensionItem;
   if(item==null){MessageBox.Show(dialog,"Vui lòng chọn một tiện ích trong danh sách trước.","Thông báo");return;}
   if(!Confirm("Gỡ bỏ tiện ích '"+item.Name+"' khỏi "+item.Browser+"?\n\nThư mục tiện ích sẽ được chuyển vào Thùng rác để đảm bảo an toàn."))return;
   string err;
   if(BrowserExtensionTools.RemoveExtension(item,out err)){
    allExts.Remove(item);refreshList();
    MessageBox.Show(dialog,"Đã gỡ bỏ tiện ích thành công và chuyển vào Thùng rác!","Thành công",MessageBoxButton.OK,MessageBoxImage.Information);
   }else{MessageBox.Show(dialog,"Không thể gỡ bỏ: "+err,"Lỗi",MessageBoxButton.OK,MessageBoxImage.Warning);}
  });
  btnRow.Children.Add(btnRemove);
  btnRow.Children.Add(ActionButton("📁 Mở thư mục tiện ích",()=>{
   var item=grid.SelectedItem as BrowserExtensionItem;
   if(item!=null&&Directory.Exists(item.InstallPath))Process.Start(new ProcessStartInfo("explorer.exe",item.InstallPath){UseShellExecute=true});
  }));
  btnRow.Children.Add(ActionButton("🔄 Làm mới",loadExts));
  loadExts();
  dialog.ShowDialog();
 }


 void OpenSqliteOptimizerDialog(){
  var dialog=new Window{
   Title=I18n.T("Tối ưu hóa Database Trình duyệt (SQLite Vacuum) · Dọn Dẹp PC 1.2"),
   Owner=window,Width=880,Height=580,MinWidth=700,MinHeight=450,
   WindowStartupLocation=WindowStartupLocation.CenterOwner,
   Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13
  };
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  var root=new DockPanel{Margin=new Thickness(20)};dialog.Content=root;
  var header=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(Label(I18n.T("TỐI ƯU HÓA & THU GỌN DATABASE TRÌNH DUYỆT (SQLITE VACUUM)"),19,true));
  header.Children.Add(Label(I18n.T("Chạy thuật toán VACUUM và REINDEX trên các tệp cơ sở dữ liệu (History, Cookies, Web Data, Places) của Chrome, Edge, Cốc Cốc, Brave, Firefox để dọn sạch phân mảnh, thu nhỏ dung lượng và tăng tốc mở trình duyệt.\n★ Khuyến nghị: Đóng các trình duyệt web trước khi tối ưu để tệp không bị khóa."),12));

  var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  var btnRow=new WrapPanel();bottom.Children.Add(btnRow);
  var statusText=Label(I18n.T("Sẵn sàng phân tích cơ sở dữ liệu trình duyệt."),12);
  statusText.Margin=new Thickness(0,0,0,6);bottom.Children.Insert(0,statusText);

  var lb=new ListBox{MinHeight=200};
  lb.SetResourceReference(Control.BackgroundProperty,"Panel");
  lb.SetResourceReference(Control.ForegroundProperty,"Ink");
  root.Children.Add(lb);

  var dbs=SqliteOptimizer.FindBrowserDatabases();
  foreach(var p in dbs){
   try{
    long sz=new FileInfo(p).Length;
    lb.Items.Add(Path.GetFileName(Path.GetDirectoryName(p)) + "\\" + Path.GetFileName(p) + " (" + Features.Size(sz) + ") — " + p);
   }catch{
    lb.Items.Add(p);
   }
  }
  statusText.Text=I18n.T("Đã tìm thấy ") + dbs.Count + I18n.T(" tệp database SQLite trình duyệt.");

  bool isBusy=false;
  btnRow.Children.Add(ActionButton(I18n.T("⚡ Bắt đầu Tối ưu hóa (Vacuum)"),async()=>{
   if(isBusy)return;
   isBusy=true;
   statusText.Text=I18n.T("Đang tiến hành tối ưu hóa các cơ sở dữ liệu...");
   int count=0;long freedTotal=0;
   await Task.Run(()=>{
    foreach(var p in dbs){
     long freed;
     if(SqliteOptimizer.Vacuum(p,out freed)){
      count++;freedTotal+=freed;
     }
    }
   });
   statusText.Text=I18n.T("Hoàn tất! Đã tối ưu thành công ") + count + " / " + dbs.Count + I18n.T(" database, thu gọn được ") + Features.Size(freedTotal) + ".";
   MessageBox.Show(dialog, statusText.Text, I18n.T("Tối ưu Database SQLite"), MessageBoxButton.OK, MessageBoxImage.Information);
   isBusy=false;
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Đóng"),()=>dialog.Close()));

  dialog.ShowDialog();
 }

 void OpenFileShredderDialog(){
  var dialog=new Window{
   Title=I18n.T("Hủy tệp & Thư mục an toàn (File Shredder) · Dọn Dẹp PC 1.2"),
   Owner=window,Width=880,Height=600,MinWidth=720,MinHeight=480,
   WindowStartupLocation=WindowStartupLocation.CenterOwner,
   Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13
  };
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");
  dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  var root=new DockPanel{Margin=new Thickness(20)};dialog.Content=root;
  var header=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(Label(I18n.T("HỦY TỆP AN TOÀN & XÓA VĨNH VIỄN (SECURE FILE SHREDDER)"),19,true));
  header.Children.Add(Label(I18n.T("Ghi đè bit dữ liệu nhiều lượt (Zero, One, Random) trước khi xóa nhằm ngăn chặn hoàn toàn khả năng phục hồi dữ liệu bằng phần mềm cứu hộ."),12));

  var warnCard=new Border{Background=new SolidColorBrush(Color.FromArgb(30,230,70,50)),CornerRadius=new CornerRadius(10),Padding=new Thickness(12),Margin=new Thickness(0,8,0,4)};
  warnCard.Child=Label(I18n.T("⚡ CẢNH BÁO: Dữ liệu sau khi hủy sẽ bị XÓA VĨNH VIỄN và KHÔNG THỂ KHÔI PHỤC kể cả bằng phần mềm cứu dữ liệu chuyên nghiệp. Hãy kiểm tra kỹ đường dẫn tệp trước khi thực hiện."),12,true);
  header.Children.Add(warnCard);

  var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  
  var optRow=new WrapPanel{Margin=new Thickness(0,0,0,8)};bottom.Children.Add(optRow);
  var rb1Pass=new RadioButton{Content=I18n.T("Nhanh (1 lượt - Ghi đè Zero)"),Margin=new Thickness(0,0,16,0)};
  rb1Pass.SetResourceReference(Control.ForegroundProperty,"Ink");
  var rb3Pass=new RadioButton{Content=I18n.T("Tiêu chuẩn DoD 5220.22-M (3 lượt - Zero, One, Random)"),IsChecked=true,Margin=new Thickness(0,0,16,0)};
  rb3Pass.SetResourceReference(Control.ForegroundProperty,"Ink");
  optRow.Children.Add(rb1Pass);optRow.Children.Add(rb3Pass);

  var statusText=Label(I18n.T("Danh sách trống. Thêm tệp hoặc thư mục cần hủy vào bảng."),12);
  statusText.Margin=new Thickness(0,0,0,8);bottom.Children.Add(statusText);

  var btnRow=new WrapPanel();bottom.Children.Add(btnRow);

  var itemsList=new List<string>();
  var lb=new ListBox{MinHeight=180};
  lb.SetResourceReference(Control.BackgroundProperty,"Panel");
  lb.SetResourceReference(Control.ForegroundProperty,"Ink");
  root.Children.Add(lb);

  Action updateList=()=>{
   lb.Items.Clear();
   foreach(var s in itemsList)lb.Items.Add(s);
   statusText.Text=I18n.T("Đã chọn ") + itemsList.Count + I18n.T(" mục cần hủy.");
  };

  var topBtns=new WrapPanel{Margin=new Thickness(0,6,0,8)};
  header.Children.Add(topBtns);
  topBtns.Children.Add(ActionButton(I18n.T("📄 Thêm tệp..."),()=>{
   var ofd=new Microsoft.Win32.OpenFileDialog{Multiselect=true,Title=I18n.T("Chọn tệp cần hủy vĩnh viễn")};
   if(ofd.ShowDialog(dialog)==true){
    foreach(var f in ofd.FileNames)if(!itemsList.Contains(f)&&!FileShredder.IsProtected(f))itemsList.Add(f);
    updateList();
   }
  }));
  topBtns.Children.Add(ActionButton(I18n.T("📁 Thêm thư mục..."),()=>{
   using(var fbd=new System.Windows.Forms.FolderBrowserDialog{Description=I18n.T("Chọn thư mục cần hủy vĩnh viễn")}){
    if(fbd.ShowDialog()==System.Windows.Forms.DialogResult.OK){
     string sel=fbd.SelectedPath;
     if(!itemsList.Contains(sel)&&!FileShredder.IsProtected(sel))itemsList.Add(sel);
     updateList();
    }
   }
  }));
  topBtns.Children.Add(ActionButton(I18n.T("❌ Xóa mục đã chọn"),()=>{
   if(lb.SelectedItem!=null){itemsList.Remove(lb.SelectedItem.ToString());updateList();}
  }));
  topBtns.Children.Add(ActionButton(I18n.T("🧹 Xóa toàn bộ danh sách"),()=>{
   itemsList.Clear();updateList();
  }));

  bool isShredding=false;
  btnRow.Children.Add(ActionButton(I18n.T("🔥 TIẾN HÀNH HỦY VĨNH VIỄN"),async()=>{
   if(isShredding)return;
   if(itemsList.Count==0){
    MessageBox.Show(dialog,I18n.T("Vui lòng thêm ít nhất một tệp hoặc thư mục cần hủy."),I18n.T("Thông báo"),MessageBoxButton.OK,MessageBoxImage.Warning);
    return;
   }
   if(MessageBox.Show(dialog,I18n.T("BẠN CÓ CHẮC CHẮN MUỐN HỦY VĨNH VIỄN ") + itemsList.Count + I18n.T(" MỤC ĐÃ CHỌN?\n\nDữ liệu sẽ bị ghi đè và XÓA HOÀN TOÀN. Thao tác này KHÔNG THỂ HOÀN TÁC!"),I18n.T("Xác nhận hủy dữ liệu"),MessageBoxButton.YesNo,MessageBoxImage.Stop)!=MessageBoxResult.Yes)return;

   isShredding=true;
   int passes=rb1Pass.IsChecked==true?1:3;
   statusText.Text=I18n.T("Đang tiến hành hủy an toàn dữ liệu...");
   int done=0;
   await Task.Run(()=>{
    foreach(var path in itemsList){
     try{
      if(Directory.Exists(path))FileShredder.ShredDirectory(path,passes);
      else if(File.Exists(path))FileShredder.ShredFile(path,passes);
      done++;
     }catch{}
    }
   });
   itemsList.Clear();updateList();
   statusText.Text=I18n.T("ĐÃ HỦY VĨNH VIỄN THÀNH CÔNG ") + done + I18n.T(" MỤC!");
   MessageBox.Show(dialog,statusText.Text,I18n.T("Hoàn tất hủy tệp"),MessageBoxButton.OK,MessageBoxImage.Information);
   isShredding=false;
  }));
  btnRow.Children.Add(ActionButton(I18n.T("Đóng"),()=>dialog.Close()));

  dialog.ShowDialog();
 }

 void OpenUwpManagerDialog(){
  var dialog=new Window{Title=I18n.T("Gỡ ứng dụng mặc định Windows (UWP Bloatware) · Dọn Dẹp PC 1.2"),Owner=window,Width=960,Height=650,MinWidth=750,MinHeight=480,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources,FontFamily=window.FontFamily,FontSize=13};
  dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");dialog.SetResourceReference(Window.ForegroundProperty,"Ink");EnableEscapeClose(dialog);
  
  var root=new DockPanel{Margin=new Thickness(20)};dialog.Content=root;
  var header=new StackPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(Label("QUẢN LÝ & GỠ ỨNG DỤNG MẶC ĐỊNH WINDOWS (UWP BLOATWARE)",19,true));
  header.Children.Add(Label("Gỡ bỏ tận gốc các ứng dụng Universal cài sẵn không cần thiết (Xbox, Cortana, Bản đồ, Tin tức, Thời tiết, Dev Home,...) để giải phóng bộ nhớ và tăng tốc máy tính.\n★ Các thành phần cốt lõi của hệ thống (như Microsoft Store) được bảo vệ tuyệt đối.",12));

  var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
  var btnRow=new WrapPanel();bottom.Children.Add(btnRow);
  var statusText=Label("Đang tải danh sách ứng dụng UWP đã cài đặt…",12);
  statusText.Margin=new Thickness(0,0,0,6);
  bottom.Children.Insert(0,statusText);

  var grid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=false,RowHeight=Double.NaN,MinRowHeight=40};
  root.Children.Add(grid);
  AddChecks(grid,"Selected","CanRemoveUwp");
  AddColumn(grid,"TÊN ỨNG DỤNG","Name",280);
  AddColumn(grid,"NHÀ PHÁT HÀNH","Vendor",180);
  AddColumn(grid,"PHIÊN BẢN","Release",130);
  AddColumn(grid,"DUNG LƯỢNG","SizeLabel",110);
  AddColumn(grid,"TRẠNG THÁI","UwpStatus",140);

  List<AppEntry> uwpApps=new List<AppEntry>();
  Action loadUwp=()=>{
   btnRow.IsEnabled=false;statusText.Text="Đang kiểm tra các gói Appx/UWP đã cài đặt…";
   Task.Run(()=>{
    return UwpManager.GetInstalledApps(true);
   }).ContinueWith(t=>{
    window.Dispatcher.Invoke(()=>{
     uwpApps=t.Result;
     grid.ItemsSource=null;grid.ItemsSource=uwpApps;
     statusText.Text="Đã tìm thấy "+uwpApps.Count+" ứng dụng UWP trên máy ("+uwpApps.Count(x=>!UwpManager.IsProtected(x.PackageFullName))+" ứng dụng có thể gỡ bỏ an toàn).";
     btnRow.IsEnabled=true;
    });
   });
  };
  loadUwp();

  var btnRemove=ActionButton("🗑️ Gỡ các ứng dụng đã chọn",async()=>{
   var chosen=uwpApps.Where(x=>x.Selected&&!UwpManager.IsProtected(x.PackageFullName)).ToList();
   if(chosen.Count==0){MessageBox.Show(dialog,"Vui lòng chọn ít nhất 1 ứng dụng có thể gỡ bỏ.","Thông báo");return;}
   if(!Confirm("Gỡ bỏ hoàn toàn "+chosen.Count+" ứng dụng Store/UWP đã chọn khỏi tài khoản Windows của bạn?\n\n"+String.Join("\n",chosen.Take(6).Select(x=>"• "+x.Name))))return;
   btnRow.IsEnabled=false;
   try {
     int done=0,failed=0;
     foreach(var app in chosen){
      statusText.Text="Đang gỡ bỏ: "+app.Name+"…";
      bool ok=await Task.Run(()=>UwpManager.RemovePackage(app.PackageFullName));
      if(ok){done++;uwpApps.Remove(app);}else failed++;
     }
     grid.ItemsSource=null;grid.ItemsSource=uwpApps;
     statusText.Text="Đã gỡ bỏ thành công "+done+" ứng dụng ("+failed+" thất bại).";
     try{System.Media.SystemSounds.Asterisk.Play();}catch{}
     MessageBox.Show(dialog,"Đã gỡ bỏ thành công "+done+" ứng dụng UWP!","Hoàn tất",MessageBoxButton.OK,MessageBoxImage.Information);
     Engine.InvalidateAppCache();
   } catch(Exception ex) {
     Error(ex);
   } finally {
     btnRow.IsEnabled=true;
   }
  });
  btnRemove.Style=(Style)window.Resources["Primary"];btnRow.Children.Add(btnRemove);

  btnRow.Children.Add(ActionButton("Chọn tất cả có thể gỡ",()=>{
   foreach(var item in uwpApps)item.Selected=!UwpManager.IsProtected(item.PackageFullName);
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton("Bỏ chọn",()=>{
   foreach(var item in uwpApps)item.Selected=false;
   grid.Items.Refresh();
  }));
  btnRow.Children.Add(ActionButton("Làm mới",loadUwp));

  dialog.ShowDialog();
 }

}
