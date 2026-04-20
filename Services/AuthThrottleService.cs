using Microsoft.Extensions.Caching.Memory;

namespace JAS_MINE_IT15.Services
{
    public class AuthThrottleService : IAuthThrottleService
    {
        private sealed class ThrottleState
        {
            public int FailureCount { get; set; }
            public DateTime LastFailureUtc { get; set; }
        }

        private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(30);
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthThrottleService> _logger;

        public AuthThrottleService(IMemoryCache cache, ILogger<AuthThrottleService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public int GetDelaySeconds(string scope, string userKey, string ipKey)
        {
            var stateByUserIp = GetState(BuildUserIpKey(scope, userKey, ipKey));
            var stateByIp = GetState(BuildIpKey(scope, ipKey));

            var delayByUserIp = ComputeDelaySeconds(stateByUserIp);
            var delayByIp = ComputeDelaySeconds(stateByIp);

            return Math.Max(delayByUserIp, delayByIp);
        }

        public void RecordFailure(string scope, string userKey, string ipKey)
        {
            IncrementFailure(BuildUserIpKey(scope, userKey, ipKey));
            IncrementFailure(BuildIpKey(scope, ipKey));
        }

        public void RecordSuccess(string scope, string userKey, string ipKey)
        {
            _cache.Remove(BuildUserIpKey(scope, userKey, ipKey));
            _cache.Remove(BuildIpKey(scope, ipKey));
        }

        private static string BuildUserIpKey(string scope, string userKey, string ipKey)
            => $"throttle:{scope}:userip:{Normalize(userKey)}:{Normalize(ipKey)}";

        private static string BuildIpKey(string scope, string ipKey)
            => $"throttle:{scope}:ip:{Normalize(ipKey)}";

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();

        private ThrottleState? GetState(string key)
        {
            return _cache.TryGetValue(key, out ThrottleState? state) ? state : null;
        }

        private void IncrementFailure(string key)
        {
            var state = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = StateTtl;
                return new ThrottleState();
            }) ?? new ThrottleState();

            state.FailureCount += 1;
            state.LastFailureUtc = DateTime.UtcNow;

            _cache.Set(key, state, StateTtl);

            _logger.LogDebug("Auth throttle failure recorded for key {ThrottleKey}; count={Count}", key, state.FailureCount);
        }

        private static int ComputeDelaySeconds(ThrottleState? state)
        {
            if (state == null || state.FailureCount < 3)
            {
                return 0;
            }

            var exponent = Math.Min(7, state.FailureCount - 3);
            var delay = 5 * (1 << exponent);
            return Math.Min(300, delay);
        }
    }
}
