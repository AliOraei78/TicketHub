using Audit.Core;
using Audit.EntityFramework;
using DNTCaptcha.Core;
using FluentValidation;
using Fluxor;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Mapping;
using TicketHub.Application.Services;
using TicketHub.Application.Validations;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Repositories;
using TicketHub.Infrastructure.Services;
using TicketHub.Web.Components;
using TicketHub.Web.Middlewares;
using TicketHub.Web.Security;
using Hangfire;
using Hangfire.SqlServer;
using MassTransit;
using TicketHub.Application.Behaviors;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using TicketHub.Web.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/javascript",
        "text/css"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddTicketHubRateLimiting();
builder.Services.AddTicketHubHealthChecks();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2);
        options.MaxBufferedUnacknowledgedRenderBatches = 10;
    });

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.MaximumReceiveMessageSize = 1024 * 1024;
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(15);
    hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors());

// برای جلوگیری از خطای کامپایل تا زمانی که تمام ریپازیتوری‌ها آپدیت شوند، 
// کانتکست را به صورت Scoped هم از طریق Factory ثبت می‌کنیم:
builder.Services.AddScoped<AppDbContext>(provider =>
    provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddScoped<IAppDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "TicketHubCache_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddScoped<ICacheService, DistributedCacheService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ICacheService).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
});

builder.Services.AddSignalR()
    .AddMessagePackProtocol();



var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
typeAdapterConfig.Scan(typeof(MapsterConfig).Assembly);
builder.Services.AddSingleton(typeAdapterConfig); // ثبت مقادیر ارسال شده به دیتابیس در لاگ
builder.Services.AddScoped<IMapper, ServiceMapper>(); // نمایش جزئیات دقیق‌تر خطاهای EF
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
        options.ExpireTimeSpan = TimeSpan.FromHours(1); // تغییر از ۷ روز به ۱ ساعت
        options.SlidingExpiration = false; // اگر کاربر در روز ششم به سایت سر زد، انقضای کوکی خودکار ۷ روز دیگر تمدید می‌شود
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
// ----------------------------------------------

builder.Services.AddValidatorsFromAssembly(typeof(AttachmentDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CategoryDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(FieldTypeDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(PermissionDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(PriorityDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(ProjectDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(RoleDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(StatusDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TicketDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TicketFieldDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(UserDtoValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(UserFormSubmissionResultValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(WorkflowDtoValidator).Assembly);

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

// --- ثبت هندلر خطاهای سفارشی ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// سرویس‌های مورد نیاز که باقی می‌مانند یا اضافه می‌شوند:
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IFieldTypeService, FieldTypeService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPriorityService, PriorityService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<ITicketFieldService, TicketFieldService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IWorkflowAutomationService, WorkflowAutomationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// --------- تنظیمات MassTransit + Transactional Outbox ---------
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TicketHub.Infrastructure.Consumers.SendEmailConsumer>();

    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

    var rabbitHost = builder.Configuration["RabbitMQ:Host"];
    if (!string.IsNullOrEmpty(rabbitHost))
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
});
// -----------------------------------------------------------

// --------- تنظیمات Hangfire ---------
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
});
// ------------------------------------

builder.Services.AddSingleton<ITicketEventBroker, TicketEventBroker>();
builder.Services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly));
builder.Services.AddHttpContextAccessor();

// --------- تنظیمات DNTCaptcha ---------
builder.Services.AddDNTCaptcha(options =>
{
    options.UseMemoryCacheStorageProvider()
           .ShowThousandsSeparators(false)
           .AbsoluteExpiration(minutes: 1)
           .WithEncryptionKey("TicketHub_Secret_Captcha_Key_2026!@#")
           .InputNames(new DNTCaptchaComponent
           {
               CaptchaHiddenInputName = "DNTCaptchaText",
               CaptchaHiddenTokenName = "DNTCaptchaToken",
               CaptchaInputName = "CaptchaInputText"
           })
           .Identifier("TicketHubAuth");
});
// --------------------------------------

var jsonOptions = new System.Text.Json.JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
};

Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .AuditTypeExplicitMapper(m => m
            // --- موجودیت‌های اصلی تیکت ---
            .Map<Ticket, AuditLog>()
            .Map<TicketFieldValue, AuditLog>()

            // --- موجودیت‌های پایه و تنظیمات سیستم ---
            .Map<Project, AuditLog>()
            .Map<Category, AuditLog>()
            .Map<Priority, AuditLog>()
            .Map<Role, AuditLog>()

            // --- موجودیت‌های کاربری، امنیتی و دسترسی ---
            .Map<User, AuditLog>()
            .Map<UserRole, AuditLog>()
            .Map<RoleProject, AuditLog>()
            .Map<TransitionRole, AuditLog>()

            // --- موجودیت‌های جریان کار (Workflow) و فرم‌های پویا ---
            .Map<Workflow, AuditLog>()
            .Map<WorkflowStatus, AuditLog>()
            .Map<Status, AuditLog>()
            .Map<Transition, AuditLog>()
            .Map<TransitionField, AuditLog>()
            .Map<TicketField, AuditLog>()

            .Map<Permission, AuditLog>()

            .AuditEntityAction<AuditLog>((ev, entry, auditLog) =>
            {
                auditLog.Id = 0;
                auditLog.EntityName = entry.EntityType.Name;
                auditLog.PrimaryKey = entry.PrimaryKey.Values.FirstOrDefault()?.ToString() ?? "";

                // اعمال jsonOptions در زمان تبدیل مقادیر به رشته JSON
                auditLog.OldValues = entry.Changes == null ? "" :
                    System.Text.Json.JsonSerializer.Serialize(entry.Changes.ToDictionary(c => c.ColumnName, c => c.OriginalValue), jsonOptions);

                auditLog.NewValues = entry.Changes == null ? "" :
                    System.Text.Json.JsonSerializer.Serialize(entry.Changes.ToDictionary(c => c.ColumnName, c => c.NewValue), jsonOptions);

                auditLog.ChangedAt = DateTime.UtcNow;

                // دریافت شناسه کاربر از HttpContext جاری
                var httpContextAccessor = ev.GetEntityFrameworkEvent().GetDbContext().GetService<IHttpContextAccessor>();

                var userClaim = httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

                if (userClaim != null && int.TryParse(userClaim.Value, out int currentUserId))
                {
                    auditLog.UserId = currentUserId;
                }
                else
                {
                    auditLog.UserId = 0; // در صورتی که کاربری لاگین نکرده باشد (مثل تغییرات توسط سیستم)
                }
            })));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // ساخت کانتکست جدید و ایزوله از طریق فکتوری
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = factory.CreateDbContext();

        // فراخوانی متد برای ساخت دیتابیس و داده‌ها
        await DbInitializer.InitializeAsync(context, app.Configuration);
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
app.UseExceptionHandler(); 
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAntiforgery();

// این دو خط حتماً قبل از MapRazorComponents باشند
app.UseAuthentication();
app.UseAuthorization();

// داشبورد و کارهای پس‌زمینه Hangfire
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new TicketHub.Web.Security.HangfireDashboardAuthFilter() },
    DashboardTitle = "مدیریت کارهای پس‌زمینه TicketHub"
});

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IWorkflowAutomationService>(
        "workflow-automatic-transitions",
        service => service.ProcessAutomaticTransitionsAsync(),
        "*/1 * * * *" // هر ۱ دقیقه
    );
    recurringJobManager.AddOrUpdate<IWorkflowAutomationService>(
        "workflow-deadline-checker",
        service => service.ProcessDeadlinesAsync(),
        "*/2 * * * *" // هر ۲ دقیقه
    );
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<TicketHub.Web.Hubs.TicketHubHub>("/hubs/tickethub");
app.MapControllers();
app.MapDefaultControllerRoute();
app.MapTicketHubHealthChecks();

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    return TypedResults.LocalRedirect("/login");
}).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

// --------- API کپچای داینامیک ---------
app.MapGet("/api/captcha", (IDNTCaptchaApiProvider apiProvider) =>
{
    var result = apiProvider.CreateDNTCaptcha(new DNTCaptchaTagHelperHtmlAttributes
    {
        Language = Language.Persian,
        DisplayMode = DisplayMode.ShowDigits,
    });

    return Results.Ok(result);
}).RequireRateLimiting(RateLimitingExtensions.AntiSpamPolicy);
// --------------------------------------

// --------- Dev Quick-Login Endpoint (Development Only) ---------
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/login", async (HttpContext context, IUserService userService, string? role) =>
    {
        var defaultAdminEmail = app.Configuration["InitialAdmin:Email"] ?? "admin@tickethub.io";
        var targetEmail = role switch
        {
            "support" => "support@tickethub.io",
            "tech" => "tech@tickethub.io",
            "user" => "user@tickethub.io",
            _ => defaultAdminEmail // default: admin
        };

        var user = await userService.GetByEmailAsync(targetEmail);
        if (user == null)
        {
            return Results.Problem($"User with email '{targetEmail}' was not found.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        foreach (var r in user.RoleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, r));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        });

        return Results.Redirect("/");
    });
}
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TicketHub.Infrastructure.Data.AppDbContext>>();
        using var dbContext = dbFactory.CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not automatically migrate database on startup.");
    }
}

app.Run();