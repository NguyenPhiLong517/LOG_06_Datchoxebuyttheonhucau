# Hướng dẫn mở và chạy giao diện Bus On Demand

## 1. Phạm vi giao diện

Project WinForms được viết bằng C# theo mô hình `WinForms UI -> Service/Business Logic -> Data Access (ADO.NET) -> SQL Server`. Mã giao diện được tạo bằng code, không phụ thuộc các file Designer riêng.

Chỉ có các nhóm chức năng thuộc các use case trong báo cáo:

- `FrmDangNhap`: đăng nhập và nhận diện vai trò.
- `FrmMain`: trang chủ, chỉ hiện chức năng theo vai trò.
- `FrmNhuCau`: khai báo, xem trạng thái, hủy nhu cầu đủ điều kiện.
- `FrmDatGhe`: xem chuyến có thể đặt, đặt/hủy đặt chỗ, xác nhận khi hủy muộn.
- `FrmDeXuatTuyen`: duyệt/từ chối đề xuất, chọn xe/lái xe và tạo chuyến.
- `FrmLichTrinh`: điều phối viên xem và chỉnh chuyến chưa khởi hành; lái xe chỉ xem chuyến được giao. Có dời giờ, đổi xe, đổi lái xe, hủy chuyến và mở thêm chuyến.
- `FrmDanhMuc`: quản lý xe và các tham số ở UC16/UC17.
- `FrmThongKe`: thống kê theo khoảng ngày được chọn.
- `FrmVanHanh`: điểm danh, xác nhận khởi hành và hoàn thành chuyến.

Không thêm đăng ký tài khoản, quên mật khẩu, đặt số ghế cụ thể, theo dõi GPS hoặc xuất Excel/PDF vì các chức năng đó không nằm trong phạm vi thiết kế đang dùng.

## 2. Chuẩn bị SQL Server

Bạn đã có database `BusOnDemandDB_Test` và đã chạy các Test Case cơ bản. Không chạy lại script khởi tạo cơ sở dữ liệu trên database đang có bảng.

Trước khi chạy ứng dụng giao diện, cần cập nhật các trạng thái mà Use Case dùng (ví dụ `Đã mở chuyến`, `Không đủ nhóm`, `Đã hủy muộn`, `Đã hủy do hệ thống`, `No-show`). Trong SSMS:

1. Sao lưu `BusOnDemandDB_Test` trước khi thay đổi schema.
2. Mở `01_CapNhatTrangThai_TheoThietKe.sql`.
3. Xác nhận phần đầu có `USE BusOnDemandDB_Test;`.
4. Chạy một lần. Script mở rộng CHECK constraint và cập nhật một số trigger/stored procedure để các trạng thái khớp Use Case; script không xóa các bảng.
5. Mở `02_MatKhauTaiKhoanMau.sql` và chạy để thiết lập mật khẩu cho ba tài khoản mẫu trên database thử nghiệm.

> Nếu SQL Server của bạn không hỗ trợ `CREATE OR ALTER` thì cần bản SQL Server 2016 SP1 trở lên. Không chạy script tạo database ban đầu trên `BusOnDemandDB_Test` vì database này đã có bảng.

## 3. Mở project trong Visual Studio 2022

1. Giải nén gói source vào một thư mục dễ tìm.
2. Mở `BusOnDemand.WinForms.sln` bằng Visual Studio 2022.
3. Project mục tiêu `.NET Framework 4.7.2`. Nếu Visual Studio báo thiếu targeting pack, cài .NET Framework 4.7.2 Developer Pack rồi mở lại solution.
4. Trong Solution Explorer, mở `BusOnDemand.WinForms` -> `App.config`.
5. Sửa `Data Source` cho khớp `Server name` trong cửa sổ kết nối SSMS. Ví dụ:
   - SQL Express: `Data Source=.\SQLEXPRESS;Initial Catalog=BusOnDemandDB_Test;Integrated Security=True;TrustServerCertificate=True;`
   - LocalDB: `Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=BusOnDemandDB_Test;Integrated Security=True;TrustServerCertificate=True;`
   - Nếu SSMS kết nối bằng `TENMAY\TENINSTANCE`, điền đúng chuỗi đó vào `Data Source`.
6. Chọn `Build -> Build Solution`. Nếu Visual Studio báo lỗi, đọc lỗi đầu tiên trong `Error List`, sửa trước rồi build lại.
7. Nhấn `F5` để chạy.

## 4. Tài khoản minh họa

Sau khi chạy `02_MatKhauTaiKhoanMau.sql`, các tài khoản thử nghiệm đều dùng mật khẩu `Demo123!`:

| Tên đăng nhập | Vai trò | Form hiển thị chính |
|---|---|---|
| `nd01` | Hành khách | Nhu cầu di chuyển, Đặt ghế |
| `nd02` | Lái xe | Lịch trình, Vận hành chuyến |
| `nd03` | Điều phối viên | Đề xuất chuyến, Lịch trình, Danh mục & Cấu hình, Thống kê |

Đây là mật khẩu minh họa chỉ dùng trên database thử nghiệm, không dùng cho hệ thống thật.

## 5. Thứ tự thao tác để minh họa nghiệp vụ

1. Đăng nhập bằng `nd01`, tạo nhu cầu có cùng ngày đi, hướng và khung giờ. Để kiểm thử UC04 nhanh hơn, có thể tạm chỉnh `CHU_KY_JOB_PHUT=1` tại Form `FrmDanhMuc`, sau đó chờ tác vụ nền chạy (kiểm tra lại sau hơn một phút). Đặt lại giá trị ban đầu sau khi thử.
2. Đăng nhập `nd03`, mở Đề xuất chuyến. Khi nhóm đủ ngưỡng, hệ thống lập lịch tạo đề xuất; điều phối viên chọn giờ, xe, lái xe và tạo chuyến.
3. Đăng nhập `nd01`, mở Đặt ghế, chọn chuyến liên quan để đặt/hủy đặt chỗ.
4. Đăng nhập `nd02`, mở Lịch trình và Vận hành chuyến. Chọn chuyến được giao, điền Có mặt/Vắng mặt cho từng đặt chỗ rồi lưu điểm danh. Khi chuyến còn ở trạng thái Dự kiến, có thể sửa kết quả và lưu lại; chỉ sau đó mới xác nhận khởi hành. Khi chuyến đã khởi hành mới xác nhận hoàn thành.
5. Đăng nhập `nd03`, thử dời giờ/đổi xe/đổi lái xe/hủy chuyến/mở thêm chuyến và xem thống kê.

Tác vụ gom nhóm hiện chạy trong tiến trình WinForms khi ứng dụng đang mở, với chu kỳ đọc từ `CHU_KY_JOB_PHUT`. Đóng ứng dụng thì tác vụ này dừng; triển khai vận hành liên tục cần chuyển tác vụ nền sang Windows Service/SQL Server Agent theo thiết kế triển khai.

## 6. Lưu ý phạm vi và kiểm chứng

- Project dùng các thủ tục/trigger có trong SQL và file cập nhật ở mục 2; UI hiển thị thông báo kết quả sau thao tác.
- Báo cáo Use Case nhắc đến việc thông báo tới hành khách sau điều chỉnh/hủy chuyến, nhưng schema 10 bảng hiện chưa có bảng hoặc kênh thông báo riêng. Do đó, bản demo hiện xác nhận thao tác ở phía điều phối viên và hiển thị trạng thái chuyến khi người dùng tra cứu lại; chưa gửi push/email chủ động. Không nên ghi rằng email/push đã triển khai.
- Các hình trong mục 5 của báo cáo là bản thiết kế giao diện tham khảo. Cần biên dịch và chạy trên Visual Studio 2022, kết nối đúng SQL Server rồi đối chiếu Test Case trên máy trước khi kết luận ứng dụng chạy hoàn tất. Môi trường tạo source này chưa có MSBuild/Visual Studio để biên dịch C# trực tiếp.
