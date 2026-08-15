using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using TicketHub.Infrastructure.Data;
using TicketHub.Web;
using Xunit;

namespace TicketHub.Tests.E2E
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly string _dbConnectionString;

        public CustomWebApplicationFactory()
        {
            _dbConnectionString = Environment.GetEnvironmentVariable("E2E_CONNECTION_STRING") 
                ?? "Server=127.0.0.1,1433;Database=TicketHubDb_Test;User Id=sa;Password=Ali433433_StrongPass!;TrustServerCertificate=True;MultipleActiveResultSets=true;";
            
            // Set environment variable so Program.cs (Hangfire, EF, MassTransit) reads the test connection string
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _dbConnectionString);
        }

        public string ServerAddress { get; private set; } = "http://127.0.0.1:0";
        private IHost _host = default!;

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Configure the actual Kestrel server
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls(ServerAddress);
                webHostBuilder.UseSetting("ConnectionStrings:DefaultConnection", _dbConnectionString);
            });

            _host = builder.Build();
            _host.Start();
            
            var server = _host.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
            var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            ServerAddress = addresses?.Addresses.FirstOrDefault() ?? ServerAddress;
            
            return _host;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<AppDbContext>));
                if (factoryDescriptor != null) services.Remove(factoryDescriptor);

                // Add DB Context pointing to Testcontainer
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    options.UseSqlServer(_dbConnectionString)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                // Mock Authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }

        public async Task InitializeAsync()
        {
            // (Docker disabled for local env)
            
            // Ensure DB is created and migrated
            using var scope = Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = factory.CreateDbContext();
            await context.Database.MigrateAsync();
        }

        new public async Task DisposeAsync()
        {
            // await _dbContainer.DisposeAsync();
            _host?.Dispose();
        }
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IServiceProvider serviceProvider)
            : base(options, logger, encoder)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Fetch the admin user from the database to get the correct UserId
            using var scope = _serviceProvider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = await factory.CreateDbContextAsync();
            var adminEmail = "admin@tickethub.io";
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail)
                         ?? await context.Users.FirstOrDefaultAsync();
            var userId = adminUser?.Id.ToString() ?? "1";
            var userEmail = adminUser?.Email ?? adminEmail;

            var claims = new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, adminUser?.Name ?? "AdminUser"),
                new Claim(ClaimTypes.Email, userEmail),
                new Claim(ClaimTypes.Role, "ادمین") 
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return AuthenticateResult.Success(ticket);
        }
    }
}
