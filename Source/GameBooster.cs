using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            sb.AppendLine("! Lỗi cấu hình Multimedia SystemProfile: " + ex.Message);
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
            sb.AppendLine("! Lỗi cấu hình Tasks Games: " + ex.Message);
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
                                // Only configure if adapter has IPAddress or DhcpIPAddress
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
            sb.AppendLine("! Lỗi cấu hình card mạng TCP/IP: " + ex.Message);
        }

        // 4. Windows Ultimate Performance Scheme
        try {
            var psi = new ProcessStartInfo("powercfg", "/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            using (var proc = Process.Start(psi)) {
                proc.WaitForExit(3000);
            }
            sb.AppendLine("✓ Mở khóa gói năng lượng Ultimate Performance (Hiệu năng tối đa).");
            successCount++;
        } catch {}

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
            sb.AppendLine("! Lỗi khôi phục Multimedia SystemProfile: " + ex.Message);
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
            sb.AppendLine("! Lỗi khôi phục Tasks Games: " + ex.Message);
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
            sb.AppendLine("! Lỗi khôi phục card mạng: " + ex.Message);
        }

        report = sb.ToString();
        return restoredCount > 0;
    }
}
