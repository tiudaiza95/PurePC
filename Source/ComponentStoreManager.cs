using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class ComponentStoreAnalysisResult {
    public string ExplorerSize { get; set; }
    public string ActualSize { get; set; }
    public string SharedSize { get; set; }
    public string BackupsSize { get; set; }
    public string CacheSize { get; set; }
    public string ReclaimablePackages { get; set; }
    public bool CleanupRecommended { get; set; }
    public int ExitCode { get; set; }
    public bool RequiresElevation { get; set; }
    public string RawOutput { get; set; }

    public ComponentStoreAnalysisResult() {
        ExplorerSize = "Chưa rõ";
        ActualSize = "Chưa rõ";
        SharedSize = "Chưa rõ";
        BackupsSize = "Chưa rõ";
        CacheSize = "Chưa rõ";
        ReclaimablePackages = "0";
        CleanupRecommended = false;
        ExitCode = 0;
        RequiresElevation = false;
        RawOutput = "";
    }
}

public static class ComponentStoreManager {
    public static string DismPath {
        get {
            return Path.Combine(Environment.SystemDirectory, "dism.exe");
        }
    }

    public static bool IsAvailable {
        get {
            return File.Exists(DismPath);
        }
    }

    static Encoding GetConsoleOemEncoding() {
        try {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        } catch {
            return Encoding.UTF8;
        }
    }

    public static ComponentStoreAnalysisResult ParseAnalysis(string output, int exitCode = 0) {
        var res = new ComponentStoreAnalysisResult();
        res.RawOutput = output ?? "";
        res.ExitCode = exitCode;

        if (exitCode == 740 ||
            res.RawOutput.IndexOf("Error: 740", StringComparison.OrdinalIgnoreCase) >= 0 ||
            res.RawOutput.IndexOf("Elevated permissions", StringComparison.OrdinalIgnoreCase) >= 0) {
            res.RequiresElevation = true;
        }

        Match mExp = Regex.Match(res.RawOutput, @"(?:Windows Explorer Reported Size[^\r\n:]*|Dung lượng hiển thị trong Windows Explorer[^\r\n:]*)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mExp.Success) res.ExplorerSize = mExp.Groups[1].Value.Trim();

        Match mAct = Regex.Match(res.RawOutput, @"(?:Actual Size[^\r\n:]*|Dung lượng thực tế[^\r\n:]*)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mAct.Success) res.ActualSize = mAct.Groups[1].Value.Trim();

        Match mShared = Regex.Match(res.RawOutput, @"(?:Shared with Windows|Chia sẻ với Windows)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mShared.Success) res.SharedSize = mShared.Groups[1].Value.Trim();

        Match mBack = Regex.Match(res.RawOutput, @"(?:Backups and Disabled Features|Bản sao lưu và các tính năng đã tắt)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mBack.Success) res.BackupsSize = mBack.Groups[1].Value.Trim();

        Match mCache = Regex.Match(res.RawOutput, @"(?:Cache and Temporary Data|Bộ đệm và Dữ liệu Tạm)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mCache.Success) res.CacheSize = mCache.Groups[1].Value.Trim();

        Match mPkg = Regex.Match(res.RawOutput, @"(?:Number of Reclaimable Packages|Số lượng gói có thể thu hồi)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mPkg.Success) res.ReclaimablePackages = mPkg.Groups[1].Value.Trim();

        Match mRec = Regex.Match(res.RawOutput, @"(?:Component Store Cleanup Recommended|Khuyến nghị dọn dẹp kho thành phần)\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        if (mRec.Success) {
            string val = mRec.Groups[1].Value.Trim();
            res.CleanupRecommended = val.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0 || val.IndexOf("Có", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return res;
    }

    public static Task<ComponentStoreAnalysisResult> AnalyzeAsync(CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsAvailable) throw new FileNotFoundException("Không tìm thấy dism.exe trên hệ thống Windows.");

            Encoding oem = GetConsoleOemEncoding();
            var psi = new ProcessStartInfo(DismPath, "/Online /Cleanup-Image /AnalyzeComponentStore");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = oem;
            psi.StandardErrorEncoding = oem;

            using (var proc = Process.Start(psi)) {
                if (proc == null) throw new InvalidOperationException("Không thể khởi chạy dism.exe.");

                // Đọc đồng thời cả stdout và stderr để chống kẹt pipe buffer (Deadlock)
                var outTask = proc.StandardOutput.ReadToEndAsync();
                var errTask = proc.StandardError.ReadToEndAsync();

                while (!proc.WaitForExit(100)) {
                    if (token.IsCancellationRequested) {
                        try { proc.Kill(); } catch {}
                        throw new OperationCanceledException("Đã hủy phân tích WinSxS.");
                    }
                }
                proc.WaitForExit();
                Task.WaitAll(new Task[] { outTask, errTask }, 3000);

                var sb = new StringBuilder();
                if (outTask.Status == TaskStatus.RanToCompletion) sb.Append(outTask.Result);
                if (errTask.Status == TaskStatus.RanToCompletion && !string.IsNullOrWhiteSpace(errTask.Result)) {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(errTask.Result);
                }
                return ParseAnalysis(sb.ToString(), proc.ExitCode);
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public static Task<int> StartCleanupAsync(bool resetBase, Action<string> onOutputLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsAvailable) throw new FileNotFoundException("Không tìm thấy dism.exe trên hệ thống Windows.");

            Encoding oem = GetConsoleOemEncoding();
            string args = "/Online /Cleanup-Image /StartComponentCleanup" + (resetBase ? " /ResetBase" : "");
            var psi = new ProcessStartInfo(DismPath, args);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = oem;
            psi.StandardErrorEncoding = oem;

            using (var proc = new Process()) {
                proc.StartInfo = psi;
                proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onOutputLine != null) {
                        onOutputLine(e.Data.Replace("\0", ""));
                    }
                };
                proc.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onOutputLine != null) {
                        onOutputLine("[Error] " + e.Data.Replace("\0", ""));
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                while (!proc.WaitForExit(150)) {
                    if (token.IsCancellationRequested) {
                        try { proc.Kill(); } catch {}
                        throw new OperationCanceledException("Đã dừng tác vụ dọn dẹp WinSxS.");
                    }
                }
                proc.WaitForExit(); // Flush toàn bộ dòng cuối cùng

                return proc.ExitCode;
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
