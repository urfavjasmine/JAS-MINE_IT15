using JAS_MINE_IT15.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace JAS_MINE_IT15.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly ILogger<SmtpEmailSender> _logger;
        private readonly SmtpSettings _settings;

        public SmtpEmailSender(ILogger<SmtpEmailSender> logger, IOptions<SmtpSettings> options)
        {
            _logger = logger;
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host)
                || string.IsNullOrWhiteSpace(_settings.FromEmail)
                || string.IsNullOrWhiteSpace(_settings.UserName)
                || string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogError("SMTP settings are incomplete. Unable to send email to {Email}.", email);
                throw new InvalidOperationException("SMTP settings are incomplete. Configure Host, FromEmail, UserName, and Password.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(email));

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email} with subject {Subject}", email, subject);
        }
    }
}
