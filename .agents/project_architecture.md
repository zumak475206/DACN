# DACN Hotel Management (BookingStay.AI) — Master Architecture & Project Memory

> **Tài liệu bàn giao & bộ nhớ dự án cốt lõi dành cho Developer và AI Models**  
> **Cập nhật:** 21/11/2026 | **Trạng thái:** Tích hợp hoàn tất 4 phân hệ (TV1 - TV4) + Chuỗi 6 Chi Nhánh + TV4 Grounded AI Travel Advisor + Tự động Tra GPS & Geocoding | **Tests:** 156/156 PASSED (104 Unit Tests + 52 Integration Tests)

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
| **Admin** | Quản Trị Viên Hệ Thống | `admin@hotel.com` | `/admin/dashboard` (Toàn cảnh 360° & RBAC) |
| **Manager** | Trần Quản Lý | `manager@hotel.com` | `/admin/dashboard` (Góc nhìn Giám Đốc, BI & Churn Alert) |
| **FrontDesk** | Lê Lễ Tân | `receptionist@hotel.com` | `/admin/dashboard` (Góc nhìn Tiền Sảnh: Khách Check-in/out, Room Rack) |
| **Housekeeping** | Nguyễn Buồng Phòng | `housekeeper@hotel.com` | `/admin/dashboard` (Góc nhìn Buồng Phòng: Phòng dọn, 1-Click Status) |
| **Accountant** | Hoàng Kế Toán | `accountant@hotel.com` | `/admin/dashboard` (Góc nhìn Kế Toán: Dòng tiền, Hóa đơn, Chi phí) |
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
3. **Vòng đời UserSessionService (Scoped vs Singleton):**  
   Trong Blazor Server, `UserSessionService` bắt buộc phải cấu hình `AddScoped` (theo từng Circuit SignalR). Tuyệt đối không để `AddSingleton` vì sẽ gây rò rỉ phiên đăng nhập (Người dùng A đăng nhập Admin thì Người dùng B vào web cũng bị nhận nhầm là Admin).
4. **Phân quyền chặt chẽ (Strict RBAC):**  
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


---

## 6. Kiến Trúc Chuỗi Khách Sạn Đa Chi Nhánh (Multi-Branch Hotel Chain Architecture)

### 6.1. Bối cảnh & Mục tiêu
- **Mục tiêu:** Đồng bộ hóa trải nghiệm cổng khách hàng (AI Recommender gợi ý du lịch toàn quốc Đà Nẵng, Phú Quốc, Đà Lạt...) với bộ máy quản trị PMS bên trong bằng mô hình **Chuỗi khách sạn BookingStay Collection**.
- **Cơ chế tương thích ngược (Zero Breaking Changes):** Mọi bảng dữ liệu liên quan (`Phong`, `LoaiPhong`, `DatPhong`, `NguoiDung`, `ChiPhiVanHanh`, `DuBaoNhuCauDatPhong_AI`) đều có cột `MaChiNhanh INT NOT NULL DEFAULT 1`. Dữ liệu cũ tự động thuộc Chi nhánh 1 (Đà Nẵng).

### 6.2. Danh mục Chi nhánh chính thức (Official Branches)
1. **Chi nhánh 1 (`CN-DANANG`):** `GrandStay Đà Nẵng Beach Resort` (120 Võ Nguyên Giáp, Sơn Trà, TP. Đà Nẵng - Trụ sở chính).
2. **Chi nhánh 2 (`CN-PHUQUOC`):** `GrandStay Sunset Villa Phú Quốc` (Bãi Trường, Dương Tơ, TP. Phú Quốc, Tỉnh Kiên Giang).
3. **Chi nhánh 3 (`CN-DALAT`):** `GrandStay Pine Hill Retreat Đà Lạt` (Đường Hoa Hồng, Hồ Tuyền Lâm, TP. Đà Lạt, Tỉnh Lâm Đồng).
4. **Chi nhánh 4 (`CN-VUNGTAU`):** `GrandStay Vũng Tàu Oceanfront Hotel` (165 Thùy Vân, Phường Thắng Tam, TP. Vũng Tàu, Tỉnh Bà Rịa - Vũng Tàu). Điểm đến gần TP.HCM (~105km, 2h di chuyển), mùa khô biển êm Tháng 11 – Tháng 4.
5. **Chi nhánh 5 (`CN-CUCHI`):** `CUCHI Hotel` (Du Coffee, Đường tỉnh lộ 7, An Nhơn Tây, Củ Chi, TP. Hồ Chí Minh). Ngoại thành TP.HCM (~45km, 1h di chuyển), kết nối Địa đạo Củ Chi (ID 111, cách 8.5km), tối ưu cho dã ngoại trong ngày hoặc cuối tuần.
6. **Chi nhánh 6 (`CN-HAIPHONG`):** `HẢI PHÒNG Hotel` (TP. Hải Phòng). Thành phố Cảng miền Bắc (~1.750km từ TP.HCM / 2h bay), kết nối vịnh Lan Hạ, đảo Cát Bà và Food Tour Đất Cảng.

### 6.3. Tab Quản Trị Chuỗi Chi Nhánh & Phòng Cho Admin (`/admin/branches`)
- **Vị trí UI:** Menu TV1 trên `NavMenu.razor` -> `/admin/branches` (`BranchManagement.razor`).
- **Tab 1 - Chi nhánh:** Tạo mới chi nhánh, Chỉnh sửa, Bật/Tắt hoạt động.
  - **Tự động Tra cứu GPS từ Địa chỉ (`AutoDetectGpsFromAddress` & `AutoFixBranchGpsAsync`):** Tích hợp nút `📍 Tự động tra GPS từ địa chỉ` trên Form tạo/sửa chi nhánh và nút `Tra GPS` trên từng thẻ chi nhánh hiện có. Người dùng chỉ cần gõ địa chỉ văn bản (ví dụ: *"Du Coffee, Đường tỉnh lộ 7, An Nhơn Tây, Củ Chi, Tp.Hồ Chí Minh"* hoặc *"tp. hải phòng"*), hệ thống tự động chuẩn hóa chữ tiếng Việt có dấu (xử lý triệt để ký tự `đ`/`Đ`), tra cứu bảng tọa độ 63 tỉnh thành & quận huyện trọng điểm (0ms offline), đồng thời có fallback tự động tới Open-Meteo Geocoding API để điền ngay lập tức vĩ độ, kinh độ và phân loại vùng miền (north/central/south) mà không cần người dùng tự tra cứu thủ công.
- **Tab 2 - Quản lý phòng:** Lọc theo chi nhánh, tìm kiếm số phòng, thêm phòng mới trực tiếp vào cơ sở, sửa phòng, xóa phòng an toàn. Giúp Admin mở rộng thêm chi nhánh ở bất kỳ tỉnh thành nào và quản trị phòng linh hoạt.

---

## 7. Kiến Trúc Bộ Máy TV4 AI Travel & Booking Recommendation Engine (Grounded Pipeline)

### 7.1. Nguyên Tắc Cốt Lõi: LLM Không Bịa Sự Thật (0% Entity Hallucination)
- **Code & CSDL là Nguồn Sự Thật:** Tên địa điểm, tọa độ GPS, khoảng cách km, dự báo thời tiết, tên khách sạn, hạng phòng và đơn giá đều được tính toán và dựng thẻ bởi C# Backend và SQL Server. LLM chỉ nhận payload top ứng viên để viết lời dẫn cảm xúc định tính.
- **No-Digits Claim Verifier:** Prompt cấm LLM sinh chữ số (`\d`). Code Backend dùng Regex `\d` kiểm tra; nếu LLM vi phạm tự động thay thế bằng lời dẫn an toàn, loại bỏ hoàn toàn hiện tượng "chém gió" số liệu.
- **Bảo vệ quyền riêng tư GPS:** Tọa độ GPS người dùng từ trình duyệt (`navigator.geolocation`) được làm tròn 2 chữ số thập phân (~1.1km) trước khi truyền về API và ghi log.
- **Dự báo thời tiết thực tế (`WeatherService`):** Tích hợp Open-Meteo 16-day forecast với `IMemoryCache` (2 giờ), tự động fallback sang mô hình khí hậu theo mùa khi tra cứu ngoài khoảng 16 ngày. Điểm đến trong nhà (`NgoaiTroi = false`) được ưu tiên khi trời mưa to.
- **Dịch vụ Địa lý & Khoảng cách (`GeoService`):** Tọa độ 63 tỉnh thành Việt Nam, công thức Haversine với hệ số đường bộ thực tế (1.30–1.38), ma trận chi phí/thời gian vé máy bay & tàu cao tốc ra biển đảo.
- **Bộ Test Đánh Giá Offline:** 6 bài test đánh giá không phụ thuộc mạng (`AiRecommendationEvalTests.cs`) kiểm thử bán kính, tránh mưa, biển đảo, ngưỡng 16 ngày, No-Digits verifier và làm tròn GPS.

### 6.3. Nguyên tắc Vận hành & Bảo mật Đa Chi Nhánh
- **Nhân viên cơ sở (Lễ tân, Buồng phòng, Kế toán):** Hệ thống tự động khóa cứng (Branch-Locked) theo `CurrentUser.MaChiNhanh`. Nhân viên không thể xem hay sửa phòng, khách, hóa đơn của chi nhánh khác.
- **Lãnh đạo (Admin, Manager):** Được trang bị **Thanh chọn chi nhánh (Branch Selector)** trên `AdminDashboard.razor` để chuyển đổi linh hoạt giữa: Toàn Chuỗi Hệ Thống, GrandStay Đà Nẵng, GrandStay Phú Quốc, GrandStay Đà Lạt.
- **AI Gợi ý Du lịch (`GoiYDuLichService.cs`):** Tự động nhận diện địa phương của điểm đến (`XacDinhChiNhanhTuDiemDen`) để đề xuất phòng và giá thuộc đúng chi nhánh đó.
- **Khách hàng đặt phòng (`Home.razor`):** Đơn đặt phòng `DatPhong` ghi nhận chính xác `MaChiNhanh` của phòng được chọn, xuất hiện ngay lập tức trên dashboard điều phối của chi nhánh tương ứng.

### 6.4. Độc Lập Dữ Liệu Trên Các Phân Hệ Vận Hành (Staff Operational Isolation)
- **Check-in / Check-out (`CheckInOut.razor`):** Lọc theo chi nhánh của nhân viên. Khi xếp phòng check-in, tự động bảo đảm `p.MaChiNhanh == booking.MaChiNhanh` để không xếp nhầm phòng ở chi nhánh khác.
- **Sơ đồ phòng & Walk-in (`RoomRackBookings.razor`):** Tách biệt sơ đồ phòng và đơn đặt theo chi nhánh. Khi tạo đơn walk-in tại quầy, tự động gán `booking.MaChiNhanh = room.MaChiNhanh`.
- **Nhật ký buồng phòng (`CleaningTasks.razor`):** Phân công và tạo nhiệm vụ dọn dẹp chỉ cho các phòng thuộc chi nhánh hiện tại.
- **Dịch vụ & Hóa đơn (`BillingServices.razor`):** Tách biệt danh sách hóa đơn theo chi nhánh. Hỗ trợ gọi dịch vụ phòng trực tiếp (minibar, F&B, spa) gắn vào chi tiết phòng lưu trú và tự động cộng dồn vào hóa đơn thanh toán.
- **Quản lý chi phí vận hành (`ExpenseManagement.razor`):** Tách biệt phiếu chi theo chi nhánh, ghi nhận chính xác `MaChiNhanh` và kiểm soát chặt chẽ nguyên tắc SoD (Separation of Duties).
- **Danh mục phòng khách hàng (`RoomsList.razor`):** Cung cấp bộ lọc điểm đến (Toàn chuỗi, Đà Nẵng, Phú Quốc, Đà Lạt) và nhãn chi nhánh rõ ràng trên từng thẻ phòng.
- **Tuân thủ SQL CHECK Constraints:** Chuẩn hóa toàn bộ trạng thái ghi CSDL (`ChiTietDatPhong`: Pending, Active, CheckedOut, Cancelled; `ThanhToan`: Cash, VNPay, MoMo, BankTransfer) qua `StatusMapper.cs`, loại trừ hoàn toàn lỗi vi phạm ràng buộc CSDL.


### Cập Nhật Kiểm Tra & Khắc Phục Toàn Diện (Session Audit & Validation)
- **Giải quyết lỗi CHECK Constraint `CHK_Booking_Details_status`:**
  - *Nguyên nhân:* Khi khách đặt cọc 30% trực tuyến tại `Home.razor`, đơn cha có trạng thái `DatPhong.TrangThai = "Confirmed"`. Code trước đó đã gán nhầm giá trị này sang dòng chi tiết con `ChiTietDatPhong.TrangThai = "Confirmed"`, vi phạm ràng buộc SQL Server `CHK_Booking_Details_status` (chỉ chấp nhận: `Pending`, `Active`, `CheckedOut`, `Cancelled`).
  - *Khắc phục:* `ChiTietDatPhong.TrangThai` được chuẩn hóa về `"Pending"` khi tạo mới đơn từ Web. Trạng thái chỉ chuyển sang `"Active"` khi Lễ tân hoàn tất Check-in tại quầy, và `"CheckedOut"` khi trả phòng.
- **Route Alias & Điều hướng Khách vãng lai:**
  - Hỗ trợ đầy đủ các định dạng URL: `/my-bookings`, `/mybookings`, `/my-booking`, `/tra-cuu-don`.
  - Khắc phục lỗi 404 khi gõ nhầm URL hoặc dư ký tự kết thúc; tích hợp `ProtectedLocalStorage` tự lưu mã đơn và số điện thoại vừa đặt để tự động điền vào khung tra cứu.
- **Phân định Voucher theo Chi nhánh (Voucher Multi-Branch Scoping):**
  - Mở rộng `VoucherManagement.razor`: Cho phép chọn phát hành voucher cho toàn chuỗi (`MaChiNhanh = null`) hoặc gán cố định cho từng chi nhánh cụ thể (`MaChiNhanh = 1, 2, 3`).
  - `Home.razor` & `DatPhongController.cs`: Ràng buộc kiểm tra chéo chi nhánh khi áp dụng voucher, ngăn chặn khách dùng mã của chi nhánh này đặt phòng ở chi nhánh khác.
- **Quan hệ CSDL EF Core Phục hồi cao (Resilient Foreign Keys):**
  - Cấu hình `.IsRequired(false)` cho các liên kết Navigation `MaChiNhanhNavigation` trong `DACNContext.cs` (`DatPhong`, `Phong`, `LoaiPhong`, `ChiPhiVanHanh`, `DuBaoAI`), giúp EF Core sinh câu lệnh `LEFT OUTER JOIN` thay vì `INNER JOIN`, đảm bảo không bao giờ bị loại bỏ bản ghi khi truy vấn dữ liệu chi nhánh.
- **Kiểm thử tự động:**
  - 88/88 Unit Tests (`DACN.UnitTests`) đạt 100% Pass.
  - Biên dịch Release 0 Error, 0 Warning.

- **Nâng cấp AI Gợi Ý Địa Điểm & Đặt Phòng Đa Chi Nhánh (`GoiYDuLichService.cs`):**
  - *Bổ sung Tri thức CSDL (`MuaDuLich`, `DiaDiemThamQuan`, `GoiYDiaDiemTheoMua_AI`, `GoiYTour_AI`):* Khắc phục lỗi mô tả thời tiết Hội An bị gán nhầm `"Se lạnh, săn mây"` (`MaID = 4`). Bổ sung đầy đủ dữ liệu điểm đến, mùa khô săn mây Tháng 10 – Tháng 3 cho **Chi nhánh Đà Lạt (`GrandStay Pine Hill Retreat Đà Lạt`)** và mùa khô biển đảo Tháng 11 – Tháng 4 cho **Chi nhánh Phú Quốc (`GrandStay Sunset Villa Phú Quốc`)**.
  - *Bộ trích xuất NLU & Khoảng cách địa lý (`TrichXuatYChinh`):* Nhận diện điểm xuất phát (`DiemXuatPhat = "hcm"` khi khách nhắc TP.HCM / Sài Gòn), yêu cầu khoảng cách gần (`YeuCauGanXuatPhat`), và điểm đến cụ thể (`DiemDenCuThe`).
  - *Thuật toán Chấm điểm Đa chiều (Season + Budget Feasibility + Geo Distance + Vibe):*
    - Loại bỏ hoàn toàn các điểm đến miền Trung/Bắc cách >900km khi khách yêu cầu đi gần TP.HCM.
    - Phạt nặng và lọc bỏ các điểm đến trái mùa (ví dụ: không gợi ý Hội An mùa đẹp Tháng 2–4 khi khách hỏi đi Tháng 12).
    - Tính toán chi phí trọn gói khả thi bao gồm cả chi phí di chuyển thực tế từ điểm xuất phát + lưu trú tại chi nhánh tương ứng (VD: Từ TP.HCM đi Đà Lạt ~300km bằng xe Limousine khứ hồi 650k + phòng `Pine Hill Retreat Superior` 750k/đêm = trọn gói `2.100.000đ – 2.850.000đ`, khớp hoàn hảo với ngân sách 3 triệu).
  - *Chuyển đổi Đặt phòng trực tiếp (Direct Booking Conversion):* Tích hợp thẻ gợi ý đặt phòng kèm nút `"Đặt Phòng Ngay"` dẫn tới `/rooms?branch={MaChiNhanh}` ngay trong khung Chat nổi (`CustomerLayout.razor`), trang quản trị AI (`AiChatbot.razor`) và Trang chủ (`Home.razor`).
