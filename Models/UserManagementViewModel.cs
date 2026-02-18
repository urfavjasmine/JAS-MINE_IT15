using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public enum UserRole
    {
        super_admin,
        barangay_admin,
        barangay_secretary,
        barangay_staff,
        council_member
    }

    public class UserItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public UserRole Role { get; set; }
        public string Status { get; set; } = "active"; // active / inactive
        public string Barangay { get; set; } = "";
    }

    public class UserManagementViewModel
    {
        public List<UserItem> Users { get; set; } = new();

        public string SearchQuery { get; set; } = "";
        public string RoleFilter { get; set; } = "all";

        // Stats
        public int TotalUsers => Users.Count;
        public int ActiveUsers => Users.Count(u => u.Status == "active");
        public int InactiveUsers => Users.Count(u => u.Status == "inactive");
        public int BarangayCount => Users.Select(u => u.Barangay).Distinct().Count();

        // Role labels for dropdowns
        public Dictionary<string, string> RoleLabels { get; } = new()
        {
            { "super_admin", "Super Admin" },
            { "barangay_admin", "Barangay Administrator" },
            { "barangay_secretary", "Barangay Secretary" },
            { "barangay_staff", "Barangay Staff" },
            { "council_member", "Council Member" }
        };

        // Helper method to get role display label
        public static string GetRoleLabel(UserRole role) => role switch
        {
            UserRole.super_admin => "Super Admin",
            UserRole.barangay_admin => "Barangay Administrator",
            UserRole.barangay_secretary => "Barangay Secretary",
            UserRole.barangay_staff => "Barangay Staff",
            UserRole.council_member => "Council Member",
            _ => "User"
        };
    }
}
