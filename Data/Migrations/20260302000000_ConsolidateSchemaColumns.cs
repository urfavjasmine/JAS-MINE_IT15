using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Data.Migrations
{
    /// <summary>
    /// Consolidates all raw DDL column-ensure scripts from Program.cs into a proper EF migration.
    /// Uses IF NOT EXISTS for idempotency — safe to run multiple times.
    /// </summary>
    public partial class ConsolidateSchemaColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Policies ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Policies') AND name = 'IsArchived')
                    ALTER TABLE dbo.Policies ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // ── LessonsLearned ──
            migrationBuilder.Sql(@"
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
            ");

            // ── KnowledgeRepository ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeRepository') AND name = 'IsArchived')
                    ALTER TABLE dbo.KnowledgeRepository ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // ── KnowledgeDiscussions ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeDiscussions') AND name = 'IsArchived')
                    ALTER TABLE dbo.KnowledgeDiscussions ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // ── BestPractices ──
            migrationBuilder.Sql(@"
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
            ");

            // ── Announcements ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Announcements') AND name = 'IsArchived')
                    ALTER TABLE dbo.Announcements ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // ── KnowledgeRepository FileType expansion ──
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeRepository') AND name = 'FileType' AND max_length = 100)
                    ALTER TABLE dbo.KnowledgeRepository ALTER COLUMN FileType NVARCHAR(255) NULL;
            ");

            // ── Invoices table ──
            migrationBuilder.Sql(@"
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
            ");

            // ── SubscriptionPayments new columns ──
            migrationBuilder.Sql(@"
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
            ");

            // ── Expand SubscriptionPayments.Status ──
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPayments') AND name = 'Status' AND max_length < 60)
                    ALTER TABLE dbo.SubscriptionPayments ALTER COLUMN Status NVARCHAR(30) NOT NULL;
            ");

            // ── SubscriptionPlans UserLimit ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubscriptionPlans') AND name = 'UserLimit')
                    ALTER TABLE dbo.SubscriptionPlans ADD UserLimit INT NOT NULL DEFAULT 4;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration intentionally left empty — these columns are
            // safe to keep and removing them could break running systems.
        }
    }
}
