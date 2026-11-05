using System;
using System.Diagnostics;
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

    public static Task<int> RunSfcScanAsync(Action<string> onLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsSfcAvailable) throw new FileNotFoundException("Không tìm thấy sfc.exe trong thư mục System32.");

            var psi = new ProcessStartInfo(SfcPath, "/scannow");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var proc = new Process()) {
                proc.StartInfo = psi;
                proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine(e.Data);
                };
                proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine("[Lỗi] " + e.Data);
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
                return proc.ExitCode;
            }
        });
    }

    public static Task<int> RunDismRestoreHealthAsync(Action<string> onLine, CancellationToken token) {
        return Task.Factory.StartNew(delegate() {
            if (!IsDismAvailable) throw new FileNotFoundException("Không tìm thấy dism.exe trong thư mục System32.");

            var psi = new ProcessStartInfo(DismPath, "/Online /Cleanup-Image /RestoreHealth");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var proc = new Process()) {
                proc.StartInfo = psi;
                proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine(e.Data);
                };
                proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data) && onLine != null) onLine("[Lỗi] " + e.Data);
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
                return proc.ExitCode;
            }
        });
    }

    public static bool ResetNetworkStack(out string report) {
        var sb = new StringBuilder();
        int ok = 0;

        try {
            var psi = new ProcessStartInfo("netsh", "winsock reset");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            using (var p = Process.Start(psi)) {
                p.WaitForExit(5000);
                if (p.ExitCode == 0) {
                    sb.AppendLine("✓ Đã thiết lập lại danh mục Winsock Catalog (netsh winsock reset).");
                    ok++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi Winsock reset: " + ex.Message);
        }

        try {
            var psi = new ProcessStartInfo("netsh", "int ip reset");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            using (var p = Process.Start(psi)) {
                p.WaitForExit(5000);
                if (p.ExitCode == 0) {
                    sb.AppendLine("✓ Đã thiết lập lại ngăn xếp TCP/IP (netsh int ip reset).");
                    ok++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi TCP/IP reset: " + ex.Message);
        }

        if (SystemPerformance.FlushDnsCache()) {
            sb.AppendLine("✓ Đã làm mới bộ nhớ đệm phân giải DNS (DnsFlushResolverCache).");
            ok++;
        }

        report = sb.ToString();
        return ok >= 2;
    }
}
