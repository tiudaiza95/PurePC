# PurePC 0.6

Tổng quan đã có quét, bảng xem trước và dọn tệp thật sau xác nhận. Gộp các vị trí Temp/cache xác định được, báo cáo lỗi cũ và phần dư thuộc lịch sử gỡ. Phần dư có khả năng chứa dữ liệu người dùng không được chọn tự động. Kiểm tra lại đường dẫn, quyền, liên kết, kích thước/thời gian và ứng dụng đang mở trước xử lý. Dọn ưu tiên Thùng rác; không coi dung lượng tệp xử lý là dung lượng đã giải phóng.

Đã bỏ các lời gọi Windows Update Agent, tìm/cài driver qua mạng Microsoft và nút mở Windows Update. Không thay đổi cấu hình dịch vụ cập nhật của hệ điều hành. Luồng CLI tìm qua Windows Update cũ trả mã từ chối, không thực hiện truy vấn.

Driver mới: đọc PnP tại máy, nhận diện lỗi thiết bị, nguồn hãng theo phần cứng, sao chép Hardware ID; mở nguồn trong trình duyệt. Với INF người dùng tải/giải nén từ hãng: đối chiếu Hardware ID và class, kiểm tra chữ ký catalog offline, sao lưu driver bên thứ ba rồi cài qua PnPUtil. Không ép driver có xếp hạng thấp hơn, không tự khởi động lại; ghi báo cáo và quét lại để xác minh phiên bản. Không phải hệ thống tự động tải/cập nhật mọi driver. Chưa kiểm thử cài driver thật; cần kiểm tra gói thực tế theo thiết bị và hệ điều hành trước dùng.

Kiểm thử: 96 kiểm tra tự động đạt; render 11 trang sáng/tối và thao tác chọn rác/giữ bản trùng bằng dữ liệu giả. Đã quét chỉ đọc 24 vị trí, tìm 20.997 tệp đề xuất / 2.599.243.485 byte; có 2 nguồn chạm giới hạn 5.000 kết quả, được ghi rõ. Đọc 155 bản ghi thiết bị/driver; nhận diện ASUS PRIME H510M-K, NVIDIA GTX 1650 SUPER, Realtek Audio. Không xóa tệp cá nhân, tải driver, cài driver hoặc cập nhật hệ điều hành trong kiểm thử.

Giới hạn: không xóa tệp chỉ vì cũ/rỗng/nghi lỗi; xóa crash report không sửa nguyên nhân lỗi. Không thể xác định mọi phần mềm đã gỡ trước khi có lịch sử. Cache đang dùng phải đóng ứng dụng trước. Cài INF hỗ trợ các nhóm ngoại vi chỉ định; BIOS/firmware và chipset System dùng trình cài hãng.

Nguồn: [NVIDIA App](https://www.nvidia.com/en-us/software/nvidia-app/), [ASUS PRIME H510M-K](https://www.asus.com/motherboards-components/motherboards/prime/prime-h510m-k/helpdesk_download?model2Name=PRIME-H510M-K), [Intel DSA](https://www.intel.com/content/www/us/en/support/detect.html), [PnPUtil](https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil-examples).
