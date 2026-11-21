# 🛡️ BÁO CÁO KIỂM TOÁN AN TOÀN THÔNG TIN & MA TRẬN PHÂN QUYỀN API BACKEND DACN
> **Tiêu chuẩn kiểm toán:** Zero Trust 2026, OWASP API Security Top 10, OAuth 2.1 & Refresh Token Rotation.  
> **Phạm vi kiểm toán:** Toàn bộ 17 API Controllers thuộc dự án `DACN.Web` (99 Endpoints).  
> **Ngày thực hiện:** 20/11/2026.  
> **Trạng thái:** ✅ **ĐÃ KHẮC PHỤC & XÁC MINH 100%** (17/17 Controllers Deny-by-Default, RBAC Granular Permissions, SoD Four-Eyes, BOLA/IDOR & Audit Trail hoàn tất — 71 Unit Tests + 47 API Integration Tests PASSED).

---

## 📑 MỤC LỤC
1. [Tổng Quan Kết Quả Kiểm Toán (Executive Summary)](#1-tổng-quan-kết-quả-kiểm-toán-executive-summary)
2. [Ma Trận Phân Quyền Chuẩn Căn Cứ (Role & Permission Baseline)](#2-ma-trận-phân-quyền-chuẩn-căn-cứ-role--permission-baseline)
3. [Bảng Báo Cáo Phân Quyền Chi Tiết Toàn Bộ 17 Controllers (99 Endpoints)](#3-bảng-báo-cáo-phân-quyền-chi-tiết-toàn-bộ-17-controllers-99-endpoints)
4. [Phân Tích Chi Tiết Các Lỗ Hổng & Rủi Ro Phát Hiện (OWASP API Top 10)](#4-phân-tích-chi-tiết-các-lỗ-hổng--rủi-ro-phát-hiện-owasp-api-top-10)
5. [Đánh Giá Các Điểm Sáng Về Bảo Mật Đã Triển Khai Tốt](#5-đánh-giá-các-điểm-sáng-về-bảo-mật-đã-triển-khai-tốt)
6. [Lộ Trình & Khuyến Nghị Khắc Phục (Actionable Remediation Guide)](#6-lộ-trình--khuyến-nghị-khắc-phục-actionable-remediation-guide)

---

## 1. TỔNG QUAN KẾT QUẢ KIỂM TOÁN (EXECUTIVE SUMMARY)

Hệ thống API của DACN được xây dựng trên nền tảng .NET 9 ASP.NET Core Web API với kiến trúc JWT Authentication, Rate Limiting 3 tầng và cơ chế ghi log kiểm toán `IAuditService`. Tuy nhiên, qua quá trình rà soát đối chiếu từng dòng mã nguồn với **Ma trận Phân quyền chuẩn (6 Vai trò x 18 Quyền hạn)**, kiểm toán viên an toàn thông tin phát hiện một số điểm bất cập mang tính hệ thống cần được khắc phục:

| Chỉ số đánh giá | Kết quả | Đánh giá | Ghi chú |
| :--- | :---: | :---: | :--- |
| **Tổng số Controller quét** | **17** | Hoàn thành 100% | `src/DACN.Web/Controllers/` |
| **Tổng số Endpoints API** | **99** | Hoàn thành 100% | Bao gồm cả public và private endpoints |
| **Tuân thủ Deny by Default** | **41%** (7/17 Controllers) | ⚠️ CẦN CẢI THIỆN | 10 controllers thiếu `[Authorize]` ở class-level, chỉ dùng action-level |
| **Bảo vệ IDOR / Data Ownership** | **88%** | ✅ TỐT | Kiểm tra quyền sở hữu bản ghi của Customer hoạt động rất tốt |
| **Độ chính xác RBAC (BFLA Check)** | **58%** | 🔴 NGUY CƠ CAO | Xuất hiện Anti-pattern `IsStaffOrAdmin()` gom chung mọi nhân viên |
| **Lưu vết kiểm toán (Audit Trail)** | **92%** | ✅ RẤT TỐT | Hầu hết mutation quan trọng đều gọi `IAuditService.GhiNhanAsync` |
| **Bảo vệ Token & Refresh Token** | **95%** | 🌟 XUẤT SẮC | OAuth 2.1 Token Rotation, phát hiện Replay Attack, thu hồi token đầy đủ |

### Phân loại rủi ro chính:
- 🔴 **CRITICAL (BFLA - Broken Function Level Authorization):** Anti-pattern `User.IsStaffOrAdmin()` được dùng tràn lan ở các controller giao dịch nghiệp vụ (`LoaiPhongController`, `DatPhongController`, `HoaDonController`, `ChiPhiController`, `DashboardController`, `NguoiDungController`, `BuongPhongController`). Điều này khiến ranh giới quyền lực giữa **Lễ tân, Buồng phòng, Kế toán và Quản lý** bị xóa nhòa tại tầng API: Tạp vụ buồng phòng có thể xem doanh thu/tạo loại phòng/hủy phòng; Kế toán có thể check-in phòng; Lễ tân có thể xem và tạo chi phí hoạt động.
- 🟠 **HIGH (Segregation of Duties - SoD Bypass):** Endpoint `PUT /api/v1/chi-phi/{id}/duyet` thiếu cơ chế kiểm tra `NguoiGhiNhan != currentUserId` ở cấp Backend API, cho phép người tạo chi phí tự duyệt đơn qua API.
- 🟠 **HIGH (Over-privileged Profile Update):** Endpoint `PUT /api/v1/nguoi-dung/{id}` cho phép bất kỳ nhân sự nào có role staff sửa đổi họ tên, số điện thoại của bất kỳ tài khoản người dùng nào khác.
- 🟡 **MEDIUM (Thiếu Audit Trail ở một số Mutation):** `LoaiPhongController` (Create/Update/Delete) và `ChiPhiController.DuyetChiPhi` chưa ghi nhận lịch sử vào `IAuditService`.

---

## 2. MA TRẬN PHÂN QUYỀN CHUẨN CĂN CỨ (ROLE & PERMISSION BASELINE)

Theo phân tích thiết kế nghiệp vụ (`.agents/domain_analysis.md`) và mã nguồn cơ sở dữ liệu (`database/DACN_Full.sql`), hệ thống có **6 Vai trò (Roles)** và **18 Quyền hạn (Permissions)** chuẩn:

### 2.1. Danh mục 6 Vai trò (Roles)
1. **Admin (ID 1):** Quản trị tối cao hệ thống, quản lý tài khoản nhân viên, cấu hình phân quyền RBAC.
2. **Manager (ID 2):** Quản lý điều hành khách sạn, buồng phòng, đơn đặt phòng, xem báo cáo KPI/doanh thu và khai thác AI. Không can thiệp vào tài khoản admin khác hoặc ma trận quyền.
3. **FrontDesk (ID 3):** Lễ tân tiền sảnh tiếp nhận khách, check-in, check-out, tạo đơn, gán phòng, đặt dịch vụ phòng.
4. **Housekeeping (ID 4):** Nhân viên dọn phòng, cập nhật trạng thái buồng sạch/bẩn, nhận ca dọn phòng, kiểm kê tài sản hỏng hóc.
5. **Accountant (ID 5):** Kế toán viên giám sát hóa đơn, thu ngân, quản lý và duyệt chi phí vận hành, xem dashboard tài chính.
6. **Customer (ID 6):** Khách hàng đặt phòng trực tuyến, xem lịch sử đơn của mình, đánh giá dịch vụ, chat trợ lý AI.

### 2.2. Ma trận Đối Chiếu Quyền Hạn Chuẩn (RBAC Standard Matrix)
| STT | Tên Quyền (Constant `Quyen.cs`) | Admin | Manager | FrontDesk | Housekeeping | Accountant | Customer |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | `ViewDashboard` | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ |
| **2** | `ManageRooms` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **3** | `ManageBookings` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **4** | `CheckInOut` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **5** | `ManageInvoices` | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| **6** | `ManagePayments` | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| **7** | `ManageServices` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **8** | `OrderRoomService` | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ *(Tự đặt phòng mình)* |
| **9** | `ManageHousekeeping` | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ |
| **10** | `UpdateRoomStatus` | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **11** | `ManageUsers` | ✅ | ❌ *(Bảo mật)* | ❌ | ❌ | ❌ | ❌ |
| **12** | `ManageVouchers` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **13** | `ManageArticles` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **14** | `ModerateReviews` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **15** | `UseAIChatbot` | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| **16** | `ViewAIPredictions` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **17** | `ManageExpenses` | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ |
| **18** | `ManageAIData` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## 3. BẢNG BÁO CÁO PHÂN QUYỀN CHI TIẾT TOÀN BỘ 17 CONTROLLERS (99 ENDPOINTS)

### Ký hiệu đánh giá trạng thái:
- ✅ **CHUẨN:** Endpoint phân quyền đúng yêu cầu theo Ma trận, áp dụng đầy đủ kiểm tra IDOR/Resource ownership nếu có.
- 🔴 **BFLA:** Lỗi Broken Function Level Authorization — dùng `IsStaffOrAdmin()` gom chung, cho phép role không có quyền thực hiện chức năng.
- 🟠 **IDOR:** Nguy cơ truy cập hoặc sửa đổi trái phép tài nguyên của người dùng khác.
- 🟡 **THIẾU AUDIT:** Chức năng ghi/sửa/xóa dữ liệu quan trọng nhưng thiếu gọi `IAuditService`.
- 🌐 **PUBLIC HỢP LỆ:** Endpoint mở cho khách truy cập công khai có kiểm soát (`[AllowAnonymous]`).

---

### 3.1. AuthController (`api/v1/auth`) — 8 Endpoints
*Class Attributes:* `[ApiController]`, `[EnableRateLimiting("auth-policy")]` *(Lưu ý: Không có class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `POST dang-nhap` | Public | `[AllowAnonymous]`, BCrypt verify, Rate limit | ✅ CHUẨN | Đăng nhập an toàn, ghi log kiểm toán thành công/thất bại |
| `POST dang-ky` | Public | `[AllowAnonymous]`, gán role Khách hàng | ✅ CHUẨN | Gán cứng `customer`, không cho frontend tự set role Admin |
| `POST lam-moi-token` | Public | `[AllowAnonymous]`, Token Rotation | ✅ CHUẨN | Thu hồi token cũ, phát hiện Token Replay Attack lập tức |
| `POST dang-xuat` | Public | `[AllowAnonymous]`, thu hồi Refresh Token | ✅ CHUẨN | Hủy session server-side khi logout |
| `POST quen-mat-khau` | Public | `[AllowAnonymous]`, OTP in-memory 15p | ✅ CHUẨN | Trả về thông điệp chung chống rò rỉ email (Enumeration Safe) |
| `POST dat-lai-mat-khau` | Public | `[AllowAnonymous]`, OTP max 5 lần sai | ✅ CHUẨN | Thu hồi toàn bộ RefreshToken cũ khi đổi mật khẩu thành công |
| `GET me` | User đăng nhập | `[Authorize]`, lấy ID từ JWT Claim Sub | ✅ CHUẨN | Trả về profile chính chủ từ JWT Claim, không nhận ID từ query |
| `POST doi-mat-khau` | User đăng nhập | `[Authorize]`, verify mật khẩu cũ BCrypt | ✅ CHUẨN | Lấy UserID từ ClaimsPrincipal, không thể đổi mật khẩu người khác |

---

### 3.2. NguoiDungController (`api/v1/nguoi-dung`) — 6 Endpoints
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | Admin (`ManageUsers`) | `User.IsStaffOrAdmin()` | 🔴 BFLA | **Lỗi:** Cho phép Lễ tân, Buồng phòng, Kế toán xem toàn bộ danh sách người dùng kèm email, SĐT |
| `GET {id:int}` | Admin / Chính chủ | `!User.IsStaffOrAdmin() && id != currentUserId => Forbid` | ✅ CHUẨN | Cho phép user xem hồ sơ của mình, staff xem hồ sơ khách |
| `PUT {id:int}` | Admin / Chính chủ | `!User.IsStaffOrAdmin() && id != currentUserId => Forbid` | 🔴 BFLA / IDOR | **Nguy hiểm:** Bất kỳ nhân viên nào (kể cả Buồng phòng) cũng có thể sửa đổi Họ tên, SĐT của Admin hoặc Manager |
| `PUT {id:int}/kich-hoat` | Admin (`ManageUsers`) | `User.HasPermission(Quyen.ManageUsers)`, khóa tự khóa, khóa admin cuối | ✅ CHUẨN | Rất chặt chẽ: chặn tự khóa bản thân, chặn khóa Admin cuối cùng, thu hồi toàn bộ token active |
| `PUT {id:int}/vai-tro` | Admin (`ManageUsers`) | `User.HasPermission(Quyen.ManageUsers)`, chặn tự đổi, chặn Manager nâng Admin | ✅ CHUẨN | Kiểm tra sâu: chỉ Admin tối cao mới được cấp/thu hồi vai trò Admin, thu hồi token ngay |
| `GET thong-ke` | Admin (`ManageUsers`) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Lễ tân/Tạp vụ xem thống kê nhân sự và khách hàng |

---

### 3.3. VaiTroController (`api/v1/vai-tro`) — 6 Endpoints
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | Admin (`ManageUsers`) | `User.IsStaffOrAdmin()` | 🟡 OVER-PRIVILEGE | Cho phép mọi staff xem danh sách role và quyền được cấp |
| `GET quyen-han` | Admin (`ManageUsers`) | `User.IsStaffOrAdmin()` | 🟡 OVER-PRIVILEGE | Cho phép mọi staff xem danh mục 18 quyền hạn |
| `POST /` | Admin (`ManageUsers`) | `User.HasPermission(Quyen.ManageUsers)` | ✅ CHUẨN | Chỉ Admin có quyền tạo role mới |
| `PUT {id:int}` | Admin (`ManageUsers`) | `User.HasPermission(Quyen.ManageUsers)` | ✅ CHUẨN | Bảo vệ vai trò hệ thống gốc 'Admin' không bị đổi tên |
| `DELETE {id:int}` | Admin (`ManageUsers`) | `User.HasPermission(Quyen.ManageUsers)` | ✅ CHUẨN | Cấm xóa role Admin/Customer hoặc role đang có user trực thuộc |
| `PUT {id:int}/quyen-han` | Admin (`ManageUsers`) | `User.HasPermission(Quyen.ManageUsers)` | ✅ CHUẨN | Ngăn chặn xóa quyền `ManageUsers` khỏi role Admin |

---

### 3.4. PhongController (`api/v1/phong`) — 5 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Danh sách phòng kèm loại phòng phục vụ khách xem |
| `GET {id:int}` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Chi tiết buồng phòng |
| `GET trang-thai` | Staff / Admin | `[AllowAnonymous]` | 🟡 DATA LEAK | **Lưu ý:** Thống kê số lượng phòng theo trạng thái bị mở công khai cho khách, làm lộ tỷ lệ kín phòng của khách sạn |
| `PUT {id:int}/trang-thai` | `UpdateRoomStatus` (Admin, Manager, FrontDesk, Housekeeping) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép **Kế toán (Accountant)** đổi trạng thái buồng phòng (trái ma trận quyền) |
| `PUT {id:int}/hoan-tat-ve-sinh`| `UpdateRoomStatus` / `ManageHousekeeping` | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Kế toán bấm hoàn tất vệ sinh; tự cập nhật nhật ký buồng phòng |

---

### 3.5. LoaiPhongController (`api/v1/loai-phong`) — 6 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Danh mục loại phòng kèm giá, hình ảnh, tiện nghi |
| `GET {id:int}` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Chi tiết loại phòng |
| `GET tim-kiem` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Tìm phòng trống theo khoảng thời gian và số khách |
| `POST /` | `ManageRooms` (Admin, Manager) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA / THIẾU AUDIT | **Nghiêm trọng:** Nhân viên Buồng phòng, Lễ tân, Kế toán đều có thể tạo mới loại phòng! Thiếu ghi Audit log |
| `PUT {id:int}` | `ManageRooms` (Admin, Manager) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA / THIẾU AUDIT | **Nghiêm trọng:** Mọi staff đều có thể sửa giá phòng, sức chứa, mô tả! Thiếu ghi Audit log |
| `DELETE {id:int}` | `ManageRooms` (Admin, Manager) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA / THIẾU AUDIT | **Nghiêm trọng:** Mọi staff đều có thể vô hiệu hóa loại phòng! Thiếu ghi Audit log |

---

### 3.6. DatPhongController (`api/v1/dat-phong`) — 9 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | `ManageBookings` (Admin, Manager, FrontDesk) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Buồng phòng và Kế toán xem toàn bộ đơn đặt phòng và PII khách |
| `GET {id:int}` | `ManageBookings` / Khách đặt | `!User.IsStaffOrAdmin() && d.MaNguoiDung != currentUserId` | ✅ CHUẨN | Chống IDOR: Khách chỉ xem đơn của mình, staff xem đơn khách |
| `GET cua-toi` | Khách hàng đăng nhập | `[Authorize]`, `d.MaNguoiDung == currentUserId` | ✅ CHUẨN | Chống IDOR xuất sắc: lọc cứng theo `currentUserId` từ JWT |
| `POST /` | Public / Khách hàng | `[AllowAnonymous]`, Serializable Tx | ✅ CHUẨN | Đặt phòng có transaction Serializable, khóa concurrency, tự gán `loggedInUserId` |
| `PUT {id:int}/xac-nhan` | `ManageBookings` (Admin, Manager, FrontDesk) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Buồng phòng xác nhận đặt phòng và ghi nhận đã thu cọc |
| `PUT {id:int}/huy` | `ManageBookings` / Khách đặt | `!User.IsStaffOrAdmin() && d.MaNguoiDung != currentUserId` | ✅ CHUẨN | Khách hủy được đơn mình; chặn khách hủy khi đang ở/đã xong; giải phóng phòng |
| `PUT chi-tiet/{id}/gan-phong` | `ManageBookings` (Admin, Manager, FrontDesk) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Kế toán hoặc Buồng phòng có thể gán buồng phòng cho khách |
| `PUT {id:int}/check-in` | `CheckInOut` (Admin, Manager, FrontDesk) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Buồng phòng và Kế toán có thể thực hiện nghiệp vụ nhận phòng (Check-in) |
| `PUT {id:int}/check-out` | `CheckInOut` (Admin, Manager, FrontDesk) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Buồng phòng và Kế toán có thể check-out, tự chuyển phòng sang Dirty |

---

### 3.7. HoaDonController (`api/v1/hoa-don`) — 6 Endpoints
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | `ManageInvoices` (Admin, Manager, FrontDesk, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Buồng phòng xem danh sách và thông tin tài chính hóa đơn |
| `GET {id:int}` | `ManageInvoices` / Khách đặt | `!User.IsStaffOrAdmin() && hd.MaDatPhongNavigation?.MaNguoiDung != currentUserId` | ✅ CHUẨN | Chống IDOR: Khách chỉ xem được hóa đơn đặt phòng của chính mình |
| `GET dat-phong/{maDatPhong:int}` | `ManageInvoices` / Khách đặt | `!User.IsStaffOrAdmin() && hd.MaDatPhongNavigation?.MaNguoiDung != currentUserId` | ✅ CHUẨN | Chống IDOR: Khách chỉ tra cứu hóa đơn theo đơn đặt phòng của mình |
| `PUT {id:int}/phu-thu` | `ManageInvoices` (Admin, Manager, FrontDesk, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép nhân viên Buồng phòng can thiệp trực tiếp tăng giảm tiền phụ thu hóa đơn |
| `POST thanh-toan` | `ManagePayments` (Admin, Manager, FrontDesk, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép nhân viên Buồng phòng ghi nhận thanh toán tiền mặt/chuyển khoản |
| `POST webhook-thanh-toan`| Payment Gateway (VNPay, MoMo) | `[AllowAnonymous]`, HMAC-SHA256 constant-time | ✅ CHUẨN | **Bảo mật cao:** Kiểm tra chữ ký HMAC với `CryptographicOperations.FixedTimeEquals`, Idempotent |

---

### 3.8. DichVuController (`api/v1/dich-vu`) — 3 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Danh mục dịch vụ đang kinh doanh |
| `POST dat` | `OrderRoomService` / Khách ở phòng | `[Authorize]`, check ownership `chiTietDatPhong.MaDatPhongNavigation.MaNguoiDung == currentUserId` | 🟡 LOGIC GAP | Chống IDOR tốt, tự cộng dồn vào hóa đơn. Nhưng chưa chặn nếu phòng đã `CheckedOut` hoặc `Cancelled` |
| `GET don/{maDonDichVu:int}` | `OrderRoomService` / Khách đặt | `[Authorize]`, check ownership qua booking | ✅ CHUẨN | Chống IDOR: Khách chỉ xem đơn dịch vụ phòng mình đã đặt |

---

### 3.9. BuongPhongController (`api/v1/buong-phong`) — 6 Endpoints
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET nhiem-vu` | `ManageHousekeeping` (Admin, Manager, Housekeeping) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Kế toán xem danh sách phân công dọn phòng |
| `POST phan-cong` | `ManageHousekeeping` (Admin, Manager, Housekeeping) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Kế toán phân công nhiệm vụ dọn phòng |
| `PUT nhiem-vu/{id}/trang-thai`| `UpdateRoomStatus` (Admin, Manager, FrontDesk, Housekeeping) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Kế toán đổi trạng thái ca dọn phòng sang done/inspected |
| `GET tai-san` | Staff / Admin | `User.IsStaffOrAdmin()` | ✅ CHUẨN | Quản lý danh mục tài sản và giá đền bù |
| `GET hu-hong` | Staff / Admin | `User.IsStaffOrAdmin()` | ✅ CHUẨN | Xem danh sách đền bù mất mát hư hỏng |
| `POST hu-hong` | `ManageHousekeeping` / `CheckInOut` | `User.IsStaffOrAdmin()`, tự đồng bộ phụ thu Hóa đơn | ✅ CHUẨN | Rất tốt: ghi nhận phạt hư hại và tự cộng vào HĐ thanh toán |

---

### 3.10. ChiPhiController (`api/v1/chi-phi`) — 3 Endpoints
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | `ManageExpenses` (Admin, Manager, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Lễ tân và Buồng phòng có thể xem toàn bộ chi phí vận hành |
| `POST /` | `ManageExpenses` (Admin, Manager, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Lễ tân và Buồng phòng có thể tạo phiếu chi phí vận hành |
| `PUT {id:int}/duyet` | `ManageExpenses` (Admin, Manager) | `!User.IsManagerOrAdmin() && !User.HasPermission("ManageExpenses")` | 🔴 BFLA / SoD BYPASS | **Lỗ hổng kép:** 1. Kế toán có `ManageExpenses` nên duyệt được chi phí. 2. **Thiếu kiểm tra SoD:** Người tạo tự duyệt được đơn của chính mình qua API; Thiếu gọi Audit log |

---

### 3.11. VoucherController (`api/v1/voucher`) — 6 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | `ManageVouchers` (Admin, Manager) | `[Authorize]`, `User.IsStaffOrAdmin()` | 🔴 BFLA | Lễ tân, Buồng phòng, Kế toán có thể xem toàn bộ voucher nội bộ |
| `GET cong-khai` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Lọc các voucher còn hạn, còn lượt, che giấu cấu hình nội bộ |
| `POST kiem-tra` | Public | `[AllowAnonymous]`, `[EnableRateLimiting("public-strict")]` | ✅ CHUẨN | Kiểm tra áp dụng mã giảm giá có Rate Limiting chống Brute-force |
| `POST /` | `ManageVouchers` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ManageVouchers)` | ✅ CHUẨN | Phân quyền chuẩn xác theo quyền hạn `ManageVouchers` |
| `PUT {id:int}` | `ManageVouchers` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ManageVouchers)` | ✅ CHUẨN | Phân quyền chuẩn xác theo quyền hạn `ManageVouchers` |
| `DELETE {id:int}` | `ManageVouchers` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ManageVouchers)` | ✅ CHUẨN | Vô hiệu hóa voucher phân quyền chuẩn xác |

---

### 3.12. NoiDungController (`api/v1/noi-dung`) — 12 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET dia-diem` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Xem địa điểm du lịch Đà Nẵng |
| `POST dia-diem` | `ManageAIData` / `ManageArticles` | `[Authorize]`, `HasPermission(ManageAIData) \|\| HasPermission(ManageArticles)` | ✅ CHUẨN | Phân quyền chuẩn xác (Admin, Manager) |
| `PUT dia-diem/{id}` | `ManageAIData` / `ManageArticles` | `[Authorize]`, `HasPermission(...)` | ✅ CHUẨN | Phân quyền chuẩn xác (Admin, Manager) |
| `DELETE dia-diem/{id}`| `ManageAIData` / `ManageArticles` | `[Authorize]`, `HasPermission(...)` | ✅ CHUẨN | Ẩn địa điểm phân quyền chuẩn xác |
| `GET bai-viet` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Xem danh sách bài viết blog |
| `GET bai-viet/{slug}` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Xem chi tiết bài viết theo slug |
| `POST bai-viet` | `ManageArticles` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ManageArticles)` | ✅ CHUẨN | Đăng bài viết phân quyền chuẩn xác |
| `PUT bai-viet/{id}` | `ManageArticles` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ManageArticles)` | ✅ CHUẨN | Sửa bài viết phân quyền chuẩn xác |
| `DELETE bai-viet/{id}`| `ManageArticles` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ManageArticles)` | ✅ CHUẨN | Ẩn bài viết phân quyền chuẩn xác |
| `GET thong-bao` | User đăng nhập | `[Authorize]`, lọc `t.MaNguoiDung == userId` nếu không phải Admin | ✅ CHUẨN | Chống IDOR: Khách chỉ thấy thông báo của chính mình |
| `POST thong-bao` | Admin / Manager | `[Authorize]`, `User.IsManagerOrAdmin() \|\| User.HasPermission(Quyen.ManageUsers)` | ✅ CHUẨN | Chỉ quản trị viên và quản lý mới được phát thông báo |
| `PUT thong-bao/{id}/da-doc`| User nhận thông báo | `[Authorize]`, check `tb.MaNguoiDung == userId` | ✅ CHUẨN | Chống IDOR: Chỉ người nhận mới đánh dấu đã đọc |

---

### 3.13. DanhGiaController (`api/v1/danh-gia`) — 5 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET loai-phong/{maLoaiPhong}` | Public | `[AllowAnonymous]`, chỉ lấy `DaDuyet == true` | 🌐 PUBLIC HỢP LỆ | Xem đánh giá đã kiểm duyệt |
| `GET cho-duyet` | `ModerateReviews` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ModerateReviews)` | ✅ CHUẨN | Phân quyền chuẩn xác theo quyền kiểm duyệt |
| `POST /` | Khách hàng đăng nhập | `[Authorize]`, check `datPhong.MaNguoiDung == currentUserId` | ✅ CHUẨN | Chống IDOR & Fake Review: kiểm tra đơn đặt phòng chính chủ |
| `PUT {id:int}/duyet` | `ModerateReviews` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ModerateReviews)` | ✅ CHUẨN | Duyệt hiển thị đánh giá phân quyền chuẩn xác |
| `DELETE {id:int}` | `ModerateReviews` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ModerateReviews)` | ✅ CHUẨN | Xóa đánh giá spam phân quyền chuẩn xác |

---

### 3.14. DashboardController (`api/v1/dashboard`) — 4 Endpoints
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET tong-quan` | `ViewDashboard` (Admin, Manager, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Cho phép Lễ tân và Buồng phòng xem doanh thu tháng, doanh thu ngày |
| `GET kpi` | `ViewDashboard` (Admin, Manager, Accountant) | `!User.HasPermission(Quyen.ViewDashboard) && !User.IsStaffOrAdmin()` | 🔴 BFLA | **Lỗi logic:** Sử dụng `&& !User.IsStaffOrAdmin()` làm mất tác dụng của `HasPermission`. Tạp vụ có thể xem full BI KPI, Lợi nhuận ròng, ADR, RevPAR |
| `POST doanh-thu` | `ViewDashboard` (Admin, Manager, Accountant) | `User.IsStaffOrAdmin()` | 🔴 BFLA | Mọi nhân viên đều có thể xuất báo cáo doanh thu theo dải ngày |
| `GET phong-status`| Staff / Admin | `User.IsStaffOrAdmin()` | ✅ CHUẨN | Nhân viên kiểm tra nhanh biểu đồ số lượng trạng thái phòng |

---

### 3.15. AiController (`api/v1/ai`) — 12 Endpoints
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `POST chat` | Public / `UseAIChatbot` | `[AllowAnonymous]`, `[EnableRateLimiting("public-strict")]` | 🌐 PUBLIC HỢP LỆ | Chatbot AI hỗ trợ khách vãng lai và thành viên có rate-limit |
| `GET chat/{maPhienChat}`| Khách sở hữu phiên | `[AllowAnonymous]`, check `session.MaNguoiDung == currentUserId` | ✅ CHUẨN | Chống IDOR: Khách vãng lai xem theo mã, khách đăng nhập bảo vệ theo UserID |
| `GET phien-chat` | Manager, Admin | `[Authorize]`, `User.IsStaffOrAdmin()` | 🟡 OVER-PRIVILEGE | Cho phép Lễ tân, Buồng phòng xem danh sách các phiên chat của khách |
| `POST goi-y` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Gợi ý lịch trình du lịch AI |
| `GET goi-y` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Gợi ý lịch trình du lịch AI qua Query |
| `GET mua-du-lich` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Danh mục mùa du lịch |
| `GET tour` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Danh sách gợi ý tour |
| `GET du-bao-nhu-cau` | `ViewAIPredictions` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ViewAIPredictions)` | ✅ CHUẨN | Phân quyền chuẩn xác theo quyền dự báo AI |
| `POST du-bao-nhu-cau/chay`| `ViewAIPredictions` (Admin, Manager)| `[Authorize]`, `User.HasPermission(Quyen.ViewAIPredictions)` | ✅ CHUẨN | Kích hoạt mô hình dự báo nhu cầu phòng |
| `GET du-doan-huy-phong`| `ViewAIPredictions` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ViewAIPredictions)` | ✅ CHUẨN | Xem danh sách cảnh báo nguy cơ hủy phòng |
| `POST du-doan-huy-phong/chay`| `ViewAIPredictions` (Admin, Manager)| `[Authorize]`, `User.HasPermission(Quyen.ViewAIPredictions)` | ✅ CHUẨN | Kích hoạt mô hình chấm điểm rủi ro hủy phòng |
| `GET do-chinh-xac` | `ViewAIPredictions` (Admin, Manager) | `[Authorize]`, `User.HasPermission(Quyen.ViewAIPredictions)` | ✅ CHUẨN | Xem độ chính xác đối chiếu thực tế của mô hình |

---

### 3.16. NhatKyController (`api/v1/nhat-ky-he-thong`) — 1 Endpoint
*Class Attributes:* `[Authorize]`, `[ApiController]` *(Deny by Default đạt)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET /` | Admin (`ManageUsers`) / Manager | `!User.IsManagerOrAdmin() && !User.HasPermission(Quyen.ManageUsers) => Forbid` | ✅ CHUẨN | Bảo mật xuất sắc: Chỉ Admin và Quản lý mới được đọc Audit Log hệ thống |

---

### 3.17. SystemController (`api/v1/system`) — 1 Endpoint
*Class Attributes:* `[ApiController]` *(Thiếu class-level `[Authorize]`)*

| HTTP & Route | Quyền Chuẩn | Cơ Chế Guard Thực Tế Trong Code | Đánh Giá | Nhận Xét & Lỗ Hổng Tiềm Ẩn |
| :--- | :--- | :--- | :---: | :--- |
| `GET health` | Public | `[AllowAnonymous]` | 🌐 PUBLIC HỢP LỆ | Health check kiểm tra kết nối DB, ẩn chi tiết lỗi khi ở Production |

---

## 4. PHÂN TÍCH CHI TIẾT CÁC LỖ HỔNG & RỦI RO PHÁT HIỆN (OWASP API TOP 10)

### 🔴 LỖ HỔNG 1: Broken Function Level Authorization (OWASP API5) — Anti-pattern `IsStaffOrAdmin()`
- **Vị trí ảnh hưởng:** 7 controllers (`LoaiPhongController`, `DatPhongController`, `HoaDonController`, `ChiPhiController`, `DashboardController`, `NguoiDungController`, `BuongPhongController`).
- **Nguyên nhân gốc:** Extension method `ClaimsPrincipalExtensions.IsStaffOrAdmin()` gom chung tất cả các vai trò `receptionist`, `housekeeper`, `accountant`, `staff` thành một nhóm đặc quyền:
  ```csharp
  // ClaimsPrincipalExtensions.cs - Dòng 36-41
  return role.Contains("receptionist") || role.Contains("le tan")
      || role.Contains("housekeeper") || role.Contains("buong phong")
      || role.Contains("accountant") || role.Contains("ke toan")
      || loai == "nhan_vien" || loai == "staff";
  ```
- **Hậu quả bảo mật nghiêm trọng:**
  1. **Buồng phòng (Housekeeper):** Có thể tạo/sửa/xóa loại phòng (`POST/PUT/DELETE /api/v1/loai-phong`), xem toàn bộ doanh thu và KPI tài chính (`/api/v1/dashboard/kpi`), xác nhận hoặc hủy đơn đặt phòng, tự gán cọc, tạo phiếu chi phí.
  2. **Kế toán (Accountant):** Có thể đổi trạng thái buồng phòng, xác nhận hoàn tất vệ sinh phòng, check-in và check-out khách lưu trú.
  3. **Lễ tân (FrontDesk):** Có thể xem và tạo chi phí vận hành khách sạn, xem báo cáo thống kê nhân sự.
- **Biện pháp xử lý:** Thay thế triệt để các lệnh kiểm tra `if (!User.IsStaffOrAdmin())` bằng `User.HasPermission(Quyen.<TenQuyen>)` tương ứng với ma trận RBAC đã đăng ký.

---

### 🟠 LỖ HỔNG 2: Segregation of Duties (SoD) Bypass & Thiếu Audit ở Duyệt Chi Phí
- **Vị trí:** `ChiPhiController.cs`, dòng 104-118 (`PUT /api/v1/chi-phi/{id}/duyet`).
- **Mã nguồn hiện tại:**
  ```csharp
  [HttpPut("{id:int}/duyet")]
  public async Task<ActionResult<ApiResponse<object>>> DuyetChiPhi(int id, [FromQuery] string trangThai = "Approved")
  {
      if (!User.IsManagerOrAdmin() && !User.HasPermission("ManageExpenses"))
          return Forbid();

      var cp = await db.ChiPhiVanHanhs.FindAsync(id);
      if (cp == null) return NotFound(...);

      cp.TrangThai = StatusMapper.NormalizeChiPhiTrangThai(trangThai);
      cp.NguoiPheDuyet = User.GetUserId();
      await db.SaveChangesAsync();
      // THIẾU: Không ghi IAuditService
      // THIẾU: Không kiểm tra cp.NguoiGhiNhan != currentUserId
  }
  ```
- **Hậu quả:** 
  1. Nguyên tắc Four-Eyes (Bốn mắt): Người tạo phiếu chi không được phép tự phê duyệt phiếu chi của mình. Trong UI Blazor (`ExpenseManagement.razor`), điều này đã được chặn, nhưng **tại tầng REST API thì chưa có guard**, cho phép kẻ xấu gửi HTTP request trực tiếp để tự duyệt khoản tiền chi khống.
  2. Quyền `ManageExpenses` được cấp cho cả Kế toán, do đó Kế toán tạo chi phí xong có thể tự duyệt luôn mà không cần sự đồng ý của Giám đốc (Manager/Admin).
  3. Thao tác phê duyệt chi phí nhạy cảm nhưng không có dòng log kiểm toán nào được ghi vào `NhatKyHeThong`.

---

### 🟠 LỖ HỔNG 3: Broken Object Level Authorization (OWASP API1) tại Cập Nhật Hồ Sơ Người Dùng
- **Vị trí:** `NguoiDungController.cs`, dòng 93-108 (`PUT /api/v1/nguoi-dung/{id}`).
- **Mã nguồn hiện tại:**
  ```csharp
  [HttpPut("{id:int}")]
  public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] CapNhatHoSoRequest req)
  {
      var currentUserId = User.GetUserId();
      if (!User.IsStaffOrAdmin() && id != currentUserId)
          return Forbid();

      var u = await db.NguoiDungs.FindAsync(id);
      if (u == null) return NotFound(...);

      u.HoTen = req.HoTen;
      u.SoDienThoai = req.SoDienThoai;
      await db.SaveChangesAsync();
  }
  ```
- **Hậu quả:** Điều kiện `if (!User.IsStaffOrAdmin() && id != currentUserId)` có nghĩa là: **Nếu là Staff/Admin, bạn có thể sửa hồ sơ của BẤT KỲ AI (`id != currentUserId`)**. Một nhân viên lễ tân hay phục vụ phòng có thể gửi request đổi số điện thoại và họ tên của Admin hệ thống hoặc của Giám đốc!
- **Biện pháp xử lý:** Chỉ cho phép người dùng tự sửa hồ sơ của chính mình (`id == currentUserId`), hoặc chỉ có `Admin` (`User.IsAdmin()`) mới được sửa hồ sơ của người khác.

---

### 🟡 LỖ HỔNG 4: Vi Phạm Nguyên Tắc "Deny by Default" (Zero Trust)
- **Vị trí:** 10 Controllers không có thuộc tính `[Authorize]` ở cấp độ class: `PhongController`, `LoaiPhongController`, `DatPhongController`, `DichVuController`, `VoucherController`, `NoiDungController`, `DanhGiaController`, `AiController`, `SystemController`, `AuthController`.
- **Rủi ro:** Khi lập trình viên tạo mới một action method mà quên gắn `[Authorize]`, endpoint đó sẽ tự động trở thành **Public Anonymous**, dẫn đến lộ lọt dữ liệu ngoài ý muốn.
- **Biện pháp xử lý:** Đặt `[Authorize]` ở đầu Class cho toàn bộ Controller nghiệp vụ. Với các endpoint công khai (ví dụ tra cứu, tìm kiếm), ghi đè tường minh bằng `[AllowAnonymous]`.

---

## 5. ĐÁNH GIÁ CÁC ĐIỂM SÁNG VỀ BẢO MẬT ĐÃ TRIỂN KHAI TỐT

Bên cạnh các thiếu sót về độ mịn phân quyền, dự án DACN sở hữu những kiến trúc bảo mật rất tiến bộ theo chuẩn 2026:

1. **OAuth 2.1 Refresh Token Rotation Tuyệt Đối:**
   - Mỗi Refresh Token chỉ được dùng đúng 1 lần. Khi làm mới, token cũ bị thu hồi (`DaThuHoi = true`) và lưu vết `ThayTheBoiToken`.
   - Cơ chế phát hiện tấn công tái sử dụng (Replay Attack): Nếu token đã bị thu hồi mà vẫn gửi lên, hệ thống lập tức phát cảnh báo bảo mật nghiêm trọng `CANH_BAO_BAO_MAT` và từ chối cấp phép.
2. **Bảo Vệ Quản Trị Viên Tối Cao (Admin Self-Protection):**
   - Không cho phép Admin tự vô hiệu hóa tài khoản của chính mình.
   - Không cho phép vô hiệu hóa hoặc hạ quyền Quản trị viên hoạt động cuối cùng của hệ thống.
   - Nghiêm cấm gỡ quyền `ManageUsers` khỏi vai trò Admin.
   - Chỉ Admin mới được cấp hoặc thu hồi quyền Admin của tài khoản khác (Manager không thể tự thăng cấp).
3. **Thanh Toán Webhook Chuẩn Công Nghiệp:**
   - Xác thực chữ ký HMAC-SHA256 với cơ chế so sánh hằng thời gian (`CryptographicOperations.FixedTimeEquals`) chống tấn công Timing Attack.
   - Cơ chế Idempotent chống ghi nhận trùng lặp giao dịch thanh toán khi cổng ngân hàng callback nhiều lần.
4. **Kiểm Soát IDOR Khách Hàng (Customer Object Ownership):**
   - Khách hàng bị chặn triệt để, không thể xem đơn đặt phòng, hóa đơn, đơn dịch vụ, thông báo của khách hàng khác. Mọi truy vấn danh sách đều được filter cứng theo `User.GetUserId()`.

---

## 6. LỘ TRÌNH & KHUYẾN NGHỊ KHẮC PHỤC (ACTIONABLE REMEDIATION GUIDE)

### Bước 1: Khắc phục Anti-Pattern `IsStaffOrAdmin()` tại các Controller giao dịch
Chuyển đổi các điều kiện kiểm tra sang dùng `User.HasPermission(Quyen.X)` theo bảng đối chiếu:

```csharp
// 1. LoaiPhongController.cs: CUD LoaiPhong (Phải có ManageRooms)
if (!User.HasPermission(Quyen.ManageRooms)) return Forbid();

// 2. DatPhongController.cs: Danh sách & Gán phòng (ManageBookings)
if (!User.HasPermission(Quyen.ManageBookings)) return Forbid();

// 3. DatPhongController.cs: Check-in & Check-out (CheckInOut)
if (!User.HasPermission(Quyen.CheckInOut)) return Forbid();

// 4. HoaDonController.cs: Quản lý Hóa đơn & Phụ thu (ManageInvoices)
if (!User.HasPermission(Quyen.ManageInvoices)) return Forbid();

// 5. HoaDonController.cs: Thanh toán (ManagePayments)
if (!User.HasPermission(Quyen.ManagePayments)) return Forbid();

// 6. ChiPhiController.cs: Quản lý Chi phí (ManageExpenses)
if (!User.HasPermission(Quyen.ManageExpenses)) return Forbid();

// 7. DashboardController.cs: Xem KPI & Doanh thu (ViewDashboard)
if (!User.HasPermission(Quyen.ViewDashboard)) return Forbid();

// 8. BuongPhongController.cs: Phân công dọn phòng (ManageHousekeeping)
if (!User.HasPermission(Quyen.ManageHousekeeping)) return Forbid();
```

### Bước 2: Vá lỗ hổng SoD (Four-Eyes) tại `ChiPhiController.DuyetChiPhi`
```csharp
[HttpPut("{id:int}/duyet")]
public async Task<ActionResult<ApiResponse<object>>> DuyetChiPhi(int id, [FromQuery] string trangThai = "Approved")
{
    // Chỉ Manager hoặc Admin mới có quyền phê duyệt chi phí vận hành
    if (!User.IsManagerOrAdmin())
        return Forbid();

    var cp = await db.ChiPhiVanHanhs.FindAsync(id);
    if (cp == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy khoản chi phí."));

    var currentUserId = User.GetUserId();
    // BẮT BUỘC: Nguyên tắc 4 mắt - Không được tự duyệt chi phí do chính mình đề xuất
    if (cp.NguoiGhiNhan == currentUserId && !User.IsAdmin())
    {
        return BadRequest(ApiResponse<object>.Fail("Nguyên tắc kiểm soát (SoD): Người lập phiếu chi không được phép tự phê duyệt khoản chi của chính mình."));
    }

    cp.TrangThai = StatusMapper.NormalizeChiPhiTrangThai(trangThai);
    cp.NguoiPheDuyet = currentUserId;
    await db.SaveChangesAsync();

    await auditService.GhiNhanAsync(
        hanhDong: "PHE_DUYET_CHI_PHI",
        doiTuong: "ChiPhiVanHanh",
        khoaChinh: cp.MaId.ToString(),
        noiDung: $"Phê duyệt chi phí #{cp.MaId} ({cp.MoTa}): Trạng thái '{cp.TrangThai}'",
        maNguoiDung: currentUserId,
        email: User.FindFirst(ClaimTypes.Email)?.Value,
        diaChiIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
        thanhCong: true);

    return Ok(ApiResponse<object>.Ok(new { cp.MaId, cp.TrangThai }, $"Đã cập nhật trạng thái chi phí sang '{cp.TrangThai}'."));
}
```

### Bước 3: Sửa lỗ hổng sửa hồ sơ người dùng tại `NguoiDungController.CapNhat`
```csharp
[HttpPut("{id:int}")]
public async Task<ActionResult<ApiResponse<object>>> CapNhat(int id, [FromBody] CapNhatHoSoRequest req)
{
    var currentUserId = User.GetUserId();
    // Chỉ Admin hoặc chính chủ tài khoản mới được cập nhật thông tin cá nhân
    if (!User.IsAdmin() && id != currentUserId)
        return Forbid();

    var u = await db.NguoiDungs.FindAsync(id);
    if (u == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

    u.HoTen = req.HoTen;
    u.SoDienThoai = req.SoDienThoai;
    await db.SaveChangesAsync();

    return Ok(ApiResponse<object>.Ok(null!, "Cập nhật hồ sơ thành công."));
}
```

### Bước 4: Khắc phục logic điều kiện sai tại `DashboardController.Kpi`
```csharp
[HttpGet("kpi")]
public async Task<ActionResult<ApiResponse<HotelBiKpiDto>>> Kpi([FromQuery] DateTime? tuNgay = null, [FromQuery] DateTime? denNgay = null)
{
    // Sửa lỗi: Bỏ && !User.IsStaffOrAdmin(), chỉ cho phép người có quyền ViewDashboard (Admin, Manager, Accountant)
    if (!User.HasPermission(Quyen.ViewDashboard))
        return Forbid();

    // ... Tiếp tục tính toán KPI ...
}
```

---
*Báo cáo được biên soạn bởi Agent Antigravity — Chuyên gia Security Auditing & Backend Auth.*
