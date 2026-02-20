using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public class RepoDocument
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string TagsCsv { get; set; } = ""; // store tags as "a, b, c"
        public string UploadedBy { get; set; } = "";
        public string Date { get; set; } = ""; // yyyy-MM-dd
        public string Status { get; set; } = "draft"; // draft/pending/approved/rejected
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public class KnowledgeRepositoryViewModel
    {
        public string SearchQuery { get; set; } = "";
        public string SelectedCategory { get; set; } = "";
        public string SelectedStatus { get; set; } = "all";

        // TODO: Load categories from database
        public List<string> Categories { get; set; } = new();

        public List<RepoDocument> Documents { get; set; } = new();

        public bool CanUpload { get; set; }
        public bool CanApprove { get; set; }

        // For modal/form
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
