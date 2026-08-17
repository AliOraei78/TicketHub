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

        public CustomWebApplicationFactory()
        {
            _dbConnectionString = Environment.GetEnvironmentVariable("E2E_CONNECTION_STRING")
                ?? "Host=127.0.0.1;Port=5432;Database=TicketHubDb_Test;Username=postgres;Password=Password123!;";

            // Set environment variable so Program.cs reads test connection string and in-memory services
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _dbConnectionString);
            Environment.SetEnvironmentVariable("ConnectionStrings__Redis", ""); // Use DistributedMemoryCache
            Environment.SetEnvironmentVariable("RabbitMQ__Host", ""); // Use InMemory MassTransit
        }

        public string ServerAddress { get; private set; } = "http://127.0.0.1:0";

        private void EnsureDatabaseExists()
        {
            try
            {
                var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(_dbConnectionString);
                var targetDbName = connBuilder.Database;
                connBuilder.Database = "postgres";
                var masterConnStr = connBuilder.ConnectionString;

                for (int retry = 0; retry < 15; retry++)
                {
                    try
                    {
                        using var masterConn = new Npgsql.NpgsqlConnection(masterConnStr);
                        masterConn.Open();
                        using var cmd = masterConn.CreateCommand();
                        cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{targetDbName}'";
                        var exists = cmd.ExecuteScalar() != null;
                        if (!exists)
                        {
                            using var createCmd = masterConn.CreateCommand();
                            createCmd.CommandText = $"CREATE DATABASE \"{targetDbName}\"";
                            createCmd.ExecuteNonQuery();
                        }
                        break;
                    }
                    catch when (retry < 14)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            catch
            {
                // Fallback to let EF Core handle it
            }
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            EnsureDatabaseExists();

            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
                webHostBuilder.UseSetting("ConnectionStrings:DefaultConnection", _dbConnectionString);
                webHostBuilder.UseSetting("ConnectionStrings:Redis", "");
                webHostBuilder.UseSetting("RabbitMQ:Host", "");
                webHostBuilder.UseSetting("InitialAdmin:Email", "admin@tickethub.io");
                webHostBuilder.UseSetting("InitialAdmin:Password", "Admin@123456");
                webHostBuilder.UseSetting("InitialAdmin:Name", "مدیر کل سیستم");
            });

            var host = builder.Build();
            host.Start();

            var server = host.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
            var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            ServerAddress = addresses?.Addresses.FirstOrDefault() ?? "http://127.0.0.1:5000";

            return new KestrelHostWrapper(host);
        }

        private class KestrelHostWrapper : IHost
        {
            private readonly IHost _host;
            private readonly TestServer _dummyServer;

            public KestrelHostWrapper(IHost host)
            {
                _host = host;
#pragma warning disable ASPDEPR004, ASPDEPR008
                _dummyServer = new TestServer(new WebHostBuilder().Configure(app => { }));
#pragma warning restore ASPDEPR004, ASPDEPR008
                Services = new CustomServiceProvider(host.Services, _dummyServer);
            }

            public IServiceProvider Services { get; }

            public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => _host.StopAsync(cancellationToken);
            public void Dispose()
            {
                _dummyServer.Dispose();
                _host.Dispose();
            }
        }

        private class CustomServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _inner;
            private readonly TestServer _dummyServer;

            public CustomServiceProvider(IServiceProvider inner, TestServer dummyServer)
            {
                _inner = inner;
                _dummyServer = dummyServer;
            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(Microsoft.AspNetCore.Hosting.Server.IServer))
                {
                    return _dummyServer;
                }
                return _inner.GetService(serviceType);
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _dbConnectionString);
            builder.UseSetting("ConnectionStrings:Redis", "");
            builder.UseSetting("RabbitMQ:Host", "");
            builder.UseSetting("InitialAdmin:Email", "admin@tickethub.io");
            builder.UseSetting("InitialAdmin:Password", "Admin@123456");
            builder.UseSetting("InitialAdmin:Name", "مدیر کل سیستم");

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
                    options.UseNpgsql(_dbConnectionString)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)
                                                    .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
                });

                // Ensure DistributedMemoryCache is registered for in-memory caching
                var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
                if (cacheDescriptor != null) services.Remove(cacheDescriptor);
                services.AddDistributedMemoryCache();
            });
        }

        public async Task InitializeAsync()
        {
            EnsureDatabaseExists();

            // 2. Ensure server is started (which triggers Program.cs migration & Hangfire)
            CreateDefaultClient();

            // 3. Ensure DB is migrated and seeded
            using var scope = Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = factory.CreateDbContext();
            await context.Database.MigrateAsync();
            var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            await DbInitializer.InitializeAsync(context, configuration);
        }

        new public async Task DisposeAsync()
        {
            base.Dispose();
            await Task.CompletedTask;
        }
    }
}
