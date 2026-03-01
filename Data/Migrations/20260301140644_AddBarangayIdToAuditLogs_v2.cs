using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBarangayIdToAuditLogs_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add BarangayId to AuditLogs (safe - checks if not exists)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = 'BarangayId')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [BarangayId] int NULL;
                END
            ");

            // Add InvoiceId to SubscriptionPayments if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPayments]') AND name = 'InvoiceId')
                BEGIN
                    ALTER TABLE [SubscriptionPayments] ADD [InvoiceId] int NULL;
                END
            ");

            // Add ProcessedAt to SubscriptionPayments if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPayments]') AND name = 'ProcessedAt')
                BEGIN
                    ALTER TABLE [SubscriptionPayments] ADD [ProcessedAt] datetime2 NULL;
                END
            ");

            // Add ProofOfPaymentUrl to SubscriptionPayments if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPayments]') AND name = 'ProofOfPaymentUrl')
                BEGIN
                    ALTER TABLE [SubscriptionPayments] ADD [ProofOfPaymentUrl] nvarchar(500) NULL;
                END
            ");

            // Add RejectionReason to SubscriptionPayments if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPayments]') AND name = 'RejectionReason')
                BEGIN
                    ALTER TABLE [SubscriptionPayments] ADD [RejectionReason] nvarchar(500) NULL;
                END
            ");

            // Alter Status column length if needed
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPayments]') AND name = 'Status' AND max_length < 60)
                BEGIN
                    ALTER TABLE [SubscriptionPayments] ALTER COLUMN [Status] nvarchar(30) NOT NULL;
                END
            ");

            // Create Invoices table if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
                BEGIN
                    CREATE TABLE [Invoices] (
                        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [InvoiceNumber] nvarchar(50) NOT NULL,
                        [SubscriptionId] int NOT NULL,
                        [BarangayId] int NULL,
                        [Amount] decimal(10,2) NOT NULL,
                        [DueDate] date NULL,
                        [Status] nvarchar(20) NOT NULL DEFAULT 'Unpaid',
                        [IssuedAt] datetime2 NOT NULL,
                        [PaidAt] datetime2 NULL,
                        [Notes] nvarchar(500) NULL,
                        [IsActive] bit NOT NULL DEFAULT 1,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] datetime2 NULL
                    );
                    
                    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
                    CREATE INDEX [IX_Invoices_SubscriptionId] ON [Invoices] ([SubscriptionId]);
                    CREATE INDEX [IX_Invoices_BarangayId] ON [Invoices] ([BarangayId]);
                    
                    ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_BarangaySubscriptions_SubscriptionId] 
                        FOREIGN KEY ([SubscriptionId]) REFERENCES [BarangaySubscriptions] ([Id]);
                    
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Barangays')
                    BEGIN
                        ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_Barangays_BarangayId] 
                            FOREIGN KEY ([BarangayId]) REFERENCES [Barangays] ([Id]);
                    END
                END
            ");

            // Add foreign key constraint if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SubscriptionPayments_Invoices_InvoiceId')
                AND EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
                AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPayments]') AND name = 'InvoiceId')
                BEGIN
                    CREATE INDEX [IX_SubscriptionPayments_InvoiceId] ON [SubscriptionPayments] ([InvoiceId]);
                    ALTER TABLE [SubscriptionPayments] ADD CONSTRAINT [FK_SubscriptionPayments_Invoices_InvoiceId]
                        FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove BarangayId from AuditLogs
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = 'BarangayId')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [BarangayId];
                END
            ");

            // Note: Not removing other columns from SubscriptionPayments or Invoices table
            // as they may be needed by other parts of the system
        }
    }
}
