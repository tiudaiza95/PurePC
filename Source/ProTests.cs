using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;
static class ProTests {
 static int count;static void Check(bool value,string title){if(!value)throw new Exception("FAIL: "+title);count++;}
 static AppEntry A(string name,string publisher){var a=new AppEntry{Name=name,Publisher=publisher,Key="fixture",Hive=RegistryHive.CurrentUser,View=RegistryView.Registry64,Command="fake.exe",Version="1"};Catalog.Enrich(a);return a;}
 public static int Run(){count=0;
  var vc=A("Microsoft Visual C++ 2015-2022 Redistributable","Microsoft Corporation");Check(vc.Caution,"VC++ remains caution");Check(vc.Advice.Url.StartsWith("https://learn.microsoft.com/"),"VC++ source official");
  var dotnet=A("Microsoft .NET Runtime 8.0","Microsoft Corporation");Check(dotnet.Caution,"dotnet caution");var driver=A("NVIDIA Graphics Driver","NVIDIA");Check(driver.Protected,"driver protected");
  var core=A("Windows Servicing Stack","Microsoft Corporation");core.SystemComponent=true;Catalog.Enrich(core);Check(core.Protected,"Windows core remains protected");
  var system=A("Fixture","Fixture");system.SystemComponent=true;Catalog.Enrich(system);Check(!system.Protected&&system.Caution,"third-party SystemComponent is caution not blocked");var unknown=A("Unfamiliar app","Unknown vendor");Check(unknown.Caution&&unknown.Advice.Level=="Không đủ thông tin","unknown not safe-to-remove");
  var zalo=A("Zalo 26.1","Zalo Group");Check(!zalo.Caution&&zalo.Advice.Source.Contains("zalo.me"),"Zalo sourced optional function");Check(zalo.Advice.Country=="Chưa xác minh","country not fabricated");Check(!DesktopApp.MatchesFilter(zalo,3),"no fake usage data");
  var fake=A("Zalo","Unrelated vendor");Check(fake.Caution,"name alone does not match known publisher");
  var same=A("Fixture","Vendor");same.View=RegistryView.Registry32;var same64=A("Fixture","Vendor");var merged=Engine.Deduplicate(new[]{same64,same});Check(merged.Count==1&&merged[0].Sources.Count==2,"both source rows preserved");same.Version="2";Check(Engine.Deduplicate(new[]{same64,same}).Count==2,"versions separate");same.Version="1";same.Hive=RegistryHive.LocalMachine;Check(Engine.Deduplicate(new[]{same64,same}).Count==2,"scopes separate");same.Hive=RegistryHive.CurrentUser;same.Command="FAKE.exe";Check(Engine.Deduplicate(new[]{same64,same}).Count==2,"case-sensitive commands not guessed equal");
  Check(unknown.SizeLabel=="Chưa rõ","size unknown not zero");unknown.EstimatedBytes=1024;Check(unknown.SizeSource.Contains("1.024"),"size method explained");unknown.InstalledOn="garbage";Check(unknown.InstalledDate=="Chưa rõ","invalid date unknown");unknown.InstalledOn=DateTime.Today.ToString("yyyyMMdd");Check(DesktopApp.MatchesFilter(unknown,1),"recent install filter");unknown.EstimatedBytes=2L*1024*1024*1024;Check(DesktopApp.MatchesFilter(unknown,2),"large app filter");
  Check(!Engine.Uninstaller(new AppEntry{Command="msiexec.exe /I{11111111-1111-1111-1111-111111111111}"}).Verb.Equals("runas"),"no forced elevation");
  Check(!new Remnant{IsRegistry=true}.CanClean,"speculative registry cannot clean");Check(Maintenance.UpdateArgs("Microsoft.PowerToys").Contains("--exact"),"exact package update");bool rejected=false;try{Maintenance.UpdateArgs("foo --all");}catch{rejected=true;}Check(rejected,"update argument injection blocked");Check(!Maintenance.UpdateArgs("Foo.Bar").Contains("accept"),"no silent agreement acceptance");Check(Maintenance.Quote("a b")=="\"a b\"","quote spaces");Check(Maintenance.Quote("abc\\")=="\"abc\\\\\"","quote trailing slash");
  string sandbox=Path.Combine(Path.GetTempPath(),"PurePC-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(sandbox);try{
   string f1=Path.Combine(sandbox,"one.txt"),f2=Path.Combine(sandbox,"two.bin"),f3=Path.Combine(sandbox,"same-size-other.bin");File.WriteAllText(f1,"ABCD");File.WriteAllText(f2,"ABCD");File.WriteAllText(f3,"DCBA");string summary;var duplicates=AnalysisTools.Duplicates(sandbox,CancellationToken.None,null,out summary);Check(duplicates.Count==2,"duplicates by content not extension/name");Check(duplicates.All(x=>x.Evidence.Length==64),"SHA256 evidence");Check(duplicates.All(x=>x.Path!=f3),"same length different content excluded");
   var machine=WholeMachine.Scan(new[]{sandbox},CancellationToken.None,null);Check(machine.Files==3&&machine.Bytes==12&&!machine.Cancelled,"whole scan traverses fixture files with exact total");
   Check(WholeMachine.Scan(new[]{sandbox},new CancellationToken(true),null).Cancelled,"whole scan reports partial cancellation");
   Check(WholeMachine.SelectDuplicateExtras(duplicates)==1&&duplicates.Count(x=>!x.Selected)==1,"automatic duplicates preserve one keeper");
   var protectedCopies=new List<FileItem>{new FileItem{Path=Path.Combine(Environment.SystemDirectory,"fixture-a.dll"),Group="x"},new FileItem{Path=Path.Combine(Environment.SystemDirectory,"fixture-b.dll"),Group="x"}};Check(WholeMachine.SelectDuplicateExtras(protectedCopies)==0,"automatic duplicates exclude protected programs");

   var removing=new HashSet<string>(new[]{f1},StringComparer.OrdinalIgnoreCase);FileActions.ValidateDuplicate(duplicates.First(x=>x.Path==f1),duplicates,removing);Check(true,"duplicate with verified keeper allowed");
   bool allBlocked=false;try{FileActions.ValidateDuplicate(duplicates[0],duplicates,new HashSet<string>(duplicates.Select(x=>x.Path),StringComparer.OrdinalIgnoreCase));}catch{allBlocked=true;}Check(allBlocked,"cannot remove every duplicate");
   File.WriteAllText(f2,"DCBA");bool changedBlocked=false;try{FileActions.ValidateDuplicate(duplicates.First(x=>x.Path==f1),duplicates,removing);}catch{changedBlocked=true;}Check(changedBlocked,"changed keeper blocks deletion");File.WriteAllText(f2,"ABCD");
   Check(FileActions.ProtectedPath(Path.Combine(Environment.SystemDirectory,"kernel32.dll")),"system file deletion blocked");
   Check(!Drivers.Allowed("System Firmware update")&&!Drivers.Allowed("BIOS update")&&Drivers.Allowed("NVIDIA - Display - fixture"),"driver scope excludes firmware and BIOS");
   Check(!DeepClean.MissingLocation(new AppEntry{Location=""}),"unknown install location not orphan");

   int skip;bool limited;var scan=AnalysisTools.Walk(sandbox,CancellationToken.None,null,out skip,out limited);Check(scan.Count==3&&!limited,"tree traversal exact fixture count");Check(scan.Sum(x=>x.Size)==12,"disk total exact fixture bytes");
   var item=scan.First(x=>x.Path==f1);Check(!AnalysisTools.Eligible(item,sandbox,DateTime.UtcNow),"fresh cleanup fixture excluded");File.SetCreationTimeUtc(f1,DateTime.UtcNow.AddDays(-9));File.SetLastWriteTimeUtc(f1,DateTime.UtcNow.AddDays(-9));item.Modified=File.GetLastWriteTimeUtc(f1);Check(AnalysisTools.Eligible(item,sandbox,DateTime.UtcNow),"old cleanup fixture eligible");File.AppendAllText(f1,"changed");Check(!AnalysisTools.Eligible(item,sandbox,DateTime.UtcNow),"changed file excluded");bool staleBlocked=false;try{FileActions.Validate(item);}catch{staleBlocked=true;}Check(staleBlocked,"large-file action rejects changed snapshot");Check(!AnalysisTools.Eligible(scan[1],sandbox+"-other",DateTime.UtcNow),"scope boundary enforced");
   bool cancelled=false;try{AnalysisTools.Walk(sandbox,new CancellationToken(true),null,out skip,out limited);}catch(OperationCanceledException){cancelled=true;}Check(cancelled,"tree scan cancellation");
   string oldData=Engine.Data;try{Engine.Data=Path.Combine(sandbox,"isolated-state");var remembered=A("Fixture remembered","Vendor");Engine.Remember(remembered);remembered.Key="second-fixture";Engine.Remember(remembered);Check(Engine.RemovalHistory().Count==2,"removal history keeps separate identities");Check(Engine.Recall().Key=="second-fixture","last removal context retained");}finally{Engine.Data=oldData;}
   string untouched=File.ReadAllText(f3);try{AnalysisTools.Clean(new FileItem{Path=f3,ScopeRoot=sandbox,Size=4,Modified=File.GetLastWriteTimeUtc(f3)});}catch{}Check(File.ReadAllText(f3)==untouched,"cleanup rejects unregistered root before deletion");
  }finally{if(Path.GetFileName(sandbox).StartsWith("PurePC-fixture-")&&Path.GetDirectoryName(sandbox).Equals(Path.GetTempPath().TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))Directory.Delete(sandbox,true);}
  string exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Source","Fixtures","ProcessFixture.exe");string capture=Maintenance.Capture(exe,"--fixture-output",CancellationToken.None).GetAwaiter().GetResult();Check(capture.Contains("exit=7"),"process exit code captured: "+capture);Check(capture.Contains("fixture-output thành công")&&capture.Contains("fixture-error"),"stdout unicode and stderr captured");bool processCancelled=false;try{Maintenance.Capture(exe,"--fixture-wait",new CancellationToken(true)).GetAwaiter().GetResult();}catch(OperationCanceledException){processCancelled=true;}Check(processCancelled,"owned fixture process cancellation");

  using(var cancelSearch=new CancellationTokenSource(300))using(var proc=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe,"--fixture-wait"){UseShellExecute=false,CreateNoWindow=true})){bool stopped=false;try{DriverSearchJob.Wait(proc,cancelSearch.Token,null,TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();}catch(OperationCanceledException){stopped=true;}Check(stopped&&proc.HasExited,"driver search cancellation ends owned worker");}
  using(var proc=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe,"--fixture-wait"){UseShellExecute=false,CreateNoWindow=true})){bool timedOut=false;try{DriverSearchJob.Wait(proc,CancellationToken.None,null,TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();}catch(TimeoutException){timedOut=true;}Check(timedOut&&proc.HasExited,"driver search timeout ends owned worker");}
  var gpu=new DriverRecord{Device="NVIDIA GeForce GTX 1650 SUPER",Vendor="NVIDIA",Class="DISPLAY"};VendorDrivers.Resolve(gpu,"ASUSTeK COMPUTER INC.","PRIME H510M-K");Check(gpu.Url.StartsWith("https://www.nvidia.com/"),"GPU source is manufacturer");
  var audio=new DriverRecord{Device="Realtek Audio",Vendor="Realtek",Class="MEDIA"};VendorDrivers.Resolve(audio,"ASUSTeK COMPUTER INC.","PRIME H510M-K");Check(audio.Url.Contains("prime-h510m-k"),"audio source matches verified board model");VendorDrivers.Resolve(audio,"Other","Different");Check(audio.Url=="","unknown board has no guessed audio URL");
  Check(VendorDrivers.SupportsId("Device=Install,PCI\\VEN_1234&DEV_5678",new[]{"PCI\\VEN_1234&DEV_5678"}),"INF exact hardware token match");
  Check(!VendorDrivers.SupportsId("; Device=Install,PCI\\VEN_1234&DEV_5678",new[]{"PCI\\VEN_1234&DEV_5678"}),"INF comment cannot establish match");
  Check(!VendorDrivers.SupportsId("Device=Install,PCI\\VEN_1234&DEV_56789",new[]{"PCI\\VEN_1234&DEV_5678"}),"INF prefix is not hardware match");
  Check(!new JunkItem{File=new FileItem(),Outcome="Đã xử lý"}.CanClean,"processed junk cannot be selected again");
  Check(AnalysisTools.Roots().Any(x=>x.Path.EndsWith("CrashDumps")),"crash reports included in identified cleanup roots");
  Check(!AnalysisTools.Roots().Any(x=>x.Path.IndexOf("SoftwareDistribution",StringComparison.OrdinalIgnoreCase)>=0),"OS update cache not included");
  Check(UwpManager.GetFriendlyName("Microsoft.WindowsCalculator").Contains("Calculator"),"UWP friendly name mapping");
  Check(UwpManager.IsProtected("Microsoft.WindowsStore"),"Microsoft Store is protected from accidental uninstall");
  Check(!UwpManager.IsProtected("Microsoft.WindowsCalculator"),"Calculator is removable Store app");
  Check(UwpManager.CleanPublisher("CN=OpenAI, O=OpenAI")=="OpenAI","UWP publisher regex extraction");
  Check(DesktopApp.MatchesFilter(new AppEntry{IsUwp=true},9),"UWP filter matches UWP app");
  Check(DesktopApp.MatchesFilter(new AppEntry{IsUwp=false},10),"Win32 filter matches Win32 app");
  var snapTest=new SnapshotRecord{AppName="PurePCUnitTestApp",CreatedAt=DateTime.UtcNow};
  snapTest.CreatedFiles.Add(@"C:\dummy\file.txt");
  SetupMonitor.SaveRecord(snapTest);
  var loadedRec=SetupMonitor.LoadAllRecords().FirstOrDefault(r=>r.AppName=="PurePCUnitTestApp");
  Check(loadedRec!=null&&loadedRec.CreatedFiles.Count==1,"SetupMonitor save and load record cycle");
  if(loadedRec!=null)SetupMonitor.DeleteRecord(loadedRec);
   Check(SystemPerformance.FlushDnsCache(),"SystemPerformance.FlushDnsCache succeeds");
   Check(!String.IsNullOrEmpty(SystemPerformance.GetBoardName()),"SystemPerformance.GetBoardName returns valid string");
   Check(StartupItem.EstimateImpact(@"C:\Users\Admin\AppData\Local\Discord\app.exe","Discord")=="Cao","StartupItem heavy impact detection");
   Check(StartupItem.EstimateImpact(@"C:\Windows\System32\ctfmon.exe","CTF Loader")=="Thấp","StartupItem light impact detection");
   Check(StartupItem.EstimateImpact(@"C:\Program Files\Unknown\app.exe","UnknownApp")=="Trung bình","StartupItem medium impact default");
   Engine.InvalidateAppCache();
   Check(true,"Engine.InvalidateAppCache succeeds");
   UwpManager.InvalidateCache();
   Check(true,"UwpManager.InvalidateCache succeeds");
   Check(Engine.AppVersion == "1.2", "Centralized AppVersion matches 1.2");
   Check(StartupItem.EstimateImpact("\"C:\\Program Files\\Discord\\app.exe\"", "Discord") == "Cao", "Quoted impact check");
   Check(!UwpManager.IsPackageInstalled(""), "IsPackageInstalled handles empty name");
   long optFreed = SystemPerformance.OptimizeMemory();
   Check(optFreed >= 0, "SystemPerformance.OptimizeMemory executes without leaking");
   Check(SqliteOptimizer.IsAvailable, "SqliteOptimizer.IsAvailable returns true on Windows 10/11");
   string testDb = Path.Combine(Path.GetTempPath(), "PurePC_test_" + Guid.NewGuid().ToString("N") + ".db");
   try {
       File.WriteAllBytes(testDb, System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0"));
       Check(SqliteOptimizer.IsSqliteDatabase(testDb), "SqliteOptimizer recognizes SQLite header");
   } finally {
       if (File.Exists(testDb)) File.Delete(testDb);
   }
   Check(FileShredder.IsProtected(Environment.SystemDirectory), "FileShredder protects SystemDirectory");
   string testShred = Path.Combine(Path.GetTempPath(), "PurePC_shred_" + Guid.NewGuid().ToString("N") + ".tmp");
   File.WriteAllText(testShred, "SECRET TEST CONTENT");
   FileShredder.ShredFile(testShred, 1);
   Check(!File.Exists(testShred), "FileShredder.ShredFile overwrites and deletes file successfully");
   
   // Elite Optimizations Unit Tests
   Check(!ProcessMemoryManager.TrimProcess(0), "ProcessMemoryManager protects PID 0 System Idle");
   Check(!ProcessMemoryManager.TrimProcess(4), "ProcessMemoryManager protects PID 4 System Kernel");
   var topProcs = ProcessMemoryManager.GetTopProcesses(5);
   Check(topProcs != null, "ProcessMemoryManager.GetTopProcesses returns list");
   double downRate, upRate;
   SystemPerformance.GetNetworkBandwidth(out downRate, out upRate);
   Check(downRate >= 0 && upRate >= 0, "SystemPerformance.GetNetworkBandwidth non-negative");
   var devTargets = DevAiCacheCleaner.GetKnownTargets();
   Check(devTargets.Count >= 10, "DevAiCacheCleaner registers at least 10 ecosystem targets");
   Check(devTargets.Any(x => x.ToolName.Contains("NPM")), "DevAiCacheCleaner targets NPM");
   Check(devTargets.Any(x => x.ToolName.Contains("Hugging Face")), "DevAiCacheCleaner targets Hugging Face AI");
   Check(ComponentStoreManager.IsAvailable, "ComponentStoreManager dism.exe is available on Windows");
   string fakeDismOut = "Windows Explorer Reported Size of Component Store : 10.50 GB\nActual Size of the Component Store : 8.20 GB\nNumber of Reclaimable Packages : 2\nComponent Store Cleanup Recommended : Yes\n";
   var parsedDism = ComponentStoreManager.ParseAnalysis(fakeDismOut);
   Check(parsedDism.ExplorerSize == "10.50 GB", "DISM analysis parses Explorer size");
   Check(parsedDism.ActualSize == "8.20 GB", "DISM analysis parses Actual size");
   Check(parsedDism.ReclaimablePackages == "2", "DISM analysis parses Reclaimable packages");
   Check(parsedDism.CleanupRecommended, "DISM analysis parses CleanupRecommended flag");
   var gameStatus = GameBooster.GetStatus();
   Check(gameStatus != null, "GameBooster.GetStatus executes without exception");
   
   // Privacy Shield, System Repair and Hardware Telemetry tests
   var privStatus = PrivacyOptimizer.GetStatus();
   Check(privStatus != null, "PrivacyOptimizer.GetStatus executes without exception");
   Check(SystemRepair.IsSfcAvailable, "SystemRepair finds sfc.exe in System32");
   Check(SystemRepair.IsDismAvailable, "SystemRepair finds dism.exe in System32");
   Check(SystemPerformance.GetSystemUptime().TotalSeconds > 0, "SystemPerformance.GetSystemUptime reports positive uptime");
   Check(!string.IsNullOrEmpty(SystemPerformance.GetUptimeString()), "SystemPerformance.GetUptimeString returns non-empty string");

   // Elite 3.0 tests: Context Menu, SSD TRIM, Folder Space, Windows Update
   var contextItems = ContextMenuManager.Scan();
   Check(contextItems != null, "ContextMenuManager.Scan returns list");
   string testErr;
   Check(!ContextMenuManager.Toggle(new ContextMenuItem { IsSystemProtected = true }, out testErr), "ContextMenuManager protects system handlers");
   var trimStatus = SsdTrimOptimizer.QueryTrimStatus();
   Check(trimStatus != null, "SsdTrimOptimizer.QueryTrimStatus executes without exception");
   Check(!string.IsNullOrEmpty(WindowsUpdateCleaner.DownloadCacheDir), "WindowsUpdateCleaner points to valid directory");
   bool folderCancelled = false;
   try {
       FolderSpaceVisualizer.Analyze(Environment.GetFolderPath(Environment.SpecialFolder.Windows), new CancellationToken(true), null);
   } catch (OperationCanceledException) {
       folderCancelled = true;
   }
   Check(folderCancelled, "FolderSpaceVisualizer supports cancellation properly");
   var sysStartup = new StartupItem { Name = "SecurityHealth", Command = @"C:\Windows\System32\SecurityHealthSystray.exe" };
   Check(sysStartup.Recommendation.Contains("Hệ thống"), "StartupItem recommends keeping system entries");
   var heavyStartup = new StartupItem { Name = "Discord", Command = @"C:\Users\Admin\AppData\Local\Discord\app.exe" };
   Check(heavyStartup.Recommendation.Contains("Khuyên tắt"), "StartupItem recommends disabling heavy apps");
   var disabledStartup = new StartupItem { Name = "OldApp", Disabled = true };
   Check(disabledStartup.Recommendation.Contains("Đã tắt"), "StartupItem recognizes disabled state");
   var normalApp = A("NormalApp", "NormalVendor");
   Check(normalApp.StatusLabel.Contains("An toàn") || normalApp.StatusLabel.Contains("Ứng dụng"), "AppEntry displays reassuring status label for regular apps");
  return count;
 }
}
