using System;
using System.Collections.Generic;
using System.IO;

static class I18n {
 public static string Lang = "vi";
 static Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

 static I18n() {
  try {
   AddTranslations();
  } catch {}
 }

 static void Add(string k, string v) {
  En[k] = v;
 }

 static void AddTranslations() {
  Add("Tổng quan", "Dashboard");
  Add("Ứng dụng", "Applications");
  Add("Quét chuyên sâu", "Deep Clean");
  Add("Khởi động", "Startup");
  Add("Công cụ ổ đĩa", "Disk Tools");
  Add("Driver & thiết bị", "Drivers & Devices");
  Add("Nhật ký", "History");

  Add("Tổng quan hệ thống & Giám sát hiệu năng", "System Overview & Performance Monitor");
  Add("Cấu hình máy tính, giám sát tài nguyên thời gian thực và dọn dẹp hệ thống", "Hardware specs, real-time resource monitor and system cleanup");
  Add("Gỡ cài đặt & Quản lý ứng dụng", "Uninstall & Manage Applications");
  Add("Quản lý ứng dụng, gỡ cài đặt và kiểm tra phần còn lại.", "Manage applications, uninstall and verify leftover remnants.");
  Add("Quét chuyên sâu toàn diện hệ thống", "Comprehensive Deep System Clean");
  Add("Tích hợp 3 trong 1: Rác & Tàn dư gỡ cài đặt · Tệp dung lượng lớn (>100MB) · Tệp trùng lặp nội dung (SHA-256)", "3-in-1: Leftover remnants · Large files (>100MB) · Duplicate content (SHA-256)");
  Add("Quản lý mục khởi động cùng Windows", "Windows Startup Applications Manager");
  Add("Bật/tắt các ứng dụng chạy ngầm khi khởi động máy để tăng tốc độ khởi động", "Enable/disable background startup items to speed up boot time");
  Add("Chăm sóc & Quản lý ổ đĩa toàn diện", "Disk Care & Partition Management");
  Add("6 bộ công cụ chuyên sâu tối ưu hóa lưu trữ, kiểm tra lỗi và quản lý phân vùng Windows", "6 advanced tools for storage optimization, error checking and partition management");
  Add("Lịch sử gỡ cài đặt & Nhật ký", "Uninstall History & System Activity Logs");
  Add("Driver từ đúng nhà sản xuất.", "Manufacturer Drivers & Devices");
  Add("Kiểm kê tại máy — nguồn hãng — cài gói cục bộ có xác minh", "Local inventory — vendor sources — verified local INF installation");

  Add("CẤU HÌNH HỆ THỐNG", "SYSTEM SPECIFICATIONS");
  Add("DỌN DẸP HỆ THỐNG & TỆP TẠM", "SYSTEM & TEMP FILE CLEANER");
  Add("Hệ điều hành", "Operating System");
  Add("Bộ vi xử lý (CPU)", "Processor (CPU)");
  Add("Card đồ họa (GPU)", "Graphics (GPU)");
  Add("Bo mạch chủ", "Motherboard");
  Add("Dọn sạch bộ nhớ đệm Google Chrome (Bỏ chọn nếu muốn giữ cache)", "Clean Google Chrome cache (Uncheck to keep cache)");
  Add("Chưa quét. Sẵn sàng quét và tối ưu hệ thống 1-click.", "Ready for 1-click scan and system optimization.");
  Add("MỨC DÙNG CPU", "CPU USAGE");
  Add("BỘ NHỚ RAM", "RAM MEMORY");
  Add("Ổ ĐĨA C: (HỆ THỐNG)", "DRIVE C: (SYSTEM)");
  Add("Đang theo dõi", "Monitoring");
  Add("Xung nhịp & tiến trình Windows", "Clock speed & Windows processes");
  Add("Ổ đĩa hệ thống", "System drive");
  Add("trống", "free");
  Add("Tổng", "Total");
  Add("đã dùng", "used");
  Add("Chế độ bảo vệ dữ liệu tích cực: Toàn bộ tác vụ quét dọn hệ thống đều ưu tiên chuyển vào Thùng rác để khôi phục và sao lưu Registry an toàn trước khi xử lý.", "Active Data Protection: All cleanup tasks prioritize Recycle Bin for recovery and backup Registry before processing.");

  Add("⚡ Quét & Tối ưu (1-Click)", "⚡ Scan & Optimize (1-Click)");
  Add("🧹 Dọn mục đã chọn", "🧹 Clean Selected Items");
  Add("🧹 Dọn dẹp Registry", "🧹 Registry Cleaner");
  Add("⚡ Tối ưu RAM", "⚡ Optimize RAM");
  Add("Gỡ mục đã chọn", "Uninstall Selected");
  Add("⚡ Gỡ cưỡng chế", "⚡ Force Uninstall");
  Add("⚡ Gỡ hàng loạt", "⚡ Batch Uninstall");
  Add("Quét phần dư", "Scan Remnants");
  Add("Dọn mục đã chọn", "Clean Selected");
  Add("Phần dư đã ghi nhận", "Recorded Remnants");
  Add("Làm mới", "Refresh");
  Add("Thông tin", "Details");
  Add("Gỡ cài đặt", "Uninstall");
  Add("Bật / Tắt mục khởi động", "Toggle Startup Item");
  Add("Xóa mục khởi động", "Delete Startup Item");

  Add("💾 DỌN DUNG LƯỢNG HỆ THỐNG", "💾 SYSTEM DISK CLEANUP");
  Add("Mở Disk Cleanup", "Open Disk Cleanup");
  Add("⚡ TỐI ƯU HÓA HDD VÀ SSD", "⚡ HDD & SSD OPTIMIZATION");
  Add("Mở Tối ưu ổ đĩa", "Open Drive Optimizer");
  Add("🔍 KIỂM TRA LỖI TẬP TIN", "🔍 FILE SYSTEM ERROR CHECK");
  Add("🔍 Quét kiểm tra ổ C:", "🔍 Scan Drive C:");
  Add("Kiểm tra phân vùng Windows", "Windows Partition Check");
  Add("▤ QUẢN LÝ PHÂN VÙNG & LƯU TRỮ", "▤ PARTITION & STORAGE MANAGER");
  Add("Quản lý phân vùng", "Manage Partitions");
  Add("Cài đặt lưu trữ Windows", "Windows Storage Settings");
  Add("▶ PHÂN TÍCH CHIẾM DỤNG Ổ ĐĨA", "▶ DISK SPACE ANALYZER");
  Add("▶ Phân tích thư mục ổ C:", "▶ Analyze Drive C: Folders");
  Add("Xem tệp tạm thời Windows", "View Temporary Files");
  Add("⚡ TỐI ƯU HIBERNATE & COMPACT OS", "⚡ HIBERNATE & COMPACT OS");
  Add("⚡ Tắt Ngủ đông", "⚡ Turn Off Hibernation");
  Add("⚡ Kiểm tra nén Compact OS", "⚡ Check Compact OS State");

  Add("🛡️ Tàn dư gỡ cài đặt", "🛡️ Leftover Remnants");
  Add("📦 Tệp dung lượng lớn", "📦 Large Files");
  Add("📑 Tệp trùng lặp", "📑 Duplicate Files");
  Add("⚡ Quét toàn bộ chuyên sâu (Cả 3 mục)", "⚡ Deep Scan All (3-in-1)");
  Add("Dọn tàn dư đã chọn", "Clean Selected Remnants");
  Add("Dọn tệp lớn đã chọn", "Delete Selected Large Files");
  Add("Tự chọn bản trùng dư thừa", "Auto-select Duplicates");
  Add("Chưa có dữ liệu cho mục này. Hãy bấm '⚡ Quét toàn bộ chuyên sâu (Cả 3 mục)' ở trên để bắt đầu phân tích!", "No data for this category yet. Click '⚡ Deep Scan All (3-in-1)' above to begin scanning!");

  Add("● Đang kích hoạt", "● Active");
  Add("○ Đã tắt", "○ Disabled");

  Add("CÀI ĐẶT & TIỆN ÍCH CHUYÊN SÂU", "ADVANCED SETTINGS & UTILITIES");
  Add("Tùy biến giao diện, ngôn ngữ, an toàn dữ liệu và trung tâm lối tắt hệ thống", "Customize appearance, language, data safety and system shortcuts");
  Add("Giao diện & Ngôn ngữ", "Appearance & Language");
  Add("⚙️ Cài đặt & An toàn", "⚙️ Settings & Safety");
  Add("🛠️ Lối tắt Tiện ích", "🛠️ Utilities & Shortcuts");
  Add("ℹ️ Giới thiệu", "ℹ️ About");
  Add("Chính sách an toàn dữ liệu 100%", "100% Data Safety Policy");
  Add("Trung tâm công cụ nâng cao", "Advanced Tools Center");
  Add("Truy cập nhanh các tính năng dọn dẹp và bảo trì hệ thống 1-chạm:", "Quick 1-click access to system maintenance and cleanup tools:");
  Add("🧩 Tiện ích trình duyệt", "🧩 Browser Extensions");
  Add("⚙️ Dịch vụ bên thứ ba", "⚙️ Third-Party Services");
  Add("📁 Bản sao lưu Registry", "📁 Registry Backups");
  Add("📁 Bản sao lưu Driver", "📁 Driver Backups");
  Add("🗑️ Mở Thùng rác Windows", "🗑️ Open Windows Recycle Bin");
  Add("📝 Mở tệp Nhật ký", "📝 Open Activity Log");
  Add("💾 Công cụ Disk Cleanup", "💾 Disk Cleanup Tool");
  Add("⚡ Tối ưu ổ đĩa Defrag", "⚡ Drive Defrag & Trim Tool");
  Add("☀ Chuyển sang Chế độ sáng", "☀ Switch to Light Mode");
  Add("◐ Chuyển sang Chế độ tối", "◐ Switch to Dark Mode");
  Add("Mở rộng thanh bên", "Expand Sidebar");
  Add("Thu gọn thanh bên", "Collapse Sidebar");

  // Drivers Page
  Add("Mở Device Manager", "Open Device Manager");
  Add("1. Quét thiết bị", "1. Scan Devices");
  Add("2. Mở nguồn hãng của thiết bị", "2. Open Manufacturer Source");
  Add("Cài gói INF đã tải từ hãng", "Install Verified Local INF Driver");
  Add("Sao chép mã phần cứng", "Copy Hardware IDs");
  Add("Mở quyền quản trị", "Run as Administrator");
  Add("Mở bản sao driver", "Open Driver Backups");
  Add("Cài gói driver đã tải / tùy chọn khác", "Install Local Driver / Advanced Options");
  Add("Card màn hình, âm thanh, mạng và thiết bị", "Display, Audio, Network and Peripheral Devices");
  Add("THIẾT BỊ", "DEVICE");
  Add("TRẠNG THÁI", "STATUS");
  Add("PHIÊN BẢN", "VERSION");
  Add("NGUỒN CHÍNH THỨC", "OFFICIAL SOURCE");
  Add("NHÓM", "CLASS");

  // Version 1.0 & 1.1 & About
  Add("Dọn Dẹp PC Pro Max · Phiên bản 1.0 Chính thức", "DonDepPC Pro Max · Official Version 1.0");
  Add("Dọn Dẹp PC Pro Max · Phiên bản 1.1 Chính thức", "DonDepPC Pro Max · Official Version 1.1");
   Add("Dọn Dẹp PC Pro Max · Phiên bản 1.2 Chính thức", "DonDepPC Pro Max · Official Version 1.2");
   Add("TÁC ĐỘNG", "IMPACT");
   Add("Tác động", "Impact");
   Add("Cao", "High");
   Add("Trung bình", "Medium");
   Add("Thấp", "Low");
   Add("⚡ Làm sạch DNS Cache", "⚡ Flush DNS Cache");
   Add("Làm sạch DNS Cache", "Flush DNS Cache");
   Add("Đã làm sạch DNS Resolver Cache thành công!", "DNS Resolver Cache flushed successfully!");
   Add("🗑️ Dọn sạch Thùng rác", "🗑️ Empty Recycle Bin");
   Add("Dọn Thùng rác", "Empty Recycle Bin");
   Add("Bạn có chắc chắn muốn dọn sạch toàn bộ Thùng rác (Recycle Bin)?", "Are you sure you want to empty the Recycle Bin completely?");
   Add("Đã làm sạch Thùng rác thành công!", "Recycle Bin emptied successfully!");
   Add("Cá nhân", "Current User");
   Add("Toàn máy", "Machine-wide");
   Add("Không có nhà phát hành", "No Publisher");
   Add("Registry · chỉ xem", "Registry · view only");
   Add("Thư mục · có thể chứa dữ liệu", "Folder · may contain data");
   Add("Tiếng Việt (Mặc định)", "Vietnamese (Default)");
   Add("Cài đặt & Tiện ích chuyên sâu · Dọn Dẹp PC 1.2", "Advanced Settings & Utilities · DonDepPC 1.2");
   Add("⚡ Hàng đợi Gỡ cài đặt hàng loạt (Batch Uninstall) · Dọn Dẹp PC 1.2", "⚡ Batch Uninstall Queue · DonDepPC 1.2");
   Add("Giám sát & Theo dõi Cài đặt mới (Setup Monitor) · Dọn Dẹp PC 1.2", "Setup Monitor & Complete Uninstall · DonDepPC 1.2");
   Add("Dọn dẹp Registry thông minh & An toàn · Dọn Dẹp PC 1.2", "Smart & Safe Registry Cleaner · DonDepPC 1.2");
   Add("Quản lý Dịch vụ bên thứ ba (Third-party Services) · Dọn Dẹp PC 1.2", "Third-Party Services Manager · DonDepPC 1.2");
   Add("Quản lý Tiện ích mở rộng Trình duyệt (Browser Extensions) · Dọn Dẹp PC 1.2", "Browser Extensions Manager · DonDepPC 1.2");

  Add("Tích hợp 4 trong 1: Tàn dư gỡ cài đặt · Tệp dung lượng lớn (>100MB) · Tệp trùng lặp · Lối tắt hỏng", "4-in-1: Leftovers · Large files (>100MB) · Duplicates · Broken shortcuts");
  Add("🔗 Lối tắt hỏng", "🔗 Broken Shortcuts");
  Add("Dọn lối tắt đã chọn", "Clean Selected Shortcuts");
  Add("TÊN LỐI TẮT", "SHORTCUT NAME");
  Add("VỊ TRÍ", "LOCATION");
  Add("ĐƯỜNG DẪN HỎNG", "BROKEN TARGET");
  Add("LÝ DO", "REASON");
  Add("Ứng dụng Windows Store (UWP)", "Windows Store Apps (UWP)");
  Add("Ứng dụng máy tính (Win32)", "Desktop Apps (Win32)");
  Add("📸 Giám sát cài đặt", "📸 Setup Monitor");
  Add("GIÁM SÁT CÀI ĐẶT MỚI & GỠ BỎ TRIỆT ĐỂ (SETUP MONITOR)", "NEW INSTALLATION MONITOR & COMPLETE UNINSTALL (SETUP MONITOR)");
  Add("Giám sát cài đặt mới", "Monitor New Installation");
  Add("Phần mềm đã giám sát", "Monitored Software");
  Add("TÊN PHẦN MỀM", "SOFTWARE NAME");
  Add("NGÀY GIÁM SÁT", "DATE MONITORED");
  Add("SỐ TỆP MỚI", "NEW FILES");
  Add("SỐ KHÓA REGISTRY", "REGISTRY KEYS");
  Add("📄 Xuất báo cáo Driver", "📄 Export Driver Report");
  Add("Phông chữ mồ côi", "Orphaned Fonts");
  Add("Kiểm tra bản cập nhật trên GitHub", "Check for Updates on GitHub");
  Add("Mở Kho mã nguồn GitHub", "Open GitHub Repository");
  Add("Sao chép thông tin phiên bản", "Copy Version Information");
 }

 public static void Init() {
  try {
   if (En.Count == 0) AddTranslations();
   string path = Path.Combine(Engine.Data, "language");
   if (File.Exists(path) && File.ReadAllText(path).Trim().Equals("en", StringComparison.OrdinalIgnoreCase)) {
    Lang = "en";
   } else {
    Lang = "vi";
   }
  } catch { Lang = "vi"; }
 }

 public static void Save(string lang) {
  Lang = lang;
  try {
   Directory.CreateDirectory(Engine.Data);
   File.WriteAllText(Path.Combine(Engine.Data, "language"), lang);
  } catch {}
 }

 public static string T(string text) {
  try {
   if (Lang != "en" || string.IsNullOrEmpty(text)) return text;
   string key = text.Trim();
   string val;
   if (En.TryGetValue(key, out val)) return val;
   return text;
  } catch { return text; }
 }
}
