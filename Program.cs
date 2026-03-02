using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Hubs;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddAuthorization();

// Tenant Service for multi-tenant filtering
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

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

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

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

        // ── Ensure all required columns exist (safe idempotent ALTER TABLE) ──
        // This runs BEFORE MigrateAsync so the app works even if EF migrations
        // can't apply (e.g. deployed DB created from raw SQL schema).
        var ensureColumnsSql = @"
            -- Policies
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Policies') AND name = 'IsArchived')
                ALTER TABLE dbo.Policies ADD IsArchived BIT NOT NULL DEFAULT 0;

            -- LessonsLearned
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Problem')
                ALTER TABLE dbo.LessonsLearned ADD Problem NVARCHAR(MAX) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'ActionTaken')
                ALTER TABLE dbo.LessonsLearned ADD ActionTaken NVARCHAR(MAX) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Result')
                ALTER TABLE dbo.LessonsLearned ADD Result NVARCHAR(MAX) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Recommendation')
                ALTER TABLE dbo.LessonsLearned ADD Recommendation NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'DateRecorded')
                ALTER TABLE dbo.LessonsLearned ADD DateRecorded DATETIME2 NOT NULL DEFAULT '0001-01-01';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'IsArchived')
                ALTER TABLE dbo.LessonsLearned ADD IsArchived BIT NOT NULL DEFAULT 0;

            -- KnowledgeRepository
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeRepository') AND name = 'IsArchived')
                ALTER TABLE dbo.KnowledgeRepository ADD IsArchived BIT NOT NULL DEFAULT 0;

            -- KnowledgeDiscussions
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeDiscussions') AND name = 'IsArchived')
                ALTER TABLE dbo.KnowledgeDiscussions ADD IsArchived BIT NOT NULL DEFAULT 0;

            -- BestPractices
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'IsArchived')
                ALTER TABLE dbo.BestPractices ADD IsArchived BIT NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'OwnerOffice')
                ALTER TABLE dbo.BestPractices ADD OwnerOffice NVARCHAR(200) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Purpose')
                ALTER TABLE dbo.BestPractices ADD Purpose NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'ResourcesNeeded')
                ALTER TABLE dbo.BestPractices ADD ResourcesNeeded NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Status')
                ALTER TABLE dbo.BestPractices ADD Status NVARCHAR(20) NOT NULL DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Steps')
                ALTER TABLE dbo.BestPractices ADD Steps NVARCHAR(MAX) NOT NULL DEFAULT '';

            -- Announcements
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Announcements') AND name = 'IsArchived')
                ALTER TABLE dbo.Announcements ADD IsArchived BIT NOT NULL DEFAULT 0;

            -- KnowledgeRepository FileType expansion
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeRepository') AND name = 'FileType' AND max_length = 100)
                ALTER TABLE dbo.KnowledgeRepository ALTER COLUMN FileType NVARCHAR(255) NULL;

            -- Invoices table
            IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('dbo.Invoices') AND type = 'U')
            BEGIN
                CREATE TABLE dbo.Invoices (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    InvoiceNumber NVARCHAR(50) NOT NULL,
                    SubscriptionId INT NOT NULL,
                    BarangayId INT NULL,
                    Amount DECIMAL(10,2) NOT NULL DEFAULT 0,
                    DueDate DATE NULL,
                    Status NVARCHAR(20) NOT NULL DEFAULT 'Unpaid',
                    IssuedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    PaidAt DATETIME2 NULL,
                    Notes NVARCHAR(500) NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                    UpdatedAt DATETIME2 NULL,
                    CONSTRAINT FK_Invoices_Subscription FOREIGN KEY (SubscriptionId) REFERENCES dbo.BarangaySubscriptions(Id),
                    CONSTRAINT FK_Invoices_Barangay FOREIGN KEY (BarangayId) REFERENCES dbo.Barangays(Id)
                );
                CREATE UNIQUE INDEX IX_Invoices_InvoiceNumber ON dbo.Invoices(InvoiceNumber);
            END

            -- SubscriptionPayments new columns
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'InvoiceId')
                ALTER TABLE dbo.SubscriptionPayments ADD InvoiceId INT NULL CONSTRAINT FK_SubscriptionPayments_Invoice FOREIGN KEY REFERENCES dbo.Invoices(Id);

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'ProofOfPaymentUrl')
                ALTER TABLE dbo.SubscriptionPayments ADD ProofOfPaymentUrl NVARCHAR(500) NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'RejectionReason')
                ALTER TABLE dbo.SubscriptionPayments ADD RejectionReason NVARCHAR(500) NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'ProcessedAt')
                ALTER TABLE dbo.SubscriptionPayments ADD ProcessedAt DATETIME2 NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'ProcessedById')
                ALTER TABLE dbo.SubscriptionPayments ADD ProcessedById INT NULL;

            -- Expand SubscriptionPayments.Status from 20 to 30 if needed
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'Status' AND max_length < 60)
                ALTER TABLE dbo.SubscriptionPayments ALTER COLUMN Status NVARCHAR(30) NOT NULL;

            -- SubscriptionPlans UserLimit column
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPlans') AND name = 'UserLimit')
                ALTER TABLE dbo.SubscriptionPlans ADD UserLimit INT NOT NULL DEFAULT 4;
        ";
        await db.Database.ExecuteSqlRawAsync(ensureColumnsSql);
        logger.LogInformation("Ensured all required database columns exist.");

        // Now run EF migrations (may no-op if columns already exist)
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception migEx)
        {
            logger.LogWarning(migEx, "EF MigrateAsync had issues (columns already ensured above).");
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
