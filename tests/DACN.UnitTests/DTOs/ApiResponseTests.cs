using DACN.Web.DTOs.Common;
using FluentAssertions;

namespace DACN.UnitTests.DTOs;

public class ApiResponseTests
{
    // ─── ApiResponse<T>.Ok ───────────────────────────────────────────────────

    [Fact]
    public void Ok_SetsThanhCongTrue()
    {
        var response = ApiResponse<string>.Ok("data");
        response.ThanhCong.Should().BeTrue();
    }

    [Fact]
    public void Ok_SetsDataCorrectly()
    {
        var response = ApiResponse<string>.Ok("hello");
        response.DuLieu.Should().Be("hello");
    }

    [Fact]
    public void Ok_SetsDefaultMessage_WhenNoMessageProvided()
    {
        var response = ApiResponse<string>.Ok("data");
        response.ThongBao.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Ok_SetsCustomMessage()
    {
        var response = ApiResponse<string>.Ok("data", "Tạo thành công");
        response.ThongBao.Should().Be("Tạo thành công");
    }

    [Fact]
    public void Ok_WorksWithNullableData()
    {
        var response = ApiResponse<string?>.Ok(null);
        response.ThanhCong.Should().BeTrue();
        response.DuLieu.Should().BeNull();
    }

    // ─── ApiResponse<T>.Fail ─────────────────────────────────────────────────

    [Fact]
    public void Fail_SetsThanhCongFalse()
    {
        var response = ApiResponse<string>.Fail("Lỗi rồi");
        response.ThanhCong.Should().BeFalse();
    }

    [Fact]
    public void Fail_SetsMessageCorrectly()
    {
        var response = ApiResponse<string>.Fail("Không tìm thấy");
        response.ThongBao.Should().Be("Không tìm thấy");
    }

    [Fact]
    public void Fail_SetsDuLieuNull()
    {
        var response = ApiResponse<string>.Fail("Lỗi");
        response.DuLieu.Should().BeNull();
    }

    [Fact]
    public void Fail_WorksWithComplexType()
    {
        var response = ApiResponse<List<int>>.Fail("Lỗi hệ thống");
        response.ThanhCong.Should().BeFalse();
        response.DuLieu.Should().BeNull();
    }

    // ─── KetQuaPhanTrang<T>.TongTrang ────────────────────────────────────────

    [Theory]
    [InlineData(10, 3, 4)]   // ceil(10/3) = 4
    [InlineData(10, 10, 1)]  // ceil(10/10) = 1
    [InlineData(0, 10, 0)]   // ceil(0/10) = 0
    [InlineData(1, 10, 1)]   // ceil(1/10) = 1
    [InlineData(9, 3, 3)]    // ceil(9/3) = 3
    [InlineData(11, 3, 4)]   // ceil(11/3) = 4
    public void KetQuaPhanTrang_TongTrang_CalculatesCorrectly(int tongSo, int soLuong, int expectedTongTrang)
    {
        var ketQua = new KetQuaPhanTrang<string>
        {
            TongSo = tongSo,
            SoLuong = soLuong
        };

        ketQua.TongTrang.Should().Be(expectedTongTrang);
    }

    [Fact]
    public void KetQuaPhanTrang_DuLieu_DefaultsToEmptyList()
    {
        var ketQua = new KetQuaPhanTrang<int>();
        ketQua.DuLieu.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void KetQuaPhanTrang_HoldsData()
    {
        var ketQua = new KetQuaPhanTrang<string>
        {
            DuLieu = ["a", "b", "c"],
            TongSo = 100,
            Trang = 2,
            SoLuong = 10
        };

        ketQua.DuLieu.Should().HaveCount(3);
        ketQua.TongTrang.Should().Be(10);
        ketQua.Trang.Should().Be(2);
    }
}
