using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

public class PrivacyStatus {
    public bool IsTelemetryDisabled { get; set; }
    public bool IsBingSearchDisabled { get; set; }
    public bool IsAdvertisingIdDisabled { get; set; }
    public bool IsActivityHistoryDisabled { get; set; }
    public bool IsFullyHardened {
        get {
            return IsTelemetryDisabled && IsBingSearchDisabled && IsAdvertisingIdDisabled && IsActivityHistoryDisabled;
        }
    }
}

public static class PrivacyOptimizer {
    public static PrivacyStatus GetStatus() {
        var status = new PrivacyStatus();
        try {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection")) {
                if (k != null) {
                    object val = k.GetValue("AllowTelemetry");
                    if (val is int && (int)val == 0) status.IsTelemetryDisabled = true;
                }
            }
        } catch {}

        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search")) {
                if (k != null) {
                    object bing = k.GetValue("BingSearchEnabled");
                    if (bing is int && (int)bing == 0) status.IsBingSearchDisabled = true;
                }
            }
        } catch {}

        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo")) {
                if (k != null) {
                    object ad = k.GetValue("Enabled");
                    if (ad is int && (int)ad == 0) status.IsAdvertisingIdDisabled = true;
                }
            }
        } catch {}

        try {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System")) {
                if (k != null) {
                    object act = k.GetValue("EnableActivityFeed");
                    if (act is int && (int)act == 0) status.IsActivityHistoryDisabled = true;
                }
            }
        } catch {}

        return status;
    }

    public static bool ApplyHardening(out string report) {
        var sb = new System.Text.StringBuilder();
        int okCount = 0;

        // 1. Tắt Telemetry & Thu thập dữ liệu chẩn đoán
        try {
            using (var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection")) {
                if (k != null) {
                    k.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                    k.SetValue("MaxTelemetryAllowed", 0, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Đã tắt Diagnostic Data Telemetry trong Group Policy.");
                    okCount++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình DataCollection: " + ex.Message);
        }

        // Tắt service DiagTrack nếu có thể
        try {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\DiagTrack", true)) {
                if (k != null) {
                    k.SetValue("Start", 4, RegistryValueKind.DWord); // Disabled
                    sb.AppendLine("✓ Đã vô hiệu hóa dịch vụ DiagTrack (Connected User Experiences).");
                    okCount++;
                }
            }
        } catch {}

        // 2. Tắt Bing Search & gợi ý web trong Start Menu
        try {
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search")) {
                if (k != null) {
                    k.SetValue("BingSearchEnabled", 0, RegistryValueKind.DWord);
                    k.SetValue("CortanaConsent", 0, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Đã tắt tìm kiếm Bing và quảng cáo trong Menu Start.");
                    okCount++;
                }
            }
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer")) {
                if (k != null) {
                    k.SetValue("DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình Search: " + ex.Message);
        }

        // 3. Tắt Advertising ID & Tailored Experiences
        try {
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo")) {
                if (k != null) {
                    k.SetValue("Enabled", 0, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Đã tắt Mã nhận dạng quảng cáo cá nhân (Advertising ID).");
                    okCount++;
                }
            }
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Privacy")) {
                if (k != null) {
                    k.SetValue("TailoredExperiencesWithDiagnosticDataEnabled", 0, RegistryValueKind.DWord);
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình Advertising: " + ex.Message);
        }

        // 4. Tắt Activity History / Timeline Feed
        try {
            using (var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System")) {
                if (k != null) {
                    k.SetValue("EnableActivityFeed", 0, RegistryValueKind.DWord);
                    k.SetValue("PublishUserActivities", 0, RegistryValueKind.DWord);
                    k.SetValue("UploadUserActivities", 0, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Đã vô hiệu hóa theo dõi dòng thời gian hoạt động (Activity History).");
                    okCount++;
                }
            }
        } catch (Exception ex) {
            sb.AppendLine("! Lỗi cấu hình ActivityFeed: " + ex.Message);
        }

        report = sb.ToString();
        return okCount >= 2;
    }

    public static int ClearExplorerMruHistory(out string report) {
        var sb = new System.Text.StringBuilder();
        int cleared = 0;

        // Xóa RecentDocs
        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs", true)) {
                if (k != null) {
                    string[] sub = k.GetSubKeyNames();
                    for (int i = 0; i < sub.Length; i++) {
                        k.DeleteSubKeyTree(sub[i], false);
                    }
                    string[] vals = k.GetValueNames();
                    for (int i = 0; i < vals.Length; i++) {
                        k.DeleteValue(vals[i], false);
                    }
                    sb.AppendLine("✓ Đã dọn sạch lịch sử tài liệu mở gần đây (RecentDocs).");
                    cleared++;
                }
            }
        } catch {}

        // Xóa RunMRU
        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", true)) {
                if (k != null) {
                    string[] vals = k.GetValueNames();
                    for (int i = 0; i < vals.Length; i++) {
                        k.DeleteValue(vals[i], false);
                    }
                    sb.AppendLine("✓ Đã dọn sạch lịch sử hộp thoại Run (Windows + R).");
                    cleared++;
                }
            }
        } catch {}

        // Xóa TypedPaths
        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths", true)) {
                if (k != null) {
                    string[] vals = k.GetValueNames();
                    for (int i = 0; i < vals.Length; i++) {
                        k.DeleteValue(vals[i], false);
                    }
                    sb.AppendLine("✓ Đã dọn sạch lịch sử thanh địa chỉ File Explorer.");
                    cleared++;
                }
            }
        } catch {}

        report = sb.ToString();
        return cleared;
    }

    public static bool RestoreDefaults(out string report) {
        var sb = new System.Text.StringBuilder();
        int restored = 0;

        try {
            using (var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection")) {
                if (k != null) {
                    k.DeleteValue("AllowTelemetry", false);
                    k.DeleteValue("MaxTelemetryAllowed", false);
                    sb.AppendLine("✓ Đã khôi phục thiết lập thu thập Telemetry mặc định.");
                    restored++;
                }
            }
            using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\DiagTrack", true)) {
                if (k != null) {
                    k.SetValue("Start", 2, RegistryValueKind.DWord); // Automatic
                    restored++;
                }
            }
        } catch {}

        try {
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search")) {
                if (k != null) {
                    k.DeleteValue("BingSearchEnabled", false);
                    k.DeleteValue("CortanaConsent", false);
                    sb.AppendLine("✓ Đã khôi phục tìm kiếm Menu Start về mặc định.");
                    restored++;
                }
            }
        } catch {}

        try {
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo")) {
                if (k != null) {
                    k.SetValue("Enabled", 1, RegistryValueKind.DWord);
                    sb.AppendLine("✓ Đã khôi phục mã nhận dạng quảng cáo.");
                    restored++;
                }
            }
        } catch {}

        try {
            using (var k = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System")) {
                if (k != null) {
                    k.DeleteValue("EnableActivityFeed", false);
                    k.DeleteValue("PublishUserActivities", false);
                    k.DeleteValue("UploadUserActivities", false);
                    sb.AppendLine("✓ Đã khôi phục thiết lập Activity History.");
                    restored++;
                }
            }
        } catch {}

        report = sb.ToString();
        return restored > 0;
    }
}
