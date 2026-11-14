#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke tests REST API cho DACN Hotel Management
    Chạy: .\smoke_test.ps1 -BaseUrl "https://localhost:7136"
    Yêu cầu: API server đang chạy, bỏ qua SSL nếu dev cert
#>
param(
    [string]$BaseUrl = "https://localhost:7136"
)

$ErrorActionPreference = "Stop"
$script:PassCount = 0
$script:FailCount = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [int]$ExpectedStatus,
        [string]$AuthToken = ""
    )

    $headers = @{ "Content-Type" = "application/json" }
    if ($AuthToken) { $headers["Authorization"] = "Bearer $AuthToken" }

    try {
        $params = @{
            Uri                  = "$BaseUrl/$Url"
            Method               = $Method
            Headers              = $headers
            SkipCertificateCheck = $true
            StatusCodeVariable   = "actualStatus"
            ErrorAction          = "SilentlyContinue"
        }
        if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Depth 5) }

        $null = Invoke-RestMethod @params
    } catch {
        $actualStatus = $_.Exception.Response?.StatusCode.value__ ?? 0
    }

    $ok = $actualStatus -eq $ExpectedStatus
    if ($ok) {
        $script:PassCount++
        Write-Host "  ✅ PASS  [$ExpectedStatus] $Name" -ForegroundColor Green
    } else {
        $script:FailCount++
        Write-Host "  ❌ FAIL  [$ExpectedStatus expected, got $actualStatus] $Name" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🏨 DACN Hotel API — Smoke Tests" -ForegroundColor Cyan
Write-Host "   Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

# ── 1. Health / Public endpoints ───────────────────────────────────────────
Write-Host ""
Write-Host "📌 Public Endpoints" -ForegroundColor Yellow

Test-Endpoint -Name "GET /api/v1/loai-phong → 200" `
    -Url "api/v1/loai-phong" -ExpectedStatus 200

Test-Endpoint -Name "GET /api/v1/loai-phong/9999 → 404" `
    -Url "api/v1/loai-phong/9999" -ExpectedStatus 404

Test-Endpoint -Name "GET /api/v1/dich-vu → 200" `
    -Url "api/v1/dich-vu" -ExpectedStatus 200

# ── 2. Auth endpoints ──────────────────────────────────────────────────────
Write-Host ""
Write-Host "📌 Auth Endpoints" -ForegroundColor Yellow

Test-Endpoint -Name "POST /auth/dang-nhap thiếu email → 400" `
    -Url "api/v1/auth/dang-nhap" -Method "POST" `
    -Body @{ MatKhau = "123456" } -ExpectedStatus 400

Test-Endpoint -Name "POST /auth/dang-nhap email sai định dạng → 400" `
    -Url "api/v1/auth/dang-nhap" -Method "POST" `
    -Body @{ Email = "not-email"; MatKhau = "123456" } -ExpectedStatus 400

Test-Endpoint -Name "POST /auth/dang-nhap sai credentials → 401" `
    -Url "api/v1/auth/dang-nhap" -Method "POST" `
    -Body @{ Email = "nonexistent@test.com"; MatKhau = "wrongpass" } -ExpectedStatus 401

Test-Endpoint -Name "POST /auth/dang-ky thiếu HoTen → 400" `
    -Url "api/v1/auth/dang-ky" -Method "POST" `
    -Body @{ Email = "new@test.com"; MatKhau = "123456" } -ExpectedStatus 400

Test-Endpoint -Name "POST /auth/dang-ky mật khẩu quá ngắn → 400" `
    -Url "api/v1/auth/dang-ky" -Method "POST" `
    -Body @{ HoTen = "Test"; Email = "x@test.com"; MatKhau = "123" } -ExpectedStatus 400

Test-Endpoint -Name "POST /auth/lam-moi-token token rỗng → 400" `
    -Url "api/v1/auth/lam-moi-token" -Method "POST" `
    -Body @{ RefreshToken = "" } -ExpectedStatus 400

Test-Endpoint -Name "POST /auth/lam-moi-token token không tồn tại → 401" `
    -Url "api/v1/auth/lam-moi-token" -Method "POST" `
    -Body @{ RefreshToken = "fake-token-12345" } -ExpectedStatus 401

Test-Endpoint -Name "POST /auth/quen-mat-khau email hợp lệ → 200" `
    -Url "api/v1/auth/quen-mat-khau" -Method "POST" `
    -Body @{ Email = "admin@hotel.com" } -ExpectedStatus 200

# ── 3. Protected endpoints (no token) ─────────────────────────────────────
Write-Host ""
Write-Host "📌 Protected Endpoints (no token → 401)" -ForegroundColor Yellow

@(
    "api/v1/auth/toi",
    "api/v1/dat-phong",
    "api/v1/dat-phong/cua-toi",
    "api/v1/voucher",
    "api/v1/phong",
    "api/v1/ho-so/diem-thuong"
) | ForEach-Object {
    Test-Endpoint -Name "GET /$_ → 401" -Url $_ -ExpectedStatus 401
}

# ── 4. Voucher KiemTra (AllowAnonymous) ───────────────────────────────────
Write-Host ""
Write-Host "📌 Voucher KiemTra" -ForegroundColor Yellow

Test-Endpoint -Name "POST /voucher/kiem-tra mã không tồn tại → 200 HopLe=false" `
    -Url "api/v1/voucher/kiem-tra" -Method "POST" `
    -Body @{ MaCode = "KHONG_TON_TAI"; TongTienDatPhong = 1000000 } -ExpectedStatus 200

# ── 5. DatPhong validation (requires auth, test 401) ──────────────────────
Write-Host ""
Write-Host "📌 DatPhong Protected" -ForegroundColor Yellow

Test-Endpoint -Name "POST /dat-phong no token → 401" `
    -Url "api/v1/dat-phong" -Method "POST" `
    -Body @{ DanhSachPhong = @(1); NgayNhanPhong = "2027-01-10"; NgayTraPhong = "2027-01-12" } `
    -ExpectedStatus 401

# ── Kết quả ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
$total = $script:PassCount + $script:FailCount
Write-Host "Kết quả: $($script:PassCount)/$total PASS" -ForegroundColor $(if ($script:FailCount -eq 0) { "Green" } else { "Red" })

if ($script:FailCount -gt 0) {
    Write-Host "⚠️  $($script:FailCount) test thất bại. Kiểm tra API server có đang chạy không." -ForegroundColor Red
    exit 1
} else {
    Write-Host "🎉 Tất cả smoke tests PASS!" -ForegroundColor Green
    exit 0
}
