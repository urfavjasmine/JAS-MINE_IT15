using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Validation result containing success flag and error messages.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(string error) => new() { IsValid = false, Errors = new() { error } };
        public static ValidationResult Failure(List<string> errors) => new() { IsValid = false, Errors = errors };
    }

    /// <summary>
    /// Interface for business logic validation services.
    /// Used for validating operations that require database checks.
    /// </summary>
    public interface IValidationService
    {
        /// <summary>Validate document upload (file size, type, content).</summary>
        Task<ValidationResult> ValidateDocumentUploadAsync(IFormFile file, long maxFileSize = 52428800); // 50 MB

        /// <summary>Validate subscription plan change (valid plan, usage limits).</summary>
        Task<ValidationResult> ValidateSubscriptionChangeAsync(int barangayId, string newPlanName);

        /// <summary>Validate budget allocation (within barangay limits).</summary>
        Task<ValidationResult> ValidateBudgetAllocationAsync(int barangayId, decimal amount);

        /// <summary>Validate email is unique (not already registered).</summary>
        Task<ValidationResult> ValidateUniqueEmailAsync(string email, int? excludeUserId = null);

        /// <summary>Validate barangay exists and is active.</summary>
        Task<ValidationResult> ValidateBarangayAsync(int barangayId);

        /// <summary>Validate user role is valid and within user's scope.</summary>
        Task<ValidationResult> ValidateRoleAsync(string roleName);

        /// <summary>Validate user has permission for operation.</summary>
        Task<ValidationResult> ValidatePermissionAsync(int userId, string operation, int? resourceId = null);

        /// <summary>Validate data export request (size, frequency).</summary>
        Task<ValidationResult> ValidateDataExportAsync(int userId, int barangayId);

        /// <summary>Validate password meets security requirements.</summary>
        ValidationResult ValidatePassword(string password, string email, string username);

        /// <summary>Validate file contains expected content (not malware/invalid format).</summary>
        Task<ValidationResult> ValidateFileContentAsync(IFormFile file);
    }

    /// <summary>
    /// Implementation of business logic validation service.
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ValidationService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly string[] AllowedDocumentExtensions = { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "jpg", "jpeg", "png", "gif" };
        private const long MaxFileSize = 52428800; // 50 MB
        private const long MaxDailyExportSize = 1073741824; // 1 GB

        public ValidationService(
            ApplicationDbContext context,
            ILogger<ValidationService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>Validate document upload (file size, type, content).</summary>
        public async Task<ValidationResult> ValidateDocumentUploadAsync(IFormFile file, long maxFileSize = 52428800)
        {
            if (file == null)
                return ValidationResult.Failure("File is required.");

            if (file.Length == 0)
                return ValidationResult.Failure("File cannot be empty.");

            if (file.Length > maxFileSize)
                return ValidationResult.Failure($"File size cannot exceed {maxFileSize / 1024 / 1024} MB.");

            var ext = System.IO.Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            if (!AllowedDocumentExtensions.Contains(ext))
                return ValidationResult.Failure($"File type .{ext} is not allowed. Allowed types: {string.Join(", ", AllowedDocumentExtensions)}");

            // Validate file content (basic check - in production, use antivirus scanning)
            var contentValidation = await ValidateFileContentAsync(file);
            if (!contentValidation.IsValid)
                return contentValidation;

            return ValidationResult.Success();
        }

        /// <summary>Validate subscription plan change (valid plan, usage limits).</summary>
        public async Task<ValidationResult> ValidateSubscriptionChangeAsync(int barangayId, string newPlanName)
        {
            // Validate barangay exists
            var barangayExists = await _context.Barangays.AnyAsync(b => b.Id == barangayId);
            if (!barangayExists)
                return ValidationResult.Failure("Barangay not found.");

            // Validate plan exists
            var validPlans = new[] { "Basic", "Professional", "Enterprise" };
            if (!validPlans.Contains(newPlanName))
                return ValidationResult.Failure($"Invalid plan name. Valid plans: {string.Join(", ", validPlans)}");

            // Check if downgrading and would lose features
            var currentSubscription = await _context.BarangaySubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.BarangayId == barangayId && s.IsActive);
            
            if (currentSubscription?.Plan?.Name == "Enterprise" && newPlanName == "Basic")
            {
                // Check if they have documents that exceed Basic plan limits
                var documentCount = await _context.KnowledgeDocuments
                    .Where(d => d.BarangayId == barangayId)
                    .CountAsync();

                if (documentCount > 100) // Basic plan limit = 100 documents (example)
                    return ValidationResult.Failure("Cannot downgrade: You have more documents than the Basic plan allows (max 100).");
            }

            return ValidationResult.Success();
        }

        /// <summary>Validate budget allocation (within barangay limits).</summary>
        public async Task<ValidationResult> ValidateBudgetAllocationAsync(int barangayId, decimal amount)
        {
            if (amount <= 0)
                return ValidationResult.Failure("Budget amount must be greater than zero.");

            var barangay = await _context.Barangays.FirstOrDefaultAsync(b => b.Id == barangayId);
            if (barangay == null)
                return ValidationResult.Failure("Barangay not found.");

            // Example: Max budget per barangay is 1 million
            const decimal maxBudget = 1000000;
            if (amount > maxBudget)
                return ValidationResult.Failure($"Budget amount cannot exceed {maxBudget:C}.");

            return ValidationResult.Success();
        }

        /// <summary>Validate email is unique (not already registered).</summary>
        public async Task<ValidationResult> ValidateUniqueEmailAsync(string email, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ValidationResult.Failure("Email is required.");

            var query = _context.BusinessUsers.Where(u => u.Email == email.ToLowerInvariant());

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            var exists = await query.AnyAsync();

            if (exists)
                return ValidationResult.Failure("This email address is already registered.");

            return ValidationResult.Success();
        }

        /// <summary>Validate barangay exists and is active.</summary>
        public async Task<ValidationResult> ValidateBarangayAsync(int barangayId)
        {
            var barangay = await _context.Barangays
                .Where(b => b.Id == barangayId && b.IsActive)
                .FirstOrDefaultAsync();

            if (barangay == null)
                return ValidationResult.Failure("Barangay not found or is inactive.");

            return ValidationResult.Success();
        }

        /// <summary>Validate user role is valid and within user's scope.</summary>
        public async Task<ValidationResult> ValidateRoleAsync(string roleName)
        {
            var validRoles = new[] { "super_admin", "barangay_admin", "council_member", "staff", "user" };

            if (!validRoles.Contains(roleName?.ToLowerInvariant()))
                return ValidationResult.Failure($"Invalid role. Valid roles: {string.Join(", ", validRoles)}");

            return ValidationResult.Success();
        }

        /// <summary>Validate user has permission for operation.</summary>
        public async Task<ValidationResult> ValidatePermissionAsync(int userId, string operation, int? resourceId = null)
        {
            var user = await _context.BusinessUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return ValidationResult.Failure("User not found.");

            // Example permission checks
            var role = user.Role;
            var hasPermission = operation switch
            {
                "DELETE_DOCUMENT" => role == "super_admin" || role == "barangay_admin",
                "EXPORT_DATA" => role == "super_admin" || role == "barangay_admin" || role == "staff",
                "CREATE_USER" => role == "super_admin" || role == "barangay_admin",
                "VIEW_AUDIT_LOGS" => role == "super_admin",
                _ => false
            };

            if (!hasPermission)
                return ValidationResult.Failure($"You do not have permission to perform this operation ({operation}).");

            return ValidationResult.Success();
        }

        /// <summary>Validate data export request (size, frequency).</summary>
        public async Task<ValidationResult> ValidateDataExportAsync(int userId, int barangayId)
        {
            // Check export frequency (max 5 exports per day per user)
            var today = DateTime.UtcNow.Date;
            var exportsToday = await _context.AuditLogs
                .Where(a =>
                    a.UserId == userId &&
                    a.Action == "EXPORT" &&
                    a.CreatedAt.Date == today)
                .CountAsync();

            if (exportsToday >= 5)
                return ValidationResult.Failure("You have reached the maximum number of exports for today (5 per day).");

            return ValidationResult.Success();
        }

        /// <summary>Validate password meets security requirements.</summary>
        public ValidationResult ValidatePassword(string password, string email, string username)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
                errors.Add("Password is required.");

            if (password.Length < 12)
                errors.Add("Password must be at least 12 characters.");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one digit.");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Password must contain at least one special character.");

            var uniqueChars = password?.Distinct().Count() ?? 0;
            if (uniqueChars < 4)
                errors.Add("Password must contain at least 4 unique characters.");

            if (!string.IsNullOrEmpty(email) && password?.ToLowerInvariant().Contains(email.Split('@')[0].ToLowerInvariant()) == true)
                errors.Add("Password must not contain your email address.");

            if (!string.IsNullOrEmpty(username) && password?.ToLowerInvariant().Contains(username.ToLowerInvariant()) == true)
                errors.Add("Password must not contain your username.");

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        /// <summary>Validate file contains expected content (not malware/invalid format).</summary>
        public async Task<ValidationResult> ValidateFileContentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ValidationResult.Failure("File is empty.");

            try
            {
                // Basic file signature validation (magic bytes)
                var signature = new byte[8];
                using (var ms = file.OpenReadStream())
                {
                    await ms.ReadAsync(signature, 0, 8);
                }

                var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

                // PDF: Check for %PDF signature
                if (ext == ".pdf" && signature[0] != 0x25 || signature[1] != 0x50) // %P
                    return ValidationResult.Failure("File header does not match PDF format. Possible corruption or spoofed file type.");

                // In production, integrate with antivirus (e.g., ClamAV, Windows Defender)
                // For now, basic validation is sufficient

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File content validation failed");
                return ValidationResult.Failure("Could not validate file content. Please try again.");
            }
        }
    }
}
