namespace JAS_MINE_IT15.Models
{
    public class SharedDocumentsViewModel
    {
        public List<SharedDocItem> Documents { get; set; } = new();
        public int TotalDocuments { get; set; }
        public int TotalDownloads { get; set; }
        public string SearchQuery { get; set; } = "";
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanArchive { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SharedDocItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public string SharedByName { get; set; } = "";
        public int DownloadCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
