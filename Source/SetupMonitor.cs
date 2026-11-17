using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;

public class SnapshotSession {
    public DateTime StartTime { get; set; }
    public Dictionary<string, DateTime> ExistingFolders { get; set; }
    public HashSet<string> UninstallKeysHklm64 { get; set; }
    public HashSet<string> UninstallKeysHklm32 { get; set; }
    public HashSet<string> UninstallKeysHkcu { get; set; }
    public HashSet<string> RunValuesHklm { get; set; }
    public HashSet<string> RunValuesHkcu { get; set; }

    public SnapshotSession() {
        ExistingFolders = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        UninstallKeysHklm64 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        UninstallKeysHklm32 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        UninstallKeysHkcu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RunValuesHklm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        RunValuesHkcu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}

public class SnapshotRecord {
    public string RecordFile { get; set; }
    public string AppName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> CreatedFolders { get; set; }
    public List<string> CreatedFiles { get; set; }
    public List<string> CreatedRegistryKeys { get; set; }
    public List<string> CreatedRegistryValues { get; set; }

    public SnapshotRecord() {
        CreatedFolders = new List<string>();
        CreatedFiles = new List<string>();
        CreatedRegistryKeys = new List<string>();
        CreatedRegistryValues = new List<string>();
    }
}

public static class SetupMonitor {
    static readonly string[] StandardRoots = {
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
    };

    public static string SnapshotDir {
        get {
            string dir = Path.Combine(Engine.Data, "Snapshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    static bool IsExcludedFolder(string folderPath) {
        if (string.IsNullOrWhiteSpace(folderPath)) return true;
        try {
            string full = Path.GetFullPath(folderPath).TrimEnd('\\');
            string name = Path.GetFileName(full);
            if (string.IsNullOrEmpty(name)) return true;

            // Bỏ qua chính PurePC, thư mục Temp hệ thống, và các thư mục nền của Windows/Trình duyệt
            if (Engine.Within(full, Engine.Data) || Engine.Within(Engine.Data, full)) return true;
            if (Engine.Within(full, Features.TempRoot)) return true;
            if (Engine.Within(full, AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'))) return true;

            string[] noisyNames = {
                "Temp", "Packages", "Microsoft", "Windows", "Package Cache",
                "CrashDumps", "D3DSCache", "Comms", "ConnectedDevicesPlatform",
                "Google", "Mozilla", "BraveSoftware", "CocCoc"
            };
            for (int i = 0; i < noisyNames.Length; i++) {
                if (name.Equals(noisyNames[i], StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        } catch {
            return true;
        }
    }

    public static SnapshotSession StartSession() {
        var s = new SnapshotSession { StartTime = DateTime.UtcNow };

        // 1. Chụp ảnh thư mục
        foreach (var root in StandardRoots.Where(r => !string.IsNullOrEmpty(r) && Directory.Exists(r)).Distinct(StringComparer.OrdinalIgnoreCase)) {
            try {
                foreach (var d in Directory.EnumerateDirectories(root)) {
                    if (IsExcludedFolder(d)) continue;
                    try {
                        s.ExistingFolders[d] = Directory.GetLastWriteTimeUtc(d);
                    } catch {}
                }
            } catch {}
        }

        // 2. Chụp ảnh Registry Uninstall (cả 64-bit & 32-bit) & Run
        Action<RegistryHive, RegistryView, HashSet<string>, HashSet<string>> snapReg = (hive, view, unKeys, runVals) => {
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view)) {
                    if (unKeys != null) {
                        using (var un = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")) {
                            if (un != null) foreach (var k in un.GetSubKeyNames()) unKeys.Add(k);
                        }
                    }
                    if (runVals != null) {
                        using (var run = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")) {
                            if (run != null) foreach (var v in run.GetValueNames()) runVals.Add(v);
                        }
                    }
                }
            } catch {}
        };

        snapReg(RegistryHive.LocalMachine, RegistryView.Registry64, s.UninstallKeysHklm64, s.RunValuesHklm);
        snapReg(RegistryHive.LocalMachine, RegistryView.Registry32, s.UninstallKeysHklm32, null);
        snapReg(RegistryHive.CurrentUser, RegistryView.Registry64, s.UninstallKeysHkcu, s.RunValuesHkcu);

        return s;
    }

    public static SnapshotRecord CompleteSession(SnapshotSession session, string appName) {
        var record = new SnapshotRecord {
            AppName = string.IsNullOrWhiteSpace(appName) ? "Phần mềm chưa đặt tên" : appName.Trim(),
            CreatedAt = DateTime.Now
        };

        DateTime threshold = session.StartTime.AddSeconds(-2);

        // 1. Đối chiếu Thư mục & Tệp phát sinh
        foreach (var root in StandardRoots.Where(r => !string.IsNullOrEmpty(r) && Directory.Exists(r)).Distinct(StringComparer.OrdinalIgnoreCase)) {
            try {
                foreach (var d in Directory.EnumerateDirectories(root)) {
                    if (IsExcludedFolder(d)) continue;
                    DateTime oldTime;
                    bool isNew = !session.ExistingFolders.TryGetValue(d, out oldTime);

                    if (isNew) {
                        if (!record.CreatedFolders.Contains(d, StringComparer.OrdinalIgnoreCase)) {
                            record.CreatedFolders.Add(d);
                        }
                        try {
                            foreach (var f in Directory.EnumerateFiles(d, "*", System.IO.SearchOption.AllDirectories)) {
                                if (!record.CreatedFiles.Contains(f, StringComparer.OrdinalIgnoreCase)) {
                                    record.CreatedFiles.Add(f);
                                }
                            }
                        } catch {}
                    } else if (Directory.GetLastWriteTimeUtc(d) > threshold) {
                        try {
                            foreach (var f in Directory.EnumerateFiles(d, "*", System.IO.SearchOption.TopDirectoryOnly)) {
                                if (File.GetCreationTimeUtc(f) > threshold) {
                                    if (!record.CreatedFiles.Contains(f, StringComparer.OrdinalIgnoreCase)) {
                                        record.CreatedFiles.Add(f);
                                    }
                                }
                            }
                        } catch {}
                    }
                }
            } catch {}
        }

        // 2. Đối chiếu Registry
        Action<RegistryHive, RegistryView, HashSet<string>, HashSet<string>, string> diffReg = (hive, view, oldUn, oldRun, prefix) => {
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view)) {
                    if (oldUn != null) {
                        using (var un = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")) {
                            if (un != null) {
                                foreach (var k in un.GetSubKeyNames()) {
                                    if (!oldUn.Contains(k)) {
                                        record.CreatedRegistryKeys.Add(prefix + @"\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + k);
                                    }
                                }
                            }
                        }
                    }
                    if (oldRun != null) {
                        using (var run = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")) {
                            if (run != null) {
                                foreach (var v in run.GetValueNames()) {
                                    if (!oldRun.Contains(v)) {
                                        record.CreatedRegistryValues.Add(prefix + @"\SOFTWARE\Microsoft\Windows\CurrentVersion\Run [" + v + "]");
                                    }
                                }
                            }
                        }
                    }
                }
            } catch {}
        };

        diffReg(RegistryHive.LocalMachine, RegistryView.Registry64, session.UninstallKeysHklm64, session.RunValuesHklm, "HKLM64");
        diffReg(RegistryHive.LocalMachine, RegistryView.Registry32, session.UninstallKeysHklm32, null, "HKLM32");
        diffReg(RegistryHive.CurrentUser, RegistryView.Registry64, session.UninstallKeysHkcu, session.RunValuesHkcu, "HKCU");

        SaveRecord(record);
        return record;
    }

    public static void SaveRecord(SnapshotRecord record) {
        if (record == null) return;
        try {
            string safeName = Engine.Norm(record.AppName);
            if (string.IsNullOrEmpty(safeName)) safeName = "snapshot";
            if (string.IsNullOrEmpty(record.RecordFile)) {
                record.RecordFile = safeName + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".xml";
            }
            string path = Path.Combine(SnapshotDir, Path.GetFileName(record.RecordFile));
            using (var f = File.Create(path)) {
                new XmlSerializer(typeof(SnapshotRecord)).Serialize(f, record);
            }
        } catch {}
    }

    public static List<SnapshotRecord> LoadAllRecords() {
        var list = new List<SnapshotRecord>();
        try {
            foreach (var f in Directory.GetFiles(SnapshotDir, "*.xml")) {
                try {
                    using (var fs = File.OpenRead(f)) {
                        var rec = (SnapshotRecord)new XmlSerializer(typeof(SnapshotRecord)).Deserialize(fs);
                        if (rec != null) {
                            rec.RecordFile = Path.GetFileName(f);
                            list.Add(rec);
                        }
                    }
                } catch {}
            }
        } catch {}
        return list.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public static void DeleteRecord(SnapshotRecord record) {
        if (record == null) return;
        try {
            if (!string.IsNullOrEmpty(record.RecordFile)) {
                string exactPath = Path.Combine(SnapshotDir, Path.GetFileName(record.RecordFile));
                if (File.Exists(exactPath)) {
                    File.Delete(exactPath);
                    return;
                }
            }
            string safeName = Engine.Norm(record.AppName);
            string legacyPath = Path.Combine(SnapshotDir, safeName + ".xml");
            if (File.Exists(legacyPath)) File.Delete(legacyPath);
        } catch {}
    }

    static void ParseHivePrefix(string keyExpr, out RegistryHive hive, out RegistryView view, out string subPath) {
        if (keyExpr.StartsWith("HKLM32\\", StringComparison.OrdinalIgnoreCase)) {
            hive = RegistryHive.LocalMachine;
            view = RegistryView.Registry32;
            subPath = keyExpr.Substring(7);
        } else if (keyExpr.StartsWith("HKLM64\\", StringComparison.OrdinalIgnoreCase)) {
            hive = RegistryHive.LocalMachine;
            view = RegistryView.Registry64;
            subPath = keyExpr.Substring(7);
        } else if (keyExpr.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)) {
            hive = RegistryHive.LocalMachine;
            view = RegistryView.Registry64;
            subPath = keyExpr.Substring(5);
        } else {
            hive = RegistryHive.CurrentUser;
            view = RegistryView.Registry64;
            subPath = keyExpr.Substring(keyExpr.IndexOf('\\') + 1);
        }
    }

    public static void UninstallMonitoredApp(SnapshotRecord record, out int filesDeleted, out int regDeleted, out List<string> errors) {
        filesDeleted = 0;
        regDeleted = 0;
        errors = new List<string>();

        // 1. Xóa tệp ghi nhận (Gửi vào Recycle Bin để bảo đảm an toàn)
        foreach (var f in record.CreatedFiles) {
            try {
                if (!IsExcludedFolder(Path.GetDirectoryName(f)) && File.Exists(f)) {
                    FileSystem.DeleteFile(f, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    filesDeleted++;
                }
            } catch (Exception ex) {
                errors.Add("Tệp " + f + ": " + ex.Message);
            }
        }

        // 2. Xóa thư mục mới tạo
        foreach (var d in record.CreatedFolders.OrderByDescending(x => x.Length)) {
            try {
                if (!IsExcludedFolder(d) && Directory.Exists(d)) {
                    FileSystem.DeleteDirectory(d, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            } catch (Exception ex) {
                errors.Add("Thư mục " + d + ": " + ex.Message);
            }
        }

        // 3. Xóa Registry Keys
        foreach (var k in record.CreatedRegistryKeys) {
            try {
                RegistryHive hive;
                RegistryView view;
                string subPath;
                ParseHivePrefix(k, out hive, out view, out subPath);
                int lastSlash = subPath.LastIndexOf('\\');
                if (lastSlash > 0) {
                    string parent = subPath.Substring(0, lastSlash);
                    string child = subPath.Substring(lastSlash + 1);
                    using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                    using (var pk = baseKey.OpenSubKey(parent, true)) {
                        if (pk != null) {
                            pk.DeleteSubKeyTree(child, false);
                            regDeleted++;
                        }
                    }
                }
            } catch (Exception ex) {
                errors.Add("Registry " + k + ": " + ex.Message);
            }
        }

        // 4. Xóa Registry Values
        foreach (var v in record.CreatedRegistryValues) {
            try {
                int bracket = v.IndexOf('[');
                if (bracket > 0 && v.EndsWith("]")) {
                    string fullKey = v.Substring(0, bracket).Trim();
                    string valName = v.Substring(bracket + 1, v.Length - bracket - 2).Trim();
                    RegistryHive hive;
                    RegistryView view;
                    string subPath;
                    ParseHivePrefix(fullKey, out hive, out view, out subPath);
                    using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                    using (var sk = baseKey.OpenSubKey(subPath, true)) {
                        if (sk != null) {
                            sk.DeleteValue(valName, false);
                            regDeleted++;
                        }
                    }
                }
            } catch (Exception ex) {
                errors.Add("Registry Value " + v + ": " + ex.Message);
            }
        }

        // Chỉ xóa bản ghi nếu không bị chặn toàn bộ bởi quyền truy cập
        if (filesDeleted > 0 || regDeleted > 0 || errors.Count == 0) {
            DeleteRecord(record);
        }
    }
}
