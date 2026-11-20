# 🔍 KIỂM TOÁN GAP: INTENDED VS. IMPLEMENTED
## Dự Án: BookingStay.AI — Quản Lý Khách Sạn & Trợ Lý Du Lịch (DACN)

> **Báo cáo đối chiếu: Ý đồ thiết kế (Design Intent) vs. Thực tế triển khai (Implemented Code)**  
> **Thời điểm kiểm toán:** 20/11/2026 | **Phiên bản mã nguồn:** Git commit `fe57190`  
> **Phương pháp:** Auditing the Gap (`intended-vs-implemented` standard)

---

## 📑 MỤC LỤC & TỔNG QUAN ĐÁNH GIÁ

Một bộ linter hay test runner chỉ kiểm tra tính nhất quán nội tại của code (`104/104 tests pass`); nó không thể phát hiện liệu mã nguồn có thực sự làm đúng những gì tài liệu thiết kế nghiệp vụ quy định hay không. Báo cáo này rà soát toàn bộ ranh giới tin cậy (Trust Boundaries), phân quyền (RBAC), quy tắc tài chính và tính toàn vẹn dữ liệu giữa tài liệu thiết kế (`DESIGN.md`, `.agents/domain_analysis.md`) và mã nguồn thực tế (`src/DACN.Web/`).

### Bảng Tổng Hợp Các Điểm Lệch (Gap Matrix)
| STT | Vấn Đề (Gap Title) | Ranh Giới Bị Ảnh Hưởng | Mức Độ | Trạng Thái Thực Tế |
| :---: | :--- | :--- | :---: | :--- |
| **GAP-01** | Thiếu chốt chặn URL cấp trang (Page-Level Guard) trên giao diện Blazor | Quyền riêng tư & Dữ liệu nội bộ | **Cao** | Staff bậc thấp xem được danh sách tài khoản nếu gõ thẳng URL |
| **GAP-02** | Xung đột nguyên tắc kiểm soát nội bộ (SoD) trong phê duyệt chi phí | Tài chính & Dòng tiền | **Cao** | Kế toán có thể tự lập và tự duyệt chi phí của chính mình |
| **GAP-03** | Thiếu ghi nhận Audit Log trong quy trình Check-in / Check-out | Kiểm toán & Trách nhiệm giải trình | **Trung bình** | Check-out thu tiền mặt không ghi vào `NhatKyHeThong` |
| **GAP-04** | Đăng ký `AddSingleton` cho UserSessionService gây ô nhiễm phiên đăng nhập chéo giữa người dùng | Bảo mật phiên & Đa người dùng | **Nghiêm trọng (Critical)** | Mọi trình duyệt/người dùng dùng chung 1 phiên đăng nhập; F5 bị reset |
| **GAP-05** | Phân chia thư mục Tiền sảnh giữa TV2 và TV3 | Cấu trúc phân công nhóm | **Thấp** | Trang `/frontdesk/bookings` nằm ở TV2 thay vì TV3 |
| **GAP-06** | Giới hạn hạn mức API Chatbot trên giao diện người dùng | Chi phí & Quota Gemini AI | **Thấp** | Widget chat chưa debounce client-side chống spam click |
| **GAP-07** | Sai lệch kiểm tra chuỗi trạng thái phòng trên Admin Dashboard khiến chỉ số ca trực luôn bằng 0 | Hiển thị nghiệp vụ & Giám sát | **Cao** | `AdminDashboard.razor` so sánh `"dang_o"` thay vì `"Occupied"`, sai cột vệ sinh |
| **GAP-08** | Danh mục hạng phòng công khai (`/rooms`) bị hardcode tĩnh thay vì truy vấn bảng `LoaiPhong` | Tính nhất quán dữ liệu công khai | **Trung bình** | Trang giới thiệu phòng dùng List mẫu trong RAM, không phản ánh CSDL |
| **GAP-09** | Phân hệ lịch sử đặt phòng khách hàng (`/my-bookings`) chạy Mock data, hủy phòng không lưu SQL | Trải nghiệm khách & Toàn vẹn đơn | **Cao** | Khách đăng nhập nhưng chỉ thấy 2 đơn mẫu tĩnh; bấm hủy không ghi CSDL |
| **GAP-10** | Form đặt phòng tại Trang chủ (`Home.razor`) chỉ giả lập sinh mã ngẫu nhiên mà chưa tạo đơn thực tế | Dòng doanh thu & Đặt phòng Web | **Nghiêm trọng (Critical)** | Khách đặt phòng ngoài Home chỉ nhận popup mã ảo; CSDL không nhận đơn |
| **GAP-11** | Nhật ký dọn phòng (`CleaningTasks.razor`) không ghi nhận nhân viên thực hiện do thiếu `UserSessionService` | Trách nhiệm ca trực buồng phòng | **Trung bình** | Nhiệm vụ dọn buồng tạo mới & cập nhật đều để `MaNhanVien = null` |

---

## 🔎 CHI TIẾT CÁC PHÁT HIỆN KIỂM TOÁN (AUDIT FINDINGS)

---

### 🔴 GAP-01: Thiếu Chốt Chặn Phân Quyền Cấp Trang Trên Giao Diện Quản Trị Tài Khoản & RBAC

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [domain_analysis.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/domain_analysis.md#L68-L75) và `DESIGN.md` quy định:
  > *"Quyền `ManageUsers` (ID 11) chỉ được cấp duy nhất cho vai trò `Admin`. Quản lý (Manager), Lễ tân (FrontDesk), Buồng phòng (Housekeeping) và Kế toán (Accountant) tuyệt đối KHÔNG có quyền truy cập hoặc can thiệp phân hệ Quản lý tài khoản và Ma trận phân quyền."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `MainLayout.razor`:**
  Lines 50-55 chỉ chặn `Customer` và người dùng chưa đăng nhập:
  ```razor
  @if (!Session.CurrentUser.IsLoggedIn || Session.CurrentUser.Role == "Customer")
  {
      <!-- Hiển thị màn hình 403 Từ chối -->
  }
  else
  {
      @Body
  }
  ```
* **Tại `AccountManagement.razor`:**
  Lines 161-174 tải toàn bộ danh sách người dùng ngay khi khởi tạo trang mà không kiểm tra quyền:
  ```csharp
  protected override async Task OnInitializedAsync()
  {
      await LoadDataAsync();
  }

  private async Task LoadDataAsync()
  {
      roles = await Db.VaiTros.AsNoTracking().OrderBy(r => r.MaId).ToListAsync();
      users = await Db.NguoiDungs.AsNoTracking().Include(u => u.MaVaiTroNavigation).ToListAsync();
  }
  ```
  *(Các nút Đổi vai trò và Khóa tài khoản có ẩn `@if (Session.HasPermission(Quyen.ManageUsers))`, nhưng bảng dữ liệu người dùng vẫn được hiển thị trọn vẹn).*
* **Tại `RbacMatrix.razor`:**
  Lines 173-185 cũng nạp toàn bộ danh sách vai trò và quyền hạn lên màn hình cho bất kỳ nhân viên nào truy cập.

#### 3. Kẻ tấn công & Nạn nhân (Attacker & Victim)
* **Kẻ tấn công (Insider):** Một nhân viên Lễ tân hoặc Buồng phòng đăng nhập tài khoản của họ, sau đó gõ thẳng URL `https://localhost:7136/admin/accounts` hoặc `/admin/rbac` vào thanh địa chỉ trình duyệt.
* **Nạn nhân (Victim):** Toàn bộ nhân sự và khách hàng bị lộ thông tin danh tính, email, số điện thoại, điểm tích lũy và vai trò nội bộ.

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
Thêm guard kiểm tra quyền ngay đầu hàm `OnInitializedAsync()` trong `AccountManagement.razor` và `RbacMatrix.razor`:
```csharp
protected override async Task OnInitializedAsync()
{
    if (!Session.HasPermission(Quyen.ManageUsers))
    {
        Nav.NavigateTo("/admin/dashboard");
        return;
    }
    await LoadDataAsync();
}
```

---

### 🔴 GAP-02: Vi Phạm Nguyên Tắc Phân Tách Trách Nhiệm (Segregation of Duties) Trong Phê Duyệt Chi Phí

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [domain_analysis.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/domain_analysis.md#L48) và nguyên tắc tài chính khách sạn:
  > *"Nhân viên kế toán lập phiếu chi vận hành; Việc phê duyệt chi phí (`TrangThai = 'Approved'`) để giải ngân bắt buộc phải do Quản lý (Manager) hoặc Quản trị viên (Admin) xét duyệt nhằm tránh biển thủ tài chính."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `ExpenseManagement.razor`:**
  Line 148 kiểm tra điều kiện hiển thị nút **Duyệt / Từ chối**:
  ```razor
  @if (expense.TrangThai == "Pending" && (Session.CurrentUser.Role == "Admin" || Session.CurrentUser.Role == "Manager" || Session.HasPermission(Quyen.ManageExpenses)))
  ```
* **Tại cơ sở dữ liệu (`DACN_Full.sql` line 994):**
  Quyền `ManageExpenses` (ID 17) được cấp cho cả 3 vai trò: `Admin` (1), `Manager` (2), và `Accountant` (5).
* **Hậu quả:** Do `Accountant` có `ManageExpenses`, điều kiện `Session.HasPermission(Quyen.ManageExpenses)` trả về `true`. Nhân viên kế toán vừa có quyền **Tạo khoản chi** vừa có quyền **Tự bấm nút Duyệt** cho chính khoản chi của mình.

#### 3. Kẻ tấn công & Nạn nhân (Attacker & Victim)
* **Kẻ tấn công:** Nhân viên kế toán thiếu trung thực.
* **Nạn nhân:** Doanh nghiệp khách sạn bị thất thoát ngân sách do không có cơ chế đối soát chéo (Four-Eyes Principle).

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
Chỉnh sửa điều kiện tại `ExpenseManagement.razor`:
1. Chỉ cho phép `Admin` hoặc `Manager` duyệt chi.
2. Tuyệt đối không cho phép người tạo tự duyệt khoản chi của chính mình:
```csharp
private bool CanApproveExpense(ChiPhiVanHanh expense)
{
    var isManagerOrAdmin = Session.CurrentUser.Role is "Admin" or "Manager";
    var isNotCreator = expense.NguoiTaoId != Session.CurrentUser.Id;
    return isManagerOrAdmin && isNotCreator;
}
```

---

### 🟡 GAP-03: Thiếu Nhật Ký Kiểm Toán (Audit Trail) Trong Quy Trình Nhận Phòng & Trả Phòng

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [project_architecture.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/project_architecture.md#L49-L51) quy định:
  > *"Mọi hành vi nhạy cảm (Đăng nhập, phân quyền, hủy phòng, check-in, check-out quyết toán thu tiền) đều phải ghi nhận vào `NhatKyHeThong` để phục vụ hậu kiểm tra cứu."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `CheckInOut.razor`:**
  Hàm `ProcessCheckInAsync(int bookingId)` (lines 200-245) và `SettleAndCheckOutAsync(int bookingId)` (lines 261-340) thực hiện:
  * Đổi trạng thái đơn đặt phòng sang `CheckedIn` / `CheckedOut`.
  * Thu tiền mặt hoặc chuyển khoản và tự động sinh bản ghi `ThanhToan`.
  * Đổi trạng thái buồng phòng sang `Dirty` và phát sinh ca dọn phòng `NhatKyBuongPhong`.
  * **TUY NHIÊN:** File này **không hề inject** `IAuditService` và không có dòng lệnh nào gọi `AuditService.GhiNhanAsync(...)`.

#### 3. Kẻ tấn công & Nạn nhân (Attacker & Victim)
* **Kẻ tấn công:** Lễ tân thu tiền mặt của khách khi check-out nhưng không thể xác minh ai là người bấm nút hoàn tất trên ca trực nếu có tranh chấp tiền mặt.
* **Nạn nhân:** Chủ khách sạn và bộ phận thanh tra không có chứng cứ kiểm toán số để đối soát ca trực.

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
1. Thêm `@inject IAuditService AuditService` vào đầu [CheckInOut.razor](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/ThanhVien2_BuongPhong/CheckInOut.razor).
2. Gọi hàm ghi nhận sau khi `transaction.CommitAsync()` thành công:
```csharp
await AuditService.GhiNhanAsync(
    "CHECK_OUT_QUYET_TOAN", 
    "DatPhong", 
    booking.MaId.ToString(), 
    $"Check-out đơn {booking.MaDatPhong}, thu {balance:N0} đ qua {paymentMethod}", 
    Session.CurrentUser.Id, 
    Session.CurrentUser.Email, 
    "127.0.0.1", 
    true);
```

---

### 🔴 GAP-04: Đăng Ký `AddSingleton` Cho UserSessionService Gây Ô Nhiễm Phiên Chéo Giữa Các Người Dùng (Multi-User Session Pollution)

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [project_architecture.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/project_architecture.md#L35) và kiến trúc chuẩn của Blazor Server:
  > *"Mỗi kết nối người dùng (SignalR Circuit) phải có một vùng nhớ phiên làm việc độc lập (`Scoped`), đảm bảo người dùng A đăng nhập không làm ảnh hưởng đến người dùng B."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `Program.cs` (Line 39):**
  Dịch vụ quản lý phiên người dùng đang được đăng ký là **Singleton**:
  ```csharp
  builder.Services.AddSingleton<UserSessionService>();
  ```
* **Tại `UserSessionService.cs`:**
  Trạng thái đăng nhập được lưu trữ trực tiếp trên trường thuộc tính của lớp:
  ```csharp
  public CurrentUserInfo CurrentUser { get; private set; }
  ```
* **Hậu quả nghiêm trọng:** 
  1. **Ô nhiễm phiên chéo (Cross-session Leakage):** Trong Blazor Server, một service `Singleton` tồn tại duy nhất trên toàn bộ tiến trình ứng dụng. Nếu Người dùng A đăng nhập bằng tài khoản `admin@hotel.com`, thì ngay lập tức bất kỳ người dùng B nào mở trình duyệt khác (hoặc máy khác) truy cập vào web **cũng sẽ dùng chung phiên Admin đó** mà không cần đăng nhập!
  2. **Văng đăng xuất đồng loạt:** Khi Người dùng B bấm nút "Đăng xuất", hàm `Session.Logout()` sẽ biến `CurrentUser` về `Khách Vãng Lai` $\rightarrow$ Người dùng A bên máy kia cũng bị đăng xuất theo do dùng chung một instance Singleton.
  3. **Mất phiên khi F5:** Do trạng thái chỉ nằm trong bộ nhớ RAM mà không được đồng bộ xuống Cookie HTTP-Only hay `ProtectedLocalStorage`, khi người dùng nhấn F5, Circuit mới được sinh ra nhưng lại phụ thuộc vào trạng thái hiện tại của Singleton.

#### 3. Kẻ tấn công & Nạn nhân (Attacker & Victim)
* **Kẻ tấn công / Khách vãng lai:** Bất kỳ ai truy cập vào trang web sau khi một nhân viên hoặc Admin vừa đăng nhập xong sẽ ngẫu nhiên sở hữu toàn bộ quyền Admin của người trước đó mà không cần biết mật khẩu.
* **Nạn nhân:** Toàn bộ hệ sinh thái khách sạn bị chiếm quyền điều khiển trái phép (Privilege Hijacking).

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
1. **Sửa ngay trong `Program.cs` line 39:** Đổi từ `AddSingleton` sang `AddScoped`:
   ```csharp
   // ĐÚNG CHO BLAZOR SERVER: Mỗi Circuit SignalR có 1 UserSessionService riêng
   builder.Services.AddScoped<UserSessionService>();
   ```
2. **Khắc phục mất phiên khi F5:** Sử dụng `ProtectedLocalStorage` của Blazor Server hoặc ghi nhận Cookie xác thực HTTP-Only sau khi đăng nhập thành công để tự động phục hồi (rehydrate) phiên làm việc trong `OnInitializedAsync()` của `App.razor` / `CustomerLayout.razor`.

---

### 🟢 GAP-05: Lệch Cấu Trúc Thư Mục Tiền Sảnh Giữa Thành Viên 2 Và Thành Viên 3

#### 1. Ý đồ thiết kế (Documented Intent)
* Báo cáo phân công đồ án:
  * **Thành viên 2:** Phụ trách Buồng phòng & Cơ sở vật chất (`Housekeeping`).
  * **Thành viên 3:** Phụ trách Tiền sảnh, Tiếp đón, Thu ngân & Hóa đơn (`FrontDesk`).

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* Do lịch sử rẽ nhánh Git của nhóm:
  * Nhánh `noaye` của TV2 đã chuyển 2 màn hình Tiền sảnh vào thư mục của mình:
    - `src/DACN.Web/Components/Pages/ThanhVien2_BuongPhong/RoomRackBookings.razor` (`/frontdesk/bookings`)
    - `src/DACN.Web/Components/Pages/ThanhVien2_BuongPhong/CheckInOut.razor` (`/frontdesk/checkin-out`)
  * Nhánh `feature/tv3-billing-finance` của TV3 giữ 2 màn hình còn lại:
    - `src/DACN.Web/Components/Pages/ThanhVien3_TienSanh/BillingServices.razor` (`/frontdesk/services-billing`)
    - `src/DACN.Web/Components/Pages/ThanhVien3_TienSanh/ExpenseManagement.razor` (`/frontdesk/expenses`)

#### 3. Đánh giá mức độ ảnh hưởng
* **Mức độ:** Thấp / Phi rủi ro bảo mật (Cosmetic Structure Drift).
* Hệ thống vẫn biên dịch và định tuyến hoàn toàn chính xác vì các URL `@page` không bị trùng lặp. Tuy nhiên, khi báo cáo phân chia công việc với giảng viên hướng dẫn, cấu trúc thư mục này có thể gây nhầm lẫn về phân định quyền sở hữu code giữa TV2 và TV3.

---

### 🟢 GAP-06: Giới Hạn Hạn Mức Gọi API Chatbot Trên Giao Diện Người Dùng (Client Debounce & Rate Limiting)

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [project_architecture.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/project_architecture.md#L45-L48) quy định:
  > *"Tích hợp Gemini 2.5 Flash thông qua `GoiYDuLichService`. Hệ thống cần có cơ chế phòng thủ chi phí: cache câu trả lời phổ biến, giới hạn tần suất gọi liên tục nhằm bảo vệ quota API Key."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `CustomerLayout.razor` (lines 459-478) và `AiChatbot.razor` (lines 186-207):**
  Widget chat đã khóa cờ `isSending` trong lúc request đang xử lý. Tuy nhiên:
  * Không có client-side cooldown (người dùng có thể nhấn Enter liên tục ngay sau khi nhận câu trả lời trước đó).
  * Chưa giới hạn số lượng tin nhắn tối đa trong một phiên chat ẩn danh (`Khách vãng lai` chưa đăng nhập có thể chat vô hạn, tiêu tốn token của máy chủ).

#### 3. Khắc phục khuyến nghị
* Đặt trần tối đa 5 câu hỏi/phiên đối với người dùng chưa đăng nhập, sau đó yêu cầu đăng nhập để tiếp tục hội thoại.
* Thêm khoảng trễ 2 giây giữa các lần gửi câu hỏi liên tiếp.

---

### 🔴 GAP-07: Sai Lệch Kiểm Tra Chuỗi Trạng Thái Phòng Trên Admin Dashboard Khiến Chỉ Số Ca Trực Luôn Bằng 0

#### 1. Ý đồ thiết kế (Documented Intent)
* Trang tổng quan ca trực [AdminDashboard.razor](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/AdminDashboard.razor) hiển thị 4 thẻ KPI nhanh cho ca trực:
  * *Khách Đang Ở (Tiền Sảnh):* Đếm số phòng đang có khách lưu trú.
  * *Phòng Cần Dọn (Buồng Phòng):* Đếm số phòng bẩn hoặc đang trong quy trình vệ sinh.

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `AdminDashboard.razor` (lines 173-174):**
  ```csharp
  occupiedCount = rooms.Count(r => r.TrangThai == "dang_o");
  cleaningCount = rooms.Count(r => r.TrangThai == "don_dep" || r.TrangThai == "chua_don");
  ```
* **Sự thật trong cơ sở dữ liệu SQL Server & `StatusMapper.cs`:**
  1. Trạng thái phòng lưu trú `r.TrangThai` tuân theo chuẩn quốc tế: `"Occupied"`, `"Available"`, `"Reserved"`, `"Maintenance"`. Chuỗi `"dang_o"` chỉ là input tìm kiếm từ client và đã được `StatusMapper.NormalizePhongTrangThai` đưa về `"Occupied"`. Vì vậy điều kiện `r.TrangThai == "dang_o"` **luôn trả về 0**.
  2. Trạng thái dọn vệ sinh nằm ở cột `r.TrangThaiVeSinh` (`"Dirty"`, `"Inspecting"`, `"Clean"`), hoàn toàn **không nằm ở cột `r.TrangThai`**. Do đó, `rooms.Count(r => r.TrangThai == "don_dep" || r.TrangThai == "chua_don")` **cũng luôn trả về 0**.

#### 3. Ảnh hưởng thực tế
* Ban quản trị khi mở dashboard luôn thấy số liệu: `0 / 30 Phòng đang ở` và `0 Phòng cần dọn`, tạo cảm giác hệ thống bị mất dữ liệu hoặc không hoạt động.

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
Sửa hàm `LoadStatsAsync` trong `AdminDashboard.razor`:
```csharp
private async Task LoadStatsAsync()
{
    try
    {
        rooms = await Db.Phongs.AsNoTracking().ToListAsync();
        if (rooms.Any())
        {
            occupiedCount = rooms.Count(r => StatusMapper.IsRoomOccupied(r.TrangThai));
            cleaningCount = rooms.Count(r => r.TrangThaiVeSinh is "Dirty" or "Inspecting");
        }
    }
    catch { }
}
```

---

### 🟡 GAP-08: Danh Mục Hạng Phòng Công Khai (/rooms) Bị Hardcode Tĩnh Thay Vì Truy Vấn Bảng LoaiPhong

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [domain_analysis.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/domain_analysis.md) quy định:
  > *"Hệ thống quản lý 4 hạng phòng (Standard, Superior, Deluxe, Suite) được lưu trữ trong bảng `LoaiPhong`. Giá phòng, diện tích và mô tả được cấu hình tập trung trong cơ sở dữ liệu."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `Customer/RoomsList.razor` (lines 3 & 81-87):**
  * Line 3 có khai báo `@inject DACNContext Db`.
  * Tuy nhiên `Db` không hề được gọi trong toàn bộ trang.
  * Danh mục hiển thị được gán cứng bằng một danh sách bộ nhớ tĩnh:
    ```csharp
    private List<RoomCatalog> roomsList = new()
    {
        new("Deluxe Sea View (Hướng Biển)", 1000000, 38, ...),
        new("Suite Gia Đình (Family Suite)", 1500000, 65, ...),
        new("Superior Giường Đôi (Double Bed)", 750000, 32, ...),
        new("Standard City View (Hướng Phố)", 500000, 26, ...)
    };
    ```
* **Hậu quả:** Khi người quản lý điều chỉnh giá niêm yết hoặc thêm hạng phòng mới trong cơ sở dữ liệu, trang giới thiệu `/rooms` vẫn hiển thị giá cũ và 4 phòng cố định ban đầu.

#### 3. Giải pháp khắc phục triệt để (Concrete Fix)
Nạp dữ liệu từ `Db.LoaiPhongs.AsNoTracking().Where(l => l.DangHoatDong)` trong `OnInitializedAsync()` để đồng bộ 100% với giá thực tế của khách sạn.

---

### 🔴 GAP-09: Phân Hệ Lịch Sử Đặt Phòng Khách Hàng (/my-bookings) Chạy Mock Data, Hủy Phòng Không Lưu SQL

#### 1. Ý đồ thiết kế (Documented Intent)
* Khách hàng đăng nhập cổng Portal được theo dõi danh sách các đơn đặt phòng của chính mình, xem mã đặt phòng `#DP-xxxx`, trạng thái cọc và gửi yêu cầu hủy đơn nếu còn thời hạn.

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `Customer/MyBookings.razor` (lines 3 & 120-134):**
  * Dù có `@inject DACNContext Db` và `@inject UserSessionService Session`, trang khởi tạo một danh sách cứng:
    ```csharp
    private List<MyBookingItem> myBookings = new()
    {
        new() { Id = 1002, RoomType = "Deluxe Sea View (Hướng Biển)", ... },
        new() { Id = 1001, RoomType = "Suite Gia Đình 2 Phòng Ngủ", ... }
    };
    ```
  * Hàm `CancelBooking(int id)` chỉ sửa trường thuộc tính trong bộ nhớ RAM (`item.Status = "da_huy";`), hoàn toàn không gọi `Db.SaveChangesAsync()` hay cập nhật trạng thái đơn trong SQL Server.
* **Hậu quả:** Khách hàng A hay Khách hàng B đăng nhập vào trang `/my-bookings` đều nhìn thấy đúng 2 đơn mẫu 1001 và 1002 này, không xem được đơn thực tế do chính mình vừa đặt.

#### 3. Giải pháp khắc phục triệt để (Concrete Fix)
1. Trong `OnInitializedAsync()`, truy vấn theo tài khoản khách hàng:
   ```csharp
   if (Session.CurrentUser.IsLoggedIn)
   {
       var dbBookings = await Db.DatPhongs
           .AsNoTracking()
           .Include(b => b.ChiTietDatPhongs).ThenInclude(ct => ct.MaLoaiPhongNavigation)
           .Where(b => b.MaKhachHang == Session.CurrentUser.Id || b.EmailNguoiDat == Session.CurrentUser.Email)
           .OrderByDescending(b => b.NgayTao)
           .ToListAsync();
       // Map sang danh sách hiển thị
   }
   ```
2. Trong `CancelBooking`, đổi `DatPhong.TrangThai = "Cancelled"` và lưu vào CSDL kèm `AuditService.GhiNhanAsync`.

---

### 🔴 GAP-10: Form Đặt Phòng Tại Trang Chủ (Home.razor) Chỉ Giả Lập Sinh Mã Ngẫu Nhiên Mà Chưa Tạo Bản Ghi DatPhong Thực Tế

#### 1. Ý đồ thiết kế (Documented Intent)
* Khách hàng vào `Home.razor` chọn ngày nhận/trả, số khách, chọn hạng phòng, áp mã Voucher và nhấn **"Xác Nhận Đặt Phòng"** để hoàn tất đặt chỗ trực tuyến.

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `Home.razor` (lines 758-763):**
  Hàm xử lý đặt phòng được viết như sau:
  ```csharp
  private void SubmitBooking()
  {
      bookingCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1005, 9999);
      bookingSuccess = true;
  }
  ```
  * Hàm này **chỉ sinh một số nguyên ngẫu nhiên** và bật cờ thông báo thành công `bookingSuccess = true`.
  * Hoàn toàn **không thêm bản ghi** nào vào bảng `DatPhong` hay `ChiTietDatPhong`.
  * Bộ phận Tiền sảnh khi mở [RoomRackBookings.razor](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/src/DACN.Web/Components/Pages/ThanhVien2_BuongPhong/RoomRackBookings.razor) sẽ **không thấy bất kỳ đơn đặt phòng nào** được tạo từ trang chủ.

#### 3. Mức độ nghiêm trọng
* **Mức độ: Nghiêm trọng (Critical).** Đây là lỗ hổng chuyển đổi luồng nghiệp vụ (Broken Business Pipeline): Giao diện khách hàng và giao diện lễ tân PMS hoàn toàn bị đứt gãy kết nối dữ liệu. Khách hàng tưởng mình đã đặt phòng thành công, nhưng khách sạn hoàn toàn không có thông tin đơn đặt.

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
Triển khai logic lưu CSDL hoàn chỉnh trong `SubmitBookingAsync()`:
1. Tạo thực thể `DatPhong` mới với `MaDatPhong = $"DP-WEB-{DateTime.UtcNow:yyMMddHHmm}"`, `TrangThai = "Pending"`, `KenhDatPhong = "web"`.
2. Tạo `ChiTietDatPhong` tương ứng với số phòng và giá tiền sau khi trừ voucher.
3. Nếu có áp mã giảm giá, tăng `v.SoLanDaDung += 1`.
4. Gọi `await Db.SaveChangesAsync()` trong Transaction.

---

### 🟡 GAP-11: Nhật Ký Dọn Phòng (CleaningTasks.razor) Không Ghi Nhận Nhân Viên Thực Hiện Do Thiếu UserSessionService

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [domain_analysis.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/domain_analysis.md#L45) quy định:
  > *"Nhân viên buồng phòng khi nhận ca và hoàn tất dọn phòng phải được ghi nhận danh tính (`MaNhanVien`) vào bản ghi `NhatKyBuongPhong` để quản lý năng suất lao động và trách nhiệm vệ sinh phòng."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `CleaningTasks.razor`:**
  * File không hề inject `UserSessionService Session`.
  * Khi hàm `CreateMissingTasksAsync` sinh nhiệm vụ dọn phòng mới (line 190):
    ```csharp
    new NhatKyBuongPhong
    {
        MaPhong = room.MaId,
        TrangThai = "assigned",
        ThoiGianPhanCong = now,
        GhiChuKiemTra = "Phòng cần vệ sinh sau lưu trú.",
        NgayTao = now
        // MaNhanVien bỏ trống (null)
    }
    ```
  * Khi nhân viên bấm "Bắt đầu dọn" hoặc "Hoàn tất" trong `UpdateTaskAsync` (lines 220-277), trường `task.MaNhanVien` **không được cập nhật** gán ID của nhân viên đang thao tác.
* **Hậu quả:** Cột "Nhân viên" trên bảng nhật ký luôn hiển thị *"Chưa phân công"*, ban quản lý không biết ai là người đã dọn phòng.

#### 3. Giải pháp khắc phục triệt để (Concrete Fix)
1. Thêm `@inject UserSessionService Session` vào đầu `CleaningTasks.razor`.
2. Trong `UpdateTaskAsync`, khi nhân viên bấm bắt đầu dọn (`status == "in_progress"`):
   ```csharp
   if (Session.CurrentUser.IsLoggedIn && Session.CurrentUser.Role is "Housekeeping" or "Admin" or "Manager")
   {
       task.MaNhanVien = Session.CurrentUser.Id;
   }
   ```

---

## 🛠️ DANH SÁCH KHUYẾN NGHỊ ƯU TIÊN HÀNH ĐỘNG (ACTION PLAN)

```
[Khẩn cấp 1 - Bảo mật phiên]     Đổi AddSingleton thành AddScoped cho UserSessionService trong Program.cs line 39.
[Khẩn cấp 2 - Dữ liệu đặt phòng] Hoàn thiện SubmitBookingAsync() trong Home.razor để ghi thực thể DatPhong vào SQL.
[Ưu tiên 1 - Bảo mật trang]      Thêm guard URL OnInitializedAsync() trong AccountManagement, RbacMatrix, AuditLogs, VoucherManagement.
[Ưu tiên 2 - Tài chính SoD]       Chặn quyền tự duyệt chi phí của Kế toán trong ExpenseManagement.razor.
[Ưu tiên 3 - Đồng bộ dữ liệu]    Chuyển RoomsList.razor và MyBookings.razor từ mock data sang truy vấn Db.LoaiPhongs & Db.DatPhongs.
[Ưu tiên 4 - Hiển thị Dashboard] Sửa điều kiện đếm phòng trong AdminDashboard.razor (StatusMapper & r.TrangThaiVeSinh).
[Ưu tiên 5 - Trách nhiệm ca dọn] Inject UserSessionService vào CleaningTasks.razor để gán MaNhanVien khi dọn phòng.
[Ưu tiên 6 - Kiểm toán Log]      Bổ sung IAuditService vào CheckInOut.razor khi nhận phòng & check-out thanh toán.
[Ưu tiên 7 - Tiện ích F5]        Lưu token phiên làm việc vào ProtectedLocalStorage để F5 không bị mất đăng nhập.
```

---

> [!TIP]
> Báo cáo này độc lập với tài liệu kiến trúc và được lưu trữ trực tiếp tại [`.agents/intended_vs_implemented.md`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/intended_vs_implemented.md) để hỗ trợ các phiên làm việc tiếp theo của nhóm và AI.
