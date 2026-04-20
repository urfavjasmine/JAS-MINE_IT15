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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

SecurityConfigurationValidator.ValidateOrThrow(builder.Configuration, builder.Environment);

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
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IAuthThrottleService, AuthThrottleService>();
builder.Services.AddScoped<ISecurityAlertService, SecurityAlertService>();
builder.Services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

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
builder.Services.Configure<JAS_MINE_IT15.Models.RetentionSettings>(
    builder.Configuration.GetSection("Retention"));
builder.Services.Configure<JAS_MINE_IT15.Models.SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<JAS_MINE_IT15.Models.AuditIntegritySettings>(
    builder.Configuration.GetSection(JAS_MINE_IT15.Models.AuditIntegritySettings.SectionName));
builder.Services.Configure<JAS_MINE_IT15.Models.FieldEncryptionSettings>(
    builder.Configuration.GetSection(JAS_MINE_IT15.Models.FieldEncryptionSettings.SectionName));
builder.Services.AddSingleton<IAuditLogHashService, AuditLogHashService>();
builder.Services.AddSingleton<IFieldEncryptionService, AesFieldEncryptionService>();
builder.Services.AddHttpClient<IPayMongoService, PayMongoService>();
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing. Configure it in appsettings, environment variables, or user secrets.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));
// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Strong password baseline (enforced by ASP.NET Core Identity)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;

    // Lock account on repeated failed login attempts
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(20);

    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddPasswordValidator<StrongPasswordValidator>()
.AddPasswordValidator<PasswordHistoryValidator>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
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
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global baseline: protect all /api routes, including future controllers.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            var partitionKey = context.User?.Identity?.Name
                               ?? context.Connection.RemoteIpAddress?.ToString()
                               ?? "api-anon";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetNoLimiter("non-api");
    });

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("forgot-password", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.AddPolicy("otp", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    options.AddPolicy("otp-resend", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
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
builder.Services.AddHostedService<DataRetentionCleanupService>();

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
    var nonce = CspUtilities.CreateNonce();
    context.Items["CspNonce"] = nonce;

    var originalBody = context.Response.Body;
    await using var responseBuffer = new MemoryStream();
    context.Response.Body = responseBuffer;
    try
    {
        await next();

        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        responseBuffer.Position = 0;
        var isHtmlResponse = (context.Response.ContentType ?? string.Empty)
            .Contains("text/html", StringComparison.OrdinalIgnoreCase);

        if (isHtmlResponse)
        {
            using var reader = new StreamReader(responseBuffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var html = await reader.ReadToEndAsync();

            html = CspUtilities.AddNonceToInlineTags(html, nonce);

            var scriptAttributeHashes = CspUtilities.ExtractAttributeHashes(html, "on[a-zA-Z]+");
            var styleAttributeHashes = CspUtilities.ExtractAttributeHashes(html, "style");

            context.Response.Headers["Content-Security-Policy"] =
                CspUtilities.BuildPolicy(nonce, scriptAttributeHashes, styleAttributeHashes);

            context.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
            await originalBody.WriteAsync(Encoding.UTF8.GetBytes(html));
            return;
        }

        context.Response.Headers["Content-Security-Policy"] =
            CspUtilities.BuildPolicy(nonce, Array.Empty<string>(), Array.Empty<string>());

        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalBody);
    }
    finally
    {
        context.Response.Body = originalBody;
    }
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
        await IdentitySeeder.SeedSuperAdmin(services);
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

internal static class CspUtilities
{
    private static readonly Regex InlineScriptTagRegex = new(
        @"<script(?![^>]*\bsrc=)(?![^>]*\bnonce=)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InlineStyleTagRegex = new(
        @"<style(?![^>]*\bnonce=)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string CreateNonce()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    }

    public static string AddNonceToInlineTags(string html, string nonce)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var withScriptNonces = InlineScriptTagRegex.Replace(html, $"<script nonce=\"{nonce}\"");
        return InlineStyleTagRegex.Replace(withScriptNonces, $"<style nonce=\"{nonce}\"");
    }

    public static IReadOnlyCollection<string> ExtractAttributeHashes(string html, string attributePattern)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<string>();
        }

        var attrRegex = new Regex(
            $@"\s(?:{attributePattern})\s*=\s*(?:""([^""]*)""|'([^']*)')",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var hashes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in attrRegex.Matches(html))
        {
            var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
            hashes.Add($"'sha256-{hash}'");
        }

        return hashes;
    }

    public static string BuildPolicy(
        string nonce,
        IReadOnlyCollection<string> scriptAttributeHashes,
        IReadOnlyCollection<string> styleAttributeHashes)
    {
        var scriptHashes = scriptAttributeHashes.Count > 0 ? " " + string.Join(" ", scriptAttributeHashes) : string.Empty;
        var styleHashes = styleAttributeHashes.Count > 0 ? " " + string.Join(" ", styleAttributeHashes) : string.Empty;

        var scriptAttrDirective = scriptAttributeHashes.Count > 0
            ? $"script-src-attr 'unsafe-hashes' {string.Join(" ", scriptAttributeHashes)}; "
            : "script-src-attr 'none'; ";

        var styleAttrDirective = styleAttributeHashes.Count > 0
            ? $"style-src-attr 'unsafe-hashes' {string.Join(" ", styleAttributeHashes)}; "
            : "style-src-attr 'none'; ";

        return
            "default-src 'self'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'; " +
            "object-src 'none'; " +
            "script-src 'self' 'nonce-" + nonce + "' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://www.google.com https://www.gstatic.com 'unsafe-hashes'" + scriptHashes + "; " +
            "script-src-elem 'self' 'nonce-" + nonce + "' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://www.google.com https://www.gstatic.com; " +
            scriptAttrDirective +
            "style-src 'self' 'nonce-" + nonce + "' https://cdn.jsdelivr.net https://fonts.googleapis.com 'unsafe-hashes'" + styleHashes + "; " +
            styleAttrDirective +
            "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
            "img-src 'self' data: blob:; " +
            "frame-src 'self' https://www.google.com https://recaptcha.google.com; " +
            "connect-src 'self' ws: wss:;";
    }
}