using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // اضافه شود
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Repositories;
using TicketHub.Infrastructure.Services;
using TicketHub.Web.Components;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --------- بخش جدید احراز هویت با کوکی ---------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TicketHub_Session";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";

        // --- تنظیمات امنیتی استاندارد ---
        options.Cookie.HttpOnly = true; // جلوگیری از سرقت کوکی توسط کدهای جاوااسکریپت مخرب (XSS)
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // اطمینان از اینکه کوکی فقط روی بستر امن HTTPS ارسال شود
        options.Cookie.SameSite = SameSiteMode.Strict; // جلوگیری از ارسال درخواست‌های جعلی از سایت‌های دیگر (حملات CSRF)

        // --- تنظیمات انقضا ---
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // کوکی بعد از ۷ روز منقضی می‌شود
        options.SlidingExpiration = true; // اگر کاربر در روز ششم به سایت سر زد، انقضای کوکی خودکار ۷ روز دیگر تمدید می‌شود
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
// ----------------------------------------------

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IStatusService, StatusService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // فراخوانی متد برای ساخت دیتابیس و داده‌ها
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "خطایی در هنگام ایجاد دیتابیس یا تزریق داده‌ها رخ داد.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// این دو خط حتماً قبل از MapRazorComponents باشند
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    return TypedResults.LocalRedirect("/login");
});

app.Run();