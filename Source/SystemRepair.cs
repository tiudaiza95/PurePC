using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class SystemRepair {
    public static string SfcPath {
        get { return Path.Combine(Environment.SystemDirectory, "sfc.exe"); }
    }

    public static string DismPath {
        get { return Path.Combine(Environment.SystemDirectory, "dism.exe"); }
    }

    public static bool IsSfcAvailable {
        get { return File.Exists(SfcPath); }
    }

    public static bool IsDismAvailable {
        get { return File.Exists(DismPath); }
    }

    static Encoding GetConsoleOemEncoding() {
        try {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        } catch {
            return Encoding.UTF8;
        }
    }

    static string CleanConsoleLine(string raw) {
        if (string.IsNullOrEmpty(raw)) return null;
        string cleaned = raw.Replace("\0", "").Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }

    public static Task<int> RunSfcScanAsync(Action<string> onLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsSfcAvailable) throw new FileNotFoundException("Không tìm thấy sfc.exe trong thư mục System32.");

            // sfc.exe trên Windows xuất dữ liệu ở định dạng UTF-16LE (Encoding.Unicode)
            var psi = new ProcessStartInfo(SfcPath, "/scannow");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.Unicode;
            psi.StandardErrorEncoding = Encoding.Unicode;

            using (var proc = new Process()) {
                proc.StartInfo = psi;
                proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    string line = CleanConsoleLine(e.Data);
                    if (line != null && onLine != null) onLine(line);
                };
                proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    string line = CleanConsoleLine(e.Data);
                    if (line != null && onLine != null) onLine("[Lỗi] " + line);
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                while (!proc.WaitForExit(150)) {
                    if (token.IsCancellationRequested) {
                        try { proc.Kill(); } catch {}
                        throw new OperationCanceledException("Đã dừng quá trình quét SFC.");
                    }
                }
                proc.WaitForExit(); // Flush toàn bộ sự kiện OutputDataReceived còn trong buffer
                return proc.ExitCode;
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public static Task<int> RunDismRestoreHealthAsync(Action<string> onLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsDismAvailable) throw new FileNotFoundException("Không tìm thấy dism.exe trong thư mục System32.");

            Encoding oem = GetConsoleOemEncoding();
            var psi = new ProcessStartInfo(DismPath, "/Online /Cleanup-Image /RestoreHealth");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = oem;
            psi.StandardErrorEncoding = oem;

            using (var proc = new Process()) {
                proc.StartInfo = psi;
                proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    string line = CleanConsoleLine(e.Data);
                    if (line != null && onLine != null) onLine(line);
                };
                proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    string line = CleanConsoleLine(e.Data);
                    if (line != null && onLine != null) onLine("[Lỗi] " + line);
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                while (!proc.WaitForExit(150)) {
                    if (token.IsCancellationRequested) {
                        try { proc.Kill(); } catch {}
                        throw new OperationCanceledException("Đã dừng quá trình phục hồi DISM.");
                    }
                }
                proc.WaitForExit(); // Flush toàn bộ sự kiện OutputDataReceived còn trong buffer
                return proc.ExitCode;
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    static bool RunNetshCommand(string args, out string output) {
        output = "";
        try {
            var psi = new ProcessStartInfo("netsh", args) {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = Process.Start(psi)) {
                if (p == null) return false;
                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(8000)) {
                    try { p.Kill(); } catch {}
                    output = "Quá thời gian chờ netsh.";
                    return false;
                }
                p.WaitForExit();
                Task.WaitAll(new Task[] { outTask, errTask }, 2000);
                output = ((outTask.Status == TaskStatus.RanToCompletion ? outTask.Result : "") + "\n" +
                          (errTask.Status == TaskStatus.RanToCompletion ? errTask.Result : "")).Trim();
                return p.ExitCode == 0;
            }
        } catch (Exception ex) {
            output = ex.Message;
            return false;
        }
    }

    public static bool ResetNetworkStack(out string report) {
        var sb = new StringBuilder();
        int resetOk = 0;

        string wsOut;
        if (RunNetshCommand("winsock reset", out wsOut)) {
            sb.AppendLine("✓ Đã thiết lập lại danh mục Winsock Catalog (netsh winsock reset).");
            resetOk++;
        } else {
            sb.AppendLine("! Không thể đặt lại Winsock (cần quyền Administrator): " + wsOut);
        }

        string ipOut;
        if (RunNetshCommand("int ip reset", out ipOut)) {
            sb.AppendLine("✓ Đã thiết lập lại ngăn xếp TCP/IP (netsh int ip reset).");
            resetOk++;
        } else {
            sb.AppendLine("! Không thể đặt lại TCP/IP (cần quyền Administrator): " + ipOut);
        }

        if (SystemPerformance.FlushDnsCache()) {
            sb.AppendLine("✓ Đã làm mới bộ nhớ đệm phân giải DNS (DnsFlushResolverCache).");
        }

        report = sb.ToString();
        return resetOk == 2;
    }
}
