using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

public class ThirdPartyService {
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string ImagePath { get; set; }
    public int StartType { get; set; } // 2=Auto, 3=Manual, 4=Disabled
    public string Vendor { get; set; }

    public string StatusLabel {
        get {
            switch (StartType) {
                case 2: return "Tự động (Auto)";
                case 3: return "Thủ công (Manual)";
                case 4: return "Đã tắt (Disabled)";
                default: return "Khác (" + StartType + ")";
            }
        }
    }

    public string CleanPath {
        get {
            if (String.IsNullOrWhiteSpace(ImagePath)) return "";
            string p = ImagePath.Trim();
            if (p.StartsWith("\"")) {
                int next = p.IndexOf('\"', 1);
                if (next > 1) return p.Substring(1, next - 1);
            }
            int sp = p.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (sp > 0) return p.Substring(0, sp + 4).Trim('\"');
            return p;
        }
    }
}

public static class ServiceTools {
    public static List<ThirdPartyService> GetServices() {
        var list = new List<ThirdPartyService>();
        try {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var servicesKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services")) {
                if (servicesKey == null) return list;
                foreach (string name in servicesKey.GetSubKeyNames()) {
                    try {
                        using (var sk = servicesKey.OpenSubKey(name)) {
                            if (sk == null) continue;
                            // Chỉ xét Win32 Own/Share Process services (Type 0x10 hoặc 0x20), bỏ qua Kernel/FileSystem Drivers (Type 1, 2)!
                            object rawType = sk.GetValue("Type");
                            if (rawType is int) {
                                int svcType = (int)rawType;
                                if ((svcType & 0x10) == 0 && (svcType & 0x20) == 0) continue;
                            }

                            string image = sk.GetValue("ImagePath") as string;
                            if (String.IsNullOrWhiteSpace(image)) continue;

                            string lower = Environment.ExpandEnvironmentVariables(image).ToLowerInvariant();
                            // Bảo vệ toàn bộ các service lõi nằm trong thư mục hệ thống Windows (System32, SysWOW64, WindowsApps, Microsoft.NET, Defender)
                            bool isWindowsInternal = lower.Contains(@"\windows\system32\") ||
                                                     lower.Contains(@"\windows\syswow64\") ||
                                                     lower.Contains(@"\windows\servicing\") ||
                                                     lower.Contains(@"\windows\microsoft.net\") ||
                                                     lower.Contains(@"\windows defender\") ||
                                                     lower.Contains(@"system32\svchost.exe") ||
                                                     lower.Contains(@"syswow64\svchost.exe");

                            if (isWindowsInternal) continue;

                            int start = 3;
                            object rawStart = sk.GetValue("Start");
                            if (rawStart is int) start = (int)rawStart;

                            string disp = sk.GetValue("DisplayName") as string;
                            if (String.IsNullOrWhiteSpace(disp) || disp.StartsWith("@")) disp = name;

                            string desc = sk.GetValue("Description") as string ?? "";
                            if (desc.StartsWith("@")) desc = "";

                            string vendor = DetectVendor(disp, name, image);

                            list.Add(new ThirdPartyService {
                                Name = name,
                                DisplayName = disp,
                                Description = desc,
                                ImagePath = image,
                                StartType = start,
                                Vendor = vendor
                            });
                        }
                    } catch { }
                }
            }
        } catch { }

        return list.OrderBy(x => x.DisplayName).ToList();
    }

    static string DetectVendor(string disp, string name, string path) {
        string t = (disp + " " + name + " " + path).ToLowerInvariant();
        if (t.Contains("google") || t.Contains("gupdate") || t.Contains("chrome")) return "Google LLC";
        if (t.Contains("adobe")) return "Adobe Inc.";
        if (t.Contains("nvidia")) return "NVIDIA Corporation";
        if (t.Contains("steam") || t.Contains("valve")) return "Valve Corporation";
        if (t.Contains("intel")) return "Intel Corporation";
        if (t.Contains("amd") || t.Contains("radeon")) return "Advanced Micro Devices (AMD)";
        if (t.Contains("realtek")) return "Realtek Semiconductor";
        if (t.Contains("microsoft")) return "Microsoft Corporation";
        if (t.Contains("brave")) return "Brave Software";
        if (t.Contains("coccoc")) return "Cốc Cốc";
        if (t.Contains("vmware")) return "VMware";
        if (t.Contains("virtualbox") || t.Contains("oracle")) return "Oracle";
        if (t.Contains("anydesk")) return "AnyDesk Software";
        if (t.Contains("teamviewer")) return "TeamViewer";
        if (t.Contains("ultraviewer")) return "UltraViewer";
        if (t.Contains("epic") || t.Contains("unreal")) return "Epic Games";
        if (t.Contains("riot") || t.Contains("vanguard")) return "Riot Games";
        if (t.Contains("blizzard") || t.Contains("battle.net")) return "Blizzard Entertainment";
        if (t.Contains("asus")) return "ASUSTeK Computer";
        if (t.Contains("gigabyte")) return "Giga-Byte Technology";
        if (t.Contains("msi") || t.Contains("micro-star")) return "Micro-Star International";
        return "Bên thứ ba khác";
    }

    public static bool SetStartType(string serviceName, int newStartType, out string message) {
        message = "";
        if (String.IsNullOrWhiteSpace(serviceName) || serviceName.IndexOfAny(new[] { '\"', '\r', '\n', '&', '|' }) >= 0) {
            message = "Tên dịch vụ không hợp lệ.";
            return false;
        }

        string arg = "";
        switch (newStartType) {
            case 2: arg = "start= auto"; break;
            case 3: arg = "start= demand"; break;
            case 4: arg = "start= disabled"; break;
            default:
                message = "Kiểu khởi động không hợp lệ.";
                return false;
        }

        try {
            var psi = new ProcessStartInfo("sc.exe", "config \"" + serviceName + "\" " + arg) {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var proc = Process.Start(psi)) {
                if (proc == null) {
                    message = "Không thể khởi động sc.exe.";
                    return false;
                }
                var outTask = proc.StandardOutput.ReadToEndAsync();
                var errTask = proc.StandardError.ReadToEndAsync();
                if (!proc.WaitForExit(5000)) {
                    try { proc.Kill(); } catch {}
                    message = "Quá thời gian chờ sc.exe.";
                    return false;
                }
                proc.WaitForExit();
                Task.WaitAll(new Task[] { outTask, errTask }, 1500);

                string stdout = outTask.Status == TaskStatus.RanToCompletion ? outTask.Result : "";
                string stderr = errTask.Status == TaskStatus.RanToCompletion ? errTask.Result : "";

                if (proc.ExitCode == 0) {
                    message = "Cập nhật thành công!";
                    return true;
                }

                if (proc.ExitCode == 5 || stderr.ToLowerInvariant().Contains("access is denied") || stdout.ToLowerInvariant().Contains("access is denied")) {
                    message = "Cần quyền Quản trị viên (Run as administrator) để thay đổi thiết lập dịch vụ này.";
                    return false;
                }

                message = !String.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout.Trim();
                return false;
            }
        } catch (Exception ex) {
            message = ex.Message;
            return false;
        }
    }
}
