using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public class SharedDocumentItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public int SharedById { get; set; }
        public string SharedByName { get; set; } = "";
        public int DownloadCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class SharedDocumentsViewModel
    {
        public List<SharedDocumentItem> Documents { get; set; } = new();
        public string SearchQuery { get; set; } = "";

        // Stats
        public int TotalDocuments => Documents.Count;
        public int TotalDownloads => Documents.Sum(d => d.DownloadCount);

        // For Create/Edit modal
        public SharedDocumentItem? EditingDocument { get; set; }
        public bool IsEditMode { get; set; }

        // Permission flags
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanArchive { get; set; }
    }
}
