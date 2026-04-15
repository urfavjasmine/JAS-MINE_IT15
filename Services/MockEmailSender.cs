using Microsoft.AspNetCore.Identity.UI.Services;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Mock email sender for development/demo environments.
    /// Logs email content instead of sending over SMTP.
    /// </summary>
    public class MockEmailSender : IEmailSender
    {
        private readonly ILogger<MockEmailSender> _logger;

        public MockEmailSender(ILogger<MockEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation(
                "[MockEmail] To: {Email}\nSubject: {Subject}\nBody: {Body}",
                email,
                subject,
                htmlMessage);

            return Task.CompletedTask;
        }
    }
}
