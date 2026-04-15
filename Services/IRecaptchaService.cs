namespace JAS_MINE_IT15.Services
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyTokenAsync(string token, string? remoteIp);
    }
}
