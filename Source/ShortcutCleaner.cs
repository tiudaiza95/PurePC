using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;

public class BrokenShortcut {
    public bool Selected { get; set; }
    public bool Cleaned { get; set; }
    public string Name { get; set; }
    public string ShortcutPath { get; set; }
    public string TargetPath { get; set; }
    public string LocationCategory { get; set; }
    public string Reason { get; set; }

    public BrokenShortcut() {
        Selected = true;
    }
}

public static class ShortcutCleaner {
    public static bool IsTargetBroken(string rawTarget) {
        if (string.IsNullOrWhiteSpace(rawTarget)) return false;
        string target = Environment.ExpandEnvironmentVariables(rawTarget).Trim();

        if (target.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("::{", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (!Path.IsPathRooted(target)) return false;

        string root = Path.GetPathRoot(target);
        if (string.IsNullOrEmpty(root)) return false;
        try {
            var drive = new DriveInfo(root);
            if (!drive.IsReady) {
                // Ổ đĩa di động/USB hoặc ổ mạng tạm thời ngắt kết nối; bỏ qua, không coi là hỏng
                return false;
            }
        } catch {
            return false;
        }

        return !File.Exists(target) && !Directory.Exists(target);
    }

    public static List<BrokenShortcut> Scan() {
        var broken = new List<BrokenShortcut>();
        dynamic shell = null;
        try {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null) {
                shell = Activator.CreateInstance(shellType);
            }
        } catch {}

        if (shell == null) return broken;

        try {
            var searchDirs = new List<Tuple<string, string>>();

            Action<Environment.SpecialFolder, string> addFolder = (folder, desc) => {
                try {
                    string path = Environment.GetFolderPath(folder);
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) {
                        searchDirs.Add(Tuple.Create(path, desc));
                    }
                } catch {}
            };

            addFolder(Environment.SpecialFolder.Desktop, "Màn hình Desktop cá nhân");
            addFolder(Environment.SpecialFolder.CommonDesktopDirectory, "Màn hình Desktop chung máy");
            addFolder(Environment.SpecialFolder.StartMenu, "Menu Start cá nhân");
            addFolder(Environment.SpecialFolder.CommonStartMenu, "Menu Start chung máy");

            foreach (var dirTuple in searchDirs) {
                string dir = dirTuple.Item1;
                string category = dirTuple.Item2;

                try {
                    var lnkFiles = Directory.GetFiles(dir, "*.lnk", System.IO.SearchOption.AllDirectories);
                    foreach (string lnk in lnkFiles) {
                        dynamic sc = null;
                        try {
                            sc = shell.CreateShortcut(lnk);
                            string target = sc.TargetPath as string;
                            if (IsTargetBroken(target)) {
                                string expanded = Environment.ExpandEnvironmentVariables(target).Trim();
                                broken.Add(new BrokenShortcut {
                                    Name = Path.GetFileName(lnk),
                                    ShortcutPath = lnk,
                                    TargetPath = expanded,
                                    LocationCategory = category,
                                    Reason = "Tệp đích không còn tồn tại: " + expanded
                                });
                            }
                        } catch {} finally {
                            try {
                                if (sc != null && Marshal.IsComObject(sc)) Marshal.FinalReleaseComObject(sc);
                            } catch {}
                        }
                    }
                } catch {}
            }
        } finally {
            try {
                if (shell != null && Marshal.IsComObject(shell)) {
                    Marshal.FinalReleaseComObject(shell);
                }
            } catch {}
        }

        return broken;
    }

    public static int Clean(IEnumerable<BrokenShortcut> items, out List<string> errors) {
        int count = 0;
        errors = new List<string>();

        foreach (var item in items) {
            item.Cleaned = false;
            try {
                if (File.Exists(item.ShortcutPath)) {
                    FileSystem.DeleteFile(item.ShortcutPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    item.Cleaned = true;
                    count++;
                } else {
                    item.Cleaned = true;
                }
            } catch (Exception ex) {
                errors.Add(item.Name + ": " + ex.Message);
            }
        }

        return count;
    }
}
