namespace JAS_MINE_IT15.Models
{
    public class SecurityDashboardViewModel
    {
        public string Role { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }

        public string Range { get; set; } = "24h";
        public string RangeLabel { get; set; } = "Last 24 Hours";
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }

        // ===== AUTHENTICATION METRICS =====
        public int FailedLoginAttemptsTotal { get; set; }
        public int AccountsWithFailedAttempts { get; set; }
        public int FailedLoginEventsInRange { get; set; }
        public int FailedLoginAlertThreshold { get; set; } = 20;
        public bool IsFailedLoginAlert { get; set; }

        public int LockedAccountsCount { get; set; }
        public List<LockedAccountItem> LockedAccounts { get; set; } = new();

        public int MfaFailuresInRange { get; set; }
        public int SuccessfulLoginsInRange { get; set; }
        public int TotalLoginAttemptsInRange { get; set; }

        // ===== API METRICS =====
        public int ApiRequestsInRange { get; set; }
        public int ApiFailedRequestsInRange { get; set; }
        public int ApiRateLimitedInRange { get; set; }
        public int ApiFailureAlertThreshold { get; set; } = 20;
        public bool IsApiFailureAlert { get; set; }
        public double ApiFailureRatePercent { get; set; }

        // ===== AUTHORIZATION & SECURITY =====
        public int AuthorizationDenialsInRange { get; set; }
        public int SuspiciousActivitiesInRange { get; set; }
        public List<SecurityEventItem> RecentSecurityEvents { get; set; } = new();

        // ===== DATA MODIFICATIONS =====
        public int DocumentsCreatedInRange { get; set; }
        public int DocumentsModifiedInRange { get; set; }
        public int DocumentsDeletedInRange { get; set; }
        public int PoliciesModifiedInRange { get; set; }
        public int UsersModifiedInRange { get; set; }
        public List<DataChangeItem> RecentDataChanges { get; set; } = new();

        // ===== USER ACTIVITY =====
        public int ActiveUsersInRange { get; set; }
        public int NewUsersInRange { get; set; }
        public int InactiveAccountsCount { get; set; }
        public List<UserActivityItem> TopActiveUsers { get; set; } = new();

        // ===== SYSTEM ALERTS =====
        public int CriticalAlertsCount { get; set; }
        public int WarningAlertsCount { get; set; }
        public List<SystemAlertItem> RecentAlerts { get; set; } = new();

        // ===== ACTIVITY LOGS =====
        public List<AdminActionItem> RecentAdminActions { get; set; } = new();
        public List<ApiEndpointUsageItem> TopApiEndpoints { get; set; } = new();
        public List<DailyApiUsageItem> DailyApiUsage { get; set; } = new();
        public List<ModuleActivityItem> ModuleActivityBreakdown { get; set; } = new();
    }

    public class LockedAccountItem
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
    }

    public class AdminActionItem
    {
        public long Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ApiEndpointUsageItem
    {
        public string Endpoint { get; set; } = string.Empty;
        public int Count { get; set; }
        public int FailedCount { get; set; }
        public double FailureRatePercent { get; set; }
    }

    public class DailyApiUsageItem
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public int FailedCount { get; set; }
    }

    public class SecurityEventItem
    {
        public long Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }

    public class DataChangeItem
    {
        public long Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class UserActivityItem
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ActionCount { get; set; }
        public string LastActive { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class SystemAlertItem
    {
        public long Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ModuleActivityItem
    {
        public string Module { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public int CreatedCount { get; set; }
        public int ModifiedCount { get; set; }
        public int DeletedCount { get; set; }
    }
}
