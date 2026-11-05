# Rà soát trước nâng cấp → PurePC

Ngày rà soát: 24/10/2026. Giữ kiến trúc C#/.NET Framework/WPF, Engine và Features; không tạo dự án mới.

| Phần | Hiện trạng 0.2 | Việc cần sửa |
|---|---|---|
| Danh sách | Đọc Uninstall HKCU/HKLM 32/64-bit; gộp HKCU cùng mục | Bỏ qua mục không có lệnh gỡ và SystemComponent; thiếu thông tin nguồn, icon, kích thước, ngày |
| Trùng | Chỉ gộp HKCU cùng khóa và metadata | Không có giao diện xem các nguồn bị gộp; kiểm tra hiện phụ thuộc Zalo/CapCut thật |
| Gỡ | Chạy trình gỡ chính thức | Luôn dùng runas; chưa báo exit code và trạng thái Registry sau gỡ; không gỡ nhiều |
| Phần dư | Thư mục theo tên và HKCU Software | Registry chỉ có bằng chứng khớp tên nên chưa đủ điều kiện xóa an toàn; thiếu dung lượng/shortcut |
| Temp | Lọc tuổi 7 ngày, kiểm tra thay đổi/tệp đang dùng | Không có tiến trình đếm thật, Windows Temp và cache chọn lọc |
| Tệp lớn | Quét từ 100 MB, bỏ liên kết và giới hạn mục | Không có biểu đồ và tìm tệp trùng theo nội dung |
| Khởi động | Sao lưu/khôi phục chuỗi HKCU Run | Chỉ phản ánh đăng ký Run, chưa đo ảnh hưởng khởi động |
| Giao diện | 5 trang WPF, một chế độ sáng | Thẻ thống kê lặp, icon chữ cái, thiếu chi tiết, menu ngữ cảnh và chủ đề tối |
| Khôi phục | Xuất .reg và dùng Thùng rác Windows | Không nên cam kết mọi thao tác đều hoàn tác được; cần phân biệt byte chuyển vào Thùng rác với byte giải phóng |
| Kiểm thử | 33 checks, render 5 trang | Bổ sung fixture không phụ thuộc phần mềm thực, test lọc/sắp xếp/nhóm/nguồn/quét/hash và theme |

Quy tắc thực hiện: chỉ đọc dữ liệu máy để hiển thị; thử xóa/gỡ/cập nhật hoặc thay đổi khởi động chỉ trên fixture. Không tự chạy dọn, nâng quyền, quét mạng hay tạo lịch hoạt động trong máy người dùng khi đóng gói.
