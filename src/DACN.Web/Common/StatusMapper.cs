namespace DACN.Web.Common;

/// <summary>
/// Chuẩn hóa trạng thái giữa giá trị tiếng Việt/tiếng Anh từ Client/Tests về đúng giá trị CHECK CONSTRAINT của SQL Server DACN.
/// </summary>
public static class StatusMapper
{
    // NguoiDung.LoaiNguoiDung: 'staff', 'customer'
    public static string NormalizeLoaiNguoiDung(string? val, int maVaiTro = 6)
    {
        if (string.IsNullOrWhiteSpace(val))
            return maVaiTro == 6 ? "customer" : "staff";
        var v = val.Trim().ToLowerInvariant();
        if (v is "customer" or "khach_hang" or "khachhang")
            return "customer";
        return "staff";
    }

    // Phong.TrangThai: 'Available', 'Reserved', 'Occupied', 'Maintenance'
    public static string NormalizePhongTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "available" or "trong" => "Available",
            "reserved" or "da_dat" or "dadat" => "Reserved",
            "occupied" or "co_khach" or "cokhach" or "dang_o" => "Occupied",
            "maintenance" or "bao_tri" or "baotri" or "dang_sua" => "Maintenance",
            _ => "Available"
        };
    }

    public static bool IsRoomAvailable(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "available" or "trong";
    }

    public static bool IsRoomOccupied(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "occupied" or "co_khach" or "dang_o";
    }

    public static bool IsRoomMaintenance(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "maintenance" or "bao_tri";
    }

    // Phong.TrangThaiVeSinh: 'Clean', 'Dirty', 'Inspecting'
    public static string NormalizePhongVeSinh(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "clean" or "sach" or "da_don" => "Clean",
            "dirty" or "ban" or "chua_don" or "can_don" => "Dirty",
            "inspecting" or "dang_kiem_tra" or "dang_don" or "cho_kiem_tra" => "Inspecting",
            _ => "Clean"
        };
    }

    // DatPhong.KenhDatPhong: 'web', 'walk_in', 'phone', 'ota'
    public static string NormalizeKenhDatPhong(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "web" or "website" or "online" => "web",
            "walk_in" or "walkin" or "walk-in" or "tai_quay" or "truc_tiep" => "walk_in",
            "phone" or "dien_thoai" or "hotline" => "phone",
            "ota" or "booking" or "agoda" or "traveloka" => "ota",
            _ => "web"
        };
    }

    // DatPhong.TrangThai: 'Pending', 'Confirmed', 'CheckedIn', 'CheckedOut', 'Cancelled'
    public static string NormalizeDatPhongTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "pending" or "cho_xac_nhan" or "cho_duyet" => "Pending",
            "confirmed" or "da_xac_nhan" or "da_duyet" => "Confirmed",
            "checkedin" or "checked_in" or "dang_o" or "da_nhan_phong" => "CheckedIn",
            "checkedout" or "checked_out" or "hoan_thanh" or "da_tra_phong" => "CheckedOut",
            "cancelled" or "canceled" or "da_huy" or "huy" => "Cancelled",
            _ => "Pending"
        };
    }

    public static bool IsBookingCancelled(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "cancelled" or "canceled" or "da_huy" or "huy";
    }

    public static bool IsBookingCheckedOut(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "checkedout" or "checked_out" or "hoan_thanh" or "da_tra_phong";
    }

    public static bool IsBookingCheckedIn(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "checkedin" or "checked_in" or "dang_o" or "da_nhan_phong";
    }

    // ChiTietDatPhong.TrangThai: 'Pending', 'Active', 'CheckedOut', 'Cancelled'
    public static string NormalizeChiTietDatPhongTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "pending" or "cho_xac_nhan" or "confirmed" or "da_xac_nhan" => "Pending",
            "active" or "dang_o" or "checkedin" or "checked_in" or "da_nhan_phong" => "Active",
            "checkedout" or "checked_out" or "hoan_thanh" or "da_tra_phong" => "CheckedOut",
            "cancelled" or "canceled" or "da_huy" or "huy" => "Cancelled",
            _ => "Pending"
        };
    }

    // HoaDon.TrangThai: 'Pending', 'Paid', 'Cancelled'
    public static string NormalizeHoaDonTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "pending" or "chua_thanh_toan" or "thanh_toan_mot_phan" => "Pending",
            "paid" or "da_thanh_toan" or "hoan_thanh" => "Paid",
            "cancelled" or "canceled" or "da_huy" => "Cancelled",
            _ => "Pending"
        };
    }

    public static bool IsInvoicePaid(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "paid" or "da_thanh_toan";
    }

    public static bool IsInvoicePending(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "pending" or "chua_thanh_toan" or "thanh_toan_mot_phan";
    }

    // ThanhToan.PhuongThucThanhToan: 'Cash', 'VNPay', 'MoMo', 'BankTransfer'
    public static string NormalizePhuongThucThanhToan(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "cash" or "tien_mat" or "tienmat" => "Cash",
            "vnpay" or "vn_pay" => "VNPay",
            "momo" or "mo_mo" => "MoMo",
            "banktransfer" or "bank_transfer" or "chuyen_khoan" or "chuyenkhoan" or "qr" or "the" or "card" => "BankTransfer",
            _ => "Cash"
        };
    }

    // ThanhToan.TrangThai: 'Pending', 'Success', 'Failed', 'Cancelled', 'Refunded'
    public static string NormalizeThanhToanTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "pending" or "cho_xu_ly" => "Pending",
            "success" or "thanh_cong" or "paid" or "da_thanh_toan" => "Success",
            "failed" or "that_bai" => "Failed",
            "cancelled" or "da_huy" => "Cancelled",
            "refunded" or "da_hoan_tien" => "Refunded",
            _ => "Success"
        };
    }

    public static bool IsPaymentSuccess(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "success" or "thanh_cong";
    }

    // MaGiamGia.LoaiGiamGia: 'percent', 'fixed'
    public static string NormalizeLoaiGiamGia(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "percent" or "phan_tram" or "phantram" or "%" => "percent",
            "fixed" or "so_tien" or "sotien" or "tien_mat" => "fixed",
            _ => "percent"
        };
    }

    public static bool IsPercentDiscount(string? val)
    {
        var v = val?.Trim().ToLowerInvariant();
        return v is "percent" or "phan_tram" or "phantram" or "%";
    }

    // DonDichVu.TrangThai: 'Pending', 'Processing', 'Completed', 'Cancelled'
    public static string NormalizeDonDichVuTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "pending" or "cho_xu_ly" or "moi_tao" => "Pending",
            "processing" or "dang_xu_ly" or "dang_lam" => "Processing",
            "completed" or "hoan_thanh" or "xong" => "Completed",
            "cancelled" or "da_huy" or "huy" => "Cancelled",
            _ => "Pending"
        };
    }

    // NhatKyBuongPhong.TrangThai: 'assigned', 'in_progress', 'done', 'inspected'
    public static string NormalizeBuongPhongTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "assigned" or "cho_xu_ly" or "da_phan_cong" or "pending" => "assigned",
            "in_progress" or "inprogress" or "dang_don" or "dang_xu_ly" => "in_progress",
            "done" or "hoan_thanh" or "da_don" or "completed" => "done",
            "inspected" or "da_kiem_tra" => "inspected",
            _ => "assigned"
        };
    }

    // ChiPhiVanHanh.TrangThai: 'Pending', 'Approved', 'Rejected', 'Paid'
    public static string NormalizeChiPhiTrangThai(string? val)
    {
        var v = val?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "pending" or "cho_duyet" => "Pending",
            "approved" or "da_duyet" => "Approved",
            "rejected" or "tu_choi" => "Rejected",
            "paid" or "da_chi" or "da_thanh_toan" => "Paid",
            _ => "Pending"
        };
    }
}
