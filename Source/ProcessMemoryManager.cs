using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

public class ProcessMemoryInfo {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }
    public int Threads { get; set; }
    public bool IsSelected { get; set; }
    public string WorkingSetLabel {
        get { return Features.Size(WorkingSetBytes); }
    }
}

public static class ProcessMemoryManager {
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("psapi.dll")]
    static extern int EmptyWorkingSet(IntPtr hwProc);

    const uint PROCESS_SET_QUOTA = 0x0100;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;

    [DllImport("ntdll.dll")]
    static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

    const int SystemMemoryListInformation = 80;
    const int MemoryPurgeStandbyList = 4;

    public static bool TrimProcess(int pid) {
        if (pid <= 4) return false;
        IntPtr hProc = IntPtr.Zero;
        try {
            hProc = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, pid);
            if (hProc != IntPtr.Zero) {
                return EmptyWorkingSet(hProc) != 0;
            }
        } catch {
            return false;
        } finally {
            if (hProc != IntPtr.Zero) CloseHandle(hProc);
        }
        return false;
    }

    public static int OptimizeAll(out long freedBytes) {
        freedBytes = 0;
        int trimmed = 0;
        try {
            var memBefore = SystemPerformance.GetMemory();
            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; i++) {
                Process p = procs[i];
                try {
                    if (p.Id <= 4) continue;
                    if (TrimProcess(p.Id)) {
                        trimmed++;
                    }
                } catch {}
            }
            try {
                int cmd = MemoryPurgeStandbyList;
                NtSetSystemInformation(SystemMemoryListInformation, ref cmd, sizeof(int));
            } catch {}
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var memAfter = SystemPerformance.GetMemory();
            if (memAfter.ullAvailPhys > memBefore.ullAvailPhys) {
                freedBytes = (long)(memAfter.ullAvailPhys - memBefore.ullAvailPhys);
            }
        } catch {}
        return trimmed;
    }

    public static List<ProcessMemoryInfo> GetTopProcesses(int count) {
        var list = new List<ProcessMemoryInfo>();
        try {
            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; i++) {
                Process p = procs[i];
                try {
                    if (p.Id <= 4) continue;
                    long ws = p.WorkingSet64;
                    if (ws < 5L * 1024 * 1024) continue;
                    var item = new ProcessMemoryInfo();
                    item.Id = p.Id;
                    item.Name = p.ProcessName;
                    item.Title = p.MainWindowTitle;
                    item.WorkingSetBytes = ws;
                    item.PrivateMemoryBytes = p.PrivateMemorySize64;
                    item.Threads = p.Threads.Count;
                    item.IsSelected = true;
                    list.Add(item);
                } catch {}
            }
        } catch {}
        return list.OrderByDescending(delegate(ProcessMemoryInfo x) { return x.WorkingSetBytes; }).Take(count).ToList();
    }
}
