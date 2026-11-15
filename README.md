# Hệ Thống Đặt Chỗ Xe Buýt Theo Nhu Cầu Quy Mô Trường (Bus On Demand)

Hệ thống hỗ trợ quản lý nhu cầu đi lại, gom nhóm nhu cầu, đề xuất mở tuyến xe buýt theo nhu cầu học tập/làm việc giữa các cơ sở của trường, quản lý lịch trình, đặt chỗ/hủy chỗ, vận hành chuyến xe và ghi nhận điểm danh / no-show.

> **Đồ án cuối kỳ:** Phương pháp phát triển phần mềm hướng đối tượng (OOSD_CNPM2)  
> **Khoa:** Công nghệ Thông tin - Trường Đại học Tài nguyên và Môi trường TP.HCM  
> **Sinh viên thực hiện:** Nguyễn Phi Long (MSSV: 1250080106 - Lớp: 12_ĐH_CNPM2 - Khóa 2023–2027)  
> **Giảng viên hướng dẫn:** ThS. Phạm Trọng Huynh  

---

## 1. Cấu trúc thư mục Repository

Repository được tổ chức theo các phân hệ thư mục rõ ràng:

```
LOG_06_Datchoxebuyttheonhucau/
├── BusOnDemand.WinForms.sln          # Solution Visual Studio 2022
├── BusOnDemand.WinForms/             # Dự án WinForms (.NET Framework 4.7.2)
│   ├── Core/                         # Tầng Core, Enums, DTO, Db, PasswordHasher
│   ├── Forms/                        # Tầng Giao diện (WinForms code-behind)
│   │   ├── FrmDangNhap.cs            # Đăng nhập và phân quyền vai trò
│   │   ├── FrmMain.cs                # Màn hình chính điều hướng theo vai trò
│   │   ├── FrmNhuCau.cs              # Khai báo và quản lý nhu cầu đi lại
│   │   ├── FrmDatGhe.cs              # Xem chuyến và đặt chỗ / hủy chỗ
│   │   ├── FrmDeXuatTuyen.cs         # Duyệt gom nhóm và đề xuất mở tuyến
│   │   ├── FrmLichTrinh.cs           # Quản lý & điều chỉnh lịch trình chuyến xe
│   │   ├── FrmDanhMuc.cs             # Quản lý xe và cấu hình ngưỡng vận hành
│   │   ├── FrmThongKe.cs             # Thống kê tỷ lệ lấp đầy, vi phạm
│   │   └── FrmVanHanh.cs             # Điểm danh, khởi hành và hoàn thành chuyến
│   └── App.config                    # Cấu hình chuỗi kết nối SQL Server
├── BusOnDemand.Tests/                # Dự án kịch bản kiểm thử (Tests)
├── diagrams/                         # Toàn bộ sơ đồ phân tích và thiết kế
│   ├── Tong_hop_cac_so_do.drawio     # File thiết kế gốc draw.io (dễ tìm, mở trực tiếp)
│   └── images/                       # Thư mục lưu 33 ảnh sơ đồ trích xuất từ báo cáo
│       ├── Hình_21_*.png             # Biểu đồ Use Case tổng quát (Level 0)
│       ├── Hình_22_*.png .. 25_*.png # Biểu đồ Use Case phân rã
│       ├── Hình_26_*.png             # Biểu đồ lớp phân tích (Analysis Class)
│       ├── Hình_27_*.png .. 29_*.png # Biểu đồ trạng thái (State Machine)
│       ├── Hình_31_*.png .. 34_*.png # Biểu đồ tuần tự (Sequence Diagram)
│       ├── Hình_35_*.png .. 38_*.png # Biểu đồ hoạt động (Activity Diagram)
│       ├── Hình_39_*.png .. 313_*.png# Biểu đồ lớp thiết kế chi tiết (Design Class)
│       ├── Hình_41_*.png             # Sơ đồ quan hệ thực thể (ERD)
│       └── Hình_51_*.png             # Sơ đồ điều hướng hệ thống Form
├── SQL/                              # Toàn bộ script Cơ sở dữ liệu SQL Server
│   ├── BusOnDemandDB_SQL_HoanChinh.sql   # Script CSDL hoàn chỉnh đầy đủ (chạy trực tiếp)
│   ├── KiemThu_SQL_BusOnDemand.sql       # Script chạy kiểm thử CSDL
│   ├── 00_TaoCoSoDuLieu_Moi.sql          # Script tạo bảng, quan hệ
│   ├── 01_CapNhatTrangThai_TheoThietKe.sql # Ràng buộc và logic nghiệp vụ
│   └── 02_MatKhauTaiKhoanMau.sql         # Dữ liệu tài khoản mẫu
├── BusOnDemand_Figma/                # Thiết kế UI Figma (Tokens, SVG, Preview)
├── GiaoDien_Mockup/                  # Ảnh chụp mockup giao diện các Form
├── docs/                             # Tài liệu hướng dẫn cài đặt & sửa giao diện
│   ├── HuongDan_CaiDat_Chay_WinForms_BusOnDemand.docx
│   ├── README_HUONG_DAN.md
│   └── README_UI_LAYOUT_FIX.txt
└── README.md                         # Tài liệu giới thiệu và hướng dẫn tổng quan
```

---

## 2. Công nghệ sử dụng

| Thành phần | Công nghệ / Thư viện |
|---|---|
| **Ngôn ngữ & Nền tảng** | C#, .NET Framework 4.7.2 |
| **Giao diện (UI)** | Windows Forms (Code-based layout, UI responsive & DPI aware) |
| **Cơ sở dữ liệu** | Microsoft SQL Server (Transact-SQL, Stored Procedures, Triggers) |
| **Truy cập dữ liệu** | ADO.NET (`System.Data.SqlClient`) |
| **Bảo mật** | PBKDF2 / SHA-256 (`PasswordHasher`) |
| **Thiết kế mô hình** | Draw.io, Enterprise Architect, UML 2.5 |
| **Thiết kế giao diện** | Figma |

---

## 3. Yêu cầu cài đặt môi trường

1. **Hệ điều hành:** Windows 10 / Windows 11.
2. **IDE:** Microsoft Visual Studio 2022 (hoặc 2019) đã cài đặt workload **.NET desktop development**.
3. **Cơ sở dữ liệu:** Microsoft SQL Server 2016 SP1 trở lên (SQL Server 2019 / 2022 / Express / Developer) & SQL Server Management Studio (SSMS).

---

## 4. Hướng dẫn cài đặt và chạy chương trình

### Bước 1: Khởi tạo Cơ sở dữ liệu trên SQL Server

1. Khởi động **SQL Server Management Studio (SSMS)** và kết nối vào SQL Server instance của bạn.
2. Mở file `SQL/BusOnDemandDB_SQL_HoanChinh.sql`:
   - Nhấn **Execute (F5)** để tạo database `BusOnDemandDB_Test`, các bảng, quan hệ, chỉ mục, view, stored procedures, triggers và dữ liệu kiểm thử mẫu.
3. Chạy thêm file `SQL/02_MatKhauTaiKhoanMau.sql` để bảo đảm các tài khoản mẫu được kích hoạt mật khẩu demo.

### Bước 2: Cấu hình chuỗi kết nối trong ứng dụng

1. Mở file solution `BusOnDemand.WinForms.sln` trong **Visual Studio 2022**.
2. Tìm đến file `App.config` trong dự án `BusOnDemand.WinForms`.
3. Chỉnh sửa tham số `Data Source` cho phù hợp với SQL Server trên máy của bạn:
   ```xml
   <connectionStrings>
     <add name="BusOnDemand"
          connectionString="Data Source=localhost;Initial Catalog=BusOnDemandDB_Test;Integrated Security=True;TrustServerCertificate=True;Application Name=BusOnDemand.WinForms;"
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```
   *(Ví dụ: nếu dùng SQL Express, sửa thành `Data Source=.\SQLEXPRESS`)*.

### Bước 3: Build và khởi chạy

1. Trong Visual Studio, chọn chế độ **Debug** hoặc **Release** (`Any CPU`).
2. Chọn menu **Build** -> **Build Solution** (hoặc nhấn `Ctrl + Shift + B`).
3. Nhấn **F5** (hoặc nút **Start**) để khởi chạy ứng dụng.

---

## 5. Danh sách tài khoản thử nghiệm

Sau khi chạy script khởi tạo dữ liệu, bạn có thể đăng nhập bằng các tài khoản mẫu dưới đây (mật khẩu mặc định: `Demo123!`):

| Tài khoản | Mật khẩu | Vai trò | Chức năng chính |
|---|---|---|---|
| `admin` | `Demo123!` | Quản trị viên (Admin) | Truy cập toàn bộ hệ thống, quản lý danh mục xe, cấu hình ngưỡng |
| `dieuphoi` | `Demo123!` | Điều phối viên (Dispatcher) | Duyệt đề xuất mở tuyến, tạo và điều chỉnh lịch trình chuyến xe |
| `laixe` | `Demo123!` | Lái xe (Driver) | Xem lịch trình phân công, điểm danh hành khách, vận hành chuyến xe |
| `sinhvien` | `Demo123!` | Hành khách (Passenger) | Khai báo nhu cầu đi lại, tra cứu lịch trình, đặt chỗ / hủy chỗ |

---

## 6. Danh mục chức năng chính của hệ thống

- **Đăng nhập & Phân quyền:** Xác thực tài khoản với mật khẩu băm an toàn, nhận diện vai trò và hiển thị giao diện phù hợp.
- **Quản lý nhu cầu đi lại:** Khai báo nhu cầu (thời gian, trạm đón/trả, hỗ trợ khuyết tật), xem trạng thái xử lý, tự động gom nhóm theo thời gian và tuyến đường.
- **Đề xuất & Mở tuyến thử nghiệm:** Thuật toán gom nhóm tự động đạt ngưỡng mở chuyến, điều phối viên phê duyệt, gán xe và tài xế.
- **Đặt chỗ & Hủy chỗ:** Đặt ghế nhanh chóng, kiểm tra sức chứa thời gian thực, xử lý quy tắc hủy sát giờ và bảo vệ quyền lợi chuyến.
- **Điều hành & Vận hành chuyến xe:** Điểm danh hành khách bằng QR/Mã vé, xác nhận giờ khởi hành thực tế, xử lý sự cố và ghi nhận vi phạm No-show.
- **Quản lý danh mục & Tham số:** Quản trị thông tin phương tiện (sức chứa, accessibility), tinh chỉnh các ngưỡng thời gian, ngưỡng gom nhóm.
- **Thống kê & Báo cáo:** Theo dõi tỷ lệ lấp đầy ghế, số lượt đặt/hủy, tần suất vi phạm theo mốc thời gian.

---

## 7. Tài liệu và Báo cáo

- File thiết kế gốc draw.io: [`diagrams/Tong_hop_cac_so_do.drawio`](./diagrams/Tong_hop_cac_so_do.drawio) (mở bằng Draw.io hoặc app diagrams.net).
- Thư mục ảnh sơ đồ trích xuất: [`diagrams/images/`](./diagrams/images/).
- Tài liệu hướng dẫn cài đặt chi tiết: [`docs/`](./docs/).
- Báo cáo đồ án chi tiết định dạng Word (.docx) đã nộp theo quy định của bộ môn.
