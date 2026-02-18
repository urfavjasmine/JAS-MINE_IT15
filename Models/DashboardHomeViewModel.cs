using System;
using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class DashboardHomeViewModel
    {
        public string Role { get; set; } = "";
        public string RoleLabel { get; set; } = "";
        public int? BarangayID { get; set; }
        public string BarangayName { get; set; } = "";

        // Top stats
        public int TotalDocuments { get; set; }
        public int ActivePolicies { get; set; }
        public int LessonsLearned { get; set; }
        public int BestPractices { get; set; }

        // Recent activity 
        public List<RecentActivityRow> RecentActivity { get; set; } = new();

        // Dashboard announcements
        public List<DashboardAnnouncementRow> Announcements { get; set; } = new();

        // Analytics (Super Admin or Barangay Admin)
        public List<MonthlyActivityRow> MonthlyActivity { get; set; } = new();
        public List<ModuleUsageRow> ModuleUsage { get; set; } = new();
    }

    public class RecentActivityRow
    {
        public string Action { get; set; } = "";
        public string Item { get; set; } = "";
        public string User { get; set; } = "";
        public string Time { get; set; } = "";   // e.g., "5 min ago"
        public string Status { get; set; } = ""; // pending, approved, rejected
    }

    public class DashboardAnnouncementRow
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Priority { get; set; } = "info"; // info, important
    }

    public class MonthlyActivityRow
    {
        public string Month { get; set; } = ""; 
        public int Documents { get; set; }
        public int Policies { get; set; }
        public int Lessons { get; set; }
        public int Practices { get; set; } 
    }

    public class ModuleUsageRow
    {
        public string Name { get; set; } = ""; 
        public int Value { get; set; }         
    }
}
