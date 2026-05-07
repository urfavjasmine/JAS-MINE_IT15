using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace JAS_MINE_IT15.Models
{
    public class LogItem
    {
        public string Id { get; set; } = "";
        public string Timestamp { get; set; } = ""; // "yyyy-MM-dd HH:mm:ss"
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public string Module { get; set; } = "";
        public string Target { get; set; } = "";
        public string Ip { get; set; } = "";
    }
s
    public class AuditLogsViewModel
    {
        public List<LogItem> Logs { get; set; } = new();

        public string SearchQuery { get; set; } = "";
        public string ModuleFilter { get; set; } = "all";
        public string ActionFilter { get; set; } = "all";

        // Total count before filtering (all logs retrieved from DB)
        public int TotalCount { get; set; } = 0;

        public List<string> Modules => Logs.Select(l => l.Module).Distinct().OrderBy(x => x).ToList();

        // Stats
        public int TotalEntries => Logs.Count;
        public int Approvals => Logs.Count(l => new[] { "Approve", "Approved" }.Contains(l.Action));
        public int Creations => Logs.Count(l => new[] { "Create", "Created", "Upload", "Uploaded", "Submit", "Submitted", "Post", "Posted" }.Contains(l.Action));
        public int RejectDelete => Logs.Count(l => new[] { "Reject", "Rejected", "Delete", "Deleted", "Cancel" }.Contains(l.Action));
    }
}
