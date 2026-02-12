namespace JAS_MINE_IT15.Models
{
    public class SettingsViewModel
    {
        // Tab
        public string Tab { get; set; } = "general";

        // Profile
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
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
