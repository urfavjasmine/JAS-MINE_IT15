namespace JAS_MINE_IT15.Models
{
    public class AuditLogIntegrityReport
    {
        public bool IsValid { get; set; }
        public int CheckedCount { get; set; }
        public long? FirstBrokenLogId { get; set; }
        public string? Error { get; set; }
    }
}
