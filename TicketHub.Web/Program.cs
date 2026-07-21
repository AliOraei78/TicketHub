using Microsoft.AspNetCore.Authentication.Cookies; // اضافه شود
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Repositories;
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
        options.LoginPath = "/login"; // مسیر ریدایرکت خودکار
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
// ----------------------------------------------

builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

var app = builder.Build();

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

app.Run();