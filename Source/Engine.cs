using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;

public partial class AppEntry {
 public string Name, Publisher, Command, Location, Key, Version;
 public string Title { get { return Name; } }
 public string Vendor { get { return String.IsNullOrEmpty(Publisher) ? I18n.T("Không có nhà phát hành") : Publisher; } }
 public string Release { get { return String.IsNullOrEmpty(Version) ? "—" : Version; } }
 public string Scope { get { return Hive==RegistryHive.CurrentUser ? I18n.T("Cá nhân") : I18n.T("Toàn máy"); } }
 
 public RegistryHive Hive; public RegistryView View;
 public string Id { get { return Hive+"|"+View+"|"+Key; } }
}
partial class Remnant {
 public bool Selected {get;set;}
 public string DisplayPath { get { return (IsRegistry ? "HKCU\\" : "")+Path; } }
 public string Kind { get { return IsRegistry?I18n.T("Registry · chỉ xem"):I18n.T("Thư mục · có thể chứa dữ liệu"); } }
 public string Path; public bool IsRegistry; public RegistryHive Hive; public RegistryView View;
 public override string ToString() { return (IsRegistry ? "Registry: " : "Thư mục: ")+Path; }
}
static partial class Engine {
 public const string AppVersion = "1.2";
 public const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
 public static string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PurePC");
 public static bool Testing;
 public static void Log(string s) { if(Testing)return; Directory.CreateDirectory(Data); File.AppendAllText(Path.Combine(Data,"activity.log"),DateTime.Now.ToString("s")+" "+s+Environment.NewLine,Encoding.UTF8); }
 public static void Remember(AppEntry a) { Directory.CreateDirectory(Data); using(var f=File.Create(Path.Combine(Data,"last-app.xml")))new System.Xml.Serialization.XmlSerializer(typeof(AppEntry)).Serialize(f,a); RememberHistory(a); }
 public static AppEntry Recall() { try { using(var f=File.OpenRead(Path.Combine(Data,"last-app.xml")))return (AppEntry)new System.Xml.Serialization.XmlSerializer(typeof(AppEntry)).Deserialize(f); }catch{return null;} }
 public static string Norm(string s) { return Regex.Replace((s??"").ToLowerInvariant(),@"[^\p{L}\p{Nd}]",""); }
 public static string[] AppNames(AppEntry a) { string plain=a.Name??""; if(!String.IsNullOrWhiteSpace(a.Version))plain=Regex.Replace(plain,@"\s+\(?"+Regex.Escape(a.Version)+@"\)?$","").Trim();return new[]{a.Name,plain}.Where(Specific).Select(Norm).Distinct().ToArray(); }
 public static bool Specific(string s) { string n=Norm(s); return n.Length>=4 && !new[]{"data","temp","user","logs","apps","cache","caches","software","programfiles","windows","microsoft","commonfiles","application","applications","programs","users","desktop","downloads","documents","appdata","local","roaming","packages","google","mozilla","adobe","steam","shared","system","system32","purepc"}.Contains(n); }
 public static int DuplicateCount;
 public static List<AppEntry> Deduplicate(IEnumerable<AppEntry> entries) {
  // HKCU Uninstall is shared by WOW64 views. Collapse only identical registry identities and metadata.
  return GroupIdentical(entries);
 }
 static List<AppEntry> cachedApps;
 public static void InvalidateAppCache() { cachedApps = null; UwpManager.InvalidateCache(); }
 public static List<AppEntry> Apps(bool forceRefresh = false) {
  if (cachedApps != null && !forceRefresh && !Testing) return cachedApps;
  var items=new List<AppEntry>();
  foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine}) foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32}) {
   using(var root=RegistryKey.OpenBaseKey(hive,view)) using(var parent=root.OpenSubKey(Uninstall)) {
    if(parent==null)continue;
    foreach(string key in parent.GetSubKeyNames()) try { using(var k=parent.OpenSubKey(key)) {
     if(k==null)continue;
     string name=Convert.ToString(k.GetValue("DisplayName")), command=Convert.ToString(k.GetValue("UninstallString"));
     if(String.IsNullOrWhiteSpace(name))continue;
     var entry=new AppEntry{Name=name,Publisher=Convert.ToString(k.GetValue("Publisher")),Command=command,Location=Convert.ToString(k.GetValue("InstallLocation")),Version=Convert.ToString(k.GetValue("DisplayVersion")),Key=Uninstall+"\\"+key,Hive=hive,View=view}; ReadMetadata(entry,k);items.Add(entry);
    }}catch(System.Security.SecurityException){}catch(UnauthorizedAccessException){}
   }
  }
  RawEntries=items;var unique=Deduplicate(items);DuplicateCount=items.Count-unique.Count;foreach(var app in unique)Catalog.Enrich(app);if(!Testing){try{var uwp=UwpManager.GetInstalledApps(forceRefresh);unique.AddRange(uwp);}catch{}}if(!Testing)cachedApps=unique;return unique;
 }
 public static bool Present(AppEntry a) { if(a.IsUwp)return UwpManager.IsPackageInstalled(a.PackageFullName); using(var r=RegistryKey.OpenBaseKey(a.Hive,a.View))using(var k=r.OpenSubKey(a.Key))return k!=null; }
 public static bool Within(string path,string parent) { return path.Equals(parent,StringComparison.OrdinalIgnoreCase)||path.StartsWith(parent.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase); }
 public static bool SafeFolder(string path,AppEntry a,List<AppEntry> apps) {
  try {
   path=Path.GetFullPath(path).TrimEnd('\\');
   if(!Directory.Exists(path)||!AppNames(a).Contains(Norm(Path.GetFileName(path))))return false;
   string[] roots={Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs"),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)};
   // Only direct children of standard application locations; never arbitrary registry paths.
   string parent=Path.GetDirectoryName(path);
   if(!roots.Any(r=>!String.IsNullOrEmpty(r)&&String.Equals(r.TrimEnd('\\'),parent,StringComparison.OrdinalIgnoreCase)))return false;
   if(Within(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),path))return false;
   if(apps.Any(x=>x.Id!=a.Id&&!String.IsNullOrWhiteSpace(x.Location)&&(Within(Path.GetFullPath(x.Location).TrimEnd('\\'),path)||Within(path,Path.GetFullPath(x.Location).TrimEnd('\\')))))return false;
   if(apps.Any(x=>x.Id!=a.Id&&AppNames(x).Intersect(AppNames(a)).Any()))return false;
   foreach(var other in apps.Where(x=>x.Id!=a.Id))try{if(Within(Uninstaller(other).FileName,path))return false;}catch{}
   // Refuse junctions/symlinks anywhere in a proposed folder.
   var stack=new Stack<string>();stack.Push(path); int count=0;
   while(stack.Count>0) { string d=stack.Pop(); if((File.GetAttributes(d)&FileAttributes.ReparsePoint)!=0)return false;
    foreach(string f in Directory.EnumerateFileSystemEntries(d)) { if(++count>100000)return false;var attr=File.GetAttributes(f);if((attr&FileAttributes.ReparsePoint)!=0)return false;if((attr&FileAttributes.Directory)!=0)stack.Push(f); }
   }
   return true;
  }catch{return false;}
 }
 public static List<Remnant> Scan(AppEntry a) {
  if(Present(a))throw new Exception("Windows vẫn ghi nhận phần mềm này đã cài. Hãy hoàn tất trình gỡ (hoặc khởi động lại máy nếu được yêu cầu), rồi quét lại.");
  var result=new List<Remnant>();var apps=Apps();if(AppNames(a).Length==0)return result;
  string[] roots={Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs"),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)};
  foreach(string root in roots.Where(x=>!String.IsNullOrEmpty(x)).Distinct(StringComparer.OrdinalIgnoreCase)) try {
   foreach(string d in Directory.EnumerateDirectories(root))if(SafeFolder(d,a,apps))result.Add(new Remnant{Path=d});
  }catch(UnauthorizedAccessException){}
  // Only exact app-name keys in the current user's Software branch.
  if(!apps.Any(x=>AppNames(x).Intersect(AppNames(a)).Any())) foreach(var view in new[]{RegistryView.Registry64}) {
   using(var r=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,view))using(var k=r.OpenSubKey("Software")) {
    if(k!=null)foreach(string name in k.GetSubKeyNames())if(AppNames(a).Contains(Norm(name)))result.Add(new Remnant{Path="Software\\"+name,IsRegistry=true,Hive=RegistryHive.CurrentUser,View=view});
   }
  }
  AddShortcutRemnants(a,result); foreach(var item in result)MeasureRemnant(item);return result.GroupBy(x=>(x.IsRegistry?"R":"F")+x.Path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
 }
 public static ProcessStartInfo Uninstaller(AppEntry a) {
  if(a.IsUwp)return new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -Command \"Remove-AppxPackage -Package '"+a.PackageFullName+"'\""){UseShellExecute=false,CreateNoWindow=true};
  if(String.IsNullOrWhiteSpace(a.Command))throw new Exception("Không có lệnh gỡ đăng ký. Dùng Cài đặt Windows hoặc bộ cài chính thức để sửa chữa.");
  string cmd=Environment.ExpandEnvironmentVariables(a.Command).Trim(),exe,args;
  if(cmd.StartsWith("\"")){int end=cmd.IndexOf('"',1);if(end<0)throw new Exception("Lệnh gỡ không hợp lệ.");exe=cmd.Substring(1,end-1);args=cmd.Substring(end+1).Trim();}
  else {var m=Regex.Match(cmd,@"^(.*?\.exe)(?:\s+(.*))?$",RegexOptions.IgnoreCase);if(!m.Success)throw new Exception("Không nhận diện được trình gỡ. Hãy gỡ ứng dụng này từ Cài đặt Windows.");exe=m.Groups[1].Value;args=m.Groups[2].Value;}
  if(String.Equals(Path.GetFileName(exe),"msiexec.exe",StringComparison.OrdinalIgnoreCase)) { exe=Path.Combine(Environment.SystemDirectory,"msiexec.exe");args=Regex.Replace(args,@"/I(?=\s|\{)","/X",RegexOptions.IgnoreCase); }
  else if(!Path.IsPathRooted(exe)||!File.Exists(exe))throw new Exception("Không tìm thấy tập tin trình gỡ. Hãy dùng Cài đặt Windows hoặc cài lại ứng dụng để sửa trình gỡ.");
  return new ProcessStartInfo(exe,args){UseShellExecute=true};
 }
 public static void Clean(AppEntry a,Remnant item) {
  if(Present(a))throw new Exception("Ứng dụng vẫn còn được cài đặt; đã dừng dọn.");
  if(item.IsRegistry) {
   throw new Exception("Khớp tên Registry chưa đủ bằng chứng sở hữu. Mục này chỉ xem; không tự động xóa.");
  } else if(item.IsShortcut) {
   if(!Scan(a).Any(x=>x.IsShortcut&&x.Path==item.Path))throw new Exception("Shortcut đã thay đổi; hãy quét lại.");
   FileSystem.DeleteFile(item.Path,UIOption.AllDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);Log("SHORTCUT_RECYCLED "+item.Path);
  } else {
   if(!SafeFolder(item.Path,a,Apps()))throw new Exception("Thư mục không còn đáp ứng điều kiện an toàn; hãy quét lại.");
   FileSystem.DeleteDirectory(item.Path,UIOption.AllDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);
   Log("Sent to Recycle Bin: "+item.Path);
  }
 }
}
