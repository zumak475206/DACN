-- ============================================================
-- Migration: Tạo bảng NhatKyHeThong (Audit Log)
-- Ngày: 2026-11-20
-- Mô tả: Tạo bảng nhật ký hệ thống để lưu audit log
--          cho AuditService ghi nhận các hành động người dùng.
-- Phạm vi: TV3 - Billing & Finance
-- Lưu ý: Script idempotent — có thể chạy nhiều lần mà không gây lỗi.
-- ============================================================

USE [DACN];
GO

-- ============================================================
-- 1. Tạo bảng NhatKyHeThong nếu chưa tồn tại
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NhatKyHeThong' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[NhatKyHeThong] (
        [MaId]        INT IDENTITY(1,1) NOT NULL,
        [MaNguoiDung] INT NULL,
        [Email]       NVARCHAR(255) NULL,
        [HanhDong]    NVARCHAR(100) NOT NULL,
        [DoiTuong]    NVARCHAR(100) NOT NULL,
        [KhoaChinh]   NVARCHAR(100) NULL,
        [NoiDung]     NVARCHAR(MAX) NULL,
        [DiaChiIp]    NVARCHAR(50) NULL,
        [ThanhCong]   BIT NOT NULL DEFAULT 1,
        [ThoiGian]    DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_NhatKyHeThong] PRIMARY KEY CLUSTERED ([MaId] ASC)
    );
    PRINT 'Da tao bang [dbo].[NhatKyHeThong] thanh cong.';
END
ELSE
BEGIN
    PRINT 'Bang [dbo].[NhatKyHeThong] da ton tai, bo qua CREATE TABLE.';
END
GO

-- ============================================================
-- 2. Tạo Foreign Key đến bảng NguoiDung (nếu chưa tồn tại)
-- ============================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_NhatKyHeThong_NguoiDung'
      AND parent_object_id = OBJECT_ID('dbo.NhatKyHeThong')
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.tables
        WHERE name = 'NguoiDung'
          AND schema_id = SCHEMA_ID('dbo')
    )
    BEGIN
        ALTER TABLE [dbo].[NhatKyHeThong]
            ADD CONSTRAINT [FK_NhatKyHeThong_NguoiDung]
            FOREIGN KEY ([MaNguoiDung])
            REFERENCES [dbo].[NguoiDung]([MaId])
            ON DELETE SET NULL;
        PRINT 'Da tao foreign key [FK_NhatKyHeThong_NguoiDung] thanh cong.';
    END
    ELSE
    BEGIN
        PRINT 'Khong tim thay bang [dbo].[NguoiDung]. Bo qua tao foreign key.';
    END
END
ELSE
BEGIN
    PRINT 'Foreign key [FK_NhatKyHeThong_NguoiDung] da ton tai, bo qua.';
END
GO

-- ============================================================
-- 3. Tạo Index theo thời gian (nếu chưa tồn tại)
-- ============================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_NhatKyHeThong_ThoiGian'
      AND object_id = OBJECT_ID('dbo.NhatKyHeThong')
)
BEGIN
    CREATE INDEX [IX_NhatKyHeThong_ThoiGian] ON [dbo].[NhatKyHeThong]([ThoiGian] DESC);
    PRINT 'Da tao index [IX_NhatKyHeThong_ThoiGian] thanh cong.';
END
ELSE
BEGIN
    PRINT 'Index [IX_NhatKyHeThong_ThoiGian] da ton tai, bo qua.';
END
GO

-- ============================================================
-- 4. Tạo Index theo người dùng (nếu chưa tồn tại)
-- ============================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_NhatKyHeThong_MaNguoiDung'
      AND object_id = OBJECT_ID('dbo.NhatKyHeThong')
)
BEGIN
    CREATE INDEX [IX_NhatKyHeThong_MaNguoiDung] ON [dbo].[NhatKyHeThong]([MaNguoiDung]);
    PRINT 'Da tao index [IX_NhatKyHeThong_MaNguoiDung] thanh cong.';
END
ELSE
BEGIN
    PRINT 'Index [IX_NhatKyHeThong_MaNguoiDung] da ton tai, bo qua.';
END
GO

-- ============================================================
-- 5. Tạo Index theo hành động (nếu chưa tồn tại)
-- ============================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_NhatKyHeThong_HanhDong'
      AND object_id = OBJECT_ID('dbo.NhatKyHeThong')
)
BEGIN
    CREATE INDEX [IX_NhatKyHeThong_HanhDong] ON [dbo].[NhatKyHeThong]([HanhDong]);
    PRINT 'Da tao index [IX_NhatKyHeThong_HanhDong] thanh cong.';
END
ELSE
BEGIN
    PRINT 'Index [IX_NhatKyHeThong_HanhDong] da ton tai, bo qua.';
END
GO
