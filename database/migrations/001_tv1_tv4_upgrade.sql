-- =============================================================================
-- Migration 001: Nâng cấp Schema & Dữ liệu chuẩn cho Phân hệ TV1 & TV4
-- Chạy an toàn nhiều lần (Idempotent) trên mọi phiên bản SQL Server
-- =============================================================================
USE [DACN];
GO

-- 1. Sửa cột LoaiThoiTiet từ varchar(30) sang nvarchar(50) để không lỗi dấu tiếng Việt
IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[GoiYDiaDiemTheoMua_AI]') AND name = N'LoaiThoiTiet'
)
BEGIN
    ALTER TABLE [dbo].[GoiYDiaDiemTheoMua_AI] ALTER COLUMN [LoaiThoiTiet] NVARCHAR(50) NULL;
END
GO

-- Khôi phục các chuỗi tiếng Việt bị lỗi '?' trong bảng GoiYDiaDiemTheoMua_AI
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Nắng đẹp, biển êm' WHERE [MaID] = 1;
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Mát mẻ về đêm' WHERE [MaID] = 2;
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Trong lành, ít mưa' WHERE [MaID] = 3;
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Se lạnh, săn mây' WHERE [MaID] = 4;
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Nắng ấm, biển lặng' WHERE [MaID] = 5;
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Mát dịu, sương sớm' WHERE [MaID] >= 6 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);
GO

-- 2. Thêm cột PhongCach (Gu trải nghiệm: songao, thugian, phieuluu, amthuc, giadinh) vào GoiYTour_AI & DiaDiemThamQuan
IF COL_LENGTH('dbo.GoiYTour_AI', 'PhongCach') IS NULL
BEGIN
    ALTER TABLE [dbo].[GoiYTour_AI] ADD [PhongCach] NVARCHAR(200) NULL;
END
GO

IF COL_LENGTH('dbo.DiaDiemThamQuan', 'PhongCach') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [PhongCach] NVARCHAR(200) NULL;
END
GO

-- Cập nhật dữ liệu Gu trải nghiệm (PhongCach) cho các Tour và Địa điểm hiện có
UPDATE [dbo].[GoiYTour_AI] SET [PhongCach] = N'thugian,songao,giadinh' WHERE [MaID] = 1 AND [PhongCach] IS NULL;
UPDATE [dbo].[GoiYTour_AI] SET [PhongCach] = N'songao,amthuc,capdoi' WHERE [MaID] = 2 AND [PhongCach] IS NULL;
UPDATE [dbo].[GoiYTour_AI] SET [PhongCach] = N'phieuluu,songao' WHERE [MaID] = 3 AND [PhongCach] IS NULL;
UPDATE [dbo].[GoiYTour_AI] SET [PhongCach] = N'thugian,giadinh' WHERE [MaID] = 4 AND [PhongCach] IS NULL;
UPDATE [dbo].[GoiYTour_AI] SET [PhongCach] = N'songao,thugian,amthuc' WHERE [PhongCach] IS NULL;

UPDATE [dbo].[DiaDiemThamQuan] SET [PhongCach] = N'thugian,songao,giadinh' WHERE [MaID] = 1 AND [PhongCach] IS NULL;
UPDATE [dbo].[DiaDiemThamQuan] SET [PhongCach] = N'songao,amthuc' WHERE [MaID] = 2 AND [PhongCach] IS NULL;
UPDATE [dbo].[DiaDiemThamQuan] SET [PhongCach] = N'thugian,songao,phieuluu' WHERE [MaID] = 3 AND [PhongCach] IS NULL;
UPDATE [dbo].[DiaDiemThamQuan] SET [PhongCach] = N'songao,phieuluu,giadinh' WHERE [MaID] = 4 AND [PhongCach] IS NULL;
UPDATE [dbo].[DiaDiemThamQuan] SET [PhongCach] = N'songao,amthuc,thugian' WHERE [PhongCach] IS NULL;
GO

-- 3. Thêm cột GiamToiDa (Mức giảm tối đa cho voucher phần trăm) vào bảng MaGiamGia
IF COL_LENGTH('dbo.MaGiamGia', 'GiamToiDa') IS NULL
BEGIN
    ALTER TABLE [dbo].[MaGiamGia] ADD [GiamToiDa] DECIMAL(18, 2) NULL;
END
GO

UPDATE [dbo].[MaGiamGia]
SET [GiamToiDa] = 500000
WHERE [LoaiGiamGia] = N'percent' AND [GiamToiDa] IS NULL;
GO
