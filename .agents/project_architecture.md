# DACN Hotel Management (BookingStay.AI) — Master Architecture & Project Memory

> **Tài liệu bàn giao & bộ nhớ dự án cốt lõi dành cho Developer và AI Models**  
> **Cập nhật:** 20/11/2026 | **Trạng thái:** Tích hợp hoàn tất 4 phân hệ (TV1 - TV4) | **Tests:** 104/104 PASSED

---

## 1. Tổng Quan Dự Án & Tech Stack
* **Tên giải pháp:** Hệ thống Đặt phòng Khách sạn & Trợ lý Du lịch Thông minh BookingStay.AI.
* **Tech Stack:**
  * **Framework:** .NET 9.0 (ASP.NET Core Web API + Blazor Server Interactive Server).
  * **Cơ sở dữ liệu:** Microsoft SQL Server (Local `Server=.;Database=DACN;Integrated Security=True;TrustServerCertificate=True`).
  * **ORM:** Entity Framework Core 9.0 (`DACNContext`, Fluent API, 36 bảng dữ liệu).
  * **Authentication:** JWT Bearer Token (HMAC-SHA256) + Sliding Refresh Token Rotation (14 ngày).
  * **Password Hashing:** BCrypt.Net-Next (Work Factor: 11).
  * **Trí tuệ nhân tạo (AI):** Bộ máy Hybrid NLU + Heuristic Scoring + Google Gemini API Fallback + AI Forecast/Churn Risk Scorer.
  * **Security & Hardening:**
    * ASP.NET Core RateLimiter (`auth-policy`: 10 req/min, `general-policy`: 100 req/min, `public-strict`: 20 req/min).
    * `SecurityHeadersMiddleware`: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy.
    * `IAuditService`: Ghi log kép (Dual Logging: Console ILogger + Bảng CSDL `NhatKyHeThong`).
    * Session Thread Safety: Bọc các sự kiện `Session.OnChange` qua `InvokeAsync(StateHasChanged)` bảo đảm an toàn luồng trong Blazor Server.

---

## 2. Cấu Trúc Solution & Các Tầng Ứng Dụng (Clean Architecture)

```
DACN_HotelManagement/
├── src/
│   ├── DACN.Domain/                     # Entities & Domain Interfaces thuần túy
│   ├── DACN.Application/                # DTOs, Business Contracts, Validation Rules
│   ├── DACN.Infrastructure/             # EF Core DACNContext, 34 Models, Fluent API Mappings
│   └── DACN.Web/                        # ASP.NET Core API + Blazor Server Components
│       ├── Common/                      # Quyen (18 quyền), GioVN, SchemaMigrationRunner, ClaimsPrincipalExtensions
│       ├── Components/
│       │   ├── App.razor                # Root component với @rendermode InteractiveServer
│       │   ├── Routes.razor             # Bộ định tuyến Blazor
│       │   ├── Layout/                  # CustomerLayout, MainLayout, NavMenu (Sidebar 4 phân hệ)
│       │   ├── Shared/                  # PageHeader, StatCard, StatusBadge
│       │   └── Pages/
│       │       ├── Home.razor           # Giao diện đặt phòng & AI gợi ý du lịch khách hàng
│       │       ├── Login.razor          # Đăng nhập & Đăng ký tài khoản (Auto BCrypt & Session)
│       │       ├── AdminDashboard.razor # Trung tâm điều hành ca trực khách sạn
│       │       ├── Customer/            # RoomsList.razor, MyBookings.razor
│       │       ├── ThanhVien1_BaoMat/   # TV1: AccountManagement, RbacMatrix, VoucherManagement, ContentManagement, AuditLogs
│       │       ├── ThanhVien2_BuongPhong/# TV2: RoomRackBookings, CheckInOut, RoomStatus, CleaningTasks, AssetInventory
│       │       ├── ThanhVien3_TienSanh/ # TV3: BillingServices, ExpenseManagement
│       │       └── ThanhVien4_AICore/   # TV4: BiReports, AiChatbot, AiForecastRisk
│       ├── Controllers/                 # 17 RESTful Controllers (/api/v1/*)
│       ├── DTOs/                        # DTOs cho Auth, Đặt phòng, Voucher, AI, Dashboard, v.v.
│       ├── Middleware/                  # ExceptionMiddleware, SecurityHeadersMiddleware
│       ├── Services/                    # AuditService, GoiYDuLichService, UserSessionService (Scoped)
│       └── wwwroot/css/                 # design-system.css (Maritime Teal & Midnight Navy)
├── tests/
│   ├── DACN.UnitTests/                  # 58 Unit Tests (RBAC, Voucher math, API Response, Audit)
│   └── DACN.IntegrationTests/           # 46 Integration Tests (Tv1Tv4ApiTests, RealDbSmokeTests)
├── database/
│   ├── DACN_Full.sql                    # Script CSDL đầy đủ (Schema + Seed Data)
│   └── migrations/                      # Các script nâng cấp (001_tv1_tv4, 002_nhatky, 008_encoding)
└── .agents/
    ├── project_architecture.md          # Master Memory của toàn bộ dự án (File này)
    └── domain_analysis.md               # Báo cáo phân tích nghiệp vụ 7 bước chuyên sâu
```

---

## 3. Phân Hệ Chi Tiết Của 4 Thành Viên Nhóm

### 🛡️ THÀNH VIÊN 1: BẢO MẬT, TÀI KHOẢN, PHÂN QUYỀN RBAC, VOUCHER, NỘI DUNG & AUDIT
* **Trách nhiệm:** An ninh hệ thống, quản lý định danh người dùng, ma trận quyền hạn, chiến dịch khuyến mãi và cẩm nang du lịch.
* **Màn hình Blazor:**
  1. `AccountManagement.razor` (`/admin/accounts`): Xem danh sách, tìm kiếm, lọc theo vai trò, khóa/mở khóa tài khoản (thu hồi refresh token ngay lập tức), bảo vệ chống khóa Admin cuối cùng.
  2. `RbacMatrix.razor` (`/admin/rbac`): Ma trận phân quyền động 18 quyền hạn trên 6 vai trò, lưu log diff khi cấp/hủy quyền.
  3. `VoucherManagement.razor` (`/admin/vouchers`): Tạo và cấu hình mã giảm giá (áp trần giảm giá, đơn hàng tối thiểu, lượt dùng tối đa).
  4. `ContentManagement.razor` (`/admin/content`): Quản lý địa điểm tham quan gắn nhãn `PhongCach` (Sea, Mountain, Healing...) và bài viết cẩm nang du lịch tự sinh slug tiếng Việt không dấu.
  5. `AuditLogs.razor` (`/admin/audit-logs`): Tra cứu nhật ký kiểm toán hệ thống bất biến theo thời gian, IP và tác vụ.
* **APIs:** `NguoiDungController`, `VaiTroController`, `VoucherController`, `NoiDungController`, `NhatKyController`.

---

### 🛏️ THÀNH VIÊN 2: BUỒNG PHÒNG, VẬN HÀNH & SƠ ĐỒ TIỀN SẢNH
* **Trách nhiệm:** Vận hành sơ đồ phòng trực quan, thủ tục check-in/out, phân công dọn dẹp buồng phòng và kiểm kê tài sản.
* **Màn hình Blazor:**
  1. `RoomRackBookings.razor` (`/frontdesk/bookings`): Sơ đồ buồng phòng trực quan phân theo tầng, màu sắc trạng thái (Trống, Đang ở, Cần dọn, Bảo trì), tiếp nhận khách vãng lai (Walk-in).
  2. `CheckInOut.razor` (`/frontdesk/checkin-out`): Danh sách chờ nhận phòng (Check-in) và chờ trả phòng (Check-out) với tổng hợp hóa đơn.
  3. `RoomStatus.razor` (`/housekeeping/rooms`): Quản trị trạng thái buồng phòng cho khối Housekeeping.
  4. `CleaningTasks.razor` (`/housekeeping/tasks`): Điều phối, phân công nhân viên vệ sinh buồng và ghi nhận thời gian hoàn thành.
  5. `AssetInventory.razor` (`/housekeeping/assets`): Kiểm kê tiện nghi trong phòng (TV, Điều hòa, Két sắt, Minibar) và ghi nhận hư hỏng/mất mát đền bù.
* **APIs:** `PhongController`, `BuongPhongController`, `DatPhongController`.

---

### 💰 THÀNH VIÊN 3: TIỀN SẢNH, THU NGÂN, HÓA ĐƠN & CHI PHÍ VẬN HÀNH
* **Trách nhiệm:** Quản lý dịch vụ phòng, quyết toán hóa đơn thu ngân và theo dõi chi phí vận hành khách sạn.
* **Màn hình Blazor:**
  1. `BillingServices.razor` (`/frontdesk/services-billing`): Ghi nhận dịch vụ phát sinh (gọi món, minibar, giặt ủi), tự động cộng dồn hóa đơn theo thời gian thực, thanh toán thu ngân (Tiền mặt, Chuyển khoản).
  2. `ExpenseManagement.razor` (`/frontdesk/expenses`): Ghi nhận các khoản chi phí vận hành (vật tư, điện nước, sửa chữa), phê duyệt và tổng hợp dòng tiền chi.
* **APIs:** `HoaDonController`, `DichVuController`, `ChiPhiController`.

---

### 🤖 THÀNH VIÊN 4: TRÍ TUỆ NHÂN TẠO (AI) & BÁO CÁO PHÂN TÍCH DOANH NGHIỆP (BI)
* **Trách nhiệm:** Trợ lý ảo AI tư vấn du lịch, thuật toán gợi ý điểm đến theo mùa & ngân sách, dự báo nhu cầu phòng và báo cáo KPI khách sạn.
* **Màn hình Blazor:**
  1. `Home.razor` (Cổng khách hàng): Bộ máy AI Recommender chấm điểm gợi ý điểm đến dựa trên Mùa du lịch + Ngân sách + Gu trải nghiệm (Gia đình, Cặp đôi, Sống ảo, Chữa lành).
  2. `AiChatbot.razor` (`/ai-analytics/chatbot`): Chatbot tư vấn du lịch và khách sạn với cơ chế Hybrid RAG (truy vấn CSDL phòng/tiện nghi/địa điểm trước, gọi Google Gemini API khi cần mở rộng kiến thức bên ngoài).
  3. `AiForecastRisk.razor` (`/ai-analytics/forecast`): Mô hình AI dự báo nhu cầu phòng 14 ngày tới (theo mùa & sự kiện) và thuật toán phát hiện nguy cơ khách hủy phòng (Churn Risk Scorer).
  4. `BiReports.razor` (`/ai-analytics/dashboard`): Báo cáo chỉ số KPI khách sạn chuẩn quốc tế: Tỷ lệ lấp đầy (`Occupancy Rate %`), Giá phòng trung bình ngày (`ADR`), Doanh thu trên mỗi phòng sẵn có (`RevPAR`), cơ cấu doanh thu theo loại phòng.
* **APIs:** `AiController`, `DashboardController`.

---

## 4. Tài Khoản Kiểm Thử Mặc Định (Seed Accounts)

Mật khẩu dùng chung cho toàn bộ tài khoản hệ thống: **`Admin@123`** *(hoặc dự phòng `123456` trong môi trường Development)*.

| Vai trò (Role) | Họ và Tên | Email Đăng Nhập | Đường Dẫn Mặc Định Sau Khi Đăng Nhập |
| :--- | :--- | :--- | :--- |
| **Admin** | Quản Trị Viên Hệ Thống | `admin@hotel.com` | `/admin/dashboard` |
| **Manager** | Trần Quản Lý | `manager@hotel.com` | `/ai-analytics/dashboard` |
| **FrontDesk** | Lê Lễ Tân | `receptionist@hotel.com` | `/frontdesk/bookings` |
| **Housekeeping** | Nguyễn Buồng Phòng | `housekeeper@hotel.com` | `/housekeeping/rooms` |
| **Accountant** | Hoàng Kế Toán | `accountant@hotel.com` | `/frontdesk/services-billing` |
| **Customer** | Nguyễn Văn An | `customer1@gmail.com` | `/` (Trang chủ khách hàng) |

---

## 5. Quy Chuẩn Lập Trình & Các Lưu Ý Sống Còn Cho AI Models

1. **Bảo toàn Encoding Tiếng Việt:**  
   Tất cả các file `.razor`, `.cs`, `.sql` khi đọc và ghi **bắt buộc phải giữ định dạng UTF-8 (ưu tiên UTF-8 with BOM)**. Tuyệt đối không dùng các script PowerShell `Set-Content` mặc định ANSI làm hỏng dấu tiếng Việt thành các ký tự `?`.
2. **Luồng giao diện Blazor Server (Dispatcher Safety):**  
   Không được gọi trực tiếp `StateHasChanged()` từ luồng ngầm sau các tác vụ `await Db...`. Mọi subscription sự kiện từ `UserSessionService.OnChange` bắt buộc phải bọc qua:
   ```csharp
   private void OnSessionChanged() => _ = InvokeAsync(StateHasChanged);
   ```
3. **Phân quyền chặt chẽ (Strict RBAC):**  
   Không cấp quyền `ManageUsers` (ID 11) cho `Manager`. Chỉ có tài khoản có vai trò `Admin` mới được phép thao tác phân quyền RBAC hoặc thay đổi vai trò tài khoản khác.
4. **Quy trình Git & Merge:**  
   Khi tích hợp code từ các nhánh thành viên khác (`noaye`, `feature/*`), luôn fetch, checkout nhánh tạm review, chạy `dotnet build` và `dotnet test` trước khi merge vào `main` với tùy chọn `--no-ff`.
5. **Chạy kiểm thử trước khi kết thúc task:**  
   Trước khi hoàn thành bất kỳ lượt tương tác nào về code:
   ```bash
   dotnet build
   dotnet test
   ```
   Đảm bảo luôn đạt **104/104 PASSED** và **0 Errors**.
