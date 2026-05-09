# Data Classification & Handling Guidelines

Comprehensive data classification system for JAS-MINE_IT15 with handling requirements.

---

## Classification Levels

### Level 1: Top Secret 🔴 (Highest Security)

**Examples**:
- Encryption keys (field encryption, backup keys)
- Database passwords
- API tokens (PayMongo, Turnstile)
- SMTP credentials
- User passwords (hashed only)
- Two-factor secret keys
- Recovery codes

**Storage**:
- Encrypted at rest (AES-256)
- Secure vault (e.g., Azure Key Vault, AWS Secrets Manager)
- Never in source code or config files
- Database: Hashed or encrypted columns only

**Transit**:
- TLS 1.3+ (HTTPS only)
- Never over HTTP
- Encrypted tunnel for key distribution

**Access Control**:
- Super admin only (extremely limited)
- Every access logged separately (audit trail)
- Multi-approve for critical operations (key rotation)
- Principle of least privilege

**Logging & Monitoring**:
- **Never** logged in plaintext or partial form
- Key rotation logged (who, when, why)
- Access attempts logged (success/failure)
- Alerts: Any attempt to export key

**Retention**:
- Keep until rotation, then destroy securely
- Backup keys: Offline secure storage (1 year)
- Access logs: 7 years (regulatory requirement)
- Backup encryption: Asymmetric (public key for backup, private key offline)

**Examples of Handling**:
```csharp
// ✅ CORRECT: Use secure storage
var apiKey = configuration["PayMongo:ApiKey"]; // From appsettings.json (empty in source)
// Actual value from: environment variable, user-secrets (dev), Key Vault (prod)

// ❌ INCORRECT: Hardcoded in source
const string ApiKey = "pk_live_abc123"; // SECURITY VIOLATION
```

---

### Level 2: Confidential 🟡 (Sensitive Data)

**Examples**:
- Personal Identifiable Information (PII)
  - Email addresses
  - Phone numbers
  - Personal documents
- Financial data
  - Invoice content
  - Payment information
  - Subscription details
- Medical/health information
- Contact information

**Storage**:
- Encrypted at rest for most PII (phone, email)
  - [AesFieldEncryptionService](Services/AesFieldEncryptionService.cs)
  - Transparent to application
- Hashed for searchable fields (email hash)
  - [DeterministicEncryptionService](Services/DeterministicEncryptionService.cs)
  - Enables search without decryption
- Database connection encrypted (TLS)

**Transit**:
- HTTPS/TLS 1.3+ only
- No HTTP access
- API responses encrypted (optional extra layer)

**Access Control**:
- Admin + resource owner
- Barangay admin: Own barangay data only
- Staff: Personal + assigned data only
- No bulk export without logging
- Viewing logged (audit trail)

**Logging & Monitoring**:
- **Masked** before storage
  - Email: `con***@domain.com`
  - Phone: `0912****`
  - In audit logs: `<masked>` instead of actual value
- [DataMaskingHelper](Services/DataMaskingHelper.cs) masks automatically
- Access/modification alerts for repeated violations
- Bulk export alert (High severity)

**Retention**:
- User documents: Subscription lifetime + 30 days after cancellation
- Audit logs containing PII: 1 year online, 7 years archive
- Deleted user data: 60-day grace period, then permanent deletion
- Backups: Same retention as live data

**Display Rules**:
```
Super Admin: See full data (email, phone, full content)
Barangay Admin: See full data for own barangay
Staff: See full own data, masked for others
```

**User Rights**:
- Request data export (within 30 days)
- Request data deletion (permanent within 60 days)
- View who accessed their data (audit trail)
- Revoke access to specific users

---

### Level 3: Internal 🟢 (Operational Data)

**Examples**:
- Audit logs (activities, timestamps)
- User IP addresses
- User-agent / browser info
- System metrics (uptime, performance)
- Configuration settings (non-sensitive)
- Error logs (sanitized)
- Application logs

**Storage**:
- Database (encrypted connection)
- Structured logging (Serilog)
- Log aggregation (e.g., ELK Stack if scale requires)
- Regular backups

**Transit**:
- HTTPS (TLS 1.3+) for transmission
- Internal network preferred for aggregation

**Access Control**:
- Admin + authorized staff (auditors)
- Barangay admin: Own barangay logs only
- Read-only for auditors (cannot modify/delete)
- Access to logs logged (separate audit trail)

**Logging & Monitoring**:
- Logged in [AuditLog table](Data/ApplicationDbContext.cs)
- Structured format (JSON-serializable)
- Timestamps with UTC timezone
- IP addresses masked (first 2 octets): `192.168.x.x`
- User-agent logged but not exposed in UI
- Alert: Bulk audit log download, repeated queries

**Retention**:
- Online: 90 days (queryable, indexed)
- Archive: 1 year (cold storage, searchable)
- Regulatory hold: 7 years (if compliance required)

**Sanitization Rules**:
- Remove stack traces (log only error message)
- Mask SQL queries (log intent, not full query)
- Mask file paths (first 2 levels: `/app/...`)
- Remove sensitive headers (Authorization, etc.)

---

### Level 4: Public 🔵 (Lowest Security)

**Examples**:
- System announcements
- Help documentation
- Security policies (this file)
- Public FAQ
- Terms of service
- System status page
- Feature descriptions

**Storage**:
- Any storage (database, files, CDN)
- No encryption required
- Publicly cacheable (CDN-safe)

**Transit**:
- HTTP or HTTPS (both acceptable)
- CDN delivery OK
- Public internet distribution OK

**Access Control**:
- All users (no authentication required)
- Search engines can index
- No access restrictions

**Logging & Monitoring**:
- Standard web analytics OK
- No special logging required
- Public availability metrics OK

**Retention**:
- Permanent (version history OK)
- Historical versions archived
- Deletion only on explicit request

---

## Data Lifecycle

```
Create → Classify → Protect → Use → Archive → Delete
```

### 1. Creation
- **Classify immediately** - Tag with Level 1-4
- **Document owner** - Who is responsible
- **Apply controls** - Encryption, access, logging
- **Store safely** - Right system per level

### 2. Classification (Ongoing)
- Review quarterly
- Downgrade if sensitive info removed
- Upscale if exposure increases
- Document reason for level

### 3. Protection (Applied)
- Encryption: [AesFieldEncryptionService](Services/AesFieldEncryptionService.cs)
- Access: [RBAC in AuthorizationFilters](Filters/AuthorizationFilters.cs)
- Masking: [DataMaskingHelper](Services/DataMaskingHelper.cs)
- Audit: [AuditService](Services/AuditService.cs)

### 4. Usage (Logged)
- All access logged
- Masking applied for non-admin users
- Exports tracked (Level 2 requires special approval)
- Data residency: Keep local (no third-party sharing without consent)

### 5. Archive (Cold Storage)
- Move Level 3-4 data to archive after retention (1+ year)
- Compress + encrypt archives
- Store offline or low-cost storage
- Maintain searchability (if required)

### 6. Deletion (Secure Erasure)
- **Standard delete**: Database delete, soft-delete for 60 days grace
- **Secure deletion**: Multi-pass overwrite (3x) then delete
- **Level 1 items**: Cryptographic erasure (destroy encryption key)
- **Compliance hold**: Keep per legal requirement (7 years)
- **Verification**: Restore backup to verify deletion worked

---

## Practical Examples

### Example 1: Email Address Classification & Handling

**Classification**: Level 2 (Confidential)

**Storage**:
```csharp
// Encrypted in database (AES-256)
public string Email { get; set; } // [Encrypted]

// Plus hash for searching
public string EmailHash { get; set; } // [DeterministicEncryption]
```

**Display (Role-based)**:
```
Super Admin: user@example.com (full)
Barangay Admin: user@example.com (full, if own barangay)
Staff: use***@example.com (masked, if different user)
```

**Logging**:
```csharp
// In audit log
_auditService.LogAsync(
    action: "Email Updated",
    description: "User email changed", // Masked: con***@domain.com
    targetId: userId
);
// Output: "UserEmail: <masked>" (actual email NOT stored)
```

**Retention**:
- Keep while user active
- 30 days after user deletion
- Permanent purge via secure deletion

---

### Example 2: Audit Log Classification & Handling

**Classification**: Level 3 (Internal)

**Storage**:
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; }
    public string Description { get; set; }
    public string IpAddress { get; set; } // Masked: 192.168.x.x
    public string? BeforeValue { get; set; } // Masked if sensitive
    public string? AfterValue { get; set; } // Masked if sensitive
}
```

**Retention**:
- 90 days online (full query capability)
- 1 year archive (search-only, cost-optimized)
- 7 years regulatory hold (if compliance required)

**Access**:
```csharp
// Only super_admin can view all
[Authorize]
[RequireRoles("super_admin")]
public IActionResult AuditLogs() { }

// Barangay admin can view own barangay only
var logs = _context.AuditLogs
    .ApplyTenantFilter(User) // Auto-filters by BarangayId
    .ToList();
```

---

### Example 3: Encryption Key Classification & Handling

**Classification**: Level 1 (Top Secret)

**Storage**:
```powershell
# Generate key (32 bytes = 256-bit AES)
$bytes = New-Object byte[] 32
(New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)

# Store securely - NEVER in appsettings.json
# Development: user-secrets
dotnet user-secrets set "FieldEncryption:Key" "$key"

# Production: Environment variable or Key Vault
$env:FieldEncryption__Key = "$key"
```

**Rotation**:
```
Every 90 days OR after suspected compromise
1. Generate new key
2. Re-encrypt all data with new key
3. Destroy old key (cryptographic erasure)
4. Verify decrypt works
5. Log rotation (who, when, why) in audit trail
```

**Backup**:
```
1. Export old key + decryption mapping (encrypted)
2. Store offline in secure vault
3. Keep for 1 year minimum
4. Document retrieval procedure
5. Test decrypt annually
```

---

## Handling Errors & Violations

### Data Breach Scenario

**If Level 2 data exposed unencrypted**:
1. ⏱️ Immediately disable affected accounts
2. ⏱️ Notify affected users (within 24 hours)
3. ⏱️ Launch investigation (determine scope, when, who)
4. ⏱️ Apply additional encryption (re-encrypt affected columns)
5. ⏱️ Inform security officer + legal team
6. ⏱️ Document & file incident report
7. ⏱️ Update controls to prevent recurrence

### Unauthorized Access

**If user accesses data above their level**:
1. ⏱️ Log in audit trail (automatic)
2. ⏱️ Alert security officer (High alert)
3. ⏱️ Investigate (intentional vs. bug)
4. ⏱️ If bug: Fix + re-deploy + verify
5. ⏱️ If intentional: Discipline per policy
6. ⏱️ If third-party compromise: Apply additional controls

---

## Compliance & Standards

- **GDPR**: Data minimization, user rights, retention, DPA
- **CCPA**: User rights to access/delete, opt-out
- **HIPAA** (if applicable): Encrypted PHI, audit trails, access logs
- **SOC 2**: Encryption, access control, monitoring, incident response

---

## Review & Updates

- **Last Review**: May 2026
- **Next Review**: August 2026 (quarterly)
- **Update Trigger**: New data type introduced, compliance change, incident lesson learned
- **Approver**: Security Officer + Project Lead
