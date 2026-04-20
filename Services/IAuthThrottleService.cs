namespace JAS_MINE_IT15.Services
{
    public interface IAuthThrottleService
    {
        int GetDelaySeconds(string scope, string userKey, string ipKey);
        void RecordFailure(string scope, string userKey, string ipKey);
        void RecordSuccess(string scope, string userKey, string ipKey);
    }
}
