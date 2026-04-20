namespace JAS_MINE_IT15.Services
{
    public interface ISecurityAlertService
    {
        Task RecordLoginFailureAsync(string email, string ipAddress, bool isLockedOut, CancellationToken cancellationToken = default);
        Task RecordOtpFailureAsync(string email, string ipAddress, int attemptCount, CancellationToken cancellationToken = default);
        Task RecordRiskySignInAsync(string email, string currentIp, string currentUserAgent, string previousIp, string previousUserAgent, CancellationToken cancellationToken = default);
        Task RecordAuditIntegrityFailureAsync(long? firstBrokenLogId, string error, CancellationToken cancellationToken = default);
    }
}
