using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using worldRecipeMvc;
using worldRecipeMvc.Data;

namespace worldRecipeMvc.Tests.Integration
{
    /// <summary>
    /// Boots the real app against SQLite in-memory (relational constraints — unique
    /// indexes, FKs — are enforced, unlike EF InMemory). The normal startup path
    /// runs: EnsureCreated builds the schema, roles and sample data get seeded.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string DemoEmail = "demo@recipeworld.com";
        public const string DemoPassword = "Demo123!";
        public const string AdminEmail = "admin@recipeworld.com";
        public const string AdminPassword = "Admin123!";

        // Kept open for the factory's lifetime: an in-memory SQLite database
        // lives exactly as long as its last open connection.
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        private readonly Dictionary<string, string?> _configOverrides = new()
        {
            ["Jwt:Key"] = "integration-test-signing-key-0123456789abcdef",
            ["SeedData:DemoPassword"] = DemoPassword,
            ["SeedData:AdminPassword"] = AdminPassword,
            ["ImageUpload:StoragePath"] = Path.Combine(Path.GetTempPath(), "recipeworld-tests", Guid.NewGuid().ToString("N")),
            ["UseHttpsRedirection"] = "false",
            // Generous defaults so the suite never rate-limits itself;
            // rate-limit tests override via OverrideConfig in a subclass.
            ["RateLimiting:GlobalPermitLimit"] = "10000",
            ["RateLimiting:AuthPermitLimit"] = "10000",
            ["RateLimiting:WindowSeconds"] = "60"
        };

        /// <summary>Subclasses tighten or replace configuration before the host boots.</summary>
        protected void OverrideConfig(string key, string? value) => _configOverrides[key] = value;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // UseSetting flows into host configuration, which WebApplicationBuilder
            // exposes through builder.Configuration during Program.Main — unlike
            // ConfigureAppConfiguration, which minimal hosting applies too late.
            foreach (var (key, value) in _configOverrides)
            {
                builder.UseSetting(key, value);
            }

            _connection.Open();

            builder.ConfigureServices(services =>
            {
                // Strip every EF registration tied to the SQL Server provider,
                // then re-register the context on the shared SQLite connection.
                var efDescriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                || d.ServiceType == typeof(DbContextOptions)
                                || d.ServiceType == typeof(ApplicationDbContext)
                                || d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true)
                    .ToList();
                foreach (var descriptor in efDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseLazyLoadingProxies().UseSqlite(_connection));
            });
        }

        /// <summary>Creates a confirmed user with the "User" role (e.g. a non-owner for 403 tests).</summary>
        public async Task<RecipeWorldUser> CreateUserAsync(string email, string password)
        {
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<RecipeWorldUser>>();

            var user = new RecipeWorldUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create test user: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, Roles.User.ToString());
            return user;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    /// <summary>Factory with a tight auth rate limit (3/minute) for 429 tests.</summary>
    public class RateLimitedWebApplicationFactory : CustomWebApplicationFactory
    {
        public RateLimitedWebApplicationFactory()
        {
            OverrideConfig("RateLimiting:AuthPermitLimit", "3");
        }
    }
}
