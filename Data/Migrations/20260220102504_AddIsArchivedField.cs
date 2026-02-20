using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop SharedDocuments table safely (if exists)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.SharedDocuments', 'U') IS NOT NULL
                    DROP TABLE dbo.SharedDocuments;
            ");

            // Add IsArchived to Policies
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Policies') AND name = 'IsArchived')
                    ALTER TABLE dbo.Policies ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // Add columns to LessonsLearned
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'ActionTaken')
                    ALTER TABLE dbo.LessonsLearned ADD ActionTaken NVARCHAR(MAX) NOT NULL DEFAULT '';
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'DateRecorded')
                    ALTER TABLE dbo.LessonsLearned ADD DateRecorded DATETIME2 NOT NULL DEFAULT '0001-01-01';
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'IsArchived')
                    ALTER TABLE dbo.LessonsLearned ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Problem')
                    ALTER TABLE dbo.LessonsLearned ADD Problem NVARCHAR(MAX) NOT NULL DEFAULT '';
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Recommendation')
                    ALTER TABLE dbo.LessonsLearned ADD Recommendation NVARCHAR(MAX) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Result')
                    ALTER TABLE dbo.LessonsLearned ADD Result NVARCHAR(MAX) NOT NULL DEFAULT '';
            ");

            // Add IsArchived to KnowledgeRepository
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeRepository') AND name = 'IsArchived')
                    ALTER TABLE dbo.KnowledgeRepository ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // Add IsArchived to KnowledgeDiscussions
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeDiscussions') AND name = 'IsArchived')
                    ALTER TABLE dbo.KnowledgeDiscussions ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            // Add columns to BestPractices
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'IsArchived')
                    ALTER TABLE dbo.BestPractices ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'OwnerOffice')
                    ALTER TABLE dbo.BestPractices ADD OwnerOffice NVARCHAR(200) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Purpose')
                    ALTER TABLE dbo.BestPractices ADD Purpose NVARCHAR(MAX) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'ResourcesNeeded')
                    ALTER TABLE dbo.BestPractices ADD ResourcesNeeded NVARCHAR(MAX) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Status')
                    ALTER TABLE dbo.BestPractices ADD Status NVARCHAR(20) NOT NULL DEFAULT '';
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Steps')
                    ALTER TABLE dbo.BestPractices ADD Steps NVARCHAR(MAX) NOT NULL DEFAULT '';
            ");

            // Add IsArchived to Announcements
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Announcements') AND name = 'IsArchived')
                    ALTER TABLE dbo.Announcements ADD IsArchived BIT NOT NULL DEFAULT 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration - drop columns if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Policies') AND name = 'IsArchived')
                    ALTER TABLE dbo.Policies DROP COLUMN IsArchived;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'ActionTaken')
                    ALTER TABLE dbo.LessonsLearned DROP COLUMN ActionTaken;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'DateRecorded')
                    ALTER TABLE dbo.LessonsLearned DROP COLUMN DateRecorded;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'IsArchived')
                    ALTER TABLE dbo.LessonsLearned DROP COLUMN IsArchived;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Problem')
                    ALTER TABLE dbo.LessonsLearned DROP COLUMN Problem;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Recommendation')
                    ALTER TABLE dbo.LessonsLearned DROP COLUMN Recommendation;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LessonsLearned') AND name = 'Result')
                    ALTER TABLE dbo.LessonsLearned DROP COLUMN Result;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeRepository') AND name = 'IsArchived')
                    ALTER TABLE dbo.KnowledgeRepository DROP COLUMN IsArchived;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KnowledgeDiscussions') AND name = 'IsArchived')
                    ALTER TABLE dbo.KnowledgeDiscussions DROP COLUMN IsArchived;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'IsArchived')
                    ALTER TABLE dbo.BestPractices DROP COLUMN IsArchived;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'OwnerOffice')
                    ALTER TABLE dbo.BestPractices DROP COLUMN OwnerOffice;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Purpose')
                    ALTER TABLE dbo.BestPractices DROP COLUMN Purpose;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'ResourcesNeeded')
                    ALTER TABLE dbo.BestPractices DROP COLUMN ResourcesNeeded;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Status')
                    ALTER TABLE dbo.BestPractices DROP COLUMN Status;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BestPractices') AND name = 'Steps')
                    ALTER TABLE dbo.BestPractices DROP COLUMN Steps;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Announcements') AND name = 'IsArchived')
                    ALTER TABLE dbo.Announcements DROP COLUMN IsArchived;
            ");
        }
    }
}
