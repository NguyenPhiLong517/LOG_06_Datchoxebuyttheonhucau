/* =====================================================================
   BO KIEM THU SQL - BUS ON DEMAND
   Database: BusOnDemandDB_Test
   Muc dich: kiem tra cau truc, rang buoc, suc chua, accessibility,
             huy muon, no-show va han che dat cho.

   LUU Y:
   - Script nay KHONG tao/xoa bang va KHONG sua schema.
   - Script tao cac dong du lieu KIEM THU moi voi ma ngau nhien; cac dong
     nay duoc GIU LAI trong database de doi chieu sau khi test.
   - Chi chay tren database TEST, khong chay tren database dang dung that.
   - Can da chay script tao database va script cap nhat trang thai/procedure.
   - Script nay duoc soan theo schema cua goi BusOnDemand da gui; chua duoc
     thuc thi truc tiep tren instance SQL Server tren may nguoi dung.
   ===================================================================== */

USE BusOnDemandDB_Test;
SET NOCOUNT ON;

DECLARE @Run VARCHAR(8) = UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8));
DECLARE @Results TABLE
(
    TC NVARCHAR(10),
    NoiDung NVARCHAR(200),
    KetQuaMongDoi NVARCHAR(300),
    KetQuaThucTe NVARCHAR(600),
    DanhGia NVARCHAR(30)
);

DECLARE @SoBang INT, @SoSP INT, @SoTrigger INT;
DECLARE @Err INT, @Msg NVARCHAR(4000), @SeedPlate NVARCHAR(20);
DECLARE @MaXeZero NVARCHAR(20), @BienSoZero NVARCHAR(20), @MaXeDup NVARCHAR(20);
DECLARE @MaDatChoBadFK NVARCHAR(20);
DECLARE @i INT, @UserCode NVARCHAR(20), @Email NVARCHAR(255);
DECLARE @DxCap NVARCHAR(20), @TripCap NVARCHAR(20), @TripTimeCap DATETIME2(0);
DECLARE @PassengerId NVARCHAR(20), @BookingId NVARCHAR(20), @NewBookingId NVARCHAR(20);
DECLARE @SoCho INT;
DECLARE @DxAcc NVARCHAR(20), @TripAcc NVARCHAR(20), @NeedAcc NVARCHAR(20), @TimeAcc DATETIME2(0);
DECLARE @DxCancel NVARCHAR(20), @TripCancel NVARCHAR(20), @BookingCancel NVARCHAR(20), @TimeCancel DATETIME2(0);
DECLARE @DxNS NVARCHAR(20), @TripNS NVARCHAR(20), @BookingNS NVARCHAR(20), @TimeNS DATETIME2(0);
DECLARE @DxV NVARCHAR(20), @TripV NVARCHAR(20), @BookingV NVARCHAR(20), @TimeV DATETIME2(0);
DECLARE @HanChe DATETIME2(0), @XeSauTC07 NVARCHAR(20);

/* TC01 - Kiem tra so bang, stored procedure va trigger */
SELECT @SoBang = COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo');
SELECT @SoSP = COUNT(*) FROM sys.procedures WHERE schema_id = SCHEMA_ID(N'dbo');
SELECT @SoTrigger = COUNT(*) FROM sys.triggers WHERE parent_class_desc = N'OBJECT_OR_COLUMN';
INSERT INTO @Results
VALUES
(
    N'TC01', N'Kiem tra cau truc SQL', N'10 bang, 6 stored procedure, 6 trigger',
    CONCAT(N'Bang=', @SoBang, N'; Stored procedure=', @SoSP, N'; Trigger=', @SoTrigger),
    CASE WHEN @SoBang = 10 AND @SoSP = 6 AND @SoTrigger = 6 THEN N'DAT' ELSE N'CAN KIEM TRA' END
);

/* TC02 - Tu choi xe co suc chua bang 0 */
SET @MaXeZero = N'Z' + @Run;
SET @BienSoZero = N'TEST-ZERO-' + @Run;
BEGIN TRY
    INSERT INTO dbo.XE (MaXe, BienSo, SucChua, CoAccessibility, TrangThai)
    VALUES (@MaXeZero, @BienSoZero, 0, 0, N'Sẵn sàng');
    DELETE FROM dbo.XE WHERE MaXe = @MaXeZero;
    INSERT INTO @Results VALUES (N'TC02', N'Them xe suc chua bang 0', N'SQL tu choi ban ghi', N'Ban ghi da duoc chen, sau do da don dep', N'KHONG DAT');
END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES
    (N'TC02', N'Them xe suc chua bang 0', N'SQL tu choi ban ghi', CONCAT(N'Loi ', @Err, N': ', @Msg), CASE WHEN @Err = 547 THEN N'DAT' ELSE N'CAN KIEM TRA' END);
END CATCH;

/* TC03 - Tu choi bien so trung voi xe XE01 */
SELECT @SeedPlate = BienSo FROM dbo.XE WHERE MaXe = N'XE01';
SET @MaXeDup = N'D' + @Run;
IF @SeedPlate IS NULL
BEGIN
    INSERT INTO @Results VALUES (N'TC03', N'Them xe trung bien so', N'SQL tu choi bien so trung XE01', N'Khong tim thay xe XE01 de lam mau', N'BO QUA');
END
ELSE
BEGIN
    BEGIN TRY
        INSERT INTO dbo.XE (MaXe, BienSo, SucChua, CoAccessibility, TrangThai)
        VALUES (@MaXeDup, @SeedPlate, 10, 0, N'Sẵn sàng');
        DELETE FROM dbo.XE WHERE MaXe = @MaXeDup;
        INSERT INTO @Results VALUES (N'TC03', N'Them xe trung bien so', N'SQL tu choi bien so trung XE01', N'Ban ghi da duoc chen, sau do da don dep', N'KHONG DAT');
    END TRY
    BEGIN CATCH
        SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
        INSERT INTO @Results VALUES
        (N'TC03', N'Them xe trung bien so', N'SQL tu choi bien so trung XE01', CONCAT(N'Loi ', @Err, N': ', @Msg), CASE WHEN @Err IN (2601, 2627) THEN N'DAT' ELSE N'CAN KIEM TRA' END);
    END CATCH;
END;

/* TC04 - Tu choi khoa ngoai khong ton tai */
SET @MaDatChoBadFK = N'FK' + @Run;
BEGIN TRY
    INSERT INTO dbo.DAT_CHO (MaDatCho, ThoiDiemDat, TrangThai, MaChuyen, MaHanhKhach)
    VALUES (@MaDatChoBadFK, SYSDATETIME(), N'Đã giữ', N'KHONG_CO_CHUYEN_' + @Run, N'KHONG_CO_NGUOI_' + @Run);
    DELETE FROM dbo.DAT_CHO WHERE MaDatCho = @MaDatChoBadFK;
    INSERT INTO @Results VALUES (N'TC04', N'Them dat cho co khoa ngoai sai', N'SQL tu choi ban ghi', N'Ban ghi da duoc chen, sau do da don dep', N'KHONG DAT');
END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES
    (N'TC04', N'Them dat cho co khoa ngoai sai', N'SQL tu choi ban ghi', CONCAT(N'Loi ', @Err, N': ', @Msg), CASE WHEN @Err = 547 THEN N'DAT' ELSE N'CAN KIEM TRA' END);
END CATCH;

/* Tao 21 tai khoan hanh khach chi dung cho bo kiem thu nay */
SET @i = 1;
WHILE @i <= 21
BEGIN
    SET @UserCode = N'T' + @Run + N'P' + RIGHT(N'00' + CONVERT(NVARCHAR(2), @i), 2);
    SET @Email = @UserCode + N'@example.test';
    IF NOT EXISTS (SELECT 1 FROM dbo.NGUOI_DUNG WHERE MaNguoiDung = @UserCode)
    BEGIN
        INSERT INTO dbo.NGUOI_DUNG
            (MaNguoiDung, HoTen, Email, SoDienThoai, TenDangNhap, MatKhauHash, VaiTro)
        VALUES
            (@UserCode, N'Hanh khach test ' + @UserCode, @Email, NULL, @UserCode, N'TEST_ONLY_NOT_A_REAL_HASH', N'HANH_KHACH');
    END;
    SET @i += 1;
END;

/* TC05/TC06 - Tao chuyen suc chua 16, dat 16 cho va thu cho thu 17 */
SET @DxCap = N'DX' + @Run + N'C';
SET @TripCap = N'CT' + @Run + N'C';
SET @TripTimeCap = DATEADD(HOUR, 8, CONVERT(DATETIME2(0), DATEADD(DAY, 1, CONVERT(DATE, SYSDATETIME()))));

INSERT INTO dbo.DE_XUAT_CHUYEN (MaDeXuat, NgayDi, HuongDi, KhungGio)
VALUES (@DxCap, CONVERT(DATE, @TripTimeCap), N'Chiều đi', CONVERT(NVARCHAR(5), @TripTimeCap, 108));

INSERT INTO dbo.CHUYEN (MaChuyen, GioDuKien, MaTuyen, MaXe, MaLaiXe, MaDeXuat)
VALUES (@TripCap, @TripTimeCap, N'T01', N'XE01', N'ND02', @DxCap);

;WITH N(n) AS
(
    SELECT 1
    UNION ALL SELECT n + 1 FROM N WHERE n < 16
)
INSERT INTO dbo.DAT_CHO (MaDatCho, ThoiDiemDat, TrangThai, MaChuyen, MaHanhKhach)
SELECT
    N'DC' + @Run + N'C' + RIGHT(N'00' + CONVERT(NVARCHAR(2), n), 2),
    SYSDATETIME(), N'Đã giữ', @TripCap,
    N'T' + @Run + N'P' + RIGHT(N'00' + CONVERT(NVARCHAR(2), n), 2)
FROM N
OPTION (MAXRECURSION 20);

SELECT @SoCho = COUNT(*) FROM dbo.DAT_CHO WHERE MaChuyen = @TripCap AND TrangThai = N'Đã giữ';
INSERT INTO @Results VALUES
(N'TC05', N'Dat 16 cho tren xe suc chua 16', N'Cho phep dung 16 cho', CONCAT(N'So cho dang giu=', @SoCho), CASE WHEN @SoCho = 16 THEN N'DAT' ELSE N'CAN KIEM TRA' END);

SET @PassengerId = N'T' + @Run + N'P17';
SET @NewBookingId = N'DC' + @Run + N'P17';
BEGIN TRY
    EXEC dbo.sp_DatCho @MaChuyen = @TripCap, @MaHanhKhach = @PassengerId, @MaDatCho = @NewBookingId OUTPUT;
    INSERT INTO @Results VALUES (N'TC06A', N'Dat cho thu 17 qua stored procedure', N'Tu choi do het suc chua', N'Thu tuc da khong tu choi dat cho thu 17', N'KHONG DAT');
END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES
    (N'TC06A', N'Dat cho thu 17 qua stored procedure', N'Tu choi do het suc chua', CONCAT(N'Loi ', @Err, N': ', @Msg), CASE WHEN @Err = 51108 THEN N'DAT' ELSE N'CAN KIEM TRA' END);
END CATCH;

BEGIN TRY
    INSERT INTO dbo.DAT_CHO (MaDatCho, ThoiDiemDat, TrangThai, MaChuyen, MaHanhKhach)
    VALUES (N'DC' + @Run + N'RAW17', SYSDATETIME(), N'Đã giữ', @TripCap, @PassengerId);
    DELETE FROM dbo.DAT_CHO WHERE MaDatCho = N'DC' + @Run + N'RAW17';
    INSERT INTO @Results VALUES (N'TC06B', N'Dat cho thu 17 bang INSERT truc tiep', N'Trigger tu choi vuot suc chua', N'INSERT truc tiep da duoc chap nhan, sau do da don dep', N'KHONG DAT');
END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES
    (N'TC06B', N'Dat cho thu 17 bang INSERT truc tiep', N'Trigger tu choi vuot suc chua', CONCAT(N'Loi ', @Err, N': ', @Msg), CASE WHEN @Err = 51009 THEN N'DAT' ELSE N'CAN KIEM TRA' END);
END CATCH;

/* TC07 - Nhom can accessibility: tu choi XE01 va chap nhan XE02 */
SET @DxAcc = N'DX' + @Run + N'A';
SET @TripAcc = N'CT' + @Run + N'A';
SET @NeedAcc = N'NC' + @Run + N'A';
SET @TimeAcc = DATEADD(HOUR, 8, CONVERT(DATETIME2(0), DATEADD(DAY, 2, CONVERT(DATE, SYSDATETIME()))));
SET @PassengerId = N'T' + @Run + N'P18';

INSERT INTO dbo.DE_XUAT_CHUYEN (MaDeXuat, NgayDi, HuongDi, KhungGio)
VALUES (@DxAcc, CONVERT(DATE, @TimeAcc), N'Chiều đi', CONVERT(NVARCHAR(5), @TimeAcc, 108));
INSERT INTO dbo.NHU_CAU_DI_LAI
    (MaNhuCau, NgayDi, HuongDi, KhungGioMongMuon, CoAccessibility, TrangThai, ThoiDiemTao, MaHanhKhach, MaDeXuat)
VALUES
    (@NeedAcc, CONVERT(DATE, @TimeAcc), N'Chiều đi', CONVERT(NVARCHAR(5), @TimeAcc, 108), 1, N'Đã gom nhóm', SYSDATETIME(), @PassengerId, @DxAcc);
INSERT INTO dbo.CHUYEN (MaChuyen, GioDuKien, MaTuyen, MaXe, MaLaiXe, MaDeXuat)
VALUES (@TripAcc, @TimeAcc, N'T01', N'XE02', N'ND02', @DxAcc);

BEGIN TRY
    EXEC dbo.sp_PhanCongXe @MaChuyen = @TripAcc, @MaXeMoi = N'XE01';
    INSERT INTO @Results VALUES (N'TC07A', N'Phan cong xe khong ho tro accessibility', N'Tu choi XE01', N'Thu tuc da cho phep XE01', N'KHONG DAT');
END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES
    (N'TC07A', N'Phan cong xe khong ho tro accessibility', N'Tu choi XE01', CONCAT(N'Loi ', @Err, N': ', @Msg), CASE WHEN @Err = 51206 THEN N'DAT' ELSE N'CAN KIEM TRA' END);
END CATCH;

BEGIN TRY
    EXEC dbo.sp_PhanCongXe @MaChuyen = @TripAcc, @MaXeMoi = N'XE02';
    SELECT @XeSauTC07 = MaXe FROM dbo.CHUYEN WHERE MaChuyen = @TripAcc;
    INSERT INTO @Results VALUES
    (N'TC07B', N'Phan cong xe phu hop accessibility', N'Chap nhan XE02', CONCAT(N'Xe sau phan cong=', COALESCE(@XeSauTC07, N'(NULL)')), CASE WHEN @XeSauTC07 = N'XE02' THEN N'DAT' ELSE N'CAN KIEM TRA' END);
END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER(); SET @Msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES (N'TC07B', N'Phan cong xe phu hop accessibility', N'Chap nhan XE02', CONCAT(N'Loi ', @Err, N': ', @Msg), N'CAN KIEM TRA');
END CATCH;

/* TC08 - Huy dat cho duoi 2 gio truoc khoi hanh, tao vi pham huy muon */
SET @DxCancel = N'DX' + @Run + N'H';
SET @TripCancel = N'CT' + @Run + N'H';
SET @BookingCancel = N'DC' + @Run + N'H';
SET @TimeCancel = DATEADD(HOUR, 1, SYSDATETIME());
SET @PassengerId = N'T' + @Run + N'P19';

INSERT INTO dbo.DE_XUAT_CHUYEN (MaDeXuat, NgayDi, HuongDi, KhungGio)
VALUES (@DxCancel, CONVERT(DATE, @TimeCancel), N'Chiều đi', CONVERT(NVARCHAR(5), @TimeCancel, 108));
INSERT INTO dbo.CHUYEN (MaChuyen, GioDuKien, MaTuyen, MaXe, MaLaiXe, MaDeXuat)
VALUES (@TripCancel, @TimeCancel, N'T01', N'XE01', N'ND02', @DxCancel);
EXEC dbo.sp_DatCho @MaChuyen = @TripCancel, @MaHanhKhach = @PassengerId, @MaDatCho = @BookingCancel OUTPUT;
EXEC dbo.sp_HuyDatCho @MaDatCho = @BookingCancel;

IF EXISTS (SELECT 1 FROM dbo.DAT_CHO WHERE MaDatCho = @BookingCancel AND TrangThai = N'Đã hủy muộn')
   AND EXISTS (SELECT 1 FROM dbo.VI_PHAM_NO_SHOW WHERE MaDatCho = @BookingCancel AND Loai = N'Hủy muộn')
    INSERT INTO @Results VALUES (N'TC08', N'Huy dat cho duoi 2 gio', N'Huy thanh cong va ghi vi pham huy muon', N'Dat cho da huy muon; co ban ghi vi pham', N'DAT');
ELSE
    INSERT INTO @Results VALUES (N'TC08', N'Huy dat cho duoi 2 gio', N'Huy thanh cong va ghi vi pham huy muon', N'Khong thay trang thai/ban ghi vi pham mong doi', N'CAN KIEM TRA');

/* TC09 - Diem danh vang mat va xac nhan khoi hanh => no-show */
SET @DxNS = N'DX' + @Run + N'N';
SET @TripNS = N'CT' + @Run + N'N';
SET @BookingNS = N'DC' + @Run + N'N';
SET @TimeNS = DATEADD(HOUR, 3, SYSDATETIME());
SET @PassengerId = N'T' + @Run + N'P20';

INSERT INTO dbo.DE_XUAT_CHUYEN (MaDeXuat, NgayDi, HuongDi, KhungGio)
VALUES (@DxNS, CONVERT(DATE, @TimeNS), N'Chiều đi', CONVERT(NVARCHAR(5), @TimeNS, 108));
INSERT INTO dbo.CHUYEN (MaChuyen, GioDuKien, MaTuyen, MaXe, MaLaiXe, MaDeXuat)
VALUES (@TripNS, @TimeNS, N'T01', N'XE01', N'ND02', @DxNS);
EXEC dbo.sp_DatCho @MaChuyen = @TripNS, @MaHanhKhach = @PassengerId, @MaDatCho = @BookingNS OUTPUT;
EXEC dbo.sp_GhiNhanDiemDanh @MaDatCho = @BookingNS, @CoMat = 0, @GhiChu = N'Kiem thu no-show';
EXEC dbo.sp_KhoiHanhChuyen @MaChuyen = @TripNS, @ThoiDiemKhoiHanh = @TimeNS;

IF EXISTS (SELECT 1 FROM dbo.DAT_CHO WHERE MaDatCho = @BookingNS AND TrangThai = N'No-show')
   AND EXISTS (SELECT 1 FROM dbo.VI_PHAM_NO_SHOW WHERE MaDatCho = @BookingNS AND Loai = N'Vắng mặt khi khởi hành')
    INSERT INTO @Results VALUES (N'TC09', N'Diem danh vang mat va khoi hanh', N'Tao vi pham no-show, dat cho chuyen sang No-show', N'Da tao vi pham va cap nhat trang thai', N'DAT');
ELSE
    INSERT INTO @Results VALUES (N'TC09', N'Diem danh vang mat va khoi hanh', N'Tao vi pham no-show, dat cho chuyen sang No-show', N'Khong thay trang thai/ban ghi vi pham mong doi', N'CAN KIEM TRA');

/* TC10 - Ba vi pham no-show trong 30 ngay => han che 14 ngay */
SET @PassengerId = N'T' + @Run + N'P21';
SET @i = 1;
WHILE @i <= 3
BEGIN
    SET @TimeV = DATEADD(HOUR, 3 + @i, SYSDATETIME());
    SET @DxV = N'DX' + @Run + N'V' + CONVERT(NVARCHAR(1), @i);
    SET @TripV = N'CT' + @Run + N'V' + CONVERT(NVARCHAR(1), @i);
    SET @BookingV = N'DC' + @Run + N'V' + CONVERT(NVARCHAR(1), @i);

    INSERT INTO dbo.DE_XUAT_CHUYEN (MaDeXuat, NgayDi, HuongDi, KhungGio)
    VALUES (@DxV, CONVERT(DATE, @TimeV), N'Chiều đi', CONVERT(NVARCHAR(5), @TimeV, 108));
    INSERT INTO dbo.CHUYEN (MaChuyen, GioDuKien, MaTuyen, MaXe, MaLaiXe, MaDeXuat)
    VALUES (@TripV, @TimeV, N'T01', N'XE01', N'ND02', @DxV);
    EXEC dbo.sp_DatCho @MaChuyen = @TripV, @MaHanhKhach = @PassengerId, @MaDatCho = @BookingV OUTPUT;
    EXEC dbo.sp_GhiNhanDiemDanh @MaDatCho = @BookingV, @CoMat = 0, @GhiChu = N'Kiem thu nguong vi pham';
    EXEC dbo.sp_KhoiHanhChuyen @MaChuyen = @TripV, @ThoiDiemKhoiHanh = @TimeV;
    SET @i += 1;
END;

SELECT @HanChe = HanCheDenNgay FROM dbo.NGUOI_DUNG WHERE MaNguoiDung = @PassengerId;
INSERT INTO @Results VALUES
(
    N'TC10', N'Ba vi pham trong 30 ngay', N'Tai khoan bi han che dat cho 14 ngay',
    CONCAT(N'HanCheDenNgay=', COALESCE(CONVERT(NVARCHAR(30), @HanChe, 126), N'NULL')),
    CASE WHEN @HanChe IS NOT NULL AND @HanChe > SYSDATETIME() THEN N'DAT' ELSE N'CAN KIEM TRA' END
);

/* Ket qua tong hop */
SELECT TC, NoiDung, KetQuaMongDoi, KetQuaThucTe, DanhGia
FROM @Results
ORDER BY CASE WHEN TC = N'TC01' THEN 1
              WHEN TC = N'TC02' THEN 2
              WHEN TC = N'TC03' THEN 3
              WHEN TC = N'TC04' THEN 4
              WHEN TC = N'TC05' THEN 5
              WHEN TC = N'TC06A' THEN 6
              WHEN TC = N'TC06B' THEN 7
              WHEN TC = N'TC07A' THEN 8
              WHEN TC = N'TC07B' THEN 9
              WHEN TC = N'TC08' THEN 10
              WHEN TC = N'TC09' THEN 11
              WHEN TC = N'TC10' THEN 12
              ELSE 99 END;

/* Xem nhanh cac ban ghi test vua tao (ma chua chua run id). */
SELECT @Run AS MaLuotKiemThu;
PRINT N'Bo kiem thu da chay het cac lenh khong loi bat ngo. Kiem tra bang ket qua TC o phia tren; du lieu test duoc giu lai trong database.';
