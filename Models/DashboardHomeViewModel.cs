using System;
using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class DashboardHomeViewModel
    {
        public string Role { get; set; } = "super_admin";
        public string RoleLabel { get; set; } = "Super Admin";
        public int? BarangayID { get; set; }
        public string BarangayName { get; set; } = "";

        // Top stats
        public int TotalDocuments { get; set; }
        public int ActivePolicies { get; set; }
        public int LessonsLearned { get; set; }
        public int BestPractices { get; set; }

        // Recent activity 
        public List<RecentActivityItem> RecentActivity { get; set; } = new();

        // Analytics (Super Admin or Barangay Admin)
        public List<MonthlyActivityRow> MonthlyActivity { get; set; } = new();
        public List<ModuleUsageRow> ModuleUsage { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string Action { get; set; } = "";
        public string Item { get; set; } = "";
        public string User { get; set; } = "";
        public DateTime Date { get; set; }
        public string Status { get; set; } = ""; 
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
