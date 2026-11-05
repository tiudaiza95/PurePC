using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

partial class DesktopApp {
 Window window; List<AppEntry> apps=new List<AppEntry>();List<Remnant> remnants=new List<Remnant>();List<FileItem> files=new List<FileItem>();AppEntry target;bool busy;string page="Apps";CancellationTokenSource cancellation;
 T C<T>(string name) where T:class{return window.FindName(name) as T;}
 void Text(string name,string text){C<TextBlock>(name).Text=text;}
 void Click(string name,Action action){C<Button>(name).Click+=(s,e)=>{try{action();}catch(Exception ex){Error(ex);}};}
 void AsyncClick(string name,Func<Task> action){C<Button>(name).Click+=async(s,e)=>await Work(action);}
 void Error(Exception ex){lastState="●  Có lỗi";Text("Status",ex.Message);MessageBox.Show(window,ex.Message,"Dọn Dẹp PC",MessageBoxButton.OK,MessageBoxImage.Information);}
 bool Confirm(string message){return MessageBox.Show(window,message,"Dọn Dẹp PC · Xác nhận",MessageBoxButton.OKCancel,MessageBoxImage.Question)==MessageBoxResult.OK;}
 public Window Build(){
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SachGon.MainWindow.xaml"))window=(Window)XamlReader.Load(stream);
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SachGon.Icon.png")){var image=new BitmapImage();image.BeginInit();image.StreamSource=stream;image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();C<Image>("BrandImage").Source=image;window.Icon=image;}
  foreach(string nav in new[]{"Apps","Temp","Large","Startup","History"}){string name=nav;C<RadioButton>("Nav"+nav).Checked+=(s,e)=>Navigate(name);}
  C<TextBox>("Search").TextChanged+=(s,e)=>Filter();
  C<DataGrid>("AppsGrid").SelectionChanged+=(s,e)=>{if(busy)return;var a=C<DataGrid>("AppsGrid").SelectedItem as AppEntry;if(a==null)return;target=a;remnants.Clear();BindRemnants();Text("SelectedApp",a.Name);State();};
  AsyncClick("RefreshApps",async()=>{await Reload();Text("Status","Danh sách đã cập nhật. Không xóa ứng dụng khi lọc dòng trùng.");});
  AsyncClick("Uninstall",UninstallSelected);
  AsyncClick("ScanRemnants",async()=>{if(target==null)return;remnants=await Task.Run(()=>Engine.Scan(target));BindRemnants();Text("Status",remnants.Count==0?"Không thấy phần dư trong phạm vi quét. Không đồng nghĩa đã sạch 100%.":"Tìm thấy "+remnants.Count+" mục. Hãy kiểm tra đường dẫn và chọn trước khi dọn.");});
  AsyncClick("CleanRemnants",async()=>{
   C<DataGrid>("RemnantsGrid").CommitEdit();var chosen=remnants.Where(x=>x.Selected && x.CanClean).ToArray();if(chosen.Length==0){Text("Status","Chưa chọn mục phần dư nào.");return;}
   if(!Confirm("Dọn "+chosen.Length+" mục đã chọn?\nCó thể bao gồm dữ liệu cá nhân và cấu hình. Registry chưa đủ bằng chứng chỉ được xem. Thư mục gửi vào Thùng rác.\nNếu Windows đề nghị xóa vĩnh viễn, hãy Hủy để giữ khả năng khôi phục."))return;
   int done=0;var errors=new List<string>();foreach(var item in chosen)try{Engine.Clean(target,item);remnants.Remove(item);done++;}catch(Exception ex){errors.Add(item.DisplayPath+": "+ex.Message);}
   BindRemnants();Text("Status","Đã dọn "+done+" mục; giữ lại "+errors.Count+" mục.");if(errors.Count>0)MessageBox.Show(window,String.Join("\n\n",errors.Take(8)),"Mục được giữ lại");await Task.FromResult(0);
  });
  AsyncClick("ScanFiles",ScanFilesPro);
  Click("CancelScan",()=>{if(cancellation!=null)cancellation.Cancel();});
  Click("SelectFiles",()=>{bool choose=files.Any(x=>!x.Selected);foreach(var f in files)f.Selected=choose;C<DataGrid>("FilesGrid").Items.Refresh();C<Button>("SelectFiles").Content=choose?"Bỏ chọn tất cả":"Chọn tất cả";});
  AsyncClick("CleanFiles",CleanFilesPro);
  Click("OpenFileLocation",()=>{var f=C<DataGrid>("FilesGrid").SelectedItem as FileItem;if(f==null){Text("Status","Chọn một dòng tệp để mở vị trí.");return;}if(!File.Exists(f.Path))throw new Exception("Tệp không còn tồn tại.");Process.Start(new ProcessStartInfo("explorer.exe","/select,\""+f.Path+"\""){UseShellExecute=true});});
  Click("RefreshStartup",LoadStartup);
  Click("WindowsStartup",()=>Process.Start(new ProcessStartInfo("ms-settings:startupapps"){UseShellExecute=true}));
  AsyncClick("ToggleStartup",async()=>{
   var item=C<DataGrid>("StartupGrid").SelectedItem as StartupItem;if(item==null){Text("Status","Chọn một mục khởi động trước.");return;}
   if(!Confirm((item.Disabled?"Khôi phục":"Tắt")+" mục Run của "+item.Name+"?\nThao tác chỉ thay đổi đăng ký khởi động của tài khoản hiện tại. Windows có thể quản lý thêm quyền khởi động trong Cài đặt."))return;
   Features.Toggle(item);LoadStartup();Text("Status","Đã cập nhật mục Run. Bạn có thể bật lại mục do Dọn Dẹp PC đã tắt.");await Task.FromResult(0);
  });
  Click("OpenBackups",()=>{string dir=Path.Combine(Engine.Data,"Registry-backups");Directory.CreateDirectory(dir);Process.Start(new ProcessStartInfo(dir){UseShellExecute=true});});
  Click("OpenRecycle",()=>Process.Start(new ProcessStartInfo("explorer.exe","shell:RecycleBinFolder"){UseShellExecute=true}));
  Click("OpenLog",()=>{string path=Path.Combine(Engine.Data,"activity.log");if(!File.Exists(path))Engine.Log("Nhật ký được tạo.");Process.Start(new ProcessStartInfo("notepad.exe","\""+path+"\""){UseShellExecute=true});});
  Click("RefreshHistory",LoadHistory);
  window.Loaded+=async(s,e)=>{
   NavigatePro("Health");
   if(Environment.GetCommandLineArgs().Contains("--drivers")){OpenV04("Drivers",BuildDriverPage);C<RadioButton>("NavDrivers").IsChecked=true;}
   await Work(async()=>{
    await Reload();
    target=Engine.Recall();
    if(target!=null){
     Text("SelectedApp","Lần gỡ gần nhất: "+target.Name);
     Text("SelectedHint","Có thể tiếp tục quét phần dư sau khi khởi động lại.");
    }
    Text("Status","Sẵn sàng · Dữ liệu Registry tại máy; không gửi danh sách lên mạng.");
   });
  };
  window.Closing+=(s,e)=>{if(busy){e.Cancel=true;if(cancellation!=null)cancellation.Cancel();Text("Status","Đang hoàn tất thao tác. Hãy đóng lại sau khi xử lý xong.");}};
  foreach(string gridName in new[]{"AppsGrid","RemnantsGrid","FilesGrid","StartupGrid"})foreach(var col in C<DataGrid>(gridName).Columns.OfType<DataGridTextColumn>()){
   var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis));style.Setters.Add(new Setter(TextBlock.ToolTipProperty,new System.Windows.Data.Binding("Text"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self)}));col.ElementStyle=style;
  }
  BuildPro();
  Click("ToggleLang",ToggleLanguage);
  window.PreviewKeyDown+=(s,e)=>{
   if(e.Key==System.Windows.Input.Key.F5){
    if(page=="Health"&&onHealthPerfTick!=null)onHealthPerfTick();
    else if(page=="Apps")C<Button>("RefreshApps").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    else if(page=="Startup")LoadStartup();
    else if(page=="History")LoadHistory();
    else if(page=="Tools")BuildTools();
    else if(page=="Deep")BuildDeepPage();
    else if(page=="DiskTools")BuildDiskTools();
    else if(page=="Drivers")BuildDriverPage();
    e.Handled=true;
   }
  };
  I18n.Init();ApplyLanguage();State();return window;
 }
 public void Preview(string folder){PreviewPro(folder);}
 async Task Reload(){apps=await Task.Run(()=>Engine.Apps());Text("AppCount",apps.Count.ToString());Text("DuplicateCount",Engine.DuplicateCount.ToString());try{var drive=new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));Text("DiskFree",Features.Size(drive.AvailableFreeSpace));Text("DriveLabel","DUNG LƯỢNG TRỐNG · "+drive.Name.TrimEnd('\\'));}catch{Text("DiskFree","—");}Filter();}
 void Filter(){FilterPro();}
 void BindRemnants(){C<DataGrid>("RemnantsGrid").ItemsSource=null;C<DataGrid>("RemnantsGrid").ItemsSource=remnants;C<DataGrid>("RemnantsGrid").Visibility=remnants.Count>0?Visibility.Visible:Visibility.Collapsed;}
 void SetNavText(string navName,string text){
  var nav=C<RadioButton>(navName);
  if(nav==null)return;
  var stack=nav.Content as StackPanel;
  if(stack!=null&&stack.Children.Count>1){
   var tb=stack.Children[1] as TextBlock;
   if(tb!=null){tb.Text=text;return;}
  }
  nav.Content=text;
 }
 void ApplyLanguage(){
  SetNavText("NavHealth",I18n.T("Tổng quan"));
  SetNavText("NavApps",I18n.T("Ứng dụng"));
  SetNavText("NavDeep",I18n.T("Quét chuyên sâu"));
  SetNavText("NavStartup",I18n.T("Khởi động"));
  SetNavText("NavDiskTools",I18n.T("Công cụ ổ đĩa"));
  SetNavText("NavDrivers",I18n.T("Driver & thiết bị"));
  SetNavText("NavHistory",I18n.T("Nhật ký"));
  SetNavText("NavTools",I18n.T("Công cụ Pro"));

  if(C<Button>("ToggleTheme")!=null)C<Button>("ToggleTheme").Content=dark?I18n.T("☀ Sáng"):I18n.T("◐ Tối");
  if(C<Button>("ToggleLang")!=null)C<Button>("ToggleLang").Content=I18n.Lang=="vi"?"🌐 VI":"🌐 EN";

  if(C<Button>("Uninstall")!=null)C<Button>("Uninstall").Content=I18n.T("Gỡ mục đã chọn");
  if(C<Button>("ForceUninstall")!=null)C<Button>("ForceUninstall").Content=I18n.T("⚡ Gỡ cưỡng chế");
  if(C<Button>("BatchUninstall")!=null)C<Button>("BatchUninstall").Content=I18n.T("⚡ Gỡ hàng loạt");
  if(C<Button>("SetupMonitorBtn")!=null)C<Button>("SetupMonitorBtn").Content=I18n.T("📸 Giám sát cài đặt");
  if(C<Button>("ScanRemnants")!=null)C<Button>("ScanRemnants").Content=I18n.T("Quét phần dư");
  if(C<Button>("CleanRemnants")!=null)C<Button>("CleanRemnants").Content=I18n.T("Dọn mục đã chọn");
  if(C<Button>("HistoryRemnants")!=null)C<Button>("HistoryRemnants").Content=I18n.T("Phần dư đã ghi nhận");
  if(C<Button>("RefreshApps")!=null)C<Button>("RefreshApps").Content=I18n.T("Làm mới");

  if(page=="Health")BuildHealth();
  else if(page=="Tools")BuildTools();
  else if(page=="DiskTools")BuildDiskTools();
  else if(page=="Deep")BuildDeepPage();
  else if(page=="Drivers")BuildDriverPage();
  else if(page=="Startup"){Navigate("Startup");}
  else if(page=="History"){Navigate("History");}
  else if(page=="Apps"){Navigate("Apps");}
  else State();
 }
 void Navigate(string name){if(busy)return;page=name;HidePro();foreach(string panel in new[]{"AppsPage","FilesPage","StartupPage","HistoryPage"})C<FrameworkElement>(panel).Visibility=Visibility.Collapsed;
  C<FrameworkElement>(name=="Temp"||name=="Large"?"FilesPage":name+"Page").Visibility=Visibility.Visible;
  string heading="",subtitle="";
  if(name=="Apps"){heading=I18n.T("Gỡ cài đặt & Quản lý ứng dụng");subtitle=I18n.T("Quản lý ứng dụng, gỡ cài đặt và kiểm tra phần còn lại.");}
  if(name=="Temp"||name=="Large"){
   bool temp=name=="Temp";heading=temp?"Dọn nhẹ, dùng thoải mái.":"Biết dung lượng đi đâu.";subtitle=temp?"Xem trước tệp tạm cũ, rồi chọn những gì bạn muốn dọn.":"Tìm các tệp lớn trong thư mục bạn chọn.";
   Text("FilesTitle",temp?"Tệp tạm của tài khoản hiện tại":"Tệp lớn từ 100 MB");Text("FilesHint",temp?"Chọn một nguồn ở bên dưới. Chỉ xét tệp được tạo và sửa hơn 7 ngày trước; kiểm tra lại khi dọn. Cache trình duyệt không bao gồm cookie, mật khẩu hoặc phiên đăng nhập.":"Chọn một thư mục để xem tối đa 500 tệp lớn nhất (từ 100 MB). Xem vị trí hoặc đánh dấu tệp không cần để chuyển vào Thùng rác sau xác nhận.");
   C<Button>("ScanFiles").Content=temp?"Quét tệp tạm":"Chọn thư mục & quét";C<Button>("SelectFiles").Visibility=Visibility.Visible;C<Button>("CleanFiles").Visibility=Visibility.Visible;C<Button>("CleanFiles").Content=temp?"Dọn mục đã chọn":"Chuyển tệp đã chọn vào Thùng rác";
   C<DataGrid>("FilesGrid").Columns[0].Visibility=Visibility.Visible;files.Clear();C<DataGrid>("FilesGrid").ItemsSource=null;Text("FilesSummary","Bấm Quét để xem trước kết quả.");C<Button>("SelectFiles").Content="Chọn tất cả";
  }
  if(name=="Startup"){heading=I18n.T("Quản lý mục khởi động cùng Windows");subtitle=I18n.T("Bật/tắt các ứng dụng chạy ngầm khi khởi động máy để tăng tốc độ khởi động");LoadStartup();}
  if(name=="History"){heading=I18n.T("Lịch sử gỡ cài đặt & Nhật ký");subtitle=I18n.T("Lịch sử gỡ cài đặt & Nhật ký");LoadHistory();}
  Text("Heading",heading);Text("Subtitle",subtitle);Text("Status","Sẵn sàng.");State();
 }
 void LoadStartup(){C<DataGrid>("StartupGrid").ItemsSource=Features.Startups();}
 void LoadHistory(){string path=Path.Combine(Engine.Data,"activity.log");if(!File.Exists(path)){C<TextBox>("HistoryText").Text="Chưa có thao tác dọn hoặc thay đổi nào được ghi nhận.";return;}var queue=new Queue<string>();foreach(string line in File.ReadLines(path)){queue.Enqueue(line);if(queue.Count>150)queue.Dequeue();}C<TextBox>("HistoryText").Text=String.Join(Environment.NewLine,queue.Reverse());}
 void State(){StatePro();foreach(string name in new[]{"NavApps","NavTemp","NavLarge","NavStartup","NavHistory","RefreshApps","Search","AppsGrid","RemnantsGrid","ScanFiles","SelectFiles","CleanFiles","FilesGrid","ToggleStartup","RefreshStartup","StartupGrid","WindowsStartup","OpenFileLocation","OpenBackups","OpenRecycle","OpenLog","RefreshHistory","SetupMonitorBtn"})C<Control>(name).IsEnabled=!busy;
  C<Button>("Uninstall").IsEnabled=!busy&&target!=null&&!target.Protected&&!String.IsNullOrWhiteSpace(target.Command);C<Button>("ScanRemnants").IsEnabled=!busy&&target!=null;C<Button>("CleanRemnants").IsEnabled=!busy&&remnants.Any(x=>x.CanClean);Text("ReadyLabel",busy?"●  Đang xử lý":lastState);}
 async Task Work(Func<Task> action){if(busy)return;busy=true;State();var elapsed=System.Diagnostics.Stopwatch.StartNew();var heartbeat=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};heartbeat.Tick+=(s,e)=>Text("ReadyLabel","Đang xử lý · "+elapsed.Elapsed.ToString(@"mm\:ss"));C<ProgressBar>("BusyProgress").Visibility=Visibility.Visible;heartbeat.Start();Text("Status","Đang xử lý…");try{await action();lastState="●  Hoàn tất";try{System.Media.SystemSounds.Asterisk.Play();}catch{}}catch(OperationCanceledException){lastState="●  Đã dừng";Text("Status","Đã dừng quét; chưa thay đổi tệp nào.");}catch(Exception ex){Error(ex);}finally{heartbeat.Stop();C<ProgressBar>("BusyProgress").Visibility=Visibility.Collapsed;busy=false;State();}}
 void ToggleLanguage(){
  string next=I18n.Lang=="vi"?"en":"vi";
  I18n.Save(next);
  ApplyLanguage();
 }
 class Owner:System.Windows.Forms.IWin32Window{public IntPtr Handle{get;private set;}public Owner(IntPtr h){Handle=h;}}
}
static class Program {
 [STAThread] static int Main(string[] args){
  try{
   if(args.Length==2&&args[0]=="--driver-search-worker")return DriverSearchJob.Worker(args[1]);
   if(args.Contains("--self-test")){Tests.Run();return 0;}
   if(args.Contains("--machine-audit")){var result=WholeMachine.Scan(WholeMachine.LocalDrives(),System.Threading.CancellationToken.None,new MachineAuditProgress(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"machine-scan-progress.txt")));File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"machine-scan-report.txt"),result.Report());return 0;}
   if(args.Contains("--diagnose-v06")){Diagnostics06.Run();return 0;}
   if(args.Contains("--diagnose-v04")){Diagnostics04.Run();return 0;}
   if(args.Contains("--audit")){var apps=Engine.Apps();File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"duplicate-audit.txt"),new[]{"Desktop apps: "+apps.Count,"Duplicate shared-view rows hidden: "+Engine.DuplicateCount,"No application or registry entry deleted."}.Concat(apps.Where(x=>x.Name.IndexOf("Zalo",StringComparison.OrdinalIgnoreCase)>=0||x.Name.IndexOf("CapCut",StringComparison.OrdinalIgnoreCase)>=0).Select(x=>x.Name+" | "+x.Version+" | "+x.Hive+" | "+x.Command)));return 0;}
   if(args.Contains("--scheduled-scan")){Maintenance.ScheduledScan();return 0;}
   var app=new Application();if(args.Contains("--preview")){Engine.Testing=true;}if(args.Contains("--preview")){var desktop=new DesktopApp();var w=desktop.Build();var timer=new System.Windows.Threading.DispatcherTimer();timer.Interval=TimeSpan.FromSeconds(1);timer.Tick+=(s,e)=>{timer.Stop();try{desktop.Preview(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Preview"));app.Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetTempPath(),"SachGon-error.txt"),ex.ToString());app.Shutdown(1);}};timer.Start();return app.Run(w);}app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(e.Exception.Message,"Dọn Dẹp PC");e.Handled=true;};return app.Run(new DesktopApp().Build());
  }catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetTempPath(),"SachGon-error.txt"),ex.ToString());if(!args.Contains("--self-test"))MessageBox.Show(ex.Message,"Dọn Dẹp PC");return 1;}
 }
}
