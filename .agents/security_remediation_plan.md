# 📋 KẾ HOẠCH KHẮC PHỤC PHÂN QUYỀN & BẢO MẬT API CONTROLLERS (SECURITY REMEDIATION PLAN)
> **Căn cứ:** Báo cáo kiểm toán `.agents/backend_security_role_audit.md` (Zero Trust 2026 & OWASP API Top 10).  
> **Mục tiêu:** Đưa toàn bộ 17 API Controllers (99 Endpoints) đạt chuẩn **100% tuân thủ Ma trận Phân quyền (6 Roles × 18 Permissions)**, vá triệt để các lỗ hổng BFLA, BOLA/IDOR, SoD Bypass, Deny-by-Default và bổ sung Audit Trail còn thiếu.

---

## 🏗️ TỔNG QUAN CÁC GIAI ĐOẠN (PHASES OVERVIEW)

| Phase | Tên Giai Đoạn | Trọng Tâm Kỹ Thuật | Files Tác Động |
| :---: | :--- | :--- | :--- |
| **PHASE 1** | **Zero Trust & Deny-by-Default** | Gắn `[Authorize]` cấp độ Class cho các Controller còn thiếu; thu hồi `[AllowAnonymous]` ở thống kê trạng thái phòng nhạy cảm. | 8 Controllers (`Phong`, `LoaiPhong`, `DatPhong`, `DichVu`, `Voucher`, `NoiDung`, `DanhGia`, `Ai`) |
| **PHASE 2** | **Chuẩn Hóa RBAC — Xóa Bỏ Anti-Pattern `IsStaffOrAdmin()`** | Thay thế `User.IsStaffOrAdmin()` bằng `User.HasPermission(Quyen.<Permission>)` khớp tuyệt đối với Ma trận 18 quyền hạn. | 9 Controllers (`LoaiPhong`, `Phong`, `DatPhong`, `HoaDon`, `BuongPhong`, `ChiPhi`, `Dashboard`, `Voucher`, `NguoiDung`) |
| **PHASE 3** | **Vá Lỗ Hổng BOLA/IDOR, SoD (Four-Eyes) & Logic Nghiệp Vụ** | Chặn tự duyệt chi phí (SoD) ở API; chặn nhân viên sửa hồ sơ Admin/người khác; chặn đặt dịch vụ cho đơn đã hủy/đã trả phòng. | 3 Controllers (`ChiPhiController`, `NguoiDungController`, `DichVuController`) |
| **PHASE 4** | **Lấp Đầy Khoảng Trống Lưu Vết Kiểm Toán (Audit Trail)** | Inject `IAuditService` và ghi nhận log cho CUD `LoaiPhong` và `DuyetChiPhi`. | 2 Controllers (`LoaiPhongController`, `ChiPhiController`) |
| **PHASE 5** | **Kiểm Thử Tự Động & Xác Minh Hồi Quy (Verification Suite)** | Viết bộ Integration Tests kiểm thử phân quyền chéo (Cross-Role Negative & Positive Tests) và chạy full 117+ tests. | `tests/DACN.IntegrationTests/Api/Tv1Tv4ApiTests.cs` |

---

## 🛠️ CHI TIẾT TỪNG PHASE & CHECKLIST THỰC THI

### 🔹 PHASE 1: Thiết Lập Zero Trust "Deny by Default" Cấp Độ Class
**Mục tiêu:** Mọi Controller nghiệp vụ đều phải có `[Authorize]` ở cấp độ Class để đảm bảo không có endpoint nào vô tình bị bỏ ngỏ khi mở rộng code. Các endpoint công khai phải khai báo tường minh `[AllowAnonymous]`.

#### Danh sách công việc (Checklist):
- [x] **1.1. [PhongController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/PhongController.cs):**
  - Thêm `[Authorize]` ở đầu class `PhongController`.
  - Đổi `GET /api/v1/phong/trang-thai` (`TheoTrangThai`) từ `[AllowAnonymous]` sang yêu cầu đăng nhập nhân viên (`User.IsStaffOrAdmin()`) để không lộ tỷ lệ lấp đầy phòng của khách sạn ra công khai.
- [x] **1.2. [LoaiPhongController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/LoaiPhongController.cs):**
  - Thêm `[Authorize]` ở đầu class `LoaiPhongController` (3 endpoints `GET` công khai giữ nguyên `[AllowAnonymous]`).
- [x] **1.3. [DatPhongController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/DatPhongController.cs):**
  - Thêm `[Authorize]` ở đầu class `DatPhongController` (`POST /api/v1/dat-phong` giữ nguyên `[AllowAnonymous]`).
- [x] **1.4. [DichVuController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/DichVuController.cs):**
  - Thêm `[Authorize]` ở đầu class `DichVuController` (`GET /api/v1/dich-vu` giữ nguyên `[AllowAnonymous]`).
- [x] **1.5. [VoucherController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/VoucherController.cs):**
  - Thêm `[Authorize]` ở đầu class `VoucherController` (`GET cong-khai` và `POST kiem-tra` giữ nguyên `[AllowAnonymous]`).
- [x] **1.6. [NoiDungController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/NoiDungController.cs):**
  - Thêm `[Authorize]` ở đầu class `NoiDungController` (`GET dia-diem`, `GET bai-viet`, `GET bai-viet/{slug}` giữ nguyên `[AllowAnonymous]`).
- [x] **1.7. [DanhGiaController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/DanhGiaController.cs):**
  - Thêm `[Authorize]` ở đầu class `DanhGiaController` (`GET loai-phong/{maLoaiPhong}` giữ nguyên `[AllowAnonymous]`).
- [x] **1.8. [AiController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects\DACN/src/DACN.Web/Controllers/AiController.cs), [AuthController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/AuthController.cs), [SystemController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/SystemController.cs):**
  - Thêm `[Authorize]` ở đầu class (đạt 17/17 Controllers 100% Deny by Default).

---

### 🔹 PHASE 2: Chuẩn Hóa RBAC — Thay Thế `IsStaffOrAdmin()` Bằng `User.HasPermission(Quyen.X)`
**Mục tiêu:** Áp dụng đúng 18 quyền hạn trong `Quyen.cs` cho từng endpoint theo đúng Ma trận Phân quyền, chấm dứt tình trạng nhân viên bộ phận này gọi được API nghiệp vụ của bộ phận khác (OWASP API5 - BFLA).

#### Bảng ánh xạ thay đổi chi tiết:

| STT | File Controller | Action / Endpoint | Guard Hiện Tại (Sai/Thừa quyền) | Guard Chuẩn Mới (Đúng Matrix) | Vai Trò Được Phép |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | `LoaiPhongController` | `POST /` (`Tao`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageRooms)` | Admin, Manager |
| 2 | `LoaiPhongController` | `PUT {id}` (`CapNhat`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageRooms)` | Admin, Manager |
| 3 | `LoaiPhongController` | `DELETE {id}` (`Xoa`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageRooms)` | Admin, Manager |
| 4 | `PhongController` | `PUT {id}/trang-thai` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.UpdateRoomStatus)` | Admin, Manager, FrontDesk, Housekeeping |
| 5 | `PhongController` | `PUT {id}/hoan-tat-ve-sinh`| `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.UpdateRoomStatus) \|\| User.HasPermission(Quyen.ManageHousekeeping)` | Admin, Manager, FrontDesk, Housekeeping |
| 6 | `DatPhongController` | `GET /` (`DanhSach`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageBookings) \|\| User.HasPermission(Quyen.CheckInOut)` | Admin, Manager, FrontDesk |
| 7 | `DatPhongController` | `PUT {id}/xac-nhan` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageBookings)` | Admin, Manager, FrontDesk |
| 8 | `DatPhongController` | `PUT chi-tiet/{id}/gan-phong`| `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageBookings) \|\| User.HasPermission(Quyen.CheckInOut)` | Admin, Manager, FrontDesk |
| 9 | `DatPhongController` | `PUT {id}/check-in` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.CheckInOut)` | Admin, Manager, FrontDesk |
| 10 | `DatPhongController` | `PUT {id}/check-out` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.CheckInOut)` | Admin, Manager, FrontDesk |
| 11 | `HoaDonController` | `GET /` (`DanhSach`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageInvoices) \|\| User.HasPermission(Quyen.ManagePayments)` | Admin, Manager, FrontDesk, Accountant |
| 12 | `HoaDonController` | `PUT {id}/phu-thu` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageInvoices)` | Admin, Manager, FrontDesk, Accountant |
| 13 | `HoaDonController` | `POST thanh-toan` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManagePayments)` | Admin, Manager, FrontDesk, Accountant |
| 14 | `BuongPhongController`| `GET nhiem-vu` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageHousekeeping) \|\| User.HasPermission(Quyen.UpdateRoomStatus)` | Admin, Manager, FrontDesk, Housekeeping |
| 15 | `BuongPhongController`| `POST phan-cong` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageHousekeeping)` | Admin, Manager, Housekeeping |
| 16 | `BuongPhongController`| `PUT nhiem-vu/{id}/trang-thai`| `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageHousekeeping) \|\| User.HasPermission(Quyen.UpdateRoomStatus)` | Admin, Manager, FrontDesk, Housekeeping |
| 17 | `BuongPhongController`| `POST hu-hong` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageHousekeeping) \|\| User.HasPermission(Quyen.CheckInOut)` | Admin, Manager, FrontDesk, Housekeeping |
| 18 | `ChiPhiController` | `GET /` (`DanhSach`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageExpenses)` | Admin, Manager, Accountant |
| 19 | `ChiPhiController` | `POST /` (`Tao`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageExpenses)` | Admin, Manager, Accountant |
| 20 | `VoucherController` | `GET /` (`DanhSach`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageVouchers)` | Admin, Manager |
| 21 | `DashboardController` | `GET tong-quan` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ViewDashboard)` | Admin, Manager, Accountant |
| 22 | `DashboardController` | `GET kpi` | `!User.HasPermission(ViewDashboard) && !User.IsStaffOrAdmin()` | `!User.HasPermission(Quyen.ViewDashboard)` | Admin, Manager, Accountant |
| 23 | `DashboardController` | `POST doanh-thu` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ViewDashboard)` | Admin, Manager, Accountant |
| 24 | `NguoiDungController` | `GET /` (`DanhSach`) | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageUsers) \|\| User.IsManagerOrAdmin()` | Admin, Manager |
| 25 | `NguoiDungController` | `GET thong-ke` | `User.IsStaffOrAdmin()` | `User.HasPermission(Quyen.ManageUsers) \|\| User.IsManagerOrAdmin()` | Admin, Manager |

---

### 🔹 PHASE 3: Vá Lỗ Hổng BOLA/IDOR, SoD (Four-Eyes) & Ràng Buộc Logic Nghiệp Vụ
**Mục tiêu:** Ngăn chặn thao túng dữ liệu chéo tài khoản và đảm bảo nguyên tắc kiểm soát nội bộ 4 mắt (Four-Eyes Principle) tại tầng REST API.

#### Danh sách công việc (Checklist):
- [x] **3.1. Chặn tự duyệt chi phí (Segregation of Duties) tại [ChiPhiController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/ChiPhiController.cs#L103-L118):**
  - Chỉ cho phép `User.IsManagerOrAdmin()` thực hiện phê duyệt (`DuyetChiPhi`). Kế toán (`Accountant`) là người lập phiếu nên không được quyền duyệt.
  - Bắt buộc kiểm tra `cp.NguoiGhiNhan == currentUserId`: Nếu người duyệt trùng với người tạo phiếu chi (và không phải Root Admin khẩn cấp hoặc chặn tuyệt đối theo chuẩn test GAP-02 `creatorUserId != approverUserId`), trả về `400 BadRequest` với thông báo vi phạm nguyên tắc phân tách trách nhiệm (SoD).
- [x] **3.2. Chặn nhân viên sửa hồ sơ người khác tại [NguoiDungController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/NguoiDungController.cs#L93-L108):**
  - Trong `PUT /api/v1/nguoi-dung/{id}` (`CapNhat`), đổi điều kiện từ `if (!User.IsStaffOrAdmin() && id != currentUserId)` thành:
    ```csharp
    if (!User.HasPermission(Quyen.ManageUsers) && id != currentUserId)
        return Forbid();
    ```
  - Chỉ **chính chủ tài khoản** hoặc **Admin (`ManageUsers`)** mới được cập nhật Họ tên, SĐT của tài khoản đó.
- [x] **3.3. Chặn đặt dịch vụ phòng cho đơn đã kết thúc/đã hủy tại [DichVuController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/DichVuController.cs#L38-L54):**
  - Trong `POST /api/v1/dich-vu/dat` (`DatDichVu`), kiểm tra quyền: Nếu là nhân viên đặt hộ khách thì phải có `User.HasPermission(Quyen.OrderRoomService)`; nếu là khách hàng thì phải là chủ đơn (`chiTietDatPhong.MaDatPhongNavigation?.MaNguoiDung == currentUserId`).
  - Kiểm tra trạng thái `chiTietDatPhong.TrangThai`: Nếu đã `Cancelled` (`da_huy`) hoặc `CheckedOut` (`hoan_thanh`), trả về `400 BadRequest("Không thể đặt dịch vụ cho phòng đã trả hoặc đơn đã hủy.")`.

---

### 🔹 PHASE 4: Bổ Sung Lưu Vết Kiểm Toán (Audit Trail) Cho Các Thao Tác Nhạy Cảm
**Mục tiêu:** Đảm bảo 100% thao tác thay đổi dữ liệu cấu hình và tài chính đều được ghi nhận vào bảng `NhatKyHeThong` qua `IAuditService`.

#### Danh sách công việc (Checklist):
- [x] **4.1. Bổ sung Audit Log vào [LoaiPhongController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/LoaiPhongController.cs):**
  - Inject `IAuditService auditService` vào primary constructor `LoaiPhongController(DACNContext db, IAuditService auditService)`.
  - Ghi log `TAO_LOAI_PHONG` trong hàm `Tao`.
  - Ghi log `CAP_NHAT_LOAI_PHONG` trong hàm `CapNhat`.
  - Ghi log `XOA_LOAI_PHONG` trong hàm `Xoa`.
- [x] **4.2. Bổ sung Audit Log vào [ChiPhiController.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Controllers/ChiPhiController.cs):**
  - Ghi log `DUYET_CHI_PHI_VAN_HANH` trong hàm `DuyetChiPhi` kèm thông tin người duyệt, trạng thái mới và số tiền.

---

### 🔹 PHASE 5: Kiểm Thử Tự Động & Xác Minh Hồi Quy (Checkpoint Verification)
**Mục tiêu:** Chứng minh bằng bộ test tự động rằng mọi lỗ hổng đã được bịt kín và không làm gãy bất kỳ tính năng hiện có nào.

#### Danh sách bài test sẽ bổ sung vào [Tv1Tv4ApiTests.cs](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/tests/DACN.IntegrationTests/Api/Tv1Tv4ApiTests.cs):
- [x] **Test 1 (`Rbac_Housekeeper_CannotCreateLoaiPhong_Returns403`):** Nhân viên Buồng phòng gọi `POST /api/v1/loai-phong` $\rightarrow$ Nhận `403 Forbidden`.
- [x] **Test 2 (`Rbac_Accountant_CannotCheckInBooking_Returns403`):** Kế toán gọi `PUT /api/v1/dat-phong/1/check-in` $\rightarrow$ Nhận `403 Forbidden`.
- [x] **Test 3 (`Rbac_Housekeeper_CannotViewFinancialDashboardKpi_Returns403`):** Buồng phòng gọi `GET /api/v1/dashboard/kpi` $\rightarrow$ Nhận `403 Forbidden`.
- [x] **Test 4 (`Rbac_FrontDesk_CannotViewOrCreateExpenses_Returns403`):** Lễ tân gọi `GET /api/v1/chi-phi` hoặc `POST /api/v1/chi-phi` $\rightarrow$ Nhận `403 Forbidden`.
- [x] **Test 5 (`SoD_Manager_CannotApproveOwnExpense_Returns400`):** Quản lý tự gọi `PUT /api/v1/chi-phi/{id}/duyet` cho phiếu chi do chính mình tạo $\rightarrow$ Nhận `400 BadRequest`.
- [x] **Test 6 (`Bola_Receptionist_CannotUpdateAdminProfile_Returns403`):** Lễ tân gọi `PUT /api/v1/nguoi-dung/1` để sửa tên Admin $\rightarrow$ Nhận `403 Forbidden`.
- [x] **Checkpoint tổng:** Chạy `dotnet test` toàn bộ Solution (`DACN.UnitTests`: 71/71 Passed + `DACN.IntegrationTests`: 47/47 API Tests & 5/5 LiveDB Tests = **123/123 PASSED 100%**).
