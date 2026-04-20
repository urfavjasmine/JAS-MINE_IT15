using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JAS_MINE_IT15.Services
{
    public class SecurityAlertService : ISecurityAlertService
    {
        private sealed class RollingCounterState
        {
            public Queue<DateTime> Hits { get; } = new();
        }

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SecurityAlertService> _logger;

        private static readonly TimeSpan CounterTtl = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan DedupeTtl = TimeSpan.FromMinutes(10);

        public SecurityAlertService(ApplicationDbContext context, IMemoryCache cache, ILogger<SecurityAlertService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task RecordLoginFailureAsync(string email, string ipAddress, bool isLockedOut, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = Normalize(email);
            var normalizedIp = Normalize(ipAddress);

            var userWindowCount = IncrementRollingCount($"security:login-fail:user:{normalizedEmail}", TimeSpan.FromMinutes(10));
            var ipWindowCount = IncrementRollingCount($"security:login-fail:ip:{normalizedIp}", TimeSpan.FromMinutes(10));

            if (isLockedOut)
            {
                await RaiseAlertAsync(
                    dedupeKey: $"lockout:{normalizedEmail}:{normalizedIp}",
                    title: "Security Alert: Account lockout detected",
                    message: $"Account lockout triggered for {normalizedEmail} from IP {normalizedIp}.",
                    cancellationToken: cancellationToken);
            }

            if (userWindowCount >= 5)
            {
                await RaiseAlertAsync(
                    dedupeKey: $"login-fail-user:{normalizedEmail}",
                    title: "Security Alert: Repeated login failures",
                    message: $"{userWindowCount} failed login attempts for {normalizedEmail} within 10 minutes.",
                    cancellationToken: cancellationToken);
            }

            if (ipWindowCount >= 8)
            {
                await RaiseAlertAsync(
                    dedupeKey: $"login-fail-ip:{normalizedIp}",
                    title: "Security Alert: Suspicious login activity by IP",
                    message: $"{ipWindowCount} failed login attempts detected from IP {normalizedIp} within 10 minutes.",
                    cancellationToken: cancellationToken);
            }
        }

        public async Task RecordOtpFailureAsync(string email, string ipAddress, int attemptCount, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = Normalize(email);
            var normalizedIp = Normalize(ipAddress);

            var userWindowCount = IncrementRollingCount($"security:otp-fail:user:{normalizedEmail}", TimeSpan.FromMinutes(10));
            var ipWindowCount = IncrementRollingCount($"security:otp-fail:ip:{normalizedIp}", TimeSpan.FromMinutes(10));

            if (attemptCount >= 4 || userWindowCount >= 6)
            {
                await RaiseAlertAsync(
                    dedupeKey: $"otp-fail-user:{normalizedEmail}",
                    title: "Security Alert: Repeated OTP verification failures",
                    message: $"Repeated OTP failures for {normalizedEmail}. Session attempts={attemptCount}, rolling failures={userWindowCount}.",
                    cancellationToken: cancellationToken);
            }

            if (ipWindowCount >= 10)
            {
                await RaiseAlertAsync(
                    dedupeKey: $"otp-fail-ip:{normalizedIp}",
                    title: "Security Alert: Suspicious OTP failures by IP",
                    message: $"{ipWindowCount} OTP failures detected from IP {normalizedIp} within 10 minutes.",
                    cancellationToken: cancellationToken);
            }
        }

        public async Task RecordRiskySignInAsync(string email, string currentIp, string currentUserAgent, string previousIp, string previousUserAgent, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = Normalize(email);
            var normalizedCurrentIp = Normalize(currentIp);
            var normalizedPreviousIp = Normalize(previousIp);

            var message =
                $"New device/location sign-in detected for {normalizedEmail}. " +
                $"Current IP: {normalizedCurrentIp}; Previous IP: {normalizedPreviousIp}.";

            await RaiseAlertAsync(
                dedupeKey: $"risky-signin:{normalizedEmail}",
                title: "Security Alert: New device or location sign-in",
                message: message,
                cancellationToken: cancellationToken,
                dedupeTtl: TimeSpan.FromMinutes(30));

            _logger.LogWarning(
                "Risky sign-in detected for {Email}. CurrentIP={CurrentIp}, PreviousIP={PreviousIp}, CurrentUA={CurrentUa}, PreviousUA={PreviousUa}",
                normalizedEmail,
                normalizedCurrentIp,
                normalizedPreviousIp,
                currentUserAgent,
                previousUserAgent);
        }

        public async Task RecordAuditIntegrityFailureAsync(long? firstBrokenLogId, string error, CancellationToken cancellationToken = default)
        {
            var message = firstBrokenLogId.HasValue
                ? $"Audit integrity check failed at log ID {firstBrokenLogId.Value}. Details: {error}"
                : $"Audit integrity check failed. Details: {error}";

            await RaiseAlertAsync(
                dedupeKey: "audit-integrity-failure",
                title: "Critical Security Alert: Audit integrity mismatch",
                message: message,
                cancellationToken: cancellationToken,
                dedupeTtl: TimeSpan.FromMinutes(30));
        }

        private int IncrementRollingCount(string key, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            var state = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CounterTtl;
                return new RollingCounterState();
            }) ?? new RollingCounterState();

            lock (state)
            {
                state.Hits.Enqueue(now);
                while (state.Hits.Count > 0 && now - state.Hits.Peek() > window)
                {
                    state.Hits.Dequeue();
                }

                _cache.Set(key, state, CounterTtl);
                return state.Hits.Count;
            }
        }

        private async Task RaiseAlertAsync(
            string dedupeKey,
            string title,
            string message,
            CancellationToken cancellationToken,
            TimeSpan? dedupeTtl = null)
        {
            var dedupeCacheKey = $"security:alert:{dedupeKey}";
            if (_cache.TryGetValue(dedupeCacheKey, out _))
            {
                return;
            }

            _cache.Set(dedupeCacheKey, true, dedupeTtl ?? DedupeTtl);

            var superAdminUserIds = await _context.BusinessUsers
                .Where(u => u.IsActive && u.Role == "super_admin")
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var userId in superAdminUserIds.Distinct())
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message.Length > 500 ? message[..500] : message,
                    Type = "urgent",
                    Link = "/SecurityDashboard",
                    RelatedEntityType = "Security",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("{Title} {Message}", title, message);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
    }
}
