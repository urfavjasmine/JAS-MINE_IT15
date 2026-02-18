namespace JAS_MINE_IT15.Models
{
    public class PaymentItem
    {
        public string Id { get; set; } = "";
        public string BarangayName { get; set; } = "";
        public string PlanName { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentDate { get; set; } = "";    // yyyy-MM-dd
        public string PaymentMethod { get; set; } = "";  // Cash/GCash/etc
        public string Status { get; set; } = "Paid";     // Paid | Pending | Failed
    }
}
