USE [master];
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'JAS_MINE_DB')
BEGIN
    ALTER DATABASE [JAS_MINE_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [JAS_MINE_DB];
    PRINT 'Existing JAS_MINE_DB dropped.';
END
GO

CREATE DATABASE [JAS_MINE_DB_New];
GO

USE [JAS_MINE_DB_New];
GO

PRINT 'Creating JAS_MINE_DB_New database...';
GO

-- ============================================
-- 1. Users Table
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

CREATE NONCLUSTERED INDEX IX_Users_Email ON [dbo].[Users] ([Email]) INCLUDE ([FullName], [Role], [IsActive]);
CREATE NONCLUSTERED INDEX IX_Users_Role ON [dbo].[Users] ([Role]) INCLUDE ([Email], [FullName], [IsActive]);
CREATE NONCLUSTERED INDEX IX_Users_BarangayId ON [dbo].[Users] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Users_IsActive ON [dbo].[Users] ([IsActive]) INCLUDE ([Email], [Role]);
GO

PRINT 'Created Users table';
GO

-- ============================================
-- 2. Barangays Table
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

CREATE NONCLUSTERED INDEX IX_Barangays_Region ON [dbo].[Barangays] ([Region]) WHERE [Region] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Barangays_Province ON [dbo].[Barangays] ([Province]) WHERE [Province] IS NOT NULL;
GO

PRINT 'Created Barangays table';
GO

-- ============================================
-- 3. SubscriptionPlans Table
-- ============================================
CREATE TABLE [dbo].[SubscriptionPlans] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Name]            NVARCHAR(100)     NOT NULL,
    [Description]     NVARCHAR(500)     NULL,
    [Price]           DECIMAL(10,2)     NOT NULL CONSTRAINT DF_SubscriptionPlans_Price DEFAULT (0.00),
    [DurationMonths]  INT               NOT NULL CONSTRAINT DF_SubscriptionPlans_Duration DEFAULT (12),
    [UserLimit]       INT               NOT NULL CONSTRAINT DF_SubscriptionPlans_UserLimit DEFAULT (5),
    [Features]        NVARCHAR(MAX)     NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_SubscriptionPlans_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_SubscriptionPlans_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_SubscriptionPlans PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

PRINT 'Created SubscriptionPlans table';
GO

-- ============================================
-- 4. BarangaySubscriptions Table
-- ============================================
CREATE TABLE [dbo].[BarangaySubscriptions] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [BarangayId]      INT               NOT NULL,
    [PlanId]          INT               NOT NULL,
    [StartDate]       DATE              NOT NULL,
    [EndDate]         DATE              NOT NULL,
    [Status]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_BarangaySubscriptions_Status DEFAULT ('Pending')
                      CONSTRAINT CK_BarangaySubscriptions_Status CHECK ([Status] IN ('Active', 'Expired', 'Cancelled', 'Pending')),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_BarangaySubscriptions_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_BarangaySubscriptions_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_BarangaySubscriptions PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_BarangaySubscriptions_Barangay FOREIGN KEY ([BarangayId]) REFERENCES [dbo].[Barangays]([Id]),
    CONSTRAINT FK_BarangaySubscriptions_Plan FOREIGN KEY ([PlanId]) REFERENCES [dbo].[SubscriptionPlans]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_BarangaySubscriptions_BarangayId ON [dbo].[BarangaySubscriptions] ([BarangayId]);
CREATE NONCLUSTERED INDEX IX_BarangaySubscriptions_Status ON [dbo].[BarangaySubscriptions] ([Status]) WHERE [IsActive] = 1;
GO

PRINT 'Created BarangaySubscriptions table';
GO

-- ============================================
-- 5. Invoices Table
-- ============================================
CREATE TABLE [dbo].[Invoices] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [InvoiceNumber]   NVARCHAR(50)      NOT NULL,
    [SubscriptionId]  INT               NOT NULL,
    [BarangayId]      INT               NULL,
    [Amount]          DECIMAL(10,2)     NOT NULL,
    [DueDate]         DATE              NULL,
    [Status]          NVARCHAR(30)      NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT ('Unpaid')
                      CONSTRAINT CK_Invoices_Status CHECK ([Status] IN ('Unpaid', 'Paid', 'Overdue', 'Void', 'PendingVerification')),
    [IssuedAt]        DATETIME2         NOT NULL CONSTRAINT DF_Invoices_IssuedAt DEFAULT (GETDATE()),
    [PaidAt]          DATETIME2         NULL,
    [Notes]           NVARCHAR(500)     NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Invoices_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE ([InvoiceNumber]),
    CONSTRAINT FK_Invoices_Subscription FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[BarangaySubscriptions]([Id]),
    CONSTRAINT FK_Invoices_Barangay FOREIGN KEY ([BarangayId]) REFERENCES [dbo].[Barangays]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_Invoices_SubscriptionId ON [dbo].[Invoices] ([SubscriptionId]);
CREATE NONCLUSTERED INDEX IX_Invoices_Status ON [dbo].[Invoices] ([Status]) WHERE [IsActive] = 1;
CREATE NONCLUSTERED INDEX IX_Invoices_InvoiceNumber ON [dbo].[Invoices] ([InvoiceNumber]);
GO

PRINT 'Created Invoices table';
GO

-- ============================================
-- 6. SubscriptionPayments Table
-- ============================================
CREATE TABLE [dbo].[SubscriptionPayments] (
    [Id]                  INT IDENTITY(1,1) NOT NULL,
    [SubscriptionId]      INT               NOT NULL,
    [InvoiceId]           INT               NULL,
    [Amount]              DECIMAL(10,2)     NOT NULL,
    [PaymentDate]         DATE              NOT NULL,
    [PaymentMethod]       NVARCHAR(50)      NULL,
    [ReferenceNumber]     NVARCHAR(100)     NULL,
    [ProofOfPaymentUrl]   NVARCHAR(500)     NULL,
    [Status]              NVARCHAR(30)      NOT NULL CONSTRAINT DF_SubscriptionPayments_Status DEFAULT ('Pending')
                          CONSTRAINT CK_SubscriptionPayments_Status CHECK ([Status] IN ('Pending', 'Paid', 'Failed', 'Refunded', 'PendingVerification', 'Approved', 'Rejected')),
    [RejectionReason]     NVARCHAR(500)     NULL,
    [Notes]               NVARCHAR(500)     NULL,
    [ProcessedById]       INT               NULL,
    [ProcessedAt]         DATETIME2         NULL,
    [IsActive]            BIT               NOT NULL CONSTRAINT DF_SubscriptionPayments_IsActive DEFAULT (1),
    [CreatedAt]           DATETIME2         NOT NULL CONSTRAINT DF_SubscriptionPayments_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]           DATETIME2         NULL,

    CONSTRAINT PK_SubscriptionPayments PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_SubscriptionPayments_Subscription FOREIGN KEY ([SubscriptionId]) REFERENCES [dbo].[BarangaySubscriptions]([Id]),
    CONSTRAINT FK_SubscriptionPayments_Invoice FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices]([Id]),
    CONSTRAINT FK_SubscriptionPayments_ProcessedBy FOREIGN KEY ([ProcessedById]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_SubscriptionPayments_Status ON [dbo].[SubscriptionPayments] ([Status]) WHERE [IsActive] = 1;
CREATE NONCLUSTERED INDEX IX_SubscriptionPayments_InvoiceId ON [dbo].[SubscriptionPayments] ([InvoiceId]) WHERE [InvoiceId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_SubscriptionPayments_SubscriptionId ON [dbo].[SubscriptionPayments] ([SubscriptionId]);
GO

PRINT 'Created SubscriptionPayments table';
GO

-- ============================================
-- 7. KnowledgeRepository Table
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
    [FileType]        NVARCHAR(255)     NULL,
    [IsArchived]      BIT               NOT NULL CONSTRAINT DF_KnowledgeRepository_IsArchived DEFAULT (0),
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

CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_Category ON [dbo].[KnowledgeRepository] ([Category]) INCLUDE ([Title], [Status]);
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_Status ON [dbo].[KnowledgeRepository] ([Status]) INCLUDE ([Title], [Category]);
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_UploadedById ON [dbo].[KnowledgeRepository] ([UploadedById]);
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_BarangayId ON [dbo].[KnowledgeRepository] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_KnowledgeRepository_IsActive ON [dbo].[KnowledgeRepository] ([IsActive]) INCLUDE ([Title], [Status]);
GO

PRINT 'Created KnowledgeRepository table';
GO

-- ============================================
-- 8. Policies Table
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
    [IsArchived]      BIT               NOT NULL CONSTRAINT DF_Policies_IsArchived DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Policies_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Policies_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_Policies PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Policies_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_Policies_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_Policies_Status ON [dbo].[Policies] ([Status]) INCLUDE ([Title], [Category]);
CREATE NONCLUSTERED INDEX IX_Policies_AuthorId ON [dbo].[Policies] ([AuthorId]);
CREATE NONCLUSTERED INDEX IX_Policies_BarangayId ON [dbo].[Policies] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Policies_IsActive ON [dbo].[Policies] ([IsActive]) INCLUDE ([Title], [Status]);
GO

PRINT 'Created Policies table';
GO

-- ============================================
-- 9. LessonsLearned Table
-- ============================================
CREATE TABLE [dbo].[LessonsLearned] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Title]           NVARCHAR(300)     NOT NULL,
    [Summary]         NVARCHAR(MAX)     NOT NULL,
    [ProjectName]     NVARCHAR(200)     NULL,
    [ProjectType]     NVARCHAR(100)     NULL,
    [Problem]         NVARCHAR(MAX)     NOT NULL CONSTRAINT DF_LessonsLearned_Problem DEFAULT (''),
    [ActionTaken]     NVARCHAR(MAX)     NOT NULL CONSTRAINT DF_LessonsLearned_ActionTaken DEFAULT (''),
    [Result]          NVARCHAR(MAX)     NOT NULL CONSTRAINT DF_LessonsLearned_Result DEFAULT (''),
    [Recommendation]  NVARCHAR(MAX)     NULL,
    [DateRecorded]    DATETIME2         NOT NULL CONSTRAINT DF_LessonsLearned_DateRecorded DEFAULT ('0001-01-01'),
    [Tags]            NVARCHAR(500)     NULL,
    [Status]          NVARCHAR(30)      NOT NULL CONSTRAINT DF_LessonsLearned_Status DEFAULT ('pending')
                      CONSTRAINT CK_LessonsLearned_Status CHECK ([Status] IN ('draft', 'pending', 'approved', 'rejected')),
    [SubmittedById]   INT               NOT NULL,
    [ApprovedById]    INT               NULL,
    [ApprovedAt]      DATETIME2         NULL,
    [BarangayId]      INT               NULL,
    [LikesCount]      INT               NOT NULL CONSTRAINT DF_LessonsLearned_LikesCount DEFAULT (0),
    [CommentsCount]   INT               NOT NULL CONSTRAINT DF_LessonsLearned_CommentsCount DEFAULT (0),
    [IsArchived]      BIT               NOT NULL CONSTRAINT DF_LessonsLearned_IsArchived DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_LessonsLearned_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_LessonsLearned_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_LessonsLearned PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_LessonsLearned_SubmittedBy FOREIGN KEY ([SubmittedById]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_LessonsLearned_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_LessonsLearned_ProjectType ON [dbo].[LessonsLearned] ([ProjectType]) INCLUDE ([Title], [Status]);
CREATE NONCLUSTERED INDEX IX_LessonsLearned_Status ON [dbo].[LessonsLearned] ([Status]) INCLUDE ([Title], [ProjectType]);
CREATE NONCLUSTERED INDEX IX_LessonsLearned_SubmittedById ON [dbo].[LessonsLearned] ([SubmittedById]);
CREATE NONCLUSTERED INDEX IX_LessonsLearned_BarangayId ON [dbo].[LessonsLearned] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_LessonsLearned_IsActive ON [dbo].[LessonsLearned] ([IsActive]) INCLUDE ([Title], [Status]);
GO

PRINT 'Created LessonsLearned table';
GO

-- ============================================
-- 10. BestPractices Table
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
    [Status]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_BestPractices_Status DEFAULT ('pending'),
    [Purpose]         NVARCHAR(MAX)     NULL,
    [Steps]           NVARCHAR(MAX)     NOT NULL CONSTRAINT DF_BestPractices_Steps DEFAULT (''),
    [ResourcesNeeded] NVARCHAR(MAX)     NULL,
    [OwnerOffice]     NVARCHAR(200)     NULL,
    [AttachmentUrl]   NVARCHAR(500)     NULL,
    [SubmittedById]   INT               NOT NULL,
    [ApprovedById]    INT               NULL,
    [ApprovedAt]      DATETIME2         NULL,
    [IsArchived]      BIT               NOT NULL CONSTRAINT DF_BestPractices_IsArchived DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_BestPractices_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_BestPractices_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_BestPractices PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_BestPractices_SubmittedBy FOREIGN KEY ([SubmittedById]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_BestPractices_ApprovedBy FOREIGN KEY ([ApprovedById]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_BestPractices_Category ON [dbo].[BestPractices] ([Category]) INCLUDE ([Title], [Rating]);
CREATE NONCLUSTERED INDEX IX_BestPractices_IsFeatured ON [dbo].[BestPractices] ([IsFeatured]) WHERE [IsFeatured] = 1;
CREATE NONCLUSTERED INDEX IX_BestPractices_BarangayId ON [dbo].[BestPractices] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_BestPractices_IsActive ON [dbo].[BestPractices] ([IsActive]) INCLUDE ([Title], [Category]);
GO

PRINT 'Created BestPractices table';
GO

-- ============================================
-- 11. KnowledgeDiscussions Table
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
    [IsArchived]      BIT               NOT NULL CONSTRAINT DF_KnowledgeDiscussions_IsArchived DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_KnowledgeDiscussions_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_KnowledgeDiscussions_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_KnowledgeDiscussions PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_KnowledgeDiscussions_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_KnowledgeDiscussions_BarangayId ON [dbo].[KnowledgeDiscussions] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_KnowledgeDiscussions_AuthorId ON [dbo].[KnowledgeDiscussions] ([AuthorId]);
CREATE NONCLUSTERED INDEX IX_KnowledgeDiscussions_IsActive ON [dbo].[KnowledgeDiscussions] ([IsActive]) WHERE [IsActive] = 1;
GO

PRINT 'Created KnowledgeDiscussions table';
GO

-- ============================================
-- 12. DiscussionComments Table
-- ============================================
CREATE TABLE [dbo].[DiscussionComments] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [DiscussionId]    INT               NOT NULL,
    [AuthorId]        INT               NOT NULL,
    [Content]         NVARCHAR(MAX)     NOT NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_DiscussionComments_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_DiscussionComments_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_DiscussionComments PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_DiscussionComments_Discussion FOREIGN KEY ([DiscussionId]) REFERENCES [dbo].[KnowledgeDiscussions]([Id]),
    CONSTRAINT FK_DiscussionComments_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_DiscussionComments_DiscussionId ON [dbo].[DiscussionComments] ([DiscussionId]) WHERE [IsActive] = 1;
GO

PRINT 'Created DiscussionComments table';
GO

-- ============================================
-- 13. DiscussionLikes Table
-- ============================================
CREATE TABLE [dbo].[DiscussionLikes] (
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [DiscussionId]    INT               NOT NULL,
    [UserId]          INT               NOT NULL,
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_DiscussionLikes_CreatedAt DEFAULT (GETDATE()),

    CONSTRAINT PK_DiscussionLikes PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_DiscussionLikes_Discussion FOREIGN KEY ([DiscussionId]) REFERENCES [dbo].[KnowledgeDiscussions]([Id]),
    CONSTRAINT FK_DiscussionLikes_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT UQ_DiscussionLikes_UniqueVote UNIQUE ([DiscussionId], [UserId])
);
GO

CREATE NONCLUSTERED INDEX IX_DiscussionLikes_DiscussionId ON [dbo].[DiscussionLikes] ([DiscussionId]);
GO

PRINT 'Created DiscussionLikes table';
GO

-- ============================================
-- 14. Announcements Table
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
    [TargetAudience]  NVARCHAR(100)     NULL,
    [ViewCount]       INT               NOT NULL CONSTRAINT DF_Announcements_ViewCount DEFAULT (0),
    [IsArchived]      BIT               NOT NULL CONSTRAINT DF_Announcements_IsArchived DEFAULT (0),
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_Announcements_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_Announcements_CreatedAt DEFAULT (GETDATE()),
    [UpdatedAt]       DATETIME2         NULL,

    CONSTRAINT PK_Announcements PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Announcements_Author FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_Announcements_Status ON [dbo].[Announcements] ([Status]) INCLUDE ([Title], [Priority], [IsPinned]);
CREATE NONCLUSTERED INDEX IX_Announcements_IsPinned ON [dbo].[Announcements] ([IsPinned]) WHERE [IsPinned] = 1;
CREATE NONCLUSTERED INDEX IX_Announcements_AuthorId ON [dbo].[Announcements] ([AuthorId]);
CREATE NONCLUSTERED INDEX IX_Announcements_BarangayId ON [dbo].[Announcements] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Announcements_IsActive ON [dbo].[Announcements] ([IsActive]) INCLUDE ([Title], [Status]);
CREATE NONCLUSTERED INDEX IX_Announcements_PublishedAt ON [dbo].[Announcements] ([PublishedAt] DESC) WHERE [Status] = 'published';
GO

PRINT 'Created Announcements table';
GO

-- ============================================
-- 15. AuditLogs Table
-- ============================================
CREATE TABLE [dbo].[AuditLogs] (
    [Id]              BIGINT IDENTITY(1,1) NOT NULL,
    [UserId]          INT               NULL,
    [UserEmail]       NVARCHAR(255)     NULL,
    [UserName]        NVARCHAR(150)     NULL,
    [Action]          NVARCHAR(50)      NOT NULL,
    [Module]          NVARCHAR(100)     NOT NULL,
    [TargetId]        INT               NULL,
    [TargetType]      NVARCHAR(100)     NULL,
    [TargetName]      NVARCHAR(300)     NULL,
    [Description]     NVARCHAR(MAX)     NULL,
    [OldValues]       NVARCHAR(MAX)     NULL,
    [NewValues]       NVARCHAR(MAX)     NULL,
    [IpAddress]       NVARCHAR(45)      NULL,
    [UserAgent]       NVARCHAR(500)     NULL,
    [SessionId]       NVARCHAR(100)     NULL,
    [BarangayId]      INT               NULL,
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_AuditLogs_IsActive DEFAULT (1),
    [CreatedAt]       DATETIME2         NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (GETDATE()),

    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON [dbo].[AuditLogs] ([UserId]) INCLUDE ([Action], [Module], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Action ON [dbo].[AuditLogs] ([Action]) INCLUDE ([Module], [UserEmail], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Module ON [dbo].[AuditLogs] ([Module]) INCLUDE ([Action], [UserEmail], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt ON [dbo].[AuditLogs] ([CreatedAt] DESC) INCLUDE ([Action], [Module], [UserEmail]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_IsActive ON [dbo].[AuditLogs] ([IsActive]) WHERE [IsActive] = 1;
CREATE NONCLUSTERED INDEX IX_AuditLogs_BarangayId ON [dbo].[AuditLogs] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
GO

PRINT 'Created AuditLogs table';
GO

-- ============================================
-- 16. PasswordResetRequests Table
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

PRINT 'Created PasswordResetRequests table';
GO

-- ============================================
-- 17. SharedDocuments Table
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

PRINT 'Created SharedDocuments table';
GO

-- ============================================
-- 18. Notifications Table
-- ============================================
CREATE TABLE [dbo].[Notifications] (
    [Id]                INT IDENTITY(1,1) NOT NULL,
    [UserId]            INT               NOT NULL,
    [BarangayId]        INT               NULL,
    [Title]             NVARCHAR(200)     NOT NULL,
    [Message]           NVARCHAR(500)     NOT NULL,
    [Type]              NVARCHAR(50)      NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT ('info'),
    [Link]              NVARCHAR(500)     NULL,
    [RelatedEntityType] NVARCHAR(50)      NULL,
    [RelatedEntityId]   INT               NULL,
    [IsRead]            BIT               NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
    [ReadAt]            DATETIME2         NULL,
    [IsActive]          BIT               NOT NULL CONSTRAINT DF_Notifications_IsActive DEFAULT (1),
    [CreatedAt]         DATETIME2         NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (GETDATE()),

    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_Notifications_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_Notifications_Barangay FOREIGN KEY ([BarangayId]) REFERENCES [dbo].[Barangays]([Id])
);
GO

CREATE NONCLUSTERED INDEX IX_Notifications_UserId ON [dbo].[Notifications] ([UserId], [IsRead], [IsActive]) INCLUDE ([Title], [Type], [CreatedAt]);
CREATE NONCLUSTERED INDEX IX_Notifications_BarangayId ON [dbo].[Notifications] ([BarangayId]) WHERE [BarangayId] IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Notifications_CreatedAt ON [dbo].[Notifications] ([CreatedAt] DESC) INCLUDE ([UserId], [IsRead]);
GO

PRINT 'Created Notifications table';
GO

-- ============================================
-- 19. Insert Default Data
-- ============================================

-- Super Admin User (Password: Admin@123 - use proper hash in production)
INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [FullName], [Role], [BarangayName])
VALUES (
    'admin@jasmine.gov.ph', 
    'AQAAAAIAAYagAAAAEA5M8VkF5d4nN5LZ2uHR7Q==',
    'System Administrator',
    'super_admin',
    NULL
);
GO

PRINT 'Inserted default super admin user';
GO

-- Subscription Plans (3 plans: Basic, Professional, Enterprise)
INSERT INTO [dbo].[SubscriptionPlans] ([Name], [Description], [Price], [DurationMonths], [Features], [UserLimit])
VALUES 
    ('Basic', 'Essential tools for small barangays getting started.', 299.00, 1, 'Up to 4 users;View records;Add and manage records;View announcements;Basic reports', 4),
    ('Professional', 'Everything you need to manage your barangay records efficiently.', 599.00, 1, 'Up to 10 users;All Basic features;Create and manage announcements;Better reports;Activity logs', 10),
    ('Enterprise', 'Complete access with advanced tools and detailed tracking.', 999.00, 1, 'Up to 20 users;All Professional features;Dashboard (summary view);Archive and restore data;Detailed tracking', 20);
GO

PRINT 'Inserted default subscription plans';
GO

-- ============================================
-- Verification
-- ============================================
SELECT 
    t.TABLE_NAME AS [Table],
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME) AS [Columns]
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_CATALOG = 'JAS_MINE_DB'
ORDER BY t.TABLE_NAME;
GO

PRINT '';
PRINT '=================================================================';
PRINT 'JAS_MINE_DB database created successfully!';
PRINT '';
PRINT 'Tables (18):';
PRINT '  - Users                    - Barangays';
PRINT '  - SubscriptionPlans        - BarangaySubscriptions';
PRINT '  - Invoices                 - SubscriptionPayments';
PRINT '  - KnowledgeRepository      - Policies';
PRINT '  - LessonsLearned           - BestPractices';
PRINT '  - KnowledgeDiscussions     - DiscussionComments';
PRINT '  - DiscussionLikes          - Announcements';
PRINT '  - AuditLogs                - PasswordResetRequests';
PRINT '  - SharedDocuments          - Notifications';
PRINT '';
PRINT 'Default Data:';
PRINT '  - Super Admin: admin@jasmine.gov.ph';
PRINT '  - 4 Subscription Plans';
PRINT '=================================================================';
GO
