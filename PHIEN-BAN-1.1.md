# Nhật ký phát hành Phiên bản 1.1 · PurePC (PurePC Pro Max)

> **Phiên bản**: `1.1.0.0` (Bản nâng cấp tính năng thông minh - Smart Features Upgrade)  
> **Kiến trúc**: 64-bit Native (.NET 4.8 / WPF Desktop)  
> **Trạng thái kiểm thử**: PASS 103/103 Unit Checks  
> **Mã băm SHA-256**: `571E8574EB92A144B0C4FA55E2DADB814ACB73BF3B2CA9414781886032D2C15B`  
> **Lối tắt Desktop**: `%USERPROFILE%\Desktop\PurePC.lnk` trỏ trực tiếp đến `%LOCALAPPDATA%\Programs\PurePC\PurePC.exe`  
> **Kho mã nguồn GitHub**: [https://github.com/tiudaiza95/PurePC](https://github.com/tiudaiza95/PurePC)

---

## 🌟 Tổng quan Cột mốc Phiên bản 1.1

Phiên bản **1.1.0.0** nâng tầm **PurePC** lên một đẳng cấp mới với các tính năng chuyên nghiệp sánh ngang các phần mềm thương mại hàng đầu như *IObit Uninstaller Pro*, *Revo Uninstaller Pro* và *CCleaner Professional*:

```
+-----------------------------------------------------------------------------------------------+
|                                  PurePC PRO MAX v1.1                                     |
|                                                                                               |
|   [WINDOWS STORE / UWP]         [SETUP MONITOR]                 [REALTIME WAVE & SHORTCUTS]   |
|   • Quản lý ứng dụng UWP        • Chụp ảnh trạng thái trước cài • Biểu đồ sóng CPU/RAM mượt   |
|   • Gỡ bloatware Windows Store  • Theo dõi tệp & Registry mới   • Quét & dọn lối tắt .lnk hỏng|
|   • Hiển thị icon Store chuẩn   • Gỡ sạch 100% không sót tàn dư • Quét phông chữ mồ côi       |
|   • Bộ lọc UWP / Win32 chuyên   • Quản lý danh sách snapshot    • Xuất báo cáo Driver ra TXT  |
+-----------------------------------------------------------------------------------------------+
|   • Đa ngôn ngữ: Tiếng Việt (Mặc định) & English (US)                                        |
|   • Đạt chuẩn tuyệt đối 103/103 Unit Checks tự động                                           |
|   • An toàn dữ liệu 100%: Toàn bộ tác vụ xóa đều ưu tiên Thùng rác & tự động sao lưu Registry |
+-----------------------------------------------------------------------------------------------+
```

---

## 🚀 Các Tính năng Mới & Đột phá trong Phiên bản 1.1

### 1. Quản lý & Gỡ bỏ Ứng dụng Windows Store (UWP Apps Manager)
- **Tự động quét toàn bộ ứng dụng Windows Store**: Nhận diện tất cả các ứng dụng người dùng, ứng dụng cài thêm (ChatGPT, Claude, Canva, v.v.) và các gói mặc định của Windows (Cortana, Dev Home, Xbox Game Bar, Photos, Calculator, v.v.).
- **Tự động trích xuất Icon chính thức**: Đọc và hiển thị trực tiếp biểu tượng PNG sắc nét từ thư mục `Assets\` của gói ứng dụng.
- **Bộ lọc ứng dụng mở rộng**: Cho phép người dùng lọc riêng:
  - `Ứng dụng Windows Store (UWP)`
  - `Ứng dụng máy tính (Win32)`
  - Cùng các bộ lọc quen thuộc: Mới cài đặt, Dung lượng lớn, Microsoft/Bên thứ ba.
- **Cơ chế bảo vệ hệ thống**: Khóa gỡ cài đặt đối với các thành phần cốt lõi của hệ thống (Microsoft Store, App Installer, Edge).
- **Gỡ bỏ an toàn 1-Click**: Tích hợp lệnh PowerShell `Remove-AppxPackage` chạy nền mượt mà, gỡ sạch ứng dụng cho tài khoản hiện tại.

### 2. Công cụ Giám sát Cài đặt Mới (Setup Monitor / Snapshot Tracker)
- **Cơ chế Snapshot 3 bước thông minh**:
  - **Bước 1**: Chụp ảnh trạng thái hệ thống trước khi cài đặt (thư mục `Program Files`, `AppData`, `ProgramData` và các nhánh Registry `Uninstall`, `Run`). Tốc độ chụp siêu nhanh < 0.5 giây.
  - **Bước 2**: Người dùng thoải mái chạy tệp cài đặt (`Setup.exe`, `Installer.msi`) của phần mềm mới.
  - **Bước 3**: Chụp ảnh đối chiếu diff sau khi cài đặt xong, tự động nhận diện và lưu lại mọi tệp tin, thư mục và khóa Registry mới phát sinh vào hồ sơ snapshot XML tại `%LOCALAPPDATA%\PurePC\Snapshots\`.
- **Gỡ bỏ triệt để 100% (Clean Uninstall)**:
  - Cho phép người dùng gỡ bỏ bất kỳ phần mềm nào đã được giám sát.
  - Xóa sạch mọi tệp tin và khóa Registry đã được ghi nhận trong snapshot, không bỏ sót dù chỉ 1 file rác hay 1 khóa đăng ký.
  - Toàn bộ tệp bị xóa đều được chuyển vào Thùng rác để đảm bảo khả năng hoàn tác.

### 3. Quét & Dọn Lối tắt Hỏng (Broken Shortcuts) & Phông chữ mồ côi
- **Tích hợp chế độ 4 trong 1 trong Quét chuyên sâu (Deep Clean)**:
  - Bổ sung tab **`🔗 Lối tắt hỏng`** bên cạnh Tàn dư gỡ cài đặt, Tệp lớn và Tệp trùng nội dung.
  - Tự động duyệt qua Desktop cá nhân, Desktop chung máy, Start Menu cá nhân và Start Menu hệ thống.
  - Phân tích đích đến (`TargetPath`) của các file `.lnk`. Phát hiện chính xác các lối tắt trỏ vào file `.exe` không còn tồn tại trên máy tính.
  - Chuyển các lối tắt hỏng đã chọn vào Thùng rác một cách an toàn.
- **Phông chữ mồ côi (Orphaned Fonts)**:
  - Nâng cấp bộ quét `RegistryCleaner`: Quét nhánh `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts`.
  - Phát hiện các mục đăng ký phông chữ mà tệp `.ttf` / `.otf` thực tế trong `%WINDIR%\Fonts` không còn tồn tại.

### 4. Biểu đồ Sóng Tài nguyên Thời gian thực trên Dashboard (Sparkline Waves)
- Trên thẻ **MỨC DÙNG CPU** và **BỘ NHỚ RAM**, bổ sung biểu đồ sóng (wave line/sparkline) thời gian thực chuyển động mượt mà.
- Lưu trữ 22 mẫu đo gần nhất, vẽ đường cong tài nguyên kèm vùng đổ bóng mềm mại bằng công nghệ đồ họa vector WPF native (`Polygon` và `Polyline`), không tốn tài nguyên phần cứng.
- Mang lại trải nghiệm giám sát hiện đại như Task Manager và các công cụ đo hiệu năng cao cấp.

### 5. Xuất Báo cáo Driver & Thiết bị Toàn diện (Export Driver List)
- Bổ sung nút **`📄 Xuất báo cáo Driver`** trên trang Driver & Thiết bị.
- Tự động tổng hợp thông tin bo mạch chủ, CPU, GPU, hệ điều hành cùng danh sách toàn bộ thiết bị phần cứng, nhà cung cấp, phiên bản và ngày tạo driver.
- Xuất file văn bản `BaoCao_Driver_YYYYMMDD_HHMMSS.txt` ra thẳng màn hình Desktop chỉ với 1-Click.

### 6. Cập nhật Song ngữ & Bộ Kiểm thử Mở rộng
- Bổ sung đầy đủ chuỗi bản dịch tiếng Anh cho tất cả các màn hình, nút bấm và tính năng mới trong `I18n.cs`.
- Mở rộng bộ kiểm thử tự động lên **103 Unit Checks PASS** (bao gồm kiểm thử ánh xạ tên UWP, bảo vệ Store, đối chiếu bộ lọc UWP/Win32, và chu trình lưu/đọc snapshot Setup Monitor).
