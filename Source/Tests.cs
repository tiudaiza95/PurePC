using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;
static class Tests {
 static int checks;static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;}
 static AppEntry Entry(RegistryView view,string key,string command){return new AppEntry{Name="Zalo",Publisher="Test vendor",Version="1.0",Hive=RegistryHive.CurrentUser,View=view,Key=key,Command=command};}
 public static void Run(){
  Engine.Testing=true;
  Check(!Engine.Specific("Microsoft"),"shared vendor rejected");Check(!Engine.Specific("Windows"),"Windows rejected");Check(!Engine.Specific("abc"),"short name rejected");
  Check(Engine.AppNames(new AppEntry{Name="Zalo 26.10.11",Version="26.10.11"}).Contains("zalo"),"known version suffix removed for folder matching");
  Check(!Engine.AppNames(new AppEntry{Name="Temp"}).Any(),"shared temp folder excluded");
  Check(Engine.Within(@"C:\Apps\Thing\data",@"C:\Apps\Thing"),"child path");Check(!Engine.Within(@"C:\Apps\Thing2",@"C:\Apps\Thing"),"prefix sibling rejected");
  var a=Entry(RegistryView.Registry64,"same","test.exe");var b=Entry(RegistryView.Registry32,"same","test.exe");
  Check(Engine.Deduplicate(new[]{a,b}).Count==1,"shared HKCU WOW64 entries collapsed");
  b.Command="other.exe";Check(Engine.Deduplicate(new[]{a,b}).Count==2,"different command preserved");b.Command=a.Command;b.Key="second";Check(Engine.Deduplicate(new[]{a,b}).Count==2,"different install key preserved");
  b.Key=a.Key;b.Version="2.0";Check(Engine.Deduplicate(new[]{a,b}).Count==2,"different versions preserved");b.Version=a.Version;b.Hive=RegistryHive.LocalMachine;Check(Engine.Deduplicate(new[]{a,b}).Count==2,"different scope preserved");a.Hive=RegistryHive.LocalMachine;Check(Engine.Deduplicate(new[]{a,b}).Count==2,"machine architectures preserved");
  a.Command="MsiExec.exe /I{11111111-1111-1111-1111-111111111111}";var start=Engine.Uninstaller(a);Check(start.Arguments.StartsWith("/X{"),"MSI uninstall flag");Check(start.FileName==Path.Combine(Environment.SystemDirectory,"msiexec.exe"),"MSI system path");
  Check(!Engine.SafeFolder(Environment.GetFolderPath(Environment.SpecialFolder.Windows),a,new List<AppEntry>()),"system path rejected");
  string name="PurePCTest"+Guid.NewGuid().ToString("N"),dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),name);
  Directory.CreateDirectory(dir);try{a=new AppEntry{Name=name,Key="own",Location=dir};Check(Engine.SafeFolder(dir,a,new List<AppEntry>()),"isolated app folder accepted");Check(!Engine.SafeFolder(dir,a,new List<AppEntry>{new AppEntry{Name="Other",Key="other",Location=Path.Combine(dir,"shared")}}),"shared install path rejected");a.Name="Other";Check(!Engine.SafeFolder(dir,a,new List<AppEntry>()),"name mismatch rejected");}finally{Directory.Delete(dir);}
  string test=Path.Combine(Features.TempRoot,"PurePCTest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(test);
  string testKey="Software\\PurePCTest-"+Guid.NewGuid().ToString("N");
  try{
   string old=Path.Combine(test,"old.tmp"),fresh=Path.Combine(test,"fresh.tmp");File.WriteAllText(old,"test");File.WriteAllText(fresh,"test");File.SetCreationTimeUtc(old,DateTime.UtcNow.AddDays(-10));File.SetLastWriteTimeUtc(old,DateTime.UtcNow.AddDays(-10));
   Check(Features.EligibleTemp(new FileInfo(old),DateTime.UtcNow),"old temp eligible");Check(!Features.EligibleTemp(new FileInfo(fresh),DateTime.UtcNow),"new temp excluded");
   File.SetAttributes(old,FileAttributes.ReadOnly);Check(!Features.EligibleTemp(new FileInfo(old),DateTime.UtcNow),"read-only temp excluded");File.SetAttributes(old,FileAttributes.Normal);
   var cancelled=new CancellationToken(true);bool stopped=false;try{Features.ScanFiles(test,false,cancelled);}catch(OperationCanceledException){stopped=true;}Check(stopped,"scan cancellation");
   Check(Features.ScanFiles(test,false,CancellationToken.None).Items.Count==0,"small files excluded from large scan");
   string large=Path.Combine(test,"large-test.bin");using(var f=File.Create(large))f.SetLength(101L*1024*1024);var largeScan=Features.ScanFiles(test,false,CancellationToken.None);Check(largeScan.Items.Count==1&&largeScan.Items[0].Path==large,"large file positive detection");File.Delete(large);
   bool scope=false;try{Features.ScanFiles(test,true,CancellationToken.None);}catch{scope=true;}Check(scope,"temp scan root constrained");
   string runKey=testKey+"\\Run",backups=Path.Combine(test,"backups");var entry=new StartupItem{Name="Synthetic test only",Command="%TEMP%\\not-a-real-app.exe",Kind=RegistryValueKind.ExpandString};
   using(var root=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64))using(var key=root.CreateSubKey(runKey))key.SetValue(entry.Name,entry.Command,entry.Kind);
   Features.Toggle(entry,runKey,backups);string backup=Directory.GetFiles(backups,"*.xml").Single();var saved=Features.ReadStartup(backup);Check(saved.Command==entry.Command&&saved.Kind==entry.Kind,"startup backup preserves unexpanded value and type");
   using(var root=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64))using(var key=root.OpenSubKey(runKey))Check(key.GetValue(entry.Name)==null,"startup disabled in synthetic key");
   saved.Backup=backup;Features.Toggle(saved,runKey,backups);
   using(var root=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64))using(var key=root.OpenSubKey(runKey))Check((string)key.GetValue(entry.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames)==entry.Command&&key.GetValueKind(entry.Name)==entry.Kind,"startup restore exact original");
   bool conflict=false;try{Features.Toggle(saved,runKey,backups);}catch{conflict=true;}Check(conflict,"startup restore does not overwrite existing value");
   Check(!Features.ReadStartup(backup).Disabled,"restored startup backup archived");
  }finally{
   using(var root=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64))root.DeleteSubKeyTree(testKey,false);
   // Only remove this test's newly created, checked sandbox.
   if(Path.GetFileName(test).StartsWith("PurePCTest-")&&Path.GetDirectoryName(test).Equals(Features.TempRoot,StringComparison.OrdinalIgnoreCase))Directory.Delete(test,true);
  }
  checks+=ProTests.Run();
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),"PASS: "+checks+" checks.\r\nDeduplication, path boundaries, temp age and read-only guards, cancellation, startup backup and restore tested.\r\nStartup mutations only used an isolated synthetic registry branch, never the real Run key. Additional tests cover sourced advisory, grouping, cleanup scope, SHA-256, cancellation and updater argument validation.\r\nNo installed applications removed.\r\n");
 }
}
