using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using worldRecipeMvc.Data;
using worldRecipeMvc.Middleware;
using worldRecipeMvc.Services;
using System.Text;
using System.Threading.RateLimiting;

namespace worldRecipeMvc;
public class Program
{

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Structured logging via Serilog, configured from the "Serilog" section
        builder.Host.UseSerilog((context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration));

        // Add services to the container.
        // Database:Provider = "SqlServer" (default) or "Sqlite". SQLite enables
        // single-container deployments (e.g. Render free tier) with no database
        // service; the schema comes from EnsureCreated instead of migrations.
        var databaseProvider = builder.Configuration.GetValue("Database:Provider", "SqlServer");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseLazyLoadingProxies();

            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var sqlitePath = builder.Configuration.GetValue("Database:SqlitePath", "data/recipeworld.db")!;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sqlitePath))!);
                options.UseSqlite($"Data Source={sqlitePath}");
            }
            else
            {
                var connectionString = builder.Configuration.GetConnectionString("RecipeWorldConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'RecipeWorldConnectionString' not found.");
                options.UseSqlServer(connectionString, sql =>
                    sql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(6), errorNumbersToAdd: null));
            }
        });
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<RecipeWorldUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // JWT Bearer for the REST API. Deliberately does NOT set default schemes:
        // the Identity cookie remains the default for MVC, and API controllers
        // opt in via [ApiAuthorize] (JwtBearerDefaults.AuthenticationScheme).
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (!builder.Environment.IsDevelopment() && jwtOptions.Key.Length < JwtOptions.MinKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be configured with at least {JwtOptions.MinKeyLength} characters outside Development (e.g. via the Jwt__Key environment variable).");
        }
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        jwtOptions.Key.PadRight(JwtOptions.MinKeyLength))),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        // Rate limiting: a per-IP window on /api plus a stricter "auth" policy for
        // the token endpoint. Page and static-file traffic is deliberately exempt —
        // a single browser page load fires dozens of asset requests, so metering it
        // just breaks navigation. Limits are configurable so tests can relax them.
        var rateLimiting = builder.Configuration.GetSection("RateLimiting");
        int globalPermitLimit = rateLimiting.GetValue("GlobalPermitLimit", 100);
        int authPermitLimit = rateLimiting.GetValue("AuthPermitLimit", 5);
        int windowSeconds = rateLimiting.GetValue("WindowSeconds", 60);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!httpContext.Request.Path.StartsWithSegments("/api"))
                {
                    return RateLimitPartition.GetNoLimiter("web");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    });
            });

            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = authPermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
                limiterOptions.QueueLimit = 0;
            });
        });

        // Register services
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IRecipeService, RecipeService>();
        builder.Services.AddScoped<IIngredientService, IngredientService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IFavoriteService, FavoriteService>();
        builder.Services.AddScoped<IRatingService, RatingService>();
        builder.Services.AddScoped<IUserAdminService, UserAdminService>();

        builder.Services.AddControllersWithViews();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        // Add Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Recipe World API",
                Version = "v1",
                Description = "API for Recipe World application",
                Contact = new OpenApiContact
                {
                    Name = "Recipe World",
                    Email = "support@recipeworld.com"
                }
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Paste the JWT from POST /api/auth/login (no 'Bearer ' prefix needed)",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            try
            {
                var dbContext = services.GetRequiredService<ApplicationDbContext>();

                // Apply pending migrations (SQL Server only; the migrations contain
                // SQL Server-specific DDL). Other providers — e.g. SQLite in the
                // integration tests — create the schema from the model instead.
                if (dbContext.Database.IsSqlServer())
                {
                    await dbContext.Database.MigrateAsync();
                }
                else
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }

                var userManager = services.GetRequiredService<UserManager<RecipeWorldUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                await ContextSeed.SeedRolesAsync(userManager, roleManager);

                // Seed sample data (recipes, ingredients, categories)
                await SeedData.Initialize(services, userManager);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "An error occurred migrating or seeding the DB.");

                // Fail fast outside Development: a booted app without a working
                // database would 500 on every request anyway. The container's
                // restart policy retries until the database is reachable.
                if (!app.Environment.IsDevelopment())
                {
                    throw;
                }
            }
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // Swagger UI stays available in all environments as live API documentation
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe World API V1");
            c.RoutePrefix = "swagger";
        });

        // Add error handling middleware (JSON ProblemDetails for /api, rethrows for MVC)
        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.UseSerilogRequestLogging();

        // Disabled in containers where no HTTPS endpoint is bound (see compose.yaml)
        if (builder.Configuration.GetValue("UseHttpsRedirection", true))
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();

        // Serve uploaded recipe images from the configured storage folder
        var uploadPath = builder.Configuration["ImageUpload:StoragePath"];
        if (!string.IsNullOrWhiteSpace(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadPath),
                RequestPath = "/images/recipes"
            });
        }

        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();
        app.MapHealthChecks("/health");

        app.Run();
    }

}

