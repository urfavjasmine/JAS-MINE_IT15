using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Filters;
using JAS_MINE_IT15.Hubs;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog structured logging ──
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext());

builder.Services.AddScoped<SanitizeInputFilter>();
builder.Services.AddScoped<ValidatePostModelFilter>();
builder.Services.AddScoped<CrudActionLoggingFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<SanitizeInputFilter>();
    options.Filters.AddService<ValidatePostModelFilter>();
    options.Filters.AddService<CrudActionLoggingFilter>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
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

// PayMongo configuration
builder.Services.Configure<JAS_MINE_IT15.Models.PayMongoSettings>(
    builder.Configuration.GetSection("PayMongo"));
builder.Services.Configure<JAS_MINE_IT15.Models.RecaptchaSettings>(
    builder.Configuration.GetSection("Recaptcha"));
builder.Services.AddHttpClient<IPayMongoService, PayMongoService>();
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

// DB
var connectionString = "Server=JASMINE\\SQLEXPRESS;Database=JAS_MINE_DB_New;Integrated Security=True;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.AddTransient<IEmailSender, MockEmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.LogoutPath = "/Home/Logout";
});

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "unknown",
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
app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");

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

        await db.Database.MigrateAsync();
        logger.LogInformation("EF migrations applied successfully.");

        await IdentitySeeder.SeedRoles(services);
        await IdentitySeeder.SeedSubscriptionPlans(db);

        var expiredSubs = await db.BarangaySubscriptions
            .Where(s => s.IsActive && s.Status == "Active" && s.EndDate < DateTime.Today)
            .ToListAsync();
        foreach (var sub in expiredSubs)
        {
            sub.Status = "Expired";
            sub.UpdatedAt = DateTime.Now;
        }
        var expiredCount = expiredSubs.Count;
        if (expiredCount > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Auto-expired {Count} subscription(s) past EndDate.", expiredCount);
        }

        var overdueInvoices = await db.Invoices
            .Where(i => i.IsActive && i.Status == "Unpaid" && i.DueDate.HasValue && i.DueDate.Value < DateTime.Today)
            .ToListAsync();
        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = "Overdue";
            invoice.UpdatedAt = DateTime.Now;
        }
        var overdueCount = overdueInvoices.Count;
        if (overdueCount > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Marked {Count} invoice(s) as Overdue.", overdueCount);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Startup migration/seed failed.");
    }
}

app.Run();