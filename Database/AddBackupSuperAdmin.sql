-- ============================================================================
-- SAFE BACKUP SUPER ADMIN CREATION SCRIPT
-- ============================================================================
-- Purpose: Add a backup super admin account for emergency recovery
-- Email: system_admin@jasmine.gov.ph
-- Password: SysAdm1n#2026!X
-- 
-- SAFETY MEASURES:
-- 1. Checks if user already exists before inserting
-- 2. Uses transactions for rollback capability
-- 3. Validates role exists before assignment
-- 4. Does NOT modify any existing accounts
-- 5. Does NOT modify application logic or schema
-- ============================================================================

BEGIN TRANSACTION;

BEGIN TRY
    -- ========== STEP 1: Verify prerequisites ==========
    DECLARE @RoleId NVARCHAR(450);
    DECLARE @ExistingUserId NVARCHAR(450);
    DECLARE @NewUserId NVARCHAR(450) = NEWID();
    DECLARE @Email NVARCHAR(256) = 'system_admin@jasmine.gov.ph';
    DECLARE @NormalizedEmail NVARCHAR(256) = UPPER(@Email);
    DECLARE @FullName NVARCHAR(255) = 'Backup System Administrator';
    DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAECaIc2u8vEh6FmzHYB0GpPUXvgUhKzKz5L5XLJOF9Ue7tOfB+MNRk8TXYzaH9bZcvA==';
    -- Password hash for: SysAdm1n#2026!X (generated using ASP.NET Core Identity PBKDF2)

    -- Verify that 'super_admin' role exists
    SELECT @RoleId = Id FROM AspNetRoles WHERE Name = 'super_admin';
    IF @RoleId IS NULL
    BEGIN
        THROW 50001, 'ERROR: super_admin role does not exist. Please seed roles first.', 1;
    END

    -- Check if user already exists
    SELECT @ExistingUserId = Id FROM AspNetUsers WHERE Email = @Email;
    IF @ExistingUserId IS NOT NULL
    BEGIN
        PRINT 'INFO: User ' + @Email + ' already exists (ID: ' + @ExistingUserId + '). Skipping creation.';
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- ========== STEP 2: Insert into AspNetUsers ==========
    INSERT INTO AspNetUsers 
    (
        Id,
        UserName,
        NormalizedUserName,
        Email,
        NormalizedEmail,
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumber,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnd,
        LockoutEnabled,
        AccessFailedCount
    )
    VALUES 
    (
        @NewUserId,
        @Email,
        @NormalizedEmail,
        @Email,
        @NormalizedEmail,
        1,                          -- EmailConfirmed = true
        @PasswordHash,
        NEWID(),                    -- SecurityStamp
        NEWID(),                    -- ConcurrencyStamp
        NULL,                       -- PhoneNumber
        0,                          -- PhoneNumberConfirmed
        0,                          -- TwoFactorEnabled
        NULL,                       -- LockoutEnd
        1,                          -- LockoutEnabled
        0                           -- AccessFailedCount
    );
    PRINT 'SUCCESS: Identity user created (ID: ' + @NewUserId + ')';

    -- ========== STEP 3: Assign super_admin role ==========
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@NewUserId, @RoleId);
    PRINT 'SUCCESS: super_admin role assigned';

    -- ========== STEP 4: Create BusinessUser entry ==========
    INSERT INTO BusinessUsers 
    (
        Id,
        Email,
        PasswordHash,
        FullName,
        Role,
        IsActive,
        CreatedAt
    )
    VALUES 
    (
        NEWID(),
        @Email,
        'IDENTITY_MANAGED',         -- Password managed by ASP.NET Identity
        @FullName,
        'super_admin',
        1,                          -- IsActive
        GETUTCDATE()
    );
    PRINT 'SUCCESS: BusinessUser entry created';

    -- ========== FINAL: Commit all changes ==========
    COMMIT TRANSACTION;
    PRINT '========================================';
    PRINT 'BACKUP SUPER ADMIN CREATED SUCCESSFULLY';
    PRINT '========================================';
    PRINT 'Email: ' + @Email;
    PRINT 'Password: SysAdm1n#2026!X';
    PRINT 'Full Name: ' + @FullName;
    PRINT 'Role: super_admin';
    PRINT '========================================';
    PRINT 'NEXT STEPS:';
    PRINT '1. Restart your ASP.NET Core application';
    PRINT '2. Log in with the credentials above';
    PRINT '3. Delete or disable the old super admin account from the admin dashboard';
    PRINT '4. Remove this password from your memory/documentation';
    PRINT '========================================';

END TRY
BEGIN CATCH
    -- Rollback if any error occurs
    ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT 'ERROR: ' + @ErrorMessage;
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH
