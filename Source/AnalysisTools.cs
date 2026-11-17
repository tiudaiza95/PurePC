using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.VisualBasic.FileIO;
public partial class FileItem {public string ScopeRoot {get;set;}public string Group {get;set;}public string Evidence {get;set;}}
class ScanProgress {public int Visited,Found,Skipped;public long Bytes;public string Phase;public override string ToString(){return Phase+" · đã duyệt "+Visited+" mục · tìm thấy "+Found+" · bỏ qua "+Skipped+(Bytes>0?" · đọc "+Features.Size(Bytes):"");}}
class CleanupRoot {public string Name,Path,Process;public override string ToString(){return Name;}}
static class AnalysisTools {
 public static List<CleanupRoot> Roots(){var roots=new List<CleanupRoot>{new CleanupRoot{Name="Temp tài khoản · hơn 7 ngày",Path=Features.TempRoot},new CleanupRoot{Name="Windows Temp · không nâng quyền",Path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Temp")}};
  string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
  var chromiumBrowsers=new[]{
   new{Dir=@"Google\Chrome\User Data",Name="Chrome",Proc="chrome"},
   new{Dir=@"Microsoft\Edge\User Data",Name="Edge",Proc="msedge"},
   new{Dir=@"CocCoc\Browser\User Data",Name="Cốc Cốc",Proc="browser"},
   new{Dir=@"BraveSoftware\Brave-Browser\User Data",Name="Brave",Proc="brave"}
  };
  foreach(var b in chromiumBrowsers){
   string basePath=System.IO.Path.Combine(local,b.Dir);if(!Directory.Exists(basePath))continue;
   try{foreach(string profile in Directory.GetDirectories(basePath).Where(x=>System.IO.Path.GetFileName(x)=="Default"||System.IO.Path.GetFileName(x).StartsWith("Profile "))){
    foreach(string cache in new[]{"Cache\\Cache_Data","Code Cache","GPUCache"}){
     string path=System.IO.Path.Combine(profile,cache);
     if(Directory.Exists(path))roots.Add(new CleanupRoot{Name=b.Name+" · "+System.IO.Path.GetFileName(profile)+" · "+cache,Path=path,Process=b.Proc});
    }
   }}catch{}
  }
  string ffProfiles=System.IO.Path.Combine(local,"Mozilla","Firefox","Profiles");
  if(Directory.Exists(ffProfiles))try{
   foreach(string profile in Directory.GetDirectories(ffProfiles)){
    string cache=System.IO.Path.Combine(profile,"cache2");
    if(Directory.Exists(cache))roots.Add(new CleanupRoot{Name="Firefox · "+System.IO.Path.GetFileName(profile)+" · cache2",Path=cache,Process="firefox"});
   }
  }catch{}
  foreach(string operaDir in new[]{@"Opera Software\Opera Stable",@"Opera Software\Opera GX Stable"}){
   string opPath=System.IO.Path.Combine(local,operaDir);if(!Directory.Exists(opPath))continue;
   string opName=operaDir.Contains("GX")?"Opera GX":"Opera";
   foreach(string cache in new[]{"Cache\\Cache_Data","Code Cache","GPUCache"}){
    string path=System.IO.Path.Combine(opPath,cache);
    if(Directory.Exists(path))roots.Add(new CleanupRoot{Name=opName+" · "+cache,Path=path,Process="opera"});
   }
  }
  string code=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Code");foreach(string cache in new[]{"Cache","Code Cache","CachedData"})if(Directory.Exists(System.IO.Path.Combine(code,cache)))roots.Add(new CleanupRoot{Name="Visual Studio Code · "+cache,Path=System.IO.Path.Combine(code,cache),Process="Code"});roots.Add(new CleanupRoot{Name="Báo cáo ứng dụng bị lỗi · CrashDumps",Path=System.IO.Path.Combine(local,"CrashDumps")});roots.Add(new CleanupRoot{Name="Báo cáo lỗi đã lưu · WER",Path=System.IO.Path.Combine(local,"Microsoft","Windows","WER","ReportArchive")});roots.Add(new CleanupRoot{Name="Báo cáo lỗi chờ gửi · WER",Path=System.IO.Path.Combine(local,"Microsoft","Windows","WER","ReportQueue")});
  string zaloPath=System.IO.Path.Combine(local,"ZaloPC");
  if(Directory.Exists(zaloPath)){
   string zaloTemp=System.IO.Path.Combine(zaloPath,"Temp");
   if(Directory.Exists(zaloTemp))roots.Add(new CleanupRoot{Name="Zalo PC · Tệp tạm Cache",Path=zaloTemp,Process="Zalo"});
  }
  string d3dCache=System.IO.Path.Combine(local,"D3DSCache");
  if(Directory.Exists(d3dCache))roots.Add(new CleanupRoot{Name="DirectX · Bộ đệm Shader D3D",Path=d3dCache});
  string nvDx=System.IO.Path.Combine(local,"NVIDIA","DXCache");
  if(Directory.Exists(nvDx))roots.Add(new CleanupRoot{Name="NVIDIA · Bộ đệm Shader DX",Path=nvDx});
  string amdDx=System.IO.Path.Combine(local,"AMD","DxCache");
  if(Directory.Exists(amdDx))roots.Add(new CleanupRoot{Name="AMD · Bộ đệm Shader DX",Path=amdDx});
  return roots;
 }
 public static bool IsJunkOrSystemCacheDir(string dir){
  string name=System.IO.Path.GetFileName(dir);
  if(String.IsNullOrEmpty(name))return false;
  if(name.StartsWith(".")&&name!="."&&name!="..")return true;
  if(name.Equals("AppData",StringComparison.OrdinalIgnoreCase))return true;
  if(name.Equals("node_modules",StringComparison.OrdinalIgnoreCase))return true;
  if(name.Equals("$Recycle.Bin",StringComparison.OrdinalIgnoreCase))return true;
  if(name.Equals("System Volume Information",StringComparison.OrdinalIgnoreCase))return true;
  if(name.Equals("Windows",StringComparison.OrdinalIgnoreCase))return true;
  if(name.Equals("Package Cache",StringComparison.OrdinalIgnoreCase))return true;
  return false;
 }
 public static List<FileItem> Walk(string root,CancellationToken token,IProgress<ScanProgress> progress,out int skipped,out bool limited,Func<string,bool> excludeDir=null){root=System.IO.Path.GetFullPath(root);if(!Directory.Exists(root)||!Features.NoLinks(root))throw new Exception("Thư mục không hợp lệ hoặc đi qua liên kết.");var list=new List<FileItem>();var stack=new Stack<string>();stack.Push(root);skipped=0;limited=false;int visited=0;
  while(stack.Count>0){token.ThrowIfCancellationRequested();var dir=stack.Pop();try{if(!Features.NoLinks(dir)){skipped++;continue;}var dInfo=new DirectoryInfo(dir);foreach(var fsi in dInfo.EnumerateFileSystemInfos()){token.ThrowIfCancellationRequested();if(++visited>200000||list.Count>=100000){limited=true;stack.Clear();break;}try{if((fsi.Attributes&FileAttributes.ReparsePoint)!=0){skipped++;continue;}if((fsi.Attributes&FileAttributes.Directory)!=0){if(excludeDir!=null&&excludeDir(fsi.FullName)){skipped++;continue;}stack.Push(fsi.FullName);continue;}var fi=fsi as FileInfo;if(fi!=null)list.Add(new FileItem{Path=fi.FullName,Size=fi.Length,Modified=fi.LastWriteTimeUtc,ScopeRoot=root});}catch{skipped++;}if(visited%250==0&&progress!=null)progress.Report(new ScanProgress{Phase="Đang đọc cây thư mục",Visited=visited,Found=list.Count,Skipped=skipped});}}catch(UnauthorizedAccessException){skipped++;}catch(IOException){skipped++;}}
  if(progress!=null)progress.Report(new ScanProgress{Phase=limited?"Đã quét nhanh phạm vi an toàn":"Hoàn tất duyệt tệp",Visited=visited,Found=list.Count,Skipped=skipped});return list;
 }
 public static bool Eligible(FileItem item,string root,DateTime now){try{var f=new FileInfo(item.Path);return Engine.Within(f.FullName,System.IO.Path.GetFullPath(root))&&f.Exists&&f.Length==item.Size&&f.LastWriteTimeUtc==item.Modified&&f.CreationTimeUtc<now.AddDays(-7)&&f.LastWriteTimeUtc<now.AddDays(-7)&&(f.Attributes&(FileAttributes.ReadOnly|FileAttributes.System|FileAttributes.ReparsePoint))==0&&Features.NoLinks(f.FullName);}catch{return false;}}
 public static FileScan Cleanup(CleanupRoot root,CancellationToken token,IProgress<ScanProgress> progress){int skipped;bool limit;var files=Walk(root.Path,token,progress,out skipped,out limit);var result=new FileScan{Skipped=skipped,Limited=limit,Visited=files.Count};result.Items=files.Where(x=>Eligible(x,root.Path,DateTime.UtcNow)).OrderByDescending(x=>x.Size).Take(5000).ToList();if(result.Items.Count==5000)result.Limited=true;foreach(var file in result.Items){file.Group=root.Name;file.Evidence="Tạo/sửa hơn 7 ngày; đúng thư mục được chọn";}return result;}
 static bool IsProcessActive(string processName){
  if(String.IsNullOrEmpty(processName))return false;
  Process[] procs=null;
  try{procs=Process.GetProcessesByName(processName);return procs.Length>0;}catch{return false;}
  finally{if(procs!=null)for(int i=0;i<procs.Length;i++)try{procs[i].Dispose();}catch{}}
 }
 public static void Clean(FileItem item){var root=Roots().FirstOrDefault(x=>String.Equals(System.IO.Path.GetFullPath(x.Path).TrimEnd('\\'),(item.ScopeRoot??"").TrimEnd('\\'),StringComparison.OrdinalIgnoreCase));if(root==null||!Eligible(item,root.Path,DateTime.UtcNow))throw new Exception("Mục không còn thuộc phạm vi dọn hợp lệ hoặc đã thay đổi.");if(IsProcessActive(root.Process))throw new Exception("Đóng "+root.Process+" trước khi dọn cache; dữ liệu được giữ nguyên.");using(var stream=new FileStream(item.Path,FileMode.Open,FileAccess.Read,FileShare.None)){}FileSystem.DeleteFile(item.Path,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);Engine.Log("RECYCLED "+item.Size+" bytes | "+item.Path);}
 static void ReadExact(Stream s,byte[] buf,int offset,int count){
  int total=0;
  while(total<count){
   int r=s.Read(buf,offset+total,count-total);
   if(r<=0)break;
   total+=r;
  }
 }
 static string FastSample(string path,long size){
  try{
   using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
    if(size<=8192){byte[] b=new byte[(int)size];ReadExact(f,b,0,b.Length);return Convert.ToBase64String(b);}
    byte[] s=new byte[8192];ReadExact(f,s,0,4096);f.Seek(-4096,SeekOrigin.End);ReadExact(f,s,4096,4096);
    using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(s));
   }
  }catch{return Guid.NewGuid().ToString();}
 }
 public static List<FileItem> Duplicates(IEnumerable<FileItem> all,CancellationToken token,IProgress<ScanProgress> progress,out string summary){var sizeCandidates=all.Where(x=>x.Size>0).GroupBy(x=>x.Size).Where(g=>g.Count()>1).ToList();var candidates=new List<FileItem>();foreach(var sg in sizeCandidates){if(sg.Key<=8192){candidates.AddRange(sg);continue;}foreach(var fg in sg.GroupBy(f=>FastSample(f.Path,f.Size)).Where(g=>g.Count()>1))candidates.AddRange(fg);}var results=new List<FileItem>();long read=0;int done=0;int skipped=0;bool limited=false;var hashes=new Dictionary<string,List<FileItem>>();
  foreach(var f in candidates){token.ThrowIfCancellationRequested();if(read+f.Size>20L*1024*1024*1024){limited=true;break;}try{using(var stream=new FileStream(f.Path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var hash=SHA256.Create()){if(stream.Length!=f.Size)continue;byte[] buffer=new byte[1024*1024];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,count,buffer,0);read+=count;}hash.TransformFinalBlock(new byte[0],0,0);if(File.GetLastWriteTimeUtc(f.Path)!=f.Modified)continue;string digest=BitConverter.ToString(hash.Hash).Replace("-","");f.Evidence=digest;string key=f.Size+":"+digest;if(!hashes.ContainsKey(key))hashes[key]=new List<FileItem>();hashes[key].Add(f);}}catch(OperationCanceledException){throw;}catch{skipped++;}done++;if(progress!=null&&(done%3==0||done==candidates.Count)){int matchedGroups=hashes.Values.Count(x=>x.Count>1);progress.Report(new ScanProgress{Phase="Đối chiếu SHA-256 ("+done+"/"+candidates.Count+")",Visited=done,Found=matchedGroups,Bytes=read,Skipped=skipped});}}
  int group=0;foreach(var g in hashes.Values.Where(x=>x.Count>1)){group++;foreach(var file in g){file.Group="Nhóm "+group;results.Add(file);}}
  summary=group+" nhóm cùng kích thước và SHA-256 · "+results.Count+" đường dẫn · "+Features.Size(read)+" đã đọc · "+skipped+" mục bỏ qua"+(limited?" · kết quả giới hạn":"")+". Có thể bao gồm hard link; không tự xóa hay tính dung lượng thu hồi.";return results;
 }
 public static List<FileItem> Duplicates(string root,CancellationToken token,IProgress<ScanProgress> progress,out string summary){int skipped;bool limited;var all=Walk(root,token,progress,out skipped,out limited,IsJunkOrSystemCacheDir);return Duplicates(all,token,progress,out summary);}
 public static string DiskSummary(IEnumerable<FileItem> files){return String.Join("\n",files.GroupBy(x=>System.IO.Path.GetExtension(x.Path).ToLowerInvariant()).OrderByDescending(x=>x.Sum(f=>f.Size)).Take(12).Select(g=>(g.Key==""?"Không đuôi":g.Key)+" | "+Features.Size(g.Sum(f=>f.Size))+" | "+g.Count()+" tệp"));}
}
partial class Remnant {
 public bool IsShortcut;public long Bytes;public bool SizeComplete;
 public bool CanClean {get{return !IsRegistry;}}
 public string Confidence {get{return IsRegistry?"Thấp · chỉ xem":IsShortcut?"Đích shortcut thuộc ứng dụng":"Trung bình · khớp tên/vị trí";}}
 public string SizeLabel {get{return IsRegistry?"—":(SizeComplete?"":"≥ ")+Features.Size(Bytes);}}
}
static partial class Engine {
 static string LinkTarget(string lnk){dynamic shell=null,shortcut=null;try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));shortcut=shell.CreateShortcut(lnk);return Convert.ToString(shortcut.TargetPath);}catch{return "";}finally{if(shortcut!=null&&System.Runtime.InteropServices.Marshal.IsComObject(shortcut))System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);if(shell!=null&&System.Runtime.InteropServices.Marshal.IsComObject(shell))System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}}
 static void AddShortcutRemnants(AppEntry a,List<Remnant> result){if(String.IsNullOrWhiteSpace(a.Location)||!Path.IsPathRooted(a.Location))return;string install=Path.GetFullPath(a.Location).TrimEnd('\\');if(install.Length<5)return;string[] menus={Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.Programs)};foreach(string menu in menus.Where(Directory.Exists)){int seen=0;foreach(string lnk in Directory.EnumerateFiles(menu,"*.lnk",System.IO.SearchOption.AllDirectories)){if(++seen>1500)break;try{if(!Features.NoLinks(lnk))continue;string target=LinkTarget(lnk);if(!String.IsNullOrWhiteSpace(target)&&Path.IsPathRooted(target)&&Within(Path.GetFullPath(target),install)&&!File.Exists(target))result.Add(new Remnant{Path=lnk,IsShortcut=true,Bytes=new FileInfo(lnk).Length,SizeComplete=true});}catch{}}}}
 static void MeasureRemnant(Remnant r){if(r.IsRegistry||r.IsShortcut)return;r.SizeComplete=true;var stack=new Stack<string>();stack.Push(r.Path);int visited=0;while(stack.Count>0){string dir=stack.Pop();try{foreach(string entry in Directory.EnumerateFileSystemEntries(dir)){if(++visited>15000){r.SizeComplete=false;return;}var attr=File.GetAttributes(entry);if((attr&FileAttributes.ReparsePoint)!=0)continue;if((attr&FileAttributes.Directory)!=0)stack.Push(entry);else r.Bytes+=new FileInfo(entry).Length;}}catch{r.SizeComplete=false;}}}
}
