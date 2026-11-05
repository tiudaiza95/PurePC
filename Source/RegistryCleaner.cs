using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

public class RegistryIssue {
    public bool Selected { get; set; }
    public string Category { get; set; }
    public RegistryHive Hive { get; set; }
    public RegistryView View { get; set; }
    public string KeyPath { get; set; }
    public string ValueName { get; set; }
    public string ProblemValue { get; set; }
    public string Reason { get; set; }

    public string DisplayKey {
        get {
            string h = Hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
            return h + "\\" + KeyPath + (String.IsNullOrEmpty(ValueName) ? "" : " [" + ValueName + "]");
        }
    }
}

public static class RegistryTools {
    public static List<RegistryIssue> ScanIssues() {
        var issues = new List<RegistryIssue>();

        // 1. Quét Uninstaller mồ côi
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine }) {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 }) {
                try {
                    using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                    using (var unKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")) {
                        if (unKey == null) continue;
                        foreach (string subName in unKey.GetSubKeyNames()) {
                            try {
                                using (var sk = unKey.OpenSubKey(subName)) {
                                    if (sk == null) continue;
                                    string disp = sk.GetValue("DisplayName") as string;
                                    string cmd = sk.GetValue("UninstallString") as string;
                                    string loc = sk.GetValue("InstallLocation") as string;

                                    if (String.IsNullOrWhiteSpace(disp)) continue;

                                    // Nếu InstallLocation được chỉ định nhưng không tồn tại
                                    if (!String.IsNullOrWhiteSpace(loc) && loc.Length > 3 && !Directory.Exists(loc)) {
                                        string exe = ExtractExe(cmd);
                                        if (String.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) {
                                            issues.Add(new RegistryIssue {
                                                Selected = true,
                                                Category = "Mục gỡ cài đặt mồ côi",
                                                Hive = hive,
                                                View = view,
                                                KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + subName,
                                                ValueName = "",
                                                ProblemValue = loc,
                                                Reason = "Ứng dụng '" + disp + "' không còn tồn tại tại thư mục cài đặt."
                                            });
                                        }
                                    }
                                }
                            } catch { }
                        }
                    }
                } catch { }
            }
        }

        // 2. Quét Run / Khởi động mồ côi
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine }) {
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (var runKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")) {
                    if (runKey != null) {
                        foreach (string valName in runKey.GetValueNames()) {
                            try {
                                string val = runKey.GetValue(valName) as string;
                                string exe = ExtractExe(val);
                                if (!String.IsNullOrWhiteSpace(exe) && !File.Exists(exe) && !exe.ToLowerInvariant().Contains("system32") && !exe.ToLowerInvariant().Contains("syswow64")) {
                                    issues.Add(new RegistryIssue {
                                        Selected = true,
                                        Category = "Mục Khởi động mồ côi",
                                        Hive = hive,
                                        View = RegistryView.Registry64,
                                        KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                                        ValueName = valName,
                                        ProblemValue = val,
                                        Reason = "Tệp thực thi không tồn tại: " + exe
                                    });
                                }
                            } catch { }
                        }
                    }
                }
            } catch { }
        }

        // 3. Quét MuiCache lỗi thời
        try {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            using (var muiKey = baseKey.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache")) {
                if (muiKey != null) {
                    foreach (string valName in muiKey.GetValueNames().Take(300)) {
                        try {
                            if (!valName.EndsWith(".ApplicationCompany", StringComparison.OrdinalIgnoreCase) &&
                                !valName.EndsWith(".FriendlyAppName", StringComparison.OrdinalIgnoreCase)) {
                                string path = valName;
                                int pipe = path.IndexOf('|');
                                if (pipe > 0) path = path.Substring(0, pipe);

                                if ((path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) &&
                                    path.Contains(":\\") && !File.Exists(path) && !path.ToLowerInvariant().Contains(@"\windows\")) {
                                    issues.Add(new RegistryIssue {
                                        Selected = true,
                                        Category = "MuiCache lỗi thời",
                                        Hive = RegistryHive.CurrentUser,
                                        View = RegistryView.Registry64,
                                        KeyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache",
                                        ValueName = valName,
                                        ProblemValue = path,
                                        Reason = "Tệp tham chiếu MuiCache đã bị xóa khỏi đĩa."
                                    });
                                }
                            }
                        } catch { }
                    }
                }
            }
        } catch { }

        // 4. Quét App Paths mồ côi
        try {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var appPaths = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths")) {
                if (appPaths != null) {
                    foreach (string sub in appPaths.GetSubKeyNames().Take(150)) {
                        try {
                            using (var sk = appPaths.OpenSubKey(sub)) {
                                if (sk == null) continue;
                                string p = sk.GetValue("") as string;
                                string exe = ExtractExe(p);
                                if (!String.IsNullOrWhiteSpace(exe) && !File.Exists(exe) && !exe.ToLowerInvariant().Contains(@"\windows\")) {
                                    issues.Add(new RegistryIssue {
                                        Selected = true,
                                        Category = "App Paths mồ côi",
                                        Hive = RegistryHive.LocalMachine,
                                        View = RegistryView.Registry64,
                                        KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + sub,
                                        ValueName = "",
                                        ProblemValue = p,
                                        Reason = "Lối tắt App Paths dẫn tới ứng dụng đã xóa."
                                    });
                                }
                            }
                        } catch { }
                    }
                }
            }
        } catch { }

        // 5. Quét Phông chữ mồ côi (Orphaned Fonts)
        try {
            string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var fontsKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts")) {
                if (fontsKey != null) {
                    foreach (string fontName in fontsKey.GetValueNames()) {
                        try {
                            string fontFile = fontsKey.GetValue(fontName) as string;
                            if (string.IsNullOrWhiteSpace(fontFile)) continue;
                            string fullFont = Path.IsPathRooted(fontFile) ? fontFile : Path.Combine(fontsDir, fontFile);
                            if (!File.Exists(fullFont) && !fontFile.Contains("%SystemRoot%")) {
                                issues.Add(new RegistryIssue {
                                    Selected = true,
                                    Category = "Phông chữ mồ côi",
                                    Hive = RegistryHive.LocalMachine,
                                    View = RegistryView.Registry64,
                                    KeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",
                                    ValueName = fontName,
                                    ProblemValue = fontFile,
                                    Reason = "Tập tin phông chữ không tồn tại: " + fontFile
                                });
                            }
                        } catch {}
                    }
                }
            }
        } catch {}

        return issues;
    }

    public static string ExtractExe(string cmd) {
        if (String.IsNullOrWhiteSpace(cmd)) return null;
        cmd = cmd.Trim();
        if (cmd.StartsWith("\"")) {
            int close = cmd.IndexOf('\"', 1);
            if (close > 1) return cmd.Substring(1, close - 1);
        }
        int exeIdx = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIdx > 0) return cmd.Substring(0, exeIdx + 4).Trim('\"');
        return cmd;
    }

    public static int CleanIssues(List<RegistryIssue> chosen, out string backupFile, out List<string> errors) {
        errors = new List<string>();
        int cleaned = 0;

        string backupDir = Path.Combine(Path.GetTempPath(), "PurePC_Backup");
        Directory.CreateDirectory(backupDir);
        backupFile = Path.Combine(backupDir, "Registry_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".reg");

        var sb = new StringBuilder();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();

        // Ghi backup
        foreach (var item in chosen) {
            string h = item.Hive == RegistryHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
            sb.AppendLine("; Backup: " + item.Category + " - " + item.Reason);
            sb.AppendLine("[" + h + "\\" + item.KeyPath + "]");
            if (!String.IsNullOrEmpty(item.ValueName)) {
                sb.AppendLine("\"" + item.ValueName.Replace("\"", "\\\"") + "\"=\"" + (item.ProblemValue ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            }
            sb.AppendLine();
        }

        try {
            File.WriteAllText(backupFile, sb.ToString(), Encoding.Unicode);
        } catch (Exception ex) {
            errors.Add("Lỗi tạo bản sao lưu: " + ex.Message);
        }

        // Tiến hành xóa
        foreach (var item in chosen) {
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(item.Hive, item.View)) {
                    if (String.IsNullOrEmpty(item.ValueName)) {
                        // Xóa cả SubKey
                        int lastSlash = item.KeyPath.LastIndexOf('\\');
                        if (lastSlash > 0) {
                            string parent = item.KeyPath.Substring(0, lastSlash);
                            string child = item.KeyPath.Substring(lastSlash + 1);
                            using (var pk = baseKey.OpenSubKey(parent, true)) {
                                if (pk != null) {
                                    pk.DeleteSubKeyTree(child, false);
                                    cleaned++;
                                }
                            }
                        }
                    } else {
                        // Xóa ValueName
                        using (var k = baseKey.OpenSubKey(item.KeyPath, true)) {
                            if (k != null) {
                                k.DeleteValue(item.ValueName, false);
                                cleaned++;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                errors.Add(item.DisplayKey + ": " + ex.Message);
            }
        }

        return cleaned;
    }
}
