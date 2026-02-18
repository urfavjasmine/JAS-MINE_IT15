using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class LessonsLearnedViewModel
    {
        // Permissions
        public bool CanSubmit { get; set; }

        // TODO: Load from database
        public List<LessonRow> Lessons { get; set; } = new();

        // Filter options (from database or static list)
        public List<string> ProjectTypes { get; set; } = new();
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Project { get; set; } = "";
        public string Summary { get; set; } = "";
        public string SubmittedBy { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";  // approved, pending, draft
        public List<string> Tags { get; set; } = new();
        public int Likes { get; set; }
        public int Comments { get; set; }
    }
}
