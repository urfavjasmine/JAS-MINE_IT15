/*
=============================================================================
  JAS-MINE Knowledge Management System - SQL Server Database Schema
  Database: JAS_MINE_DB
  
  Description: ERP-based Knowledge Management System for Barangays
  
  Execute this script in SQL Server Management Studio (SSMS)
  Compatible with SQL Server 2016+
=============================================================================
*/

-- ============================================
-- Step 1: Create Database
-- ============================================
USE [master];
GO

-- Drop database if exists (comment out in production)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'JAS_MINE_DB')
BEGIN
    ALTER DATABASE [JAS_MINE_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [JAS_MINE_DB];
END
GO

CREATE DATABASE [JAS_MINE_DB];
GO

USE [JAS_MINE_DB];
GO

-- ============================================
-- Step 2: Create Users Table
-- ============================================
CREATE TABLE [dbo].[Users] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Email]           NVARCHAR(255)     NOT NULL,
    [PasswordHash]    NVARCHAR(512)     NOT NULL,
    [FullName]        NVARCHAR(150)     NOT NULL,
    [Role]            NVARCHAR(50)      NOT NULL 
                      CONSTRAINT CK_Users_Role CHECK ([Role] IN (
                          'super_admin', 
                          'barangay_admin', 
                          'barangay_secretary', 
                          'barangay_staff', 
                          'council_member'
                      )),
    [BarangayId]      INT               NULL,
    [BarangayName]    NVARCHAR(150)     NULL,
    [PhoneNumber]     NVARCHAR(20)      NULL,
    [ProfileImageUrl] NVARCHAR(500)     NULL,
    [LastLoginAt]     DATETIME2         NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,
    [CreatedBy]       INT               NULL,
    [UpdatedBy]       INT               NULL,

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT UQ_Users_Email UNIQUE ([Email])
);
GO

-- Indexes for Users table
CREATE NONCLUSTERED INDEX IX_Users_Email ON [dbo].[Users] ([Email]) INCLUDE ([FullName], [Role], [IsActive]);
CREATE NONCLUSTERED INDEX IX_Users_Role ON [dbo].[Users] ([Role]) INCLUDE ([Email], [FullName], [IsActive]);
CREATE NONCLUSTERED INDEX IX_Users_BarangayId ON [dbo].[Users] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Users_IsActive ON [dbo].[Users] ([IsActive]) INCLUDE ([Email], [Role]);
GO

-- ============================================
-- Step 3: Create KnowledgeRepository Table
-- ============================================
CREATE TABLE [dbo].[KnowledgeRepository] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Description]     NVARCHAR(MAX)     NULL,
    [Category]        NVARCHAR(100)     NOT NULL,
    [Tags]            NVARCHAR(500)     NULL,
    [FileUrl]         NVARCHAR(500)     NULL,
    [FileName]        NVARCHAR(255)     NULL,
    [FileSize]        BIGINT            NULL,
    [FileType]        NVARCHAR(50)      NULL,
    [Status]          NVARCHAR(30)      NOT NULL CONSTRAINT DF_KnowledgeRepository_Status DEFAULT ('pending')
                      CONSTRAINT CK_KnowledgeRepository_Status CHECK ([Status] IN ('draft', 'pending', 'approved', 'rejected')),
    [Version]         NVARCHAR(20)      NOT NULL CONSTRAINT DF_KnowledgeRepository_Version DEFAULT ('1.0'),
    [UploadedById]    INT               NOT NULL,
    [ApprovedById]    INT               NULL,
    [ApprovedAt]      DATETIME2         NULL,
    [BarangayId]      INT               NULL,
    [ViewCount]       INT               NOT NULL CONSTRAINT DF_KnowledgeRepository_ViewCount DEFAULT (0),
    [DownloadCount]   INT               NOT NULL CONSTRAINT DF_KnowledgeRepository_DownloadCount DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_KnowledgeRepository_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_KnowledgeRepository_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_KnowledgeRepository PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_KnowledgeRepository_UploadedBy FOREIGN KEY ([UploadedById]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_KnowledgeRepository_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes for KnowledgeRepository table
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_Category ON [dbo].[KnowledgeRepository] ([Category]) INCLUDE ([Title], [Status]);
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_Status ON [dbo].[KnowledgeRepository] ([Status]) INCLUDE ([Title], [Category]);
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_UploadedById ON [dbo].[KnowledgeRepository] ([UploadedById]);
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_BarangayId ON [dbo].[KnowledgeRepository] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_IsActive ON [dbo].[KnowledgeRepository] ([IsActive]) INCLUDE ([Title], [Status]);
GO

-- ============================================
-- Step 4: Create Policies Table
-- ============================================
CREATE TABLE [dbo].[Policies] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Description]     NVARCHAR(MAX)     NULL,
    [Content]         NVARCHAR(MAX)     NULL,
    [Category]        NVARCHAR(100)     NULL,
    [Status]          NVARCHAR(30)      NOT NULL CONSTRAINT DF_Policies_Status DEFAULT ('draft')
                      CONSTRAINT CK_Policies_Status CHECK ([Status] IN ('draft', 'pending', 'approved', 'rejected', 'archived')),
    [Version]         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Policies_Version DEFAULT ('1.0'),
    [EffectiveDate]   DATE              NULL,
    [ExpiryDate]      DATE              NULL,
    [AuthorId]        INT               NOT NULL,
    [ApprovedById]    INT               NULL,
    [ApprovedAt]      DATETIME2         NULL,
    [BarangayId]      INT               NULL,
    [AttachmentUrl]   NVARCHAR(500)     NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Policies_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Policies_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_Policies PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Policies_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_Policies_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes for Policies table
CREATE NONCLUSTERED INDEX IX_Policies_Status ON [dbo].[Policies] ([Status]) INCLUDE ([Title], [Category]);
CREATE NONCLUSTERED INDEX IX_Policies_AuthorId ON [dbo].[Policies] ([AuthorId]);
CREATE NONCLUSTERED INDEX IX_Policies_BarangayId ON [dbo].[Policies] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Policies_IsActive ON [dbo].[Policies] ([IsActive]) INCLUDE ([Title], [Status]);
GO

-- ============================================
-- Step 5: Create LessonsLearned Table
-- ============================================
CREATE TABLE [dbo].[LessonsLearned] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Summary]         NVARCHAR(MAX)     NOT NULL,
    [ProjectName]     NVARCHAR(200)     NULL,
    [ProjectType]     NVARCHAR(100)     NULL,
    [Tags]            NVARCHAR(500)     NULL,
    [Status]          NVARCHAR(30)      NOT NULL CONSTRAINT DF_LessonsLearned_Status DEFAULT ('pending')
                      CONSTRAINT CK_LessonsLearned_Status CHECK ([Status] IN ('draft', 'pending', 'approved', 'rejected')),
    [SubmittedById]   INT               NOT NULL,
    [ApprovedById]    INT               NULL,
    [ApprovedAt]      DATETIME2         NULL,
    [BarangayId]      INT               NULL,
    [LikesCount]      INT               NOT NULL CONSTRAINT DF_LessonsLearned_LikesCount DEFAULT (0),
    [CommentsCount]   INT               NOT NULL CONSTRAINT DF_LessonsLearned_CommentsCount DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_LessonsLearned_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_LessonsLearned_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_LessonsLearned PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_LessonsLearned_SubmittedBy FOREIGN KEY ([SubmittedById]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_LessonsLearned_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes for LessonsLearned table
CREATE NONCLUSTERED INDEX IX_LessonsLearned_ProjectType ON [dbo].[LessonsLearned] ([ProjectType]) INCLUDE ([Title], [Status]);
CREATE NONCLUSTERED INDEX IX_LessonsLearned_Status ON [dbo].[LessonsLearned] ([Status]) INCLUDE ([Title], [ProjectType]);
CREATE NONCLUSTERED INDEX IX_LessonsLearned_SubmittedById ON [dbo].[LessonsLearned] ([SubmittedById]);
CREATE NONCLUSTERED INDEX IX_LessonsLearned_BarangayId ON [dbo].[LessonsLearned] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_LessonsLearned_IsActive ON [dbo].[LessonsLearned] ([IsActive]) INCLUDE ([Title], [Status]);
GO

-- ============================================
-- Step 6: Create BestPractices Table
-- ============================================
CREATE TABLE [dbo].[BestPractices] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Description]     NVARCHAR(MAX)     NOT NULL,
    [Category]        NVARCHAR(100)     NOT NULL,
    [BarangayId]      INT               NULL,
    [BarangayName]    NVARCHAR(150)     NULL,
    [Rating]          DECIMAL(3,2)      NOT NULL CONSTRAINT DF_BestPractices_Rating DEFAULT (0.00),
    [Implementations] INT               NOT NULL CONSTRAINT DF_BestPractices_Implementations DEFAULT (0),
    [IsFeatured]      BIT               NOT NULL CONSTRAINT DF_BestPractices_IsFeatured DEFAULT (0),
    [AttachmentUrl]   NVARCHAR(500)     NULL,
    [SubmittedById]   INT               NOT NULL,
    [ApprovedById]    INT               NULL,
    [ApprovedAt]      DATETIME2         NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_BestPractices_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_BestPractices_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_BestPractices PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_BestPractices_SubmittedBy FOREIGN KEY ([SubmittedById]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_BestPractices_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes for BestPractices table
CREATE NONCLUSTERED INDEX IX_BestPractices_Category ON [dbo].[BestPractices] ([Category]) INCLUDE ([Title], [Rating]);
CREATE NONCLUSTERED INDEX IX_BestPractices_IsFeatured ON [dbo].[BestPractices] ([IsFeatured]) WHERE [IsFeatured] = 1;
CREATE NONCLUSTERED INDEX IX_BestPractices_BarangayId ON [dbo].[BestPractices] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_BestPractices_IsActive ON [dbo].[BestPractices] ([IsActive]) INCLUDE ([Title], [Category]);
GO

-- ============================================
-- Step 7: Create Announcements Table
-- ============================================
CREATE TABLE [dbo].[Announcements] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Content]         NVARCHAR(MAX)     NOT NULL,
    [Priority]        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Announcements_Priority DEFAULT ('medium')
                      CONSTRAINT CK_Announcements_Priority CHECK ([Priority] IN ('low', 'medium', 'high')),
    [Status]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_Announcements_Status DEFAULT ('draft')
                      CONSTRAINT CK_Announcements_Status CHECK ([Status] IN ('draft', 'published', 'archived')),
    [IsPinned]        BIT               NOT NULL CONSTRAINT DF_Announcements_IsPinned DEFAULT (0),
    [PublishedAt]     DATETIME2         NULL,
    [ExpiresAt]       DATETIME2         NULL,
    [AuthorId]        INT               NOT NULL,
    [BarangayId]      INT               NULL,
    [TargetAudience]  NVARCHAR(100)     NULL, -- 'all', 'barangay_admins', 'staff', etc.
    [ViewCount]       INT               NOT NULL CONSTRAINT DF_Announcements_ViewCount DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Announcements_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Announcements_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_Announcements PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Announcements_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes for Announcements table
CREATE NONCLUSTERED INDEX IX_Announcements_Status ON [dbo].[Announcements] ([Status]) INCLUDE ([Title], [Priority], [IsPinned]);
CREATE NONCLUSTERED INDEX IX_Announcements_IsPinned ON [dbo].[Announcements] ([IsPinned]) WHERE [IsPinned] = 1;
CREATE NONCLUSTERED INDEX IX_Announcements_AuthorId ON [dbo].[Announcements] ([AuthorId]);
CREATE NONCLUSTERED INDEX IX_Announcements_BarangayId ON [dbo].[Announcements] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Announcements_IsActive ON [dbo].[Announcements] ([IsActive]) INCLUDE ([Title], [Status]);
CREATE NONCLUSTERED INDEX IX_Announcements_PublishedAt ON [dbo].[Announcements] ([PublishedAt] DESC) WHERE [Status] = 'published';
GO

-- ============================================
-- Step 8: Create AuditLogs Table
-- ============================================
CREATE TABLE [dbo].[AuditLogs] (
    [Id]              BIGINT IDENTITY(1,1) NOT NULL,
    [UserId]          INT               NULL,
    [UserEmail]       NVARCHAR(255)     NULL,
    [UserName]        NVARCHAR(150)     NULL,
    [Action]          NVARCHAR(50)      NOT NULL, -- 'Created', 'Updated', 'Deleted', 'Approved', 'Rejected', 'Login', 'Logout', etc.
    [Module]          NVARCHAR(100)     NOT NULL, -- 'KnowledgeRepository', 'Policies', 'Users', etc.
    [TargetId]        INT               NULL,
    [TargetType]      NVARCHAR(100)     NULL,
    [TargetName]      NVARCHAR(300)     NULL,
    [Description]     NVARCHAR(MAX)     NULL,
    [OldValues]       NVARCHAR(MAX)     NULL, -- JSON of previous values
    [NewValues]       NVARCHAR(MAX)     NULL, -- JSON of new values
    [IpAddress]       NVARCHAR(45)      NULL,
    [UserAgent]       NVARCHAR(500)     NULL,
    [SessionId]       NVARCHAR(100)     NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_AuditLogs_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (GETDATE()),

    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes for AuditLogs table (optimized for querying)
CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON [dbo].[AuditLogs] ([UserId]) INCLUDE ([Action], [Module], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Action ON [dbo].[AuditLogs] ([Action]) INCLUDE ([Module], [UserEmail], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Module ON [dbo].[AuditLogs] ([Module]) INCLUDE ([Action], [UserEmail], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt ON [dbo].[AuditLogs] ([CreatedAt] DESC) INCLUDE ([Action], [Module], [UserEmail]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_IsActive ON [dbo].[AuditLogs] ([IsActive]) WHERE [IsActive] = 1;
GO

-- ============================================
-- Step 9: Create Barangays Table (Reference)
-- ============================================
CREATE TABLE [dbo].[Barangays] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Name]            NVARCHAR(150)     NOT NULL,
    [Code]            NVARCHAR(20)      NULL,
    [Municipality]    NVARCHAR(100)     NULL,
    [Province]        NVARCHAR(100)     NULL,
    [Region]          NVARCHAR(100)     NULL,
    [ContactEmail]    NVARCHAR(255)     NULL,
    [ContactPhone]    NVARCHAR(20)      NULL,
    [Address]         NVARCHAR(500)     NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Barangays_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Barangays_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_Barangays PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT UQ_Barangays_Name UNIQUE ([Name])
);
GO

-- ============================================
-- Step 10: Create SubscriptionPlans Table
-- ============================================
CREATE TABLE [dbo].[SubscriptionPlans] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Name]            NVARCHAR(100)     NOT NULL,
    [Description]     NVARCHAR(500)     NULL,
    [Price]           DECIMAL(10,2)     NOT NULL CONSTRAINT DF_SubscriptionPlans_Price DEFAULT (0.00),
    [DurationMonths]  INT               NOT NULL CONSTRAINT DF_SubscriptionPlans_Duration DEFAULT (12),
    [Features]        NVARCHAR(MAX)     NULL, -- JSON array of features
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_SubscriptionPlans_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_SubscriptionPlans_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_SubscriptionPlans PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- ============================================
-- Step 11: Create BarangaySubscriptions Table
-- ============================================
CREATE TABLE [dbo].[BarangaySubscriptions] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [BarangayId]      INT               NOT NULL,
    [PlanId]          INT               NOT NULL,
    [StartDate]       DATE              NOT NULL,
    [EndDate]         DATE              NOT NULL,
    [Status]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_BarangaySubscriptions_Status DEFAULT ('Active')
                      CONSTRAINT CK_BarangaySubscriptions_Status CHECK ([Status] IN ('Active', 'Expired', 'Cancelled', 'Pending')),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_BarangaySubscriptions_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_BarangaySubscriptions_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_BarangaySubscriptions PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_BarangaySubscriptions_Barangay FOREIGN KEY ([BarangayId]) REFERENCES [dbo].[Barangays]([Id]),
    CONSTRAINT FK_BarangaySubscriptions_Plan FOREIGN KEY ([PlanId]) REFERENCES [dbo].[SubscriptionPlans]([Id])
);
GO

-- ============================================
-- Step 12: Create SubscriptionPayments Table
-- ============================================
CREATE TABLE [dbo].[SubscriptionPayments] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [SubscriptionId]  INT               NOT NULL,
    [Amount]          DECIMAL(10,2)     NOT NULL,
    [PaymentDate]     DATE              NOT NULL,
    [PaymentMethod]   NVARCHAR(50)      NULL, -- 'GCash', 'Bank Transfer', 'Cash', etc.
    [ReferenceNumber] NVARCHAR(100)     NULL,
    [Status]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_SubscriptionPayments_Status DEFAULT ('Pending')
                      CONSTRAINT CK_SubscriptionPayments_Status CHECK ([Status] IN ('Pending', 'Paid', 'Failed', 'Refunded')),
    [Notes]           NVARCHAR(500)     NULL,
    [ProcessedById]   INT               NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_SubscriptionPayments_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_SubscriptionPayments_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_SubscriptionPayments PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SubscriptionPayments_Subscription FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[BarangaySubscriptions]([Id]),
    CONSTRAINT FK_SubscriptionPayments_ProcessedBy FOREIGN KEY ([ProcessedById]) REFERENCES [dbo].[Users]([Id])
);
GO

-- ============================================
-- Step 13: Create KnowledgeDiscussions Table
-- ============================================
CREATE TABLE [dbo].[KnowledgeDiscussions] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Content]         NVARCHAR(MAX)     NOT NULL,
    [Category]        NVARCHAR(100)     NULL,
    [AuthorId]        INT               NOT NULL,
    [BarangayId]      INT               NULL,
    [LikesCount]      INT               NOT NULL CONSTRAINT DF_KnowledgeDiscussions_LikesCount DEFAULT (0),
    [RepliesCount]    INT               NOT NULL CONSTRAINT DF_KnowledgeDiscussions_RepliesCount DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_KnowledgeDiscussions_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_KnowledgeDiscussions_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_KnowledgeDiscussions PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_KnowledgeDiscussions_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- ============================================
-- Step 14: Create PasswordResetRequests Table
-- ============================================
CREATE TABLE [dbo].[PasswordResetRequests] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [UserId]          INT               NULL,
    [Email]           NVARCHAR(255)     NOT NULL,
    [Token]           NVARCHAR(256)     NULL,
    [Status]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_PasswordResetRequests_Status DEFAULT ('Pending')
                      CONSTRAINT CK_PasswordResetRequests_Status CHECK ([Status] IN ('Pending', 'Approved', 'Completed', 'Rejected', 'Expired')),
    [Notes]           NVARCHAR(500)     NULL,
    [ProcessedById]   INT               NULL,
    [ProcessedAt]     DATETIME2         NULL,
    [ExpiresAt]       DATETIME2         NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_PasswordResetRequests_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_PasswordResetRequests_CreatedAt DEFAULT (GETDATE()),

    CONSTRAINT PK_PasswordResetRequests PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_PasswordResetRequests_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_PasswordResetRequests_ProcessedBy FOREIGN KEY ([ProcessedById]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_PasswordResetRequests_Email ON [dbo].[PasswordResetRequests] ([Email]);
CREATE NONCLUSTERED INDEX IX_PasswordResetRequests_Status ON [dbo].[PasswordResetRequests] ([Status]);
GO

-- ============================================
-- Step 15: Create SharedDocuments Table
-- ============================================
CREATE TABLE [dbo].[SharedDocuments] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [FileUrl]         NVARCHAR(500)     NULL,
    [FileName]        NVARCHAR(255)     NULL,
    [SharedById]      INT               NOT NULL,
    [DownloadCount]   INT               NOT NULL CONSTRAINT DF_SharedDocuments_DownloadCount DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_SharedDocuments_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_SharedDocuments_CreatedAt DEFAULT (GETDATE()),

    CONSTRAINT PK_SharedDocuments PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SharedDocuments_SharedBy FOREIGN KEY ([SharedById]) REFERENCES [dbo].[Users]([Id])
);
GO

-- ============================================
-- Step 16: Insert Default Super Admin User
-- ============================================
-- Note: Password is hashed version of 'Admin@123' (use proper hashing in production)
INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [FullName], [Role], [BarangayName])
VALUES (
    'admin@jasmine.gov.ph', 
    'AQAAAAIAAYagAAAAEA5M8VkF5d4nN5LZ2uHR7Q==', -- Placeholder hash, replace with actual hashed password
    'System Administrator',
    'super_admin',
    NULL
);
GO

-- ============================================
-- Step 17: Insert Sample Barangays
-- ============================================
INSERT INTO [dbo].[Barangays] ([Name], [Code], [Municipality], [Province])
VALUES 
    ('Barangay San Antonio', 'BSA001', 'Quezon City', 'Metro Manila'),
    ('Barangay Santa Cruz', 'BSC002', 'Quezon City', 'Metro Manila'),
    ('Barangay San Miguel', 'BSM003', 'Quezon City', 'Metro Manila'),
    ('Barangay San Jose', 'BSJ004', 'Quezon City', 'Metro Manila'),
    ('Barangay Santo Niño', 'BSN005', 'Quezon City', 'Metro Manila');
GO

-- ============================================
-- Step 18: Insert Sample Subscription Plans
-- ============================================
INSERT INTO [dbo].[SubscriptionPlans] ([Name], [Description], [Price], [DurationMonths])
VALUES 
    ('Basic', 'Basic plan for small barangays', 5000.00, 12),
    ('Standard', 'Standard plan with additional features', 10000.00, 12),
    ('Premium', 'Premium plan with full features', 20000.00, 12),
    ('Enterprise', 'Custom enterprise plan', 50000.00, 12);
GO

-- ============================================
-- Verification: List all tables created
-- ============================================
SELECT 
    t.TABLE_NAME AS [Table],
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME) AS [Columns]
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_CATALOG = 'JAS_MINE_DB'
ORDER BY t.TABLE_NAME;
GO

PRINT '=================================================================';
PRINT 'JAS_MINE_DB database created successfully!';
PRINT 'Tables: Users, KnowledgeRepository, Policies, LessonsLearned,';
PRINT '        BestPractices, Announcements, AuditLogs, Barangays,';
PRINT '        SubscriptionPlans, BarangaySubscriptions, SubscriptionPayments,';
PRINT '        KnowledgeDiscussions, PasswordResetRequests, SharedDocuments';
PRINT '=================================================================';
GO
