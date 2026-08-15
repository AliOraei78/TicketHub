using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using TicketHub.Infrastructure.Data;
using TicketHub.Web;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [CollectionDefinition("E2E Tests", DisableParallelization = true)]
    public class E2ETestCollection : ICollectionFixture<CustomWebApplicationFactory>
    {
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly string _dbConnectionString;
        private IHost? _kestrelHost;

        public CustomWebApplicationFactory()
        {
            _dbConnectionString = Environment.GetEnvironmentVariable("E2E_CONNECTION_STRING") 
                ?? "Server=127.0.0.1,1433;Database=TicketHubDb_Test;User Id=sa;Password=Password123!;TrustServerCertificate=True;MultipleActiveResultSets=true;";
            
            // Set environment variable so Program.cs reads test connection string and in-memory services
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _dbConnectionString);
            Environment.SetEnvironmentVariable("ConnectionStrings__Redis", ""); // Use DistributedMemoryCache
            Environment.SetEnvironmentVariable("RabbitMQ__Host", ""); // Use InMemory MassTransit
        }

        public string ServerAddress { get; private set; } = "http://127.0.0.1:0";

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // 1. Build test server for WebApplicationFactory internal management
            var testHost = builder.Build();

            // 2. Build and start the real Kestrel host on an ephemeral port for Playwright browser automation
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
                webHostBuilder.UseSetting("ConnectionStrings:DefaultConnection", _dbConnectionString);
                webHostBuilder.UseSetting("ConnectionStrings:Redis", "");
                webHostBuilder.UseSetting("RabbitMQ:Host", "");
            });

            _kestrelHost = builder.Build();
            _kestrelHost.Start();
            
            var server = _kestrelHost.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
            var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            ServerAddress = addresses?.Addresses.FirstOrDefault() ?? "http://127.0.0.1:5000";
            
            return testHost;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _dbConnectionString);
            builder.UseSetting("ConnectionStrings:Redis", "");
            builder.UseSetting("RabbitMQ:Host", "");

            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<AppDbContext>));
                if (factoryDescriptor != null) services.Remove(factoryDescriptor);

                // Add DB Context pointing to Test DB
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    options.UseSqlServer(_dbConnectionString)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                // Ensure DistributedMemoryCache is registered for in-memory caching
                var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
                if (cacheDescriptor != null) services.Remove(cacheDescriptor);
                services.AddDistributedMemoryCache();

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
            // Ensure server is started
            CreateDefaultClient();

            // Ensure DB is created and migrated
            using var scope = (_kestrelHost?.Services ?? Services).CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = factory.CreateDbContext();
            await context.Database.MigrateAsync();
        }

        new public async Task DisposeAsync()
        {
            if (_kestrelHost != null)
            {
                await _kestrelHost.StopAsync();
                _kestrelHost.Dispose();
                _kestrelHost = null;
            }
            base.Dispose();
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
