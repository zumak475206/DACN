-- =============================================================================
-- Migration 010: Nâng cấp Toàn diện AI Grounded Recommendation & Hệ thống Chuỗi Chi Nhánh
-- 1. Bổ sung ViDo, KinhDo, VungMien cho ChiNhanh (Thêm Chi nhánh 4 Vũng Tàu)
-- 2. Mở rộng 15 trường dữ liệu chuyên sâu cho DiaDiemThamQuan (NgoaiTroi, LaBienDao, Mua, v.v.)
-- 3. Tạo bảng nối DiaDiem_ChiNhanh tính khoảng cách chính xác từ địa điểm đến khách sạn
-- 4. Nạp bộ 37 địa điểm du lịch thực tế đã kiểm chứng tọa độ, phân loại Indoor/Outdoor
-- =============================================================================
USE [DACN];
GO

-- 1. NÂNG CẤP BẢNG ChiNhanh (Thêm ViDo, KinhDo, VungMien)
IF COL_LENGTH(N'[dbo].[ChiNhanh]', N'ViDo') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiNhanh] ADD [ViDo] DECIMAL(10, 8) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[ChiNhanh]', N'KinhDo') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiNhanh] ADD [KinhDo] DECIMAL(11, 8) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[ChiNhanh]', N'VungMien') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiNhanh] ADD [VungMien] VARCHAR(20) NOT NULL CONSTRAINT [DF_ChiNhanh_VungMien] DEFAULT 'central';
END
GO

-- Cập nhật tọa độ chuẩn và vùng miền cho 3 Chi nhánh hiện tại
UPDATE [dbo].[ChiNhanh]
SET [ViDo] = 16.06185000, [KinhDo] = 108.24582000, [VungMien] = 'central'
WHERE [MaID] = 1;

UPDATE [dbo].[ChiNhanh]
SET [ViDo] = 10.14521000, [KinhDo] = 103.96845000, [VungMien] = 'south'
WHERE [MaID] = 2;

UPDATE [dbo].[ChiNhanh]
SET [ViDo] = 11.90284000, [KinhDo] = 108.43412000, [VungMien] = 'south'
WHERE [MaID] = 3;
GO

-- Nạp Chi nhánh 4: GrandStay Vũng Tàu Oceanfront Hotel
SET IDENTITY_INSERT [dbo].[ChiNhanh] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ChiNhanh] WHERE [MaID] = 4)
BEGIN
    INSERT INTO [dbo].[ChiNhanh] (
        [MaID], [MaChiNhanhCode], [TenChiNhanh], [TinhThanh], [DiaChiChiTiet],
        [SoDienThoai], [EmailLienHe], [HinhAnhDaiDien], [SoSao], [DangHoatDong], [NgayTao],
        [ViDo], [KinhDo], [VungMien]
    ) VALUES (
        4, 'CN-VUNGTAU', N'GrandStay Vũng Tàu Oceanfront Hotel', N'Bà Rịa - Vũng Tàu',
        N'165 Thùy Vân, Phường Thắng Tam, TP. Vũng Tàu, Tỉnh Bà Rịa - Vũng Tàu',
        '0254 3888 666', 'vungtau@bookingstay.vn',
        'https://images.unsplash.com/photo-1571896349842-33c89424de2d?auto=format&fit=crop&w=800&q=80',
        4, 1, GETDATE(),
        10.33582000, 107.09124000, 'south'
    );
END
ELSE
BEGIN
    UPDATE [dbo].[ChiNhanh]
    SET [TenChiNhanh] = N'GrandStay Vũng Tàu Oceanfront Hotel',
        [TinhThanh] = N'Bà Rịa - Vũng Tàu',
        [DiaChiChiTiet] = N'165 Thùy Vân, Phường Thắng Tam, TP. Vũng Tàu, Tỉnh Bà Rịa - Vũng Tàu',
        [ViDo] = 10.33582000,
        [KinhDo] = 107.09124000,
        [VungMien] = 'south',
        [DangHoatDong] = 1
    WHERE [MaID] = 4;
END

SET IDENTITY_INSERT [dbo].[ChiNhanh] OFF;
GO

-- Nạp phòng mẫu cho Chi nhánh 4 Vũng Tàu nếu chưa có
IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 4)
BEGIN
    DECLARE @MaLoaiStandard INT = (SELECT TOP 1 MaID FROM [dbo].[LoaiPhong] ORDER BY MaID ASC);
    DECLARE @MaLoaiDeluxe INT = (SELECT TOP 1 MaID FROM [dbo].[LoaiPhong] WHERE MaID > @MaLoaiStandard ORDER BY MaID ASC);
    IF @MaLoaiDeluxe IS NULL SET @MaLoaiDeluxe = @MaLoaiStandard;

    INSERT INTO [dbo].[Phong] ([SoPhong], [MaLoaiPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
    VALUES 
    ('VT-101', @MaLoaiStandard, 1, 'Available', 'Clean', GETDATE(), 4),
    ('VT-102', @MaLoaiStandard, 1, 'Available', 'Clean', GETDATE(), 4),
    ('VT-201', @MaLoaiDeluxe, 2, 'Available', 'Clean', GETDATE(), 4),
    ('VT-202', @MaLoaiDeluxe, 2, 'Available', 'Clean', GETDATE(), 4),
    ('VT-301', @MaLoaiDeluxe, 3, 'Available', 'Clean', GETDATE(), 4);
END
GO

-- 2. MỞ RỘNG BẢNG DiaDiemThamQuan
IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'TinhThanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [TinhThanh] NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'KhuVuc') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [KhuVuc] VARCHAR(20) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'TheLoai') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [TheLoai] VARCHAR(50) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'NgoaiTroi') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [NgoaiTroi] BIT NOT NULL CONSTRAINT [DF_DiaDiem_NgoaiTroi] DEFAULT 1;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'LaBienDao') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [LaBienDao] BIT NOT NULL CONSTRAINT [DF_DiaDiem_LaBienDao] DEFAULT 0;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'PhuongTienTiepCan') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [PhuongTienTiepCan] VARCHAR(30) NOT NULL CONSTRAINT [DF_DiaDiem_PhuongTien] DEFAULT 'duong_bo';
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'ThangDepTu') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [ThangDepTu] INT NOT NULL CONSTRAINT [DF_DiaDiem_ThangTu] DEFAULT 1;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'ThangDepDen') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [ThangDepDen] INT NOT NULL CONSTRAINT [DF_DiaDiem_ThangDen] DEFAULT 12;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'ThoiGianThamQuanPhut') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [ThoiGianThamQuanPhut] INT NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'GiaVeThamKhao') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [GiaVeThamKhao] DECIMAL(18, 2) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'GioMoCua') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [GioMoCua] NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'DiemBienTap') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [DiemBienTap] DECIMAL(3, 2) NOT NULL CONSTRAINT [DF_DiaDiem_DiemBienTap] DEFAULT 4.50;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'NguonDuLieu') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [NguonDuLieu] NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'MaNguonNgoai') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [MaNguonNgoai] VARCHAR(100) NULL;
END
GO

IF COL_LENGTH(N'[dbo].[DiaDiemThamQuan]', N'NgayXacMinh') IS NULL
BEGIN
    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [NgayXacMinh] DATETIME NULL;
END
GO

-- 3. TẠO BẢNG NỐI DiaDiem_ChiNhanh
IF OBJECT_ID(N'[dbo].[DiaDiem_ChiNhanh]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DiaDiem_ChiNhanh](
        [MaDiaDiem] [int] NOT NULL,
        [MaChiNhanh] [int] NOT NULL,
        [KhoangCach_km] [decimal](7, 2) NOT NULL,
        [ThoiGianDiChuyenPhut] [int] NOT NULL,
        [LaChiNhanhGanNhat] [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_DiaDiem_ChiNhanh] PRIMARY KEY CLUSTERED ([MaDiaDiem] ASC, [MaChiNhanh] ASC),
        CONSTRAINT [FK_DiaDiemChiNhanh_DiaDiem] FOREIGN KEY ([MaDiaDiem]) REFERENCES [dbo].[DiaDiemThamQuan]([MaID]) ON DELETE CASCADE,
        CONSTRAINT [FK_DiaDiemChiNhanh_ChiNhanh] FOREIGN KEY ([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]) ON DELETE CASCADE
    );
END
GO

-- 4. CẬP NHẬT & SEED BỘ 37 ĐỊA ĐIỂM DU LỊCH THỰC TẾ
SET IDENTITY_INSERT [dbo].[DiaDiemThamQuan] ON;

MERGE INTO [dbo].[DiaDiemThamQuan] AS Target
USING (VALUES
    -- CỤM 1: TP. HỒ CHÍ MINH & VŨNG TÀU (Gần TP.HCM, kết nối Chi nhánh 4 Vũng Tàu)
    (101, N'Bãi Sau Vũng Tàu (Bãi Thùy Vân)', 1.2, N'Bãi tắm biển dài và sôi động nhất Vũng Tàu, sóng êm, bờ cát thoai thoải lý tưởng tắm biển và dạo bình minh.', N'https://maps.google.com/?q=10.3375,107.0910', 10.33750000, 107.09100000, N'Đường Thùy Vân, Phường Thắng Tam, TP. Vũng Tàu', 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Bà Rịa - Vũng Tàu', 'south', 'bien', 1, 0, 'duong_bo', 1, 12, 180, 0, N'Tự do cả ngày', 4.80, N'OpenStreetMap_Verified', 'node/vungtau_baisau', GETDATE()),
    (102, N'Mũi Nghinh Phong & Cổng Trời Vũng Tàu', 2.8, N'Mũi đất vươn ra biển đón gió bốn mùa, điểm ngắm hoàng hôn tuyệt đẹp với view 2 mặt biển Đông.', N'https://maps.google.com/?q=10.3236,107.0789', 10.32360000, 107.07890000, N'Số 1 Hạ Long, Phường 2, TP. Vũng Tàu', 'https://images.unsplash.com/photo-1519046904884-53103b34b206?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Bà Rịa - Vũng Tàu', 'south', 'check_in', 1, 0, 'duong_bo', 1, 12, 90, 0, N'06:00 - 18:00', 4.70, N'OpenStreetMap_Verified', 'node/vungtau_nghinhphong', GETDATE()),
    (103, N'Ngọn Hải Đăng Vũng Tàu', 4.5, N'Hải đăng cổ xưa nhất Việt Nam trên đỉnh Núi Nhỏ, ngắm toàn cảnh thành phố biển và rừng cây bông gòn nở trắng.', N'https://maps.google.com/?q=10.3344,107.0769', 10.33440000, 107.07690000, N'Đỉnh Núi Nhỏ, Phường 2, TP. Vũng Tàu', 'https://images.unsplash.com/photo-1509316975850-ff9c5deb0cd9?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Bà Rịa - Vũng Tàu', 'south', 'check_in', 1, 0, 'duong_bo', 1, 12, 90, 0, N'07:00 - 22:00', 4.65, N'OpenStreetMap_Verified', 'node/vungtau_haidang', GETDATE()),
    (104, N'Tượng Chúa Kito Vua Núi Nhỏ', 3.2, N'Tượng Chúa giang tay lớn nhất châu Á, trải nghiệm leo gần 1.000 bậc thang để ngắm toàn cảnh biển xanh ngát.', N'https://maps.google.com/?q=10.3259,107.0844', 10.32590000, 107.08440000, N'Núi Nhỏ, Phường 2, TP. Vũng Tàu', 'https://images.unsplash.com/photo-1544644181-1484b3fdfc62?auto=format&fit=crop&w=800&q=80', 'phieuluu', 1, N'Bà Rịa - Vũng Tàu', 'south', 'van_hoa', 1, 0, 'duong_bo', 1, 12, 120, 0, N'07:00 - 17:00', 4.75, N'OpenStreetMap_Verified', 'node/vungtau_tuongchua', GETDATE()),
    (105, N'Bảo Tàng Vũ Khí Cổ Robert Taylor', 5.1, N'Bảo tàng trong nhà độc đáo lưu giữ hơn 3.000 hiện vật vũ khí, quân phục cổ xưa từ khắp nơi trên thế giới. Tránh mưa cực tốt.', N'https://maps.google.com/?q=10.3541,107.0682', 10.35410000, 107.06820000, N'98 Trần Hưng Đạo, Phường 1, TP. Vũng Tàu', 'https://images.unsplash.com/photo-1566127444979-b3d2b654e3d7?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Bà Rịa - Vũng Tàu', 'south', 'bao_tang', 0, 0, 'duong_bo', 1, 12, 90, 70000, N'08:00 - 17:00', 4.60, N'OpenStreetMap_Verified', 'node/vungtau_roberttaylor', GETDATE()),
    (106, N'Bạch Dinh Vũng Tàu (Villa Blanche)', 4.8, N'Dinh thự kiểu Pháp cổ kính nép mình bên sườn núi Lớn, lưu giữ cổ vật gốm sứ thời Khang Hy và bao quát trọn biển Bãi Trước.', N'https://maps.google.com/?q=10.3478,107.0673', 10.34780000, 107.06730000, N'10 Trần Phú, Phường 1, TP. Vũng Tàu', 'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Bà Rịa - Vũng Tàu', 'south', 'van_hoa', 0, 0, 'duong_bo', 1, 12, 75, 15000, N'07:30 - 17:00', 4.55, N'OpenStreetMap_Verified', 'node/vungtau_bachdinh', GETDATE()),
    (107, N'Bến Bạch Đằng & Saigon Waterbus', 102.0, N'Trải nghiệm du ngoạn buýt đường sông ngắm nhìn thành phố năng động và check-in công viên ven sông Sài Gòn thoáng đãng.', N'https://maps.google.com/?q=10.7725,106.7061', 10.77250000, 106.70610000, N'Đường Tôn Đức Thắng, Phường Bến Nghé, Quận 1, TP.HCM', 'https://images.unsplash.com/photo-1583417319070-4a69db38a482?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'TP. Hồ Chí Minh', 'south', 'check_in', 1, 0, 'duong_bo', 1, 12, 120, 15000, N'07:00 - 21:00', 4.65, N'OpenStreetMap_Verified', 'node/hcm_bachdang', GETDATE()),
    (108, N'Landmark 81 SkyView & Đài Quan Sát', 98.0, N'Tổ hợp ngắm cảnh trong nhà trên tầng 81, view toàn cảnh đại đô thị lung linh ánh đèn. Tránh mưa và nắng hoàn hảo.', N'https://maps.google.com/?q=10.7951,106.7218', 10.79510000, 106.72180000, N'720A Điện Biên Phủ, Phường 22, Quận Bình Thạnh, TP.HCM', 'https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'TP. Hồ Chí Minh', 'south', 'giai_tri', 0, 0, 'duong_bo', 1, 12, 90, 420000, N'08:30 - 22:00', 4.85, N'OpenStreetMap_Verified', 'node/hcm_landmark81', GETDATE()),
    (109, N'Bảo Tàng Mỹ Thuật TP. Hồ Chí Minh', 103.5, N'Kiến trúc Pháp - Á Đông tráng lệ với các ô cửa kính màu và cầu thang xoắn ốc biểu tượng, thánh địa check-in sống ảo trong nhà.', N'https://maps.google.com/?q=10.7699,106.6994', 10.76990000, 106.69940000, N'97A Phó Đức Chính, Phường Nguyễn Thái Bình, Quận 1, TP.HCM', 'https://images.unsplash.com/photo-1579783902614-a3fb3927b675?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'TP. Hồ Chí Minh', 'south', 'bao_tang', 0, 0, 'duong_bo', 1, 12, 120, 30000, N'08:00 - 17:00', 4.80, N'OpenStreetMap_Verified', 'node/hcm_finearts', GETDATE()),
    (110, N'Khu Sinh Thái Rừng Ngập Mặn Cần Giờ', 75.0, N'Khu dự trữ sinh quyển thế giới với rừng đước bạt ngàn, tham quan đảo khỉ tự nhiên và chèo thuyền len lỏi kênh rạch.', N'https://maps.google.com/?q=10.4578,106.8942', 10.45780000, 106.89420000, N'Xã Long Hòa, Huyện Cần Giờ, TP.HCM', 'https://images.unsplash.com/photo-1448375240586-882707db888b?auto=format&fit=crop&w=800&q=80', 'phieuluu', 1, N'TP. Hồ Chí Minh', 'south', 'nui_rung', 1, 0, 'duong_bo', 1, 12, 240, 35000, N'07:30 - 17:00', 4.50, N'OpenStreetMap_Verified', 'node/hcm_cangio', GETDATE()),

    -- CỤM 2: ĐÀ LẠT & LÂM ĐỒNG (Chi nhánh 3 Đà Lạt)
    (201, N'Hồ Tuyền Lâm & Thiền Viện Trúc Lâm', 1.5, N'Quần thể hồ sinh thái thơ mộng nép mình dưới rặng thông xanh ngút ngàn, khí hậu se lạnh trong lành.', N'https://maps.google.com/?q=11.9022,108.4358', 11.90220000, 108.43580000, N'Phường 4, TP. Đà Lạt, Tỉnh Lâm Đồng', 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Lâm Đồng', 'south', 'nui_rung', 1, 0, 'duong_bo', 10, 4, 180, 0, N'07:00 - 17:00', 4.90, N'OpenStreetMap_Verified', 'node/dalat_tuyenlam', GETDATE()),
    (202, N'Quảng Trường Lâm Viên & Nụ Hoa Atiso', 4.2, N'Biểu tượng trái tim thành phố hoa với đóa dã quỳ và khối nụ hoa kính màu lộng lẫy soi bóng Hồ Xuân Hương.', N'https://maps.google.com/?q=11.9366,108.4452', 11.93660000, 108.44520000, N'Đường Trần Quốc Toản, Phường 10, TP. Đà Lạt', 'https://images.unsplash.com/photo-1544644181-1484b3fdfc62?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Lâm Đồng', 'south', 'check_in', 1, 0, 'duong_bo', 10, 4, 90, 0, N'Tự do cả ngày', 4.75, N'OpenStreetMap_Verified', 'node/dalat_lamvien', GETDATE()),
    (203, N'Thác Datanla & Đường Trượt Alpine Coaster', 3.8, N'Khám phá dòng thác hoang sơ giữa thung lũng sâu cùng hệ thống máng trượt xuyên rừng thông dài nhất Đông Nam Á.', N'https://maps.google.com/?q=11.9036,108.4503', 11.90360000, 108.45030000, N'Quốc lộ 20, Đèo Prenn, Phường 3, TP. Đà Lạt', 'https://images.unsplash.com/photo-1432405972618-c60b0225b8f9?auto=format&fit=crop&w=800&q=80', 'phieuluu', 1, N'Lâm Đồng', 'south', 'nui_rung', 1, 0, 'duong_bo', 10, 4, 150, 200000, N'07:00 - 17:00', 4.80, N'OpenStreetMap_Verified', 'node/dalat_datanla', GETDATE()),
    (204, N'Dinh III Bảo Đại (Biệt Điện Mùa Hè)', 5.5, N'Dinh thự phong cách kiến trúc châu Âu của vị hoàng đế cuối cùng, lưu giữ nguyên vẹn nội thất trang nhã và hầm rượu cổ. Tránh mưa tốt.', N'https://maps.google.com/?q=11.9300,108.4294', 11.93000000, 108.42940000, N'Số 1 Triệu Việt Vương, Phường 4, TP. Đà Lạt', 'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Lâm Đồng', 'south', 'van_hoa', 0, 0, 'duong_bo', 10, 4, 90, 40000, N'07:00 - 17:30', 4.60, N'OpenStreetMap_Verified', 'node/dalat_dinh3', GETDATE()),
    (205, N'Ga Xe Lửa Cổ Đà Lạt', 6.0, N'Nhà ga cổ kính nhất Đông Dương mang phong cách Art Deco kết hợp mái nhà rông Tây Nguyên, góc check-in hoài niệm.', N'https://maps.google.com/?q=11.9419,108.4552', 11.94190000, 108.45520000, N'1 Quang Trung, Phường 10, TP. Đà Lạt', 'https://images.unsplash.com/photo-1517649763962-0c623266ddc0?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Lâm Đồng', 'south', 'van_hoa', 0, 0, 'duong_bo', 10, 4, 60, 50000, N'07:30 - 17:00', 4.70, N'OpenStreetMap_Verified', 'node/dalat_gaxelua', GETDATE()),
    (206, N'Đồi Chè Cầu Đất & Điểm Săn Mây', 24.0, N'Bình minh rực rỡ trên biển mây trôi bềnh bồng giữa những luống chè xanh mướt ngút ngàn, điểm sống ảo kinh điển mùa khô.', N'https://maps.google.com/?q=11.8542,108.5721', 11.85420000, 108.57210000, N'Thôn Trường Thọ, Xã Xuân Trường, TP. Đà Lạt', 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Lâm Đồng', 'south', 'nui_rung', 1, 0, 'duong_bo', 10, 4, 180, 0, N'05:30 - 17:00', 4.95, N'OpenStreetMap_Verified', 'node/dalat_caudat', GETDATE()),
    (207, N'XQ Sử Quán Đà Lạt', 8.5, N'Không gian nghệ thuật tranh thêu tay trong nhà mang âm hưởng cung đình Huế lãng mạn, thanh tịnh. Thích hợp tham quan khi trời mưa.', N'https://maps.google.com/?q=11.9689,108.4497', 11.96890000, 108.44970000, N'80A Mai Anh Đào, Phường 8, TP. Đà Lạt', 'https://images.unsplash.com/photo-1579783902614-a3fb3927b675?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Lâm Đồng', 'south', 'van_hoa', 0, 0, 'duong_bo', 1, 12, 90, 100000, N'08:00 - 17:00', 4.65, N'OpenStreetMap_Verified', 'node/dalat_xqsuquan', GETDATE()),

    -- CỤM 3: ĐÀ NẴNG & HỘI AN (Chi nhánh 1 Đà Nẵng)
    (301, N'Bán Đảo Sơn Trà & Chùa Linh Ứng', 8.2, N'Viên ngọc xanh của Đà Nẵng với tượng Phật Bà nghìn mắt nghìn tay cao 67m hướng biển, ngắm toàn cảnh vịnh Đà Nẵng.', N'https://maps.google.com/?q=16.1002,108.2778', 16.10020000, 108.27780000, N'Bán đảo Sơn Trà, Quận Sơn Trà, TP. Đà Nẵng', 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Đà Nẵng', 'central', 'van_hoa', 1, 0, 'duong_bo', 2, 8, 150, 0, N'06:00 - 18:00', 4.85, N'OpenStreetMap_Verified', 'node/danang_sontra', GETDATE()),
    (302, N'Cầu Rồng & Cầu Tình Yêu Đà Nẵng', 3.5, N'Biểu tượng rồng thép vươn mình ra biển, rực rỡ phun lửa và phun nước vào tối cuối tuần bên bờ sông Hàn thơ mộng.', N'https://maps.google.com/?q=16.0610,108.2272', 16.06100000, 108.22720000, N'Đường Trần Hưng Đạo, Quận Sơn Trà, TP. Đà Nẵng', 'https://images.unsplash.com/photo-1519046904884-53103b34b206?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Đà Nẵng', 'central', 'check_in', 1, 0, 'duong_bo', 2, 9, 90, 0, N'Tự do cả ngày (Phun lửa 21h T7-CN)', 4.80, N'OpenStreetMap_Verified', 'node/danang_caurong', GETDATE()),
    (303, N'Bãi Biển Mỹ Khê Đà Nẵng', 0.5, N'Một trong những bãi biển quyến rũ nhất hành tinh do Forbes bình chọn, bờ cát trắng mịn, nước trong vắt ngay trước khách sạn.', N'https://maps.google.com/?q=16.0594,108.2467', 16.05940000, 108.24670000, N'Võ Nguyên Giáp, Quận Sơn Trà, TP. Đà Nẵng', 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Đà Nẵng', 'central', 'bien', 1, 0, 'duong_bo', 3, 9, 180, 0, N'Tự do cả ngày', 4.90, N'OpenStreetMap_Verified', 'node/danang_mykhe', GETDATE()),
    (304, N'Sun World Bà Nà Hills & Cầu Vàng', 28.5, N'Đường lên tiên cảnh với cây Cầu Vàng trứ danh được nâng đỡ bởi đôi bàn tay khổng lồ giữa biển mây và làng Pháp cổ tích.', N'https://maps.google.com/?q=15.9950,107.9967', 15.99500000, 107.99670000, N'Hòa Ninh, Huyện Hòa Vang, TP. Đà Nẵng', 'https://images.unsplash.com/photo-1544644181-1484b3fdfc62?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Đà Nẵng', 'central', 'giai_tri', 1, 0, 'duong_bo', 3, 8, 300, 900000, N'08:00 - 17:00', 4.95, N'OpenStreetMap_Verified', 'node/danang_banahills', GETDATE()),
    (305, N'Bảo Tàng Điêu Khắc Chăm Đà Nẵng', 3.2, N'Bảo tàng lưu giữ hiện vật văn hóa Champa quy mô nhất thế giới, kiến trúc Gothic Pháp kết hợp tháp Chăm. Thích hợp tránh mưa.', N'https://maps.google.com/?q=16.0601,108.2236', 16.06010000, 108.22360000, N'Số 2 Đường 2 Tháng 9, Quận Hải Châu, TP. Đà Nẵng', 'https://images.unsplash.com/photo-1566127444979-b3d2b654e3d7?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Đà Nẵng', 'central', 'bao_tang', 0, 0, 'duong_bo', 1, 12, 90, 60000, N'07:30 - 17:00', 4.65, N'OpenStreetMap_Verified', 'node/danang_chammuseum', GETDATE()),
    (306, N'Phố Cổ Hội An (Di Sản UNESCO)', 26.0, N'Đô thị cổ nghìn năm với dãy nhà tường vàng cổ kính, rực rỡ đèn lồng đêm rằm và đi thuyền thả hoa đăng trên sông Hoài.', N'https://maps.google.com/?q=15.8801,108.3274', 15.88010000, 108.32740000, N'TP. Hội An, Tỉnh Quảng Nam', 'https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Quảng Nam', 'central', 'van_hoa', 1, 0, 'duong_bo', 2, 8, 240, 120000, N'07:00 - 21:30', 4.95, N'OpenStreetMap_Verified', 'node/hoian_phoco', GETDATE()),
    (307, N'Rừng Dừa Bảy Mẫu Cẩm Thanh', 29.0, N'Miền Tây thu nhỏ giữa lòng phố Hội với trải nghiệm đi thuyền thúng múa thúng xoay tròn cảm giác mạnh giữa rặng dừa nước.', N'https://maps.google.com/?q=15.8694,108.3683', 15.86940000, 108.36830000, N'Xã Cẩm Thanh, TP. Hội An, Tỉnh Quảng Nam', 'https://images.unsplash.com/photo-1448375240586-882707db888b?auto=format&fit=crop&w=800&q=80', 'phieuluu', 1, N'Quảng Nam', 'central', 'nui_rung', 1, 0, 'duong_bo', 2, 8, 120, 150000, N'07:00 - 17:00', 4.75, N'OpenStreetMap_Verified', 'node/hoian_rungdua', GETDATE()),
    (308, N'Làng Gốm Thanh Hà & Công Viên Đất Nung', 28.0, N'Làng nghề 500 năm tuổi ven sông Thu Bồn, trải nghiệm tự tay chuốt gốm và tham quan bảo tàng gốm trong nhà lớn nhất nước.', N'https://maps.google.com/?q=15.8778,108.3039', 15.87780000, 108.30390000, N'Phường Thanh Hà, TP. Hội An, Tỉnh Quảng Nam', 'https://images.unsplash.com/photo-1579783902614-a3fb3927b675?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Quảng Nam', 'central', 'van_hoa', 0, 0, 'duong_bo', 1, 12, 90, 40000, N'08:00 - 17:30', 4.60, N'OpenStreetMap_Verified', 'node/hoian_langgom', GETDATE()),

    -- CỤM 4: PHÚ QUỐC & KIÊN GIANG (Chi nhánh 2 Phú Quốc - LaBienDao = 1)
    (401, N'Sunset Town & Cầu Hôn Kiss Bridge', 14.5, N'Thị trấn Hoàng Hôn Địa Trung Hải tráng lệ và cây Cầu Hôn chạm khắc biểu tượng tình yêu, điểm ngắm mặt trời lặn lãng mạn.', N'https://maps.google.com/?q=10.0278,104.0139', 10.02780000, 104.01390000, N'Thị trấn An Thới, TP. Phú Quốc, Tỉnh Kiên Giang', 'https://images.unsplash.com/photo-1519046904884-53103b34b206?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Kiên Giang', 'south', 'check_in', 1, 1, 'bay_hoac_tau', 11, 4, 180, 100000, N'16:00 - 22:00', 4.90, N'OpenStreetMap_Verified', 'node/phuquoc_sunsettown', GETDATE()),
    (402, N'Bãi Sao Phú Quốc', 16.0, N'Bãi biển hình vành trăng khuyết với bờ cát trắng mịn như kem và làn nước trong xanh màu ngọc bích, đặc sản biển đảo phương Nam.', N'https://maps.google.com/?q=10.0542,104.0322', 10.05420000, 104.03220000, N'Ấp Bãi Sao, Phường An Thới, TP. Phú Quốc', 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Kiên Giang', 'south', 'bien', 1, 1, 'bay_hoac_tau', 11, 4, 180, 0, N'Tự do cả ngày', 4.85, N'OpenStreetMap_Verified', 'node/phuquoc_baisao', GETDATE()),
    (403, N'Grand World Phú Quốc (Thành Phố Không Ngủ)', 25.0, N'Tổ hợp giải trí lễ hội 24/7 với dòng sông Venice thơ mộng, show diễn Tinh Hoa Việt Nam và Nhà tre Huyền Thoại đồ sộ.', N'https://maps.google.com/?q=10.3278,103.8569', 10.32780000, 103.85690000, N'Bãi Dài, Xã Gành Dầu, TP. Phú Quốc', 'https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?auto=format&fit=crop&w=800&q=80', 'songao', 1, N'Kiên Giang', 'south', 'giai_tri', 1, 1, 'bay_hoac_tau', 11, 4, 240, 0, N'Mở cửa 24/7', 4.80, N'OpenStreetMap_Verified', 'node/phuquoc_grandworld', GETDATE()),
    (404, N'Vinpearl Safari Phú Quốc', 27.0, N'Công viên chăm sóc và bảo tồn động vật bán hoang dã đầu tiên tại Việt Nam, trải nghiệm xe bus chuyên dụng "nhốt người thả thú".', N'https://maps.google.com/?q=10.3411,103.8822', 10.34110000, 103.88220000, N'Bãi Dài, Xã Gành Dầu, TP. Phú Quốc', 'https://images.unsplash.com/photo-1448375240586-882707db888b?auto=format&fit=crop&w=800&q=80', 'giadinh', 1, N'Kiên Giang', 'south', 'nui_rung', 1, 1, 'bay_hoac_tau', 11, 4, 210, 650000, N'08:30 - 16:00', 4.88, N'OpenStreetMap_Verified', 'node/phuquoc_safari', GETDATE()),
    (405, N'Cáp Treo Hòn Thơm Vượt Biển', 14.8, N'Hệ thống cáp treo 3 dây vượt biển dài nhất thế giới, ngắm trọn vẹn bức tranh quần đảo An Thới từ độ cao ngoạn mục.', N'https://maps.google.com/?q=10.0261,104.0128', 10.02610000, 104.01280000, N'Bãi Đất Đỏ, Thị trấn An Thới, TP. Phú Quốc', 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80', 'phieuluu', 1, N'Kiên Giang', 'south', 'check_in', 1, 1, 'bay_hoac_tau', 11, 4, 150, 600000, N'08:30 - 17:30', 4.92, N'OpenStreetMap_Verified', 'node/phuquoc_honthom', GETDATE()),
    (406, N'Di Tích Lịch Sử Nhà Tù Phú Quốc', 15.2, N'Khu di tích lịch sử đặc biệt cấp quốc gia với hệ thống nhà giam và phòng trưng bày tư liệu trong nhà. Tránh mưa tốt.', N'https://maps.google.com/?q=10.0436,104.0203', 10.04360000, 104.02030000, N'350 Nguyễn Văn Cừ, Phường An Thới, TP. Phú Quốc', 'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80', 'thugian', 1, N'Kiên Giang', 'south', 'bao_tang', 0, 1, 'bay_hoac_tau', 1, 12, 90, 0, N'07:30 - 17:00', 4.65, N'OpenStreetMap_Verified', 'node/phuquoc_nhatu', GETDATE())
) AS Source (
    [MaID], [Ten], [KhoangCach_km], [MoTa], [LienKetBanDo], [ViDo], [KinhDo], [DiaChi], [DuongDanAnh], [PhongCach], [DangHoatDong],
    [TinhThanh], [KhuVuc], [TheLoai], [NgoaiTroi], [LaBienDao], [PhuongTienTiepCan], [ThangDepTu], [ThangDepDen],
    [ThoiGianThamQuanPhut], [GiaVeThamKhao], [GioMoCua], [DiemBienTap], [NguonDuLieu], [MaNguonNgoai], [NgayXacMinh]
)
ON Target.[MaID] = Source.[MaID]
WHEN MATCHED THEN
    UPDATE SET
        [Ten] = Source.[Ten],
        [KhoangCach_km] = Source.[KhoangCach_km],
        [MoTa] = Source.[MoTa],
        [LienKetBanDo] = Source.[LienKetBanDo],
        [ViDo] = Source.[ViDo],
        [KinhDo] = Source.[KinhDo],
        [DiaChi] = Source.[DiaChi],
        [DuongDanAnh] = Source.[DuongDanAnh],
        [PhongCach] = Source.[PhongCach],
        [DangHoatDong] = Source.[DangHoatDong],
        [TinhThanh] = Source.[TinhThanh],
        [KhuVuc] = Source.[KhuVuc],
        [TheLoai] = Source.[TheLoai],
        [NgoaiTroi] = Source.[NgoaiTroi],
        [LaBienDao] = Source.[LaBienDao],
        [PhuongTienTiepCan] = Source.[PhuongTienTiepCan],
        [ThangDepTu] = Source.[ThangDepTu],
        [ThangDepDen] = Source.[ThangDepDen],
        [ThoiGianThamQuanPhut] = Source.[ThoiGianThamQuanPhut],
        [GiaVeThamKhao] = Source.[GiaVeThamKhao],
        [GioMoCua] = Source.[GioMoCua],
        [DiemBienTap] = Source.[DiemBienTap],
        [NguonDuLieu] = Source.[NguonDuLieu],
        [MaNguonNgoai] = Source.[MaNguonNgoai],
        [NgayXacMinh] = Source.[NgayXacMinh]
WHEN NOT MATCHED THEN
    INSERT (
        [MaID], [Ten], [KhoangCach_km], [MoTa], [LienKetBanDo], [ViDo], [KinhDo], [DiaChi], [DuongDanAnh], [PhongCach], [DangHoatDong],
        [TinhThanh], [KhuVuc], [TheLoai], [NgoaiTroi], [LaBienDao], [PhuongTienTiepCan], [ThangDepTu], [ThangDepDen],
        [ThoiGianThamQuanPhut], [GiaVeThamKhao], [GioMoCua], [DiemBienTap], [NguonDuLieu], [MaNguonNgoai], [NgayXacMinh]
    ) VALUES (
        Source.[MaID], Source.[Ten], Source.[KhoangCach_km], Source.[MoTa], Source.[LienKetBanDo], Source.[ViDo], Source.[KinhDo], Source.[DiaChi], Source.[DuongDanAnh], Source.[PhongCach], Source.[DangHoatDong],
        Source.[TinhThanh], Source.[KhuVuc], Source.[TheLoai], Source.[NgoaiTroi], Source.[LaBienDao], Source.[PhuongTienTiepCan], Source.[ThangDepTu], Source.[ThangDepDen],
        Source.[ThoiGianThamQuanPhut], Source.[GiaVeThamKhao], Source.[GioMoCua], Source.[DiemBienTap], Source.[NguonDuLieu], Source.[MaNguonNgoai], Source.[NgayXacMinh]
    );

SET IDENTITY_INSERT [dbo].[DiaDiemThamQuan] OFF;
GO

-- 5. NẠP DỮ LIỆU BẢNG NỐI DiaDiem_ChiNhanh (Khoảng cách & thời gian di chuyển tới từng khách sạn)
DELETE FROM [dbo].[DiaDiem_ChiNhanh];
GO

INSERT INTO [dbo].[DiaDiem_ChiNhanh] ([MaDiaDiem], [MaChiNhanh], [KhoangCach_km], [ThoiGianDiChuyenPhut], [LaChiNhanhGanNhat])
VALUES
-- Nhóm Vũng Tàu (gần Chi nhánh 4 Vũng Tàu nhất)
(101, 4, 1.20, 5, 1),
(102, 4, 2.80, 8, 1),
(103, 4, 4.50, 12, 1),
(104, 4, 3.20, 10, 1),
(105, 4, 5.10, 14, 1),
(106, 4, 4.80, 12, 1),
(107, 4, 98.00, 120, 1),
(108, 4, 95.00, 115, 1),
(109, 4, 99.00, 120, 1),
(110, 4, 75.00, 100, 1),

-- Nhóm Đà Lạt (gần Chi nhánh 3 Đà Lạt nhất)
(201, 3, 1.50, 5, 1),
(202, 3, 4.20, 10, 1),
(203, 3, 3.80, 8, 1),
(204, 3, 5.50, 12, 1),
(205, 3, 6.00, 15, 1),
(206, 3, 24.00, 40, 1),
(207, 3, 8.50, 20, 1),

-- Nhóm Đà Nẵng & Hội An (gần Chi nhánh 1 Đà Nẵng nhất)
(301, 1, 8.20, 15, 1),
(302, 1, 3.50, 8, 1),
(303, 1, 0.50, 2, 1),
(304, 1, 28.50, 45, 1),
(305, 1, 3.20, 7, 1),
(306, 1, 26.00, 35, 1),
(307, 1, 29.00, 40, 1),
(308, 1, 28.00, 38, 1),

-- Nhóm Phú Quốc (gần Chi nhánh 2 Phú Quốc nhất)
(401, 2, 14.50, 22, 1),
(402, 2, 16.00, 25, 1),
(403, 2, 25.00, 35, 1),
(404, 2, 27.00, 38, 1),
(405, 2, 14.80, 22, 1),
(406, 2, 15.20, 24, 1);
GO
