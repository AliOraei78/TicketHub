using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // اضافه شود
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Repositories;
using TicketHub.Infrastructure.Services;
using TicketHub.Web.Components;
using TicketHub.Web.Facades;
using TicketHub.Web.Middlewares;
using TicketHub.Web.State;
using TicketHub.Web.States;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .EnableSensitiveDataLogging() // ثبت مقادیر ارسال شده به دیتابیس در لاگ
           .EnableDetailedErrors());     // نمایش جزئیات دقیق‌تر خطاهای EF

// --- افزودن کدهای Mapster ---
var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
// مسیر اسمبلی حاوی MapsterConfig.cs را بدهید (مثلا typeof(MapsterConfig).Assembly)
typeAdapterConfig.Scan(typeof(TicketHub.Application.Mapping.MapsterConfig).Assembly);
builder.Services.AddSingleton(typeAdapterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();
// ------------------------------

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

// ------ این خطوط باید حذف شوند ------
// builder.Services.AddScoped<ITicketService, TicketService>();
// builder.Services.AddScoped<IProjectService, ProjectService>();
// builder.Services.AddScoped<IStatusService, StatusService>();
// ------------------------------------

// سرویس‌های مورد نیاز که باقی می‌مانند یا اضافه می‌شوند:
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFieldTypeService, FieldTypeService>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<UserFacade>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ProjectState>();
builder.Services.AddScoped<ProjectFacade>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<CategoryState>();
builder.Services.AddScoped<CategoryFacade>();
builder.Services.AddScoped<IPriorityService, PriorityService>();
builder.Services.AddScoped<PriorityState>();
builder.Services.AddScoped<PriorityFacade>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<RoleState>();
builder.Services.AddScoped<RoleFacade>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<StatusState>();
builder.Services.AddScoped<StatusFacade>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();

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

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
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