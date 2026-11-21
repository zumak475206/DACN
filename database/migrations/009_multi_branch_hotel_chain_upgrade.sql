-- =============================================================================
-- Migration 009: Nâng cấp Hệ thống Chuỗi Khách sạn Đa Chi Nhánh (Multi-Branch Hotel Chain)
-- Chạy an toàn nhiều lần (Idempotent) trên mọi phiên bản SQL Server
-- Tương thích ngược 100% với dữ liệu hiện tại (DEFAULT 1 cho Chi nhánh Trụ sở Đà Nẵng)
-- =============================================================================
USE [DACN];
GO

-- 1. TẠO BẢNG ChiNhanh NẾU CHƯA CÓ
IF OBJECT_ID(N'[dbo].[ChiNhanh]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChiNhanh](
        [MaID] [int] IDENTITY(1,1) NOT NULL,
        [MaChiNhanhCode] [varchar](20) NOT NULL,
        [TenChiNhanh] [nvarchar](255) NOT NULL,
        [TinhThanh] [nvarchar](100) NOT NULL,
        [DiaChiChiTiet] [nvarchar](500) NOT NULL,
        [SoDienThoai] [varchar](50) NOT NULL,
        [EmailLienHe] [varchar](100) NOT NULL,
        [HinhAnhDaiDien] [nvarchar](500) NULL,
        [SoSao] [int] NOT NULL DEFAULT 4,
        [DangHoatDong] [bit] NOT NULL DEFAULT 1,
        [NgayTao] [datetime] NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_ChiNhanh] PRIMARY KEY CLUSTERED ([MaID] ASC),
        CONSTRAINT [UQ_ChiNhanh_Code] UNIQUE ([MaChiNhanhCode])
    );
END
GO

-- 2. NẠP DỮ LIỆU 3 CHI NHÁNH MẪU ĐẠI DIỆN 3 ĐIỂM ĐẾN CHÍNH
SET IDENTITY_INSERT [dbo].[ChiNhanh] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ChiNhanh] WHERE [MaID] = 1)
BEGIN
    INSERT INTO [dbo].[ChiNhanh] (
        [MaID], [MaChiNhanhCode], [TenChiNhanh], [TinhThanh], [DiaChiChiTiet],
        [SoDienThoai], [EmailLienHe], [HinhAnhDaiDien], [SoSao], [DangHoatDong], [NgayTao]
    ) VALUES (
        1, 'CN-DANANG', N'GrandStay Đà Nẵng Beach Resort', N'Đà Nẵng',
        N'120 Võ Nguyên Giáp, Phường Phước Mỹ, Quận Sơn Trà, TP. Đà Nẵng',
        '0236 3888 999', 'danang@bookingstay.vn',
        'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80',
        5, 1, GETDATE()
    );
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ChiNhanh] WHERE [MaID] = 2)
BEGIN
    INSERT INTO [dbo].[ChiNhanh] (
        [MaID], [MaChiNhanhCode], [TenChiNhanh], [TinhThanh], [DiaChiChiTiet],
        [SoDienThoai], [EmailLienHe], [HinhAnhDaiDien], [SoSao], [DangHoatDong], [NgayTao]
    ) VALUES (
        2, 'CN-PHUQUOC', N'GrandStay Sunset Villa Phú Quốc', N'Kiên Giang',
        N'Khu phức hợp Bãi Trường, Xã Dương Tơ, TP. Phú Quốc, Tỉnh Kiên Giang',
        '0297 3777 888', 'phuquoc@bookingstay.vn',
        'https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80',
        5, 1, GETDATE()
    );
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[ChiNhanh] WHERE [MaID] = 3)
BEGIN
    INSERT INTO [dbo].[ChiNhanh] (
        [MaID], [MaChiNhanhCode], [TenChiNhanh], [TinhThanh], [DiaChiChiTiet],
        [SoDienThoai], [EmailLienHe], [HinhAnhDaiDien], [SoSao], [DangHoatDong], [NgayTao]
    ) VALUES (
        3, 'CN-DALAT', N'GrandStay Pine Hill Retreat Đà Lạt', N'Lâm Đồng',
        N'Đường Hoa Hồng, Khu du lịch Quốc gia Hồ Tuyền Lâm, Phường 4, TP. Đà Lạt',
        '0263 3666 777', 'dalat@bookingstay.vn',
        'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800&q=80',
        4, 1, GETDATE()
    );
END

SET IDENTITY_INSERT [dbo].[ChiNhanh] OFF;
GO

-- 3. BỔ SUNG CỘT MaChiNhanh VÀO CÁC BẢNG LIÊN QUAN (MẶC ĐỊNH = 1 CHO DỮ LIỆU HIỆN CÓ)

-- A. LoaiPhong
IF COL_LENGTH('dbo.LoaiPhong', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[LoaiPhong] ADD [MaChiNhanh] [int] NOT NULL CONSTRAINT [DF_LoaiPhong_ChiNhanh] DEFAULT 1;
    ALTER TABLE [dbo].[LoaiPhong] ADD CONSTRAINT [FK_LoaiPhong_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- B. Phong
IF COL_LENGTH('dbo.Phong', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[Phong] ADD [MaChiNhanh] [int] NOT NULL CONSTRAINT [DF_Phong_ChiNhanh] DEFAULT 1;
    ALTER TABLE [dbo].[Phong] ADD CONSTRAINT [FK_Phong_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
    
    -- Xóa constraint unique cũ nếu có trên SoPhong đơn lẻ, thêm composite unique (MaChiNhanh, SoPhong)
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Phong_SoPhong' AND object_id = OBJECT_ID('dbo.Phong'))
    BEGIN
        ALTER TABLE [dbo].[Phong] DROP CONSTRAINT [UQ_Phong_SoPhong];
    END
    
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Phong_ChiNhanh_SoPhong' AND object_id = OBJECT_ID('dbo.Phong'))
    BEGIN
        ALTER TABLE [dbo].[Phong] ADD CONSTRAINT [UQ_Phong_ChiNhanh_SoPhong] UNIQUE ([MaChiNhanh], [SoPhong]);
    END
END
GO

-- C. DatPhong
IF COL_LENGTH('dbo.DatPhong', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[DatPhong] ADD [MaChiNhanh] [int] NOT NULL CONSTRAINT [DF_DatPhong_ChiNhanh] DEFAULT 1;
    ALTER TABLE [dbo].[DatPhong] ADD CONSTRAINT [FK_DatPhong_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- D. NguoiDung (Nhân viên gắn với chi nhánh)
IF COL_LENGTH('dbo.NguoiDung', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[NguoiDung] ADD [MaChiNhanh] [int] NULL CONSTRAINT [DF_NguoiDung_ChiNhanh] DEFAULT 1;
    ALTER TABLE [dbo].[NguoiDung] ADD CONSTRAINT [FK_NguoiDung_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- E. ChiPhiVanHanh
IF COL_LENGTH('dbo.ChiPhiVanHanh', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChiPhiVanHanh] ADD [MaChiNhanh] [int] NOT NULL CONSTRAINT [DF_ChiPhiVanHanh_ChiNhanh] DEFAULT 1;
    ALTER TABLE [dbo].[ChiPhiVanHanh] ADD CONSTRAINT [FK_ChiPhiVanHanh_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- F. DuBaoNhuCauDatPhong_AI
IF COL_LENGTH('dbo.DuBaoNhuCauDatPhong_AI', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[DuBaoNhuCauDatPhong_AI] ADD [MaChiNhanh] [int] NOT NULL CONSTRAINT [DF_DuBaoAI_ChiNhanh] DEFAULT 1;
    ALTER TABLE [dbo].[DuBaoNhuCauDatPhong_AI] ADD CONSTRAINT [FK_DuBaoAI_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- G. DichVu (NULL = Toàn chuỗi)
IF COL_LENGTH('dbo.DichVu', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[DichVu] ADD [MaChiNhanh] [int] NULL;
    ALTER TABLE [dbo].[DichVu] ADD CONSTRAINT [FK_DichVu_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- H. MaGiamGia (NULL = Toàn chuỗi)
IF COL_LENGTH('dbo.MaGiamGia', 'MaChiNhanh') IS NULL
BEGIN
    ALTER TABLE [dbo].[MaGiamGia] ADD [MaChiNhanh] [int] NULL;
    ALTER TABLE [dbo].[MaGiamGia] ADD CONSTRAINT [FK_MaGiamGia_ChiNhanh] FOREIGN KEY([MaChiNhanh]) REFERENCES [dbo].[ChiNhanh]([MaID]);
END
GO

-- 4. BỔ SUNG LOẠI PHÒNG VÀ PHÒNG VẬT LÝ MẪU CHO CHI NHÁNH PHÚ QUỐC VÀ ĐÀ LẠT
-- A. Loại phòng Phú Quốc (MaChiNhanh = 2)
IF NOT EXISTS (SELECT 1 FROM [dbo].[LoaiPhong] WHERE [DuongDanRutGon] = 'grandstay-suite-family-resort-phuquoc')
BEGIN
    INSERT INTO [dbo].[LoaiPhong] (
        [Ten], [DuongDanRutGon], [GiaCoBan], [SoNguoiLonToiDa], [SoTreEmToiDa],
        [DienTich_m2], [LoaiGiuong], [HuongNhin], [MoTa], [NoiDung],
        [DangHoatDong], [NgayTao], [MaChiNhanh]
    ) VALUES (
        N'GrandStay Suite Family Resort', 'grandstay-suite-family-resort-phuquoc',
        1500000, 4, 2, 65, N'2 Giường King', N'Hướng biển hoàng hôn',
        N'Không gian 65m² với 2 phòng ngủ riêng biệt, hồ bơi vô cực ngắm hoàng hôn biển đảo Phú Quốc.',
        N'Trải nghiệm đẳng cấp nghỉ dưỡng biển đảo tại Phú Quốc với đầy đủ tiện nghi 5 sao.',
        1, GETDATE(), 2
    );
END

-- B. Loại phòng Đà Lạt (MaChiNhanh = 3)
IF NOT EXISTS (SELECT 1 FROM [dbo].[LoaiPhong] WHERE [DuongDanRutGon] = 'pine-hill-retreat-superior-dalat')
BEGIN
    INSERT INTO [dbo].[LoaiPhong] (
        [Ten], [DuongDanRutGon], [GiaCoBan], [SoNguoiLonToiDa], [SoTreEmToiDa],
        [DienTich_m2], [LoaiGiuong], [HuongNhin], [MoTa], [NoiDung],
        [DangHoatDong], [NgayTao], [MaChiNhanh]
    ) VALUES (
        N'Pine Hill Retreat Superior', 'pine-hill-retreat-superior-dalat',
        750000, 2, 1, 35, N'1 Giường Queen gỗ sồi', N'Hướng thung lũng đồi thông',
        N'Phòng gỗ ấm áp nhìn ra thung lũng thông xanh mướt, yên tĩnh tuyệt đối để nghỉ ngơi đọc sách và săn mây.',
        N'Nghỉ dưỡng chữa lành tại vùng đồi thông Đà Lạt mộng mơ.',
        1, GETDATE(), 3
    );
END
GO

-- C. Tạo các phòng vật lý mẫu cho Chi nhánh Phú Quốc (P101, P102, P201)
DECLARE @MaLpPhuQuoc INT = (SELECT TOP 1 [MaID] FROM [dbo].[LoaiPhong] WHERE [DuongDanRutGon] = 'grandstay-suite-family-resort-phuquoc');
IF @MaLpPhuQuoc IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 2 AND [SoPhong] = 'PQ-101')
        INSERT INTO [dbo].[Phong] ([MaLoaiPhong], [SoPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
        VALUES (@MaLpPhuQuoc, 'PQ-101', 1, 'Available', 'Clean', GETDATE(), 2);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 2 AND [SoPhong] = 'PQ-102')
        INSERT INTO [dbo].[Phong] ([MaLoaiPhong], [SoPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
        VALUES (@MaLpPhuQuoc, 'PQ-102', 1, 'Available', 'Clean', GETDATE(), 2);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 2 AND [SoPhong] = 'PQ-201')
        INSERT INTO [dbo].[Phong] ([MaLoaiPhong], [SoPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
        VALUES (@MaLpPhuQuoc, 'PQ-201', 2, 'Available', 'Clean', GETDATE(), 2);
END
GO

-- D. Tạo các phòng vật lý mẫu cho Chi nhánh Đà Lạt (DL-101, DL-102, DL-201)
DECLARE @MaLpDaLat INT = (SELECT TOP 1 [MaID] FROM [dbo].[LoaiPhong] WHERE [DuongDanRutGon] = 'pine-hill-retreat-superior-dalat');
IF @MaLpDaLat IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 3 AND [SoPhong] = 'DL-101')
        INSERT INTO [dbo].[Phong] ([MaLoaiPhong], [SoPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
        VALUES (@MaLpDaLat, 'DL-101', 1, 'Available', 'Clean', GETDATE(), 3);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 3 AND [SoPhong] = 'DL-102')
        INSERT INTO [dbo].[Phong] ([MaLoaiPhong], [SoPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
        VALUES (@MaLpDaLat, 'DL-102', 1, 'Available', 'Clean', GETDATE(), 3);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Phong] WHERE [MaChiNhanh] = 3 AND [SoPhong] = 'DL-201')
        INSERT INTO [dbo].[Phong] ([MaLoaiPhong], [SoPhong], [Tang], [TrangThai], [TrangThaiVeSinh], [NgayTao], [MaChiNhanh])
        VALUES (@MaLpDaLat, 'DL-201', 2, 'Available', 'Clean', GETDATE(), 3);
END
GO

-- 5. TẠO TÀI KHOẢN NHÂN SỰ CÁC CHI NHÁNH ĐỂ TEST PHÂN QUYỀN VẬN HÀNH
-- Mật khẩu mặc định: 'Password123!' (BCrypt: $2a$11$wK1W5yV5N7G1p1wF8xN9eugk8vGq1aC3uH8uP1sD0zQ1zQ1zQ1zQ1 hoặc dùng chuỗi hash chuẩn của dự án)
DECLARE @DefaultHash NVARCHAR(MAX) = (SELECT TOP 1 [MaBamMatKhau] FROM [dbo].[NguoiDung] WHERE [Email] = 'nhanvien1@hotel.com');
IF @DefaultHash IS NULL SET @DefaultHash = '$2a$11$G7V.1/pQJ43u8WffcO4VYeU7Jv4.GqE4N63aHqjY40YQ0jX1L314m';

-- Lễ tân Phú Quốc (MaChiNhanh = 2, MaVaiTro = 3 [Lễ tân])
IF NOT EXISTS (SELECT 1 FROM [dbo].[NguoiDung] WHERE [Email] = 'letan_phuquoc@hotel.com')
BEGIN
    INSERT INTO [dbo].[NguoiDung] (
        [MaVaiTro], [HoTen], [Email], [SoDienThoai], [MaBamMatKhau],
        [LoaiNguoiDung], [TrangThai], [TongDiem], [NgayTao], [MaChiNhanh]
    ) VALUES (
        3, N'Lê Lễ Tân Phú Quốc', 'letan_phuquoc@hotel.com', '0297 3777 888',
        @DefaultHash, 'staff', 1, 0, GETDATE(), 2
    );
END

-- Lễ tân Đà Lạt (MaChiNhanh = 3, MaVaiTro = 3 [Lễ tân])
IF NOT EXISTS (SELECT 1 FROM [dbo].[NguoiDung] WHERE [Email] = 'letan_dalat@hotel.com')
BEGIN
    INSERT INTO [dbo].[NguoiDung] (
        [MaVaiTro], [HoTen], [Email], [SoDienThoai], [MaBamMatKhau],
        [LoaiNguoiDung], [TrangThai], [TongDiem], [NgayTao], [MaChiNhanh]
    ) VALUES (
        3, N'Đặng Lễ Tân Đà Lạt', 'letan_dalat@hotel.com', '0263 3666 777',
        @DefaultHash, 'staff', 1, 0, GETDATE(), 3
    );
END
GO

-- 6. CHUẨN HÓA TRI THỨC AI GỢI Ý DU LỊCH ĐA VÙNG MIỀN & ĐA CHI NHÁNH
-- 6A. Sửa lỗi mô tả thời tiết bị lệch của Hội An (MaID = 4)
UPDATE [dbo].[GoiYDiaDiemTheoMua_AI]
SET [LoaiThoiTiet] = N'Nắng xuân dịu nhẹ, khô ráo'
WHERE [MaID] = 4;

-- 6B. Bổ sung Mùa Du Lịch cho Đà Lạt - Tây Nguyên (Tháng 10 - Tháng 3) và Mùa Xuân Miền Trung
SET IDENTITY_INSERT [dbo].[MuaDuLich] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[MuaDuLich] WHERE [MaID] = 5)
BEGIN
    INSERT INTO [dbo].[MuaDuLich] ([MaID], [Ten], [LoaiMua], [ThangBatDau], [ThangKetThuc], [KhuVuc], [MoTa], [MoTaThoiTiet], [NhietDoTrungBinh], [LaMuaCaoDiem], [NgayTao])
    VALUES (5, N'Mùa Khô Săn Mây & Hoa Cuối Năm Đà Lạt', N'winter', 10, 3, N'south',
            N'Mùa đẹp nhất trong năm tại Đà Lạt (Lâm Đồng), cách TP.HCM chỉ 300km. Trời trong xanh, biển mây dày đặc mỗi sớm, mùa cỏ hồng, hoa dã quỳ và mai anh đào.',
            N'Se lạnh 14-20°C, nắng vàng trong trẻo, không mưa, biển mây buổi sớm cực đẹp để sống ảo',
            17.50, 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[MuaDuLich] WHERE [MaID] = 6)
BEGIN
    INSERT INTO [dbo].[MuaDuLich] ([MaID], [Ten], [LoaiMua], [ThangBatDau], [ThangKetThuc], [KhuVuc], [MoTa], [MoTaThoiTiet], [NhietDoTrungBinh], [LaMuaCaoDiem], [NgayTao])
    VALUES (6, N'Mùa Xuân Khô Ráo Miền Trung (Đà Nẵng - Hội An)', N'spring', 2, 4, N'central',
            N'Tiết trời đầu xuân tại Đà Nẵng - Hội An khô ráo, nắng nhẹ, ít mưa.',
            N'Nắng xuân dịu nhẹ 22-26°C, khô ráo thoáng đãng',
            24.50, 0, GETDATE());
END

SET IDENTITY_INSERT [dbo].[MuaDuLich] OFF;
GO

-- 6C. Bổ sung Địa Điểm Tham Quan cho Chi nhánh Đà Lạt (Gần TP.HCM ~300km) và Chi nhánh Phú Quốc
SET IDENTITY_INSERT [dbo].[DiaDiemThamQuan] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[DiaDiemThamQuan] WHERE [MaID] = 6)
BEGIN
    INSERT INTO [dbo].[DiaDiemThamQuan] ([MaID], [Ten], [KhoangCach_km], [MoTa], [LienKetBanDo], [ViDo], [KinhDo], [DiaChi], [DuongDanAnh], [DangHoatDong], [PhongCach])
    VALUES (6, N'Đồi Chè Cầu Đất & Thung Lũng Săn Mây Đà Lạt', 12.50,
            N'Tọa độ săn mây và sống ảo đẹp bậc nhất gần TP.HCM (~300km), với những luống chè xanh mướt ngập trong biển mây trắng xóa vào tháng 10 đến tháng 3.',
            N'https://maps.google.com/maps?q=Cau+Dat+Tea+Hill+Da+Lat', 11.87451200, 108.56781200,
            N'TP. Đà Lạt, Tỉnh Lâm Đồng (Cách TP.HCM ~300km)',
            N'https://images.unsplash.com/photo-1583417319070-4a69db38a482?auto=format&fit=crop&w=800&q=80',
            1, N'songao,thugian,phieuluu');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[DiaDiemThamQuan] WHERE [MaID] = 7)
BEGIN
    INSERT INTO [dbo].[DiaDiemThamQuan] ([MaID], [Ten], [KhoangCach_km], [MoTa], [LienKetBanDo], [ViDo], [KinhDo], [DiaChi], [DuongDanAnh], [DangHoatDong], [PhongCach])
    VALUES (7, N'Hồ Tuyền Lâm & Rừng Thông Pine Hill Đà Lạt', 1.20,
            N'Hồ nước ngọt rộng lớn bao quanh bởi rừng thông ba lá xanh ngắt và sương sớm lãng mạn, sát cạnh khu nghỉ dưỡng GrandStay Pine Hill Retreat Đà Lạt.',
            N'https://maps.google.com/maps?q=Tuyen+Lam+Lake+Da+Lat', 11.90123400, 108.43123400,
            N'Khu du lịch Hồ Tuyền Lâm, Phường 4, TP. Đà Lạt, Tỉnh Lâm Đồng (Cách TP.HCM ~300km)',
            N'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800&q=80',
            1, N'songao,thugian,giadinh');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[DiaDiemThamQuan] WHERE [MaID] = 8)
BEGIN
    INSERT INTO [dbo].[DiaDiemThamQuan] ([MaID], [Ten], [KhoangCach_km], [MoTa], [LienKetBanDo], [ViDo], [KinhDo], [DiaChi], [DuongDanAnh], [DangHoatDong], [PhongCach])
    VALUES (8, N'Thị Trấn Hoàng Hôn (Sunset Town) & Bãi Trường Phú Quốc', 2.50,
            N'Tổ hợp kiến trúc Địa Trung Hải rực rỡ sắc màu bên bờ biển Bãi Trường và Cầu Hôn Phú Quốc, nơi ngắm hoàng hôn biển đẹp nhất miền Nam.',
            N'https://maps.google.com/maps?q=Sunset+Town+Phu+Quoc', 10.03123400, 104.00123400,
            N'Bãi Trường & An Thới, TP. Phú Quốc, Tỉnh Kiên Giang',
            N'https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80',
            1, N'songao,thugian,giadinh,amthuc');
END

SET IDENTITY_INSERT [dbo].[DiaDiemThamQuan] OFF;
GO

-- 6D. Bổ sung Gợi Ý Địa Điểm Theo Mùa AI cho Đà Lạt & Phú Quốc
SET IDENTITY_INSERT [dbo].[GoiYDiaDiemTheoMua_AI] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[GoiYDiaDiemTheoMua_AI] WHERE [MaID] = 6)
BEGIN
    INSERT INTO [dbo].[GoiYDiaDiemTheoMua_AI] ([MaID], [MaDiaDiem], [MaMua], [MuaDuLich], [ThangBatDauGoiY], [ThangKetThucGoiY], [DiemPhuHop], [LyDoGoiY], [LoaiThoiTiet], [NoiBat], [NgayTao])
    VALUES (6, 6, 5, N'winter', 10, 3, 0.990,
            N'Cách TP.HCM chỉ ~300km (6 tiếng đi xe Limousine, tiết kiệm tối đa chi phí di chuyển). Tháng 12 là đỉnh cao mùa khô Đà Lạt: trời nắng trong veo, se lạnh 15-20°C, biển mây dày đặc và đồi cỏ hồng/hoa cuối năm nở rộ ở mọi góc chụp ảnh sống ảo.',
            N'Se lạnh 15-20°C, nắng trong, biển mây buổi sớm', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[GoiYDiaDiemTheoMua_AI] WHERE [MaID] = 7)
BEGIN
    INSERT INTO [dbo].[GoiYDiaDiemTheoMua_AI] ([MaID], [MaDiaDiem], [MaMua], [MuaDuLich], [ThangBatDauGoiY], [ThangKetThucGoiY], [DiemPhuHop], [LyDoGoiY], [LoaiThoiTiet], [NoiBat], [NgayTao])
    VALUES (7, 7, 5, N'winter', 10, 3, 0.975,
            N'Điểm nghỉ dưỡng & check-in rừng thông chuẩn châu Âu gần TP.HCM nhất. Tháng 10 đến tháng 3 mặt hồ Tuyền Lâm phẳng lặng phủ sương mờ buổi sớm, thời tiết khô ráo mát lạnh, tổng chi phí xe + phòng 2N1Đ hoặc 3N2Đ chỉ từ 2.1 - 2.8 triệu đồng.',
            N'Mát lạnh 16-21°C, sương sớm rừng thông', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[GoiYDiaDiemTheoMua_AI] WHERE [MaID] = 8)
BEGIN
    INSERT INTO [dbo].[GoiYDiaDiemTheoMua_AI] ([MaID], [MaDiaDiem], [MaMua], [MuaDuLich], [ThangBatDauGoiY], [ThangKetThucGoiY], [DiemPhuHop], [LyDoGoiY], [LoaiThoiTiet], [NoiBat], [NgayTao])
    VALUES (8, 8, 3, N'peak', 11, 4, 0.970,
            N'Tháng 11 đến tháng 4 là mùa khô đẹp nhất tại đảo ngọc Phú Quốc (Miền Nam). Biển êm, nắng vàng rực rỡ, kiến trúc Địa Trung Hải và hoàng hôn Bãi Trường lên hình sống ảo tuyệt đẹp.',
            N'Nắng ấm 28°C, khô ráo, biển êm xanh ngắt', 1, GETDATE());
END

SET IDENTITY_INSERT [dbo].[GoiYDiaDiemTheoMua_AI] OFF;
GO

-- 6E. Bổ sung Gợi Ý Tour / Combo AI cho Đà Lạt & Phú Quốc
SET IDENTITY_INSERT [dbo].[GoiYTour_AI] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[GoiYTour_AI] WHERE [MaID] = 4)
BEGIN
    INSERT INTO [dbo].[GoiYTour_AI] ([MaID], [TieuDe], [MoTa], [DiemDen], [GiaToiThieu], [GiaToiDa], [SoNgay], [MuaDuLich], [DoiTuongPhuHop], [CacThangTotNhat], [DuongDanAnh], [LienHeDoiTac], [DiemDanhGiaTrungBinh], [DangHoatDong], [NgayTao], [NgayCapNhat], [PhongCach])
    VALUES (4, N'Combo Săn Mây Đồi Thông & Sống Ảo Đà Lạt (Gần TP.HCM) 3N2Đ',
            N'Lịch trình tối ưu cho ngân sách ~2-3 triệu xuất phát từ TP.HCM: Di chuyển xe giường nằm Limousine (6h), lưu trú tại GrandStay Pine Hill Retreat Đà Lạt, săn mây Đồi Chè Cầu Đất, dạo Hồ Tuyền Lâm và check-in quán cafe đồi thông.',
            N'TP. Đà Lạt, Lâm Đồng (Cách TP.HCM ~300km)',
            1950000.00, 2850000.00, 3, N'winter', N'["couple","friends","solo"]', N'[10,11,12,1,2,3]',
            N'https://images.unsplash.com/photo-1583417319070-4a69db38a482?auto=format&fit=crop&w=800&q=80',
            N'0263-DALAT-STAY', 4.95, 1, GETDATE(), GETDATE(), N'songao,thugian,phieuluu');
END
ELSE
BEGIN
    UPDATE [dbo].[GoiYTour_AI] SET [MuaDuLich] = N'winter' WHERE [MaID] = 4;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[GoiYTour_AI] WHERE [MaID] = 5)
BEGIN
    INSERT INTO [dbo].[GoiYTour_AI] ([MaID], [TieuDe], [MoTa], [DiemDen], [GiaToiThieu], [GiaToiDa], [SoNgay], [MuaDuLich], [DoiTuongPhuHop], [CacThangTotNhat], [DuongDanAnh], [LienHeDoiTac], [DiemDanhGiaTrungBinh], [DangHoatDong], [NgayTao], [NgayCapNhat], [PhongCach])
    VALUES (5, N'Kỳ Nghỉ Hoàng Hôn Biển Đảo Phú Quốc & Sunset Town 3N2Đ',
            N'Tận hưởng mùa khô nắng ấm phương Nam tại GrandStay Sunset Villa Phú Quốc, check-in Cầu Hôn, Thị trấn Hoàng Hôn Địa Trung Hải và lặn ngắm san hô.',
            N'TP. Phú Quốc, Kiên Giang',
            3500000.00, 5200000.00, 3, N'peak', N'["family","couple","friends"]', N'[11,12,1,2,3,4]',
            N'https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80',
            N'0297-PHUQUOC-VILLA', 4.92, 1, GETDATE(), GETDATE(), N'songao,thugian,giadinh');
END

SET IDENTITY_INSERT [dbo].[GoiYTour_AI] OFF;
GO
