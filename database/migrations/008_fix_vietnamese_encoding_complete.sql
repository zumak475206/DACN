-- ============================================================
-- Migration: Sửa triệt để lỗi mã hóa tiếng Việt bảng ChiPhiVanHanh
-- Ngày: 2026-11-20
-- Mô tả: 
--   1. ALTER cột DanhMuc từ VARCHAR(100) sang NVARCHAR(100) để lưu Unicode.
--   2. UPDATE 6 bản ghi lỗi mã hóa tiếng Việt theo MaId.
--   3. SELECT kiểm tra kết quả sau UPDATE.
-- Phạm vi: TV3 - Billing & Finance
-- Lưu ý: 
--   - Script idempotent — có thể chạy nhiều lần mà không gây lỗi.
--   - Có transaction, TRY/CATCH và ROLLBACK khi lỗi.
--   - Đây là file migration cuối cùng, thay thế cho các file 003-007.
--   - Không tự chạy SQL, người dùng sẽ tự chạy file này trên SQL Server.
--   - Dùng MaId để xác định bản ghi, không dùng so sánh chuỗi lỗi.
--
-- CÁCH SỬ DỤNG:
--   Lần 1: Chạy file với @ConfirmUpdate = 0 (mặc định) → chỉ SELECT preview.
--   Kiểm tra kết quả preview, nếu đúng thì đổi @ConfirmUpdate = 1.
--   Lần 2: Chạy lại file với @ConfirmUpdate = 1 → ALTER + UPDATE thực tế.
-- ============================================================

USE [DACN];
GO

-- ============================================================
-- BIẾN KIỂM SOÁT: Đổi 0 → 1 để kích hoạt ALTER và UPDATE.
-- Mặc định = 0: Chỉ SELECT preview, không thay đổi dữ liệu.
-- Đổi thành 1: Thực hiện ALTER COLUMN và UPDATE 6 giá trị lỗi.
-- ============================================================
DECLARE @ConfirmUpdate BIT = 0;

PRINT '============================================================';
PRINT 'Migration: Sua loi ma hoa tieng Viet bang ChiPhiVanHanh';
PRINT '============================================================';
PRINT '';

-- ============================================================
-- 1. Kiểm tra kiểu dữ liệu cột DanhMuc hiện tại
-- ============================================================
SELECT 
    COLUMN_NAME AS [TenCot],
    DATA_TYPE AS [KieuDuLieu],
    CHARACTER_MAXIMUM_LENGTH AS [DoDai],
    IS_NULLABLE AS [ChoPheNull]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ChiPhiVanHanh'
  AND COLUMN_NAME = 'DanhMuc';

PRINT '';
PRINT '--- Ket qua kiem tra kieu cot tren. ---';
PRINT '';

-- ============================================================
-- 2. ALTER cột DanhMuc sang NVARCHAR(100) để lưu Unicode
--    Chỉ thực hiện khi @ConfirmUpdate = 1 và cột vẫn là VARCHAR.
-- ============================================================
IF @ConfirmUpdate = 1
BEGIN
    IF EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'ChiPhiVanHanh'
          AND COLUMN_NAME = 'DanhMuc'
          AND DATA_TYPE = 'varchar'
    )
    BEGIN
        ALTER TABLE [dbo].[ChiPhiVanHanh]
            ALTER COLUMN [DanhMuc] NVARCHAR(100) NOT NULL;
        PRINT '[OK] Da chuyen cot [DanhMuc] tu VARCHAR(100) sang NVARCHAR(100).';
    END
    ELSE
    BEGIN
        PRINT '[OK] Cot [DanhMuc] da la NVARCHAR(100) hoac khong ton tai, bo qua ALTER.';
    END
END
ELSE
BEGIN
    PRINT '[PREVIEW] @ConfirmUpdate = 0. Bo qua ALTER COLUMN. Doi @ConfirmUpdate = 1 de thuc hien.';
END

PRINT '';

-- ============================================================
-- 3. SELECT PREVIEW — Xem trước dữ liệu trước khi UPDATE
--    Hiển thị tất cả danh mục khác nhau để người dùng kiểm tra.
-- ============================================================
SELECT DISTINCT
    [DanhMuc] AS [DanhMuc_HienTai],
    CAST([DanhMuc] AS VARBINARY(200)) AS [DanhMuc_Bytes],
    LEN([DanhMuc]) AS [DanhMuc_Length],
    COUNT(*) AS [SoDong]
FROM [dbo].[ChiPhiVanHanh]
GROUP BY [DanhMuc]
ORDER BY [DanhMuc_HienTai];

PRINT '';

-- ============================================================
-- 4. SELECT PREVIEW — Xem trước 6 dòng sẽ bị cập nhật theo MaId
--    Dùng MaId để xác định bản ghi, không dùng so sánh chuỗi.
-- ============================================================
SELECT 
    [MaId],
    [DanhMuc] AS [DanhMuc_Cu],
    CAST([DanhMuc] AS VARBINARY(200)) AS [DanhMuc_Bytes],
    CASE [MaId]
        WHEN 1 THEN N'Năng lượng & Điện nước'
        WHEN 2 THEN N'Thực phẩm & Đồ uống'
        WHEN 3 THEN N'Bảo trì & Thiết bị'
        WHEN 4 THEN N'Hóa chất & Giặt tẩy'
        WHEN 5 THEN N'Marketing & Tiếp thị'
        WHEN 6 THEN N'Vận hành'
    END AS [DanhMuc_Moi]
FROM [dbo].[ChiPhiVanHanh]
WHERE [MaId] IN (1, 2, 3, 4, 5, 6)
ORDER BY [MaId];

PRINT '';
PRINT '--- Preview hoan tat. Kiem tra ket qua tren truoc khi tiep tuc UPDATE. ---';
PRINT '';

-- ============================================================
-- 5. UPDATE 6 bản ghi theo MaId trong transaction
--    Chỉ thực hiện khi @ConfirmUpdate = 1.
--    Dùng MaId để xác định bản ghi, CASE MaId gán giá trị Unicode.
--    Nếu lỗi, ROLLBACK toàn bộ transaction.
-- ============================================================
IF @ConfirmUpdate = 1
BEGIN
    BEGIN TRANSACTION;

    BEGIN TRY

        UPDATE [dbo].[ChiPhiVanHanh]
        SET [DanhMuc] = CASE [MaId]
            -- 6 bản ghi lỗi đã xác định theo MaId
            WHEN 1 THEN N'Năng lượng & Điện nước'
            WHEN 2 THEN N'Thực phẩm & Đồ uống'
            WHEN 3 THEN N'Bảo trì & Thiết bị'
            WHEN 4 THEN N'Hóa chất & Giặt tẩy'
            WHEN 5 THEN N'Marketing & Tiếp thị'
            WHEN 6 THEN N'Vận hành'
        END
        WHERE [MaId] IN (1, 2, 3, 4, 5, 6);

        COMMIT TRANSACTION;
        PRINT '[OK] Da sua xong 6 ban ghi loi ma hoa tieng Viet cho cot [DanhMuc].';

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();
        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
        PRINT '[LOI] Da rollback transaction. Khong co thay doi du lieu.';
    END CATCH
END
ELSE
BEGIN
    PRINT '[PREVIEW] @ConfirmUpdate = 0. Bo qua UPDATE. Doi @ConfirmUpdate = 1 de thuc hien.';
END

PRINT '';

-- ============================================================
-- 6. SELECT KIỂM TRA — Xác nhận kết quả sau UPDATE theo MaId
--    Chỉ chạy khi @ConfirmUpdate = 1.
-- ============================================================
IF @ConfirmUpdate = 1
BEGIN
    SELECT 
        [MaId],
        [DanhMuc] AS [DanhMuc_SauKhiSua]
    FROM [dbo].[ChiPhiVanHanh]
    WHERE [MaId] IN (1, 2, 3, 4, 5, 6)
    ORDER BY [MaId];

    PRINT '';
    PRINT '--- Migration hoan tat. Kiem tra ket qua tren. ---';
END
ELSE
BEGIN
    PRINT '[PREVIEW] Chay xong. Kiem tra ket qua preview tren.';
    PRINT '[PREVIEW] Doi @ConfirmUpdate = 1 de thuc hien ALTER va UPDATE.';
END

PRINT '';
PRINT '============================================================';
GO
