# DACN Hotel Management - Project Architecture & Memory

## 1. Tech Stack
- **Framework:** .NET 9.0 (ASP.NET Core Web API + Blazor Server)
- **Database:** Microsoft SQL Server (Local `Server=.;Database=DACN`)
- **ORM:** Entity Framework Core 9.0 (`DACNContext`)
- **Authentication:** JWT Bearer Token (HMAC-SHA256) + Sliding Refresh Token Rotation (14-day expiry)
- **Password Hashing:** BCrypt.Net-Next
- **Security & Headers:** Built-in RateLimiter (`auth-policy`: 10 req/min, `general-policy`: 100 req/min), Custom `SecurityHeadersMiddleware` (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy)
- **Audit System:** Dual-logging (`IAuditService` -> Structured `ILogger` + EF Core Entity `NhatKyHeThong`)

## 2. Solution Structure
- `src/DACN.Domain`: Domain abstractions.
- `src/DACN.Application`: Application contracts & interfaces.
- `src/DACN.Infrastructure`: EF Core `DACNContext`, DB Entities, Fluent API mapping, Indexes, Relationships.
- `src/DACN.Web`:
  - `Controllers/`: RESTful APIs (`/api/v1/*`)
    - `AuthController`: Đăng nhập, Đăng ký (`customer`), Làm mới Token (Refresh Token Rotation), Đăng xuất (Revoke Token), Quên mật khẩu (OTP Demo), Đặt lại mật khẩu, Đổi mật khẩu, Profile (`me`).
    - `DatPhongController`: Đặt phòng (Transaction Serializable), Hủy phòng, Gán phòng, Check-in, Check-out.
    - `PhongController`: Danh sách, Chi tiết, Cập nhật trạng thái, Hoàn tất vệ sinh buồng phòng.
    - `LoaiPhongController`: Tìm kiếm phòng trống theo thời gian & số người, CRUD loại phòng.
    - `DichVuController`: Danh mục dịch vụ, Đặt dịch vụ theo phòng (đồng bộ tự động vào Hóa đơn).
    - `HoaDonController`: Chi tiết hóa đơn, Xem theo đơn đặt phòng, Thanh toán hóa đơn.
    - `DashboardController`: Thống kê tổng quan phòng, doanh thu theo ngày, biểu đồ trạng thái, KPI khách sạn (`Occupancy %`, `ADR`, `RevPAR`, tỷ lệ hủy, doanh thu theo kênh/loại phòng).
    - `NguoiDungController`: Quản lý danh sách (lọc theo từ khóa/vai trò/trạng thái), Xem/Cập nhật hồ sơ, Khóa/Mở khóa (thu hồi RefreshToken & bảo vệ Admin cuối cùng), Đổi vai trò RBAC.
    - `VaiTroController`: Quản lý vai trò (`CRUD`) & cập nhật ma trận 18 quyền hạn (`QuyenHan`), ghi log diff quyền thêm/bớt.
    - `VoucherController`: Danh sách công khai (`cong-khai`), Kiểm tra mã giảm giá (rate-limit `public-strict`, áp trần `GiamToiDa`), CRUD voucher (`ManageVouchers`).
    - `NoiDungController`: CRUD Địa điểm tham quan (gắn nhãn `PhongCach` cho AI), Bài viết cẩm nang (tự động tạo slug ASCII không dấu `GioVN.TaoSlug`), Thông báo.
    - `AiController`: Chatbot Hybrid RAG (`POST /api/v1/ai/chat`, lịch sử phiên chat), Bộ máy chấm điểm gợi ý du lịch theo Ngân sách + Tháng/Mùa + Gu trải nghiệm (`POST/GET /api/v1/ai/goi-y`, `mua-du-lich`, `tour`), Dự báo nhu cầu 14 ngày (`du-bao-nhu-cau/chay`), Chấm điểm rủi ro hủy đơn (`du-doan-huy-phong/chay`, `do-chinh-xac`).
    - `DanhGiaController`: Đánh giá loại phòng, Duyệt/Xóa đánh giá (`ModerateReviews`).
    - `NhatKyController`: Xem nhật ký kiểm toán hệ thống (`GET /api/v1/nhat-ky-he-thong`).
    - `SystemController`: Health check kết nối Database & máy chủ (`GET /api/v1/system/health`).
  - `DTOs/`: Data Transfer Objects with Data Annotations validation.
  - `Middleware/`: `ExceptionMiddleware`, `SecurityHeadersMiddleware`.
  - `Services/`: `IAuditService`, `AuditService`, `GoiYDuLichService` (Scoring Engine + NLU Intent Extractor + Hybrid Gemini/Rule Chatbot + Forecasting & Churn Risk), `UserSessionService`.
  - `Common/`: `ClaimsPrincipalExtensions` (strict `HasPermission`), `Quyen` (18 hằng số quyền), `GioVN`, `SchemaMigrationRunner`, `StatusMapper`, `ApiResponse<T>`, `KetQuaPhanTrang<T>`.
- `tests/DACN.UnitTests`: 53 Unit tests (ClaimsPrincipalExtensions, ApiResponse, VoucherCalculation, AuditService).
- `tests/DACN.IntegrationTests`: 40 Integration tests (WebApplicationFactory + InMemory DB + LiveDb tests on real SQL Server).
- `tests/smoke_test.ps1`: Automated CLI Smoke tests for all public & protected API endpoints.

## 3. Key Design Decisions & Security Patterns
1. **Refresh Token Rotation (Sliding Session):**
   - Đăng nhập/Đăng ký cấp cả Access Token (JWT) và Refresh Token (64-byte CSPRNG token).
   - API `POST /api/v1/auth/lam-moi-token`: Đổi Refresh Token cũ lấy Access Token mới + Refresh Token mới. Token cũ bị thu hồi ngay lập tức (`DaThuHoi = true`).
   - Chống Replay Attack: Nếu một token đã bị thu hồi được gửi lên, hệ thống phát hiện và ghi audit cảnh báo bảo mật.
2. **Khôi phục Mật khẩu an toàn (OTP Flow):**
   - `POST /api/v1/auth/quen-mat-khau`: Phát sinh mã OTP 6 số ngẫu nhiên (hạn 15 phút).
   - `POST /api/v1/auth/dat-lai-mat-khau`: Xác nhận OTP, cập nhật mật khẩu băm BCrypt và tự động thu hồi toàn bộ Refresh Token cũ để bảo vệ tài khoản.
3. **Chuẩn hóa Kiểm toán Hệ thống (AuditService):**
   - Chuyển toàn bộ `NhatKyHeThong` thành First-Class Entity trong EF Core.
   - Loại bỏ hoàn toàn mã DDL `CREATE TABLE` động tại runtime, đảm bảo tương thích mọi môi trường (SQL Server, InMemory, Staging, CI/CD).
4. **Chống Overbooking (Race Condition):**
   - Áp dụng `IsolationLevel.Serializable` trong transaction tạo đặt phòng (`TaoDatPhong`).
   - Kiểm tra overlap khoảng thời gian phòng trống trước khi chốt đơn.
5. **Chống lạm dụng Voucher:**
   - Atomic SQL Update & Validate chặt chẽ giá trị đặt phòng tối thiểu.
6. **Quy trình Buồng phòng (Housekeeping Lifecycle):**
   - Check-out: Chuyển phòng sang `don_dep` và tự động phát sinh phiếu công việc tại `NhatKyBuongPhong`.
   - Hoàn tất vệ sinh: API `PUT /api/v1/phong/{id}/hoan-tat-ve-sinh` đóng phiếu và đưa phòng về `trong` (`sach_se`).
7. **Đồng bộ Tài chính & Hóa đơn tự động:**
   - Đặt phòng tạo ngay `HoaDon` ban đầu.
   - Đặt dịch vụ (`DatDichVu`) tự động cộng dồn chi phí vào `HoaDon.TongTienDichVu` và `HoaDon.TongThanhToan`.
8. **IDOR & Bảo mật Phân quyền:**
   - Lấy `userId` từ JWT claims (`User.GetUserId()`), chặn khách hàng truy cập đơn/hóa đơn của người khác.

## 4. Current Quality Status
- **Build:** `0 Warning(s), 0 Error(s)`.
- **Automated Tests:** 93/93 tests PASS (0 failures, 100% pass rate).
- **Database DACN (SQL Server Local):** Kết nối thực tế thành công, schema 36 bảng chuẩn xác, seed data đầy đủ.
- **Backend Status:** Hoàn tất, sẵn sàng làm nền tảng vững chắc để xây dựng Frontend.


## 5. Frontend Architecture & Design System (Blazor Web App)
- **Design System CSS:** `src/DACN.Web/wwwroot/css/design-system.css`
  - Bảng màu dịu mắt: Maritime Teal (`#1E5F74`), Deep Midnight Navy (`#0D2833`), Warm Sand (`#DDA15E`), App Background (`#F8FAFC`).
  - Semantic Status: Success (`#10B981`), Warning (`#F59E0B`), Danger (`#EF4444`), Info (`#3B82F6`), AI Purple (`#8B5CF6`).
  - Font & Icon: Plus Jakarta Sans + Bootstrap Icons v1.11.3.
- **Shared Components:**
  - `PageHeader.razor`: Tiêu đề chuẩn phân hệ kèm thẻ thành viên.
  - `StatCard.razor`: Thẻ thống kê chỉ số nghiệp vụ với các variant màu.
  - `StatusBadge.razor`: Tự động map trạng thái buồng phòng, đơn đặt phòng, thanh toán thành badge chuẩn.
- **Phân hệ 4 Thành viên (Đã xây khung đầy đủ):**
  - **TV1:** `ThanhVien1_BaoMat/RbacDashboard.razor` (`/admin/rbac`, `/admin/vouchers`, `/admin/audit-logs`) - Quản lý tài khoản, RBAC, kho Voucher, nội dung du lịch, Audit Logs.
  - **TV2:** `ThanhVien2_BuongPhong/HousekeepingDashboard.razor` (`/housekeeping/rooms`, `/housekeeping/tasks`, `/housekeeping/assets`) - Trạng thái buồng phòng, nhật ký dọn dẹp, kiểm kê tài sản & hư hỏng.
  - **TV3:** `ThanhVien3_TienSanh/FrontDeskDashboard.razor` (`/frontdesk/bookings`, `/frontdesk/checkin-out`, `/frontdesk/services-billing`) - Sơ đồ phòng trực quan, check-in/out, dịch vụ, hóa đơn, thanh toán.
  - **TV4:** `ThanhVien4_AICore/AiBiDashboard.razor` (`/ai-analytics/dashboard`, `/ai-analytics/chatbot`, `/ai-analytics/forecast`) - AI Chatbot tư vấn tour theo mùa, dự báo nhu cầu phòng, cảnh báo nguy cơ hủy phòng, báo cáo BI.
- **Tài liệu Hướng Dẫn Kỹ Thuật:** `DESIGN.md` đặt tại thư mục gốc dự án.
