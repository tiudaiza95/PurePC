# Nhật ký phát hành chính thức Phiên bản 1.0 · Dọn Dẹp PC (PurePC Pro Max)

> **Phiên bản**: `1.0.0.0` (Bản phát hành chính thức - Official Release)  
> **Kiến trúc**: 64-bit Native (.NET 4.8 / WPF Desktop)  
> **Trạng thái kiểm thử**: PASS 96/96 Unit Checks  
> **Mã băm SHA-256**: `729F566CD4060A6B5B91067CCEE5B3947E7BE16E8F951B077D14E7D9CD1872C9`  
> **Lối tắt Desktop**: `C:\Users\Admin\Desktop\Dọn Dẹp PC.lnk` trỏ trực tiếp đến `C:\Users\Admin\AppData\Local\Programs\PurePC\PurePC.exe`  
> **Kho mã nguồn GitHub**: [https://github.com/tiudaiza95/PurePC](https://github.com/tiudaiza95/PurePC)

---

## 🌟 Tổng quan Cột mốc Phiên bản 1.0

Phiên bản **1.0.0.0** đánh dấu sự hoàn thiện trọn vẹn mô hình kết hợp những ưu điểm xuất sắc nhất của **CCleaner**, **IObit Uninstaller**, cùng hệ thống bảo trì đĩa và cập nhật driver an toàn tuyệt đối của Windows:

```
+---------------------------------------------------------------------------------------+
|                                  DỌN DẸP PC PRO MAX v1.0                              |
|                                                                                       |
|   [CCLEANER]                [IOBIT UNINSTALLER]          [DRIVER & DISK TOOLS]        |
|   • Tối ưu RAM thời gian    • Gỡ cài đặt tiêu chuẩn      • Kiểm kê phần cứng máy      |
|   • Dọn tệp rác (%TEMP%)    • Gỡ cưỡng chế sâu           • 1-Click mở Device Manager  |
|   • Dọn cache 7 trình duyệt • Gỡ hàng loạt (Batch queue) • 6 bộ công cụ bảo trì ổ đĩa |
|   • Dọn Registry mồ côi     • Dọn tàn dư sau gỡ          • Nén Compact OS / Hibernate |
+---------------------------------------------------------------------------------------+
|   • Đa ngôn ngữ: Tiếng Việt (Mặc định) & English (US)                                |
|   • Giao diện cân đối, hiện đại, hỗ trợ Dark Mode & Thu gọn Sidebar                   |
|   • An toàn dữ liệu 100%: Ưu tiên Thùng rác, tự động sao lưu .reg trước khi xử lý     |
+---------------------------------------------------------------------------------------+
```

---

## 🚀 Các Tính năng Mới & Cải tiến trong Phiên bản 1.0

### 1. Nâng cấp Toàn diện Trang Driver & Thiết bị
- Bổ sung nút truy cập nhanh **`Mở Device Manager (devmgmt.msc)`** giúp kiểm tra cây phần cứng trực tiếp từ Windows.
- Đồng bộ hóa toàn bộ tiêu đề, cột dữ liệu và các nút chức năng trên trang Driver với công cụ đa ngôn ngữ `I18n`.
- Giữ vững nguyên tắc an toàn: Không tải driver trôi nổi trên mạng, hiển thị link trang chủ chính thức của nhà sản xuất (ASUS, Intel, NVIDIA, Realtek, v.v.) và chỉ cài đặt các gói INF có chữ ký hợp lệ sau khi đã sao lưu driver bên thứ ba.

### 2. Trung tâm Bản quyền & Cập nhật GitHub trực tiếp
- Trong mục Cài đặt (`☰` > Giới thiệu), tích hợp 2 nút tiện ích:
  - **`Kiểm tra bản cập nhật trên GitHub`**: Mở trực tiếp trang phát hành chính thức.
  - **`Mở Kho mã nguồn GitHub`**: Truy cập mã nguồn mở tại `tiudaiza95/PurePC`.

### 3. Động cơ Đa ngôn ngữ Hoàn thiện & Khởi tạo An toàn (Fail-Safe I18n)
- Từ điển song ngữ Anh - Việt hoàn chỉnh từ giao diện chính, thanh điều hướng, các cửa sổ tiện ích (Registry, Tiện ích trình duyệt, Quản lý dịch vụ, Gỡ hàng loạt) đến trang Driver.
- Cơ chế nạp từ điển chống trùng lặp, bọc khối ngoại lệ an toàn, đảm bảo khởi động ứng dụng trơn tru không bao giờ phát sinh lỗi.

### 4. Giao diện Người dùng Tinh chuẩn
- Toàn bộ tiêu đề trang chính và phụ đề đều được căn giữa (`Center`) chuẩn mực.
- Thanh công cụ Ứng dụng thẳng hàng tắp với khoảng đệm đồng nhất (`Padding="18,12"`, `Margin="0,0,8,8"`).
- Thẻ Dọn dẹp hệ thống tại trang Tổng quan gọn gàng, thoáng đãng với lưới 4 nút chức năng đối xứng 2x2.
- 6 thẻ công cụ ổ đĩa loại bỏ sạch các chú thích trong ngoặc đơn, hiển thị rõ ràng, chuyên nghiệp.

---

## 🛡️ Kiểm thử & Nghiệm thu
- **Bộ kiểm tra tự động**: `PASS: 96 checks`.
- **Khởi chạy ứng dụng**: Hoạt động mượt mà, phản hồi tức thì, không giật lag.
- **Tính toàn vẹn**: Không có bất kỳ cảnh báo biên dịch nào.
