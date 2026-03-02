using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class SettingsViewModel
    {
        // Tab
        public string Tab { get; set; } = "general";

        // Profile
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = "";

        [StringLength(200, ErrorMessage = "Barangay name cannot exceed 200 characters.")]
        public string Barangay { get; set; } = "";
        public string Language { get; set; } = "en"; // en / fil

        // Notifications
        public bool NotifApprovals { get; set; } = true;
        public bool NotifPolicyUpdates { get; set; } = true;
        public bool NotifSubmissions { get; set; } = true;
        public bool NotifAnnouncements { get; set; } = false;
        public bool NotifReplies { get; set; } = false;

        // Security
        public bool TwoFaEnabled { get; set; } = false;

        // System
        public bool MaintenanceMode { get; set; } = false;
        public string SessionTimeout { get; set; } = "30"; // 15/30/60
        public string DocFormat { get; set; } = "pdf";     // pdf/docx

        // UI messages
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
