using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;
public partial class AppEntry {public bool WindowsInstaller;}
static class MsiSupport {
 [DllImport("msi.dll",CharSet=CharSet.Unicode)]public static extern int MsiQueryProductState(string code);
 public static string Code(AppEntry a){Guid code;string value=(a.Key??"").Split('\\').Last();return Guid.TryParse(value,out code)?code.ToString("B").ToUpperInvariant():null;}
 public static void FillCommand(AppEntry a){string code=Code(a);if(a.WindowsInstaller&&String.IsNullOrWhiteSpace(a.Command)&&code!=null){int state=MsiQueryProductState(code);if(state==5||state==1)a.Command="MsiExec.exe /X"+code;}}
}
static class FileActions {
 public static bool ProtectedPath(string path){try{path=Path.GetFullPath(path);string[] roots={Engine.Data,Environment.GetFolderPath(Environment.SpecialFolder.Windows),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs"),AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')};return path.StartsWith("\\\\")||roots.Where(x=>!String.IsNullOrWhiteSpace(x)).Any(x=>Engine.Within(path,x));}catch{return true;}}
 public static void Validate(FileItem item){var f=new FileInfo(item.Path);if(ProtectedPath(item.Path)||!Features.NoLinks(item.Path)||!f.Exists||(f.Attributes&(FileAttributes.System|FileAttributes.ReadOnly|FileAttributes.ReparsePoint))!=0)throw new Exception("Không xử lý đường dẫn hệ thống/chương trình, liên kết hoặc tệp được bảo vệ: "+item.Path);if(f.Length!=item.Size||f.LastWriteTimeUtc!=item.Modified)throw new Exception("Tệp đã thay đổi từ lúc quét: "+item.Path);using(var read=new FileStream(item.Path,FileMode.Open,FileAccess.Read,FileShare.None)){} }
 public static string Digest(FileItem item){Validate(item);using(var f=new FileStream(item.Path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var sha=SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(f)).Replace("-","");if(new FileInfo(item.Path).Length!=item.Size||File.GetLastWriteTimeUtc(item.Path)!=item.Modified)throw new Exception("Tệp thay đổi trong lúc đối chiếu.");return hash;}}
 public static void ValidateDuplicate(FileItem item,IEnumerable<FileItem> all,ISet<string> removing){string digest=Digest(item);if(digest!=item.Evidence)throw new Exception("Nội dung khác lần quét; giữ lại tệp.");var survivors=all.Where(x=>x.Group==item.Group&&!removing.Contains(x.Path)&&!String.Equals(x.Path,item.Path,StringComparison.OrdinalIgnoreCase));foreach(var survivor in survivors){try{if(survivor.Size==item.Size&&Digest(survivor)==digest)return;}catch{}}throw new Exception("Phải giữ ít nhất một bản cùng nội dung còn tồn tại và chưa thay đổi trong nhóm.");}
 public static void Recycle(FileItem item,Action<string> fixture=null){Validate(item);if(fixture!=null)fixture(item.Path);else FileSystem.DeleteFile(item.Path,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);Engine.Log("USER_FILE_RECYCLED "+item.Size+" | "+item.Path);}
}
class DeepItem {
 public bool Selected {get;set;}public string Name {get;set;}public string Path {get;set;}public string Evidence {get;set;}public string Action {get;set;}
 public AppEntry App;public Remnant Remnant;public bool Stale;
 public bool Actionable {get{return Remnant!=null&&Remnant.CanClean||App!=null&&(Stale||App.CanUninstall);}}
}
static class DeepClean {
 public static bool MissingLocation(AppEntry a){try{if(String.IsNullOrWhiteSpace(a.Location)||!Path.IsPathRooted(a.Location)||a.Location.StartsWith("\\\\"))return false;string path=Path.GetFullPath(a.Location).TrimEnd('\\');if(path.Length<5||!new DriveInfo(Path.GetPathRoot(path)).IsReady)return false;return !Directory.Exists(path);}catch{return false;}}
 public static List<DeepItem> Scan(CancellationToken token,IProgress<ScanProgress> progress){var result=new List<DeepItem>();var apps=Engine.Apps();int seen=0;foreach(var app in apps){token.ThrowIfCancellationRequested();seen++;if(progress!=null)progress.Report(new ScanProgress{Phase="Đối chiếu đăng ký/phần dư",Visited=seen,Found=result.Count});if(app.Protected||!MissingLocation(app))continue;string code=MsiSupport.Code(app);int state=app.WindowsInstaller&&code!=null?MsiSupport.MsiQueryProductState(code):-999;bool stale=app.WindowsInstaller&&state==-1&&String.IsNullOrEmpty(Engine.KnownExecutable(app));result.Add(new DeepItem{Name=app.Name,Path=app.Location,App=app,Stale=stale,Action=stale?"Sao lưu + bỏ đăng ký mồ côi":app.CanUninstall?"Chạy bộ gỡ chính thức":"Chưa đủ bằng chứng",Evidence="Vị trí cài khai báo không tồn tại. "+(app.WindowsInstaller?"MSI state="+state:"Không có bằng chứng MSI")+"; không mặc định là tệp rác."});}
  foreach(var app in Engine.RemovalHistory()){token.ThrowIfCancellationRequested();if(Engine.Present(app))continue;try{foreach(var r in Engine.Scan(app))result.Add(new DeepItem{Name=app.Name,Path=r.DisplayPath,App=app,Remnant=r,Action=r.CanClean?"Chuyển phần dư vào Thùng rác":"Registry chỉ xem",Evidence=r.Confidence+" · "+r.SizeLabel});}catch(Exception ex){Engine.Log("DEEP_SCAN_SKIPPED "+app.Name+" | "+ex.Message);}}
  return result.GroupBy(x=>x.Name+"|"+x.Path+"|"+x.Action).Select(g=>g.First()).ToList();
 }
 public static void RemoveStale(AppEntry app){if(app.Protected||!app.WindowsInstaller||MsiSupport.Code(app)==null||MsiSupport.MsiQueryProductState(MsiSupport.Code(app))!=-1||!MissingLocation(app)||!String.IsNullOrEmpty(Engine.KnownExecutable(app)))throw new Exception("Không đủ bằng chứng mục đăng ký mồ côi; không xóa.");
  using(var root=RegistryKey.OpenBaseKey(app.Hive,app.View))using(var key=root.OpenSubKey(app.Key)){if(key==null)return;if(Convert.ToString(key.GetValue("DisplayName"))!=app.Name||Convert.ToString(key.GetValue("InstallLocation"))!=app.Location)throw new Exception("Đăng ký đã thay đổi, hãy quét lại.");}
  string directory=Path.Combine(Engine.Data,"Registry-backups");Directory.CreateDirectory(directory);string backup=Path.Combine(directory,"stale-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".reg");string regPath=(app.Hive==RegistryHive.LocalMachine?"HKLM":"HKCU")+"\\"+app.Key;string reg=Path.Combine(Environment.SystemDirectory,"reg.exe");if(app.View==RegistryView.Registry32)reg=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"SysWOW64","reg.exe");
  using(var proc=Process.Start(new ProcessStartInfo(reg,"export "+Maintenance.Quote(regPath)+" "+Maintenance.Quote(backup)+" /y"){UseShellExecute=false,CreateNoWindow=true})){proc.WaitForExit();if(proc.ExitCode!=0||!File.Exists(backup)||new FileInfo(backup).Length==0)throw new Exception("Sao lưu thất bại; không sửa Registry.");}
  using(var root=RegistryKey.OpenBaseKey(app.Hive,app.View))root.DeleteSubKeyTree(app.Key,false);Engine.Log("STALE_REGISTRATION_REMOVED "+regPath+" | "+backup);
 }
}
