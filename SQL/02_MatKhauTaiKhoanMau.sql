/*
  TAI KHOAN DEMO CHO DO AN - CHI DUNG TREN DATABASE THU NGHIEM.
  Mat khau chung: Demo123!
  Hay thay mat khau sau khi test. Khong dung cac tai khoan nay tren moi truong that.
*/
USE BusOnDemandDB_Test;
GO
UPDATE dbo.NGUOI_DUNG SET MatKhauHash=N'PBKDF2$120000$BVh4W+Mw3DjvlOv54dCApw==$/l4OnD5zi5IqCpOZASooTPt6bEzudcAI5zqGrfI2I88=' WHERE TenDangNhap=N'nd01';
UPDATE dbo.NGUOI_DUNG SET MatKhauHash=N'PBKDF2$120000$d9YIB4eO0dPFkKN62y0iFw==$FSN+mmS++wJVlabbLMgSrfhO/6P/ut1TCDFj3R7QIM8=' WHERE TenDangNhap=N'nd02';
UPDATE dbo.NGUOI_DUNG SET MatKhauHash=N'PBKDF2$120000$OND1ryrRUxl54QnvNxQ5sg==$ShlJ6ad+s1+0FIH+8Mn6UXlC6n8l4lJB8MKwwqfBRTI=' WHERE TenDangNhap=N'nd03';
GO
SELECT TenDangNhap,VaiTro FROM dbo.NGUOI_DUNG WHERE TenDangNhap IN (N'nd01',N'nd02',N'nd03');
GO
