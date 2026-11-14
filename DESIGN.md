# GRAND HOTEL — DESIGN SYSTEM & FRONTEND SPECIFICATION
> **Phiên bản:** 1.0 (Q1/2026)  
> **Tech Stack:** ASP.NET Core 9.0 + Blazor Web App (InteractiveServer) + Bootstrap 5.3 + Bootstrap Icons v1.11.3 + SQL Server  
> **Mục tiêu:** Chuẩn hóa toàn bộ giao diện và luồng nghiệp vụ cho 4 thành viên đồ án, đảm bảo phong cách nhất quán, màu sắc thân thiện - chống mỏi mắt cho ca trực dài, và không bị lệch layout khi ghép code.

---

## 1. Triết Lý Thiết Kế & Bảng Màu (Color Tokens)

Hệ thống được thiết kế theo bảng màu **Maritime Teal & Warm Sand** lấy cảm hứng từ biển Đà Nẵng và bãi cát vàng, kết hợp tông nền dịu nhẹ (Slate-tinted Cream) giúp nhân viên lễ tân, buồng phòng và quản lý làm việc liên tục 8-12 tiếng không bị căng thẳng thị giác.

### 1.1. Color Tokens (CSS Variables)

Các biến CSS này đã được khai báo tập trung tại [`src/DACN.Web/wwwroot/css/design-system.css`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/wwwroot/css/design-system.css):

```css
:root {
    /* Màu thương hiệu & Chủ đạo */
    --hotel-primary: #1E5F74;        /* Maritime Teal - Xanh ngọc biển sâu trang nhã */
    --hotel-primary-hover: #164758;  /* Teal trầm khi hover */
    --hotel-primary-dark: #0D2833;   /* Midnight Navy dùng cho thanh điều hướng Sidebar */
    --hotel-accent: #DDA15E;         /* Warm Sand - Vàng cát hoàng hôn cao cấp */
    --hotel-accent-hover: #C58A47;   /* Sand đậm */
    
    /* Màu nền ứng dụng (Anti-glare / Chống lóa mắt) */
    --hotel-bg-app: #F8FAFC;         /* Nền tổng thể mềm mại (tránh dùng #FFFFFF tuyệt đối) */
    --hotel-surface: #FFFFFF;        /* Nền thẻ Card / Bảng biểu */
    --hotel-border: #E2E8F0;         /* Viền mỏng phân cách thanh thoát */
    
    /* Màu chữ (Typography) */
    --hotel-text: #1E293B;           /* Chữ chính (Slate 800) tương phản cao */
    --hotel-text-secondary: #475569; /* Chữ phụ (Slate 600) */
    --hotel-text-muted: #94A3B8;     /* Chữ làm mờ / ngày giờ (Slate 400) */
    
    /* Trạng thái nghiệp vụ (Semantic Tokens) */
    --hotel-success: #10B981;        /* Xanh lục Emerald - Đã dọn, Đã thanh toán, Hoạt động */
    --hotel-warning: #F59E0B;        /* Vàng hổ phách - Đang dọn, Chờ thanh toán, Đặt cọc */
    --hotel-danger: #EF4444;         /* Đỏ san hô - Phòng hỏng, Hủy phòng, Đã khóa */
    --hotel-info: #3B82F6;           /* Xanh dương - Đang ở, Thông tin chung */
    --hotel-ai: #8B5CF6;             /* Tím thạch anh - AI Chatbot & Dự báo BI */
}
```

### 1.2. Thẻ Đánh Dấu Phân Hệ (Member Badges)

Để phân biệt rõ ràng màn hình và chức năng do từng thành viên phụ trách:

| Thành Viên | Phân Hệ Phụ Trách | Class Tag | Màu Đại Diện |
| :--- | :--- | :--- | :--- |
| **Thành viên 1** | Bảo Mật, RBAC & Trải Nghiệm Khách Hàng | `.member-tag-tv1` | `#0EA5E9` (Sky Blue) |
| **Thành viên 2** | Buồng Phòng, Tiện Nghi & Vận Hành (Housekeeping) | `.member-tag-tv2` | `#10B981` (Emerald Green) |
| **Thành viên 3** | Tiền Sảnh (Front-Desk), Đặt Phòng & Thanh Toán | `.member-tag-tv3` | `#F59E0B` (Warm Amber) |
| **Thành viên 4** | Trí Tuệ Nhân Tạo (AI Core), Chatbot & Báo Cáo BI | `.member-tag-tv4` | `#8B5CF6` (AI Purple) |

---

## 2. Cấu Trúc Khung Layout (App Shell)

Toàn bộ ứng dụng sử dụng khung bố cục 2 cột cố định:

```
+------------------------------------------------------------------------------------+
|  [SIDEBAR] 260px (Cố định)        |  [TOPBAR] Chiều cao 64px                       |
|  - Logo Grand Hotel               |  - Trạng thái ca trực / Live API Status        |
|  - Trung Tâm Điều Hành            |  - Swagger link / Avatar nhân viên             |
|  - Menu TV3 (Tiền Sảnh)           +------------------------------------------------+
|  - Menu TV2 (Buồng Phòng)         |  [MAIN CANVAS] .hotel-page-body                |
|  - Menu TV4 (AI & BI)             |  - <PageHeader />                              |
|  - Menu TV1 (Bảo Mật & RBAC)      |  - <StatCard /> metrics hàng ngang             |
|  - Footer thông tin người đăng nhập |  - Thẻ Card nội dung / Bảng dữ liệu / Tabs     |
+------------------------------------------------------------------------------------+
```

- **File Master Layout:** [`src/DACN.Web/Components/Layout/MainLayout.razor`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Layout/MainLayout.razor)
- **File Sidebar Menu:** [`src/DACN.Web/Components/Layout/NavMenu.razor`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Layout/NavMenu.razor)

---

## 3. Thư Viện Thành Phần Dùng Chung (Shared Components)

Khi viết màn hình mới, **BẮT BUỘC** tái sử dụng các component có sẵn sau đây, **KHÔNG ĐƯỢC** tự tạo khung riêng gây lệch giao diện:

### 3.1. `<PageHeader />`
Đặt ở đầu mọi trang chức năng.

```razor
<PageHeader Title="Tiêu đề phân hệ"
            Subtitle="Mô tả ngắn gọn chức năng của trang"
            Icon="bi bi-door-open-fill"
            MemberTag="THÀNH VIÊN 2: BUỒNG PHÒNG"
            MemberTagClass="member-tag-tv2">
    <Actions>
        <button class="hotel-btn hotel-btn-primary">
            <i class="bi bi-plus-lg"></i> Thao Tác Chính
        </button>
        <button class="hotel-btn hotel-btn-outline">
            <i class="bi bi-download"></i> Xuất File
        </button>
    </Actions>
</PageHeader>
```

### 3.2. `<StatCard />`
Dùng hiển thị chỉ số tổng quan ở hàng đầu mỗi trang.

```razor
<div class="row g-3 mb-4">
    <div class="col-6 col-md-3">
        <StatCard Title="Chỉ Số A" Value="42" Description="Ghi chú thêm" Icon="bi bi-door-closed" Variant="primary" />
    </div>
    <div class="col-6 col-md-3">
        <StatCard Title="Chỉ Số B" Value="18" Description="Tăng 12%" Icon="bi bi-check-circle" Variant="success" />
    </div>
    <div class="col-6 col-md-3">
        <StatCard Title="Chỉ Số C" Value="5" Description="Cần xử lý ngay" Icon="bi bi-exclamation-triangle" Variant="warning" />
    </div>
    <div class="col-6 col-md-3">
        <StatCard Title="Chỉ Số D" Value="94.2%" Description="Độ chính xác AI" Icon="bi bi-cpu" Variant="ai" />
    </div>
</div>
```
*Các Variant hỗ trợ: `primary`, `accent`, `success`, `warning`, `danger`, `info`, `ai`.*

### 3.3. `<StatusBadge />`
Tự động chuyển đổi chuỗi trạng thái thành huy hiệu màu chuẩn:

```razor
<StatusBadge Status="@room.TrangThai" />
<StatusBadge Status="@booking.TrangThai" />
```
*Hỗ trợ tự động: `trong`, `dang_o`, `don_dep`, `chua_don`, `sua_chua`, `da_xac_nhan`, `da_nhan_phong`, `da_tra_phong`, `da_huy`, `da_thanh_toan`.*

---

## 4. Quy Chuẩn CSS Classes & HTML Elements

| Phần Tử | Class Chuẩn | Lưu Ý |
| :--- | :--- | :--- |
| **Thẻ chứa nội dung** | `.hotel-card` | Thẻ bo góc 12px viền nhạt nền trắng |
| **Tiêu đề thẻ** | `.hotel-card-header`, `.hotel-card-title` | Gồm icon + chữ tiêu đề |
| **Nút bấm chính (Teal)** | `.hotel-btn.hotel-btn-primary` | Nút hành động chính trang |
| **Nút nổi bật (Sand)** | `.hotel-btn.hotel-btn-accent` | Dùng cho Tạo mới, Thanh toán |
| **Nút viền phụ** | `.hotel-btn.hotel-btn-outline` | Dùng cho Lọc, Xuất dữ liệu |
| **Nút AI (Tím)** | `.hotel-btn.hotel-btn-ai` | Dành riêng cho tác vụ AI / Phân tích |
| **Bảng dữ liệu** | `table.hotel-table` | Hàng hover nhẹ, header xám nhạt, canh lề chuẩn |
| **Ô nhập liệu Form** | `input.hotel-input`, `select.hotel-select` | Bo góc 8px, viền đổi màu teal khi focus |

---

## 5. Bảng Phân Công Nhiệm Vụ 4 Thành Viên

### 🔹 THÀNH VIÊN 1: BẢO MẬT, PHÂN QUYỀN (RBAC) & TRẢI NGHIỆM KHÁCH HÀNG
- **Đường dẫn trang mẫu:** [`/admin/rbac`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/ThanhVien1_BaoMat/RbacDashboard.razor)
- **Các chức năng cốt lõi theo slide:**
  1. *Xác thực & Tài khoản:* Đăng ký, đăng nhập bảo mật (2FA / JWT Token Rotation), quản lý tài khoản nhân viên & khách hàng, cấp lại mật khẩu (OTP).
  2. *Phân quyền (RBAC):* Quản lý vai trò (Roles: Admin, FrontDesk, Housekeeping, Customer), danh mục quyền hạn (Permissions) và gán phân quyền theo vai trò.
  3. *Chính sách & Khuyến mãi:* Quản trị kho Voucher, cấu hình mức giảm (% hoặc số tiền), hạng hội viên Loyalty tích điểm (Bạc, Vàng, Kim Cương).
  4. *Quản trị nội dung:* Quản lý danh mục điểm tham quan địa phương, bài viết blog du lịch & tin tức.
  5. *Giám sát hệ thống (Audit Logs):* Theo dõi toàn bộ lịch sử thao tác hệ thống theo thời gian thực (ai làm gì, thời điểm nào, IP nào).

---

### 🔹 THÀNH VIÊN 2: BUỒNG PHÒNG, TIỆN NGHI & VẬN HÀNH (HOUSEKEEPING)
- **Đường dẫn trang mẫu:** [`/housekeeping/rooms`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/ThanhVien2_BuongPhong/HousekeepingDashboard.razor)
- **Các chức năng cốt lõi theo slide:**
  1. *Trạng thái buồng phòng:* Theo dõi trực quan trạng thái thực tế của từng phòng (Trống sạch, Đang ở, Đang dọn, Chưa dọn, Đang sửa chữa).
  2. *Phân công dọn phòng:* Giao việc cho nhân viên buồng phòng, ghi nhận thời gian bắt đầu và hoàn tất dọn dẹp.
  3. *Kiểm soát tài sản phòng (Room Inventory):* Đối chiếu danh mục tiện nghi (TV, máy lạnh, máy sấy, khăn tắm...), ghi nhận hư hỏng/mất mát (`price_if_lost`) để tính phí đền bù khi khách check-out.
  4. *Quản lý chi phí vận hành:* Ghi nhận chi phí mua sắm hóa chất, giặt ủi, vật tư buồng phòng.

---

### 🔹 THÀNH VIÊN 3: TIỀN SẢNH (FRONT-DESK), ĐẶT PHÒNG & THANH TOÁN
- **Đường dẫn trang mẫu:** [`/frontdesk/bookings`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/ThanhVien3_TienSanh/FrontDeskDashboard.razor)
- **Các chức năng cốt lõi theo slide:**
  1. *Sơ đồ phòng & Đặt phòng:* Giao diện dạng lưới trực quan theo tầng & số phòng; tiếp nhận đơn đặt phòng trực tuyến hoặc khách vãng lai (walk-in).
  2. *Lưu trú (Check-in / Check-out):* Quy trình tiếp đón khách, quét thông tin, đổi phòng, gia hạn lưu trú.
  3. *Dịch vụ & Tiêu dùng:* Ghi nhận dịch vụ minibar, giặt ủi, ẩm thực, spa vào hóa đơn phòng.
  4. *Thanh toán & Hóa đơn:* Lập hóa đơn tổng hợp, áp dụng mã giảm giá voucher, tích hợp cổng thanh toán VNPay / MoMo, in biên lai thanh toán.
  5. *Bàn giao ca:* Báo cáo doanh thu thu ngân theo ca trực, bàn giao tiền mặt và các việc tồn đọng cho ca sau.

---

### 🔹 THÀNH VIÊN 4: TRÍ TUỆ NHÂN TẠO (AI CORE), DỰ BÁO & BÁO CÁO (AI & BI)
- **Đường dẫn trang mẫu:** [`/ai-analytics/dashboard`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/ThanhVien4_AICore/AiBiDashboard.razor)
- **Các chức năng cốt lõi theo slide:**
  1. *AI Chatbot Trợ Lý:* Hộp thoại tư vấn khách hàng tự động gợi ý phòng, bảng giá dịch vụ và các địa điểm tham quan du lịch Đà Nẵng theo mùa.
  2. *Dự báo nhu cầu đặt phòng (Demand Forecasting):* Thuật toán phân tích xu hướng đặt phòng theo tuần/tháng, đề xuất mức giá phòng tối ưu (Dynamic Pricing).
  3. *Cảnh báo nguy cơ hủy phòng (Cancellation Risk):* Phân tích các đặt phòng có nguy cơ hủy cao dựa trên hành vi đặt cọc, kênh đặt và thời gian để lễ tân chủ động liên hệ.
  4. *Báo cáo BI (Business Intelligence):* Dashboard biểu đồ trực quan hóa doanh thu, chi phí, lợi nhuận ròng, công suất buồng phòng (RevPAR / ADR).

---

## 6. Hướng Dẫn Kỹ Thuật Khi Phát Triển Trang (Developer Checklist)

Trước khi gửi pull request hoặc bàn giao mã nguồn:

1. **Khai báo Render Mode:**  
   Thêm `@rendermode InteractiveServer` ở dòng đầu trang nếu component có xử lý sự kiện `@onclick`, `@onchange`, modal hoặc input binding.
2. **Truy vấn Dữ liệu Tối ưu:**  
   - Inject `DACNContext Db` trực tiếp trong component.
   - Thêm `.AsNoTracking()` cho mọi truy vấn chỉ để hiển thị (Read-only) nhằm tối ưu bộ nhớ.
   - Luôn bọc trong khối `try { ... } catch { ... }` và có fallback mock data để giao diện không bị trắng trang khi DB đang trống.
3. **Màu sắc & Spacing:**  
   - Tuyệt đối không dùng mã màu inline rải rác kiểu `style="color: red;"`. Dùng các class `.text-danger`, `.text-primary`, `var(--hotel-*)`.
   - Giữ lề thẻ card chuẩn với `class="hotel-card mb-4"`.
4. **Kiểm tra Build & Run:**  
   - Mở terminal chạy `dotnet build src\DACN.Web` để đảm bảo 0 lỗi biên dịch.
   - Chạy `dotnet run --project src\DACN.Web` để kiểm tra trực quan trên trình duyệt tại `http://localhost:5000`.
