using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

public class BrokenShortcut {
    public bool Selected { get; set; }
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
                    try {
                        dynamic sc = shell.CreateShortcut(lnk);
                        string target = sc.TargetPath as string;

                        if (string.IsNullOrWhiteSpace(target)) continue;

                        target = Environment.ExpandEnvironmentVariables(target).Trim();

                        // Bỏ qua các giao thức đặc biệt, URL, shell protocol
                        if (target.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) ||
                            target.StartsWith("::{", StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }

                        // Chỉ kiểm tra các đường dẫn có ổ đĩa cụ thể (C:\, D:\, ...) hoặc UNC
                        if (Path.IsPathRooted(target)) {
                            string root = Path.GetPathRoot(target);
                            if (!string.IsNullOrEmpty(root)) {
                                try {
                                    var drive = new DriveInfo(root);
                                    if (!drive.IsReady) {
                                        // Ổ đĩa di động/USB hoặc ổ mạng tạm thời ngắt kết nối; bỏ qua, không coi là hỏng
                                        continue;
                                    }
                                } catch {
                                    continue;
                                }
                            }
                            if (!File.Exists(target) && !Directory.Exists(target)) {
                                broken.Add(new BrokenShortcut {
                                    Name = Path.GetFileName(lnk),
                                    ShortcutPath = lnk,
                                    TargetPath = target,
                                    LocationCategory = category,
                                    Reason = "Tệp đích không còn tồn tại: " + target
                                });
                            }
                        }
                    } catch {}
                }
            } catch {}
        }

        try {
            if (shell != null && System.Runtime.InteropServices.Marshal.IsComObject(shell)) {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
        } catch {}
        return broken;
    }

    public static int Clean(IEnumerable<BrokenShortcut> items, out List<string> errors) {
        int count = 0;
        errors = new List<string>();

        foreach (var item in items) {
            try {
                if (File.Exists(item.ShortcutPath)) {
                    FileSystem.DeleteFile(item.ShortcutPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    count++;
                }
            } catch (Exception ex) {
                errors.Add(item.Name + ": " + ex.Message);
            }
        }

        return count;
    }
}
