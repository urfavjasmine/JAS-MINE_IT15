namespace JAS_MINE_IT15.Models
{
    public class PracticeItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Barangay { get; set; } = "";
        public string DateAdded { get; set; } = "";
        public decimal Rating { get; set; }
        public int Implementations { get; set; }
        public bool Featured { get; set; }
    }
}
