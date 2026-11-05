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
  Add("Công cụ Pro", "Pro Utilities");
  Add("Trung tâm Tiện ích Pro & Công cụ nâng cao", "Pro Utilities Hub & Advanced Tools");
  Add("Kho công cụ chuyên sâu: Tối ưu CSDL, Hủy tệp an toàn, Dọn Registry, Gỡ UWP và bảo trì Windows", "Advanced tools: Database optimization, file shredder, registry cleaner, UWP removal and Windows maintenance");
  Add("Tối ưu hóa Database SQLite", "SQLite Database Optimization");
  Add("Hút bụi chân không (Vacuum) và tái lập chỉ mục (Reindex) CSDL của Chrome, Edge, Firefox, VS Code... tăng tốc mở app.", "Vacuum and reindex databases for Chrome, Edge, Firefox, VS Code... to speed up app startup.");
  Add("Mở SQLite Optimizer", "Open SQLite Optimizer");
  Add("Hủy tệp an toàn (File Shredder)", "Secure File Shredder");
  Add("Ghi đè dữ liệu 3 lượt theo chuẩn DoD 5220.22-M và xóa dấu vết MFT, chống khôi phục hoàn toàn.", "3-pass DoD 5220.22-M overwrite and MFT record wiping for permanent irrecoverable deletion.");
  Add("Mở File Shredder", "Open File Shredder");
  Add("Dọn dẹp Registry thông minh", "Smart Registry Cleaner");
  Add("Quét và làm sạch các khóa Registry mồ côi (CLSID, DLL thiếu, MUI cache rác). Tự động sao lưu .reg.", "Scan and fix orphaned registry keys (CLSIDs, missing DLLs, stale MUI cache). Automatic .reg backups.");
  Add("Mở Dọn Registry", "Open Registry Cleaner");
  Add("Gỡ Bloatware Windows 10/11", "Uninstall Windows 10/11 Bloatware");
  Add("Gỡ bỏ sạch sẽ các ứng dụng Universal mặc định như Xbox, Cortana, Maps, Thời tiết... Bảo vệ Store.", "Remove pre-installed Universal Windows apps like Xbox, Cortana, Maps, Weather... Store is protected.");
  Add("Mở Gỡ ứng dụng UWP", "Open UWP App Manager");
  Add("Tiện ích mở rộng trình duyệt", "Browser Extensions");
  Add("Liệt kê, bật/tắt hoặc gỡ bỏ các Add-on/Extension mở rộng trên Chrome, Edge, Cốc Cốc, Brave, Firefox.", "List, toggle, or remove extensions and add-ons on Chrome, Edge, Cốc Cốc, Brave, Firefox.");
  Add("Quản lý tiện ích", "Manage Extensions");
  Add("Quản lý Dịch vụ Windows", "Windows Services Manager");
  Add("Kiểm soát các dịch vụ nền của phần mềm bên thứ 3 tự khởi chạy, bảo vệ tuyệt đối dịch vụ Windows cốt lõi.", "Manage background services of third-party software while protecting core Windows services.");
  Add("Giám sát cài đặt ứng dụng", "Installation Setup Monitor");
  Add("Chụp snapshot hệ thống trước và sau cài đặt để theo dõi 100% tệp và registry đã sinh ra, hỗ trợ gỡ sạch.", "Take system snapshots before and after install to track 100% of generated files and registry keys.");
  Add("Bật Giám sát cài đặt", "Start Setup Monitor");
  Add("Làm sạch DNS & Dọn Thùng rác", "Flush DNS & Empty Recycle Bin");
  Add("Xóa sạch bộ đệm phân giải tên miền (DNS Resolver Cache) và dọn sạch Thùng rác Recycle Bin chỉ với 1 thao tác.", "Flush DNS Resolver Cache and empty Windows Recycle Bin in one click.");
  Add("Làm sạch DNS", "Flush DNS");
  Add("Dọn Thùng rác", "Empty Recycle Bin");
  Add("Lịch quét tự động", "Scheduled Auto-Scan");
  Add("Zalo PC · Tệp tạm Cache", "Zalo PC · Cache & Temp");
  Add("DirectX · Bộ đệm Shader D3D", "DirectX · D3D Shader Cache");
  Add("NVIDIA · Bộ đệm Shader DX", "NVIDIA · DX Shader Cache");
  Add("AMD · Bộ đệm Shader DX", "AMD · DX Shader Cache");
  Add("Windows Update · Delivery Optimization", "Windows Update · Delivery Optimization Cache");
  Add("Ổ ĐĨA HỆ THỐNG & DỮ LIỆU", "SYSTEM & DATA STORAGE DRIVES");
  Add("☀ Sáng", "☀ Light");
  Add("◐ Tối", "◐ Dark");
  Add("◐ Giao diện", "◐ Theme");
  Add("Có thể gỡ", "Removable");
  Add("★ Bảo vệ cốt lõi", "★ Core Protected");
  Add("TRẠNG THÁI", "STATUS");
  Add("CHĂM SÓC MÁY TÍNH · v1.2", "PC MAINTENANCE · v1.2");
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
  Add("PurePC Pro Max · Phiên bản 1.0 Chính thức", "PurePC Pro Max · Official Version 1.0");
  Add("PurePC Pro Max · Phiên bản 1.1 Chính thức", "PurePC Pro Max · Official Version 1.1");
   Add("PurePC Pro Max · Phiên bản 1.2 Chính thức", "PurePC Pro Max · Official Version 1.2");
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
   Add("Cài đặt & Tiện ích chuyên sâu · PurePC 1.2", "Advanced Settings & Utilities · PurePC 1.2");
   Add("⚡ Hàng đợi Gỡ cài đặt hàng loạt (Batch Uninstall) · PurePC 1.2", "⚡ Batch Uninstall Queue · PurePC 1.2");
   Add("Giám sát & Theo dõi Cài đặt mới (Setup Monitor) · PurePC 1.2", "Setup Monitor & Complete Uninstall · PurePC 1.2");
   Add("Dọn dẹp Registry thông minh & An toàn · PurePC 1.2", "Smart & Safe Registry Cleaner · PurePC 1.2");
   Add("Quản lý Dịch vụ bên thứ ba (Third-party Services) · PurePC 1.2", "Third-Party Services Manager · PurePC 1.2");
   Add("Quản lý Tiện ích mở rộng Trình duyệt (Browser Extensions) · PurePC 1.2", "Browser Extensions Manager · PurePC 1.2");
   Add("⚡ Tối ưu Database SQLite", "⚡ Optimize SQLite Databases");
   Add("Tối ưu Database SQLite", "Optimize SQLite Databases");
   Add("Tối ưu hóa Database Trình duyệt (SQLite Vacuum) · PurePC 1.2", "Browser Database Optimization (SQLite Vacuum) · PurePC 1.2");
   Add("TỐI ƯU HÓA & THU GỌN DATABASE TRÌNH DUYỆT (SQLITE VACUUM)", "BROWSER DATABASE OPTIMIZATION & COMPACTION (SQLITE VACUUM)");
   Add("⚡ Bắt đầu Tối ưu hóa (Vacuum)", "⚡ Start Optimization (Vacuum)");
   Add("Sẵn sàng phân tích cơ sở dữ liệu trình duyệt.", "Ready to analyze browser databases.");
   Add("Đang tiến hành tối ưu hóa các cơ sở dữ liệu...", "Optimizing browser databases...");
   Add("Đã tìm thấy ", "Found ");
   Add(" tệp database SQLite trình duyệt.", " browser SQLite database files.");
   Add(" database, thu gọn được ", " databases, reclaimed ");
   Add("🔥 Hủy tệp an toàn (Shredder)", "🔥 Secure File Shredder");
   Add("Hủy tệp & Thư mục an toàn (File Shredder) · PurePC 1.2", "Secure File & Folder Shredder · PurePC 1.2");
   Add("HỦY TỆP AN TOÀN & XÓA VĨNH VIỄN (SECURE FILE SHREDDER)", "SECURE FILE SHREDDER & PERMANENT DELETION");
   Add("Ghi đè bit dữ liệu nhiều lượt (Zero, One, Random) trước khi xóa nhằm ngăn chặn hoàn toàn khả năng phục hồi dữ liệu bằng phần mềm cứu hộ.", "Overwrites bit data multiple passes (Zero, One, Random) before deletion to completely prevent data recovery by forensic tools.");
   Add("⚡ CẢNH BÁO: Dữ liệu sau khi hủy sẽ bị XÓA VĨNH VIỄN và KHÔNG THỂ KHÔI PHỤC kể cả bằng phần mềm cứu dữ liệu chuyên nghiệp. Hãy kiểm tra kỹ đường dẫn tệp trước khi thực hiện.", "⚡ WARNING: Data will be PERMANENTLY DESTROYED and CANNOT BE RECOVERED even by professional data recovery software. Please verify file paths carefully.");
   Add("📄 Thêm tệp...", "📄 Add Files...");
   Add("📁 Thêm thư mục...", "📁 Add Folder...");
   Add("❌ Xóa mục đã chọn", "❌ Remove Selected");
   Add("🧹 Xóa toàn bộ danh sách", "🧹 Clear List");
   Add("Nhanh (1 lượt - Ghi đè Zero)", "Fast (1 Pass - Zero Fill)");
   Add("Tiêu chuẩn DoD 5220.22-M (3 lượt - Zero, One, Random)", "DoD 5220.22-M Standard (3 Passes - Zero, One, Random)");
   Add("Danh sách trống. Thêm tệp hoặc thư mục cần hủy vào bảng.", "List is empty. Add files or folders to shred.");
   Add("Đã chọn ", "Selected ");
   Add(" mục cần hủy.", " items to shred.");
   Add("Chọn tệp cần hủy vĩnh viễn", "Select files to shred permanently");
   Add("Chọn thư mục cần hủy vĩnh viễn", "Select folder to shred permanently");
   Add("🔥 TIẾN HÀNH HỦY VĨNH VIỄN", "🔥 PROCEED SECURE SHREDDING");
   Add("Vui lòng thêm ít nhất một tệp hoặc thư mục cần hủy.", "Please add at least one file or folder to shred.");
   Add("Xác nhận hủy dữ liệu", "Confirm Data Shredding");
   Add("Đang tiến hành hủy an toàn dữ liệu...", "Securely shredding data...");
   Add("Hoàn tất hủy tệp", "File Shredding Complete");
   Add("Đóng", "Close");


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

  // Elite 2.0 Translations
  Add("Rác Lập trình & Mô hình AI", "Developer & AI Model Caches");
  Add("Dọn sạch bộ đệm của NPM, Yarn, Pip, PyTorch, Hugging Face, Gradle, Cargo... tiết kiệm hàng chục GB ổ C:.", "Purge caches for NPM, Yarn, Pip, PyTorch, Hugging Face, Gradle, Cargo... reclaiming tens of GBs on C:.");
  Add("Dọn rác Dev & AI", "Clean Dev & AI Caches");
  Add("Tăng tốc Game & Giảm Ping", "Game Booster & Low Latency");
  Add("Tối ưu hóa độ trễ TCP/IP, tắt bóp băng thông mạng (Throttling) và mở khóa gói Ultimate Performance.", "Optimize TCP/IP latency, disable network throttling, and unlock Ultimate Performance power scheme.");
  Add("Mở Bộ Tăng tốc Game", "Open Game Booster");
  Add("Tối ưu kho WinSxS (DISM)", "WinSxS Component Store (DISM)");
  Add("Phân tích và thu hồi các gói cập nhật Windows Update cũ qua công cụ Microsoft DISM chính thức.", "Analyze and reclaim superseded Windows Update packages via official Microsoft DISM.");
  Add("Mở Tối ưu WinSxS", "Open WinSxS Optimizer");
  Add("Bộ Tối ưu RAM Chuyên sâu", "Intelligent RAM Optimizer");
  Add("Thu dọn Working Set của các tiến trình bằng API Win32, giải phóng bộ nhớ RAM mà không tắt ứng dụng.", "Trim process working sets via Win32 APIs, recovering memory without closing apps.");
  Add("Mở Tối ưu RAM", "Open RAM Optimizer");
  Add("BỘ TỐI ƯU RAM CHUYÊN SÂU & QUẢN LÝ BỘ NHỚ TIẾN TRÌNH", "INTELLIGENT RAM OPTIMIZER & PROCESS MEMORY MANAGER");
  Add("TRÌNH DỌN RÁC LẬP TRÌNH & MÔ HÌNH AI (DEV & AI CLEANER)", "DEVELOPER & AI MODEL CACHE CLEANER");
  Add("PHÂN TÍCH & TỐI ƯU KHO THÀNH PHẦN WINSXS (DISM)", "WINSXS COMPONENT STORE ANALYZER & CLEANER (DISM)");
  Add("TĂNG TỐC GAME & TỐI ƯU HÓA ĐỘ TRỄ MẠNG (LOW-LATENCY BOOSTER)", "GAME BOOSTER & NETWORK LATENCY OPTIMIZER");
  Add("⚡ Thu dọn RAM toàn bộ tiến trình (Deep Compact)", "⚡ Deep Compact RAM for All Processes");
  Add("🧹 Dọn các tiến trình đã chọn", "🧹 Trim Selected Processes");
  Add("🌐 Lưu lượng mạng thời gian thực: ↓ 0 KB/s · ↑ 0 KB/s", "🌐 Real-time Network Traffic: ↓ 0 KB/s · ↑ 0 KB/s");

  // Elite 2.5 Privacy & System Repair Translations
  Add("Bảo vệ Riêng tư & Tắt Telemetry", "Privacy & Telemetry Shield");
  Add("Tắt thu thập dữ liệu chẩn đoán (DiagTrack), chặn quảng cáo và tìm kiếm Bing trong Start Menu.", "Disable diagnostic telemetry (DiagTrack), block advertising ID and Start Menu Bing search.");
  Add("Mở Privacy Shield", "Open Privacy Shield");
  Add("KHIÊN BẢO VỆ QUYỀN RIÊNG TƯ & TẮT THEO DÕI (PRIVACY SHIELD)", "PRIVACY & TELEMETRY SHIELD");
  Add("🛡️ Kích hoạt Bảo vệ Riêng tư (Harden Privacy)", "🛡️ Harden Privacy Protection");
  Add("🧹 Dọn lịch sử Explorer & Run MRU", "🧹 Clear Explorer & Run MRU History");

  Add("Sửa lỗi tệp hệ thống & DISM", "System File Checker & DISM Repair");
  Add("Quét SFC Scannow, phục hồi kho ảnh Windows DISM RestoreHealth và đặt lại ngăn xếp mạng TCP/IP.", "Run SFC Scannow, restore Windows image via DISM RestoreHealth and reset TCP/IP network stack.");
  Add("Mở Sửa lỗi Windows", "Open Windows Repair");
  Add("SỬA LỖI TỆP HỆ THỐNG & PHỤC HỒI WINDOWS (SFC & DISM)", "SYSTEM FILE CHECKER & WINDOWS REPAIR (SFC & DISM)");
  Add("🛠️ Quét & Sửa tệp hệ thống (SFC Scannow)", "🛠️ Scan & Repair System Files (SFC Scannow)");
  Add("⚡ Phục hồi kho ảnh Windows (DISM RestoreHealth)", "⚡ Restore Windows Image (DISM RestoreHealth)");
  Add("🌐 Đặt lại mạng TCP/IP & Winsock", "🌐 Reset TCP/IP & Winsock Stack");

  Add("Thời gian hoạt động", "System Uptime");
  Add("Hoạt động", "Uptime");
  Add("Pin", "Battery");
  Add("Lưu lượng mạng", "Network Traffic");

  Add("💻 Rác Lập trình & AI", "💻 Dev & AI Caches");
  Add("Dọn rác Dev & AI đã chọn", "Clean Selected Dev & AI Caches");
  Add("Tích hợp 5 trong 1: Tàn dư gỡ cài đặt · Tệp dung lượng lớn (>100MB) · Tệp trùng lặp · Lối tắt hỏng · Rác Lập trình & AI", "5-in-1: Leftover remnants · Large files (>100MB) · Duplicate content · Broken shortcuts · Dev & AI Caches");

  // Elite 3.0 Context Menu, SSD TRIM, Folder Space, Windows Update Cache
  Add("Menu Chuột phải (Context Menu)", "Shell Context Menu Handlers");
  Add("Tắt hoặc bật lại các menu mở rộng bên thứ 3 trong menu chuột phải File Explorer, chống giật đơ.", "Enable or disable third-party context menu handlers in File Explorer to eliminate right-click lag.");
  Add("Quản lý Menu chuột phải", "Manage Context Menu");
  Add("QUẢN LÝ MENU CHUỘT PHẢI (SHELL CONTEXT MENU HANDLERS)", "SHELL CONTEXT MENU HANDLERS MANAGER");

  Add("Tối ưu hóa & Kích hoạt TRIM SSD", "SSD TRIM Optimizer");
  Add("Kiểm tra trạng thái TRIM và phát tín hiệu Re-TRIM cho chip điều khiển SSD duy trì tốc độ và tuổi thọ.", "Check TRIM status and issue Re-TRIM commands to SSD controller to maintain speed and cell lifespan.");
  Add("Tối ưu TRIM SSD", "Optimize SSD TRIM");
  Add("TỐI ƯU HÓA TUỔI THỌ & KÍCH HOẠT TRIM SSD (SOLID STATE DRIVE)", "SSD LIFESPAN OPTIMIZER & FORCE TRIM (SOLID STATE DRIVE)");

  Add("Phân tích Dung lượng Thư mục", "Disk Space Folder Visualizer");
  Add("Đo lường và xếp hạng trực quan các thư mục chiếm nhiều dung lượng nhất trên ổ đĩa C:\\ hoặc D:\\.", "Measure and rank largest space-consuming folders on drives C:\\ or D:\\.");
  Add("Phân tích Dung lượng", "Analyze Folder Space");
  Add("PHÂN TÍCH TRỰC QUAN DUNG LƯỢNG THƯ MỤC (DISK SPACE ANALYZER)", "DISK SPACE FOLDER VISUALIZER & ANALYZER");

  Add("Dọn Windows Update Cache", "Purge Windows Update Cache");
  Add("Dọn sạch các bản cập nhật tải về bị lỗi trong SoftwareDistribution\\Download và khởi động lại dịch vụ.", "Clean corrupted or cached update files in SoftwareDistribution\\Download and restart update services.");
  Add("Dọn Update Cache", "Clean Update Cache");
  Add("DỌN DẸP & KHẮC PHỤC LỖI WINDOWS UPDATE CACHE", "WINDOWS UPDATE CACHE CLEANER & REPAIR");
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
