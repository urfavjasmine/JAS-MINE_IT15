namespace JAS_MINE_IT15.Models
{
    public class PolicyItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        // draft | pending | approved | rejected
        public string Status { get; set; } = "draft";

        public string LastUpdated { get; set; } = "";
        public string Author { get; set; } = "";
        public string Version { get; set; } = "1.0";
    }
}
