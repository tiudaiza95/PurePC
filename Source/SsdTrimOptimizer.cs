using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class SsdTrimStatus {
    public bool IsNtfsTrimEnabled { get; set; }
    public bool IsReFsTrimEnabled { get; set; }
    public string RawQueryOutput { get; set; }

    public bool IsTrimOptimal {
        get { return IsNtfsTrimEnabled; }
    }

    public string StatusText {
        get {
            return IsTrimOptimal ? "✓ TRIM đang BẬT (Tối ưu)" : "● TRIM đang TẮT (Cần kích hoạt)";
        }
    }
}

public static class SsdTrimOptimizer {
    public static SsdTrimStatus QueryTrimStatus() {
        var status = new SsdTrimStatus();
        try {
            var psi = new ProcessStartInfo("fsutil", "behavior query DisableDeleteNotify");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            using (var p = Process.Start(psi)) {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                status.RawQueryOutput = output;

                Match mNtfs = Regex.Match(output, @"NTFS\s*DisableDeleteNotify\s*=\s*(\d)", RegexOptions.IgnoreCase);
                if (mNtfs.Success) {
                    status.IsNtfsTrimEnabled = mNtfs.Groups[1].Value == "0";
                } else if (output.Contains("= 0")) {
                    status.IsNtfsTrimEnabled = true;
                }

                Match mRefs = Regex.Match(output, @"ReFS\s*DisableDeleteNotify\s*=\s*(\d)", RegexOptions.IgnoreCase);
                if (mRefs.Success) {
                    status.IsReFsTrimEnabled = mRefs.Groups[1].Value == "0";
                }
            }
        } catch {
            status.RawQueryOutput = "Không thể truy vấn fsutil.";
        }
        return status;
    }

    public static bool EnableTrim(out string report) {
        var sb = new StringBuilder();
        try {
            var psi = new ProcessStartInfo("fsutil", "behavior set DisableDeleteNotify 0");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            using (var p = Process.Start(psi)) {
                if (p == null) {
                    report = "Không thể khởi động tiến trình fsutil.";
                    return false;
                }
                string outStr = p.StandardOutput.ReadToEnd();
                string errStr = p.StandardError.ReadToEnd();
                p.WaitForExit(3000);
                string combined = ((outStr ?? "") + " " + (errStr ?? "")).ToLowerInvariant();
                if (p.ExitCode == 0 && !combined.Contains("access is denied") && !combined.Contains("từ chối")) {
                    sb.AppendLine("✓ Đã kích hoạt lệnh TRIM: fsutil behavior set DisableDeleteNotify 0.");
                    if (!string.IsNullOrWhiteSpace(outStr)) sb.AppendLine(outStr.Trim());
                    report = sb.ToString();
                    return true;
                } else {
                    sb.AppendLine("! Lỗi khi kích hoạt TRIM: Cần quyền Administrator.");
                    if (!string.IsNullOrWhiteSpace(errStr)) sb.AppendLine(errStr.Trim());
                    else if (!string.IsNullOrWhiteSpace(outStr)) sb.AppendLine(outStr.Trim());
                    report = sb.ToString();
                    return false;
                }
            }
        } catch (Exception ex) {
            report = "Lỗi khi kích hoạt TRIM: " + ex.Message;
            return false;
        }
    }

    public static Task<int> RunRetrimAsync(string driveLetter, Action<string> onLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            string drive = (driveLetter ?? "C").Trim().TrimEnd('\\', ':') + ":";
            var psi = new ProcessStartInfo("defrag.exe", drive + " /L");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var p = new Process()) {
                p.StartInfo = psi;
                p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine(e.Data);
                };
                p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine("[Lỗi] " + e.Data);
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                while (!p.WaitForExit(150)) {
                    if (token.IsCancellationRequested) {
                        try { p.Kill(); } catch {}
                        throw new OperationCanceledException("Đã dừng quá trình Re-TRIM.");
                    }
                }
                return p.ExitCode;
            }
        });
    }
}
