using DACN.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DACN.Web.Common;

public static class SchemaMigrationRunner
{
    public static async Task EnsureLatestSchemaAsync(DACNContext db, ILogger logger)
    {
        if (!db.Database.IsSqlServer())
            return;

        try
        {
            const string sql = """
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[GoiYDiaDiemTheoMua_AI]') AND name = N'LoaiThoiTiet'
                )
                BEGIN
                    ALTER TABLE [dbo].[GoiYDiaDiemTheoMua_AI] ALTER COLUMN [LoaiThoiTiet] NVARCHAR(50) NULL;
                END;

                IF COL_LENGTH('dbo.GoiYTour_AI', 'PhongCach') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[GoiYTour_AI] ADD [PhongCach] NVARCHAR(200) NULL;
                END;

                IF COL_LENGTH('dbo.DiaDiemThamQuan', 'PhongCach') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[DiaDiemThamQuan] ADD [PhongCach] NVARCHAR(200) NULL;
                END;

                IF COL_LENGTH('dbo.MaGiamGia', 'GiamToiDa') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[MaGiamGia] ADD [GiamToiDa] DECIMAL(18, 2) NULL;
                END;
                """;

            await db.Database.ExecuteSqlRawAsync(sql);

            const string seedFixSql = """
                UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Nắng đẹp, biển êm' WHERE [MaID] = 1 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);
                UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Mát mẻ về đêm' WHERE [MaID] = 2 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);
                UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Trong lành, ít mưa' WHERE [MaID] = 3 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);
                UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Se lạnh, săn mây' WHERE [MaID] = 4 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);
                UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Nắng ấm, biển lặng' WHERE [MaID] = 5 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);
                UPDATE [dbo].[GoiYDiaDiemTheoMua_AI] SET [LoaiThoiTiet] = N'Mát dịu, sương sớm' WHERE [MaID] >= 6 AND ([LoaiThoiTiet] LIKE N'%?%' OR [LoaiThoiTiet] IS NULL);

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

                UPDATE [dbo].[MaGiamGia] SET [GiamToiDa] = 500000 WHERE [LoaiGiamGia] = N'percent' AND [GiamToiDa] IS NULL;
                """;

            await db.Database.ExecuteSqlRawAsync(seedFixSql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể tự động chạy SchemaMigrationRunner (có thể DB chưa khởi tạo).");
        }
    }
}
