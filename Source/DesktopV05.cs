using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

partial class DesktopApp {
 void BuildDiskTools(){
  Text("Heading",I18n.T("Chăm sóc & Quản lý ổ đĩa toàn diện"));
  Text("Subtitle",I18n.T("6 bộ công cụ chuyên sâu tối ưu hóa lưu trữ, kiểm tra lỗi và quản lý phân vùng Windows"));
  proHost.Children.Clear();

  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  proHost.Children.Add(scroll);

  var root=new StackPanel{Margin=new Thickness(0,0,0,16)};
  scroll.Content=root;

  var grid=new Grid{Margin=new Thickness(0,0,0,12)};
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  root.Children.Add(grid);

  // HÀNG 1 - CARD 1: Dọn dung lượng Windows (Disk Cleanup)
  var c1=new StackPanel();
  var t1=Label(I18n.T("💾 DỌN DUNG LƯỢNG HỆ THỐNG"),14,true);t1.HorizontalAlignment=HorizontalAlignment.Center;t1.Margin=new Thickness(0,0,0,6);c1.Children.Add(t1);
  c1.Children.Add(Label("Mở công cụ Disk Cleanup để dọn sạch các tệp hệ thống cũ, bản sao Windows Update trước đó, bộ nhớ đệm và tệp dump dung lượng lớn.",11));
  var b1=ActionButton(I18n.T("Mở Disk Cleanup"),()=>OpenNative("cleanmgr.exe"));
  b1.Margin=new Thickness(0,10,0,0);b1.Padding=new Thickness(14,8,14,8);b1.HorizontalAlignment=HorizontalAlignment.Left;
  c1.Children.Add(b1);
  var card1=Card(c1);card1.MinHeight=175;card1.Margin=new Thickness(0,0,6,12);
  Grid.SetRow(card1,0);Grid.SetColumn(card1,0);
  grid.Children.Add(card1);

  // HÀNG 1 - CARD 2: Tối ưu HDD & SSD (Defrag / Trim)
  var c2=new StackPanel();
  var t2=Label(I18n.T("⚡ TỐI ƯU HÓA HDD VÀ SSD"),14,true);t2.HorizontalAlignment=HorizontalAlignment.Center;t2.Margin=new Thickness(0,0,0,6);c2.Children.Add(t2);
  c2.Children.Add(Label("Tối ưu hóa ổ cứng thể rắn SSD (lệnh Trim) và chống phân mảnh ổ đĩa HDD truyền thống theo thuật toán bảo toàn tuổi thọ của Microsoft.",11));
  var b2=ActionButton(I18n.T("Mở Tối ưu ổ đĩa"),()=>OpenNative("dfrgui.exe"));
  b2.Margin=new Thickness(0,10,0,0);b2.Padding=new Thickness(14,8,14,8);b2.HorizontalAlignment=HorizontalAlignment.Left;
  c2.Children.Add(b2);
  var card2=Card(c2);card2.MinHeight=175;card2.Margin=new Thickness(6,0,0,12);
  Grid.SetRow(card2,0);Grid.SetColumn(card2,1);
  grid.Children.Add(card2);

  // HÀNG 2 - CARD 3: Kiểm tra lỗi hệ thống tập tin (chkdsk)
  var c3=new StackPanel();
  var t3=Label(I18n.T("🔍 KIỂM TRA LỖI TẬP TIN"),14,true);t3.HorizontalAlignment=HorizontalAlignment.Center;t3.Margin=new Thickness(0,0,0,6);c3.Children.Add(t3);
  c3.Children.Add(Label("Quét kiểm tra tính toàn vẹn của phân vùng và cấu trúc tệp NTFS/FAT32 trực tuyến không cần khởi động lại máy, phát hiện sớm bad sector.",11));
  var chkStatus=Label("Sẵn sàng kiểm tra phân vùng hệ thống.",11);
  chkStatus.Margin=new Thickness(0,6,0,8);
  c3.Children.Add(chkStatus);
  var b3Row=new WrapPanel();
  var b3Scan=WorkButton(I18n.T("🔍 Quét kiểm tra ổ C:"),async()=>{
   if(!Engine.IsAdministrator()){
    chkStatus.Text="⚠ Cần quyền Administrator để quét chkdsk trực tuyến.";
    MessageBox.Show(window,"Lệnh chkdsk C: /scan yêu cầu quyền Quản trị viên (Run as Administrator).\nHãy bấm nút nâng quyền trong mục Công cụ Pro hoặc nhấp chuột phải chọn Run as administrator.","Yêu cầu quyền Administrator",MessageBoxButton.OK,MessageBoxImage.Warning);
    return;
   }
   chkStatus.Text="Đang tiến hành quét phân vùng C: ở chế độ an toàn (chỉ đọc)…";
   try{
    int exitCode=0;
    string output=await Task.Run(()=>{
     var psi=new ProcessStartInfo("chkdsk.exe","C: /scan"){
      UseShellExecute=false,
      CreateNoWindow=true,
      RedirectStandardOutput=true,
      RedirectStandardError=true
     };
     using(var p=Process.Start(psi)){
      if(p==null){exitCode=-1;return "Không thể khởi động chkdsk.";}
      var o=p.StandardOutput.ReadToEndAsync();
      var e=p.StandardError.ReadToEndAsync();
      p.WaitForExit();
      Task.WaitAll(new Task[]{o,e},3000);
      exitCode=p.ExitCode;
      return ((o.Status==TaskStatus.RanToCompletion?o.Result:"")+"\n"+(e.Status==TaskStatus.RanToCompletion?e.Result:"")).Trim();
     }
    });
    chkStatus.Text=exitCode==0?"Quét hoàn tất phân vùng C:!":"Quét kết thúc (mã: "+exitCode+"). Xem chi tiết báo cáo.";
    ShowLongReport("Kết quả kiểm tra lỗi ổ C: (chkdsk)",output);
   }catch(Exception ex){
    chkStatus.Text="Lỗi: "+ex.Message;
   }
  });
  b3Scan.Style=(Style)window.Resources["Primary"];
  b3Scan.Padding=new Thickness(14,8,14,8);
  b3Row.Children.Add(b3Scan);
  var b3Win=ActionButton(I18n.T("Kiểm tra phân vùng Windows"),()=>Process.Start(new ProcessStartInfo("cmd.exe","/k chkdsk C:"){UseShellExecute=true}));
  b3Win.Padding=new Thickness(12,8,12,8);
  b3Row.Children.Add(b3Win);
  c3.Children.Add(b3Row);
  var card3=Card(c3);card3.MinHeight=175;card3.Margin=new Thickness(0,0,6,12);
  Grid.SetRow(card3,1);Grid.SetColumn(card3,0);
  grid.Children.Add(card3);

  // HÀNG 2 - CARD 4: Quản lý phân vùng & Lưu trữ (Storage & Partition Manager)
  var c4=new StackPanel();
  var t4=Label(I18n.T("▤ QUẢN LÝ PHÂN VÙNG & LƯU TRỮ"),14,true);t4.HorizontalAlignment=HorizontalAlignment.Center;t4.Margin=new Thickness(0,0,0,6);c4.Children.Add(t4);
  string drivesSummary="Các ổ đĩa trên máy: ";
  try{
   foreach(var d in DriveInfo.GetDrives().Where(d=>d.IsReady)){
    drivesSummary+=d.Name+" ("+Features.Size(d.AvailableFreeSpace)+" trống / "+Features.Size(d.TotalSize)+")  ";
   }
  }catch{}
  c4.Children.Add(Label(drivesSummary,11));
  var b4Row=new WrapPanel{Margin=new Thickness(0,10,0,0)};
  var b4Part=ActionButton(I18n.T("Quản lý phân vùng"),()=>OpenNative("diskmgmt.msc"));
  b4Part.Padding=new Thickness(12,8,12,8);
  b4Row.Children.Add(b4Part);
  var b4Sense=ActionButton(I18n.T("Cài đặt lưu trữ Windows"),()=>{
   try{Process.Start(new ProcessStartInfo("ms-settings:storagesense"){UseShellExecute=true});}catch{}
  });
  b4Sense.Padding=new Thickness(12,8,12,8);
  b4Row.Children.Add(b4Sense);
  c4.Children.Add(b4Row);
  var card4=Card(c4);card4.MinHeight=175;card4.Margin=new Thickness(6,0,0,12);
  Grid.SetRow(card4,1);Grid.SetColumn(card4,1);
  grid.Children.Add(card4);

  // HÀNG 3 - CARD 5: Phân tích chiếm dụng dung lượng (Disk Space Analyzer)
  var c5=new StackPanel();
  var t5=Label(I18n.T("▶ PHÂN TÍCH CHIẾM DỤNG Ổ ĐĨA"),14,true);t5.HorizontalAlignment=HorizontalAlignment.Center;t5.Margin=new Thickness(0,0,0,6);c5.Children.Add(t5);
  c5.Children.Add(Label("Phân tích nhanh các thư mục lớn trên ổ hệ thống (Windows, Users, Program Files) để dễ dàng xác định nguồn ngốn dung lượng nhiều nhất.",11));
  var c5Status=Label("Sẵn sàng phân tích cấu trúc thư mục ổ C:.",11);
  c5Status.Margin=new Thickness(0,6,0,8);
  c5.Children.Add(c5Status);
  var b5Row=new WrapPanel();
  var b5Scan=WorkButton(I18n.T("▶ Phân tích thư mục ổ C:"),async()=>{
   c5Status.Text="Đang tính toán dung lượng các thư mục chính ổ C:…";
   try{
    string report=await Task.Run(()=>{
     var sb=new System.Text.StringBuilder();
     sb.AppendLine("=== PHÂN TÍCH CẤU TRÚC DUNG LƯỢNG Ổ C: ===");
     sb.AppendLine("Thời điểm quét: "+DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
     sb.AppendLine();
     string[] targets={
      Environment.GetFolderPath(Environment.SpecialFolder.Windows),
      Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
      Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp")
     };
     foreach(var t in targets.Where(Directory.Exists)){
      try{
       long size=0;int count=0;
       var stack=new Stack<string>();stack.Push(t);
       while(stack.Count>0 && count<=15000){
        string cur=stack.Pop();
        try{
         if((File.GetAttributes(cur)&FileAttributes.ReparsePoint)!=0)continue;
         foreach(var f in Directory.EnumerateFiles(cur)){
          try{size+=new FileInfo(f).Length;if(++count>15000)break;}catch{}
         }
         if(count<=15000)foreach(var sub in Directory.EnumerateDirectories(cur))stack.Push(sub);
        }catch{}
       }
       sb.AppendLine(Path.GetFileName(t).PadRight(25)+" : "+Features.Size(size)+(count>15000?" (mẫu 15.000 tệp đầu)":"")+" ["+t+"]");
      }catch(Exception ex){
       sb.AppendLine(Path.GetFileName(t).PadRight(25)+" : (Không có quyền đọc: "+ex.Message+")");
      }
     }
     return sb.ToString();
    });
    c5Status.Text="Đã phân tích xong các thư mục chính ổ C:!";
    ShowLongReport("Báo cáo phân tích thư mục ổ C:",report);
   }catch(Exception ex){
    c5Status.Text="Lỗi: "+ex.Message;
   }
  });
  b5Scan.Padding=new Thickness(12,8,12,8);
  b5Row.Children.Add(b5Scan);
  var b5Temp=ActionButton(I18n.T("Xem tệp tạm thời Windows"),()=>{
   try{Process.Start(new ProcessStartInfo("ms-settings:storagesense-temporaryfiles"){UseShellExecute=true});}catch{}
  });
  b5Temp.Padding=new Thickness(12,8,12,8);
  b5Row.Children.Add(b5Temp);
  c5.Children.Add(b5Row);
  var card5=Card(c5);card5.MinHeight=175;card5.Margin=new Thickness(0,0,6,0);
  Grid.SetRow(card5,2);Grid.SetColumn(card5,0);
  grid.Children.Add(card5);

  // HÀNG 3 - CARD 6: Quản lý Ngủ đông & Nén Compact OS (Hibernation & Compact OS)
  var c6=new StackPanel();
  var t6=Label(I18n.T("⚡ TỐI ƯU HIBERNATE & COMPACT OS"),14,true);t6.HorizontalAlignment=HorizontalAlignment.Center;t6.Margin=new Thickness(0,0,0,6);c6.Children.Add(t6);
  c6.Children.Add(Label("Quản lý tệp ngủ đông hiberfil.sys (tiết kiệm 8–16 GB RAM size) và kích hoạt cơ chế nén hệ thống Compact OS giúp tiết kiệm thêm 3–5 GB trên ổ C:.",11));
  var c6Status=Label("Sẵn sàng kiểm tra tính năng nén và tệp ngủ đông.",11);
  c6Status.Margin=new Thickness(0,6,0,8);
  c6.Children.Add(c6Status);
  var b6Row=new WrapPanel();
  var b6Hiber=WorkButton(I18n.T("⚡ Tắt Ngủ đông"),async()=>{
   if(!Confirm("Tắt tính năng Ngủ đông (Hibernate)?\n\nThao tác này sẽ xóa ngay lập tức tệp hiberfil.sys trên ổ C: (thường bằng 40–100% dung lượng RAM) giúp tăng dung lượng trống tức thì.\n(Bạn có thể bật lại bất cứ lúc nào qua cmd: powercfg -h on)"))return;
   c6Status.Text="Đang tắt tính năng ngủ đông để giải phóng dung lượng…";
   try{
    int exitCode=-1;string errMsg="";
    await Task.Run(()=>{
     var psi=new ProcessStartInfo("powercfg.exe","-h off"){CreateNoWindow=true,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
     using(var p=Process.Start(psi)){
      if(p==null)return;
      var o=p.StandardOutput.ReadToEndAsync();
      var e=p.StandardError.ReadToEndAsync();
      p.WaitForExit(5000);
      Task.WaitAll(new Task[]{o,e},1500);
      exitCode=p.ExitCode;
      errMsg=((o.Status==TaskStatus.RanToCompletion?o.Result:"")+" "+(e.Status==TaskStatus.RanToCompletion?e.Result:"")).Trim();
     }
    });
    if(exitCode==0){
     c6Status.Text="✓ Đã tắt tính năng ngủ đông! Đã giải phóng dung lượng ổ C:.";
     MessageBox.Show(window,"Đã tắt Hibernate thành công! Tệp hiberfil.sys trên ổ C: đã được giải phóng.","Thành công",MessageBoxButton.OK,MessageBoxImage.Information);
    }else{
     c6Status.Text="⚠ Cần quyền Administrator để tắt Hibernate.";
     MessageBox.Show(window,"Không thể tắt Hibernate (yêu cầu quyền Quản trị viên — Run as Administrator).\n"+errMsg,"Yêu cầu quyền Administrator",MessageBoxButton.OK,MessageBoxImage.Warning);
    }
   }catch(Exception ex){
    c6Status.Text="Cần quyền Administrator để thay đổi: "+ex.Message;
   }
  });
  b6Hiber.Padding=new Thickness(12,8,12,8);
  b6Row.Children.Add(b6Hiber);
  var b6Compact=WorkButton(I18n.T("⚡ Kiểm tra nén Compact OS"),async()=>{
   c6Status.Text="Đang kiểm tra trạng thái Compact OS…";
   try{
    string res=await Task.Run(()=>{
     var psi=new ProcessStartInfo("compact.exe","/compactos:query"){CreateNoWindow=true,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
     using(var p=Process.Start(psi)){
      if(p==null)return "Không thể khởi chạy compact.exe.";
      var o=p.StandardOutput.ReadToEndAsync();
      var e=p.StandardError.ReadToEndAsync();
      p.WaitForExit(5000);
      Task.WaitAll(new Task[]{o,e},1500);
      return ((o.Status==TaskStatus.RanToCompletion?o.Result:"")+"\n"+(e.Status==TaskStatus.RanToCompletion?e.Result:"")).Trim();
     }
    });
    c6Status.Text="Kiểm tra Compact OS hoàn tất.";
    MessageBox.Show(window,"TRẠNG THÁI NÉN HỆ ĐIỀU HÀNH (COMPACT OS):\n\n"+res,"Thông tin Compact OS",MessageBoxButton.OK,MessageBoxImage.Information);
   }catch(Exception ex){
    c6Status.Text="Lỗi: "+ex.Message;
   }
  });
  b6Compact.Padding=new Thickness(12,8,12,8);
  b6Row.Children.Add(b6Compact);
  c6.Children.Add(b6Row);
  var card6=Card(c6);card6.MinHeight=175;card6.Margin=new Thickness(6,0,0,0);
  Grid.SetRow(card6,2);Grid.SetColumn(card6,1);
  grid.Children.Add(card6);

  root.Children.Add(Label("Toàn bộ 6 công cụ trên đều vận hành dựa trên engine chuẩn gốc của Windows, đảm bảo an toàn tuyệt đối cho dữ liệu và tuổi thọ phần cứng.",11));
 }
 void StateV05(){}
}
