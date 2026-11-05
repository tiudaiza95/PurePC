# Nhật ký cập nhật phiên bản 0.9 · PurePC (PurePC Pro Max)

> **Phiên bản**: `0.9.0.0`  
> **Kiến trúc**: 64-bit Native (.NET 4.8 / WPF)  
> **Trạng thái kiểm thử**: PASS 96/96 Unit Checks  
> **Mã băm SHA-256**: `A0DD023B0CA9315EECF253DA9867E8638E23FFFF857BFAE7B1569DC10A6C9002`  
> **Lối tắt Desktop**: `%USERPROFILE%\Desktop\PurePC.lnk` trỏ trực tiếp đến `%LOCALAPPDATA%\Programs\PurePC\PurePC.exe`

---

## 1. Các cải tiến mới nhất theo yêu cầu người dùng

### 🌐 1. Bổ sung hỗ trợ Đa ngôn ngữ (Tiếng Anh & Tiếng Việt)
- Tích hợp động cơ dịch thuật `I18n` hỗ trợ chuyển đổi linh hoạt giữa **Tiếng Việt** và **English (US)**.
- Người dùng có thể dễ dàng chuyển đổi ngôn ngữ trong menu Cài đặt (`☰` > Cài đặt & An toàn > Giao diện & Ngôn ngữ). Khi chọn, toàn bộ Sidebar, tiêu đề, nút bấm, thẻ đo và thông số sẽ chuyển sang tiếng Anh tức thì và lưu cấu hình cho các lần khởi động tiếp theo.
- Khởi tạo an toàn (Fail-safe Initializer): Cơ chế nạp từ điển chống trùng lặp tuyệt đối, có bọc khối ngoại lệ an toàn, đảm bảo khởi động ứng dụng trơn tru không bao giờ bị lỗi type initializer.

### 📐 2. Tiêu đề căn giữa chuẩn mực & Tối giản thẻ Dọn dẹp
- **Căn giữa toàn bộ tiêu đề trang**: Tiêu đề chính (*"Tổng quan hệ thống & Giám sát hiệu năng"*) và phụ đề của tất cả các trang đều được căn giữa (`HorizontalAlignment="Center"`, `TextAlignment="Center"`), mang lại vẻ ngoài trang nhã, hiện đại.
- **Tối giản thẻ Dọn dẹp hệ thống**: Đã loại bỏ dòng chữ chú thích dài dòng *"Tích hợp dọn sạch tệp tạm (%TEMP%), Thùng rác, Registry và cache trình duyệt."* giúp thẻ dọn dẹp gọn gàng, thoáng mắt.

### 🎯 3. Thẳng hàng thanh công cụ Ứng dụng
- Đưa nút `[Phần dư đã ghi nhận]` vào khai báo XAML chuẩn cùng với các nút `[Gỡ mục đã chọn]`, `[⚡ Gỡ cưỡng chế]`, `[⚡ Gỡ hàng loạt]`, `[Quét phần dư]`, `[Dọn mục đã chọn]`.
- Đồng bộ hóa hoàn toàn khoảng đệm (`Padding="18,12"`), lề viền (`Margin="0,0,8,8"`) và chiều cao, khắc phục triệt để hiện tượng lệch dòng hoặc nhảy vị trí.

### 💽 4. Tinh gọn Công cụ ổ đĩa (Xóa bỏ các ghi chú ngoặc đơn)
- Loại bỏ toàn bộ các hậu tố kỹ thuật trong ngoặc `(...)` ở tiêu đề thẻ và các nút bấm:
  - `Mở Disk Cleanup` *(bỏ `(cleanmgr.exe)`)*
  - `Mở Tối ưu ổ đĩa` *(bỏ `(dfrgui.exe)`)*
  - `🔍 KIỂM TRA LỖI TẬP TIN` và `🔍 Quét kiểm tra ổ C:` *(bỏ `(CHKDSK)` và `(chkdsk)`)*
  - `Quản lý phân vùng` và `Cài đặt lưu trữ Windows` *(bỏ `(diskmgmt.msc)` và `(Storage Sense)`)*
  - `⚡ Tắt Ngủ đông` *(bỏ `(Giải phóng 8-16 GB)`)*
- Toàn bộ 6 tiêu đề thẻ công cụ đều được căn giữa thẳng hàng tuyệt đối.

---

## 2. Kết quả nghiệm thu kỹ thuật
- **Unit Tests**: `PASS: 96 checks` (Kiểm thử bộ nhớ, dọn rác, registry, driver, startup).
- **GUI Launch**: Đã kiểm thử khởi chạy thực tế cả 2 chế độ ngôn ngữ Tiếng Việt và Tiếng Anh đều hoàn hảo 100%.
