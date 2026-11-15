/*
  NANG CAP BO SUNG CHO APP WINFORMS BUS ON DEMAND
  Chay mot lan tren BusOnDemandDB_Test SAU khi da chay BusOnDemandDB_SQL_HoanChinh.sql.
  Script chi mo rong cac trang thai duoc mo ta trong UC; khong xoa du lieu.
*/
USE BusOnDemandDB_Test;
GO

/* 1. Mo rong trang thai de xuat theo UC05 */
IF OBJECT_ID(N'dbo.CK_DE_XUAT_TrangThai', N'C') IS NOT NULL
    ALTER TABLE dbo.DE_XUAT_CHUYEN DROP CONSTRAINT CK_DE_XUAT_TrangThai;
GO
ALTER TABLE dbo.DE_XUAT_CHUYEN WITH CHECK ADD CONSTRAINT CK_DE_XUAT_TrangThai
CHECK (TrangThai IN (N'Chờ duyệt',N'Đã duyệt',N'Không đủ nhu cầu',N'Từ chối'));
GO

/* 2. Mo rong trang thai nhu cau theo UC02, UC04, UC05 */
IF OBJECT_ID(N'dbo.CK_NHU_CAU_TrangThai', N'C') IS NOT NULL
    ALTER TABLE dbo.NHU_CAU_DI_LAI DROP CONSTRAINT CK_NHU_CAU_TrangThai;
GO
ALTER TABLE dbo.NHU_CAU_DI_LAI WITH CHECK ADD CONSTRAINT CK_NHU_CAU_TrangThai
CHECK (TrangThai IN (N'Chưa gom nhóm',N'Đã gom nhóm',N'Đã mở chuyến',N'Không đủ nhóm',N'Đã hủy'));
GO

/* 3. Mo rong trang thai dat cho theo UC07, UC10, UC14, UC15 */
IF OBJECT_ID(N'dbo.CK_DAT_CHO_TrangThai', N'C') IS NOT NULL
    ALTER TABLE dbo.DAT_CHO DROP CONSTRAINT CK_DAT_CHO_TrangThai;
GO
ALTER TABLE dbo.DAT_CHO WITH CHECK ADD CONSTRAINT CK_DAT_CHO_TrangThai
CHECK (TrangThai IN (N'Đã giữ',N'Đã hủy',N'Đã hủy muộn',N'Đã hủy do hệ thống',N'Đã sử dụng',N'Đã hoàn thành',N'Vắng mặt',N'No-show'));
GO

/* 4. Bao ve huy dat cho sau gio; huy do he thong khong tinh vi pham huy muon */
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
        INNER JOIN deleted d ON d.MaDatCho=i.MaDatCho
        INNER JOIN dbo.CHUYEN c ON c.MaChuyen=i.MaChuyen
        WHERE i.TrangThai IN (N'Đã hủy',N'Đã hủy muộn',N'Đã hủy do hệ thống')
          AND d.TrangThai=N'Đã giữ'
          AND (c.TrangThai<>N'Dự kiến' OR @ThoiDiemHienTai>=c.GioDuKien)
    )
        THROW 51006, N'Khong the huy dat cho sau khi chuyen da khoi hanh hoac da qua gio du kien khoi hanh.', 1;

    IF EXISTS
    (
        SELECT 1 FROM inserted i
        INNER JOIN dbo.CHUYEN c ON c.MaChuyen=i.MaChuyen
        WHERE i.TrangThai=N'Đã giữ'
          AND (c.TrangThai<>N'Dự kiến' OR c.GioDuKien<=@ThoiDiemHienTai)
    )
        THROW 51007, N'Chi duoc giu cho tren chuyen dang o trang thai Du kien va chua den gio khoi hanh.', 1;

    IF EXISTS
    (
        SELECT 1 FROM inserted i
        INNER JOIN dbo.NGUOI_DUNG nd ON nd.MaNguoiDung=i.MaHanhKhach
        WHERE i.TrangThai=N'Đã giữ'
          AND (nd.VaiTro<>N'HANH_KHACH' OR (nd.HanCheDenNgay IS NOT NULL AND nd.HanCheDenNgay>@ThoiDiemHienTai))
    )
        THROW 51008, N'Nguoi dat cho phai la hanh khach va khong dang trong thoi gian bi han che.', 1;

    IF EXISTS
    (
        SELECT 1 FROM (SELECT DISTINCT MaChuyen FROM inserted) i
        INNER JOIN dbo.CHUYEN c WITH (UPDLOCK,HOLDLOCK) ON c.MaChuyen=i.MaChuyen
        INNER JOIN dbo.XE x ON x.MaXe=c.MaXe
        CROSS APPLY (SELECT COUNT(*) AS SoChoDangGiu FROM dbo.DAT_CHO d WITH (UPDLOCK,HOLDLOCK)
                     WHERE d.MaChuyen=c.MaChuyen AND d.TrangThai=N'Đã giữ') dc
        WHERE dc.SoChoDangGiu>x.SucChua
    )
        THROW 51009, N'Vuot qua suc chua xe; thao tac dat cho bi huy.', 1;
END;
GO

/* 5. Huy muon: ghi vi pham ca khi trang thai luu la Da huy muon */
CREATE OR ALTER TRIGGER dbo.TR_DAT_CHO_GHI_NHAN_HUY_MUON
ON dbo.DAT_CHO
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2(0)=SYSDATETIME();
    DECLARE @HanHuyGio INT=2;
    SELECT @HanHuyGio=TRY_CONVERT(INT,GiaTri) FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo=N'HAN_HUY_GIO';
    SET @HanHuyGio=COALESCE(@HanHuyGio,2);

    INSERT INTO dbo.VI_PHAM_NO_SHOW(MaViPham,Loai,NgayGhiNhan,MaHanhKhach,MaDatCho)
    SELECT N'VP'+LEFT(REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N''),18),N'Hủy muộn',@Now,i.MaHanhKhach,i.MaDatCho
    FROM inserted i
    INNER JOIN deleted d ON d.MaDatCho=i.MaDatCho
    INNER JOIN dbo.CHUYEN c ON c.MaChuyen=i.MaChuyen
    WHERE i.TrangThai IN (N'Đã hủy',N'Đã hủy muộn')
      AND d.TrangThai=N'Đã giữ'
      AND @Now<c.GioDuKien
      AND @Now>DATEADD(HOUR,-@HanHuyGio,c.GioDuKien)
      AND NOT EXISTS(SELECT 1 FROM dbo.VI_PHAM_NO_SHOW vp WHERE vp.MaDatCho=i.MaDatCho);
END;
GO

/* 6. Diem danh vang mat: ghi vi pham va chuyen DatCho sang No-show */
IF OBJECT_ID(N'dbo.TR_DIEM_DANH_GHI_NHAN_VANG_MAT',N'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_DIEM_DANH_GHI_NHAN_VANG_MAT;
GO
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

/* 7. Dat cho: them kiem tra accessibility de bao ve UC06/UC20 */
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

/* 8. Huy dat cho dung trang thai UC07; trigger tu ghi vi pham neu huy muon */
CREATE OR ALTER PROCEDURE dbo.sp_HuyDatCho
    @MaDatCho NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Now DATETIME2(0)=SYSDATETIME();
    DECLARE @TrangThaiDatCho NVARCHAR(30),@TrangThaiChuyen NVARCHAR(30),@GioDuKien DATETIME2(0),@HanHuyGio INT=2;
    SELECT @HanHuyGio=TRY_CONVERT(INT,GiaTri) FROM dbo.CAU_HINH_HE_THONG WHERE TenThamSo=N'HAN_HUY_GIO';
    SET @HanHuyGio=COALESCE(@HanHuyGio,2);

    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT @TrangThaiDatCho=d.TrangThai,@TrangThaiChuyen=c.TrangThai,@GioDuKien=c.GioDuKien
        FROM dbo.DAT_CHO d WITH (UPDLOCK,HOLDLOCK)
        INNER JOIN dbo.CHUYEN c WITH (UPDLOCK,HOLDLOCK) ON c.MaChuyen=d.MaChuyen
        WHERE d.MaDatCho=@MaDatCho;
        IF @TrangThaiDatCho IS NULL THROW 51301,N'Dat cho khong ton tai.',1;
        IF @TrangThaiDatCho<>N'Đã giữ' THROW 51302,N'Chi co dat cho dang giu moi duoc huy.',1;
        IF @TrangThaiChuyen<>N'Dự kiến' OR @Now>=@GioDuKien THROW 51303,N'Khong the huy dat cho sau khi chuyen da khoi hanh hoac da qua gio du kien.',1;

        IF @Now>DATEADD(HOUR,-@HanHuyGio,@GioDuKien)
            UPDATE dbo.DAT_CHO SET TrangThai=N'Đã hủy muộn' WHERE MaDatCho=@MaDatCho;
        ELSE
            UPDATE dbo.DAT_CHO SET TrangThai=N'Đã hủy' WHERE MaDatCho=@MaDatCho;

        COMMIT TRANSACTION;
        SELECT @MaDatCho AS MaDatCho,N'Hủy đặt chỗ thành công' AS KetQua,
               CASE WHEN EXISTS(SELECT 1 FROM dbo.VI_PHAM_NO_SHOW WHERE MaDatCho=@MaDatCho AND Loai=N'Hủy muộn')
                    THEN N'Có ghi nhận vi phạm hủy muộn' ELSE N'Không ghi nhận vi phạm hủy muộn' END AS KetQuaViPham;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* 9. Ghi va sua diem danh truoc khoi hanh; xu ly No-show tai UC13 */
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
/* 10. Ket thuc chuyen dung trang thai theo UC14; No-show duoc giu nguyen */
CREATE OR ALTER PROCEDURE dbo.sp_HoanThanhChuyen
    @MaChuyen NVARCHAR(20),
    @ThoiDiemHoanThanh DATETIME2(0)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @ThoiDiemHoanThanh=COALESCE(@ThoiDiemHoanThanh,SYSDATETIME());
    DECLARE @TrangThai NVARCHAR(30),@GioKhoiHanh DATETIME2(0);
    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT @TrangThai=TrangThai,@GioKhoiHanh=GioKhoiHanhThucTe FROM dbo.CHUYEN WITH (UPDLOCK,HOLDLOCK) WHERE MaChuyen=@MaChuyen;
        IF @TrangThai IS NULL THROW 51601,N'Chuyen khong ton tai.',1;
        IF @TrangThai<>N'Đã khởi hành' OR @GioKhoiHanh IS NULL THROW 51602,N'Chi hoan thanh chuyen da khoi hanh.',1;
        IF @ThoiDiemHoanThanh<@GioKhoiHanh THROW 51603,N'Thoi diem hoan thanh khong the truoc thoi diem khoi hanh thuc te.',1;

        UPDATE dbo.CHUYEN SET TrangThai=N'Đã hoàn thành',GioHoanThanhThucTe=@ThoiDiemHoanThanh WHERE MaChuyen=@MaChuyen;
        UPDATE d SET TrangThai=N'Đã hoàn thành'
        FROM dbo.DAT_CHO d INNER JOIN dbo.DIEM_DANH dd ON dd.MaDatCho=d.MaDatCho
        WHERE d.MaChuyen=@MaChuyen AND d.TrangThai=N'Đã giữ' AND dd.CoMat=1;
        COMMIT TRANSACTION;
        SELECT N'Đã ghi nhận hoàn thành chuyến' AS KetQua;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

PRINT N'Đã cập nhật các trạng thái và thủ tục để phù hợp các UC trên database BusOnDemandDB_Test.';
GO
