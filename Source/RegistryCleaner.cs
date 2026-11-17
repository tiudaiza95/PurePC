using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

public class RegistryIssue {
    public bool Selected { get; set; }
    public bool Cleaned { get; set; }
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
    public static bool DriveIsReady(string path) {
        if (String.IsNullOrWhiteSpace(path)) return false;
        try {
            string expanded = Environment.ExpandEnvironmentVariables(path).Trim('\"', ' ');
            if (!Path.IsPathRooted(expanded) || expanded.StartsWith(@"\\")) return false;
            string root = Path.GetPathRoot(expanded);
            if (String.IsNullOrEmpty(root)) return false;
            return new DriveInfo(root).IsReady;
        } catch {
            return false;
        }
    }

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
                                    string expLoc = String.IsNullOrWhiteSpace(loc) ? "" : Environment.ExpandEnvironmentVariables(loc).Trim('\"', ' ');

                                    // Chỉ xét khi ổ đĩa cài đặt đang kết nối sẵn sàng (không xóa nhầm ổ di động đang rút)
                                    if (!String.IsNullOrWhiteSpace(expLoc) && expLoc.Length > 3 && DriveIsReady(expLoc) && !Directory.Exists(expLoc)) {
                                        string exe = ExtractExe(cmd);
                                        if (String.IsNullOrWhiteSpace(exe) || !ExeExists(exe)) {
                                            issues.Add(new RegistryIssue {
                                                Selected = true,
                                                Category = "Mục gỡ cài đặt mồ côi",
                                                Hive = hive,
                                                View = view,
                                                KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + subName,
                                                ValueName = "",
                                                ProblemValue = expLoc,
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
                                string val = runKey.GetValue(valName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                                string exe = ExtractExe(val);
                                if (!String.IsNullOrWhiteSpace(exe) &&
                                    (!Path.IsPathRooted(exe) || DriveIsReady(exe)) &&
                                    !ExeExists(exe) &&
                                    !exe.ToLowerInvariant().Contains("system32") &&
                                    !exe.ToLowerInvariant().Contains("syswow64")) {
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
                                string path = Environment.ExpandEnvironmentVariables(valName);
                                int pipe = path.IndexOf('|');
                                if (pipe > 0) path = path.Substring(0, pipe);

                                if ((path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) &&
                                    path.Contains(":\\") && DriveIsReady(path) && !File.Exists(path) && !path.ToLowerInvariant().Contains(@"\windows\")) {
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
                                if (!String.IsNullOrWhiteSpace(exe) &&
                                    (!Path.IsPathRooted(exe) || DriveIsReady(exe)) &&
                                    !ExeExists(exe) &&
                                    !exe.ToLowerInvariant().Contains(@"\windows\")) {
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
            string userFontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Fonts");
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var fontsKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts")) {
                if (fontsKey != null) {
                    foreach (string fontName in fontsKey.GetValueNames()) {
                        try {
                            string fontFile = fontsKey.GetValue(fontName) as string;
                            if (string.IsNullOrWhiteSpace(fontFile)) continue;
                            string expanded = Environment.ExpandEnvironmentVariables(fontFile).Trim('\"', ' ');
                            string fullFont = Path.IsPathRooted(expanded) ? expanded : Path.Combine(fontsDir, expanded);
                            string userFont = Path.IsPathRooted(expanded) ? expanded : Path.Combine(userFontsDir, expanded);
                            if (DriveIsReady(fullFont) && !File.Exists(fullFont) && !File.Exists(userFont)) {
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

    public static bool ExeExists(string exe) {
        if (String.IsNullOrWhiteSpace(exe)) return false;
        string expanded = Environment.ExpandEnvironmentVariables(exe).Trim('\"', ' ');
        if (File.Exists(expanded)) return true;
        if (!Path.IsPathRooted(expanded)) {
            string nameWithExt = expanded.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? expanded : expanded + ".exe";
            string sys = Path.Combine(Environment.SystemDirectory, nameWithExt);
            if (File.Exists(sys)) return true;
            string win = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), nameWithExt);
            if (File.Exists(win)) return true;
            string wow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", nameWithExt);
            if (File.Exists(wow)) return true;
        }
        return false;
    }

    static string GetRegExportKeyHeader(RegistryIssue item) {
        string h = item.Hive == RegistryHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
        string kp = item.KeyPath ?? "";
        if (Environment.Is64BitOperatingSystem &&
            item.Hive == RegistryHive.LocalMachine &&
            item.View == RegistryView.Registry32 &&
            kp.StartsWith(@"SOFTWARE\", StringComparison.OrdinalIgnoreCase) &&
            !kp.StartsWith(@"SOFTWARE\WOW6432Node\", StringComparison.OrdinalIgnoreCase)) {
            kp = @"SOFTWARE\WOW6432Node\" + kp.Substring(9);
        }
        return h + "\\" + kp;
    }

    public static void ExportKeyToReg(StringBuilder sb, RegistryKey baseKey, string keyPath, string fullKeyHeader) {
        using (var k = baseKey.OpenSubKey(keyPath)) {
            if (k == null) return;
            ExportSubKeyRecursive(sb, k, fullKeyHeader);
        }
    }

    static void ExportSubKeyRecursive(StringBuilder sb, RegistryKey key, string keyHeader) {
        sb.AppendLine("[" + keyHeader + "]");
        foreach (string vName in key.GetValueNames()) {
            object val = key.GetValue(vName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            RegistryValueKind kind = key.GetValueKind(vName);
            string vPrefix = String.IsNullOrEmpty(vName) ? "@" : "\"" + vName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            AppendRegValue(sb, vPrefix, val, kind);
        }
        sb.AppendLine();
        foreach (string sub in key.GetSubKeyNames()) {
            using (var child = key.OpenSubKey(sub)) {
                if (child != null) {
                    ExportSubKeyRecursive(sb, child, keyHeader + "\\" + sub);
                }
            }
        }
    }

    static void AppendRegValue(StringBuilder sb, string vPrefix, object val, RegistryValueKind kind) {
        if (kind == RegistryValueKind.DWord) {
            int d = val is int ? (int)val : 0;
            sb.AppendLine(vPrefix + "=dword:" + d.ToString("x8"));
        } else if (kind == RegistryValueKind.QWord) {
            long q = val is long ? (long)val : 0;
            sb.AppendLine(vPrefix + "=hex(b):" + String.Join(",", BitConverter.GetBytes(q).Select(b => b.ToString("x2"))));
        } else if (kind == RegistryValueKind.Binary) {
            byte[] bytes = val as byte[] ?? new byte[0];
            sb.AppendLine(vPrefix + "=hex:" + String.Join(",", bytes.Select(b => b.ToString("x2"))));
        } else if (kind == RegistryValueKind.MultiString) {
            string[] arr = val as string[] ?? new string[0];
            var rawBytes = new List<byte>();
            foreach (string s in arr) {
                rawBytes.AddRange(Encoding.Unicode.GetBytes(s));
                rawBytes.Add(0); rawBytes.Add(0);
            }
            rawBytes.Add(0); rawBytes.Add(0);
            sb.AppendLine(vPrefix + "=hex(7):" + String.Join(",", rawBytes.Select(b => b.ToString("x2"))));
        } else if (kind == RegistryValueKind.ExpandString) {
            byte[] bytes = Encoding.Unicode.GetBytes((val as string ?? "") + "\0");
            sb.AppendLine(vPrefix + "=hex(2):" + String.Join(",", bytes.Select(b => b.ToString("x2"))));
        } else {
            string str = (val ?? "").ToString();
            sb.AppendLine(vPrefix + "=\"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
        }
    }

    public static string ExtractExe(string cmd) {
        if (String.IsNullOrWhiteSpace(cmd)) return null;
        cmd = Environment.ExpandEnvironmentVariables(cmd.Trim());
        if (cmd.StartsWith("\"")) {
            int close = cmd.IndexOf('\"', 1);
            if (close > 1) return cmd.Substring(1, close - 1);
        }
        int exeIdx = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIdx > 0) return cmd.Substring(0, exeIdx + 4).Trim('\"');
        int sp = cmd.IndexOf(' ');
        if (sp > 0) return cmd.Substring(0, sp).Trim('\"');
        return cmd;
    }

    public static int CleanIssues(List<RegistryIssue> chosen, out string backupFile, out List<string> errors) {
        errors = new List<string>();
        int cleaned = 0;

        string backupDir = Path.Combine(Engine.Data, "Registry-backups");
        Directory.CreateDirectory(backupDir);
        backupFile = Path.Combine(backupDir, "Registry_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".reg");

        var sb = new StringBuilder();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();

        // Ghi backup đầy đủ đệ quy
        foreach (var item in chosen) {
            string header = GetRegExportKeyHeader(item);
            sb.AppendLine("; Backup: " + item.Category + " - " + item.Reason);
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(item.Hive, item.View)) {
                    if (String.IsNullOrEmpty(item.ValueName)) {
                        ExportKeyToReg(sb, baseKey, item.KeyPath, header);
                    } else {
                        sb.AppendLine("[" + header + "]");
                        using (var k = baseKey.OpenSubKey(item.KeyPath)) {
                            if (k != null && k.GetValue(item.ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) != null) {
                                object rawVal = k.GetValue(item.ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                                RegistryValueKind kind = k.GetValueKind(item.ValueName);
                                string vPrefix = "\"" + item.ValueName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                                AppendRegValue(sb, vPrefix, rawVal, kind);
                            } else {
                                sb.AppendLine("\"" + item.ValueName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"=\"" + (item.ProblemValue ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
                            }
                        }
                        sb.AppendLine();
                    }
                }
            } catch {
                sb.AppendLine("[" + header + "]");
                sb.AppendLine();
            }
        }

        try {
            File.WriteAllText(backupFile, sb.ToString(), Encoding.Unicode);
        } catch (Exception ex) {
            errors.Add("Lỗi tạo bản sao lưu: " + ex.Message);
            return 0; // Tuyệt đối không xóa Registry nếu sao lưu thất bại!
        }

        // Tiến hành xóa và đánh dấu chính xác mục nào đã xóa thành công
        foreach (var item in chosen) {
            item.Cleaned = false;
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(item.Hive, item.View)) {
                    if (String.IsNullOrEmpty(item.ValueName)) {
                        int lastSlash = item.KeyPath.LastIndexOf('\\');
                        if (lastSlash > 0) {
                            string parent = item.KeyPath.Substring(0, lastSlash);
                            string child = item.KeyPath.Substring(lastSlash + 1);
                            using (var pk = baseKey.OpenSubKey(parent, true)) {
                                if (pk != null) {
                                    pk.DeleteSubKeyTree(child, false);
                                    item.Cleaned = true;
                                    cleaned++;
                                } else {
                                    errors.Add(item.DisplayKey + ": Cần quyền Administrator.");
                                }
                            }
                        }
                    } else {
                        using (var k = baseKey.OpenSubKey(item.KeyPath, true)) {
                            if (k != null) {
                                k.DeleteValue(item.ValueName, false);
                                item.Cleaned = true;
                                cleaned++;
                            } else {
                                errors.Add(item.DisplayKey + ": Cần quyền Administrator.");
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
