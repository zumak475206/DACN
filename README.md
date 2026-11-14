# 🏨 BookingStay.AI — Hệ Thống Đặt Phòng Lưu Trú & Trợ Lý Du Lịch Thông Minh (DACN)

Dự án Đồ án Chuyên ngành (DACN) kết hợp:
1. **Cổng Khách Hàng (Booking & AI Travel Advisor):** Giao diện đặt phòng phong cách Booking.com tích hợp Trợ lý AI gợi ý điểm đến, mùa đẹp nhất, lọc theo ngân sách và gu trải nghiệm (*Check-in sống ảo, Nghỉ dưỡng chữa lành, Gia đình*).
2. **Cổng Quản Trị Nội Bộ (PMS — 4 Thành Viên):** Quản lý vận hành chuỗi lưu trú với 17 API Controllers, phân quyền RBAC chi tiết (18 quyền hạn), Lễ tân Check-in/Check-out, Buồng phòng, Kiểm kê tài sản/Đền bù hư hỏng và Báo cáo BI.

---

## 👥 Phân Công Module Cho 4 Thành Viên

Mỗi thành viên làm việc trên **thư mục giao diện (`.razor`) và Controller riêng biệt** để không bị xung đột code (`git conflict`) khi gộp nhánh:

| Thành viên | Phân hệ phụ trách | Thư mục Frontend (`src/DACN.Web/Components/Pages/`) | Các API Controller phụ trách (`src/DACN.Web/Controllers/`) |
| :--- | :--- | :--- | :--- |
| **Thành viên 1 (TV1)** | Bảo mật, Phân quyền RBAC, Voucher, Địa điểm & Bài viết | `ThanhVien1_BaoMat/` (`AccountManagement.razor`, `RbacMatrix.razor`, `VoucherManagement.razor`, `ContentManagement.razor`, `AuditLogs.razor`) | `AuthController.cs`, `NguoiDungController.cs`, `VaiTroController.cs`, `VoucherController.cs`, `NoiDungController.cs`, `NhatKyController.cs` |
| **Thành viên 2 (TV2)** | Buồng phòng, Ca dọn vệ sinh, Tài sản & Chi phí vận hành | `ThanhVien2_BuongPhong/` (`RoomStatus.razor`, `CleaningTasks.razor`, `AssetInventory.razor`) | `PhongController.cs`, `LoaiPhongController.cs`, `BuongPhongController.cs`, `ChiPhiController.cs` |
| **Thành viên 3 (TV3)** | Tiền sảnh, Đặt phòng, Xếp phòng, Check-in/out, Hóa đơn | `ThanhVien3_TienSanh/` (`RoomRackBookings.razor`, `CheckInOut.razor`, `BillingServices.razor`) + `Customer/` | `DatPhongController.cs`, `HoaDonController.cs`, `DichVuController.cs` |
| **Thành viên 4 (TV4)** | AI Tư vấn Du lịch theo mùa/ngân sách, Dự báo & BI | `ThanhVien4_AICore/` (`BiReports.razor`, `AiChatbot.razor`, `AiForecastRisk.razor`) | `AiController.cs`, `DashboardController.cs`, `DanhGiaController.cs` |

---

## 🚀 Hướng Dẫn Cho Thành Viên Clone Về & Chạy Thử (5 Phút)

### Bước 1: Yêu cầu cài đặt
- **.NET 9.0 SDK** (kiểm tra bằng lệnh `dotnet --version`).
- **SQL Server** (LocalDB, SQL Express hoặc SQL Server Developer).

### Bước 2: Clone dự án về máy
```bash
git clone <URL_GITHUB_CUA_NHOM>
cd DACN
```

### Bước 3: Khởi tạo Cơ sở dữ liệu (`DACN`)
1. Mở **SQL Server Management Studio (SSMS)**, tạo database tên là `DACN` và chạy script tạo bảng + dữ liệu mẫu (`database_script.sql`) của nhóm.
2. Kiểm tra chuỗi kết nối trong `src/DACN.Web/appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.;Database=DACN;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
   *(Nếu máy bạn dùng SQL Express thì đổi `Server=.` thành `Server=.\\SQLEXPRESS`)*.

### Bước 4: Chạy bộ Test Tự Động để kiểm tra môi trường (94 bài test)
Trước khi bắt tay vào code, chạy lệnh sau tại thư mục gốc để đảm bảo BE và DB hoạt động 100%:
```bash
dotnet test
```
Nếu màn hình hiện **`Passed! - Failed: 0, Passed: 53 (DACN.UnitTests)`** và **`Passed! - Failed: 0, Passed: 41 (DACN.IntegrationTests)`** là máy bạn đã sẵn sàng!

### Bước 5: Chạy Trang Web & Swagger API
```bash
dotnet run --project src/DACN.Web/DACN.Web.csproj --launch-profile https
```
Sau khi chạy, mở trình duyệt tại các đường dẫn:
1. **Trang chủ Đặt phòng & AI Gợi ý Du lịch (Khách hàng):** `https://localhost:7136/`
2. **Trang Đăng nhập & Chuyển nhanh 5 vai trò test (RBAC):** `https://localhost:7136/login`
3. **Cổng Quản trị Nội bộ (TV1 - TV4):** `https://localhost:7136/admin/dashboard`
4. **Tài liệu & Test API trực tiếp (Swagger UI):** `https://localhost:7136/swagger`

---

## 🛠️ Quy Trình Làm Việc Nhóm Trên Git (Tránh Xung Đột Code)

1. **Tạo nhánh riêng theo tên thành viên trước khi code:**
   ```bash
   git checkout -b feature/tv1-rbac-voucher
   # hoặc: feature/tv2-buong-phong, feature/tv3-tien-sanh, feature/tv4-ai-bi
   ```
2. **Tìm các vị trí `// TODO [TV...]`** trong thư mục của mình để nối giao diện Blazor vào API hoặc `DACNContext`.
3. **Lưu ý quan trọng khi ghi trạng thái vào Database `DACN`:**
   - Luôn dùng các hàm chuẩn hóa trong `DACN.Web.Common.StatusMapper` (ví dụ: `StatusMapper.NormalizeDatPhongTrangThai(...)`, `StatusMapper.NormalizePhongTrangThai(...)`) để đảm bảo không bao giờ vi phạm `CHECK CONSTRAINT` của SQL Server.
4. **Trước khi đẩy code lên GitHub (`git push`):**
   - Chạy lại `dotnet test` để chắc chắn không làm hỏng chức năng chung.
   - Commit và đẩy nhánh lên:
     ```bash
     git add .
     git commit -m "feat(tvX): hoan thien chuc nang ..."
     git push origin feature/tvX-...
     ```
