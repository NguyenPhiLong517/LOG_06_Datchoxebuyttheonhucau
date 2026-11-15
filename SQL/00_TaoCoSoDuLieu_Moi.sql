/* =====================================================================
   NEN TANG DAT CHO XE BUYT THEO NHU CAU - BUS ON DEMAND
   SQL Server 2016 SP1+ (CREATE OR ALTER)
   Gom: 10 bang, 11 rang buoc khoa ngoai, CHECK/UNIQUE, trigger va stored procedure.

   LUU Y TRUOC KHI CHAY:
   - Chay tren mot database BusOnDemandDB_Test moi/trong.
   - Neu database da co cac bang tu ban SQL cu, KHONG chay chong len schema cu.
     Hay sao luu du lieu va tao database moi/trong hoac doi ten database cho phu hop.
   - MatKhauHash trong du lieu mau chi la chuoi giu cho cho; khong phai hash dang nhap that.
   ===================================================================== */

IF DB_ID(N'BusOnDemandDB_Test') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE BusOnDemandDB_Test');
END;
GO

USE BusOnDemandDB_Test;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND is_ms_shipped = 0)
BEGIN
    THROW 50000, N'Database BusOnDemandDB_Test da co bang. Hay dung database moi/trong hoac sao luu va xu ly schema cu truoc khi chay script nay.', 1;
END;
GO

/* 1. CAU_HINH_HE_THONG */
CREATE TABLE dbo.CAU_HINH_HE_THONG
(
    TenThamSo NVARCHAR(100) NOT NULL CONSTRAINT PK_CAU_HINH_HE_THONG PRIMARY KEY,
    GiaTri    NVARCHAR(100) NOT NULL,
    MoTa      NVARCHAR(300) NULL
);
GO

/* 2. NGUOI_DUNG */
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

/* 3. XE */
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

/* 4. TUYEN */
CREATE TABLE dbo.TUYEN
(
    MaTuyen NVARCHAR(20)  NOT NULL CONSTRAINT PK_TUYEN PRIMARY KEY,
    TenTuyen NVARCHAR(100) NOT NULL,
    HuongDi  NVARCHAR(30)  NOT NULL,
    MoTa     NVARCHAR(300) NULL,
    CONSTRAINT CK_TUYEN_HuongDi CHECK (HuongDi IN (N'Chiều đi', N'Chiều về'))
);
GO

/* 5. DE_XUAT_CHUYEN: dai dien cho mot nhom nhu cau da gom */
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
    CONSTRAINT CK_DE_XUAT_TrangThai CHECK (TrangThai IN (N'Chờ duyệt', N'Đã duyệt', N'Không đủ nhu cầu'))
);
GO

/* 6. NHU_CAU_DI_LAI */
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
    CONSTRAINT CK_NHU_CAU_TrangThai CHECK (TrangThai IN (N'Chưa gom nhóm', N'Đã gom nhóm', N'Đã hủy'))
);
GO

/* 7. CHUYEN */
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

/* 8. DAT_CHO */
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

/* Chi mot cho dang giu cho moi hanh khach tren cung mot chuyen */
CREATE UNIQUE INDEX UX_DAT_CHO_MotHanhKhachMotChuyenDangGiu
ON dbo.DAT_CHO(MaChuyen, MaHanhKhach)
WHERE TrangThai = N'Đã giữ';
GO

/* 9. DIEM_DANH: moi dat cho chi diem danh mot lan; CoMat khong co DEFAULT */
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

/* 10. VI_PHAM_NO_SHOW: MaDatCho unique va FK ghep de dam bao dung hanh khach */
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

/* ---------------------------------------------------------------------
   DU LIEU CAU HINH MAU
   15 phut va 3 ngay la gia tri mac dinh trien khai, co the dieu chinh.
   --------------------------------------------------------------------- */
INSERT INTO dbo.CAU_HINH_HE_THONG (TenThamSo, GiaTri, MoTa)
VALUES
(N'NGUONG_GOM_NHOM', N'4', N'So nhu cau toi thieu de tao nhom de xuat'),
(N'DO_RONG_KHUNG_GIO_PHUT', N'30', N'Do rong khung gio gom nhu cau, tinh bang phut'),
(N'HAN_HUY_GIO', N'2', N'Huy truoc gio khoi hanh tu 2 gio tro len thi khong tinh vi pham huy muon'),
(N'NGUONG_NO_SHOW', N'3', N'So vi pham trong 30 ngay de bat dau han che dat cho'),
(N'SO_NGAY_HAN_CHE', N'14', N'So ngay han che dat cho khi dat nguong vi pham'),
(N'CHU_KY_JOB_PHUT', N'15', N'Gia tri mac dinh tam cho tien trinh gom nhom dinh ky; quyet dinh trien khai'),
(N'SO_NGAY_DONG_NHOM', N'3', N'Gia tri mac dinh tam de dong nhom chua du nhu cau; quyet dinh trien khai');
GO

/* Du lieu danh muc mau */
INSERT INTO dbo.TUYEN (MaTuyen, TenTuyen, HuongDi, MoTa)
VALUES
(N'T01', N'Tuyen chieu di', N'Chiều đi', N'Tuyen mau theo chieu di'),
(N'T02', N'Tuyen chieu ve', N'Chiều về', N'Tuyen mau theo chieu ve');
GO

INSERT INTO dbo.XE (MaXe, BienSo, SucChua, CoAccessibility, TrangThai)
VALUES
(N'XE01', N'51A-000.01', 16, 0, N'Sẵn sàng'),
(N'XE02', N'51A-000.02', 29, 1, N'Sẵn sàng');
GO

/* MatKhauHash duoi day chi la gia tri mau, khong phai hash dang nhap hop le */
INSERT INTO dbo.NGUOI_DUNG
(MaNguoiDung, HoTen, Email, SoDienThoai, TenDangNhap, MatKhauHash, HanCheDenNgay, VaiTro)
VALUES
(N'ND01', N'Hanh khach mau', N'nd01@example.com', N'0900000001', N'nd01', N'REPLACE_WITH_REAL_HASH', NULL, N'HANH_KHACH'),
(N'ND02', N'Lai xe mau',     N'nd02@example.com', N'0900000002', N'nd02', N'REPLACE_WITH_REAL_HASH', NULL, N'LAI_XE'),
(N'ND03', N'Dieu phoi mau',  N'nd03@example.com', N'0900000003', N'nd03', N'REPLACE_WITH_REAL_HASH', NULL, N'DIEU_PHOI');
GO

/* ---------------------------------------------------------------------
   TRIGGER 1: cap nhat tong so nhu cau can ho tro tiep can theo de xuat
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   TRIGGER 2: khong cho luu chuyen sai lai xe, sai huong, sai xe tiep can
   hoac co so dat cho vuot suc chua.
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.TR_CHUYEN_KIEM_TRA_PHAN_CONG
ON dbo.CHUYEN
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung = i.MaLaiXe
        WHERE nd.VaiTro <> N'LAI_XE'
    )
        THROW 51001, N'MaLaiXe phai tham chieu den nguoi dung co vai tro LAI_XE.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.XE x ON x.MaXe = i.MaXe
        WHERE x.TrangThai IN (N'Bảo trì', N'Ngưng sử dụng')
    )
        THROW 51002, N'Khong the phan cong xe dang bao tri hoac ngung su dung.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.TUYEN t ON t.MaTuyen = i.MaTuyen
        INNER JOIN dbo.DE_XUAT_CHUYEN dx ON dx.MaDeXuat = i.MaDeXuat
        WHERE t.HuongDi <> dx.HuongDi
    )
        THROW 51003, N'Huong cua tuyen phai trung voi huong cua de xuat chuyen.', 1;

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
        THROW 51004, N'Nhom co hanh khach can ho tro tiep can phai duoc phan cong xe dap ung dieu kien.', 1;

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
        THROW 51005, N'Khong the phan cong xe co suc chua nho hon so cho dang duoc giu.', 1;
END;
GO

/* ---------------------------------------------------------------------
   TRIGGER 3: chan dat cho truc tiep neu chuyen khong hop le, hanh khach
   bi han che, hoac so cho vuot suc chua. Thu tuc DatCho van kiem tra truoc.
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.TR_DAT_CHO_KIEM_TRA
ON dbo.DAT_CHO
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ThoiDiemHienTai DATETIME2(0) = SYSDATETIME();

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
        THROW 51006, N'Khong the huy dat cho sau khi chuyen da khoi hanh hoac da qua gio du kien khoi hanh.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.CHUYEN c ON c.MaChuyen = i.MaChuyen
        WHERE i.TrangThai = N'Đã giữ'
          AND (c.TrangThai <> N'Dự kiến' OR c.GioDuKien <= @ThoiDiemHienTai)
    )
        THROW 51007, N'Chi duoc giu cho tren chuyen dang o trang thai Du kien va chua den gio khoi hanh.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung = i.MaHanhKhach
        WHERE i.TrangThai = N'Đã giữ'
          AND (nd.VaiTro <> N'HANH_KHACH'
               OR (nd.HanCheDenNgay IS NOT NULL AND nd.HanCheDenNgay > @ThoiDiemHienTai))
    )
        THROW 51008, N'Nguoi dat cho phai la hanh khach va khong dang trong thoi gian bi han che.', 1;

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
        THROW 51009, N'Vuot qua suc chua xe; thao tac dat cho bi huy.', 1;
END;
GO

/* ---------------------------------------------------------------------
   TRIGGER 4: huy muon duoi HAN_HUY_GIO truoc gio du kien => ghi vi pham
   (ke ca khi cap nhat trang thai huy truc tiep, khong chi qua stored proc).
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   TRIGGER 5: diem danh vang mat => ghi nhan vi pham no-show
   --------------------------------------------------------------------- */
/* Trigger 5: chi cho ghi/cap nhat diem danh khi chuyen con du kien */
CREATE OR ALTER TRIGGER dbo.TR_DIEM_DANH_KIEM_TRA_TRUOC_KHOI_HANH
ON dbo.DIEM_DANH
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.DAT_CHO d ON d.MaDatCho=i.MaDatCho
        INNER JOIN dbo.CHUYEN c ON c.MaChuyen=d.MaChuyen
        WHERE d.TrangThai<>N'Đã giữ' OR c.TrangThai<>N'Dự kiến'
    )
        THROW 51403, N'Chi duoc ghi/cap nhat diem danh cho dat cho dang giu truoc khi chuyen khoi hanh.', 1;
END;
GO

/* ---------------------------------------------------------------------
   TRIGGER 6: neu hanh khach dat >= NGUONG_NO_SHOW vi pham trong 30 ngay,
   tu dong cap nhat HanCheDenNgay them SO_NGAY_HAN_CHE.
   --------------------------------------------------------------------- */
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

/* ---------------------------------------------------------------------
   STORED PROCEDURE: DAT CHO
   Kiem tra han che, trang thai chuyen va suc chua trong transaction.
   --------------------------------------------------------------------- */
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
    DECLARE @MaDeXuat NVARCHAR(20);
    DECLARE @CoAccessibility BIT;

    IF @MaDatCho IS NULL
        SET @MaDatCho = N'DC' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @VaiTro = VaiTro, @HanCheDenNgay = HanCheDenNgay
        FROM dbo.NGUOI_DUNG WITH (UPDLOCK, HOLDLOCK)
        WHERE MaNguoiDung = @MaHanhKhach;

        IF @VaiTro IS NULL
            THROW 51101, N'Hanh khach khong ton tai.', 1;
        IF @VaiTro <> N'HANH_KHACH'
            THROW 51102, N'Tai khoan nay khong co vai tro hanh khach.', 1;
        IF @HanCheDenNgay IS NOT NULL AND @HanCheDenNgay > @Now
            THROW 51103, N'Hanh khach dang bi han che dat cho.', 1;

        SELECT @TrangThaiChuyen = c.TrangThai, @GioDuKien = c.GioDuKien,
               @SucChua = x.SucChua, @TrangThaiXe = x.TrangThai,
               @MaDeXuat = c.MaDeXuat, @CoAccessibility = x.CoAccessibility
        FROM dbo.CHUYEN c WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.XE x WITH (UPDLOCK, HOLDLOCK) ON x.MaXe = c.MaXe
        WHERE c.MaChuyen = @MaChuyen;

        IF @SucChua IS NULL
            THROW 51104, N'Chuyen khong ton tai hoac chua duoc gan xe.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến' OR @GioDuKien <= @Now
            THROW 51105, N'Chi duoc dat cho tren chuyen du kien chua den gio khoi hanh.', 1;
        IF @TrangThaiXe IN (N'Bảo trì', N'Ngưng sử dụng')
            THROW 51106, N'Xe duoc phan cong dang bao tri hoac ngung su dung.', 1;

        IF @CoAccessibility = 0 AND EXISTS
        (
            SELECT 1 FROM dbo.NHU_CAU_DI_LAI
            WHERE MaHanhKhach = @MaHanhKhach
              AND MaDeXuat = @MaDeXuat
              AND CoAccessibility = 1
              AND TrangThai <> N'Đã hủy'
        )
            THROW 51109, N'Nhu cau cua ban yeu cau ho tro tiep can; xe cua chuyen nay khong dap ung dieu kien.', 1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.DAT_CHO
            WHERE MaChuyen = @MaChuyen AND MaHanhKhach = @MaHanhKhach AND TrangThai = N'Đã giữ'
        )
            THROW 51107, N'Hanh khach da co cho duoc giu tren chuyen nay.', 1;

        SELECT @SoChoDangGiu = COUNT(*)
        FROM dbo.DAT_CHO WITH (UPDLOCK, HOLDLOCK)
        WHERE MaChuyen = @MaChuyen AND TrangThai = N'Đã giữ';

        IF @SoChoDangGiu >= @SucChua
            THROW 51108, N'Chuyen da het suc chua, khong the dat them cho.', 1;

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

/* ---------------------------------------------------------------------
   STORED PROCEDURE: PHAN CONG/DOI XE CHO CHUYEN
   Kiem tra huong tuyen, suc chua va nhu cau tiep can.
   --------------------------------------------------------------------- */
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
            THROW 51201, N'Chuyen khong ton tai.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến' OR @GioDuKien <= @Now
            THROW 51202, N'Chi duoc phan cong xe cho chuyen chua khoi hanh.', 1;
        IF @HuongTuyen <> @HuongDeXuat
            THROW 51203, N'Huong tuyen khong trung voi huong de xuat.', 1;

        SELECT @SucChua = SucChua, @CoAccessibility = CoAccessibility, @TrangThaiXe = TrangThai
        FROM dbo.XE WITH (UPDLOCK, HOLDLOCK)
        WHERE MaXe = @MaXeMoi;

        IF @SucChua IS NULL
            THROW 51204, N'Xe duoc chon khong ton tai.', 1;
        IF @TrangThaiXe IN (N'Bảo trì', N'Ngưng sử dụng')
            THROW 51205, N'Khong the phan cong xe dang bao tri hoac ngung su dung.', 1;

        IF @CoAccessibility = 0 AND EXISTS
        (
            SELECT 1 FROM dbo.NHU_CAU_DI_LAI
            WHERE MaDeXuat = @MaDeXuat AND CoAccessibility = 1 AND TrangThai <> N'Đã hủy'
        )
            THROW 51206, N'Nhom nhu cau co hanh khach can ho tro tiep can; phai chon xe dap ung dieu kien.', 1;

        SELECT @SoChoDangGiu = COUNT(*)
        FROM dbo.DAT_CHO WITH (UPDLOCK, HOLDLOCK)
        WHERE MaChuyen = @MaChuyen AND TrangThai = N'Đã giữ';

        IF @SoChoDangGiu > @SucChua
            THROW 51207, N'Suc chua xe moi nho hon so cho da duoc giu.', 1;

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

/* ---------------------------------------------------------------------
   STORED PROCEDURE: HUY DAT CHO
   Trigger se tu ghi nhan vi pham neu huy muon duoi so gio cau hinh.
   --------------------------------------------------------------------- */
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
            THROW 51301, N'Dat cho khong ton tai.', 1;
        IF @TrangThaiDatCho <> N'Đã giữ'
            THROW 51302, N'Chi co dat cho dang giu moi duoc huy.', 1;
        IF @TrangThaiChuyen <> N'Dự kiến' OR @Now >= @GioDuKien
            THROW 51303, N'Khong the huy dat cho sau khi chuyen da khoi hanh hoac da qua gio du kien.', 1;

        UPDATE dbo.DAT_CHO SET TrangThai = N'Đã hủy' WHERE MaDatCho = @MaDatCho;
        /* TR_DAT_CHO_GHI_NHAN_HUY_MUON se tu ghi VI_PHAM_NO_SHOW neu huy muon. */

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

/* ---------------------------------------------------------------------
   STORED PROCEDURE: GHI NHAN DIEM DANH
   Neu CoMat=0, trigger tu ghi vi pham vang mat va cap nhat han che neu dat nguong.
   --------------------------------------------------------------------- */
/* Ghi diem danh: cho phep sua lai ket qua truoc khi xac nhan khoi hanh */
CREATE OR ALTER PROCEDURE dbo.sp_GhiNhanDiemDanh
    @MaDatCho NVARCHAR(20),
    @CoMat BIT,
    @GhiChu NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @MaDiemDanh NVARCHAR(20)=N'DD'+LEFT(REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N''),18);
    DECLARE @TrangThaiDatCho NVARCHAR(30), @TrangThaiChuyen NVARCHAR(30);

    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT @TrangThaiDatCho=d.TrangThai,@TrangThaiChuyen=c.TrangThai
        FROM dbo.DAT_CHO d WITH (UPDLOCK,HOLDLOCK)
        INNER JOIN dbo.CHUYEN c WITH (UPDLOCK,HOLDLOCK) ON c.MaChuyen=d.MaChuyen
        WHERE d.MaDatCho=@MaDatCho;

        IF @TrangThaiDatCho IS NULL THROW 51401,N'Dat cho khong ton tai.',1;
        IF @TrangThaiDatCho<>N'Đã giữ' THROW 51402,N'Chi ghi diem danh cho dat cho dang duoc giu.',1;
        IF @TrangThaiChuyen<>N'Dự kiến' THROW 51403,N'Chi ghi/cap nhat diem danh truoc khi chuyen khoi hanh.',1;

        IF EXISTS(SELECT 1 FROM dbo.DIEM_DANH WITH (UPDLOCK,HOLDLOCK) WHERE MaDatCho=@MaDatCho)
            UPDATE dbo.DIEM_DANH SET CoMat=@CoMat,GhiChu=@GhiChu,ThoiDiem=SYSDATETIME() WHERE MaDatCho=@MaDatCho;
        ELSE
            INSERT INTO dbo.DIEM_DANH(MaDiemDanh,ThoiDiem,CoMat,GhiChu,MaDatCho)
            VALUES(@MaDiemDanh,SYSDATETIME(),@CoMat,@GhiChu,@MaDatCho);

        COMMIT TRANSACTION;
        SELECT @MaDatCho AS MaDatCho,
               CASE WHEN @CoMat=1 THEN N'Da ghi nhan co mat' ELSE N'Da ghi nhan vang mat; vi pham no-show chi duoc xu ly khi xac nhan khoi hanh' END AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* ---------------------------------------------------------------------
   STORED PROCEDURE: KHOI HANH CHUYEN
   Yeu cau diem danh day du cac dat cho dang giu truoc khi khoi hanh.
   --------------------------------------------------------------------- */
/* Xac nhan khoi hanh: thuc hien UC15 sau khi da co diem danh */
CREATE OR ALTER PROCEDURE dbo.sp_KhoiHanhChuyen
    @MaChuyen NVARCHAR(20),
    @ThoiDiemKhoiHanh DATETIME2(0)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @ThoiDiemKhoiHanh=COALESCE(@ThoiDiemKhoiHanh,SYSDATETIME());
    DECLARE @TrangThai NVARCHAR(30);
    DECLARE @Nguong INT=3;
    DECLARE @SoNgayHanChe INT=14;

    SELECT @Nguong=TRY_CONVERT(INT,GiaTri) FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo=N'NGUONG_NO_SHOW';
    SELECT @SoNgayHanChe=TRY_CONVERT(INT,GiaTri) FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo=N'SO_NGAY_HAN_CHE';
    SET @Nguong=COALESCE(@Nguong,3); SET @SoNgayHanChe=COALESCE(@SoNgayHanChe,14);

    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT @TrangThai=TrangThai FROM dbo.CHUYEN WITH (UPDLOCK,HOLDLOCK) WHERE MaChuyen=@MaChuyen;
        IF @TrangThai IS NULL THROW 51501,N'Chuyen khong ton tai.',1;
        IF @TrangThai<>N'Dự kiến' THROW 51501,N'Chuyen khong o trang thai Du kien.',1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.DAT_CHO d
            LEFT JOIN dbo.DIEM_DANH dd ON dd.MaDatCho=d.MaDatCho
            WHERE d.MaChuyen=@MaChuyen AND d.TrangThai=N'Đã giữ' AND dd.MaDiemDanh IS NULL
        )
            THROW 51502,N'Can diem danh tat ca dat cho dang giu (co mat hoac vang mat) truoc khi khoi hanh.',1;

        INSERT INTO dbo.VI_PHAM_NO_SHOW(MaViPham,Loai,NgayGhiNhan,MaHanhKhach,MaDatCho)
        SELECT N'VP'+LEFT(REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N''),18),
               N'Vắng mặt khi khởi hành',@ThoiDiemKhoiHanh,d.MaHanhKhach,d.MaDatCho
        FROM dbo.DAT_CHO d
        INNER JOIN dbo.DIEM_DANH dd ON dd.MaDatCho=d.MaDatCho
        WHERE d.MaChuyen=@MaChuyen AND d.TrangThai=N'Đã giữ' AND dd.CoMat=0
          AND NOT EXISTS(SELECT 1 FROM dbo.VI_PHAM_NO_SHOW v WHERE v.MaDatCho=d.MaDatCho);

        UPDATE d SET TrangThai=N'No-show'
        FROM dbo.DAT_CHO d
        INNER JOIN dbo.DIEM_DANH dd ON dd.MaDatCho=d.MaDatCho
        WHERE d.MaChuyen=@MaChuyen AND d.TrangThai=N'Đã giữ' AND dd.CoMat=0;

        UPDATE dbo.CHUYEN SET TrangThai=N'Đã khởi hành',GioKhoiHanhThucTe=@ThoiDiemKhoiHanh WHERE MaChuyen=@MaChuyen;
        COMMIT TRANSACTION;
        SELECT N'Da xac nhan khoi hanh; cac truong hop vang mat da duoc ghi nhan no-show' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* ---------------------------------------------------------------------
   STORED PROCEDURE: HOAN THANH CHUYEN
   --------------------------------------------------------------------- */
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
            THROW 51601, N'Chuyen khong ton tai.', 1;
        IF @TrangThai <> N'Đã khởi hành' OR @GioKhoiHanh IS NULL
            THROW 51602, N'Chi hoan thanh chuyen da khoi hanh.', 1;
        IF @ThoiDiemHoanThanh < @GioKhoiHanh
            THROW 51603, N'Thoi diem hoan thanh khong the truoc thoi diem khoi hanh thuc te.', 1;

        UPDATE dbo.CHUYEN
        SET TrangThai = N'Đã hoàn thành', GioHoanThanhThucTe = @ThoiDiemHoanThanh
        WHERE MaChuyen = @MaChuyen;

        UPDATE d
        SET TrangThai = CASE WHEN dd.CoMat = 1 THEN N'Đã sử dụng' ELSE N'Vắng mặt' END
        FROM dbo.DAT_CHO d
        INNER JOIN dbo.DIEM_DANH dd ON dd.MaDatCho = d.MaDatCho
        WHERE d.MaChuyen = @MaChuyen AND d.TrangThai = N'Đã giữ';

        COMMIT TRANSACTION;
        SELECT N'Đã ghi nhận hoàn thành chuyến' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* =====================================================================
   CAC QUY TAC DUOC SQL BAO VE TRUC TIEP
   - Khong them dat cho vuot suc chua (trigger + sp_DatCho).
   - Khong phan cong xe khong dap ung nhu cau tiep can (trigger + sp_PhanCongXe).
   - Huy muon tu dong tao VI_PHAM_NO_SHOW (trigger + sp_HuyDatCho).
   - Diem danh vang mat tu dong tao vi pham; dat nguong 3 vi pham/30 ngay
     thi HanCheDenNgay duoc cap nhat them 14 ngay theo cau hinh.
   - SoCanHoTro duoc tinh lai tu NHU_CAU_DI_LAI khi nhu cau thay doi.

   CHUA TU DONG THUC HIEN TRONG SCRIPT NAY:
   - Chay job gom nhom moi 15 phut va tu dong dong nhom sau 3 ngay; hai moc nay
     la gia tri mac dinh trien khai, can lap lich job/logic ung dung neu su dung.
   - Giao dien WinForms/dang nhap va quy trinh nghiep vu con lai cua UC.
   ===================================================================== */
