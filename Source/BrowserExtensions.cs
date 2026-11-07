using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

public class BrowserExtensionItem {
    public bool Selected { get; set; }
    public string Browser { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Id { get; set; }
    public string InstallPath { get; set; }
}

public static class BrowserExtensionTools {
    public static List<BrowserExtensionItem> ScanExtensions() {
        var list = new List<BrowserExtensionItem>();
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var browsers = new[] {
            new { Name = "Google Chrome", Path = Path.Combine(local, @"Google\Chrome\User Data") },
            new { Name = "Microsoft Edge", Path = Path.Combine(local, @"Microsoft\Edge\User Data") },
            new { Name = "Brave Browser", Path = Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data") },
            new { Name = "Cốc Cốc", Path = Path.Combine(local, @"CocCoc\Browser\User Data") }
        };

        foreach (var b in browsers) {
            if (!Directory.Exists(b.Path)) continue;
            try {
                // Duyệt qua Default và Profile 1..9
                var profiles = Directory.GetDirectories(b.Path, "Default")
                    .Concat(Directory.GetDirectories(b.Path, "Profile *"));

                foreach (var prof in profiles) {
                    string extDir = Path.Combine(prof, "Extensions");
                    if (!Directory.Exists(extDir)) continue;

                    foreach (var idDir in Directory.GetDirectories(extDir)) {
                        try {
                            string extId = Path.GetFileName(idDir);
                            var verDirs = Directory.GetDirectories(idDir);
                            if (verDirs.Length == 0) continue;

                            string latestVerDir = verDirs.OrderByDescending(x => Path.GetFileName(x)).First();
                            string manifestPath = Path.Combine(latestVerDir, "manifest.json");
                            if (!File.Exists(manifestPath)) continue;

                            string json = File.ReadAllText(manifestPath);
                            string name = ExtractJsonValue(json, "name");
                            string version = ExtractJsonValue(json, "version");
                            string desc = ExtractJsonValue(json, "description");

                            // Nếu tên là chuỗi đa ngôn ngữ __MSG_xxx__
                            if (!String.IsNullOrEmpty(name) && name.StartsWith("__MSG_")) {
                                string key = name.Substring(6).TrimEnd('_');
                                name = ResolveLocaleName(latestVerDir, key) ?? extId;
                            }

                            if (String.IsNullOrWhiteSpace(name)) name = extId;

                            list.Add(new BrowserExtensionItem {
                                Browser = b.Name,
                                Id = extId,
                                Name = name,
                                Version = version ?? "-",
                                Description = desc ?? "",
                                InstallPath = idDir
                            });
                        } catch { }
                    }
                }
            } catch { }
        }

        return list.OrderBy(x => x.Browser).ThenBy(x => x.Name).ToList();
    }

    static string ExtractJsonValue(string json, string key) {
        var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    static string ResolveLocaleName(string verDir, string key) {
        string[] locs = { "vi", "en", "en_US", "en_GB" };
        foreach (var loc in locs) {
            string msgFile = Path.Combine(verDir, "_locales", loc, "messages.json");
            if (File.Exists(msgFile)) {
                try {
                    string txt = File.ReadAllText(msgFile);
                    var m = Regex.Match(txt, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\{[^}]*\"message\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (m.Success) return m.Groups[1].Value;
                } catch { }
            }
        }
        return null;
    }

    static string GetBrowserProcessName(string browserName) {
        if (string.IsNullOrEmpty(browserName)) return null;
        if (browserName.Contains("Chrome")) return "chrome";
        if (browserName.Contains("Edge")) return "msedge";
        if (browserName.Contains("Brave")) return "brave";
        if (browserName.Contains("Cốc Cốc") || browserName.Contains("CocCoc")) return "browser";
        return null;
    }

    public static bool RemoveExtension(BrowserExtensionItem item, out string error) {
        error = "";
        try {
            string proc = GetBrowserProcessName(item.Browser ?? "");
            if (!string.IsNullOrEmpty(proc) && Process.GetProcessesByName(proc).Length > 0) {
                error = "Vui lòng đóng trình duyệt " + item.Browser + " trước khi gỡ tiện ích mở rộng.";
                return false;
            }
            if (Directory.Exists(item.InstallPath)) {
                FileSystem.DeleteDirectory(item.InstallPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                return true;
            }
            return false;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }
}
