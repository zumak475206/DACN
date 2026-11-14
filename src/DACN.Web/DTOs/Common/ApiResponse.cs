namespace DACN.Web.DTOs.Common;

public class ApiResponse<T>
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public T? DuLieu { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Thành công") =>
        new() { ThanhCong = true, ThongBao = message, DuLieu = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { ThanhCong = false, ThongBao = message };
}

public class PhanTrangRequest
{
    public int Trang { get; set; } = 1;
    public int SoLuong { get; set; } = 10;
}

public class KetQuaPhanTrang<T>
{
    public List<T> DuLieu { get; set; } = [];
    public int TongSo { get; set; }
    public int Trang { get; set; }
    public int SoLuong { get; set; }
    public int TongTrang => (int)Math.Ceiling((double)TongSo / SoLuong);
}
