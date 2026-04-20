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

        public int FailedLoginAttemptsTotal { get; set; }
        public int AccountsWithFailedAttempts { get; set; }
        public int FailedLoginEventsInRange { get; set; }
        public int FailedLoginAlertThreshold { get; set; } = 20;
        public bool IsFailedLoginAlert { get; set; }

        public int LockedAccountsCount { get; set; }
        public List<LockedAccountItem> LockedAccounts { get; set; } = new();

        public int ApiRequestsInRange { get; set; }
        public int ApiFailedRequestsInRange { get; set; }
        public int ApiRateLimitedInRange { get; set; }
        public int ApiFailureAlertThreshold { get; set; } = 20;
        public bool IsApiFailureAlert { get; set; }
        public double ApiFailureRatePercent { get; set; }

        public List<AdminActionItem> RecentAdminActions { get; set; } = new();
        public List<ApiEndpointUsageItem> TopApiEndpoints { get; set; } = new();
        public List<DailyApiUsageItem> DailyApiUsage { get; set; } = new();
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
}
