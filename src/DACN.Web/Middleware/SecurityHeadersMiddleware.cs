namespace DACN.Web.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Chống MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Chống Clickjacking
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

        // Chống reflected XSS
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Chính sách Referrer an toàn
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Hạn chế quyền trình duyệt nhạy cảm
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(), camera=(), microphone=()");

        // Xóa thông tin máy chủ nếu có
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await next(context);
    }
}
