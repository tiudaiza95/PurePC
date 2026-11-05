using System;
using System.Diagnostics;
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
    public string RawOutput { get; set; }

    public ComponentStoreAnalysisResult() {
        ExplorerSize = "Chưa rõ";
        ActualSize = "Chưa rõ";
        SharedSize = "Chưa rõ";
        BackupsSize = "Chưa rõ";
        CacheSize = "Chưa rõ";
        ReclaimablePackages = "0";
        CleanupRecommended = false;
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

    public static ComponentStoreAnalysisResult ParseAnalysis(string output) {
        var res = new ComponentStoreAnalysisResult();
        res.RawOutput = output ?? "";

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

            var psi = new ProcessStartInfo(DismPath, "/Online /Cleanup-Image /AnalyzeComponentStore");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var proc = Process.Start(psi)) {
                var sb = new StringBuilder();
                while (!proc.WaitForExit(100)) {
                    if (token.IsCancellationRequested) {
                        try { proc.Kill(); } catch {}
                        throw new OperationCanceledException("Đã hủy phân tích WinSxS.");
                    }
                }
                sb.Append(proc.StandardOutput.ReadToEnd());
                sb.Append(proc.StandardError.ReadToEnd());
                return ParseAnalysis(sb.ToString());
            }
        });
    }

    public static Task<int> StartCleanupAsync(bool resetBase, Action<string> onOutputLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsAvailable) throw new FileNotFoundException("Không tìm thấy dism.exe trên hệ thống Windows.");

            string args = "/Online /Cleanup-Image /StartComponentCleanup" + (resetBase ? " /ResetBase" : "");
            var psi = new ProcessStartInfo(DismPath, args);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var proc = new Process()) {
                proc.StartInfo = psi;
                proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onOutputLine != null) {
                        onOutputLine(e.Data);
                    }
                };
                proc.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onOutputLine != null) {
                        onOutputLine("[Error] " + e.Data);
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

                return proc.ExitCode;
            }
        });
    }
}
