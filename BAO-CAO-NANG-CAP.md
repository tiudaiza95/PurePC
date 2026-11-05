# Dọn Dẹp PC 0.3 — kết quả nâng cấp

Ngày: 24/10/2026. Nâng cấp trực tiếp mã C#/.NET Framework/WPF của Sạch Gọn 0.2; giữ lại Engine, Features, quản lý Run và cơ chế bảo vệ đường dẫn. Không phải dự án mới hay giao diện mô phỏng.

## Đã triển khai

| Hạng mục | Phạm vi hoạt động |
|---|---|
| Nhận diện | Tên Dọn Dẹp PC, icon gốc mới, executable PurePC.exe, shortcut Desktop |
| Giao diện | Light/Dark, lưu chế độ; thanh trái thu gọn; co giãn cửa sổ; trạng thái xử lý/kết quả/lỗi; thẻ tổng quan chỉ ở Health Check |
| Danh sách | STT sau tìm/lọc/sắp xếp; icon thật từ tệp hoặc icon chung; tên, publisher, version, size, date, trạng thái, phân loại, nút thông tin/gỡ; nhiều lựa chọn Ctrl/Shift; menu chuột phải |
| Nguồn danh sách | Registry Uninstall HKCU/HKLM 32/64-bit. Có cả mục SystemComponent và mục không có trình gỡ, được giữ chỉ xem khi phù hợp. Không phải danh sách toàn bộ Microsoft Store |
| Dung lượng/ngày | EstimatedSize × 1.024 byte; hiển thị nguồn và cảnh báo là số bộ cài khai báo. InstallDate có thể là ngày cập nhật. Không có dữ liệu → Chưa rõ |
| Bộ lọc | Mới cài 30 ngày, ≥1 GB, nhà phát hành Microsoft, bên thứ ba, hệ thống/driver, cần thận trọng, cân nhắc theo nhu cầu. Ít sử dụng trả rõ chưa có dữ liệu, không bịa lịch sử sử dụng |
| Thông tin phần mềm | Hồ sơ riêng gồm metadata, chức năng, ảnh hưởng, lý do, độ tin cậy, sao lưu, nguồn và ngày xác minh; copy; mở vị trí; nguồn chính thức; tìm trực tuyến có xác nhận |
| Kiến thức offline | Quy tắc có nguồn cho Visual C++, .NET, Zalo, CapCut, 7-Zip và cảnh báo ASUS. Các mục khác hiển thị chưa đủ thông tin. Không suy đoán quốc gia |
| Chữ ký | WinVerifyTrust ngoại tuyến trên tệp tham chiếu được chỉ rõ. Không tuyên bố kết quả đại diện mọi tệp, không truy vấn thu hồi chứng thư qua mạng |
| Trùng mục cài | Gộp đúng danh tính HKCU/metadata từ các view dùng chung; giữ danh sách nguồn. Khác phiên bản, khóa, phạm vi, command hoặc kiến trúc HKLM giữ riêng. Có cây mở rộng để xem |
| Gỡ | Trình gỡ chính thức, xác nhận rõ danh sách, tuần tự, báo exit code và trạng thái Registry; dừng chuỗi khi chưa xác nhận xong; không runas mặc định. Chặn driver/bảo mật/SystemComponent và chặn batch có mục chưa rõ/cần thận trọng |
| Trình gỡ lỗi | Báo lỗi thật, giữ nguyên dữ liệu; đường mở Cài đặt Windows và nguồn chính thức để sửa bằng bộ cài. Không ép xóa thư mục để giả vờ đã gỡ |
| Phần dư | Thư mục theo quy tắc có giới hạn; số byte/độ đầy đủ; shortcut hỏng có đích thuộc vị trí cài đã xác định; mức tin cậy; chọn từng mục. Registry khớp tên chỉ xem, không được tự xóa |
| Lịch sử gỡ | Ghi các lần gỡ do ứng dụng khởi chạy; chọn lại để quét phần dư. Không đoán được phần mềm đã gỡ trước khi công cụ ghi nhận |
| Cleanup | Temp tài khoản, Windows Temp với quyền hiện tại; cache Chrome/Edge từng profile và cache VS Code có trong danh sách cho phép; tuổi tạo và sửa >7 ngày; không chọn sẵn, không dọn cookie/mật khẩu/phiên đăng nhập |
| Health Check | Temp đủ điều kiện xem trước, Run, số mục cần thận trọng và nguồn đọc gộp. Không có “điểm sức khỏe” hay phần trăm giả |
| Dung lượng | Duyệt thư mục chọn, bảng tệp ≥100 MB, biểu đồ thanh theo phần mở rộng; kích thước logic của tệp đọc được, không coi là dung lượng vật lý |
| Tệp trùng nội dung | Nhóm kích thước và SHA-256; kiểm tra thay đổi; báo giới hạn và mục bỏ qua. Chỉ xem/mở vị trí; không tự xóa. Có thể gồm hard link, không tính chúng thành dung lượng chắc chắn thu hồi |
| Tiến trình | Đếm mục đã duyệt, tệp tìm thấy, byte đã hash; nút dừng quét. Không tạo phần trăm ước lệ |
| Startup | Giữ module HKCU Run: sao lưu trước khi tắt, bật lại không ghi đè. Trạng thái là đăng ký Run, không phải đo Startup Impact |
| Updater | Gọi WinGet thật sau xác nhận; hiển thị output/error/exit code; cập nhật theo ID chính xác. Không tự đồng ý điều khoản hay chạy upgrade --all |
| Lịch quét | Module Task Scheduler quyền thường, tài khoản đăng nhập, hàng ngày; chỉ quét Temp và thông báo, không dọn. Mặc định chưa tạo lịch |
| Lịch sử/khôi phục | Ghi thao tác; giữ bản sao Registry cũ; mở Thùng rác. Báo riêng byte chuyển vào Thùng rác và biến động free-space đo được; không gọi toàn bộ byte tái chế là dung lượng giải phóng |

## Kiểm thử đã thực hiện

- Biên dịch thành công bằng compiler .NET Framework trên máy.
- **73 checks đạt**: nhận diện và gộp nguồn; phiên bản/phạm vi khác; khuyến nghị có căn cứ/unknown; không tự nâng quyền; khóa Registry chỉ xem; bộ lọc và ngày; giới hạn đường dẫn; tuổi/chỉ đọc/thay đổi tệp; SHA-256; đọc cây; hủy; lịch sử gỡ; sao lưu/khôi phục Run trên nhánh giả; quote/ID WinGet; stdout Unicode, stderr, exit code và hủy tiến trình console fixture.
- Kiểm tra WPF bằng fixture: tìm kiếm, sắp xếp chuỗi/số, STT, selection. Kết xuất 8 màn hình ở cả Light/Dark, hồ sơ phần mềm, và sidebar thu gọn tại kích thước tối thiểu.
- Không gỡ ứng dụng, dọn dữ liệu, tắt mục Run thật, cập nhật gói thật hoặc tạo lịch thực trên máy trong quá trình kiểm thử.
- Việc trích metadata/icon và tạo preview chỉ đọc danh sách ứng dụng tại máy.

## Chưa được xác nhận end-to-end / chưa triển khai

**Chưa chạy thay đổi máy thật:** quy trình gỡ nhiều bộ cài bên thứ ba; chuyển dữ liệu thật vào Thùng rác; update qua WinGet; đăng ký và chạy task theo giờ thật. Có mã thực, không phải nút giả, nhưng kiểm thử fixture không thay cho thử nghiệm tích hợp trong máy ảo Windows sạch. Không coi mọi bộ cài là tương thích.

**Các module nâng cao còn để giai đoạn sau:** Install Monitor toàn hệ thống; Browser Extension Manager; đo Startup Impact; Software Relocator; khôi phục phiên bản sau update; ép gỡ phần mềm; kiểm kê/gỡ đầy đủ Microsoft Store. Không có nút giả để quảng cáo các chức năng này.

Kiến thức offline chưa phủ mọi phần mềm. Tra cứu trực tuyến hiện mở truy vấn tên/publisher trong trình duyệt, ghi ngày và URL; **không tự đọc rồi tuyên bố đã xác minh nội dung web**. Không dùng API trả phí hoặc gửi toàn bộ danh sách lên dịch vụ riêng.

## Giới hạn quét và khôi phục

- Cây tệp: tối đa 100.000 mục / 50.000 tệp mỗi lượt, bỏ qua liên kết và lỗi quyền. Cleanup đề xuất tối đa 5.000 tệp. Hash tối đa 20 GB. Tệp lớn hiển thị 500 mục. Mọi kết quả chỉ đại diện phạm vi đã duyệt.
- Không có quyền → giữ lại; không chiếm quyền sở hữu, không sửa ACL và không tự nâng Administrator.
- Thùng rác phụ thuộc ổ đĩa/Windows. Nếu được hỏi xóa vĩnh viễn, hãy Hủy nếu muốn khôi phục. Chuyển vào Thùng rác thường chưa giải phóng dung lượng đĩa.
- Biến động free-space trước/sau có thể bị tác vụ khác ảnh hưởng; không thể quy toàn bộ biến động cho công cụ.
- Để tương thích dữ liệu 0.2, lịch sử/bản sao vẫn nằm trong `%LOCALAPPDATA%\PurePC`. Tên này là đường dẫn dữ liệu cũ, không phải tên ứng dụng hiện tại.

## Nguồn kiến thức đã đối chiếu ngày 24/10/2026

- [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist): vai trò thư viện runtime dùng chung.
- [Cài .NET trên Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows): phân biệt runtime/SDK và ứng dụng phụ thuộc.
- [ASUS System Control Interface và MyASUS](https://www.asus.com/support/faq/1045676/): không đại diện tài liệu riêng cho mọi dịch vụ ASUS.
- [Zalo PC](https://zalo.me/pc): chức năng liên lạc, đồng bộ và gửi tệp.
- [CapCut](https://www.capcut.com/): công cụ biên tập video.
- [7-Zip](https://www.7-zip.org/): nén/giải nén.
- [WinGet upgrade](https://learn.microsoft.com/en-us/windows/package-manager/winget/upgrade): cơ chế kiểm tra/cập nhật gói.
