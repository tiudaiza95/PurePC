# PurePC 0.5

Hoàn tất ngày 26/10/2026. Đã tách Công cụ ổ đĩa thành hai thẻ riêng; bỏ nút Disk Cleanup/tối ưu khỏi hàng thao tác tệp lớn. Sửa tiêu đề cột khi bảng trống và sidebar thu gọn.

Quét tổng quan cũ chỉ xem Temp và metadata hệ thống. Bản này thay bằng duyệt cây thư mục trên mọi ổ Fixed đang sẵn sàng; có số tệp/thư mục, thời gian, đường dẫn, số mục bỏ qua, nút dừng và kết quả một phần. Không giới hạn số tệp duyệt; chỉ giữ tối đa 1.000 tệp lớn nhất để trình bày. Không đọc mọi nội dung hay kiểm tra virus. Không duyệt ổ mạng/USB, reparse points, Thùng rác và System Volume Information.

Lượt thử thực tế C: và D: đọc 552.013 tệp, 118.810 thư mục trong 2 phút 25 giây; ghi nhận 662 mục/thư mục bỏ qua và 161 tệp từ 100 MB. Không xóa dữ liệu. Đây là số liệu của lượt thử, không phải cam kết tốc độ hay số lượng các lần sau.

Tệp trùng có nút tự chọn bản dư, giữ một bản có đường dẫn ngắn nhất trong mỗi nhóm hợp lệ. Người dùng xem lại trước khi xác nhận dọn; nội dung được đối chiếu lại và chặn xóa cả nhóm. Không gộp nội dung khác nhau hoặc tạo hard link.

Driver có luồng ba bước, thanh hoạt động và đồng hồ. Tìm Windows Update chạy trong tiến trình riêng, hủy được và có timeout 10 phút. Trong lúc cài có trạng thái từng giai đoạn và đồng hồ; không cưỡng bức ngắt cài hoặc khởi động lại. Cần quản trị sẽ mở lại đúng trang Driver để kiểm tra lại. Không tải/cài driver thật trong kiểm thử.

Xác minh: 87 kiểm tra tự động đạt, gồm duyệt cây tệp, tổng dung lượng fixture, hủy quét, giữ bản trùng, chặn đường dẫn được bảo vệ, kết thúc worker khi hủy/timeout. Render 11 trang sáng/tối, sidebar thu gọn và thao tác nút tự chọn bản trùng bằng dữ liệu giả; không xóa dữ liệu người dùng trong kiểm thử. Tìm/cài Windows Update còn phụ thuộc mạng, dịch vụ và chính sách máy; chưa kiểm thử cài driver thật.
