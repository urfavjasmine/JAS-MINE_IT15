# Access Control Matrix (RBAC)

Complete role-based access control (RBAC) matrix for JAS-MINE_IT15 system.

---

## System Features by Role

| Feature | Guest | Staff | Council Member | Barangay Admin | Super Admin |
|---------|-------|-------|---|---|---|
| **AUTHENTICATION** |
| View Landing Page | ✅ | ✅ | ✅ | ✅ | ✅ |
| User Registration | ✅ | ❌ | ❌ | ❌ | ❌ |
| Login | ✅ | ✅ | ✅ | ✅ | ✅ |
| Password Reset | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Login History | ❌ | ✅ Own | ❌ | ✅ Barangay | ✅ All |
| Enable/Disable MFA | ❌ | ✅ Own | ✅ Own | ✅ Own | ✅ All |
| View Recovery Codes | ❌ | ✅ Own | ✅ Own | ✅ Own | ✅ All |
| **DASHBOARD** |
| Home/Landing Page | ✅ | ✅ | ✅ | ✅ | ✅ |
| Personal Dashboard | ❌ | ✅ | ✅ | ✅ | ✅ |
| Barangay Dashboard | ❌ | ❌ | ✅ Read-Only | ✅ | ✅ |
| System Dashboard | ❌ | ❌ | ❌ | ❌ | ✅ |
| Security Dashboard | ❌ | ❌ | ❌ | ❌ | ✅ |
| **DOCUMENTS & FILES** |
| Upload Document | ❌ | ✅ Own | ❌ | ✅ Any | ✅ Any |
| View Own Documents | ❌ | ✅ | ✅ Barangay | ✅ Barangay | ✅ |
| View All Documents | ❌ | ❌ | ✅ Barangay | ✅ Barangay | ✅ All |
| Edit Document | ❌ | ✅ Own | ❌ | ✅ Own/Barangay | ✅ |
| Delete Document | ❌ | ✅ Own | ❌ | ✅ Own/Barangay | ✅ |
| Share Document | ❌ | ✅ Own | ❌ | ✅ Own/Barangay | ✅ |
| Download Document | ❌ | ✅ Own | ✅ Barangay | ✅ Barangay | ✅ |
| Export Documents | ❌ | ✅ Own (Logged) | ✅ Barangay (Logged) | ✅ Barangay (Logged) | ✅ All (Logged) |
| **REPORTS & ANALYTICS** |
| View Reports | ❌ | ✅ Own | ✅ Barangay | ✅ Barangay | ✅ All |
| Generate Reports | ❌ | ✅ Own | ❌ | ✅ Barangay | ✅ All |
| Schedule Reports | ❌ | ❌ | ❌ | ❌ | ✅ |
| Export Report | ❌ | ✅ Own (Logged) | ✅ Barangay (Logged) | ✅ Barangay (Logged) | ✅ All (Logged) |
| **AUDIT & COMPLIANCE** |
| View Audit Logs | ❌ | ❌ | ❌ | ❌ | ✅ Read-Only |
| Export Audit Report | ❌ | ❌ | ❌ | ❌ | ✅ (Logged) |
| View Security Events | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Compliance Reports | ❌ | ❌ | ❌ | ❌ | ✅ |
| **USER & SUBSCRIPTION MANAGEMENT** |
| View Own Profile | ❌ | ✅ | ✅ | ✅ | ✅ |
| Edit Own Profile | ❌ | ✅ | ✅ | ✅ | ✅ |
| View Subscription Status | ❌ | ✅ | ❌ | ✅ | ✅ |
| Manage Subscription | ❌ | ✅ Own | ❌ | ✅ Own Barangay | ✅ |
| Upgrade/Downgrade Plan | ❌ | ✅ Own | ❌ | ✅ Own Barangay | ✅ |
| Manage Users (Create/Edit) | ❌ | ❌ | ❌ | ✅ Barangay Only | ✅ |
| Delete User Account | ❌ | ❌ | ❌ | ❌ | ✅ |
| Reset User Password | ❌ | ❌ | ❌ | ✅ Barangay Only | ✅ |
| **ADMINISTRATION** |
| System Settings | ❌ | ❌ | ❌ | ❌ | ✅ Read-Write |
| Security Settings | ❌ | ❌ | ❌ | ❌ | ✅ Read-Write |
| Password Policies | ❌ | ❌ | ❌ | ❌ | ✅ Read-Write |
| Encryption Settings | ❌ | ❌ | ❌ | ❌ | ✅ Read-Write |
| Manage Roles | ❌ | ❌ | ❌ | ❌ | ✅ |
| View System Health | ❌ | ❌ | ❌ | ❌ | ✅ |
| Rotate Encryption Keys | ❌ | ❌ | ❌ | ❌ | ✅ |
| Database Backup/Restore | ❌ | ❌ | ❌ | ❌ | ✅ |
| Seed Test Data | ❌ | ❌ | ❌ | ❌ | ✅ Dev Only |

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Allowed |
| ❌ | Denied |
| Own | User's own resources only |
| Barangay | Resources from user's barangay only |
| Read-Only | View-only access, no modification |
| Logged | Action recorded in audit trail |

---

## Authorization Rules

### 1. Tenant Isolation (Multi-tenancy)
- Automatically enforced by [TenantQueryExtensions](Services/TenantQueryExtensions.cs)
- Queries filtered by `BarangayId` (barangay_admin scope)
- Barangay admins cannot view other barangays' data
- Staff see only assigned resources + personal data

### 2. Ownership Rule
- Users can only modify their own resources
- Exception: Admins can modify within their scope (same barangay or system-wide)
- Example:
  - Staff member can edit their own documents
  - Barangay admin can edit staff documents in their barangay
  - Super admin can edit any document

### 3. Role Hierarchy
```
Super Admin (top)
    ↓
Barangay Admin (middle)
    ↓
Council Member / Staff (lower)
    ↓
Guest (no auth)
```
- Higher roles can manage lower roles
- Lower roles cannot elevate themselves
- Role assignment requires admin action

### 4. MFA Requirement for Admin Actions
- Admin-level operations (user create/delete, role changes) require MFA verification
- "Recent" verification = within 30 minutes
- Prevents unauthorized account takeover

### 5. Scope-Based Filtering
```csharp
// Automatically applied by BaseAppController
var barangayId = GetCurrentBarangayId(); // From claims
if (!IsSuperAdmin() && barangayId.HasValue)
{
    query = query.Where(x => x.BarangayId == barangayId.Value);
}
```

---

## Implementation Details

### How Authorization is Enforced

1. **Attribute-Based** (Controllers):
```csharp
[Authorize]
[RequireRoles("super_admin", "barangay_admin")]
public IActionResult ManageUsers() { }
```

2. **Filter-Based** (Reusable):
```csharp
[DenyViewOnlyAttribute]
[RequireRoles("super_admin", "barangay_admin")]
public IActionResult CreateDocument() { }
```

3. **Helper Methods** (BaseAppController):
```csharp
protected bool IsAdminRole() => User.IsInRole("super_admin") || User.IsInRole("barangay_admin");
protected int? GetCurrentBarangayId() => /* extracts from claims */
```

4. **Query Filtering** (Automatic):
```csharp
// In controllers, use TenantQueryExtensions
var documents = _context.Documents.ApplyTenantFilter(User).ToList();
```

### Fine-Grained Control Example

```csharp
// Allow POST only for super_admin
[HttpPost]
[RequireRoles("super_admin")]
public async Task<IActionResult> CreateUser(CreateUserViewModel model)
{
    // Only super_admin reaches here
    return await _userService.CreateAsync(model);
}

// Allow GET for barangay_admin viewing own barangay
[HttpGet]
[RequireRoles("super_admin", "barangay_admin")]
public async Task<IActionResult> ListUsers()
{
    var barangayId = GetCurrentBarangayId();
    var users = IsSuperAdmin()
        ? await _context.Users.ToListAsync()
        : await _context.Users.Where(u => u.BarangayId == barangayId).ToListAsync();
    
    return View(users);
}
```

---

## Privilege Escalation Prevention

| Scenario | Prevention |
|----------|-----------|
| User assigns themselves admin role | Only super_admin can assign roles (API permission) |
| User grants others admin access | Role assignment requires 2-factor approval |
| Admin keeps elevated privileges forever | MFA re-verification required every 30 min for admin ops |
| User access persists after deletion | Session invalidated on role removal |
| Stale tokens used after logout | Secure logout clears session + tokens |

---

## Testing & Verification

### Manual Testing Checklist
- [ ] Guest cannot access authenticated pages
- [ ] Staff member cannot see other staff members' documents
- [ ] Council member cannot delete documents (read-only)
- [ ] Barangay admin cannot see other barangays
- [ ] Super admin can see all data across barangays
- [ ] Audit log records all authorization denials
- [ ] MFA required before admin creates new user
- [ ] Session invalidated on logout
- [ ] Tokens expire after 20 minutes inactivity

### Automated Tests (Unit/Integration)
```csharp
[TestMethod]
public async Task BafistayCounsellorCannotDeleteDocument()
{
    var councilMember = new User { Role = "council_member" };
    var result = await _controller.DeleteDocument(1); // Should be 403 Forbidden
    Assert.AreEqual(403, result.StatusCode);
}
```

---

## Review & Maintenance

- **Review Frequency**: Quarterly (last: May 2026, next: August 2026)
- **Who Reviews**: Security Officer + Project Manager
- **Update Trigger**: Role changes, new features, incident lessons learned
- **Approval**: Project lead + security officer signature required
