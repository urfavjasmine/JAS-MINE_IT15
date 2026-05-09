namespace JAS_MINE_IT15.Models
{
    /// <summary>
    /// Standardized security event types for categorizing audit logs.
    /// Provides consistent event classification for compliance reporting and security monitoring.
    /// </summary>
    public enum SecurityEventType
    {
        // Authentication Events
        LoginSuccess = 1,
        LoginFailure = 2,
        LogoutSuccess = 3,
        MfaAttempt = 4,
        MfaSuccess = 5,
        MfaFailure = 6,
        PasswordReset = 7,
        PasswordChanged = 8,
        PasswordChangeFailure = 9,
        AccountLocked = 10,
        AccountUnlocked = 11,

        // Authorization Events
        AuthorizationDenial = 20,
        PermissionDenial = 21,
        RoleGranted = 22,
        RoleRevoked = 23,
        PrivilegeEscalation = 24,
        UnauthorizedAccess = 25,

        // Data Modification Events
        DocumentCreated = 30,
        DocumentModified = 31,
        DocumentDeleted = 32,
        DocumentDownloaded = 33,
        DocumentShared = 34,
        BulkDelete = 35,
        DataExport = 36,

        // User Management Events
        UserCreated = 40,
        UserModified = 41,
        UserDeleted = 42,
        UserActivated = 43,
        UserDeactivated = 44,

        // System Events
        ConfigurationChanged = 50,
        SystemStartup = 51,
        SystemShutdown = 52,
        BackupCreated = 53,
        AuditLogVerified = 54,

        // Security Events
        SuspiciousActivity = 60,
        BruteForceAttempt = 61,
        InjectionAttempt = 62,
        CrossTenantAccess = 63,
        ValidationFailure = 64,
        AuditTamperingDetected = 65,

        // Compliance Events
        ComplianceCheckPassed = 70,
        ComplianceCheckFailed = 71,
        ComplianceReportGenerated = 72,
        DataRetentionEnforced = 73
    }

    /// <summary>
    /// Severity levels for security events used in alerting and reporting.
    /// </summary>
    public enum SecurityEventSeverity
    {
        Info = 1,           // Informational - routine operations
        Warning = 2,        // Warning - potential security concern
        Error = 3,          // Error - security violation occurred
        Critical = 4        // Critical - immediate action required
    }

    public static class SecurityEventTypeExtensions
    {
        /// <summary>
        /// Get human-readable description of security event type
        /// </summary>
        public static string GetDescription(this SecurityEventType eventType)
        {
            return eventType switch
            {
                SecurityEventType.LoginSuccess => "Successful Login",
                SecurityEventType.LoginFailure => "Failed Login Attempt",
                SecurityEventType.LogoutSuccess => "Logout",
                SecurityEventType.MfaAttempt => "MFA Attempt",
                SecurityEventType.MfaSuccess => "MFA Verified",
                SecurityEventType.MfaFailure => "MFA Failed",
                SecurityEventType.PasswordReset => "Password Reset",
                SecurityEventType.PasswordChanged => "Password Changed",
                SecurityEventType.PasswordChangeFailure => "Password Change Failed",
                SecurityEventType.AccountLocked => "Account Locked",
                SecurityEventType.AccountUnlocked => "Account Unlocked",
                SecurityEventType.AuthorizationDenial => "Authorization Denied",
                SecurityEventType.PermissionDenial => "Permission Denied",
                SecurityEventType.RoleGranted => "Role Granted",
                SecurityEventType.RoleRevoked => "Role Revoked",
                SecurityEventType.PrivilegeEscalation => "Privilege Escalation Attempt",
                SecurityEventType.UnauthorizedAccess => "Unauthorized Access",
                SecurityEventType.DocumentCreated => "Document Created",
                SecurityEventType.DocumentModified => "Document Modified",
                SecurityEventType.DocumentDeleted => "Document Deleted",
                SecurityEventType.DocumentDownloaded => "Document Downloaded",
                SecurityEventType.DocumentShared => "Document Shared",
                SecurityEventType.BulkDelete => "Bulk Delete Operation",
                SecurityEventType.DataExport => "Data Exported",
                SecurityEventType.UserCreated => "User Created",
                SecurityEventType.UserModified => "User Modified",
                SecurityEventType.UserDeleted => "User Deleted",
                SecurityEventType.UserActivated => "User Activated",
                SecurityEventType.UserDeactivated => "User Deactivated",
                SecurityEventType.ConfigurationChanged => "Configuration Changed",
                SecurityEventType.SystemStartup => "System Startup",
                SecurityEventType.SystemShutdown => "System Shutdown",
                SecurityEventType.BackupCreated => "Backup Created",
                SecurityEventType.AuditLogVerified => "Audit Log Integrity Verified",
                SecurityEventType.SuspiciousActivity => "Suspicious Activity Detected",
                SecurityEventType.BruteForceAttempt => "Brute Force Attack Detected",
                SecurityEventType.InjectionAttempt => "SQL/Code Injection Attempt",
                SecurityEventType.CrossTenantAccess => "Cross-Tenant Access Attempt",
                SecurityEventType.ValidationFailure => "Validation Failure",
                SecurityEventType.AuditTamperingDetected => "Audit Log Tampering Detected",
                SecurityEventType.ComplianceCheckPassed => "Compliance Check Passed",
                SecurityEventType.ComplianceCheckFailed => "Compliance Check Failed",
                SecurityEventType.ComplianceReportGenerated => "Compliance Report Generated",
                SecurityEventType.DataRetentionEnforced => "Data Retention Policy Enforced",
                _ => "Unknown Event"
            };
        }

        /// <summary>
        /// Get default severity level for event type
        /// </summary>
        public static SecurityEventSeverity GetDefaultSeverity(this SecurityEventType eventType)
        {
            return eventType switch
            {
                // Critical Events
                SecurityEventType.PrivilegeEscalation => SecurityEventSeverity.Critical,
                SecurityEventType.UnauthorizedAccess => SecurityEventSeverity.Critical,
                SecurityEventType.BruteForceAttempt => SecurityEventSeverity.Critical,
                SecurityEventType.InjectionAttempt => SecurityEventSeverity.Critical,
                SecurityEventType.CrossTenantAccess => SecurityEventSeverity.Critical,
                SecurityEventType.AuditTamperingDetected => SecurityEventSeverity.Critical,

                // Error Events
                SecurityEventType.LoginFailure => SecurityEventSeverity.Error,
                SecurityEventType.MfaFailure => SecurityEventSeverity.Error,
                SecurityEventType.PasswordChangeFailure => SecurityEventSeverity.Error,
                SecurityEventType.AccountLocked => SecurityEventSeverity.Error,
                SecurityEventType.AuthorizationDenial => SecurityEventSeverity.Error,
                SecurityEventType.PermissionDenial => SecurityEventSeverity.Error,
                SecurityEventType.SuspiciousActivity => SecurityEventSeverity.Error,
                SecurityEventType.ValidationFailure => SecurityEventSeverity.Error,
                SecurityEventType.ComplianceCheckFailed => SecurityEventSeverity.Error,

                // Warning Events
                SecurityEventType.MfaAttempt => SecurityEventSeverity.Warning,
                SecurityEventType.PasswordReset => SecurityEventSeverity.Warning,
                SecurityEventType.PasswordChanged => SecurityEventSeverity.Warning,
                SecurityEventType.BulkDelete => SecurityEventSeverity.Warning,
                SecurityEventType.DataExport => SecurityEventSeverity.Warning,
                SecurityEventType.ConfigurationChanged => SecurityEventSeverity.Warning,
                SecurityEventType.UserCreated => SecurityEventSeverity.Warning,
                SecurityEventType.UserModified => SecurityEventSeverity.Warning,
                SecurityEventType.UserDeleted => SecurityEventSeverity.Warning,
                SecurityEventType.RoleGranted => SecurityEventSeverity.Warning,
                SecurityEventType.RoleRevoked => SecurityEventSeverity.Warning,

                // Info Events
                _ => SecurityEventSeverity.Info
            };
        }
    }
}
