using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;

public partial class FileItem {
 public bool Selected {get;set;}
 public string Path {get;set;}
 public long Size {get;set;}
 public DateTime Modified {get;set;}
 public string Name { get { return System.IO.Path.GetFileName(Path); } }
 public string SizeLabel { get { return Features.Size(Size); } }
}
public class FileScan {
 public List<FileItem> Items=new List<FileItem>();
 public int Skipped,Visited; public bool Limited;
}
public class StartupItem {
 public string Name {get;set;}
 public string Command {get;set;}
 public RegistryValueKind Kind {get;set;}
 public bool Disabled {get;set;}
 public string Backup {get;set;}
 public string Status {get {return I18n.T(Disabled?"○ Đã tắt":"● Đang kích hoạt");}}
 public string Recommendation {
  get {
   if(Disabled)return I18n.T("○ Đã tắt");
   string combined=((Name??"")+" "+(Command??"")).ToLowerInvariant();
   if(combined.Contains("unikey")||combined.Contains("evkey")||combined.Contains("realtek")||
      combined.Contains("rtkaud")||combined.Contains("ravcpl")||combined.Contains("igfxtray")||
      combined.Contains("hkcmd")||combined.Contains("securityhealth")||combined.Contains("ctfmon")||
      combined.Contains("windows defender")||combined.Contains("touchpad")||combined.Contains("synaptics"))
    return I18n.T("🛡️ Hệ thống (Nên giữ)");
   if(combined.Contains("teams")||combined.Contains("discord")||combined.Contains("spotify")||
      combined.Contains("steam")||combined.Contains("epicgames")||combined.Contains("onedrive")||
      combined.Contains("googledrive")||combined.Contains("dropbox")||combined.Contains("adobe")||
      combined.Contains("creative cloud")||combined.Contains("skype")||combined.Contains("zoom")||
      combined.Contains("fathom")||combined.Contains("update")||combined.Contains("torrent")||
      combined.Contains("chrome")||combined.Contains("zalo")||combined.Contains("viber"))
    return I18n.T("⚡ Khuyên tắt (Tăng tốc)");
   return I18n.T("Tùy chọn người dùng");
  }
 }
 public string Impact {
  get {
   if(Disabled)return "—";
   return I18n.T(EstimateImpact(Command,Name));
  }
 }
 public static string EstimateImpact(string cmd,string name){
  if(String.IsNullOrEmpty(cmd)&&String.IsNullOrEmpty(name))return "Thấp";
  string combined=((name??"")+" "+(cmd??"")).ToLowerInvariant();
  if(combined.Contains("teams")||combined.Contains("discord")||combined.Contains("spotify")||
     combined.Contains("steam")||combined.Contains("epicgames")||combined.Contains("onedrive")||
     combined.Contains("googledrive")||combined.Contains("dropbox")||combined.Contains("adobe")||
     combined.Contains("creative cloud")||combined.Contains("skype")||combined.Contains("zoom")){
   return "Cao";
  }
  if(combined.Contains("unikey")||combined.Contains("evkey")||combined.Contains("realtek")||
     combined.Contains("rtkaud")||combined.Contains("ravcpl")||combined.Contains("igfxtray")||
     combined.Contains("hkcmd")||combined.Contains("securityhealth")||combined.Contains("ctfmon")){
   return "Thấp";
  }
  try{
   string path=cmd;
   if(!String.IsNullOrEmpty(path)){
    path=path.Trim();
    if(path.StartsWith("\"")){
     int q=path.IndexOf('"',1);if(q>1)path=path.Substring(1,q-1);
    }else{
     int sp=path.IndexOf(' ');if(sp>0)path=path.Substring(0,sp);
    }
    path=Environment.ExpandEnvironmentVariables(path);
    if(File.Exists(path)){
     long len=new FileInfo(path).Length;
     if(len>30L*1024*1024)return "Cao";
     if(len<3L*1024*1024)return "Thấp";
    }
   }
  }catch{}
  return "Trung bình";
 }
}
static partial class Features {
 public const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 public static string Size(long n) {double v=n;string[] u={"B","KB","MB","GB","TB"};int i=0;while(v>=1024&&i<4){v/=1024;i++;}return v.ToString(i==0?"0":"0.#")+" "+u[i];}
 public static string TempRoot {get{return System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()).TrimEnd('\\');}}
 public static bool NoLinks(string path) {
  try {string p=System.IO.Path.GetFullPath(path);while(!String.IsNullOrEmpty(p)){if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)return false;p=System.IO.Path.GetDirectoryName(p);}return true;}catch{return false;}
 }
 public static bool EligibleTemp(FileInfo f,DateTime now) {
  try{return Engine.Within(f.FullName,TempRoot)&&f.FullName!=TempRoot&&f.Exists&&f.LastWriteTimeUtc<now.AddDays(-7)&&f.CreationTimeUtc<now.AddDays(-7)&&(f.Attributes&(FileAttributes.ReparsePoint|FileAttributes.System|FileAttributes.ReadOnly))==0&&NoLinks(f.FullName);}catch{return false;}
 }
 public static FileScan ScanFiles(string root,bool temp,CancellationToken token) {
  root=System.IO.Path.GetFullPath(root);var result=new FileScan();if(!Directory.Exists(root)||!NoLinks(root))throw new Exception("Thư mục không tồn tại hoặc đi qua liên kết; đã bỏ qua.");
  if(temp&&!String.Equals(root.TrimEnd('\\'),TempRoot,StringComparison.OrdinalIgnoreCase))throw new Exception("Phạm vi dọn tệp tạm không hợp lệ.");
  var dirs=new Stack<string>();dirs.Push(root);DateTime now=DateTime.UtcNow;
  while(dirs.Count>0){token.ThrowIfCancellationRequested();var dir=dirs.Pop();try {
   if(!NoLinks(dir)){result.Skipped++;continue;}
   var dInfo=new DirectoryInfo(dir);
   foreach(var fsi in dInfo.EnumerateFileSystemInfos()){
    token.ThrowIfCancellationRequested();if(++result.Visited>200000){result.Limited=true;dirs.Clear();break;}
    try{
     if((fsi.Attributes&FileAttributes.ReparsePoint)!=0){result.Skipped++;continue;}
     if((fsi.Attributes&FileAttributes.Directory)!=0){dirs.Push(fsi.FullName);continue;}
     var f=fsi as FileInfo;if(f==null)continue;
     if(temp?!EligibleTemp(f,now):f.Length<100L*1024*1024)continue;
     result.Items.Add(new FileItem{Path=f.FullName,Size=f.Length,Modified=f.LastWriteTimeUtc});
     if(temp&&result.Items.Count>=5000){result.Limited=true;dirs.Clear();break;}
    }catch(UnauthorizedAccessException){result.Skipped++;}catch(IOException){result.Skipped++;}
   }
  }catch(UnauthorizedAccessException){result.Skipped++;}catch(IOException){result.Skipped++;}}
  result.Items=result.Items.OrderByDescending(x=>x.Size).ToList();if(!temp&&result.Items.Count>500){result.Items=result.Items.Take(500).ToList();result.Limited=true;}return result;
 }
 public static void CleanTemp(FileItem item) {
  var f=new FileInfo(item.Path);if(!EligibleTemp(f,DateTime.UtcNow)||f.Length!=item.Size||f.LastWriteTimeUtc!=item.Modified)throw new Exception("Tệp mới thay đổi hoặc không còn đủ điều kiện; giữ lại.");
  // Do not clean files held by another application. Recycle operation remains visible to the user.
  using(var stream=new FileStream(item.Path,FileMode.Open,FileAccess.Read,FileShare.None)){}
  FileSystem.DeleteFile(item.Path,UIOption.AllDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);
  Engine.Log("TEMP_RECYCLED "+item.Path);
 }
 public static List<StartupItem> Startups() {
  var items=new List<StartupItem>();
  using(var r=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64))using(var key=r.OpenSubKey(RunKey))if(key!=null)foreach(string name in key.GetValueNames()) {
   var kind=key.GetValueKind(name);if(kind!=RegistryValueKind.String&&kind!=RegistryValueKind.ExpandString)continue;
   items.Add(new StartupItem{Name=name,Command=Convert.ToString(key.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames)),Kind=kind});
  }
  string startupDir=Environment.GetFolderPath(Environment.SpecialFolder.Startup);if(Directory.Exists(startupDir))try{foreach(string f in Directory.GetFiles(startupDir,"*.lnk")){string n=System.IO.Path.GetFileNameWithoutExtension(f)+" (Thư mục Startup)";if(!items.Any(x=>x.Name.Equals(n,StringComparison.OrdinalIgnoreCase)))items.Add(new StartupItem{Name=n,Command=f,Kind=RegistryValueKind.String});}}catch{}
  try{using(var r=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64))using(var key=r.OpenSubKey(RunKey))if(key!=null)foreach(string name in key.GetValueNames()){var kind=key.GetValueKind(name);if(kind!=RegistryValueKind.String&&kind!=RegistryValueKind.ExpandString)continue;string n=name+" (Toàn máy)";if(!items.Any(x=>x.Name.Equals(n,StringComparison.OrdinalIgnoreCase)))items.Add(new StartupItem{Name=n,Command=Convert.ToString(key.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames)),Kind=kind});}}catch{
  }
  string dir=System.IO.Path.Combine(Engine.Data,"Startup-backups");if(Directory.Exists(dir))foreach(string path in Directory.GetFiles(dir,"*.xml"))try{var item=ReadStartup(path);if(item.Disabled&&!items.Any(x=>x.Name.Equals(item.Name,StringComparison.OrdinalIgnoreCase))){item.Backup=path;items.Add(item);}}catch{}
  return items.OrderBy(x=>x.Name).ToList();
 }
 public static StartupItem ReadStartup(string path){using(var f=File.OpenRead(path))return (StartupItem)new XmlSerializer(typeof(StartupItem)).Deserialize(f);}
 static void WriteStartup(string path,StartupItem item){using(var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write))new XmlSerializer(typeof(StartupItem)).Serialize(f,item);}
 public static void Toggle(StartupItem item, string runKey=RunKey, string backupDirectory=null) {
  string dir=backupDirectory??System.IO.Path.Combine(Engine.Data,"Startup-backups");Directory.CreateDirectory(dir);
  using(var r=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64))using(var key=r.CreateSubKey(runKey)) {
   var current=key.GetValue(item.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);
   if(!item.Disabled){
    if(current==null||Convert.ToString(current)!=item.Command||key.GetValueKind(item.Name)!=item.Kind)throw new Exception("Mục khởi động đã thay đổi. Hãy làm mới danh sách.");
    var backup=new StartupItem{Name=item.Name,Command=item.Command,Kind=item.Kind,Disabled=true};string path=System.IO.Path.Combine(dir,Guid.NewGuid().ToString("N")+".xml");WriteStartup(path,backup);
    key.DeleteValue(item.Name);Engine.Log("STARTUP_DISABLED "+item.Name+"; backup: "+path);
   }else{
    if(current!=null)throw new Exception("Đã có mục trùng tên. Không ghi đè; hãy làm mới danh sách.");
    string full=System.IO.Path.GetFullPath(item.Backup);if(!Engine.Within(full,dir))throw new Exception("Bản sao không hợp lệ.");
    var backup=ReadStartup(full);if(!backup.Disabled||backup.Name!=item.Name||backup.Command!=item.Command)throw new Exception("Bản sao đã thay đổi.");
    if(backup.Kind!=RegistryValueKind.String&&backup.Kind!=RegistryValueKind.ExpandString)throw new Exception("Kiểu dữ liệu không hỗ trợ.");
    key.SetValue(backup.Name,backup.Command,backup.Kind);backup.Disabled=false;
    string archive=System.IO.Path.Combine(dir,Guid.NewGuid().ToString("N")+".xml");WriteStartup(archive,backup);
    // Keep original backup for audit, but remove it from the disabled list atomically.
    File.Replace(archive,full,null);Engine.Log("STARTUP_RESTORED "+item.Name);
   }
  }
 }
}
