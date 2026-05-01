namespace JAS_MINE_IT15.Services
{
    public interface ITurnstileService
    {
        Task<bool> VerifyTokenAsync(string token, string? remoteIp);
    }
}
