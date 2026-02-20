using System;
using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class LessonsLearnedViewModel
    {
        // Permissions
        public bool CanSubmit { get; set; }
        public bool CanModify { get; set; }
        public bool CanArchive { get; set; }

        // Stats
        public int TotalLessons { get; set; }
        public int RecentLessons { get; set; }
        public int ArchivedLessons { get; set; }

        // Filters
        public string SearchQuery { get; set; } = "";
        public string DateFilter { get; set; } = "";
        public string ArchiveStatus { get; set; } = "active";
        public List<string> AvailableDates { get; set; } = new();

        // TODO: Load from database
        public List<LessonRow> Lessons { get; set; } = new();

        // Filter options (from database or static list)
        public List<string> ProjectTypes { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Problem { get; set; } = "";
        public string ActionTaken { get; set; } = "";
        public string Result { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public DateTime DateRecorded { get; set; }
        public string Project { get; set; } = "";
        public string Summary { get; set; } = "";
        public string SubmittedBy { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";  // approved, pending, draft
        public bool IsArchived { get; set; }
        public List<string> Tags { get; set; } = new();
        public int Likes { get; set; }
        public int Comments { get; set; }
    }
}
