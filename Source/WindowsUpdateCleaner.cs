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
        int errors = 0;

        if (!Engine.IsAdministrator()) {
            sb.AppendLine("! Cảnh báo: PurePC chưa chạy dưới quyền Administrator. Không thể tạm dừng dịch vụ wuauserv/bits hoặc xóa tệp hệ thống trong SoftwareDistribution.");
            report = sb.ToString();
            return false;
        }

        // 1. Tạm dừng dịch vụ Windows Update
        bool stopWu = RunServiceCommand("stop", "wuauserv");
        bool stopBits = RunServiceCommand("stop", "bits");
        if (stopWu || stopBits) {
            sb.AppendLine("✓ Đã tạm dừng dịch vụ Windows Update (wuauserv & bits).");
        } else {
            sb.AppendLine("i Dịch vụ Windows Update hiện đang ở trạng thái nghỉ.");
        }

        // 2. Dọn dẹp thư mục Download
        string dir = DownloadCacheDir;
        if (Directory.Exists(dir)) {
            try {
                var dInfo = new DirectoryInfo(dir);
                foreach (var fi in dInfo.GetFiles("*", SearchOption.AllDirectories)) {
                    try {
                        fi.Attributes = FileAttributes.Normal;
                        long len = fi.Length;
                        fi.Delete();
                        freedBytes += len;
                        filesDeleted++;
                    } catch {
                        errors++;
                    }
                }
                foreach (var sub in dInfo.GetDirectories()) {
                    try {
                        sub.Delete(true);
                    } catch {}
                }
                sb.AppendLine("✓ Đã dọn sạch " + filesDeleted + " tệp bản cập nhật cũ (" + Features.Size(freedBytes) + ")" + (errors > 0 ? " · bỏ qua " + errors + " tệp đang bận." : "."));
            } catch (Exception ex) {
                sb.AppendLine("! Lỗi khi xóa tệp cache: " + ex.Message);
            }
        } else {
            sb.AppendLine("i Thư mục SoftwareDistribution\\Download hiện đang trống.");
        }

        // 3. Khởi động lại dịch vụ (nếu trước đó đang bật hoặc cần làm mới)
        RunServiceCommand("start", "wuauserv");
        RunServiceCommand("start", "bits");
        sb.AppendLine("✓ Đã làm mới và khởi động lại dịch vụ Windows Update.");

        report = sb.ToString();
        return true;
    }

    static bool RunServiceCommand(string verb, string service) {
        try {
            var psi = new ProcessStartInfo("net", verb + " " + service);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            using (var p = Process.Start(psi)) {
                if (p == null) return false;
                var o = p.StandardOutput.ReadToEndAsync();
                var e = p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(6000)) {
                    try { p.Kill(); } catch {}
                    return false;
                }
                return p.ExitCode == 0;
            }
        } catch {
            return false;
        }
    }
}
