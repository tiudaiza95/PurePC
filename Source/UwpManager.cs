using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

public static class UwpManager {
    static readonly Dictionary<string, string> FriendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        { "Microsoft.WindowsCalculator", "Máy tính (Calculator)" },
        { "Microsoft.WindowsCamera", "Máy ảnh (Camera)" },
        { "Microsoft.Windows.Photos", "Xem ảnh (Photos)" },
        { "Microsoft.WindowsSoundRecorder", "Ghi âm (Sound Recorder)" },
        { "Microsoft.Windows.DevHome", "Windows Dev Home" },
        { "Microsoft.XboxGamingOverlay", "Xbox Game Bar" },
        { "Microsoft.XboxGameOverlay", "Xbox Game Overlay" },
        { "Microsoft.XboxIdentityProvider", "Xbox Identity Provider" },
        { "Microsoft.XboxSpeechToTextOverlay", "Xbox Speech To Text" },
        { "Microsoft.Wallet", "Microsoft Wallet (Ví điện tử)" },
        { "Microsoft.VP9VideoExtensions", "VP9 Video Extensions" },
        { "Microsoft.HEIFImageExtension", "HEIF Image Extensions" },
        { "Microsoft.WebpImageExtension", "WebP Image Extensions" },
        { "Microsoft.WebMediaExtensions", "Web Media Extensions" },
        { "MicrosoftCorporationII.WindowsSubsystemForLinux", "Windows Subsystem for Linux (WSL)" },
        { "B9ECED6F.ArmouryCrate", "ASUS Armoury Crate" },
        { "OpenAI.Codex", "ChatGPT Desktop / Codex" },
        { "Claude", "Claude Desktop" },
        { "Microsoft.WindowsStore", "Microsoft Store (Cửa hàng)" },
        { "Microsoft.DesktopAppInstaller", "App Installer (Trình cài đặt gói)" },
        { "Microsoft.MicrosoftEdge.Stable", "Microsoft Edge" }
    };

    static readonly HashSet<string> ProtectedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "Microsoft.WindowsStore",
        "Microsoft.DesktopAppInstaller",
        "Microsoft.MicrosoftEdge.Stable",
        "Microsoft.WindowsTerminal",
        "Microsoft.SecHealthUI",
        "Microsoft.Windows.SecHealthUI"
    };

    public static string GetFriendlyName(string packageName) {
        if (string.IsNullOrEmpty(packageName)) return "Ứng dụng Store";
        string friendly;
        if (FriendlyNames.TryGetValue(packageName, out friendly)) return friendly;
        // Tách chuỗi dạng Microsoft.ZuneVideo -> Zune Video
        string clean = packageName;
        if (clean.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(10);
        return clean;
    }

    public static string CleanPublisher(string rawPublisher) {
        if (string.IsNullOrEmpty(rawPublisher)) return "Microsoft Corporation";
        var mO = Regex.Match(rawPublisher, @"O=""?([^"",]+)""?", RegexOptions.IgnoreCase);
        if (mO.Success) return mO.Groups[1].Value.Trim();
        var mCN = Regex.Match(rawPublisher, @"CN=""?([^"",]+)""?", RegexOptions.IgnoreCase);
        if (mCN.Success) return mCN.Groups[1].Value.Trim();
        return rawPublisher;
    }

    public static bool IsProtected(string packageName) {
        if (string.IsNullOrEmpty(packageName)) return false;
        foreach (var p in ProtectedPackages) {
            if (packageName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    static List<AppEntry> cachedUwp;
    public static void InvalidateCache() { cachedUwp = null; }
    public static List<AppEntry> GetInstalledApps(bool forceRefresh = false) {
        if (cachedUwp != null && !forceRefresh) return cachedUwp;
        var apps = new List<AppEntry>();
        try {
            string cmd = "Get-AppxPackage | Where-Object { -not $_.NonRemovable -and -not $_.IsFramework } | ForEach-Object { ($_.Name, $_.Publisher, $_.Version, $_.InstallLocation, $_.PackageFullName) -join '|||' }";
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"") {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (var proc = Process.Start(psi)) {
                if (proc == null) return apps;
                string line;
                while ((line = proc.StandardOutput.ReadLine()) != null) {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length < 5) continue;

                    string rawName = parts[0].Trim();
                    string rawPub = parts[1].Trim();
                    string version = parts[2].Trim();
                    string loc = parts[3].Trim();
                    string pkgFullName = parts[4].Trim();

                    string friendly = GetFriendlyName(rawName);
                    string pub = CleanPublisher(rawPub);
                    bool isProt = IsProtected(rawName);

                    var entry = new AppEntry {
                        Name = friendly,
                        Publisher = pub,
                        Version = version,
                        Location = loc,
                        PackageFullName = pkgFullName,
                        IsUwp = true,
                        Key = "Appx\\" + pkgFullName,
                        Hive = RegistryHive.CurrentUser,
                        View = RegistryView.Registry64,
                        Command = "powershell -NoProfile -ExecutionPolicy Bypass -Command Remove-AppxPackage -Package '" + pkgFullName + "'"
                    };

                    // Đo dung lượng thư mục nếu có quyền truy cập
                    if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc)) {
                        try {
                            long total = 0;
                            int fCount = 0;
                            foreach (var f in Directory.EnumerateFiles(loc, "*", SearchOption.AllDirectories)) {
                                try { total += new FileInfo(f).Length; if (++fCount > 2500) break; } catch {}
                            }
                            if (total > 0) {
                                entry.EstimatedBytes = total;
                                entry.MeasuredFromDisk = true;
                            }
                        } catch {}
                    }

                    var adv = new Advisory {
                        Category = "Ứng dụng Windows Store (UWP)",
                        Description = "Gói ứng dụng UWP/AppX cài đặt từ Microsoft Store cho tài khoản người dùng hiện tại.",
                        Impact = isProt ? "Thành phần cốt lõi của Windows Store; không được gỡ bỏ." : "Gỡ bỏ ứng dụng Store này khỏi máy tính một cách an toàn và sạch sẽ.",
                        Level = isProt ? "Hệ thống / Store — Chỉ xem" : "Ứng dụng Store",
                        Caution = isProt,
                        Protected = isProt,
                        Url = "https://apps.microsoft.com/"
                    };
                    entry.Advice = adv;

                    apps.Add(entry);
                }
                if (!proc.WaitForExit(25000)) { try { proc.Kill(); } catch {} }
            }
        } catch {}
        cachedUwp = apps;
        return apps;
    }

    public static bool IsPackageInstalled(string packageFullName) {
        if (string.IsNullOrEmpty(packageFullName)) return false;
        try {
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"if (Get-AppxPackage -Package '" + packageFullName + "') { 'YES' }\"") {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using (var proc = Process.Start(psi)) {
                if (proc == null) return false;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(4000);
                return output.Contains("YES");
            }
        } catch {
            return false;
        }
    }

    public static bool RemovePackage(string packageFullName) {
        if (string.IsNullOrEmpty(packageFullName)) return false;
        try {
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"Remove-AppxPackage -Package '" + packageFullName + "'\"") {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using (var proc = Process.Start(psi)) {
                if (proc == null) return false;
                if (!proc.WaitForExit(30000)) { try { proc.Kill(); } catch {} return false; }
                bool success = proc.ExitCode == 0;
                if (success) { InvalidateCache(); Engine.InvalidateAppCache(); }
                return success;
            }
        } catch {
            return false;
        }
    }

    public static string FindUwpIcon(string installLocation) {
        if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation)) return null;
        try {
            string assets = Path.Combine(installLocation, "Assets");
            string searchDir = Directory.Exists(assets) ? assets : installLocation;
            var pngs = Directory.GetFiles(searchDir, "*.png", SearchOption.TopDirectoryOnly);
            if (pngs.Length == 0) return null;

            // Ưu tiên AppList, Square44x44, Logo, StoreLogo
            string target = pngs.FirstOrDefault(p => p.IndexOf("AppList", StringComparison.OrdinalIgnoreCase) >= 0 && p.IndexOf("contrast", StringComparison.OrdinalIgnoreCase) < 0)
                         ?? pngs.FirstOrDefault(p => p.IndexOf("Square44x44", StringComparison.OrdinalIgnoreCase) >= 0 && p.IndexOf("contrast", StringComparison.OrdinalIgnoreCase) < 0)
                         ?? pngs.FirstOrDefault(p => p.IndexOf("Logo", StringComparison.OrdinalIgnoreCase) >= 0 && p.IndexOf("contrast", StringComparison.OrdinalIgnoreCase) < 0)
                         ?? pngs.FirstOrDefault();
            return target;
        } catch {
            return null;
        }
    }
}
