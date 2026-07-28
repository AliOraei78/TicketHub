using Serilog.Context;
using System.Security.Claims;

namespace TicketHub.Web.Middlewares; // نام فضایی (Namespace) خود را بررسی کنید

public class LogEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public LogEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown"
            : "Anonymous";

        // افزودن اطلاعات به کانتکست Serilog
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        {
            // اضافه کردن شناسه به هدر خروجی برای ردیابی در کلاینت (اختیاری)
            context.Response.Headers.Append("X-Correlation-ID", correlationId);

            await _next(context);
        }
    }
}