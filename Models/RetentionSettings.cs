namespace JAS_MINE_IT15.Models
{
    public class RetentionSettings
    {
        public int AuditLogRetentionDays { get; set; } = 365;
        public int TempFileRetentionDays { get; set; } = 7;
        public int PendingRegistrationRetentionDays { get; set; } = 1;
        public int PasswordResetRequestRetentionDays { get; set; } = 30;
    }
}
