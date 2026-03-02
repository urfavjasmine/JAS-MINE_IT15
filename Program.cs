using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Hubs;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog structured logging ──
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext());

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddAuthorization();

// Tenant Service for multi-tenant filtering
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Domain services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/Login";
    options.LogoutPath = "/Home/Logout";
});

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: 5 attempts per 1 minute per IP
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // API: 60 requests per minute per user
    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2
            }));
});

// ── Background Subscription Expiry Service ──
builder.Services.AddHostedService<SubscriptionExpiryService>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ── Security Headers Middleware ──
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self' wss: ws:;";
    await next();
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=LandingPage}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // ── Run EF Migrations (includes ConsolidateSchemaColumns) ──
        // All column-ensure DDL has been moved to Data/Migrations/20260302000000_ConsolidateSchemaColumns.cs
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("EF migrations applied successfully.");
        }
        catch (Exception migEx)
        {
            logger.LogWarning(migEx, "EF MigrateAsync had issues — columns may already exist.");
        }

        await IdentitySeeder.SeedRoles(services);
        await IdentitySeeder.SeedSuperAdmin(services);
        await IdentitySeeder.SeedDefaultUsers(services);
        await IdentitySeeder.SeedTestBarangayAndLinkUsers(db);
        await IdentitySeeder.SeedSubscriptionPlans(db);

        // ── Auto-expire subscriptions past EndDate ──
        var expiredCount = await db.Database.ExecuteSqlRawAsync(@"
            UPDATE dbo.BarangaySubscriptions
            SET Status = 'Expired', UpdatedAt = GETDATE()
            WHERE IsActive = 1
              AND Status = 'Active'
              AND EndDate < CAST(GETDATE() AS DATE)
        ");
        if (expiredCount > 0)
            logger.LogInformation("Auto-expired {Count} subscription(s) past EndDate.", expiredCount);

        // ── Auto-mark overdue invoices ──
        var overdueCount = await db.Database.ExecuteSqlRawAsync(@"
            UPDATE dbo.Invoices
            SET Status = 'Overdue', UpdatedAt = GETDATE()
            WHERE IsActive = 1
              AND Status = 'Unpaid'
              AND DueDate < CAST(GETDATE() AS DATE)
        ");
        if (overdueCount > 0)
            logger.LogInformation("Marked {Count} invoice(s) as Overdue.", overdueCount);
    }
    catch (Exception ex)
    {
        // ✅ Prevent 500.30 from killing the whole app
        logger.LogError(ex, "Startup migration/seed failed.");

        // OPTIONAL: Comment this out to allow the site to start even if DB is failing
        // throw;
    }
}

app.Run();
