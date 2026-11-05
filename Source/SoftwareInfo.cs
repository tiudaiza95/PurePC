using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Xml.Serialization;
using Microsoft.Win32;

public partial class AppEntry {
 public string DisplayIcon, InstalledOn, Website;
 public long EstimatedBytes; public bool SystemComponent, MeasuredFromDisk;

 public bool IsUwp; public string PackageFullName;

 public string AppType { get { return IsUwp ? "UWP (Store)" : "Win32 (Desktop)"; } }
 public int Number {get;set;}
 public bool Selected {get;set;}
 [XmlIgnore] public List<AppEntry> Sources=new List<AppEntry>();
 [XmlIgnore] public Advisory Advice;
 [XmlIgnore] ImageSource icon;
 [XmlIgnore] public ImageSource Icon {get {return icon??(icon=Icons.ForApp(this));}}
 public long InstallBytes {get{return EstimatedBytes;}}
 public string SizeLabel {get{return EstimatedBytes>0?Features.Size(EstimatedBytes):"Chưa rõ";}}
 public string SizeSource {get{return MeasuredFromDisk?"Đo trực tiếp từ thư mục cài đặt thực tế; chính xác hơn Registry.":(EstimatedBytes>0?"Registry EstimatedSize × 1.024 byte; ước tính do trình cài khai báo, không phải số đo đĩa.":"Không có EstimatedSize đáng tin cậy; chưa đo thư mục.");}}
 public string InstalledDate {get{DateTime d;return DateTime.TryParseExact(InstalledOn,"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d)?d.ToString("dd/MM/yyyy"):"Chưa rõ";}}
 public string Category {get{return Advice==null?"Chưa xác định":Advice.Category;}}
 public string StatusLabel {get{return Advice==null?"Chưa đủ thông tin":Advice.Level;}}
 public bool Caution {get{return Advice==null||Advice.Caution||SystemComponent;}}
 public bool CanUninstall {get{return !Protected&&!String.IsNullOrWhiteSpace(Command);}}
 public bool Protected {get{return Advice!=null&&Advice.Protected;}}
 public string Architecture {get{return IsUwp?"UWP (Universal Windows Platform) · Windows Store":(Hive==RegistryHive.CurrentUser?"Nguồn HKCU dùng chung; chưa xác minh kiến trúc":(View==RegistryView.Registry64?"Nguồn Registry 64-bit":"Nguồn Registry 32-bit")+"; không khẳng định kiến trúc tệp");}}
 public string SourceSummary {get{return Sources.Count+" nguồn đọc · "+Scope;}}
}
public class Advisory {
 public string Category="Chưa xác định",Description="Chưa có mô tả đã xác minh cho ứng dụng này.",Level="Không đủ thông tin",Reason="Chỉ có dữ liệu đăng ký cài đặt tại máy; chưa đủ để kết luận mức độ cần thiết.",Confidence="Thấp — metadata chưa xác minh",Impact="Chưa xác định được phụ thuộc. Liên hệ nhà phát hành trước khi gỡ nếu không rõ vai trò.",Backup="Sao lưu tài liệu, dự án, cấu hình và thông tin bản quyền liên quan trước khi gỡ.",Url="",Source="Registry Uninstall tại máy",Checked="Không có tra cứu trực tuyến",Country="Chưa xác minh";
 public bool Caution=true,Protected;
}
static class Catalog {
 static Advisory Known(string category,string description,string impact,string url,bool caution){return new Advisory{Category=category,Description=description,Impact=impact,Url=url,Source="Tài liệu chính thức: "+url,Checked="24/10/2026 · bản kiến thức offline",Caution=caution,Level=caution?"Thành phần hỗ trợ — thận trọng":"Ứng dụng theo nhu cầu",Reason=caution?"Tài liệu nhà sản xuất xác nhận vai trò hỗ trợ; chưa kiểm kê được mọi ứng dụng phụ thuộc trên máy này.":"Chức năng dành cho người dùng. Chỉ cân nhắc gỡ nếu bạn không cần chức năng này; chưa có dữ liệu tần suất sử dụng.",Confidence="Trung bình — tên và nhà phát hành khớp quy tắc; cần đối chiếu chữ ký"};}
 public static void Enrich(AppEntry a){
  string n=(a.Name??"").ToLowerInvariant(),p=(a.Publisher??"").ToLowerInvariant();Advisory d=new Advisory();
  if(p.Contains("microsoft")&&n.Contains("visual c++"))d=Known("Thư viện dùng chung","Microsoft Visual C++ Redistributable cung cấp thư viện chạy cho phần mềm được xây dựng bằng Visual C++. Nhiều phiên bản/kiến trúc có thể cùng cần thiết.","Ứng dụng phụ thuộc có thể không mở hoặc báo thiếu DLL. Không gỡ chỉ vì có nhiều phiên bản.","https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist",true);
  else if(p.Contains("microsoft")&&(n.Contains(".net")||n.Contains("asp.net")))d=Known("Nền tảng .NET",".NET Runtime chạy ứng dụng .NET; Desktop Runtime hỗ trợ ứng dụng desktop, ASP.NET Core Runtime phục vụ ứng dụng web. SDK dùng để phát triển.","Có thể mất khả năng chạy hoặc phát triển ứng dụng cần đúng phiên bản runtime/SDK. Không suy luận phiên bản cũ là thừa.","https://learn.microsoft.com/en-us/dotnet/core/install/windows",true);
  else if(n.StartsWith("zalo")&&(p.Contains("vng")||p.Contains("zalo")))d=Known("Liên lạc","Zalo PC hỗ trợ nhắn tin, trao đổi nhóm và gửi tệp trên máy tính; đồng bộ tin nhắn với điện thoại.","Không còn dùng Zalo trên máy này. Kiểm tra sao lưu tin nhắn và tệp tải về; không giả định dữ liệu đã đồng bộ đầy đủ.","https://zalo.me/pc",false);
  else if(n.StartsWith("capcut")&&(p.Contains("bytedance")||p.Contains("capcut")))d=Known("Biên tập video","CapCut là công cụ chỉnh sửa video, âm thanh, phụ đề và hiệu ứng. Thường được dùng để dựng và xuất video.","Không còn mở/chỉnh sửa dự án bằng bản CapCut này. Sao lưu dự án và toàn bộ media nguồn, không chỉ video đã xuất.","https://www.capcut.com/",false);
  else if(n.StartsWith("7-zip")&&p.Contains("igor"))d=Known("Nén và giải nén","7-Zip tạo và giải nén tệp lưu trữ, hỗ trợ nhiều định dạng như 7z và ZIP.","Mất tiện ích và tích hợp 7-Zip; các tệp nén hiện có không bị xóa bởi việc gỡ chính thức.","https://www.7-zip.org/",false);
  else if(p.Contains("asus")){
   d=Known("Tiện ích / hỗ trợ phần cứng ASUS","Đây là mục do ASUS khai báo phát hành. Tên có thể chỉ một dịch vụ hoặc tiện ích hỗ trợ thiết bị; chưa có tài liệu đủ chi tiết cho mọi mục ASUS.","Có thể ảnh hưởng phím chức năng, điều khiển thiết bị hoặc tích hợp tiện ích ASUS. Cần đối chiếu đúng model và tên gói.","https://www.asus.com/support/faq/1045676/",true);
   d.Confidence="Thấp đến trung bình — tài liệu ASUS System Control Interface không mô tả mọi dịch vụ ASUS";
  }
  else if(n.StartsWith("unikey"))d=Known("Bộ gõ tiếng Việt","UniKey là bộ gõ tiếng Việt phổ biến hỗ trợ Unicode, TCVN3 và VNI.","Mất công cụ gõ dấu tiếng Việt nếu không có bộ gõ khác thay thế.","https://www.unikey.org/",false);
  else if(n.StartsWith("evkey"))d=Known("Bộ gõ tiếng Việt","EVKey là bộ gõ tiếng Việt hỗ trợ sửa lỗi gõ dấu trên trình duyệt và ứng dụng.","Mất công cụ gõ dấu tiếng Việt nếu không có bộ gõ khác thay thế.","https://evkeyvn.com/",false);
  else if(n.StartsWith("winrar")||n.Contains("winrar"))d=Known("Nén và giải nén","WinRAR là tiện ích nén và giải nén tệp lưu trữ RAR, ZIP.","Mất tiện ích mở tệp RAR chuyên dụng; Windows vẫn hỗ trợ tệp ZIP cơ bản.","https://www.rarlab.com/",false);
  else if(n.StartsWith("telegram")||n.Contains("telegram desktop"))d=Known("Liên lạc","Telegram Desktop hỗ trợ nhắn tin, gọi thoại và trao đổi tệp đám mây.","Không còn truy cập Telegram bằng ứng dụng này; dữ liệu tin nhắn vẫn lưu trên máy chủ Telegram.","https://desktop.telegram.org/",false);
  else if(n.StartsWith("discord"))d=Known("Liên lạc / Cộng đồng","Discord là nền tảng liên lạc giọng nói, video và văn bản cho cộng đồng.","Không còn mở ứng dụng Discord trực tiếp; vẫn có thể đăng nhập trên web.","https://discord.com/",false);
  else if(n.StartsWith("ultraviewer"))d=Known("Điều khiển từ xa","UltraViewer là phần mềm hỗ trợ điều khiển máy tính từ xa.","Mất công cụ kết nối hỗ trợ từ xa qua UltraViewer.","https://ultraviewer.net/",false);
  else if(n.StartsWith("teamviewer"))d=Known("Điều khiển từ xa","TeamViewer là giải pháp truy cập và điều khiển máy tính từ xa.","Mất công cụ kết nối từ xa qua TeamViewer.","https://www.teamviewer.com/",false);
  else if(n.StartsWith("spotify"))d=Known("Nghe nhạc / Giải trí","Spotify hỗ trợ nghe nhạc trực tuyến và podcast.","Không còn ứng dụng nghe nhạc Spotify; thư viện nhạc lưu trên tài khoản.","https://www.spotify.com/",false);
  else if(n.StartsWith("notion"))d=Known("Ghi chú / Năng suất","Notion là ứng dụng ghi chú, tài liệu và quản lý công việc.","Không còn ứng dụng Notion tại máy; dữ liệu vẫn đồng bộ trên đám mây.","https://www.notion.so/",false);
  else if(n.Contains("google chrome"))d=Known("Trình duyệt web","Google Chrome là trình duyệt web bảo mật do Google phát triển.","Mất trình duyệt Google Chrome; cần đảm bảo có trình duyệt khác trước khi gỡ.","https://www.google.com/chrome/",false);
  else if(n.Contains("cốc cốc")||n.Contains("coc coc"))d=Known("Trình duyệt web","Trình duyệt Cốc Cốc tối ưu cho người dùng Việt Nam.","Mất trình duyệt Cốc Cốc; cần đảm bảo có trình duyệt khác trước khi gỡ.","https://coccoc.com/",false);
  if(n.Contains("driver")||n.Contains("display audio")||n.Contains("graphics driver")||n.Contains("antivirus")||n.Contains("defender")||n.Contains("security update")){
   d.Caution=true;d.Protected=true;d.Level="Driver / bảo mật — chỉ xem";d.Category="Thành phần hệ thống / driver";d.Reason="Tên gói cho thấy khả năng liên quan driver hoặc bảo mật; chưa đủ dữ liệu để gỡ an toàn. Bản này chặn thao tác gỡ trực tiếp.";
  }
  if(a.SystemComponent){d.Caution=true;if(!d.Protected)d.Level="Thành phần phụ — gỡ riêng";d.Reason="SystemComponent=1 chỉ là cờ ẩn trong danh sách Windows, không chứng minh đây là thành phần hệ điều hành. Chỉ dùng bộ gỡ chính thức; cần kiểm tra phụ thuộc.";if(p.Contains("microsoft")&&(n.StartsWith("windows ")||n.Contains("servicing stack")||n.Contains("security update"))){d.Protected=true;d.Level="Windows cốt lõi — chỉ xem";}}
  if(a.EstimatedBytes<=0&&!String.IsNullOrWhiteSpace(a.Location)&&Directory.Exists(a.Location)){try{long total=0;int fCount=0;foreach(var file in Directory.EnumerateFiles(a.Location,"*",SearchOption.AllDirectories)){try{var fi=new FileInfo(file);if((fi.Attributes&(FileAttributes.ReparsePoint|FileAttributes.System))==0)total+=fi.Length;if(++fCount>2500)break;}catch{}}if(total>0){a.EstimatedBytes=total;a.MeasuredFromDisk=true;}}catch{}}
  a.Advice=d;
 }
}
static partial class Engine {
 public static List<AppEntry> RawEntries=new List<AppEntry>();
 public static string Identity(AppEntry x){return String.Join("",new[]{x.Hive.ToString(),x.Hive==RegistryHive.CurrentUser?"HKCU-shared":x.View.ToString(),x.Key,x.Name,x.Version,x.Command,x.Location,x.Publisher,x.SystemComponent.ToString()}.Select(v=>(v??"").Length+":"+(v??"")));}
 public static List<AppEntry> GroupIdentical(IEnumerable<AppEntry> entries){return entries.GroupBy(Identity,StringComparer.Ordinal).Select(g=>{var first=g.First();first.Sources=g.ToList();return first;}).OrderBy(x=>x.Name).ToList();}
 public static void ReadMetadata(AppEntry a,RegistryKey k){a.DisplayIcon=Convert.ToString(k.GetValue("DisplayIcon"));a.InstalledOn=Convert.ToString(k.GetValue("InstallDate"));a.Website=Convert.ToString(k.GetValue("URLInfoAbout"));a.SystemComponent=Convert.ToString(k.GetValue("SystemComponent"))=="1";a.WindowsInstaller=Convert.ToString(k.GetValue("WindowsInstaller"))=="1";MsiSupport.FillCommand(a);long kb;if(Int64.TryParse(Convert.ToString(k.GetValue("EstimatedSize")),out kb)&&kb>0&&kb<Int64.MaxValue/1024)a.EstimatedBytes=kb*1024;}
 public static bool LocalFile(string path){try{return Path.IsPathRooted(path)&&!path.StartsWith("\\\\")&&new DriveInfo(Path.GetPathRoot(path)).DriveType!=DriveType.Network;}catch{return false;}}
 public static string KnownExecutable(AppEntry a){string s=Environment.ExpandEnvironmentVariables(a.DisplayIcon??"").Trim();int comma=s.LastIndexOf(',');int index;if(comma>0&&Int32.TryParse(s.Substring(comma+1),out index))s=s.Substring(0,comma);s=s.Trim('"');if(LocalFile(s)&&File.Exists(s))return s;try{var p=Uninstaller(a).FileName;if(LocalFile(p)&&File.Exists(p)&&!Path.GetFileName(p).Equals("msiexec.exe",StringComparison.OrdinalIgnoreCase))return p;}catch{}return "";}
}
static class Icons {
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern uint ExtractIconEx(string file,int index,IntPtr[] large,IntPtr[] small,uint count);
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr h);
 static ImageSource fallbackIcon;
 static readonly object iconSync=new object();
 public static ImageSource Fallback{get{if(fallbackIcon!=null)return fallbackIcon;lock(iconSync){if(fallbackIcon==null){var fb=Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Application.Handle,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(28,28));fb.Freeze();fallbackIcon=fb;}return fallbackIcon;}}}
 public static ImageSource ForApp(AppEntry a){
  if(a.IsUwp){
   string iconPath=UwpManager.FindUwpIcon(a.Location);
   if(!String.IsNullOrEmpty(iconPath)&&File.Exists(iconPath)){
    try{
     var bi=new BitmapImage();
     bi.BeginInit();
     bi.UriSource=new Uri(iconPath);
     bi.DecodePixelWidth=28;
     bi.DecodePixelHeight=28;
     bi.CacheOption=BitmapCacheOption.OnLoad;
     bi.EndInit();
     bi.Freeze();
     return bi;
    }catch{}
   }
  }
  IntPtr h=IntPtr.Zero;try{string path=Engine.KnownExecutable(a);int index=0;string raw=a.DisplayIcon??"";int comma=raw.LastIndexOf(',');if(comma>0)Int32.TryParse(raw.Substring(comma+1),out index);var icons=new IntPtr[1];if(path!="")ExtractIconEx(path,index,icons,null,1);h=icons[0];if(h!=IntPtr.Zero){var bmp=Imaging.CreateBitmapSourceFromHIcon(h,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(28,28));bmp.Freeze();return bmp;}}catch{}finally{if(h!=IntPtr.Zero)DestroyIcon(h);}return Fallback;}
}
static class SignatureCheck {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct FileInfoNative{public uint Size;public IntPtr Path,File,Subject;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct TrustData{public uint Size;public IntPtr Callback,Client;public uint UI,Revocation,Choice;public IntPtr File;public uint State;public IntPtr StateData,Url;public uint Flags,Context;}
 [DllImport("wintrust.dll",ExactSpelling=true,CharSet=CharSet.Unicode)]static extern int WinVerifyTrust(IntPtr hwnd,[In]ref Guid action,[In]ref TrustData data);
 public static string Check(string path){bool trusted;return Check(path,out trusted);}
 public static string Check(string path,out bool trusted){trusted=false;if(String.IsNullOrEmpty(path))return "Chưa có tệp thực thi để kiểm tra.";IntPtr str=IntPtr.Zero,mem=IntPtr.Zero;try{
   str=Marshal.StringToCoTaskMemUni(path);var f=new FileInfoNative{Size=(uint)Marshal.SizeOf(typeof(FileInfoNative)),Path=str};mem=Marshal.AllocHGlobal(Marshal.SizeOf(f));Marshal.StructureToPtr(f,mem,false);
   var data=new TrustData{Size=(uint)Marshal.SizeOf(typeof(TrustData)),UI=2,Choice=1,File=mem,Flags=0x1000};Guid action=new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");int result=WinVerifyTrust(new IntPtr(-1),ref action,ref data);trusted=result==0;
   string signer="";try{using(var cert=new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))signer="\nChủ thể: "+cert.Subject;}catch{}
   return (result==0?"Windows xác minh chữ ký hợp lệ theo dữ liệu cục bộ.":"Chưa xác minh hợp lệ / không có chữ ký được nhận diện. Mã: 0x"+result.ToString("X8"))+signer+"\nKhông truy vấn thu hồi chứng thư qua mạng. Kết quả chỉ áp dụng tệp: "+path;
  }catch(Exception ex){return "Không xác minh được: "+ex.Message;}finally{if(mem!=IntPtr.Zero)Marshal.FreeHGlobal(mem);if(str!=IntPtr.Zero)Marshal.FreeCoTaskMem(str);}}
}
