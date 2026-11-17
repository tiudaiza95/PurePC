using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

public class GameBoostStatus {
    public bool IsTcpOptimized { get; set; }
    public bool IsSystemResponsivenessOptimized { get; set; }
    public bool IsNetworkThrottlingDisabled { get; set; }
    public bool IsGamesPriorityOptimized { get; set; }
    public bool IsFullyOptimized {
        get {
            return IsTcpOptimized && IsSystemResponsivenessOptimized && IsNetworkThrottlingDisabled && IsGamesPriorityOptimized;
        }
    }
}

public static class GameBooster {
    const string SysProfileKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
    const string GamesKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
    const string TcpInterfacesKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
    const string UltimateTemplateGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    public static GameBoostStatus GetStatus() {
        var status = new GameBoostStatus();
        try {
            using (var k = Registry.LocalMachine.OpenSubKey(SysProfileKey)) {
                if (k != null) {
                    object resp = k.GetValue("SystemResponsiveness");
                    if (resp is int && (int)resp == 0) status.IsSystemResponsivenessOptimized = true;

                    object thrott = k.GetValue("NetworkThrottlingIndex");
                    if (thrott is int && (int)thrott == -1) status.IsNetworkThrottlingDisabled = true;
                }
            }
        } catch {}

        try {
            using (var k = Registry.LocalMachine.OpenSubKey(GamesKey)) {
                if (k != null) {
                    object gpu = k.GetValue("GPU Priority");
                    object cat = k.GetValue("Scheduling Category");
                    if (gpu is int && (int)gpu >= 8 && "High".Equals(cat as string, StringComparison.OrdinalIgnoreCase)) {
                        status.IsGamesPriorityOptimized = true;
                    }
                }
            }
        } catch {}

        try {
            using (var k = Registry.LocalMachine.OpenSubKey(TcpInterfacesKey)) {
                if (k != null) {
                    string[] subKeys = k.GetSubKeyNames();
                    bool anyOptimized = false;
                    for (int i = 0; i < subKeys.Length; i++) {
                        using (var sub = k.OpenSubKey(subKeys[i])) {
                            if (sub != null) {
                                object ack = sub.GetValue("TcpAckFrequency");
                                object noDelay = sub.GetValue("TCPNoDelay");
                                if (ack is int && (int)ack == 1 && noDelay is int && (int)noDelay == 1) {
                                    anyOptimized = true;
                                    break;
                                }
                            }
                        }
                    }
                    status.IsTcpOptimized = anyOptimized;
                }
            }
        } catch {}

        return status;
    }

    static bool RunPowerCfg(string args, out string output) {
        output = "";
        try {
            var psi = new ProcessStartInfo("powercfg", args) {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var proc = Process.Start(psi)) {
                if (proc == null) return false;
                var o = proc.StandardOutput.ReadToEndAsync();
                var e = proc.StandardError.ReadToEndAsync();
                if (!proc.WaitForExit(4000)) {
                    try { proc.Kill(); } catch {}
                    return false;
                }
                proc.WaitForExit();
                Task.WaitAll(new Task[] { o, e }, 1500);
                output = (o.Status == TaskStatus.RanToCompletion ? o.Result : "") + "\n" +
                         (e.Status == TaskStatus.RanToCompletion ? e.Result : "");
                return proc.ExitCode == 0;
            }
        } catch {
            return false;
        }
    }

    public static bool ApplyOptimizations(out string report) {
        var sb = new System.Text.StringBuilder();
        int successCount = 0;

        // 1. System Responsiveness & Network Throttling
        try {
            using (var k = Registry.LocalMachine.CreateSubKey(SysProfileKey)) {
                if (k != null) {
                    k.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                    k.SetValue("NetworkThrottlingIndex", -1, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Tối ưu hóa SystemResponsiveness (0%) & Tắt Network Throttling (0xFFFFFFFF).");
                    successCount++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình Multimedia SystemProfile (cần quyền Administrator): " + ex.Message);
        }

        // 2. Games Task Priority
        try {
            using (var k = Registry.LocalMachine.CreateSubKey(GamesKey)) {
                if (k != null) {
                    k.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                    k.SetValue("Priority", 6, RegistryValueKind.DWord);
                    k.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                    k.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                    sb.AppendLine("✓ Cấu hình ưu tiên GPU Priority (8) & Luồng Game (High).");
                    successCount++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình Tasks Games (cần quyền Administrator): " + ex.Message);
        }

        // 3. TCP/IP Latency & Ack Frequency
        int tcpCount = 0;
        try {
            using (var k = Registry.LocalMachine.OpenSubKey(TcpInterfacesKey, true)) {
                if (k != null) {
                    string[] subKeys = k.GetSubKeyNames();
                    for (int i = 0; i < subKeys.Length; i++) {
                        using (var sub = k.OpenSubKey(subKeys[i], true)) {
                            if (sub != null) {
                                object ip = sub.GetValue("IPAddress");
                                object dhcp = sub.GetValue("DhcpIPAddress");
                                if (ip != null || dhcp != null) {
                                    sub.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                    sub.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                    tcpCount++;
                                }
                            }
                        }
                    }
                }
            }
            if (tcpCount > 0) {
                sb.AppendLine("✓ Kích hoạt TcpAckFrequency=1 & TCPNoDelay=1 cho " + tcpCount + " card mạng.");
                successCount++;
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình card mạng TCP/IP (cần quyền Administrator): " + ex.Message);
        }

        // 4. Windows Ultimate Performance Scheme (Kiểm tra tồn tại trước để không nhân bản trùng lặp!)
        if (Engine.IsAdministrator()) {
            try {
                string listOut;
                string targetGuid = null;
                if (RunPowerCfg("/list", out listOut)) {
                    foreach (string line in listOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                        if (line.IndexOf("Ultimate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            line.IndexOf("Hiệu suất tối tối đa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            line.IndexOf(UltimateTemplateGuid, StringComparison.OrdinalIgnoreCase) >= 0) {
                            Match m = Regex.Match(line, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                            if (m.Success) { targetGuid = m.Value; break; }
                        }
                    }
                }
                if (targetGuid == null) {
                    string dupOut;
                    if (RunPowerCfg("/duplicatescheme " + UltimateTemplateGuid, out dupOut)) {
                        Match m = Regex.Match(dupOut, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                        if (m.Success) targetGuid = m.Value;
                    }
                }
                if (targetGuid != null) {
                    string actOut;
                    if (RunPowerCfg("/setactive " + targetGuid, out actOut)) {
                        sb.AppendLine("✓ Đã kích hoạt gói năng lượng Ultimate Performance (" + targetGuid + ").");
                    }
                }
            } catch {}
        }

        report = sb.ToString();
        return successCount >= 2;
    }

    public static bool RestoreDefaults(out string report) {
        var sb = new System.Text.StringBuilder();
        int restoredCount = 0;

        // 1. Restore SystemProfile
        try {
            using (var k = Registry.LocalMachine.CreateSubKey(SysProfileKey)) {
                if (k != null) {
                    k.SetValue("SystemResponsiveness", 20, RegistryValueKind.DWord);
                    k.SetValue("NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Khôi phục SystemResponsiveness (20%) & NetworkThrottlingIndex (10).");
                    restoredCount++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi khôi phục Multimedia SystemProfile (cần quyền Administrator): " + ex.Message);
        }

        // 2. Restore Games Task Priority
        try {
            using (var k = Registry.LocalMachine.CreateSubKey(GamesKey)) {
                if (k != null) {
                    k.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                    k.SetValue("Priority", 2, RegistryValueKind.DWord);
                    k.SetValue("Scheduling Category", "Medium", RegistryValueKind.String);
                    k.SetValue("SFIO Priority", "Normal", RegistryValueKind.String);
                    sb.AppendLine("✓ Khôi phục ưu tiên luồng Game về mặc định Windows (Medium).");
                    restoredCount++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi khôi phục Tasks Games (cần quyền Administrator): " + ex.Message);
        }

        // 3. Restore TCP/IP Interfaces
        int restoredAdapters = 0;
        try {
            using (var k = Registry.LocalMachine.OpenSubKey(TcpInterfacesKey, true)) {
                if (k != null) {
                    string[] subKeys = k.GetSubKeyNames();
                    for (int i = 0; i < subKeys.Length; i++) {
                        using (var sub = k.OpenSubKey(subKeys[i], true)) {
                            if (sub != null) {
                                sub.DeleteValue("TcpAckFrequency", false);
                                sub.DeleteValue("TCPNoDelay", false);
                                restoredAdapters++;
                            }
                        }
                    }
                }
            }
            if (restoredAdapters > 0) {
                sb.AppendLine("✓ Đã xóa TcpAckFrequency & TCPNoDelay tùy chỉnh trên các card mạng.");
                restoredCount++;
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi khôi phục card mạng (cần quyền Administrator): " + ex.Message);
        }

        // 4. Khôi phục gói nguồn Balanced mặc định của Windows
        if (Engine.IsAdministrator()) {
            string balOut;
            if (RunPowerCfg("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", out balOut)) {
                sb.AppendLine("✓ Đã chuyển gói năng lượng về mặc định Balanced của Windows.");
            }
        }

        report = sb.ToString();
        return restoredCount >= 2;
    }
}
