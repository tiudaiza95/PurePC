using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

static class Maintenance {
 public const string TaskName="PurePC-ReadOnlyScan";
 public static string Quote(string value){var output=new StringBuilder();output.Append('"');int slashes=0;foreach(char c in value){if(c=='\\'){slashes++;continue;}if(c=='"'){output.Append('\\',slashes*2+1);output.Append(c);}else{output.Append('\\',slashes);output.Append(c);}slashes=0;}output.Append('\\',slashes*2);output.Append('"');return output.ToString();}
 public static string UpdateArgs(string id){if(!Regex.IsMatch(id??"",@"^[A-Za-z0-9][A-Za-z0-9._-]{1,150}$"))throw new Exception("Nhập ID gói chính xác từ kết quả WinGet, chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch nối.");return "upgrade --id "+Quote(id)+" --exact --source winget --interactive --disable-interactivity";}
 public static Task<string> Winget(string arguments,CancellationToken token){string executable=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Microsoft","WindowsApps","winget.exe");if(!File.Exists(executable))throw new Exception("Chưa tìm thấy WinGet. Cài App Installer từ nguồn Microsoft chính thức; ứng dụng không tự cài thêm.");return Capture(executable,arguments,token);}
 public static async Task<string> Capture(string executable,string arguments,CancellationToken token){var info=new ProcessStartInfo(executable,arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};using(var proc=Process.Start(info)){var output=proc.StandardOutput.ReadToEndAsync();var error=proc.StandardError.ReadToEndAsync();var watch=Stopwatch.StartNew();while(!proc.WaitForExit(100)){if(token.IsCancellationRequested||watch.Elapsed>TimeSpan.FromMinutes(3)){try{proc.Kill();}catch{}throw new OperationCanceledException("Đã dừng tiến trình WinGet. Nếu bộ cài riêng đã mở, kiểm tra cửa sổ của bộ cài.");}await Task.Delay(80);}string result="WinGet exit="+proc.ExitCode+" · "+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+"\nLệnh: winget "+arguments+"\n\n"+await output+"\n"+await error;Engine.Log("WINGET_RESULT "+proc.ExitCode+" | "+arguments);return result;}}
 public static string ScheduleStatus(){dynamic service=null,folder=null,task=null;try{service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();folder=service.GetFolder("\\");task=folder.GetTask(TaskName);return "Lịch: "+(task.Enabled?"đang bật":"đã tắt")+" · lần kế tiếp: "+task.NextRunTime+" · chỉ quét Temp, không dọn.";}catch{return "Chưa có lịch quét do PurePC tạo hoặc không đọc được Task Scheduler.";}finally{Release(task);Release(folder);Release(service);}}
 public static void SetSchedule(string time,string executable){TimeSpan when;if(!Regex.IsMatch(time??"",@"^\d{2}:\d{2}$")||!TimeSpan.TryParse(time,out when)||when<TimeSpan.Zero||when>=TimeSpan.FromDays(1))throw new Exception("Giờ cần ở dạng HH:mm, từ 00:00 đến 23:59.");dynamic service=null,folder=null,definition=null,trigger=null,action=null,registered=null;try{service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();folder=service.GetFolder("\\");definition=service.NewTask(0);definition.RegistrationInfo.Description="PurePC: quét Temp chỉ đọc và thông báo, tuyệt đối không tự dọn.";definition.Principal.LogonType=3;definition.Principal.RunLevel=0;definition.Settings.StartWhenAvailable=true;definition.Settings.ExecutionTimeLimit="PT15M";trigger=definition.Triggers.Create(2);trigger.StartBoundary=DateTime.Today.Add(when).ToString("yyyy-MM-ddTHH:mm:ss");trigger.DaysInterval=1;trigger.Enabled=true;action=definition.Actions.Create(0);action.Path=executable;action.Arguments="--scheduled-scan";action.WorkingDirectory=Path.GetDirectoryName(executable);registered=folder.RegisterTaskDefinition(TaskName,definition,6,null,null,3);Engine.Log("SCHEDULE_ENABLED "+time+" | "+executable);}finally{Release(registered);Release(action);Release(trigger);Release(definition);Release(folder);Release(service);}}
 public static void DeleteSchedule(){dynamic service=null,folder=null;try{service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();folder=service.GetFolder("\\");folder.DeleteTask(TaskName,0);Engine.Log("SCHEDULE_REMOVED "+TaskName);}finally{Release(folder);Release(service);}}
 static void Release(object obj){if(obj!=null&&System.Runtime.InteropServices.Marshal.IsComObject(obj))System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);}
 public static void ScheduledScan(){using(var cancel=new CancellationTokenSource(TimeSpan.FromMinutes(10))){var found=AnalysisTools.Cleanup(AnalysisTools.Roots()[0],cancel.Token,null);string report="Quét theo lịch hoàn tất lúc "+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+"\n"+found.Items.Count+" tệp Temp đủ điều kiện xem trước / "+Features.Size(found.Items.Sum(x=>x.Size))+"\nKhông có tệp nào bị xóa. Mở PurePC để xem và tự chọn nếu muốn dọn.";Directory.CreateDirectory(Engine.Data);File.WriteAllText(Path.Combine(Engine.Data,"scheduled-last.txt"),report);Engine.Log("SCHEDULE_SCAN "+report.Replace("\n"," | "));MessageBox.Show(report,"PurePC · Thông báo quét");}}
}
class ProToolItem {
 public int Category;
 public string Title;
 public string Keywords;
 public Border Card;
}
partial class DesktopApp {
 TextBox toolSearchBox;
 void BuildTools(){
  proHost.Children.Clear();
  Text("Heading",I18n.T("Trung tâm Tiện ích Pro & Công cụ nâng cao"));
  Text("Subtitle",I18n.T("Kho 20 công cụ chuyên sâu: Tối ưu CSDL, Hủy tệp, Dọn Registry, Gỡ UWP, Tăng tốc RAM/Game và bảo trì Windows"));

  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  proHost.Children.Add(scroll);
  var rootPanel=new StackPanel{Margin=new Thickness(0,0,0,16)};
  scroll.Content=rootPanel;

  // THANH TÌM KIẾM VÀ BỘ LỌC DANH MỤC TIỆN ÍCH PRO
  var filterBar=new StackPanel{Margin=new Thickness(0,0,0,16)};
  rootPanel.Children.Add(filterBar);

  var searchBorder=new Border{CornerRadius=new CornerRadius(10),Padding=new Thickness(14,6,14,6),Margin=new Thickness(0,0,0,12)};
  searchBorder.SetResourceReference(Border.BackgroundProperty,"Header");
  var searchGrid=new Grid();
  toolSearchBox=new TextBox{FontSize=13,VerticalAlignment=VerticalAlignment.Center,BorderThickness=new Thickness(0),Background=Brushes.Transparent};
  toolSearchBox.SetResourceReference(TextBox.ForegroundProperty,"Ink");
  var searchHint=Label(I18n.T("🔍 Tìm kiếm tiện ích... (ví dụ: ram, ssd, registry, mạng, game, dism, uwp...) · Phím tắt: Ctrl+F"),12);
  searchHint.SetResourceReference(TextBlock.ForegroundProperty,"Muted");
  searchHint.VerticalAlignment=VerticalAlignment.Center;searchHint.IsHitTestVisible=false;
  searchGrid.Children.Add(toolSearchBox);
  searchGrid.Children.Add(searchHint);
  searchBorder.Child=searchGrid;
  filterBar.Children.Add(searchBorder);

  // HÀNG NÚT DANH MỤC
  var catRow=new WrapPanel{Margin=new Thickness(0,0,0,4)};
  filterBar.Children.Add(catRow);

  int activeCat=0;
  var catButtons=new List<Button>();
  string[] catNames=new string[]{
   "✨ Tất cả (20)",
   "🚀 Tăng tốc & RAM (5)",
   "🧹 Dọn dẹp chuyên sâu (6)",
   "🛡️ Bảo mật & Menu (4)",
   "🔧 Sửa lỗi & Bảo trì (5)"
  };

  // LƯỚI CARD CÔNG CỤ CHUYÊN SÂU (2 Cột x N Hàng động)
  var grid=new Grid{Margin=new Thickness(0,0,0,14)};
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  rootPanel.Children.Add(grid);

  Func<string,string,string,string,Action,Border> createToolCard=(icon,title,desc,btnText,action)=>{
   var p=new StackPanel();
   var headerRow=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,6)};
   headerRow.Children.Add(Label(icon+"  ",18,true));
   headerRow.Children.Add(Label(I18n.T(title),14,true));
   p.Children.Add(headerRow);
   var descLabel=Label(I18n.T(desc),11);
   descLabel.Margin=new Thickness(0,0,0,12);
   descLabel.MinHeight=32;
   p.Children.Add(descLabel);
   var btn=ActionButton(I18n.T(btnText),action);
   btn.Padding=new Thickness(12,6,12,6);
   btn.HorizontalAlignment=HorizontalAlignment.Left;
   p.Children.Add(btn);
   var card=Card(p);
   card.Padding=new Thickness(16,14,16,14);
   return card;
  };

  var toolList=new List<ProToolItem>();

  // Card 1: SQLite Optimizer (Cat 1: Tăng tốc)
  var c1=createToolCard("⚡","Tối ưu hóa Database SQLite","Hút bụi chân không (Vacuum) và tái lập chỉ mục (Reindex) CSDL của Chrome, Edge, Firefox, VS Code... tăng tốc mở app.","Mở SQLite Optimizer",OpenSqliteOptimizerDialog);
  toolList.Add(new ProToolItem{Category=1,Title="Tối ưu hóa Database SQLite",Keywords="sqlite database chrome edge firefox csdl vacuum reindex toc do khoi dong",Card=c1});

  // Card 2: File Shredder (Cat 3: Bảo mật)
  var c2=createToolCard("🔥","Hủy tệp an toàn (File Shredder)","Ghi đè dữ liệu 3 lượt theo chuẩn DoD 5220.22-M và xóa dấu vết MFT, chống khôi phục hoàn toàn.","Mở File Shredder",OpenFileShredderDialog);
  toolList.Add(new ProToolItem{Category=3,Title="Hủy tệp an toàn (File Shredder)",Keywords="huy tep an toan file shredder dod 5220 xoa vinh vien mft chong khoi phuc bao mat",Card=c2});

  // Card 3: Registry Cleaner (Cat 2: Dọn dẹp)
  var c3=createToolCard("🧹","Dọn dẹp Registry thông minh","Quét và làm sạch các khóa Registry mồ côi (CLSID, DLL thiếu, MUI cache rác). Tự động sao lưu .reg.","Mở Dọn Registry",OpenRegistryCleanerDialog);
  toolList.Add(new ProToolItem{Category=2,Title="Dọn dẹp Registry thông minh",Keywords="don dep registry cleaner mo coi clsid dll mui cache reg he thong",Card=c3});

  // Card 4: UWP Bloatware Uninstaller (Cat 2: Dọn dẹp)
  var c4=createToolCard("📦","Gỡ Bloatware Windows 10/11","Gỡ bỏ sạch sẽ các ứng dụng Universal mặc định như Xbox, Cortana, Maps, Thời tiết... Bảo vệ Store.","Mở Gỡ ứng dụng UWP",OpenUwpManagerDialog);
  toolList.Add(new ProToolItem{Category=2,Title="Gỡ Bloatware Windows 10/11",Keywords="go bloatware uwp windows 10 11 xbox cortana store app universal go cai dat",Card=c4});

  // Card 5: Browser Extensions (Cat 3: Bảo mật)
  var c5=createToolCard("🧩","Tiện ích mở rộng trình duyệt","Liệt kê, bật/tắt hoặc gỡ bỏ các Add-on/Extension mở rộng trên Chrome, Edge, Cốc Cốc, Brave, Firefox.","Quản lý tiện ích",OpenBrowserExtensionsDialog);
  toolList.Add(new ProToolItem{Category=3,Title="Tiện ích mở rộng trình duyệt",Keywords="tien ich mo rong browser extensions add-on chrome edge coc coc brave firefox",Card=c5});

  // Card 6: Services Manager (Cat 4: Sửa lỗi & Bảo trì)
  var c6=createToolCard("⚙️","Quản lý Dịch vụ Windows","Kiểm soát các dịch vụ nền của phần mềm bên thứ 3 tự khởi chạy, bảo vệ tuyệt đối dịch vụ Windows cốt lõi.","Quản lý Dịch vụ",OpenServicesDialog);
  toolList.Add(new ProToolItem{Category=4,Title="Quản lý Dịch vụ Windows",Keywords="quan ly dich vu windows services chay ngam tat dich vu toi uu khoi dong",Card=c6});

  // Card 7: Setup Monitor (Cat 4: Sửa lỗi & Bảo trì)
  var c7=createToolCard("🔍","Giám sát cài đặt ứng dụng","Chụp snapshot hệ thống trước và sau cài đặt để theo dõi 100% tệp và registry đã sinh ra, hỗ trợ gỡ sạch.","Bật Giám sát cài đặt",OpenSetupMonitorDialog);
  toolList.Add(new ProToolItem{Category=4,Title="Giám sát cài đặt ứng dụng",Keywords="giam sat cai dat setup monitor snapshot theo doi go sach phan du",Card=c7});

  // Card 8: DNS Flush & Recycle Bin (Cat 1: Tăng tốc)
  var p8=new StackPanel();
  var h8=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,6)};
  h8.Children.Add(Label("🚀  ",18,true));h8.Children.Add(Label(I18n.T("Làm sạch DNS & Dọn Thùng rác"),14,true));p8.Children.Add(h8);
  var d8=Label(I18n.T("Xóa sạch bộ đệm phân giải tên miền (DNS Resolver Cache) và dọn sạch Thùng rác Recycle Bin chỉ với 1 thao tác."),11);
  d8.Margin=new Thickness(0,0,0,12);d8.MinHeight=32;p8.Children.Add(d8);
  var row8=new WrapPanel();
  row8.Children.Add(ActionButton(I18n.T("Làm sạch DNS"),()=>{
   SystemPerformance.FlushDnsCache();
   try{System.Media.SystemSounds.Asterisk.Play();}catch{}
   MessageBox.Show(window,I18n.T("Đã làm sạch DNS Resolver Cache thành công!"),I18n.T("DNS Cache"),MessageBoxButton.OK,MessageBoxImage.Information);
  }));
  row8.Children.Add(ActionButton(I18n.T("Dọn Thùng rác"),()=>{
   if(Confirm(I18n.T("Bạn có chắc chắn muốn dọn sạch toàn bộ Thùng rác (Recycle Bin)?"))){
    SystemPerformance.EmptyRecycleBin();
    try{System.Media.SystemSounds.Asterisk.Play();}catch{}
    MessageBox.Show(window,I18n.T("Đã làm sạch Thùng rác thành công!"),I18n.T("Dọn Thùng rác"),MessageBoxButton.OK,MessageBoxImage.Information);
   }
  }));
  p8.Children.Add(row8);
  var c8=Card(p8);c8.Padding=new Thickness(16,14,16,14);
  toolList.Add(new ProToolItem{Category=1,Title="Làm sạch DNS & Dọn Thùng rác",Keywords="lam sach dns flush dns resolver cache thung rac recycle bin xoa mang",Card=c8});

  // Card 9: WinGet Updater (Cat 4: Sửa lỗi & Bảo trì)
  var updates=new StackPanel();
  var hu=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,6)};
  hu.Children.Add(Label("🔄  ",18,true));hu.Children.Add(Label("Software Updater · WinGet",14,true));updates.Children.Add(hu);
  updates.Children.Add(Label("Dùng nguồn winget chính thức của Microsoft. Kiểm tra metadata gói qua mạng, cập nhật an toàn từng phần mềm.",11));
  var rowU=new WrapPanel{Margin=new Thickness(0,8,0,8)};
  toolCheck=WorkButton("Kiểm tra bản cập nhật",async()=>{
   if(!Confirm("Cho phép WinGet kết nối nguồn winget và đối chiếu phần mềm trên máy để tìm bản cập nhật?\nỨng dụng không tải danh sách lên máy chủ riêng."))return;
   await WithScan(async token=>{toolOutput.Text="Đang chờ WinGet…";toolOutput.Text=await Maintenance.Winget("upgrade --source winget --disable-interactivity",token);});
   Text("Status","WinGet đã kết thúc.");
  });
  rowU.Children.Add(toolCheck);
  rowU.Children.Add(ActionButton("Tài liệu Microsoft",()=>OpenUrl("https://learn.microsoft.com/en-us/windows/package-manager/winget/upgrade")));
  updates.Children.Add(rowU);
  var id=new TextBox{MinWidth=220,Text="",Margin=new Thickness(0,4,0,8)};id.SetResourceReference(Control.BackgroundProperty,"Header");
  updates.Children.Add(Label("ID gói cần cập nhật:",11));updates.Children.Add(id);
  toolUpdate=WorkButton("Cập nhật gói này",async()=>{
   string args=Maintenance.UpdateArgs(id.Text.Trim());
   if(!Confirm("Cập nhật gói "+id.Text.Trim()+" qua WinGet?"))return;
   await WithScan(async token=>toolOutput.Text=await Maintenance.Winget(args,token));
   Text("Status","Đã nhận kết quả từ WinGet.");await Reload();
  });
  updates.Children.Add(toolUpdate);
  toolOutput=new TextBox{IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Height=130,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Text="Chưa kiểm tra trực tuyến.",FontFamily=new System.Windows.Media.FontFamily("Consolas"),FontSize=11,Margin=new Thickness(0,8,0,0)};
  updates.Children.Add(toolOutput);
  var c9=Card(updates);c9.Padding=new Thickness(16,14,16,14);
  toolList.Add(new ProToolItem{Category=4,Title="Software Updater · WinGet",Keywords="software updater winget cap nhat phan mem official microsoft package",Card=c9});

  // Card 10: Task Scheduler (Cat 4: Sửa lỗi & Bảo trì)
  var schedule=new StackPanel();
  var hs=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,6)};
  hs.Children.Add(Label("⏰  ",18,true));hs.Children.Add(Label(I18n.T("Lịch quét tự động"),14,true));schedule.Children.Add(hs);
  var status=Label(Maintenance.ScheduleStatus(),12,true);status.Margin=new Thickness(0,0,0,4);schedule.Children.Add(status);
  schedule.Children.Add(Label("Tự động quét Temp hàng ngày khi đăng nhập Windows, ghi nhật ký báo cáo. Không tự ý xóa tệp.",11));
  var time=new TextBox{Text="19:00",Width=80,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,8,0,8)};time.SetResourceReference(Control.BackgroundProperty,"Header");schedule.Children.Add(time);
  var buttons=new WrapPanel();
  buttons.Children.Add(ActionButton("Lưu lịch hàng ngày",()=>{
   if(!Confirm("Tạo/cập nhật tác vụ "+Maintenance.TaskName+" lúc "+time.Text+" mỗi ngày?"))return;
   Maintenance.SetSchedule(time.Text,Process.GetCurrentProcess().MainModule.FileName);
   status.Text=Maintenance.ScheduleStatus();
   try{System.Media.SystemSounds.Asterisk.Play();}catch{}
  }));
  buttons.Children.Add(ActionButton("Xóa lịch",()=>{
   if(!Confirm("Xóa tác vụ "+Maintenance.TaskName+"?"))return;
   Maintenance.DeleteSchedule();status.Text=Maintenance.ScheduleStatus();
  }));
  schedule.Children.Add(buttons);
  var c10=Card(schedule);c10.Padding=new Thickness(16,14,16,14);
  toolList.Add(new ProToolItem{Category=4,Title="Lịch quét tự động",Keywords="lich quet tu dong task scheduler hen gio tu dong quet temp",Card=c10});

  // Card 11: Dev & AI Cache Cleaner (Cat 2: Dọn dẹp)
  var c11=createToolCard("💻","Rác Lập trình & Mô hình AI","Dọn sạch bộ đệm của NPM, Yarn, Pip, PyTorch, Hugging Face, Gradle, Cargo... tiết kiệm hàng chục GB ổ C:.","Dọn rác Dev & AI",OpenDevAiCleanerDialog);
  toolList.Add(new ProToolItem{Category=2,Title="Rác Lập trình & Mô hình AI",Keywords="rac lap trinh dev ai cache python pip npm yarn pytorch hugging face gradle cargo",Card=c11});

  // Card 12: Game & Low-Latency Booster (Cat 1: Tăng tốc)
  var c12=createToolCard("🎮","Tăng tốc Game & Giảm Ping","Tối ưu hóa độ trễ TCP/IP, tắt bóp băng thông mạng (Throttling) và mở khóa gói Ultimate Performance.","Mở Bộ Tăng tốc Game",OpenGameBoosterDialog);
  toolList.Add(new ProToolItem{Category=1,Title="Tăng tốc Game & Giảm Ping",Keywords="tang toc game giam ping lag tcp nagle throttling ultimate performance latency do tre",Card=c12});

  // Card 13: WinSxS & Component Store Optimizer (Cat 2: Dọn dẹp)
  var c13=createToolCard("🧱","Tối ưu kho WinSxS (DISM)","Phân tích và thu hồi các gói cập nhật Windows Update cũ qua công cụ Microsoft DISM chính thức.","Mở Tối ưu WinSxS",OpenComponentStoreDialog);
  toolList.Add(new ProToolItem{Category=2,Title="Tối ưu kho WinSxS (DISM)",Keywords="toi uu winsxs dism component store windows update ban cap nhat cu ban sao",Card=c13});

  // Card 14: Intelligent RAM Optimizer (Cat 1: Tăng tốc)
  var c14=createToolCard("🧠","Bộ Tối ưu RAM Chuyên sâu","Thu dọn Working Set của các tiến trình bằng API Win32, giải phóng bộ nhớ RAM mà không tắt ứng dụng.","Mở Tối ưu RAM",OpenRamOptimizerDialog);
  toolList.Add(new ProToolItem{Category=1,Title="Bộ Tối ưu RAM Chuyên sâu",Keywords="toi uu ram bo nho working set standby list purge islc giai phong memory",Card=c14});

  // Card 15: Privacy & Telemetry Shield (Cat 3: Bảo mật)
  var c15=createToolCard("🛡️","Bảo vệ Riêng tư & Tắt Telemetry","Tắt thu thập dữ liệu chẩn đoán (DiagTrack), chặn quảng cáo và tìm kiếm Bing trong Start Menu.","Mở Privacy Shield",OpenPrivacyShieldDialog);
  toolList.Add(new ProToolItem{Category=3,Title="Bảo vệ Riêng tư & Tắt Telemetry",Keywords="bao ve rieng tu privacy shield telemetry diagtrack chan quang cao bing tracking theo doi",Card=c15});

  // Card 16: System File Checker & DISM Repair (Cat 4: Sửa lỗi & Bảo trì)
  var c16=createToolCard("🛠️","Sửa lỗi tệp hệ thống & DISM","Quét SFC Scannow, phục hồi kho ảnh Windows DISM RestoreHealth và đặt lại ngăn xếp mạng TCP/IP.","Mở Sửa lỗi Windows",OpenSystemRepairDialog);
  toolList.Add(new ProToolItem{Category=4,Title="Sửa lỗi tệp hệ thống & DISM",Keywords="sua loi tep he thong sfc scannow dism restorehealth khoi phuc tcp ip winsock mang loi",Card=c16});

  // Card 17: Context Menu Manager (Cat 3: Bảo mật)
  var c17=createToolCard("🖱️","Menu Chuột phải (Context Menu)","Tắt hoặc bật lại các menu mở rộng bên thứ 3 trong menu chuột phải File Explorer, chống giật đơ.","Quản lý Menu chuột phải",OpenContextMenuDialog);
  toolList.Add(new ProToolItem{Category=3,Title="Menu Chuột phải (Context Menu)",Keywords="menu chuot phai context menu file explorer shell extension giat do tat bat",Card=c17});

  // Card 18: SSD TRIM Optimizer (Cat 1: Tăng tốc)
  var c18=createToolCard("⚡","Tối ưu hóa & Kích hoạt TRIM SSD","Kiểm tra trạng thái TRIM và phát tín hiệu Re-TRIM cho chip điều khiển SSD duy trì tốc độ và tuổi thọ.","Tối ưu TRIM SSD",OpenSsdTrimDialog);
  toolList.Add(new ProToolItem{Category=1,Title="Tối ưu hóa & Kích hoạt TRIM SSD",Keywords="toi uu hoa trim ssd retrim o cung the ran toc do tuoi tho chip controller",Card=c18});

  // Card 19: Folder Space Visualizer (Cat 2: Dọn dẹp)
  var c19=createToolCard("📊","Phân tích Dung lượng Thư mục","Đo lường và xếp hạng trực quan các thư mục chiếm nhiều dung lượng nhất trên ổ đĩa C:\\ hoặc D:\\.","Phân tích Dung lượng",OpenFolderSpaceDialog);
  toolList.Add(new ProToolItem{Category=2,Title="Phân tích Dung lượng Thư mục",Keywords="phan tich dung luong thu muc visualizer xep hang o dia c d thu muc lon nhat",Card=c19});

  // Card 20: Windows Update Cache Cleaner (Cat 2: Dọn dẹp)
  var c20=createToolCard("🧹","Dọn Windows Update Cache","Dọn sạch các bản cập nhật tải về bị lỗi trong SoftwareDistribution\\Download và khởi động lại dịch vụ.","Dọn Update Cache",OpenWindowsUpdateCleanerDialog);
  toolList.Add(new ProToolItem{Category=2,Title="Dọn Windows Update Cache",Keywords="don windows update cache softwaredistribution download tai ve loi wuauserv bits",Card=c20});

  // Logic lọc và hiển thị động
  Action filterTools=()=>{
   string q=(toolSearchBox!=null?(toolSearchBox.Text??"").ToLowerInvariant().Trim():"");
   var visible=toolList.Where(t=>{
    bool catMatch=(activeCat==0||t.Category==activeCat);
    if(!catMatch)return false;
    if(String.IsNullOrEmpty(q))return true;
    return (t.Title??"").ToLowerInvariant().Contains(q)||(t.Keywords??"").ToLowerInvariant().Contains(q);
   }).ToList();

   grid.Children.Clear();
   grid.RowDefinitions.Clear();
   int rows=(visible.Count+1)/2;
   for(int r=0;r<rows;r++)grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});

   for(int i=0;i<visible.Count;i++){
    var card=visible[i].Card;
    int r=i/2;
    int c=i%2;
    Grid.SetRow(card,r);
    Grid.SetColumn(card,c);
    card.Margin=new Thickness(c==0?0:5,0,c==0?5:0,10);
    grid.Children.Add(card);
   }
  };

  // Khởi tạo các nút tab danh mục
  for(int idx=0;idx<catNames.Length;idx++){
   int catIdx=idx;
   var btnCat=new Button{Content=I18n.T(catNames[idx]),Padding=new Thickness(12,6,12,6),Margin=new Thickness(0,0,8,6),FontSize=12};
   btnCat.Click+=(s,e)=>{
    activeCat=catIdx;
    for(int b=0;b<catButtons.Count;b++){
     catButtons[b].Style=b==activeCat?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
    }
    filterTools();
   };
   catButtons.Add(btnCat);
   catRow.Children.Add(btnCat);
  }
  catButtons[0].Style=(Style)window.Resources["Primary"];

  toolSearchBox.TextChanged+=(s,e)=>{
   searchHint.Visibility=String.IsNullOrEmpty(toolSearchBox.Text)?Visibility.Visible:Visibility.Collapsed;
   filterTools();
  };

  filterTools();
 }
}