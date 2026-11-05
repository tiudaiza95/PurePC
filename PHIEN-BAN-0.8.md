# Nhật ký cập nhật phiên bản 0.8 · PurePC (PurePC Pro)

> **Phiên bản**: `0.8.0.0`  
> **Kiến trúc**: 64-bit Native (.NET 4.8 / WPF)  
> **Trạng thái kiểm thử**: PASS 96/96 Unit Checks  
> **Mã băm SHA-256**: `4BABE71C808550760A0E3C9FAE402EF95737566E5A3A9123145B33CBFF7A7E74`  
> **Lối tắt Desktop**: `%USERPROFILE%\Desktop\PurePC.lnk` trỏ trực tiếp đến `%LOCALAPPDATA%\Programs\PurePC\PurePC.exe`

---

## 1. Mục tiêu nâng cấp phiên bản 0.8
Hiện thực hóa trọn vẹn tầm nhìn kết hợp sức mạnh tối ưu hóa giữa **CCleaner** (dọn rác hệ thống, tối ưu bộ nhớ), **IObit Uninstaller** (gỡ cài đặt chuyên sâu, gỡ cưỡng chế, dọn sạch phần dư) và **Driver & Device Management** (quản lý phần cứng, PnPUtil, nguồn chính hãng), nâng tầm trải nghiệm người dùng với các tính năng thông minh và chủ động:

---

## 2. Các tính năng & nâng cấp mới nổi bật trong v0.8

### ⚡ 1. Gỡ cài đặt cưỡng chế (Force Uninstall) · Phong cách IObit Uninstaller
- **Vấn đề giải quyết**: Khi các phần mềm bên thứ ba bị hỏng file gỡ cài đặt (`uninstall.exe`), bị crash giữa chừng hoặc không đăng ký lệnh gỡ trong Windows, người dùng không thể gỡ theo cách thông thường.
- **Cơ chế hoạt động**:
  1. **An toàn tuyệt đối**: Tự động từ chối nếu ứng dụng là thành phần cốt lõi của Windows (`Protected`).
  2. **Tự động sao lưu Registry**: Tạo bản sao lưu khóa Registry của phần mềm ra thư mục tạm (`%TEMP%\PurePC_Backup\*.reg`) trước khi thực hiện.
  3. **Xóa đăng ký hệ thống**: Xóa triệt để khóa cài đặt khỏi Registry Uninstall (`HKCU` hoặc `HKLM`).
  4. **Chuyển thư mục cài đặt vào Thùng rác**: Toàn bộ thư mục cài đặt thực tế (`InstallLocation`) được chuyển an toàn vào Recycle Bin thay vì xóa vĩnh viễn, tránh rủi ro mất dữ liệu.
  5. **Quét sâu tàn dư (Deep Remnants Clean)**: Tự động chạy thuật toán quét rác phần dư trong AppData, LocalAppData, ProgramData và Shortcuts Desktop/StartMenu, hiển thị hộp thoại xác nhận dọn sạch 1-click.
- **Vị trí truy cập**:
  - Nút **⚡ Gỡ cưỡng chế** trên thanh công cụ trang Ứng dụng.
  - Chuột phải vào ứng dụng trong bảng chọn **⚡ Gỡ cưỡng chế**.
  - Cửa sổ Thông tin chi tiết phần mềm (**⚡ Gỡ cưỡng chế**).

---

### ⚙️ 2. Quản lý Dịch vụ bên thứ ba (Third-party Services Manager)
- **Vấn đề giải quyết**: Nhiều phần mềm (Google Chrome, Adobe, Steam, NVIDIA, AMD, AnyDesk, VMware...) tự động đăng ký các Windows Services chạy nền liên tục ngay khi bật máy, làm chậm tốc độ khởi động và tiêu tốn dung lượng RAM mà Task Manager tab Startup không hiển thị.
- **Cơ chế hoạt động**:
  - Quét nhanh trực tiếp từ Registry `HKLM\SYSTEM\CurrentControlSet\Services`.
  - Phân tích thông minh: Tự động lọc bỏ các dịch vụ nội bộ hệ thống của Microsoft/Windows (`svchost.exe`, `System32`), chỉ hiển thị các dịch vụ do phần mềm bên thứ ba cài đặt.
  - Tự động nhận diện nhà phát triển: Google, Adobe, Valve/Steam, NVIDIA, AMD, Realtek, AnyDesk, TeamViewer, Oracle/VirtualBox, Riot Games, v.v.
- **Thao tác 1-chạm**:
  - **⚡ Chuyển sang Thủ công (Manual)**: Khuyên dùng — khi khởi động máy dịch vụ sẽ không chạy ngầm tiết kiệm RAM, khi người dùng mở ứng dụng dịch vụ mới tự động kích hoạt.
  - **⛔ Vô hiệu hóa (Disabled)**: Tắt hẳn dịch vụ không cần thiết.
  - **▶️ Bật Tự động (Auto)**: Khôi phục lại trạng thái ban đầu.
  - **📁 Mở thư mục**: Mở ngay file thực thi của dịch vụ trong Windows Explorer.
- **Vị trí truy cập**:
  - Nút **⚙️ Dịch vụ bên thứ ba** trong trang **Khởi động**.
  - Nút **⚙️ Quản lý Dịch vụ bên thứ ba** trong Menu Cài đặt 3 gạch (`☰`).

---

### ⚡ 3. Chăm sóc 1-chạm (1-Click Smart Care & Memory Optimization)
- **Nâng cấp Dashboard Tổng quan**:
  - Tích hợp Windows Native API `EmptyWorkingSet` từ `psapi.dll` và garbage collection giải phóng RAM đệm vật lý ngay lập tức mà không gây crash ứng dụng.
  - Tự động chạy quét rác toàn diện (Cache trình duyệt Chrome, Edge, Cốc Cốc, Brave, Firefox, Opera, Temp hệ thống, Thùng rác).
  - Tự động đánh giá **Điểm sức khỏe hệ thống (System Health Score 0–100)**: Phân tích mức tải RAM, lượng rác tích tụ và số tiến trình khởi động.
  - Tự động đánh dấu tất cả các mục rác an toàn (`Recommended`), cho phép người dùng hoàn tất dọn dẹp và tối ưu máy tính chỉ với 1 cú click chuột.

---

### 🎨 4. Nhận diện phiên bản v0.8 & Giao diện tối ưu
- Cập nhật số phiên bản đồng bộ `0.8.0.0` trên toàn hệ thống:
  - Cửa sổ chính: `Title="PurePC 0.8"`
  - Nhãn thương hiệu: `PurePC 0.8` · `CHĂM SÓC MÁY TÍNH · v0.8`
  - Chân trang: `Phiên bản 0.8 · Desktop Pro`
  - Menu Cài đặt: `Cài đặt & Tiện ích chuyên sâu · PurePC 0.8`
  - File thông tin: `AssemblyInfo.cs` (`0.8.0.0`), `app.manifest` (`0.8.0.0`), `installation-check.json` (`0.8.0.0`).

---

## 3. Bảng kiểm tra an toàn & Kiểm thử tự động
| Thành phần kiểm tra | Kết quả v0.8 | Ghi chú |
| :--- | :---: | :--- |
| **Unit Checks (Self-Test)** | **PASS 96/96** | Đảm bảo tính toàn vẹn bộ nhớ, ranh giới file, logic hash SHA-256 |
| **Quy tắc an toàn tệp** | **ĐẠT** | Ưu tiên gửi vào Recycle Bin, sao lưu Registry `.reg` trước khi xóa |
| **Bảo vệ Windows & System** | **ĐẠT** | Tuyệt đối không can thiệp vào các thư mục và tiến trình bảo vệ của Windows |
| **Tốc độ phản hồi UI** | **~0ms** | Mọi tác vụ nặng (quét rác, đo RAM, quản lý service) đều chạy trên background thread |
