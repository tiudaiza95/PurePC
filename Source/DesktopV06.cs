using System.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
partial class DesktopApp {
 JunkReport junkReport=new JunkReport();TextBlock junkStatus;DispatcherTimer perfTimer;Action onHealthPerfTick;
 void BuildHealth(){
  Text("Heading",I18n.T("Tổng quan hệ thống & Giám sát hiệu năng"));
  Text("Subtitle",I18n.T("Cấu hình máy tính, giám sát tài nguyên thời gian thực và dọn dẹp hệ thống"));
  proHost.Children.Clear();

  var mainStack=new StackPanel{Margin=new Thickness(0,0,0,10)};
  proHost.Children.Add(new ScrollViewer{Content=mainStack,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});

  // HÀNG 1: Grid 2 cột ngang hàng cân xứng: [Cấu hình hệ thống] | [Dọn dẹp hệ thống & Tệp tạm]
  var topGrid=new Grid{Margin=new Thickness(0,0,0,14)};
  topGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1.05,GridUnitType.Star)});
  topGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1.25,GridUnitType.Star)});
  mainStack.Children.Add(topGrid);

  // Cột 0: Card CẤU HÌNH HỆ THỐNG
  var specCard=new StackPanel();
  var specTitle=Label(I18n.T("CẤU HÌNH HỆ THỐNG"),16,true);
  specTitle.HorizontalAlignment=HorizontalAlignment.Center;
  specTitle.Margin=new Thickness(0,0,0,10);
  specCard.Children.Add(specTitle);

  var specGrid=new Grid{Margin=new Thickness(0,2,0,4)};
  specGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(145)});
  specGrid.ColumnDefinitions.Add(new ColumnDefinition());
  string[,] specs={
   {I18n.T("Hệ điều hành"),SystemPerformance.GetOsName()},
   {I18n.T("Bộ vi xử lý (CPU)"),SystemPerformance.GetCpuName()},
   {I18n.T("Card đồ họa (GPU)"),SystemPerformance.GetGpuName()},
   {I18n.T("Bo mạch chủ"),SystemPerformance.GetBoardName()},
   {I18n.T("Thời gian hoạt động"),SystemPerformance.GetUptimeString()}
  };
  for(int i=0;i<specs.GetLength(0);i++){
   specGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   var k=Label(specs[i,0],13);k.SetResourceReference(TextBlock.ForegroundProperty,"Muted");k.Margin=new Thickness(0,5,0,5);
   var v=Label(specs[i,1],13,true);v.Margin=new Thickness(0,5,0,5);
   Grid.SetRow(k,i);Grid.SetRow(v,i);Grid.SetColumn(v,1);
   specGrid.Children.Add(k);specGrid.Children.Add(v);
  }
  specCard.Children.Add(specGrid);
  var cardLeft=Card(specCard);
  cardLeft.Margin=new Thickness(0,0,6,0);
  cardLeft.Padding=new Thickness(22,18,22,18);
  Grid.SetColumn(cardLeft,0);
  topGrid.Children.Add(cardLeft);

  // Cột 1: Card DỌN DẸP HỆ THỐNG & TỆP TẠM
  var cleanCard=new StackPanel();
  var cleanTitle=Label(I18n.T("DỌN DẸP HỆ THỐNG & TỆP TẠM"),16,true);
  cleanTitle.HorizontalAlignment=HorizontalAlignment.Center;
  cleanTitle.Margin=new Thickness(0,0,0,10);
  cleanCard.Children.Add(cleanTitle);

  var chkChrome=new CheckBox{Content=I18n.T("Dọn sạch bộ nhớ đệm Google Chrome (Bỏ chọn nếu muốn giữ cache)"),IsChecked=true,Margin=new Thickness(0,2,0,10)};
  chkChrome.SetResourceReference(Control.ForegroundProperty,"Ink");
  chkChrome.HorizontalAlignment=HorizontalAlignment.Center;
  cleanCard.Children.Add(chkChrome);

  junkStatus=Label(junkReport.Items.Count==0?I18n.T("Chưa quét. Sẵn sàng quét và tối ưu hệ thống 1-click."):JunkSummary(),13);
  junkStatus.HorizontalAlignment=HorizontalAlignment.Center;
  junkStatus.TextAlignment=TextAlignment.Center;
  junkStatus.Margin=new Thickness(0,2,0,12);
  cleanCard.Children.Add(junkStatus);

  // Lưới 2x2 cho 4 nút bấm cân đối hoàn hảo
  var btnGrid=new Grid{Margin=new Thickness(0,2,0,0)};
  btnGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  btnGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  btnGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  btnGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  cleanCard.Children.Add(btnGrid);

  var cardRight=Card(cleanCard);
  cardRight.Margin=new Thickness(6,0,0,0);
  cardRight.Padding=new Thickness(22,18,22,18);
  Grid.SetColumn(cardRight,1);
  topGrid.Children.Add(cardRight);

  // HÀNG 2: 3 Card Giám sát tài nguyên (CPU, RAM, Ổ C:)
  var perfGrid=new Grid{Margin=new Thickness(0,0,0,14)};
  perfGrid.ColumnDefinitions.Add(new ColumnDefinition());
  perfGrid.ColumnDefinitions.Add(new ColumnDefinition());
  perfGrid.ColumnDefinitions.Add(new ColumnDefinition());

  // Ô 1: CPU
  var cpuTitle=Label(I18n.T("MỨC DÙNG CPU"),14,true);cpuTitle.HorizontalAlignment=HorizontalAlignment.Center;
  var cpuVal=Label("…",28,true);cpuVal.HorizontalAlignment=HorizontalAlignment.Center;cpuVal.Margin=new Thickness(0,4,0,4);
  var cpuBar=new ProgressBar{Height=8,Margin=new Thickness(0,6,0,6),Maximum=100};
  var cpuSub=Label(I18n.T("Đang theo dõi"),12);cpuSub.SetResourceReference(TextBlock.ForegroundProperty,"Muted");cpuSub.HorizontalAlignment=HorizontalAlignment.Center;
  var cpuWave=new Canvas{Height=32,Margin=new Thickness(0,4,0,4),ClipToBounds=true};
  var cpuBox=new StackPanel();cpuBox.Children.Add(cpuTitle);cpuBox.Children.Add(cpuVal);cpuBox.Children.Add(cpuBar);cpuBox.Children.Add(cpuWave);cpuBox.Children.Add(cpuSub);
  var cpuCard=Card(cpuBox);cpuCard.Margin=new Thickness(0,0,6,0);cpuCard.Padding=new Thickness(18,16,18,16);Grid.SetColumn(cpuCard,0);perfGrid.Children.Add(cpuCard);
  cpuCard.Cursor=System.Windows.Input.Cursors.Hand;cpuCard.ToolTip=I18n.T("Bấm để mở Bộ Tăng Tốc Game & Giảm Ping");
  cpuCard.MouseLeftButtonUp+=(s,e)=>OpenGameBoosterDialog();

  // Ô 2: RAM
  var ramTitle=Label(I18n.T("BỘ NHỚ RAM"),14,true);ramTitle.HorizontalAlignment=HorizontalAlignment.Center;
  var ramVal=Label("…",28,true);ramVal.HorizontalAlignment=HorizontalAlignment.Center;ramVal.Margin=new Thickness(0,4,0,4);
  var ramBar=new ProgressBar{Height=8,Margin=new Thickness(0,6,0,6),Maximum=100};
  var ramSub=Label(I18n.T("Đang theo dõi"),12);ramSub.SetResourceReference(TextBlock.ForegroundProperty,"Muted");ramSub.HorizontalAlignment=HorizontalAlignment.Center;
  var ramWave=new Canvas{Height=32,Margin=new Thickness(0,4,0,4),ClipToBounds=true};
  var ramBox=new StackPanel();ramBox.Children.Add(ramTitle);ramBox.Children.Add(ramVal);ramBox.Children.Add(ramBar);ramBox.Children.Add(ramWave);ramBox.Children.Add(ramSub);
  var ramCard=Card(ramBox);ramCard.Margin=new Thickness(3,0,3,0);ramCard.Padding=new Thickness(18,16,18,16);Grid.SetColumn(ramCard,1);perfGrid.Children.Add(ramCard);
  ramCard.Cursor=System.Windows.Input.Cursors.Hand;ramCard.ToolTip=I18n.T("Bấm để mở Bộ Tối Ưu RAM Chuyên Sâu & Quản Lý Tiến Trình");
  ramCard.MouseLeftButtonUp+=(s,e)=>OpenRamOptimizerDialog();

  // Ô 3: Ổ ĐĨA (Hỗ trợ đa phân vùng C:, D:,...)
  int selectedDriveIdx=0;
  var diskTitle=Label(I18n.T("Ổ ĐĨA HỆ THỐNG & DỮ LIỆU"),14,true);diskTitle.HorizontalAlignment=HorizontalAlignment.Center;
  var driveChips=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,2,0,2)};
  var diskVal=Label("…",28,true);diskVal.HorizontalAlignment=HorizontalAlignment.Center;diskVal.Margin=new Thickness(0,2,0,2);
  var diskBar=new ProgressBar{Height=8,Margin=new Thickness(0,6,0,6),Maximum=100};
  var diskSub=Label(I18n.T("Ổ đĩa hệ thống"),12);diskSub.SetResourceReference(TextBlock.ForegroundProperty,"Muted");diskSub.HorizontalAlignment=HorizontalAlignment.Center;
  var diskBox=new StackPanel();diskBox.Children.Add(diskTitle);diskBox.Children.Add(driveChips);diskBox.Children.Add(diskVal);diskBox.Children.Add(diskBar);diskBox.Children.Add(diskSub);
  var diskCard=Card(diskBox);diskCard.Margin=new Thickness(6,0,0,0);diskCard.Padding=new Thickness(18,16,18,16);Grid.SetColumn(diskCard,2);perfGrid.Children.Add(diskCard);
  diskCard.Cursor=System.Windows.Input.Cursors.Hand;diskCard.ToolTip=I18n.T("Bấm để mở Quét Chuyên Sâu Toàn Diện");
  diskCard.MouseLeftButtonUp+=(s,e)=>NavigatePro("Deep");

  mainStack.Children.Add(perfGrid);

  // Dải ghi chú an toàn chân trang & Băng thông mạng
  var footerCard=new StackPanel();
  var netBadge=Label(I18n.T("🌐 Lưu lượng mạng thời gian thực: ↓ 0 KB/s · ↑ 0 KB/s"),12,true);
  netBadge.HorizontalAlignment=HorizontalAlignment.Center;
  netBadge.Margin=new Thickness(0,0,0,4);
  footerCard.Children.Add(netBadge);

  var footNote=Label(I18n.T("Chế độ bảo vệ dữ liệu tích cực: Toàn bộ tác vụ quét dọn hệ thống đều ưu tiên chuyển vào Thùng rác để khôi phục và sao lưu Registry an toàn trước khi xử lý."),12);
  footNote.HorizontalAlignment=HorizontalAlignment.Center;
  footNote.SetResourceReference(TextBlock.ForegroundProperty,"Muted");
  footerCard.Children.Add(footNote);
  var footBorder=Card(footerCard);
  footBorder.Padding=new Thickness(18,10,18,10);
  mainStack.Children.Add(footBorder);

  var cpuPts=new List<double>();
  var ramPts=new List<double>();
  Action<Canvas,List<double>,double,Color,Color,bool> drawWave=(canvas,history,val,strokeCol,fillCol,push)=>{
   if(push){
    history.Add(val);
    if(history.Count>24)history.RemoveAt(0);
   }
   canvas.Children.Clear();
   double w=canvas.ActualWidth>20?canvas.ActualWidth:(canvas.RenderSize.Width>20?canvas.RenderSize.Width:240);
   double h=32;
   int count=history.Count;
   if(count<2)return;
   var pts=new PointCollection();
   var area=new PointCollection();
   area.Add(new Point(0,h));
   for(int i=0;i<count;i++){
    double x=i*(w/(count-1));
    double y=h-(history[i]/100.0*(h-4))-2;
    if(y<2)y=2;if(y>h-2)y=h-2;
    var pt=new Point(x,y);
    pts.Add(pt);area.Add(pt);
   }
   area.Add(new Point(w,h));
   canvas.Children.Add(new System.Windows.Shapes.Polygon{Points=area,Fill=new SolidColorBrush(fillCol)});
   canvas.Children.Add(new System.Windows.Shapes.Polyline{Points=pts,Stroke=new SolidColorBrush(strokeCol),StrokeThickness=2,StrokeLineJoin=PenLineJoin.Round});
  };

  Action redrawWaves=()=>{
   drawWave(cpuWave,cpuPts,0,Color.FromRgb(40,117,245),Color.FromArgb(45,40,117,245),false);
   drawWave(ramWave,ramPts,0,Color.FromRgb(22,133,105),Color.FromArgb(45,22,133,105),false);
  };

  // Cập nhật real-time metrics & biểu đồ sóng động
  Action updateM=()=>{
   var m=SystemPerformance.Sample();
   cpuVal.Text=m.CpuUsage.ToString("0.0")+"%";cpuBar.Value=m.CpuUsage;cpuSub.Text=I18n.T("Xung nhịp & tiến trình Windows");
   ramVal.Text=m.RamPercent+"%";ramBar.Value=m.RamPercent;ramSub.Text=Features.Size((long)m.UsedRam)+" / "+Features.Size((long)m.TotalRam);
   if(m.Drives!=null&&m.Drives.Count>1){
    driveChips.Visibility=Visibility.Visible;
    if(driveChips.Children.Count!=m.Drives.Count){
     driveChips.Children.Clear();
     for(int idx=0;idx<m.Drives.Count;idx++){
      int dIdx=idx;
      var d=m.Drives[idx];
      var btnChip=new Button{Content="Ổ "+d.Name.TrimEnd('\\'),Padding=new Thickness(7,2,7,2),Margin=new Thickness(2,0,2,0),FontSize=11};
      btnChip.Click+=(s,e)=>{selectedDriveIdx=dIdx;if(onHealthPerfTick!=null)onHealthPerfTick();};
      driveChips.Children.Add(btnChip);
     }
    }
    for(int idx=0;idx<driveChips.Children.Count;idx++){
     var btn=(Button)driveChips.Children[idx];
     var targetStyle=idx==selectedDriveIdx?(Style)window.Resources["Primary"]:(Style)window.FindResource(typeof(Button));
     if(btn.Style!=targetStyle)btn.Style=targetStyle;
    }
   } else {
    driveChips.Visibility=Visibility.Collapsed;
   }
   var activeDrive=(m.Drives!=null&&selectedDriveIdx>=0&&selectedDriveIdx<m.Drives.Count)?m.Drives[selectedDriveIdx]:null;
   long dTotal=activeDrive!=null?activeDrive.Total:m.DiskTotal;
   long dFree=activeDrive!=null?activeDrive.Free:m.DiskFree;
   double dPct=activeDrive!=null?activeDrive.Percent:m.DiskPercent;
   string dName=activeDrive!=null?("Ổ ĐĨA "+activeDrive.Name.TrimEnd('\\')+" ("+activeDrive.VolumeLabel.ToUpper()+")"):I18n.T("Ổ ĐĨA C: (HỆ THỐNG)");
   diskTitle.Text=dName;
   diskVal.Text=Features.Size(dFree)+" "+I18n.T("trống");
   diskBar.Value=dPct;
   diskSub.Text=I18n.T("Tổng")+" "+Features.Size(dTotal)+" · "+I18n.T("đã dùng")+" "+dPct.ToString("0.0")+"%";
   string downStr=m.NetworkDownKBps>=1024.0?(m.NetworkDownKBps/1024.0).ToString("0.0")+" MB/s":m.NetworkDownKBps.ToString("0")+" KB/s";
   string upStr=m.NetworkUpKBps>=1024.0?(m.NetworkUpKBps/1024.0).ToString("0.0")+" MB/s":m.NetworkUpKBps.ToString("0")+" KB/s";
   string battStr=!string.IsNullOrEmpty(m.BatteryStatus)?("  ·  🔋 "+I18n.T("Pin")+": "+m.BatteryStatus):"";
   netBadge.Text=string.Format("🌐 {0}: ↓ {1}  ·  ↑ {2}  ·  ⏱️ {3}: {4}{5}",I18n.T("Lưu lượng mạng"),downStr,upStr,I18n.T("Hoạt động"),m.UptimeString,battStr);
   drawWave(cpuWave,cpuPts,m.CpuUsage,Color.FromRgb(40,117,245),Color.FromArgb(45,40,117,245),true);
   drawWave(ramWave,ramPts,(double)m.RamPercent,Color.FromRgb(22,133,105),Color.FromArgb(45,22,133,105),true);
  };
  onHealthPerfTick=updateM;

  // Khởi tạo điểm ban đầu để biểu đồ hiển thị ngay tức thì
  var initPerf=SystemPerformance.Sample();
  cpuPts.Add(initPerf.CpuUsage);cpuPts.Add(initPerf.CpuUsage);
  ramPts.Add((double)initPerf.RamPercent);ramPts.Add((double)initPerf.RamPercent);
  updateM();

  cpuWave.SizeChanged+=(s,e)=>{if(page=="Health")redrawWaves();};
  ramWave.SizeChanged+=(s,e)=>{if(page=="Health")redrawWaves();};

  if(perfTimer==null){
   perfTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1.0)};
   perfTimer.Tick+=(s,e)=>{if(page=="Health"&&onHealthPerfTick!=null)onHealthPerfTick();};
   perfTimer.Start();
  }

  // Logic lọc tệp Chrome nếu người dùng bỏ tích
  Func<JunkItem,bool> allowItem=item=>{
   if(chkChrome.IsChecked!=true){
    string p=(item.Path??"")+" "+(item.Category??"");
    if(p.IndexOf("Chrome",StringComparison.OrdinalIgnoreCase)>=0)return false;
    if(item.File!=null&&!String.IsNullOrEmpty(item.File.ScopeRoot)&&item.File.ScopeRoot.IndexOf("Chrome",StringComparison.OrdinalIgnoreCase)>=0)return false;
   }
   return true;
  };

  // NÚT 1: ⚡ Quét & Tối ưu toàn diện (1-Click) (Hàng 0, Cột 0)
  var smartCare=WorkButton(I18n.T("⚡ Quét & Tối ưu (1-Click)"),async()=>{
   await WithScan(async token=>{
    var progress=new Progress<string>(s=>{junkStatus.Text=s;Text("Status",s);});
    junkStatus.Text="Đang tối ưu giải phóng RAM đệm & làm sạch DNS Cache…";
    long freedRam=await Task.Run(()=>SystemPerformance.OptimizeMemory());
    bool dnsOk=await Task.Run(()=>SystemPerformance.FlushDnsCache());
    int optDbs=0;long freedDbBytes=0;
    try{optDbs=await Task.Run(()=>SqliteOptimizer.OptimizeAll(out optDbs,out freedDbBytes));}catch{}
    junkStatus.Text="Đang quét tệp tạm (%TEMP%), cache trình duyệt & tệp rác…";
    var found=await Task.Run(()=>JunkOverview.Scan(false,token,progress));
    junkReport=found;
    foreach(var item in junkReport.Items)item.Selected=item.CanClean&&item.Recommended&&allowItem(item);
    int startupCount=0;try{startupCount=Features.Startups().Count;}catch{}
    var chosen=junkReport.Items.Where(x=>x.Selected).ToList();
    long junkBytes=chosen.Sum(x=>x.Bytes);
    var mem=SystemPerformance.GetMemory();
    int score=100;
    if(mem.dwMemoryLoad>80)score-=20;else if(mem.dwMemoryLoad>65)score-=10;
    if(junkBytes>2L*1024*1024*1024)score-=20;else if(junkBytes>500L*1024*1024)score-=10;
    if(startupCount>12)score-=15;else if(startupCount>6)score-=5;
    if(score<30)score=30;
    string healthStatus=score>=85?"RẤT TỐT":(score>=65?"KHÁ - NÊN TỐI ƯU":"CẦN DỌN DẸP NGAY");
    string ramMsg=freedRam>0?"Đã giải phóng "+Features.Size(freedRam)+" RAM. ":"";string dnsMsg=dnsOk?"Đã làm mới bộ nhớ đệm DNS mạng. ":"";
    string dbMsg=optDbs>0?"Đã thu gọn "+optDbs+" database trình duyệt ("+Features.Size(freedDbBytes)+"). ":"";
    string chromeNotice=chkChrome.IsChecked!=true?" (Đã bỏ qua cache Chrome)":"";
    junkStatus.Text="★ SỨC KHỎE: "+score+"/100 ("+healthStatus+")"+chromeNotice+"\n"+ramMsg+dnsMsg+dbMsg+"Phát hiện "+chosen.Count+" mục tệp tạm/rác ("+Features.Size(junkBytes)+"). Bấm 'Dọn mục đã chọn' để hoàn tất!";
    Text("Status","Đã quét toàn diện: Sức khỏe "+score+"/100 · "+Features.Size(junkBytes)+" rác · "+Features.Size(freedRam)+" RAM tối ưu.");
    Engine.Log("SMART_CARE score="+score+" freedRam="+freedRam+" junk="+junkBytes);
   });
  });
  smartCare.Style=(Style)window.Resources["Primary"];
  smartCare.Padding=new Thickness(12,10,12,10);
  smartCare.Margin=new Thickness(0,0,4,6);
  Grid.SetRow(smartCare,0);Grid.SetColumn(smartCare,0);
  btnGrid.Children.Add(smartCare);

  // NÚT 2: 🧹 Dọn mục đã chọn (Hàng 0, Cột 1)
  var btnClean=WorkButton(I18n.T("🧹 Dọn mục đã chọn"),async()=>{
   var chosen=junkReport.Items.Where(x=>x.Selected&&x.CanClean&&allowItem(x)).ToList();
   if(chosen.Count==0){
    junkStatus.Text="Đang tự động quét tệp tạm & cache trình duyệt…";
    await WithScan(async token=>{
     var p=new Progress<string>(s=>junkStatus.Text=s);
     var found=await Task.Run(()=>JunkOverview.Scan(false,token,p));
     junkReport=found;
     foreach(var item in junkReport.Items)item.Selected=item.CanClean&&item.Recommended&&allowItem(item);
     chosen=junkReport.Items.Where(x=>x.Selected&&x.CanClean).ToList();
    });
   }
   if(chosen.Count==0){junkStatus.Text="Hệ thống sạch sẽ! Không có tệp rác nào cần dọn.";return;}
   if(!Confirm("Dọn sạch "+chosen.Count+" tệp tạm và bộ nhớ đệm ("+Features.Size(chosen.Sum(x=>x.Bytes))+")?\n\nCác tệp sẽ được chuyển vào Thùng rác để đảm bảo an toàn."))return;
   int done=0,failed=0;long bytes=0;
   foreach(var item in chosen){
    try{
     JunkOverview.Clean(item);item.Selected=false;done++;bytes+=item.Bytes;
    }catch{failed++;}
    junkStatus.Text="Đang dọn dẹp: "+done+" / "+chosen.Count+" tệp…";
    await Task.Delay(1);
   }
   junkStatus.Text="Hoàn tất! Đã dọn sạch "+done+" tệp tạm ("+Features.Size(bytes)+") an toàn vào Thùng rác.";
   Text("Status","Đã dọn sạch "+done+" tệp tạm ("+Features.Size(bytes)+").");
  });
  btnClean.Padding=new Thickness(12,10,12,10);
  btnClean.Margin=new Thickness(4,0,0,6);
  Grid.SetRow(btnClean,0);Grid.SetColumn(btnClean,1);
  btnGrid.Children.Add(btnClean);

  // NÚT 3: 🧹 Dọn dẹp Registry (Hàng 1, Cột 0)
  var btnReg=ActionButton(I18n.T("🧹 Dọn dẹp Registry"),OpenRegistryCleanerDialog);
  btnReg.Padding=new Thickness(12,10,12,10);
  btnReg.Margin=new Thickness(0,4,4,0);
  Grid.SetRow(btnReg,1);Grid.SetColumn(btnReg,0);
  btnGrid.Children.Add(btnReg);

  // NÚT 4: ⚡ Tối ưu RAM (Hàng 1, Cột 1)
  var btnRam=ActionButton(I18n.T("⚡ Tối ưu RAM"),()=>{
   OpenRamOptimizerDialog();
   updateM();
  });
  btnRam.Padding=new Thickness(12,10,12,10);
  btnRam.Margin=new Thickness(4,4,0,0);
  Grid.SetRow(btnRam,1);Grid.SetColumn(btnRam,1);
  btnGrid.Children.Add(btnRam);
 }
 string JunkSummary(){var remaining=junkReport.Items.Where(x=>x.CanClean).ToList();return remaining.Count+" tệp có thể xem xét · "+Features.Size(remaining.Sum(x=>x.Bytes))+"\nĐã chọn "+remaining.Count(x=>x.Selected)+" tệp · "+Features.Size(remaining.Where(x=>x.Selected).Sum(x=>x.Bytes))+". Phần dư cần xem: "+remaining.Count(x=>!x.Recommended)+" tệp. Quét "+junkReport.Roots+" vị trí rác.";}
 void ShowLongReport(string title,string text){var dialog=new Window{Title=title,Owner=window,Width=850,Height=600,WindowStartupLocation=WindowStartupLocation.CenterOwner,Resources=window.Resources};dialog.SetResourceReference(Window.BackgroundProperty,"Canvas");dialog.Content=new TextBox{Text=text,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(18)};dialog.ShowDialog();}
 async Task VendorActivity(Func<Task> action){var clock=Stopwatch.StartNew();var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};string last="";timer.Tick+=(s,e)=>{if(driverStatus.Text!=last)last=driverStatus.Text.Split(new[]{"\nThời gian:"},StringSplitOptions.None)[0];driverStatus.Text=last+"\nThời gian: "+clock.Elapsed.ToString(@"mm\:ss");};timer.Start();try{await action();}catch(Exception ex){driverStatus.Text="Không hoàn tất: "+ex.Message;throw;}finally{timer.Stop();}}

 void ExportDriverList(){
  try{
   var drivers=Drivers.Inventory();
   var sb=new StringBuilder();
   sb.AppendLine("================================================================================");
   sb.AppendLine("                     BÁO CÁO TOÀN DIỆN DRIVER & THIẾT BỊ                      ");
   sb.AppendLine("                   Dọn Dẹp PC Pro Max - Phiên bản 1.2                          ");
   sb.AppendLine("             Thời gian xuất: "+DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
   sb.AppendLine("================================================================================");
   sb.AppendLine();
   sb.AppendLine("THÔNG TIN HỆ THỐNG:");
   sb.AppendLine("  Hệ điều hành: "+SystemPerformance.GetOsName());
   sb.AppendLine("  Bộ vi xử lý:  "+SystemPerformance.GetCpuName());
   sb.AppendLine("  Bo mạch chủ:  "+VendorDrivers.Board);
   sb.AppendLine("  Card đồ họa:  "+SystemPerformance.GetGpuName());
   sb.AppendLine();
   sb.AppendLine("DANH SÁCH THIẾT BỊ & DRIVER ĐÃ CÀI ĐẶT ("+drivers.Count+" thiết bị):");
   sb.AppendLine(String.Format("{0,-35} | {1,-30} | {2,-15} | {3,-12} | {4}","TÊN THIẾT BỊ","NHÀ CUNG CẤP","PHIÊN BẢN","NGÀY TẠO","PHÂN LỚP"));
   sb.AppendLine(new string('-',120));
   foreach(var d in drivers.OrderBy(x=>x.Class).ThenBy(x=>x.Device)){
    sb.AppendLine(String.Format("{0,-35} | {1,-30} | {2,-15} | {3,-12} | {4}",
     (d.Device??"").Length>35?(d.Device??"").Substring(0,32)+"...":(d.Device??""),
     (d.Vendor??"").Length>30?(d.Vendor??"").Substring(0,27)+"...":(d.Vendor??""),
     d.Version??"",
     d.Date??"",
     d.Class??""));
   }
   string desktopPath=Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
   string outFile=Path.Combine(desktopPath,"BaoCao_Driver_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".txt");
   File.WriteAllText(outFile,sb.ToString(),Encoding.UTF8);
   if(Confirm("ĐÃ XUẤT BÁO CÁO DRIVER THÀNH CÔNG!\n\nFile báo cáo chi tiết "+drivers.Count+" thiết bị đã được lưu tại:\n"+outFile+"\n\nBạn có muốn mở tệp báo cáo xem ngay không?")){
    Process.Start(new ProcessStartInfo(outFile){UseShellExecute=true});
   }
  }catch(Exception ex){Error(ex);}
 }

 void BuildDriverPage(){
  Text("Heading",I18n.T("Driver từ đúng nhà sản xuất."));
  Text("Subtitle",I18n.T("Kiểm kê tại máy — nguồn hãng — cài gói cục bộ có xác minh"));
  var dock=new DockPanel();
  proHost.Children.Add(dock);
  var top=new StackPanel();
  DockPanel.SetDock(top,Dock.Top);
  dock.Children.Add(top);
  var panel=new StackPanel();
  panel.Children.Add(Label(I18n.T("Card màn hình, âm thanh, mạng và thiết bị"),20,true));
  panel.Children.Add(Label(I18n.T("Không kết nối Windows Update, không cập nhật hệ điều hành. Quét để xem thiết bị báo lỗi, phiên bản đang cài và nguồn tải của hãng. Ngày driver cũ không có nghĩa là cần thay.")));
  var actions=new WrapPanel();
  panel.Children.Add(actions);
  driverStatus=Label(driverRecords.Count==0?I18n.T("Bấm Quét thiết bị. Sau đó chọn thiết bị và mở nguồn hãng."):"Đã nạp "+driverRecords.Count+" thiết bị.");
  panel.Children.Add(driverStatus);
  top.Children.Add(Card(panel));

  driversGrid=new DataGrid{IsReadOnly=true,ItemsSource=driverRecords};
  proControls.Add(driversGrid);
  AddColumn(driversGrid,I18n.T("THIẾT BỊ"),"Device",300);
  AddColumn(driversGrid,I18n.T("TRẠNG THÁI"),"Status",200);
  AddColumn(driversGrid,I18n.T("PHIÊN BẢN"),"Version",140);
  AddColumn(driversGrid,I18n.T("NGUỒN CHÍNH THỨC"),"Source",270);
  AddColumn(driversGrid,I18n.T("NHÓM"),"Class",110);
  dock.Children.Add(driversGrid);

  var scan=WorkButton(I18n.T("1. Quét thiết bị"),async()=>{
   await VendorActivity(async()=>{
    driverStatus.Text="Đang đọc phần cứng và trạng thái driver tại máy…";
    driverRecords=await Task.Run(()=>VendorDrivers.Scan());
    driversGrid.ItemsSource=driverRecords;
    driversGrid.SelectedItem=driverRecords.FirstOrDefault(x=>x.Class=="DISPLAY")??driverRecords.FirstOrDefault();
    driverStatus.Text="Đã đọc "+driverRecords.Count+" thiết bị · "+VendorDrivers.Board+"\nChọn thiết bị cần xử lý. Không tuyên bố driver lỗi thời khi chưa đối chiếu với hãng.";
   });
  });
  scan.Style=(Style)window.Resources["Primary"];
  actions.Children.Add(scan);

  actions.Children.Add(ActionButton(I18n.T("2. Mở nguồn hãng của thiết bị"),()=>{
   var record=driversGrid.SelectedItem as DriverRecord;
   if(record==null){driverStatus.Text="Chọn một thiết bị trong bảng trước.";return;}
   if(String.IsNullOrEmpty(record.Url)){driverStatus.Text="Chưa có nguồn hãng được xác minh cho thiết bị này. Sao chép mã phần cứng để tra với nhà sản xuất; không cài gói đoán theo tên.";return;}
   OpenUrl(record.Url);
   driverStatus.Text="Đã mở "+record.Source+". Kiểm tra đúng model và phiên bản Windows trên trang hãng; chưa tải/cài trong ứng dụng.";
  }));

  actions.Children.Add(ActionButton(I18n.T("Mở Device Manager"),()=>OpenNative("devmgmt.msc")));
  actions.Children.Add(ActionButton(I18n.T("📄 Xuất báo cáo Driver"),ExportDriverList));

  var extra=new WrapPanel();
  extra.Children.Add(WorkButton(I18n.T("Cài gói INF đã tải từ hãng"),async()=>{
   var record=driversGrid.SelectedItem as DriverRecord;
   if(record==null){driverStatus.Text="Chọn thiết bị muốn cập nhật trước.";return;}
   if(!Drivers.Administrator()){RequestAdministrator();return;}
   var picker=new Microsoft.Win32.OpenFileDialog{Title="Chọn INF trong gói driver chính thức đã giải nén",Filter="Gói driver (*.inf)|*.inf",CheckFileExists=true};
   if(picker.ShowDialog(window)!=true)return;
   InfPlan plan=null;
   await VendorActivity(async()=>{
    driverStatus.Text="Đang đối chiếu mã phần cứng và chữ ký catalog…";
    plan=await Task.Run(()=>VendorDrivers.Inspect(picker.FileName,record));
   });
   if(!Confirm("Cài gói driver cục bộ này?\nThiết bị đã chọn: "+record.Device+"\n"+plan.Path+"\nINF có mã phần cứng tương ứng và catalog có chữ ký hợp lệ. PnPUtil sẽ kiểm tra gói và xếp hạng; có thể áp dụng cho các thiết bị khác cũng khớp gói.\nSao lưu các driver bên thứ ba trước khi cài; dừng nếu sao lưu lỗi. Lưu công việc trước, có thể gián đoạn màn hình/mạng. Không tự khởi động lại."))return;
   await VendorActivity(async()=>{
    var progress=new Progress<string>(s=>driverStatus.Text=s);
    string report=await Task.Run(()=>VendorDrivers.Install(plan,progress));
    ShowLongReport("Kết quả cài gói driver",report);
    driverStatus.Text="Đã kết thúc thao tác. Xem báo cáo; quét lại thiết bị để đối chiếu phiên bản thực tế.";
   });
  }));

  extra.Children.Add(ActionButton(I18n.T("Sao chép mã phần cứng"),()=>{
   var record=driversGrid.SelectedItem as DriverRecord;
   if(record!=null)Clipboard.SetText(record.Device+"\n"+record.Version+"\n"+String.Join("\n",record.HardwareIds??new string[0])+"\n"+VendorDrivers.Board);
  }));
  extra.Children.Add(ActionButton(I18n.T("Mở quyền quản trị"),RequestAdministrator));
  extra.Children.Add(ActionButton(I18n.T("Mở bản sao driver"),()=>{
   string path=Path.Combine(Engine.Data,"Driver-backups");
   Directory.CreateDirectory(path);
   Process.Start(new ProcessStartInfo("explorer.exe",Maintenance.Quote(path)){UseShellExecute=true});
  }));

  var expander=new Expander{Header=I18n.T("Cài gói driver đã tải / tùy chọn khác"),Content=extra,Margin=new Thickness(0,0,0,12)};
  expander.SetResourceReference(Control.ForegroundProperty,"Ink");
  top.Children.Add(expander);
  top.Children.Add(Label(I18n.T("Nguồn hãng được mở trong trình duyệt; bộ cài EXE dùng giao diện của hãng. Chức năng cài INF không hỗ trợ BIOS/firmware và không ép hạ phiên bản. Chưa có cập nhật tự động mọi hãng bằng một nút."),11));
 }
}