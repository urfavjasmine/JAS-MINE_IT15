namespace JAS_MINE_IT15.Models
{
    public class PasswordResetRequestViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Status { get; set; } = "Pending"; // Pending / Approved / Completed / Rejected
        public string Requested { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
