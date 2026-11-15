/* =====================================================================
   NỀN TẢNG ĐẶT CHỖ XE BUÝT THEO NHU CẦU - BUS ON DEMAND
   BẢN SQL TOÀN DIỆN & HOÀN CHỈNH (CHẠY 1 LẦN TỪ ĐẦU ĐẾN CUỐI)
   Hỗ trợ: SQL Server 2016 SP1+ / SQL Server 2017 / 2019 / 2022
   Bao gồm:
     1. Khởi tạo Database sạch & cơ chế Re-run an toàn (Clean Drop)
     2. 10 Bảng quan hệ (PK, FK, CHECK, UNIQUE, DEFAULT)
     3. Dữ liệu mẫu (Cấu hình, Danh mục Tuyến, Xe, Người dùng với mật khẩu mã hóa PBKDF2 chuẩn)
     4. 4 Views nghiệp vụ phục vụ tra cứu & báo cáo
     5. 6 Triggers bảo vệ toàn vẹn dữ liệu tự động (BR01 - BR11)
     6. 6 Stored Procedures bao bọc Transaction
     7. Truy vấn kiểm tra tổng hợp kết quả (Verification Query)
   ===================================================================== */

SET NOCOUNT ON;
GO

-- 1. TẠO DATABASE NẾU CHƯA CÓ
IF DB_ID(N'BusOnDemandDB_Test') IS NULL
BEGIN
    CREATE DATABASE BusOnDemandDB_Test;
END;
GO

USE BusOnDemandDB_Test;
GO

-- =====================================================================
-- 2. DỌN DẸP SẠCH CÁC ĐỐI TƯỢNG CŨ ĐỂ SCRIPT CHẠY LẠI KHÔNG BỊ LỖI
-- =====================================================================

-- Xóa Views nếu đã có
IF OBJECT_ID(N'dbo.v_DanhSachChuyenKhaDung', 'V') IS NOT NULL DROP VIEW dbo.v_DanhSachChuyenKhaDung;
IF OBJECT_ID(N'dbo.v_ThongKeNhuCauTheoKhungGio', 'V') IS NOT NULL DROP VIEW dbo.v_ThongKeNhuCauTheoKhungGio;
IF OBJECT_ID(N'dbo.v_LichSuDatChoHanhKhach', 'V') IS NOT NULL DROP VIEW dbo.v_LichSuDatChoHanhKhach;
IF OBJECT_ID(N'dbo.v_TheoDoiViPhamNoShow', 'V') IS NOT NULL DROP VIEW dbo.v_TheoDoiViPhamNoShow;
GO

-- Xóa Stored Procedures nếu đã có
IF OBJECT_ID(N'dbo.sp_DatCho', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_DatCho;
IF OBJECT_ID(N'dbo.sp_PhanCongXe', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_PhanCongXe;
IF OBJECT_ID(N'dbo.sp_HuyDatCho', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HuyDatCho;
IF OBJECT_ID(N'dbo.sp_GhiNhanDiemDanh', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GhiNhanDiemDanh;
IF OBJECT_ID(N'dbo.sp_KhoiHanhChuyen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_KhoiHanhChuyen;
IF OBJECT_ID(N'dbo.sp_HoanThanhChuyen', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_HoanThanhChuyen;
GO

-- Xóa Bảng theo thứ tự ngược của Khóa Ngoại (Foreign Key)
IF OBJECT_ID(N'dbo.VI_PHAM_NO_SHOW', 'U') IS NOT NULL DROP TABLE dbo.VI_PHAM_NO_SHOW;
IF OBJECT_ID(N'dbo.DIEM_DANH', 'U') IS NOT NULL DROP TABLE dbo.DIEM_DANH;
IF OBJECT_ID(N'dbo.DAT_CHO', 'U') IS NOT NULL DROP TABLE dbo.DAT_CHO;
IF OBJECT_ID(N'dbo.CHUYEN', 'U') IS NOT NULL DROP TABLE dbo.CHUYEN;
IF OBJECT_ID(N'dbo.NHU_CAU_DI_LAI', 'U') IS NOT NULL DROP TABLE dbo.NHU_CAU_DI_LAI;
IF OBJECT_ID(N'dbo.DE_XUAT_CHUYEN', 'U') IS NOT NULL DROP TABLE dbo.DE_XUAT_CHUYEN;
IF OBJECT_ID(N'dbo.TUYEN', 'U') IS NOT NULL DROP TABLE dbo.TUYEN;
IF OBJECT_ID(N'dbo.XE', 'U') IS NOT NULL DROP TABLE dbo.XE;
IF OBJECT_ID(N'dbo.NGUOI_DUNG', 'U') IS NOT NULL DROP TABLE dbo.NGUOI_DUNG;
IF OBJECT_ID(N'dbo.CAU_HINH_HE_THONG', 'U') IS NOT NULL DROP TABLE dbo.CAU_HINH_HE_THONG;
GO

-- =====================================================================
-- 3. ĐỊNH NGHĨA 10 BẢNG CƠ SỞ DỮ LIỆU
-- =====================================================================

/* Bảng 1: Cấu hình hệ thống */
CREATE TABLE dbo.CAU_HINH_HE_THONG
(
    TenThamSo NVARCHAR(100) NOT NULL CONSTRAINT PK_CAU_HINH_HE_THONG PRIMARY KEY,
    GiaTri    NVARCHAR(100) NOT NULL,
    MoTa      NVARCHAR(300) NULL
);
GO

/* Bảng 2: Người dùng hệ thống */
CREATE TABLE dbo.NGUOI_DUNG
(
    MaNguoiDung   NVARCHAR(20)  NOT NULL CONSTRAINT PK_NGUOI_DUNG PRIMARY KEY,
    HoTen         NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(255) NOT NULL,
    SoDienThoai   NVARCHAR(20)  NULL,
    TenDangNhap   NVARCHAR(50)  NOT NULL,
    MatKhauHash   NVARCHAR(255) NOT NULL,
    HanCheDenNgay DATETIME2(0)  NULL,
    VaiTro        NVARCHAR(20)  NOT NULL,
    CONSTRAINT UQ_NGUOI_DUNG_Email UNIQUE (Email),
    CONSTRAINT UQ_NGUOI_DUNG_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT CK_NGUOI_DUNG_VaiTro CHECK (VaiTro IN (N'HANH_KHACH', N'LAI_XE', N'DIEU_PHOI'))
);
GO

/* Bảng 3: Phương tiện xe buýt */
CREATE TABLE dbo.XE
(
    MaXe           NVARCHAR(20)  NOT NULL CONSTRAINT PK_XE PRIMARY KEY,
    BienSo         NVARCHAR(20)  NOT NULL,
    SucChua         INT           NOT NULL,
    CoAccessibility BIT           NOT NULL CONSTRAINT DF_XE_CoAccessibility DEFAULT (0),
    TrangThai       NVARCHAR(30)  NOT NULL CONSTRAINT DF_XE_TrangThai DEFAULT (N'Sẵn sàng'),
    CONSTRAINT UQ_XE_BienSo UNIQUE (BienSo),
    CONSTRAINT CK_XE_SucChua CHECK (SucChua > 0),
    CONSTRAINT CK_XE_TrangThai CHECK (TrangThai IN (N'Sẵn sàng', N'Đang hoạt động', N'Bảo trì', N'Ngưng sử dụng'))
);
GO

/* Bảng 4: Tuyến xe buýt */
CREATE TABLE dbo.TUYEN
(
    MaTuyen  NVARCHAR(20)  NOT NULL CONSTRAINT PK_TUYEN PRIMARY KEY,
    TenTuyen NVARCHAR(100) NOT NULL,
    HuongDi  NVARCHAR(30)  NOT NULL,
    MoTa     NVARCHAR(300) NULL,
    CONSTRAINT CK_TUYEN_HuongDi CHECK (HuongDi IN (N'Chiều đi', N'Chiều về'))
);
GO

/* Bảng 5: Đề xuất chuyến (kết quả gom nhóm nhu cầu) */
CREATE TABLE dbo.DE_XUAT_CHUYEN
(
    MaDeXuat       NVARCHAR(20)  NOT NULL CONSTRAINT PK_DE_XUAT_CHUYEN PRIMARY KEY,
    NgayDi         DATE          NOT NULL,
    HuongDi        NVARCHAR(30)  NOT NULL,
    KhungGio       NVARCHAR(20)  NOT NULL,
    SoCanHoTro     INT           NOT NULL CONSTRAINT DF_DE_XUAT_SoCanHoTro DEFAULT (0),
    CanhBaoThieuXe BIT           NOT NULL CONSTRAINT DF_DE_XUAT_CanhBaoThieuXe DEFAULT (0),
    TrangThai      NVARCHAR(30)  NOT NULL CONSTRAINT DF_DE_XUAT_TrangThai DEFAULT (N'Chờ duyệt'),
    NgayTao        DATETIME2(0)  NOT NULL CONSTRAINT DF_DE_XUAT_NgayTao DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_DE_XUAT_HuongDi CHECK (HuongDi IN (N'Chiều đi', N'Chiều về')),
    CONSTRAINT CK_DE_XUAT_SoCanHoTro CHECK (SoCanHoTro >= 0),
    CONSTRAINT CK_DE_XUAT_TrangThai CHECK (TrangThai IN (N'Chờ duyệt', N'Đã duyệt', N'Không đủ nhu cầu', N'Từ chối'))
);
GO

/* Bảng 6: Nhu cầu đi lại của hành khách */
CREATE TABLE dbo.NHU_CAU_DI_LAI
(
    MaNhuCau         NVARCHAR(20) NOT NULL CONSTRAINT PK_NHU_CAU_DI_LAI PRIMARY KEY,
    NgayDi           DATE         NOT NULL,
    HuongDi          NVARCHAR(30) NOT NULL,
    KhungGioMongMuon NVARCHAR(20) NOT NULL,
    CoAccessibility  BIT          NOT NULL CONSTRAINT DF_NHU_CAU_CoAccessibility DEFAULT (0),
    TrangThai        NVARCHAR(30) NOT NULL CONSTRAINT DF_NHU_CAU_TrangThai DEFAULT (N'Chưa gom nhóm'),
    ThoiDiemTao      DATETIME2(0) NOT NULL CONSTRAINT DF_NHU_CAU_ThoiDiemTao DEFAULT (SYSDATETIME()),
    MaHanhKhach      NVARCHAR(20) NOT NULL,
    MaDeXuat         NVARCHAR(20) NULL,
    CONSTRAINT FK_NHU_CAU_NGUOI_DUNG FOREIGN KEY (MaHanhKhach) REFERENCES dbo.NGUOI_DUNG(MaNguoiDung),
    CONSTRAINT FK_NHU_CAU_DE_XUAT FOREIGN KEY (MaDeXuat) REFERENCES dbo.DE_XUAT_CHUYEN(MaDeXuat),
    CONSTRAINT CK_NHU_CAU_HuongDi CHECK (HuongDi IN (N'Chiều đi', N'Chiều về')),
    CONSTRAINT CK_NHU_CAU_TrangThai CHECK (TrangThai IN (N'Chưa gom nhóm', N'Đã gom nhóm', N'Không đủ nhóm', N'Đã hủy'))
);
GO

/* Bảng 7: Chuyến xe vận hành */
CREATE TABLE dbo.CHUYEN
(
    MaChuyen             NVARCHAR(20) NOT NULL CONSTRAINT PK_CHUYEN PRIMARY KEY,
    GioDuKien             DATETIME2(0) NOT NULL,
    GioKhoiHanhThucTe     DATETIME2(0) NULL,
    GioHoanThanhThucTe    DATETIME2(0) NULL,
    TrangThai             NVARCHAR(30) NOT NULL CONSTRAINT DF_CHUYEN_TrangThai DEFAULT (N'Dự kiến'),
    MaTuyen               NVARCHAR(20) NOT NULL,
    MaXe                  NVARCHAR(20) NOT NULL,
    MaLaiXe               NVARCHAR(20) NOT NULL,
    MaDeXuat              NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_CHUYEN_TUYEN FOREIGN KEY (MaTuyen) REFERENCES dbo.TUYEN(MaTuyen),
    CONSTRAINT FK_CHUYEN_XE FOREIGN KEY (MaXe) REFERENCES dbo.XE(MaXe),
    CONSTRAINT FK_CHUYEN_LAI_XE FOREIGN KEY (MaLaiXe) REFERENCES dbo.NGUOI_DUNG(MaNguoiDung),
    CONSTRAINT FK_CHUYEN_DE_XUAT FOREIGN KEY (MaDeXuat) REFERENCES dbo.DE_XUAT_CHUYEN(MaDeXuat),
    CONSTRAINT CK_CHUYEN_TrangThaiVaThoiGian CHECK
    (
        (TrangThai IN (N'Dự kiến', N'Đã hủy') AND GioKhoiHanhThucTe IS NULL AND GioHoanThanhThucTe IS NULL)
        OR
        (TrangThai = N'Đã khởi hành' AND GioKhoiHanhThucTe IS NOT NULL AND GioHoanThanhThucTe IS NULL)
        OR
        (TrangThai = N'Đã hoàn thành' AND GioKhoiHanhThucTe IS NOT NULL AND GioHoanThanhThucTe IS NOT NULL
            AND GioHoanThanhThucTe >= GioKhoiHanhThucTe)
    )
);
GO

/* Bảng 8: Đặt chỗ của hành khách */
CREATE TABLE dbo.DAT_CHO
(
    MaDatCho     NVARCHAR(20) NOT NULL CONSTRAINT PK_DAT_CHO PRIMARY KEY,
    ThoiDiemDat  DATETIME2(0) NOT NULL CONSTRAINT DF_DAT_CHO_ThoiDiemDat DEFAULT (SYSDATETIME()),
    TrangThai    NVARCHAR(30) NOT NULL CONSTRAINT DF_DAT_CHO_TrangThai DEFAULT (N'Đã giữ'),
    MaChuyen     NVARCHAR(20) NOT NULL,
    MaHanhKhach  NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_DAT_CHO_CHUYEN FOREIGN KEY (MaChuyen) REFERENCES dbo.CHUYEN(MaChuyen),
    CONSTRAINT FK_DAT_CHO_NGUOI_DUNG FOREIGN KEY (MaHanhKhach) REFERENCES dbo.NGUOI_DUNG(MaNguoiDung),
    CONSTRAINT UQ_DAT_CHO_MaDatCho_MaHanhKhach UNIQUE (MaDatCho, MaHanhKhach),
    CONSTRAINT CK_DAT_CHO_TrangThai CHECK (TrangThai IN (N'Đã giữ', N'Đã hủy', N'Đã sử dụng', N'Vắng mặt'))
);
GO

/* Chỉ 1 chỗ đang giữ cho mỗi hành khách trên cùng 1 chuyến */
CREATE UNIQUE INDEX UX_DAT_CHO_MotHanhKhachMotChuyenDangGiu
ON dbo.DAT_CHO(MaChuyen, MaHanhKhach)
WHERE TrangThai = N'Đã giữ';
GO

/* Bảng 9: Điểm danh hành khách */
CREATE TABLE dbo.DIEM_DANH
(
    MaDiemDanh  NVARCHAR(20)  NOT NULL CONSTRAINT PK_DIEM_DANH PRIMARY KEY,
    ThoiDiem    DATETIME2(0)  NOT NULL CONSTRAINT DF_DIEM_DANH_ThoiDiem DEFAULT (SYSDATETIME()),
    CoMat       BIT           NOT NULL,
    GhiChu      NVARCHAR(300) NULL,
    MaDatCho    NVARCHAR(20)  NOT NULL,
    CONSTRAINT FK_DIEM_DANH_DAT_CHO FOREIGN KEY (MaDatCho) REFERENCES dbo.DAT_CHO(MaDatCho),
    CONSTRAINT UQ_DIEM_DANH_MaDatCho UNIQUE (MaDatCho)
);
GO

/* Bảng 10: Vi phạm No-show & Hủy muộn */
CREATE TABLE dbo.VI_PHAM_NO_SHOW
(
    MaViPham     NVARCHAR(20) NOT NULL CONSTRAINT PK_VI_PHAM_NO_SHOW PRIMARY KEY,
    Loai         NVARCHAR(50) NOT NULL,
    NgayGhiNhan  DATETIME2(0) NOT NULL CONSTRAINT DF_VI_PHAM_NgayGhiNhan DEFAULT (SYSDATETIME()),
    MaHanhKhach  NVARCHAR(20) NOT NULL,
    MaDatCho     NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_VI_PHAM_NGUOI_DUNG FOREIGN KEY (MaHanhKhach) REFERENCES dbo.NGUOI_DUNG(MaNguoiDung),
    CONSTRAINT FK_VI_PHAM_DAT_CHO_HANH_KHACH FOREIGN KEY (MaDatCho, MaHanhKhach)
        REFERENCES dbo.DAT_CHO(MaDatCho, MaHanhKhach),
    CONSTRAINT UQ_VI_PHAM_MaDatCho UNIQUE (MaDatCho),
    CONSTRAINT CK_VI_PHAM_Loai CHECK (Loai IN (N'Vắng mặt khi khởi hành', N'Hủy muộn'))
);
GO

-- =====================================================================
-- 4. DỮ LIỆU CẤU HÌNH & DỮ LIỆU MẪU ĐẦY ĐỦ ĐỂ DEMO MỌI CHỨC NĂNG
-- =====================================================================

-- 4.1 Cấu hình hệ thống (7 tham số nghiệp vụ)
INSERT INTO dbo.CAU_HINH_HE_THONG (TenThamSo, GiaTri, MoTa)
VALUES
(N'NGUONG_GOM_NHOM', N'4', N'Số nhu cầu tối thiểu để gom nhóm thành đề xuất'),
(N'DO_RONG_KHUNG_GIO_PHUT', N'30', N'Độ rộng khung giờ gom nhu cầu (phút)'),
(N'HAN_HUY_GIO', N'2', N'Hủy trước giờ khởi hành từ 2 giờ trở lên thì không tính vi phạm hủy muộn'),
(N'NGUONG_NO_SHOW', N'3', N'Số lần vi phạm trong 30 ngày để bắt đầu hạn chế đặt chỗ'),
(N'SO_NGAY_HAN_CHE', N'14', N'Số ngày hạn chế đặt chỗ khi đạt ngưỡng vi phạm'),
(N'CHU_KY_JOB_PHUT', N'15', N'Chu kỳ quét tiến trình gom nhóm định kỳ (phút)'),
(N'SO_NGAY_DONG_NHOM', N'3', N'Số ngày tự động đóng nhóm nếu chưa đủ nhu cầu');
GO

-- 4.2 Danh mục Tuyến xe (4 tuyến cả 2 chiều)
INSERT INTO dbo.TUYEN (MaTuyen, TenTuyen, HuongDi, MoTa)
VALUES
(N'T01', N'Tuyến KTX ĐHQG - Cơ sở 1', N'Chiều đi', N'Tuyến phục vụ sinh viên chiều đi học buổi sáng từ KTX đến trường'),
(N'T02', N'Tuyến Cơ sở 1 - KTX ĐHQG', N'Chiều về', N'Tuyến phục vụ sinh viên chiều về KTX buổi chiều từ trường về KTX'),
(N'T03', N'Tuyến KTX Khu B - Khu Công nghệ cao', N'Chiều đi', N'Tuyến phục vụ sinh viên đi thực tập sáng'),
(N'T04', N'Tuyến Khu Công nghệ cao - KTX Khu B', N'Chiều về', N'Tuyến phục vụ sinh viên thực tập về chiều');
GO

-- 4.3 Danh mục Xe buýt (6 xe nhiều phân khúc: 16, 29, 45 chỗ, có/không bệ nâng, sẵn sàng, bảo trì)
INSERT INTO dbo.XE (MaXe, BienSo, SucChua, CoAccessibility, TrangThai)
VALUES
(N'XE01', N'51B-123.45', 16, 0, N'Sẵn sàng'),
(N'XE02', N'51B-678.90', 29, 1, N'Sẵn sàng'),
(N'XE03', N'51B-999.88', 45, 1, N'Sẵn sàng'),
(N'XE04', N'51B-444.55', 16, 0, N'Sẵn sàng'),
(N'XE05', N'51B-888.77', 29, 1, N'Bảo trì'),     -- Dùng để demo cố tình gán xe hỏng sẽ bị Trigger chặn!
(N'XE06', N'51B-222.33', 45, 0, N'Sẵn sàng');
GO

-- 4.4 Danh mục Người dùng (Mật khẩu dùng chung cho tất cả là: Demo123!)
INSERT INTO dbo.NGUOI_DUNG (MaNguoiDung, HoTen, Email, SoDienThoai, TenDangNhap, MatKhauHash, HanCheDenNgay, VaiTro)
VALUES
(N'ND01', N'Nguyễn Văn A (Hành khách)',   N'nd01@example.com', N'0900000001', N'nd01', N'PBKDF2$120000$BVh4W+Mw3DjvlOv54dCApw==$/l4OnD5zi5IqCpOZASooTPt6bEzudcAI5zqGrfI2I88=', NULL, N'HANH_KHACH'),
(N'ND02', N'Trần Văn B (Lái xe 1)',       N'nd02@example.com', N'0900000002', N'nd02', N'PBKDF2$120000$d9YIB4eO0dPFkKN62y0iFw==$FSN+mmS++wJVlabbLMgSrfhO/6P/ut1TCDFj3R7QIM8=', NULL, N'LAI_XE'),
(N'ND03', N'Lê Thị C (Điều phối viên)',   N'nd03@example.com', N'0900000003', N'nd03', N'PBKDF2$120000$OND1ryrRUxl54QnvNxQ5sg==$ShlJ6ad+s1+0FIH+8Mn6UXlC6n8l4lJB8MKwwqfBRTI=', NULL, N'DIEU_PHOI'),
(N'ND04', N'Phạm Minh D (Hành khách 2)',  N'nd04@example.com', N'0900000004', N'nd04', N'PBKDF2$120000$BVh4W+Mw3DjvlOv54dCApw==$/l4OnD5zi5IqCpOZASooTPt6bEzudcAI5zqGrfI2I88=', NULL, N'HANH_KHACH'),
(N'ND05', N'Lê Hoàng E (Hành khách 3)',   N'nd05@example.com', N'0900000005', N'nd05', N'PBKDF2$120000$BVh4W+Mw3DjvlOv54dCApw==$/l4OnD5zi5IqCpOZASooTPt6bEzudcAI5zqGrfI2I88=', NULL, N'HANH_KHACH'),
(N'ND06', N'Ngô Quỳnh F (Khách xe lăn)',  N'nd06@example.com', N'0900000006', N'nd06', N'PBKDF2$120000$BVh4W+Mw3DjvlOv54dCApw==$/l4OnD5zi5IqCpOZASooTPt6bEzudcAI5zqGrfI2I88=', NULL, N'HANH_KHACH'),
(N'ND07', N'Đặng Quốc G (Lái xe 2)',      N'nd07@example.com', N'0900000007', N'nd07', N'PBKDF2$120000$d9YIB4eO0dPFkKN62y0iFw==$FSN+mmS++wJVlabbLMgSrfhO/6P/ut1TCDFj3R7QIM8=', NULL, N'LAI_XE');
GO

-- 4.5 Đề xuất chuyến (Đủ các trạng thái: Đã duyệt mở chuyến, Chờ duyệt để điều phối, Đã xong)
INSERT INTO dbo.DE_XUAT_CHUYEN (MaDeXuat, NgayDi, HuongDi, KhungGio, SoCanHoTro, CanhBaoThieuXe, TrangThai, NgayTao)
VALUES
(N'DX01', CAST(GETDATE() AS DATE), N'Chiều đi', N'20:00', 1, 0, N'Đã duyệt', DATEADD(HOUR, -10, SYSDATETIME())),
(N'DX02', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều đi', N'07:30', 0, 0, N'Đã duyệt', DATEADD(HOUR, -5, SYSDATETIME())),
(N'DX03', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 1, 0, N'Chờ duyệt', DATEADD(HOUR, -2, SYSDATETIME())), -- Để Điều phối vào FrmDeXuatTuyen duyệt!
(N'DX04', DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), N'Chiều đi', N'07:30', 0, 0, N'Đã duyệt', DATEADD(DAY, -3, SYSDATETIME())),  -- Quá khứ cho FrmThongKe
(N'DX05', DATEADD(DAY, -4, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 1, 0, N'Đã duyệt', DATEADD(DAY, -5, SYSDATETIME()));  -- Quá khứ cho FrmThongKe
GO

-- 4.6 Nhu cầu đi lại (Đủ các trạng thái: Chưa gom nhóm để thử hủy, Đã gom nhóm, Khuyết tật)
INSERT INTO dbo.NHU_CAU_DI_LAI (MaNhuCau, NgayDi, HuongDi, KhungGioMongMuon, CoAccessibility, TrangThai, ThoiDiemTao, MaHanhKhach, MaDeXuat)
VALUES
-- Nhu cầu thuộc DX01 (Chuyến C01 tối nay)
(N'NC01', CAST(GETDATE() AS DATE), N'Chiều đi', N'20:00', 1, N'Đã gom nhóm', DATEADD(HOUR, -12, SYSDATETIME()), N'ND01', N'DX01'),
(N'NC02', CAST(GETDATE() AS DATE), N'Chiều đi', N'20:00', 0, N'Đã gom nhóm', DATEADD(HOUR, -11, SYSDATETIME()), N'ND04', N'DX01'),
(N'NC03', CAST(GETDATE() AS DATE), N'Chiều đi', N'20:00', 0, N'Đã gom nhóm', DATEADD(HOUR, -10, SYSDATETIME()), N'ND05', N'DX01'),

-- Nhu cầu thuộc DX02 (Chuyến C02 sáng mai - ND01 có nhu cầu nhưng CHƯA đặt vé để vào FrmDatGhe bấm Đặt chỗ!)
(N'NC04', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều đi', N'07:30', 0, N'Đã gom nhóm', DATEADD(HOUR, -6, SYSDATETIME()), N'ND01', N'DX02'),
(N'NC05', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều đi', N'07:30', 0, N'Đã gom nhóm', DATEADD(HOUR, -5, SYSDATETIME()), N'ND04', N'DX02'),

-- Nhu cầu thuộc DX03 (Đang Chờ duyệt gom nhóm 4 người)
(N'NC06', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 1, N'Đã gom nhóm', DATEADD(HOUR, -3, SYSDATETIME()), N'ND06', N'DX03'),
(N'NC07', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 0, N'Đã gom nhóm', DATEADD(HOUR, -3, SYSDATETIME()), N'ND01', N'DX03'),
(N'NC08', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 0, N'Đã gom nhóm', DATEADD(HOUR, -2, SYSDATETIME()), N'ND04', N'DX03'),
(N'NC09', DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 0, N'Đã gom nhóm', DATEADD(HOUR, -2, SYSDATETIME()), N'ND05', N'DX03'),

-- Nhu cầu CHƯA GOM NHÓM (Để ND01 vào FrmNhuCau bấm thử nút "HỦY NHU CẦU")
(N'NC10', DATEADD(DAY, 3, CAST(GETDATE() AS DATE)), N'Chiều đi', N'08:00', 0, N'Chưa gom nhóm', DATEADD(HOUR, -1, SYSDATETIME()), N'ND01', NULL),

-- Nhu cầu quá khứ (Phục vụ thống kê KPI)
(N'NC11', DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), N'Chiều đi', N'07:30', 0, N'Đã gom nhóm', DATEADD(DAY, -3, SYSDATETIME()), N'ND01', N'DX04'),
(N'NC12', DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), N'Chiều đi', N'07:30', 0, N'Đã gom nhóm', DATEADD(DAY, -3, SYSDATETIME()), N'ND04', N'DX04'),
(N'NC13', DATEADD(DAY, -4, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 1, N'Đã gom nhóm', DATEADD(DAY, -5, SYSDATETIME()), N'ND06', N'DX05'),
(N'NC14', DATEADD(DAY, -4, CAST(GETDATE() AS DATE)), N'Chiều về', N'17:30', 0, N'Đã gom nhóm', DATEADD(DAY, -5, SYSDATETIME()), N'ND05', N'DX05');
GO

-- 4.7 Chuyến xe (Đủ các giai đoạn: Dự kiến chưa chạy, Chuyến ngày mai, Chuyến đã hoàn thành)
INSERT INTO dbo.CHUYEN (MaChuyen, GioDuKien, GioKhoiHanhThucTe, GioHoanThanhThucTe, TrangThai, MaTuyen, MaXe, MaLaiXe, MaDeXuat)
VALUES
-- Chuyến C01: Tối nay, Dự kiến, Lái xe ND02 (Để ND02 vào FrmVanHanh điểm danh, bấm Khởi hành, Hoàn thành!)
(N'C01', DATEADD(HOUR, 22, CAST(CAST(GETDATE() AS DATE) AS DATETIME2(0))), NULL, NULL, N'Dự kiến', N'T01', N'XE02', N'ND02', N'DX01'),

-- Chuyến C02: Sáng mai, Dự kiến, Lái xe ND07 (Để ND01 vào FrmDatGhe thấy còn chỗ và bấm "ĐẶT CHỖ"!)
(N'C02', DATEADD(HOUR, 31, CAST(CAST(GETDATE() AS DATE) AS DATETIME2(0))), NULL, NULL, N'Dự kiến', N'T01', N'XE03', N'ND07', N'DX02'),

-- Chuyến C03 & C04: Quá khứ, Đã hoàn thành (Để FrmThongKe hiển thị tỷ lệ lấp đầy và sản lượng vận chuyển)
(N'C03', DATEADD(HOUR, 7, CAST(DATEADD(DAY, -2, CAST(GETDATE() AS DATE)) AS DATETIME2(0))), 
        DATEADD(MINUTE, 7 * 60 + 5, CAST(DATEADD(DAY, -2, CAST(GETDATE() AS DATE)) AS DATETIME2(0))), 
        DATEADD(MINUTE, 7 * 60 + 45, CAST(DATEADD(DAY, -2, CAST(GETDATE() AS DATE)) AS DATETIME2(0))), 
        N'Đã hoàn thành', N'T01', N'XE01', N'ND02', N'DX04'),
(N'C04', DATEADD(HOUR, 17, CAST(DATEADD(DAY, -4, CAST(GETDATE() AS DATE)) AS DATETIME2(0))), 
        DATEADD(MINUTE, 17 * 60 + 2, CAST(DATEADD(DAY, -4, CAST(GETDATE() AS DATE)) AS DATETIME2(0))), 
        DATEADD(MINUTE, 17 * 60 + 50, CAST(DATEADD(DAY, -4, CAST(GETDATE() AS DATE)) AS DATETIME2(0))), 
        N'Đã hoàn thành', N'T02', N'XE02', N'ND02', N'DX05');
GO

-- 4.8 Đặt chỗ (Có vé đang giữ để ND01 thử hủy vé, có vé cho bác tài điểm danh, có vé lịch sử)
INSERT INTO dbo.DAT_CHO (MaDatCho, ThoiDiemDat, TrangThai, MaChuyen, MaHanhKhach)
VALUES
-- Vé trên chuyến C01 (Để Lái xe ND02 vào điểm danh: ND01 có mặt, ND04 có mặt, ND05 vắng mặt)
-- Và để ND01 vào FrmDatGhe thấy vé DC01 của mình có thể bấm "HỦY ĐẶT GHẾ"
(N'DC01', DATEADD(HOUR, -8, SYSDATETIME()), N'Đã giữ', N'C01', N'ND01'),
(N'DC02', DATEADD(HOUR, -7, SYSDATETIME()), N'Đã giữ', N'C01', N'ND04'),
(N'DC03', DATEADD(HOUR, -6, SYSDATETIME()), N'Đã giữ', N'C01', N'ND05'),

-- Vé quá khứ trên chuyến C03 (Đã sử dụng)
(N'DC04', DATEADD(DAY, -3, SYSDATETIME()), N'Đã sử dụng', N'C03', N'ND01'),
(N'DC05', DATEADD(DAY, -3, SYSDATETIME()), N'Đã sử dụng', N'C03', N'ND04'),

-- Vé quá khứ trên chuyến C04 (1 vé Đã dùng, 1 vé Vắng mặt No-show)
(N'DC06', DATEADD(DAY, -5, SYSDATETIME()), N'Đã sử dụng', N'C04', N'ND06'),
(N'DC07', DATEADD(DAY, -5, SYSDATETIME()), N'Vắng mặt', N'C04', N'ND05');
GO

-- 4.9 Điểm danh quá khứ
INSERT INTO dbo.DIEM_DANH (MaDiemDanh, ThoiDiem, CoMat, GhiChu, MaDatCho)
VALUES
(N'DD04', DATEADD(DAY, -2, SYSDATETIME()), 1, N'Có mặt đúng giờ', N'DC04'),
(N'DD05', DATEADD(DAY, -2, SYSDATETIME()), 1, N'Có mặt đúng giờ', N'DC05'),
(N'DD06', DATEADD(DAY, -4, SYSDATETIME()), 1, N'Hỗ trợ xe lăn lên bệ', N'DC06'),
(N'DD07', DATEADD(DAY, -4, SYSDATETIME()), 0, N'Hành khách không đến bến', N'DC07');
GO

-- 4.10 Vi phạm No-show mẫu trong quá khứ (Phục vụ hiển thị FrmThongKe và View 4)
INSERT INTO dbo.VI_PHAM_NO_SHOW (MaViPham, Loai, NgayGhiNhan, MaHanhKhach, MaDatCho)
VALUES
(N'VP01', N'Vắng mặt khi khởi hành', DATEADD(DAY, -4, SYSDATETIME()), N'ND05', N'DC07');
GO

-- =====================================================================
-- 5. ĐỊNH NGHĨA 4 VIEWS NGHIỆP VỤ & BÁO CÁO (TRẢ LỜI CÂU HỎI CỦA THẦY)
-- =====================================================================

/* View 1: Danh sách các chuyến khả dụng để đặt chỗ (hiển thị số chỗ trống) */
CREATE OR ALTER VIEW dbo.v_DanhSachChuyenKhaDung
AS
SELECT 
    c.MaChuyen,
    t.TenTuyen,
    t.HuongDi,
    c.GioDuKien,
    c.TrangThai AS TrangThaiChuyen,
    x.BienSo,
    x.SucChua,
    x.CoAccessibility,
    nl.HoTen AS TenLaiXe,
    ISNULL(dc.SoChoDaDat, 0) AS SoChoDaDat,
    (x.SucChua - ISNULL(dc.SoChoDaDat, 0)) AS SoChoConTrong
FROM dbo.CHUYEN c
INNER JOIN dbo.TUYEN t ON t.MaTuyen = c.MaTuyen
INNER JOIN dbo.XE x ON x.MaXe = c.MaXe
INNER JOIN dbo.NGUOI_DUNG nl ON nl.MaNguoiDung = c.MaLaiXe
LEFT JOIN 
(
    SELECT MaChuyen, COUNT(*) AS SoChoDaDat
    FROM dbo.DAT_CHO
    WHERE TrangThai = N'Đã giữ'
    GROUP BY MaChuyen
) dc ON dc.MaChuyen = c.MaChuyen;
GO

/* View 2: Thống kê nhu cầu đi lại theo ngày, hướng và khung giờ (hỗ trợ điều phối) */
CREATE OR ALTER VIEW dbo.v_ThongKeNhuCauTheoKhungGio
AS
SELECT 
    nc.NgayDi,
    nc.HuongDi,
    nc.KhungGioMongMuon AS KhungGio,
    COUNT(*) AS TongSoNhuCau,
    SUM(CASE WHEN nc.CoAccessibility = 1 THEN 1 ELSE 0 END) AS SoNhuCauCanAccessibility,
    SUM(CASE WHEN nc.TrangThai = N'Chưa gom nhóm' THEN 1 ELSE 0 END) AS SoChuaGomNhom,
    SUM(CASE WHEN nc.TrangThai = N'Đã gom nhóm' THEN 1 ELSE 0 END) AS SoDaGomNhom,
    SUM(CASE WHEN nc.TrangThai = N'Đã hủy' THEN 1 ELSE 0 END) AS SoDaHuy
FROM dbo.NHU_CAU_DI_LAI nc
GROUP BY nc.NgayDi, nc.HuongDi, nc.KhungGioMongMuon;
GO

/* View 3: Lịch sử đặt chỗ chi tiết của từng hành khách kèm trạng thái điểm danh */
CREATE OR ALTER VIEW dbo.v_LichSuDatChoHanhKhach
AS
SELECT 
    d.MaDatCho,
    nd.MaNguoiDung AS MaHanhKhach,
    nd.HoTen AS TenHanhKhach,
    nd.SoDienThoai,
    c.MaChuyen,
    t.TenTuyen,
    t.HuongDi,
    c.GioDuKien,
    d.ThoiDiemDat,
    d.TrangThai AS TrangThaiDatCho,
    CASE 
        WHEN dd.MaDiemDanh IS NULL THEN N'Chưa điểm danh'
        WHEN dd.CoMat = 1 THEN N'Có mặt'
        ELSE N'Vắng mặt'
    END AS KetQuaDiemDanh,
    dd.ThoiDiem AS ThoiDiemDiemDanh
FROM dbo.DAT_CHO d
INNER JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung = d.MaHanhKhach
INNER JOIN dbo.CHUYEN c ON c.MaChuyen = d.MaChuyen
INNER JOIN dbo.TUYEN t ON t.MaTuyen = c.MaTuyen
LEFT JOIN dbo.DIEM_DANH dd ON dd.MaDatCho = d.MaDatCho;
GO

/* View 4: Theo dõi vi phạm hủy muộn / no-show và thời hạn chế đặt chỗ */
CREATE OR ALTER VIEW dbo.v_TheoDoiViPhamNoShow
AS
SELECT 
    nd.MaNguoiDung AS MaHanhKhach,
    nd.HoTen AS TenHanhKhach,
    nd.Email,
    nd.SoDienThoai,
    COUNT(vp.MaViPham) AS TongSoViPham,
    SUM(CASE WHEN vp.Loai = N'Hủy muộn' THEN 1 ELSE 0 END) AS SoLanHuyMuon,
    SUM(CASE WHEN vp.Loai = N'Vắng mặt khi khởi hành' THEN 1 ELSE 0 END) AS SoLanVangMat,
    nd.HanCheDenNgay,
    CASE 
        WHEN nd.HanCheDenNgay IS NOT NULL AND nd.HanCheDenNgay > SYSDATETIME() THEN N'Đang bị hạn chế đặt chỗ'
        ELSE N'Bình thường'
    END AS TinhTrangTaiKhoan
FROM dbo.NGUOI_DUNG nd
LEFT JOIN dbo.VI_PHAM_NO_SHOW vp ON vp.MaHanhKhach = nd.MaNguoiDung
WHERE nd.VaiTro = N'HANH_KHACH'
GROUP BY nd.MaNguoiDung, nd.HoTen, nd.Email, nd.SoDienThoai, nd.HanCheDenNgay;
GO

-- =====================================================================
-- 6. ĐỊNH NGHĨA 6 TRIGGERS BẢO VỆ TOÀN VẸN DỮ LIỆU TỰ ĐỘNG
-- =====================================================================

/* TRIGGER 1: Tự động cập nhật tổng số nhu cầu cần hỗ trợ tiếp cận theo Đề xuất */
CREATE OR ALTER TRIGGER dbo.TR_NHU_CAU_CAP_NHAT_SO_CAN_HO_TRO
ON dbo.NHU_CAU_DI_LAI
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH CacDeXuatThayDoi AS
    (
        SELECT MaDeXuat FROM inserted WHERE MaDeXuat IS NOT NULL
        UNION
        SELECT MaDeXuat FROM deleted WHERE MaDeXuat IS NOT NULL
    )
    UPDATE dx
       SET SoCanHoTro = q.SoLuong
    FROM dbo.DE_XUAT_CHUYEN AS dx
    INNER JOIN CacDeXuatThayDoi AS td ON td.MaDeXuat = dx.MaDeXuat
    CROSS APPLY
    (
        SELECT COUNT(*) AS SoLuong
        FROM dbo.NHU_CAU_DI_LAI AS nc
        WHERE nc.MaDeXuat = dx.MaDeXuat
          AND nc.CoAccessibility = 1
          AND nc.TrangThai <> N'Đã hủy'
    ) AS q;
END;
GO

/* TRIGGER 2: Kiểm tra tính hợp lệ khi phân công xe, lái xe, hướng đi và sức chứa */
CREATE OR ALTER TRIGGER dbo.TR_CHUYEN_KIEM_TRA_PHAN_CONG
ON dbo.CHUYEN
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra 1: Người được phân công phải có vai trò LAI_XE
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung = i.MaLaiXe
        WHERE nd.VaiTro <> N'LAI_XE'
    )
        THROW 51001, N'Mã lái xe phải tham chiếu đến người dùng có vai trò LAI_XE.', 1;

    -- Kiểm tra 2: Không phân công xe đang bảo trì hoặc ngưng sử dụng
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.XE x ON x.MaXe = i.MaXe
        WHERE x.TrangThai IN (N'Bảo trì', N'Ngưng sử dụng')
    )
        THROW 51002, N'Không thể phân công xe đang bảo trì hoặc ngưng sử dụng.', 1;

    -- Kiểm tra 3: Hướng của tuyến phải trùng hướng của đề xuất chuyến
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.TUYEN t ON t.MaTuyen = i.MaTuyen
        INNER JOIN dbo.DE_XUAT_CHUYEN dx ON dx.MaDeXuat = i.MaDeXuat
        WHERE t.HuongDi <> dx.HuongDi
    )
        THROW 51003, N'Hướng của tuyến phải trùng với hướng của đề xuất chuyến.', 1;

    -- Kiểm tra 4: Nhóm có nhu cầu khuyết tật/hỗ trợ tiếp cận thì xe bắt buộc phải có CoAccessibility=1
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.XE x ON x.MaXe = i.MaXe
        WHERE x.CoAccessibility = 0
          AND EXISTS
          (
              SELECT 1
              FROM dbo.NHU_CAU_DI_LAI nc
              WHERE nc.MaDeXuat = i.MaDeXuat
                AND nc.CoAccessibility = 1
                AND nc.TrangThai <> N'Đã hủy'
          )
    )
        THROW 51004, N'Nhóm có hành khách cần hỗ trợ tiếp cận, bắt buộc phải phân công xe có thiết bị hỗ trợ.', 1;

    -- Kiểm tra 5: Sức chứa của xe không được nhỏ hơn số chỗ đang được giữ
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.XE x ON x.MaXe = i.MaXe
        CROSS APPLY
        (
            SELECT COUNT(*) AS SoChoDangGiu
            FROM dbo.DAT_CHO d WITH (UPDLOCK, HOLDLOCK)
            WHERE d.MaChuyen = i.MaChuyen
              AND d.TrangThai = N'Đã giữ'
        ) AS dc
        WHERE dc.SoChoDangGiu > x.SucChua
    )
        THROW 51005, N'Không thể phân công xe có sức chứa nhỏ hơn số chỗ đang được giữ.', 1;
END;
GO

/* TRIGGER 3: Kiểm soát quy trình đặt chỗ & hủy chỗ hợp lệ */
CREATE OR ALTER TRIGGER dbo.TR_DAT_CHO_KIEM_TRA
ON dbo.DAT_CHO
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ThoiDiemHienTai DATETIME2(0) = SYSDATETIME();

    -- Không cho hủy đặt chỗ khi chuyến đã khởi hành hoặc đã qua giờ dự kiến
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN deleted d ON d.MaDatCho = i.MaDatCho
        INNER JOIN dbo.CHUYEN c ON c.MaChuyen = i.MaChuyen
        WHERE i.TrangThai = N'Đã hủy'
          AND d.TrangThai = N'Đã giữ'
          AND (c.TrangThai <> N'Dự kiến' OR @ThoiDiemHienTai >= c.GioDuKien)
    )
        THROW 51006, N'Không thể hủy đặt chỗ sau khi chuyến đã khởi hành hoặc đã qua giờ dự kiến.', 1;

    -- Chỉ cho giữ chỗ trên chuyến đang Dự kiến và chưa đến giờ khởi hành
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.CHUYEN c ON c.MaChuyen = i.MaChuyen
        WHERE i.TrangThai = N'Đã giữ'
          AND (c.TrangThai <> N'Dự kiến' OR c.GioDuKien <= @ThoiDiemHienTai)
    )
        THROW 51007, N'Chỉ được giữ chỗ trên chuyến đang ở trạng thái Dự kiến và chưa đến giờ khởi hành.', 1;

    -- Người đặt chỗ phải là hành khách và không bị hạn chế đặt chỗ
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung = i.MaHanhKhach
        WHERE i.TrangThai = N'Đã giữ'
          AND (nd.VaiTro <> N'HANH_KHACH'
               OR (nd.HanCheDenNgay IS NOT NULL AND nd.HanCheDenNgay > @ThoiDiemHienTai))
    )
        THROW 51008, N'Người đặt chỗ phải có vai trò hành khách và không trong thời gian bị hạn chế.', 1;

    -- Không vượt quá sức chứa xe
    IF EXISTS
    (
        SELECT 1
        FROM (SELECT DISTINCT MaChuyen FROM inserted) AS i
        INNER JOIN dbo.CHUYEN c WITH (UPDLOCK, HOLDLOCK) ON c.MaChuyen = i.MaChuyen
        INNER JOIN dbo.XE x ON x.MaXe = c.MaXe
        CROSS APPLY
        (
            SELECT COUNT(*) AS SoChoDangGiu
            FROM dbo.DAT_CHO d WITH (UPDLOCK, HOLDLOCK)
            WHERE d.MaChuyen = c.MaChuyen
              AND d.TrangThai = N'Đã giữ'
        ) AS dc
        WHERE dc.SoChoDangGiu > x.SucChua
    )
        THROW 51009, N'Vượt quá sức chứa của xe; thao tác đặt chỗ bị từ chối.', 1;
END;
GO

/* TRIGGER 4: Tự động ghi nhận vi phạm Hủy muộn khi hủy dưới ngưỡng giờ cấu hình */
CREATE OR ALTER TRIGGER dbo.TR_DAT_CHO_GHI_NHAN_HUY_MUON
ON dbo.DAT_CHO
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2(0) = SYSDATETIME();
    DECLARE @HanHuyGio INT = 2;

    SELECT @HanHuyGio = TRY_CONVERT(INT, GiaTri)
    FROM dbo.CAU_HINH_HE_THONG
    WHERE TenThamSo = N'HAN_HUY_GIO';
    SET @HanHuyGio = COALESCE(@HanHuyGio, 2);

    INSERT INTO dbo.VI_PHAM_NO_SHOW (MaViPham, Loai, NgayGhiNhan, MaHanhKhach, MaDatCho)
    SELECT
        N'VP' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18),
        N'Hủy muộn', @Now, i.MaHanhKhach, i.MaDatCho
    FROM inserted i
    INNER JOIN deleted d ON d.MaDatCho = i.MaDatCho
    INNER JOIN dbo.CHUYEN c ON c.MaChuyen = i.MaChuyen
    WHERE i.TrangThai = N'Đã hủy'
      AND d.TrangThai = N'Đã giữ'
      AND @Now < c.GioDuKien
      AND @Now > DATEADD(HOUR, -@HanHuyGio, c.GioDuKien)
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.VI_PHAM_NO_SHOW vp WHERE vp.MaDatCho = i.MaDatCho
      );
END;
GO

/* TRIGGER 5: Điểm danh vắng mặt => tự động ghi nhận vi phạm No-Show */
CREATE OR ALTER TRIGGER dbo.TR_DIEM_DANH_GHI_NHAN_VANG_MAT
ON dbo.DIEM_DANH
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.VI_PHAM_NO_SHOW (MaViPham, Loai, NgayGhiNhan, MaHanhKhach, MaDatCho)
    SELECT
        N'VP' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18),
        N'Vắng mặt khi khởi hành', i.ThoiDiem, d.MaHanhKhach, i.MaDatCho
    FROM inserted i
    INNER JOIN dbo.DAT_CHO d ON d.MaDatCho = i.MaDatCho
    WHERE i.CoMat = 0
      AND d.TrangThai = N'Đã giữ'
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.VI_PHAM_NO_SHOW vp WHERE vp.MaDatCho = i.MaDatCho
      );
END;
GO

/* TRIGGER 6: Tích lũy đủ 3 lần vi phạm trong 30 ngày => Tự động phạt khóa 14 ngày */
CREATE OR ALTER TRIGGER dbo.TR_VI_PHAM_CAP_NHAT_HAN_CHE
ON dbo.VI_PHAM_NO_SHOW
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Nguong INT = 3;
    DECLARE @SoNgayHanChe INT = 14;

    SELECT @Nguong = TRY_CONVERT(INT, GiaTri)
    FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo = N'NGUONG_NO_SHOW';
    SELECT @SoNgayHanChe = TRY_CONVERT(INT, GiaTri)
    FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo = N'SO_NGAY_HAN_CHE';

    SET @Nguong = COALESCE(@Nguong, 3);
    SET @SoNgayHanChe = COALESCE(@SoNgayHanChe, 14);

    ;WITH CacMocThoiGian AS
    (
        SELECT DISTINCT MaHanhKhach, NgayGhiNhan FROM inserted
    ),
    NguoiDatNguong AS
    (
        SELECT m.MaHanhKhach, MAX(m.NgayGhiNhan) AS NgayDatNguong
        FROM CacMocThoiGian m
        WHERE
        (
            SELECT COUNT(*)
            FROM dbo.VI_PHAM_NO_SHOW vp
            WHERE vp.MaHanhKhach = m.MaHanhKhach
              AND vp.NgayGhiNhan >= DATEADD(DAY, -30, m.NgayGhiNhan)
              AND vp.NgayGhiNhan <= m.NgayGhiNhan
        ) >= @Nguong
        GROUP BY m.MaHanhKhach
    )
    UPDATE nd
       SET HanCheDenNgay =
           CASE
               WHEN nd.HanCheDenNgay IS NULL OR nd.HanCheDenNgay <= n.NgayDatNguong
                   THEN DATEADD(DAY, @SoNgayHanChe, n.NgayDatNguong)
               ELSE nd.HanCheDenNgay
           END
    FROM dbo.NGUOI_DUNG nd
    INNER JOIN NguoiDatNguong n ON n.MaHanhKhach = nd.MaNguoiDung;
END;
GO

-- =====================================================================
-- 7. ĐỊNH NGHĨA 6 STORED PROCEDURES (GIAO DỊCH TRANSACTION TOÀN VẸN)
-- =====================================================================

/* PROCEDURE 1: Đặt chỗ cho hành khách */
CREATE OR ALTER PROCEDURE dbo.sp_DatCho
    @MaChuyen NVARCHAR(20),
    @MaHanhKhach NVARCHAR(20),
    @MaDatCho NVARCHAR(20) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(0) = SYSDATETIME();
    DECLARE @VaiTro NVARCHAR(20);
    DECLARE @HanCheDenNgay DATETIME2(0);
    DECLARE @TrangThaiChuyen NVARCHAR(30);
    DECLARE @GioDuKien DATETIME2(0);
    DECLARE @SucChua INT;
    DECLARE @TrangThaiXe NVARCHAR(30);
    DECLARE @SoChoDangGiu INT;

    IF @MaDatCho IS NULL
        SET @MaDatCho = N'DC' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @VaiTro = VaiTro, @HanCheDenNgay = HanCheDenNgay
        FROM dbo.NGUOI_DUNG WITH (UPDLOCK, HOLDLOCK)
        WHERE MaNguoiDung = @MaHanhKhach;

        IF @VaiTro IS NULL
            THROW 51101, N'Hành khách không tồn tại.', 1;
        IF @VaiTro <> N'HANH_KHACH'
            THROW 51102, N'Tài khoản này không có vai trò hành khách.', 1;
        IF @HanCheDenNgay IS NOT NULL AND @HanCheDenNgay > @Now
            THROW 51103, N'Hành khách đang bị hạn chế đặt chỗ do vi phạm quy định.', 1;

        SELECT @TrangThaiChuyen = c.TrangThai, @GioDuKien = c.GioDuKien,
               @SucChua = x.SucChua, @TrangThaiXe = x.TrangThai
        FROM dbo.CHUYEN c WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.XE x WITH (UPDLOCK, HOLDLOCK) ON x.MaXe = c.MaXe
        WHERE c.MaChuyen = @MaChuyen;

        IF @SucChua IS NULL
            THROW 51104, N'Chuyến không tồn tại hoặc chưa được gán xe.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến' OR @GioDuKien <= @Now
            THROW 51105, N'Chỉ được đặt chỗ trên chuyến dự kiến chưa đến giờ khởi hành.', 1;
        IF @TrangThaiXe IN (N'Bảo trì', N'Ngưng sử dụng')
            THROW 51106, N'Xe được phân công đang bảo trì hoặc ngưng sử dụng.', 1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.DAT_CHO
            WHERE MaChuyen = @MaChuyen AND MaHanhKhach = @MaHanhKhach AND TrangThai = N'Đã giữ'
        )
            THROW 51107, N'Hành khách đã có chỗ được giữ trên chuyến này.', 1;

        SELECT @SoChoDangGiu = COUNT(*)
        FROM dbo.DAT_CHO WITH (UPDLOCK, HOLDLOCK)
        WHERE MaChuyen = @MaChuyen AND TrangThai = N'Đã giữ';

        IF @SoChoDangGiu >= @SucChua
            THROW 51108, N'Chuyến xe đã hết chỗ trống.', 1;

        INSERT INTO dbo.DAT_CHO (MaDatCho, ThoiDiemDat, TrangThai, MaChuyen, MaHanhKhach)
        VALUES (@MaDatCho, @Now, N'Đã giữ', @MaChuyen, @MaHanhKhach);

        COMMIT TRANSACTION;
        SELECT @MaDatCho AS MaDatChoDaTao, N'Đặt chỗ thành công' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* PROCEDURE 2: Phân công hoặc đổi xe cho chuyến */
CREATE OR ALTER PROCEDURE dbo.sp_PhanCongXe
    @MaChuyen NVARCHAR(20),
    @MaXeMoi NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(0) = SYSDATETIME();
    DECLARE @TrangThaiChuyen NVARCHAR(30), @GioDuKien DATETIME2(0), @MaDeXuat NVARCHAR(20);
    DECLARE @HuongTuyen NVARCHAR(30), @HuongDeXuat NVARCHAR(30);
    DECLARE @SucChua INT, @CoAccessibility BIT, @TrangThaiXe NVARCHAR(30), @SoChoDangGiu INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThaiChuyen = c.TrangThai, @GioDuKien = c.GioDuKien,
               @MaDeXuat = c.MaDeXuat, @HuongTuyen = t.HuongDi, @HuongDeXuat = dx.HuongDi
        FROM dbo.CHUYEN c WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TUYEN t ON t.MaTuyen = c.MaTuyen
        INNER JOIN dbo.DE_XUAT_CHUYEN dx ON dx.MaDeXuat = c.MaDeXuat
        WHERE c.MaChuyen = @MaChuyen;

        IF @TrangThaiChuyen IS NULL
            THROW 51201, N'Chuyến không tồn tại.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến' OR @GioDuKien <= @Now
            THROW 51202, N'Chỉ được phân công xe cho chuyến chưa khởi hành.', 1;
        IF @HuongTuyen <> @HuongDeXuat
            THROW 51203, N'Hướng tuyến không trùng với hướng đề xuất.', 1;

        SELECT @SucChua = SucChua, @CoAccessibility = CoAccessibility, @TrangThaiXe = TrangThai
        FROM dbo.XE WITH (UPDLOCK, HOLDLOCK)
        WHERE MaXe = @MaXeMoi;

        IF @SucChua IS NULL
            THROW 51204, N'Xe được chọn không tồn tại.', 1;
        IF @TrangThaiXe IN (N'Bảo trì', N'Ngưng sử dụng')
            THROW 51205, N'Không thể phân công xe đang bảo trì hoặc ngưng sử dụng.', 1;

        IF @CoAccessibility = 0 AND EXISTS
        (
            SELECT 1 FROM dbo.NHU_CAU_DI_LAI
            WHERE MaDeXuat = @MaDeXuat AND CoAccessibility = 1 AND TrangThai <> N'Đã hủy'
        )
            THROW 51206, N'Nhóm nhu cầu có hành khách cần hỗ trợ tiếp cận; phải chọn xe đáp ứng điều kiện.', 1;

        SELECT @SoChoDangGiu = COUNT(*)
        FROM dbo.DAT_CHO WITH (UPDLOCK, HOLDLOCK)
        WHERE MaChuyen = @MaChuyen AND TrangThai = N'Đã giữ';

        IF @SoChoDangGiu > @SucChua
            THROW 51207, N'Sức chứa xe mới nhỏ hơn số chỗ đã được giữ.', 1;

        UPDATE dbo.CHUYEN SET MaXe = @MaXeMoi WHERE MaChuyen = @MaChuyen;
        COMMIT TRANSACTION;
        SELECT N'Phân công xe thành công' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* PROCEDURE 3: Hủy đặt chỗ */
CREATE OR ALTER PROCEDURE dbo.sp_HuyDatCho
    @MaDatCho NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(0) = SYSDATETIME();
    DECLARE @TrangThaiDatCho NVARCHAR(30), @TrangThaiChuyen NVARCHAR(30), @GioDuKien DATETIME2(0);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThaiDatCho = d.TrangThai, @TrangThaiChuyen = c.TrangThai, @GioDuKien = c.GioDuKien
        FROM dbo.DAT_CHO d WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.CHUYEN c WITH (UPDLOCK, HOLDLOCK) ON c.MaChuyen = d.MaChuyen
        WHERE d.MaDatCho = @MaDatCho;

        IF @TrangThaiDatCho IS NULL
            THROW 51301, N'Đặt chỗ không tồn tại.', 1;
        IF @TrangThaiDatCho <> N'Đã giữ'
            THROW 51302, N'Chỉ có đặt chỗ đang giữ mới được hủy.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến' OR @Now >= @GioDuKien
            THROW 51303, N'Không thể hủy đặt chỗ sau khi chuyến đã khởi hành hoặc đã qua giờ dự kiến.', 1;

        UPDATE dbo.DAT_CHO SET TrangThai = N'Đã hủy' WHERE MaDatCho = @MaDatCho;

        COMMIT TRANSACTION;
        SELECT @MaDatCho AS MaDatCho, N'Hủy đặt chỗ thành công' AS KetQua,
               CASE WHEN EXISTS
                    (SELECT 1 FROM dbo.VI_PHAM_NO_SHOW WHERE MaDatCho = @MaDatCho AND Loai = N'Hủy muộn')
                    THEN N'Có ghi nhận vi phạm hủy muộn'
                    ELSE N'Không ghi nhận vi phạm hủy muộn' END AS KetQuaViPham;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* PROCEDURE 4: Ghi nhận điểm danh từng chỗ ngồi */
CREATE OR ALTER PROCEDURE dbo.sp_GhiNhanDiemDanh
    @MaDatCho NVARCHAR(20),
    @CoMat BIT,
    @GhiChu NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaDiemDanh NVARCHAR(20) = N'DD' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18);
    DECLARE @TrangThaiDatCho NVARCHAR(30), @TrangThaiChuyen NVARCHAR(30);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThaiDatCho = d.TrangThai, @TrangThaiChuyen = c.TrangThai
        FROM dbo.DAT_CHO d WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.CHUYEN c WITH (UPDLOCK, HOLDLOCK) ON c.MaChuyen = d.MaChuyen
        WHERE d.MaDatCho = @MaDatCho;

        IF @TrangThaiDatCho IS NULL
            THROW 51401, N'Đặt chỗ không tồn tại.', 1;
        IF @TrangThaiDatCho <> N'Đã giữ'
            THROW 51402, N'Chỉ ghi điểm danh cho đặt chỗ đang được giữ.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến'
            THROW 51403, N'Chỉ ghi điểm danh trước khi chuyến khởi hành.', 1;
        IF EXISTS (SELECT 1 FROM dbo.DIEM_DANH WHERE MaDatCho = @MaDatCho)
            THROW 51404, N'Đặt chỗ này đã được điểm danh; mỗi đặt chỗ chỉ điểm danh một lần.', 1;

        INSERT INTO dbo.DIEM_DANH (MaDiemDanh, ThoiDiem, CoMat, GhiChu, MaDatCho)
        VALUES (@MaDiemDanh, SYSDATETIME(), @CoMat, @GhiChu, @MaDatCho);

        COMMIT TRANSACTION;
        SELECT @MaDiemDanh AS MaDiemDanhDaTao,
               CASE WHEN @CoMat = 1 THEN N'Đã ghi nhận có mặt' ELSE N'Đã ghi nhận vắng mặt' END AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* PROCEDURE 5: Khởi hành chuyến xe */
CREATE OR ALTER PROCEDURE dbo.sp_KhoiHanhChuyen
    @MaChuyen NVARCHAR(20),
    @ThoiDiemKhoiHanh DATETIME2(0) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @ThoiDiemKhoiHanh = COALESCE(@ThoiDiemKhoiHanh, SYSDATETIME());

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.CHUYEN WITH (UPDLOCK, HOLDLOCK)
            WHERE MaChuyen = @MaChuyen AND TrangThai = N'Dự kiến'
        )
            THROW 51501, N'Chuyến không tồn tại hoặc không ở trạng thái Dự kiến.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.DAT_CHO d
            LEFT JOIN dbo.DIEM_DANH dd ON dd.MaDatCho = d.MaDatCho
            WHERE d.MaChuyen = @MaChuyen
              AND d.TrangThai = N'Đã giữ'
              AND dd.MaDiemDanh IS NULL
        )
            THROW 51502, N'Cần điểm danh tất cả đặt chỗ đang giữ (có mặt hoặc vắng mặt) trước khi khởi hành.', 1;

        UPDATE dbo.CHUYEN
        SET TrangThai = N'Đã khởi hành', GioKhoiHanhThucTe = @ThoiDiemKhoiHanh
        WHERE MaChuyen = @MaChuyen;

        COMMIT TRANSACTION;
        SELECT N'Đã ghi nhận khởi hành chuyến' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* PROCEDURE 6: Hoàn thành chuyến xe */
CREATE OR ALTER PROCEDURE dbo.sp_HoanThanhChuyen
    @MaChuyen NVARCHAR(20),
    @ThoiDiemHoanThanh DATETIME2(0) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @ThoiDiemHoanThanh = COALESCE(@ThoiDiemHoanThanh, SYSDATETIME());

    DECLARE @TrangThai NVARCHAR(30), @GioKhoiHanh DATETIME2(0);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThai = TrangThai, @GioKhoiHanh = GioKhoiHanhThucTe
        FROM dbo.CHUYEN WITH (UPDLOCK, HOLDLOCK)
        WHERE MaChuyen = @MaChuyen;

        IF @TrangThai IS NULL
            THROW 51601, N'Chuyến không tồn tại.', 1;
        IF @TrangThai <> N'Đã khởi hành' OR @GioKhoiHanh IS NULL
            THROW 51602, N'Chỉ hoàn thành chuyến đã khởi hành.', 1;
        IF @ThoiDiemHoanThanh < @GioKhoiHanh
            THROW 51603, N'Thời điểm hoàn thành không thể trước thời điểm khởi hành thực tế.', 1;

        UPDATE dbo.CHUYEN
        SET TrangThai = N'Đã hoàn thành', GioHoanThanhThucTe = @ThoiDiemHoanThanh
        WHERE MaChuyen = @MaChuyen;

        UPDATE d
        SET TrangThai = CASE WHEN dd.CoMat = 1 THEN N'Đã sử dụng' ELSE N'Vắng mặt' END
        FROM dbo.DAT_CHO d
        INNER JOIN dbo.DIEM_DANH dd ON dd.MaDatCho = d.MaDatCho
        WHERE d.MaChuyen = @MaChuyen AND d.TrangThai = N'Đã giữ';

        COMMIT TRANSACTION;
        SELECT N'Đã ghi nhận hoàn thành chuyến xe' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- =====================================================================
-- 8. TRUY VẤN KIỂM CHỨNG TỔNG HỢP KẾT QUẢ KHỞI TẠO CSDL
-- =====================================================================

SELECT N'BẢNG (TABLES)' AS LoaiDoiTuong, COUNT(*) AS SoLuong, N'10 Bảng quan hệ' AS ChiTiet
FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo')
UNION ALL
SELECT N'VIEWS' AS LoaiDoiTuong, COUNT(*) AS SoLuong, N'4 Views nghiệp vụ' AS ChiTiet
FROM sys.views WHERE schema_id = SCHEMA_ID(N'dbo')
UNION ALL
SELECT N'TRIGGERS' AS LoaiDoiTuong, COUNT(*) AS SoLuong, N'6 Triggers tự động' AS ChiTiet
FROM sys.triggers WHERE parent_class_desc = N'OBJECT_OR_COLUMN'
UNION ALL
SELECT N'PROCEDURES' AS LoaiDoiTuong, COUNT(*) AS SoLuong, N'6 Stored Procedures giao dịch' AS ChiTiet
FROM sys.procedures WHERE schema_id = SCHEMA_ID(N'dbo');
GO
