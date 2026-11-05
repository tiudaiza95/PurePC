using System.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ComFILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using Microsoft.Win32;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class MEMORYSTATUSEX {
 public uint dwLength;
 public uint dwMemoryLoad;
 public ulong ullTotalPhys;
 public ulong ullAvailPhys;
 public ulong ullTotalPageFile;
 public ulong ullAvailPageFile;
 public ulong ullTotalVirtual;
 public ulong ullAvailVirtual;
 public ulong ullAvailExtendedVirtual;
 public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
}

public class SystemMetrics {
 public double CpuUsage { get; set; }
 public ulong TotalRam { get; set; }
 public ulong UsedRam { get; set; }
 public uint RamPercent { get; set; }
 public long DiskTotal { get; set; }
 public long DiskFree { get; set; }
 public double DiskPercent { get; set; }
}

public static class SystemPerformance {
 [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
 public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

 [DllImport("kernel32.dll", SetLastError = true)]
 static extern bool GetSystemTimes(out ComFILETIME idleTime, out ComFILETIME kernelTime, out ComFILETIME userTime);

 static ulong ToUInt64(ComFILETIME ft) {
  return ((ulong)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
 }

 static ulong prevIdle, prevKernel, prevUser;
 static double lastCpuUsage = 5.0;
 static readonly object sync = new object();

 public static double GetCpuUsage() {
  lock (sync) {
   ComFILETIME idle, kernel, user;
   if (!GetSystemTimes(out idle, out kernel, out user)) return lastCpuUsage;
   ulong i = ToUInt64(idle), k = ToUInt64(kernel), u = ToUInt64(user);
   if (prevKernel == 0) {
    prevIdle = i; prevKernel = k; prevUser = u;
    return lastCpuUsage;
   }
   ulong diffIdle = i - prevIdle;
   ulong diffKernel = k - prevKernel;
   ulong diffUser = u - prevUser;
   ulong total = diffKernel + diffUser;
   if (total == 0) return lastCpuUsage;
   prevIdle = i; prevKernel = k; prevUser = u;
   double pct = (double)(total - diffIdle) * 100.0 / total;
   lastCpuUsage = Math.Max(0.5, Math.Min(100.0, pct));
   return lastCpuUsage;
  }
 }

 public static MEMORYSTATUSEX GetMemory() {
  var mem = new MEMORYSTATUSEX();
  try { GlobalMemoryStatusEx(mem); } catch {}
  return mem;
 }

 static string cpuName, osName, gpuName, boardName;

 [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
 static extern int DnsFlushResolverCache();

 [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
 static extern int SHEmptyRecycleBin(IntPtr hwnd, string rootPath, uint flags);

 public static bool FlushDnsCache() {
  try {
   return DnsFlushResolverCache() == 1;
  } catch { return false; }
 }

 public static bool EmptyRecycleBin(IntPtr hwnd = default(IntPtr)) {
  try {
   return SHEmptyRecycleBin(hwnd, null, 7) == 0;
  } catch { return false; }
 }

 public static string GetBoardName() {
  if (boardName != null) return boardName;
  try {
   using (var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS")) {
    if (k != null) {
     string man = Convert.ToString(k.GetValue("BaseBoardManufacturer")).Trim();
     string prod = Convert.ToString(k.GetValue("BaseBoardProduct")).Trim();
     if (!String.IsNullOrEmpty(prod)) {
      boardName = (String.IsNullOrEmpty(man) ? "" : man + " · ") + prod;
      VendorDrivers.Board = boardName;
      return boardName;
     }
    }
   }
  } catch {}
  return VendorDrivers.Board ?? "Bo mạch chủ tiêu chuẩn";
 }

 public static string GetCpuName() {
  if (cpuName != null) return cpuName;
  try {
   using (var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
    if (k != null) cpuName = Convert.ToString(k.GetValue("ProcessorNameString")).Trim();
  } catch {}
  return cpuName ?? "Bộ xử lý Intel/AMD";
 }

 public static string GetOsName() {
  if (osName != null) return osName;
  try {
   using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
    if (k != null) {
     string prod = Convert.ToString(k.GetValue("ProductName")), build = Convert.ToString(k.GetValue("DisplayVersion"));
     osName = prod + (String.IsNullOrEmpty(build) ? "" : " " + build) + (Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit");
    }
  } catch {}
  return osName ?? "Windows 10/11";
 }

 public static string GetGpuName() {
  if (gpuName != null) return gpuName;
  try {
   using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")) {
    if (baseKey != null) {
     foreach (string sub in new[] { "0000", "0001", "0002" }) {
      using (var k = baseKey.OpenSubKey(sub)) {
       if (k != null) {
        string desc = Convert.ToString(k.GetValue("DriverDesc"));
        if (!String.IsNullOrEmpty(desc)) { gpuName = desc; break; }
       }
      }
     }
    }
   }
  } catch {}
  if (gpuName != null) return gpuName;
  try {
   var d = VendorDrivers.Scan().FirstOrDefault(x => x.Class == "DISPLAY");
   if (d != null) gpuName = d.Device;
  } catch {}
  return gpuName ?? "Bộ xử lý đồ họa";
 }

 public static SystemMetrics Sample() {
  var m = new SystemMetrics();
  m.CpuUsage = GetCpuUsage();
  var mem = GetMemory();
  m.TotalRam = mem.ullTotalPhys;
  m.UsedRam = mem.ullTotalPhys > mem.ullAvailPhys ? (mem.ullTotalPhys - mem.ullAvailPhys) : 0;
  m.RamPercent = mem.dwMemoryLoad;
  try {
   var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
   if (drive.IsReady) {
    m.DiskTotal = drive.TotalSize;
    m.DiskFree = drive.AvailableFreeSpace;
    long used = drive.TotalSize - drive.AvailableFreeSpace;
    m.DiskPercent = drive.TotalSize > 0 ? (double)used * 100.0 / drive.TotalSize : 0;
   }
  } catch {}
  return m;
 }

 [DllImport("psapi.dll")]
 static extern int EmptyWorkingSet(IntPtr hwProc);

 public static long OptimizeMemory() {
  long freed = 0;
  try {
   var memBefore = GetMemory();
   foreach (var p in Process.GetProcesses()) {
    try {
     if (p.Id <= 4) continue;
     EmptyWorkingSet(p.Handle);
    } catch {}
   }
   GC.Collect();
   GC.WaitForPendingFinalizers();
   var memAfter = GetMemory();
   if (memAfter.ullAvailPhys > memBefore.ullAvailPhys) {
    freed = (long)(memAfter.ullAvailPhys - memBefore.ullAvailPhys);
   }
  } catch {}
  return freed;
 }
}
