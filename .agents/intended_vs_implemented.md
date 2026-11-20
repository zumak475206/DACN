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
| **GAP-04** | Trạng thái phiên người dùng Blazor (Circuit State) dễ mất khi F5 | Trải nghiệm & Phiên đăng nhập | **Trung bình** | F5 trình duyệt đưa phiên về `Khách Vãng Lai` do thiếu Cookie |
| **GAP-05** | Phân chia thư mục Tiền sảnh giữa TV2 và TV3 | Cấu trúc phân công nhóm | **Thấp** | Trang `/frontdesk/bookings` nằm ở TV2 thay vì TV3 |
| **GAP-06** | Giới hạn hạn mức API Chatbot trên giao diện người dùng | Chi phí & Quota Gemini AI | **Thấp** | Widget chat chưa debounce client-side chống spam click |

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

### 🟡 GAP-04: Tính Bay Mùi Của Phiên Làm Việc (Circuit In-Memory Session) Khi Tải Lại Trang

#### 1. Ý đồ thiết kế (Documented Intent)
* Tài liệu [project_architecture.md](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/project_architecture.md#L6-L8) quy định:
  > *"Hệ thống sử dụng cơ chế JWT + Sliding Refresh Token có hạn 14 ngày, duy trì trạng thái đăng nhập liên tục cho người dùng."*

#### 2. Thực tế triển khai trong mã nguồn (Implemented Reality)
* **Tại `UserSessionService.cs`:**
  Trạng thái đăng nhập được lưu trữ trong thuộc tính:
  ```csharp
  public CurrentUserInfo CurrentUser { get; private set; }
  ```
  `UserSessionService` được đăng ký dạng `Scoped` (gắn liền với 1 SignalR Circuit trong Blazor Server).
* **Hậu quả:** Khi người dùng nhấn **F5** trên trình duyệt, kết nối SignalR Circuit cũ bị hủy và tạo mới $\rightarrow$ `UserSessionService` khởi tạo lại với giá trị mặc định:
  `CurrentUser.HoTen = "Khách Vãng Lai"` và `CurrentUser.Role = "Customer"`.
  Người dùng đang ở trang quản trị nội bộ sẽ bị văng ra màn hình 403 hoặc bắt đăng nhập lại.

#### 3. Kẻ tấn công & Nạn nhân (Attacker & Victim)
* **Nạn nhân:** Trải nghiệm người dùng thực tế bị gián đoạn; nhân viên mỗi lần F5 trang web đều phải đăng nhập lại từ đầu.

#### 4. Giải pháp khắc phục triệt để (Concrete Fix)
Sử dụng `ProtectedLocalStorage` của Blazor Server hoặc ghi nhận Cookie xác thực HTTP-Only sau khi đăng nhập thành công để tự động phục hồi (rehydrate) phiên làm việc trong `OnInitializedAsync()` của `App.razor` / `CustomerLayout.razor`.

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

## 🛠️ DANH SÁCH KHUYẾN NGHỊ ƯU TIÊN HÀNH ĐỘNG (ACTION PLAN)

```
[Ưu tiên 1 - Bảo mật]  Thêm guard URL OnInitializedAsync() trong AccountManagement.razor & RbacMatrix.razor.
[Ưu tiên 2 - Tài chính] Chặn quyền tự duyệt chi phí của Kế toán trong ExpenseManagement.razor.
[Ưu tiên 3 - Kiểm toán] Bổ sung IAuditService vào CheckInOut.razor khi nhận phòng & check-out thanh toán.
[Ưu tiên 4 - Tiện ích]  Lưu token phiên làm việc vào ProtectedLocalStorage để F5 không bị mất đăng nhập.
```

---

> [!TIP]
> Báo cáo này độc lập với tài liệu kiến trúc và được lưu trữ trực tiếp tại [`.agents/intended_vs_implemented.md`](file:///d:/THAI_ANH_KY_WORKSPACE/Projects/DACN/.agents/intended_vs_implemented.md) để hỗ trợ các phiên làm việc tiếp theo của nhóm và AI.
