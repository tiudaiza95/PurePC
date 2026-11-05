using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

static class Maintenance {
 public const string TaskName="DonDepPC-ReadOnlyScan";
 public static string Quote(string value){var output=new StringBuilder();output.Append('"');int slashes=0;foreach(char c in value){if(c=='\\'){slashes++;continue;}if(c=='"'){output.Append('\\',slashes*2+1);output.Append(c);}else{output.Append('\\',slashes);output.Append(c);}slashes=0;}output.Append('\\',slashes*2);output.Append('"');return output.ToString();}
 public static string UpdateArgs(string id){if(!Regex.IsMatch(id??"",@"^[A-Za-z0-9][A-Za-z0-9._-]{1,150}$"))throw new Exception("Nhập ID gói chính xác từ kết quả WinGet, chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch nối.");return "upgrade --id "+Quote(id)+" --exact --source winget --interactive --disable-interactivity";}
 public static Task<string> Winget(string arguments,CancellationToken token){string executable=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Microsoft","WindowsApps","winget.exe");if(!File.Exists(executable))throw new Exception("Chưa tìm thấy WinGet. Cài App Installer từ nguồn Microsoft chính thức; ứng dụng không tự cài thêm.");return Capture(executable,arguments,token);}
 public static async Task<string> Capture(string executable,string arguments,CancellationToken token){var info=new ProcessStartInfo(executable,arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};using(var proc=Process.Start(info)){var output=proc.StandardOutput.ReadToEndAsync();var error=proc.StandardError.ReadToEndAsync();var watch=Stopwatch.StartNew();while(!proc.WaitForExit(100)){if(token.IsCancellationRequested||watch.Elapsed>TimeSpan.FromMinutes(3)){try{proc.Kill();}catch{}throw new OperationCanceledException("Đã dừng tiến trình WinGet. Nếu bộ cài riêng đã mở, kiểm tra cửa sổ của bộ cài.");}await Task.Delay(80);}string result="WinGet exit="+proc.ExitCode+" · "+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+"\nLệnh: winget "+arguments+"\n\n"+await output+"\n"+await error;Engine.Log("WINGET_RESULT "+proc.ExitCode+" | "+arguments);return result;}}
 public static string ScheduleStatus(){dynamic service=null,folder=null,task=null;try{service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();folder=service.GetFolder("\\");task=folder.GetTask(TaskName);return "Lịch: "+(task.Enabled?"đang bật":"đã tắt")+" · lần kế tiếp: "+task.NextRunTime+" · chỉ quét Temp, không dọn.";}catch{return "Chưa có lịch quét do Dọn Dẹp PC tạo hoặc không đọc được Task Scheduler.";}finally{Release(task);Release(folder);Release(service);}}
 public static void SetSchedule(string time,string executable){TimeSpan when;if(!Regex.IsMatch(time??"",@"^\d{2}:\d{2}$")||!TimeSpan.TryParse(time,out when)||when<TimeSpan.Zero||when>=TimeSpan.FromDays(1))throw new Exception("Giờ cần ở dạng HH:mm, từ 00:00 đến 23:59.");dynamic service=null,folder=null,definition=null,trigger=null,action=null,registered=null;try{service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();folder=service.GetFolder("\\");definition=service.NewTask(0);definition.RegistrationInfo.Description="Dọn Dẹp PC: quét Temp chỉ đọc và thông báo, tuyệt đối không tự dọn.";definition.Principal.LogonType=3;definition.Principal.RunLevel=0;definition.Settings.StartWhenAvailable=true;definition.Settings.ExecutionTimeLimit="PT15M";trigger=definition.Triggers.Create(2);trigger.StartBoundary=DateTime.Today.Add(when).ToString("yyyy-MM-ddTHH:mm:ss");trigger.DaysInterval=1;trigger.Enabled=true;action=definition.Actions.Create(0);action.Path=executable;action.Arguments="--scheduled-scan";action.WorkingDirectory=Path.GetDirectoryName(executable);registered=folder.RegisterTaskDefinition(TaskName,definition,6,null,null,3);Engine.Log("SCHEDULE_ENABLED "+time+" | "+executable);}finally{Release(registered);Release(action);Release(trigger);Release(definition);Release(folder);Release(service);}}
 public static void DeleteSchedule(){dynamic service=null,folder=null;try{service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();folder=service.GetFolder("\\");folder.DeleteTask(TaskName,0);Engine.Log("SCHEDULE_REMOVED "+TaskName);}finally{Release(folder);Release(service);}}
 static void Release(object obj){if(obj!=null&&System.Runtime.InteropServices.Marshal.IsComObject(obj))System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);}
 public static void ScheduledScan(){using(var cancel=new CancellationTokenSource(TimeSpan.FromMinutes(10))){var found=AnalysisTools.Cleanup(AnalysisTools.Roots()[0],cancel.Token,null);string report="Quét theo lịch hoàn tất lúc "+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+"\n"+found.Items.Count+" tệp Temp đủ điều kiện xem trước / "+Features.Size(found.Items.Sum(x=>x.Size))+"\nKhông có tệp nào bị xóa. Mở Dọn Dẹp PC để xem và tự chọn nếu muốn dọn.";Directory.CreateDirectory(Engine.Data);File.WriteAllText(Path.Combine(Engine.Data,"scheduled-last.txt"),report);Engine.Log("SCHEDULE_SCAN "+report.Replace("\n"," | "));MessageBox.Show(report,"Dọn Dẹp PC · Thông báo quét");}}
}
partial class DesktopApp {
 void BuildTools(){
  proHost.Children.Clear();
  Text("Heading",I18n.T("Trung tâm Tiện ích Pro & Công cụ nâng cao"));
  Text("Subtitle",I18n.T("Kho công cụ chuyên sâu: Tối ưu CSDL, Hủy tệp an toàn, Dọn Registry, Gỡ UWP và bảo trì Windows"));

  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  proHost.Children.Add(scroll);
  var rootPanel=new StackPanel{Margin=new Thickness(0,0,0,16)};
  scroll.Content=rootPanel;

  // LƯỚI CARD CÔNG CỤ CHUYÊN SÂU (2 Cột x 5 Hàng)
  var grid=new Grid{Margin=new Thickness(0,0,0,14)};
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  for(int r=0;r<8;r++)grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
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

  // Card 1: SQLite Optimizer
  var c1=createToolCard("⚡","Tối ưu hóa Database SQLite","Hút bụi chân không (Vacuum) và tái lập chỉ mục (Reindex) CSDL của Chrome, Edge, Firefox, VS Code... tăng tốc mở app.","Mở SQLite Optimizer",OpenSqliteOptimizerDialog);
  c1.Margin=new Thickness(0,0,5,10);Grid.SetRow(c1,0);Grid.SetColumn(c1,0);grid.Children.Add(c1);

  // Card 2: File Shredder
  var c2=createToolCard("🔥","Hủy tệp an toàn (File Shredder)","Ghi đè dữ liệu 3 lượt theo chuẩn DoD 5220.22-M và xóa dấu vết MFT, chống khôi phục hoàn toàn.","Mở File Shredder",OpenFileShredderDialog);
  c2.Margin=new Thickness(5,0,0,10);Grid.SetRow(c2,0);Grid.SetColumn(c2,1);grid.Children.Add(c2);

  // Card 3: Registry Cleaner
  var c3=createToolCard("🧹","Dọn dẹp Registry thông minh","Quét và làm sạch các khóa Registry mồ côi (CLSID, DLL thiếu, MUI cache rác). Tự động sao lưu .reg.","Mở Dọn Registry",OpenRegistryCleanerDialog);
  c3.Margin=new Thickness(0,0,5,10);Grid.SetRow(c3,1);Grid.SetColumn(c3,0);grid.Children.Add(c3);

  // Card 4: UWP Bloatware Uninstaller
  var c4=createToolCard("📦","Gỡ Bloatware Windows 10/11","Gỡ bỏ sạch sẽ các ứng dụng Universal mặc định như Xbox, Cortana, Maps, Thời tiết... Bảo vệ Store.","Mở Gỡ ứng dụng UWP",OpenUwpManagerDialog);
  c4.Margin=new Thickness(5,0,0,10);Grid.SetRow(c4,1);Grid.SetColumn(c4,1);grid.Children.Add(c4);

  // Card 5: Browser Extensions
  var c5=createToolCard("🧩","Tiện ích mở rộng trình duyệt","Liệt kê, bật/tắt hoặc gỡ bỏ các Add-on/Extension mở rộng trên Chrome, Edge, Cốc Cốc, Brave, Firefox.","Quản lý tiện ích",OpenBrowserExtensionsDialog);
  c5.Margin=new Thickness(0,0,5,10);Grid.SetRow(c5,2);Grid.SetColumn(c5,0);grid.Children.Add(c5);

  // Card 6: Services Manager
  var c6=createToolCard("⚙️","Quản lý Dịch vụ Windows","Kiểm soát các dịch vụ nền của phần mềm bên thứ 3 tự khởi chạy, bảo vệ tuyệt đối dịch vụ Windows cốt lõi.","Quản lý Dịch vụ",OpenServicesDialog);
  c6.Margin=new Thickness(5,0,0,10);Grid.SetRow(c6,2);Grid.SetColumn(c6,1);grid.Children.Add(c6);

  // Card 7: Setup Monitor
  var c7=createToolCard("🔍","Giám sát cài đặt ứng dụng","Chụp snapshot hệ thống trước và sau cài đặt để theo dõi 100% tệp và registry đã sinh ra, hỗ trợ gỡ sạch.","Bật Giám sát cài đặt",OpenSetupMonitorDialog);
  c7.Margin=new Thickness(0,0,5,10);Grid.SetRow(c7,3);Grid.SetColumn(c7,0);grid.Children.Add(c7);

  // Card 8: DNS Flush & Recycle Bin
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
  c8.Margin=new Thickness(5,0,0,10);Grid.SetRow(c8,3);Grid.SetColumn(c8,1);grid.Children.Add(c8);

  // Card 9: WinGet Updater
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
  c9.Margin=new Thickness(0,0,5,0);Grid.SetRow(c9,4);Grid.SetColumn(c9,0);grid.Children.Add(c9);

  // Card 10: Task Scheduler
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
  c10.Margin=new Thickness(5,0,0,10);Grid.SetRow(c10,4);Grid.SetColumn(c10,1);grid.Children.Add(c10);
  c9.Margin=new Thickness(0,0,5,10);

  // Card 11: Dev & AI Cache Cleaner
  var c11=createToolCard("💻","Rác Lập trình & Mô hình AI","Dọn sạch bộ đệm của NPM, Yarn, Pip, PyTorch, Hugging Face, Gradle, Cargo... tiết kiệm hàng chục GB ổ C:.","Dọn rác Dev & AI",OpenDevAiCleanerDialog);
  c11.Margin=new Thickness(0,0,5,10);Grid.SetRow(c11,5);Grid.SetColumn(c11,0);grid.Children.Add(c11);

  // Card 12: Game & Low-Latency Booster
  var c12=createToolCard("🎮","Tăng tốc Game & Giảm Ping","Tối ưu hóa độ trễ TCP/IP, tắt bóp băng thông mạng (Throttling) và mở khóa gói Ultimate Performance.","Mở Bộ Tăng tốc Game",OpenGameBoosterDialog);
  c12.Margin=new Thickness(5,0,0,10);Grid.SetRow(c12,5);Grid.SetColumn(c12,1);grid.Children.Add(c12);

  // Card 13: WinSxS & Component Store Optimizer
  var c13=createToolCard("🧱","Tối ưu kho WinSxS (DISM)","Phân tích và thu hồi các gói cập nhật Windows Update cũ qua công cụ Microsoft DISM chính thức.","Mở Tối ưu WinSxS",OpenComponentStoreDialog);
  c13.Margin=new Thickness(0,0,5,10);Grid.SetRow(c13,6);Grid.SetColumn(c13,0);grid.Children.Add(c13);

  // Card 14: Intelligent RAM Optimizer
  var c14=createToolCard("🧠","Bộ Tối ưu RAM Chuyên sâu","Thu dọn Working Set của các tiến trình bằng API Win32, giải phóng bộ nhớ RAM mà không tắt ứng dụng.","Mở Tối ưu RAM",OpenRamOptimizerDialog);
  c14.Margin=new Thickness(5,0,0,10);Grid.SetRow(c14,6);Grid.SetColumn(c14,1);grid.Children.Add(c14);

  // Card 15: Privacy & Telemetry Shield
  var c15=createToolCard("🛡️","Bảo vệ Riêng tư & Tắt Telemetry","Tắt thu thập dữ liệu chẩn đoán (DiagTrack), chặn quảng cáo và tìm kiếm Bing trong Start Menu.","Mở Privacy Shield",OpenPrivacyShieldDialog);
  c15.Margin=new Thickness(0,0,5,0);Grid.SetRow(c15,7);Grid.SetColumn(c15,0);grid.Children.Add(c15);

  // Card 16: System File Checker & DISM Repair
  var c16=createToolCard("🛠️","Sửa lỗi tệp hệ thống & DISM","Quét SFC Scannow, phục hồi kho ảnh Windows DISM RestoreHealth và đặt lại ngăn xếp mạng TCP/IP.","Mở Sửa lỗi Windows",OpenSystemRepairDialog);
  c16.Margin=new Thickness(5,0,0,0);Grid.SetRow(c16,7);Grid.SetColumn(c16,1);grid.Children.Add(c16);
 }
}