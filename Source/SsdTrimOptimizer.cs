using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

public class SsdTrimStatus {
    public bool IsNtfsTrimEnabled { get; set; }
    public bool IsReFsTrimEnabled { get; set; }
    public bool QuerySucceeded { get; set; }
    public string RawQueryOutput { get; set; }

    public bool IsTrimOptimal {
        get { return IsNtfsTrimEnabled; }
    }

    public string StatusText {
        get {
            if (!QuerySucceeded) return "⚠ Cần quyền Administrator để kiểm tra chi tiết";
            return IsTrimOptimal ? "✓ TRIM đang BẬT (Tối ưu)" : "● TRIM đang TẮT (Cần kích hoạt)";
        }
    }
}

public static class SsdTrimOptimizer {
    static Encoding GetConsoleOemEncoding() {
        try {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        } catch {
            return Encoding.UTF8;
        }
    }

    public static SsdTrimStatus QueryTrimStatus() {
        var status = new SsdTrimStatus();
        try {
            var psi = new ProcessStartInfo("fsutil", "behavior query DisableDeleteNotify");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            using (var p = Process.Start(psi)) {
                if (p != null) {
                    var outTask = p.StandardOutput.ReadToEndAsync();
                    var errTask = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(3000)) {
                        try { p.Kill(); } catch {}
                    } else {
                        p.WaitForExit();
                    }
                    Task.WaitAll(new Task[] { outTask, errTask }, 1500);
                    string output = (outTask.Status == TaskStatus.RanToCompletion ? outTask.Result : "") + "\n" +
                                    (errTask.Status == TaskStatus.RanToCompletion ? errTask.Result : "");
                    status.RawQueryOutput = output.Trim();

                    Match mNtfs = Regex.Match(output, @"NTFS\s*DisableDeleteNotify\s*=\s*(\d)", RegexOptions.IgnoreCase);
                    if (mNtfs.Success) {
                        status.IsNtfsTrimEnabled = mNtfs.Groups[1].Value == "0";
                        status.QuerySucceeded = true;
                    } else if (p.ExitCode == 0 && Regex.IsMatch(output, @"DisableDeleteNotify\s*=\s*0", RegexOptions.IgnoreCase)) {
                        status.IsNtfsTrimEnabled = true;
                        status.QuerySucceeded = true;
                    }

                    Match mRefs = Regex.Match(output, @"ReFS\s*DisableDeleteNotify\s*=\s*(\d)", RegexOptions.IgnoreCase);
                    if (mRefs.Success) {
                        status.IsReFsTrimEnabled = mRefs.Groups[1].Value == "0";
                    }
                }
            }
        } catch {
            status.RawQueryOutput = "Không thể truy vấn fsutil.";
        }

        // Dự phòng đọc trực tiếp từ khóa Registry FileSystem (hoạt động cả khi không có quyền Admin)
        if (!status.QuerySucceeded) {
            try {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem")) {
                    if (k != null) {
                        object val = k.GetValue("DisableDeleteNotification") ?? k.GetValue("DisableDeleteNotify");
                        if (val is int) {
                            status.IsNtfsTrimEnabled = ((int)val == 0);
                            status.QuerySucceeded = true;
                        }
                        object rVal = k.GetValue("RefsDisableDeleteNotification") ?? k.GetValue("RefsDisableDeleteNotify");
                        if (rVal is int) {
                            status.IsReFsTrimEnabled = ((int)rVal == 0);
                        }
                    }
                }
            } catch {}
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
                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(5000)) {
                    try { p.Kill(); } catch {}
                    report = "Quá thời gian chờ fsutil.";
                    return false;
                }
                p.WaitForExit();
                Task.WaitAll(new Task[] { outTask, errTask }, 1500);

                string outStr = outTask.Status == TaskStatus.RanToCompletion ? outTask.Result : "";
                string errStr = errTask.Status == TaskStatus.RanToCompletion ? errTask.Result : "";
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
            Encoding oem = GetConsoleOemEncoding();
            string drive = (driveLetter ?? "C").Trim().TrimEnd('\\', ':') + ":";
            var psi = new ProcessStartInfo("defrag.exe", drive + " /L");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = oem;
            psi.StandardErrorEncoding = oem;

            using (var p = new Process()) {
                p.StartInfo = psi;
                p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine(e.Data.Replace("\0", ""));
                };
                p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine("[Lỗi] " + e.Data.Replace("\0", ""));
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
                p.WaitForExit(); // Flush toàn bộ dòng cuối cùng
                return p.ExitCode;
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
