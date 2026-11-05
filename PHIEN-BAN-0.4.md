# PurePC 0.4

Đã thêm Google, gỡ MSI cho thành phần phụ hợp lệ, quét chuyên sâu dựa trên đăng ký/lịch sử, thao tác xóa tệp lớn và bản trùng, nút Disk Cleanup/Optimize Drives, kiểm kê và cập nhật driver qua Windows Update.

Đã gỡ thật, theo xác nhận UAC của người dùng: AutoCAD 2025 VBA Enabler, AutoCAD 2025 - English, ACAD Private. Cả ba exit=0, khóa Uninstall không còn và MSI state=-1. Thành phần Autodesk Interoperability Engine Manager vẫn state=5. Không nhắm đến bản vẽ hoặc thành phần dùng chung. Nhật ký và bản xuất Registry ở thư mục AutoCAD-removal cạnh bộ phần mềm.

Kiểm thử: 81 kiểm tra tự động; render WPF 10 trang Light/Dark; kiểm kê thật 153 bản ghi driver, GPU NVIDIA GTX 1650 SUPER. Kiểm tra này chỉ đọc, không tải/cài driver. Chức năng tìm/tải/cài Windows Update đã triển khai nhưng chưa kiểm thử cài thật trên máy; không tuyên bố toàn bộ driver đã được cập nhật.

Quét chuyên sâu không thể xác định chắc chắn mọi thư mục từ các ứng dụng đã gỡ trước khi có lịch sử. Chỉ xử lý khi có bằng chứng và người dùng chọn. Không tự xóa thư mục vì trùng tên hoặc không còn đăng ký. Disk Cleanup và tối ưu ổ đĩa dùng giao diện Windows, không phải một bộ máy thay thế riêng.

Tài liệu triển khai chính thức: [Windows Update Agent](https://learn.microsoft.com/en-us/windows/win32/wua_sdk/searching--downloading--and-installing-updates), [MSI product state](https://learn.microsoft.com/en-us/windows/win32/api/msi/nf-msi-msiqueryproductstatew), [Optimize Drives](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/defrag).
