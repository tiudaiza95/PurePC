using System;
using System.Diagnostics;
using System.IO;
using System.Text;

public static class WindowsUpdateCleaner {
    public static string DownloadCacheDir {
        get {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
        }
    }

    public static long GetCacheBytes(out int fileCount) {
        fileCount = 0;
        long total = 0;
        string dir = DownloadCacheDir;
        if (!Directory.Exists(dir)) return 0;

        try {
            var dInfo = new DirectoryInfo(dir);
            foreach (var fi in dInfo.EnumerateFiles("*", SearchOption.AllDirectories)) {
                try {
                    total += fi.Length;
                    fileCount++;
                } catch {}
            }
        } catch {}
        return total;
    }

    public static bool Purge(out string report, out long freedBytes) {
        freedBytes = 0;
        var sb = new StringBuilder();
        int filesDeleted = 0;

        // 1. Tạm dừng dịch vụ Windows Update
        RunServiceCommand("stop", "wuauserv");
        RunServiceCommand("stop", "bits");
        sb.AppendLine("✓ Đã tạm dừng dịch vụ Windows Update (wuauserv & bits).");

        // 2. Dọn dẹp thư mục Download
        string dir = DownloadCacheDir;
        if (Directory.Exists(dir)) {
            try {
                var dInfo = new DirectoryInfo(dir);
                foreach (var fi in dInfo.GetFiles("*", SearchOption.AllDirectories)) {
                    try {
                        long len = fi.Length;
                        fi.Delete();
                        freedBytes += len;
                        filesDeleted++;
                    } catch {}
                }
                foreach (var sub in dInfo.GetDirectories()) {
                    try {
                        sub.Delete(true);
                    } catch {}
                }
                sb.AppendLine("✓ Đã dọn sạch " + filesDeleted + " tệp bản cập nhật cũ (" + Features.Size(freedBytes) + ").");
            } catch (Exception ex) {
                sb.AppendLine("! Lỗi khi xóa tệp cache: " + ex.Message);
            }
        }

        // 3. Khởi động lại dịch vụ
        RunServiceCommand("start", "wuauserv");
        RunServiceCommand("start", "bits");
        sb.AppendLine("✓ Đã khởi động lại dịch vụ Windows Update.");

        report = sb.ToString();
        return filesDeleted > 0;
    }

    static void RunServiceCommand(string verb, string service) {
        try {
            var psi = new ProcessStartInfo("net", verb + " " + service);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            using (var p = Process.Start(psi)) {
                p.WaitForExit(4000);
            }
        } catch {}
    }
}
