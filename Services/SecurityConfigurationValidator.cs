using JAS_MINE_IT15.Models;

namespace JAS_MINE_IT15.Services
{
    public static class SecurityConfigurationValidator
    {
        public static void ValidateOrThrow(IConfiguration configuration, IHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                return;
            }

            var smtpHost = configuration["Smtp:Host"] ?? string.Empty;
            var smtpUser = configuration["Smtp:UserName"] ?? string.Empty;
            var smtpPassword = configuration["Smtp:Password"] ?? string.Empty;
            var smtpFromEmail = configuration["Smtp:FromEmail"] ?? string.Empty;
            var auditHmacKey = configuration[$"{AuditIntegritySettings.SectionName}:HmacKey"] ?? string.Empty;
            var encryptionEnabled = bool.TryParse(configuration[$"{FieldEncryptionSettings.SectionName}:Enabled"], out var enabled) && enabled;
            var encryptionKey = configuration[$"{FieldEncryptionSettings.SectionName}:Key"] ?? string.Empty;

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(smtpHost)
                || string.IsNullOrWhiteSpace(smtpUser)
                || string.IsNullOrWhiteSpace(smtpPassword)
                || string.IsNullOrWhiteSpace(smtpFromEmail))
            {
                errors.Add("Smtp settings (Host/UserName/Password/FromEmail) must be configured in non-development environments.");
            }

            if (string.IsNullOrWhiteSpace(auditHmacKey))
            {
                errors.Add("AuditIntegrity:HmacKey must be configured in non-development environments.");
            }

            if (encryptionEnabled && string.IsNullOrWhiteSpace(encryptionKey))
            {
                errors.Add("FieldEncryption:Enabled is true but FieldEncryption:Key is missing.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Security configuration validation failed: " + string.Join(" ", errors));
            }
        }
    }
}
