# JAS-MINE_IT15 Security Policies & Procedures

## Executive Summary

This document outlines the security architecture, policies, and procedures for JAS-MINE_IT15, a barrangay information and data management system with built-in access controls, encryption, audit logging, and incident response capabilities.

**Last Updated**: May 9, 2026  
**Next Review**: May 9, 2027

---

## 1. AUTHENTICATION & PASSWORD POLICY

### Password Requirements

- **Minimum Length**: 12 characters (enforced)
- **Complexity**: Must contain ALL of the following:
  - Uppercase letters (A-Z)
  - Lowercase letters (a-z)
  - Digits (0-9)
  - Special characters (!@#$%^&*)
- **Uniqueness**: Minimum 4 unique characters required
- **Password History**: Cannot reuse last 5 passwords
- **Expiration**: No forced expiration (but reuse prevented by history)
- **Validation**: System blocks:
  - Common passwords (password, admin, qwerty, etc.)
  - Keyboard sequences (qwerty, asdf, etc.)
  - Repeated characters (aaaa)
  - Email or username within password

**Implementation**: Enforced by `StrongPasswordValidator` + ASP.NET Identity options in [Program.cs](Program.cs#L103-L107)

### Multi-Factor Authentication (MFA)

- **Scope**: Required for `super_admin` and `barangay_admin` roles
- **Method**: Email-based One-Time Password (OTP)
  - 6-digit code
  - 5-minute expiry
  - 3 resend attempts per session, 60-second cooldown
- **Implementation**: Uses ASP.NET Identity `GenerateTwoFactorTokenAsync` / `VerifyTwoFactorTokenAsync`
- **Trusted Device**: 30-day HttpOnly cookie allows skipping OTP on recognized devices
- **Recovery Codes**: 10 recovery codes generated, stored hashed, for account lockout
- **Location/Device Detection**: Alerts on suspicious sign-ins from new locations/devices

### Login & Lockout Policy

- **Failed Attempt Limit**: 5 failed attempts per user per IP
- **Lockout Duration**: 20 minutes after threshold exceeded
- **Rate Limiting**: 3 login attempts per IP per minute (global)
- **Account Lockout Alerts**:
  - Email notification to user
  - Admin dashboard alert (Critical severity)
  - Logged in audit trail
- **Progressive Throttling**: Exponential backoff (1s → 2s → 4s → 8s → 16s) on repeated failures

**Implementation**: 
- [AuthThrottleService](Services/AuthThrottleService.cs) - exponential backoff
- [SecurityAlertService](Services/SecurityAlertService.cs) - alerts to admins
- [Program.cs](Program.cs#L156-L179) - rate limiting policies

### Session Management

- **Timeout**: 20 minutes of inactivity (auto-logout)
- **Session Token**: Secure, HttpOnly cookie
  - `CookieSecurePolicy.Always` (production) / `SameAsRequest` (dev)
  - `SameSite=Lax` (CSRF protection)
- **Sliding Expiration**: Yes (resets on activity)
- **Concurrent Sessions**: One active session per user (new login invalidates old)
- **Logout**: Destroys session immediately, clears tokens

---

## 2. DATA PROTECTION POLICY

### Encryption Standards

| Data | Method | Key Size | Implementation |
|------|--------|----------|-----------------|
| **Passwords** | bcrypt (ASP.NET Identity) | - | Automatic via UserManager |
| **Sensitive Fields** (Phone, Email) | AES-256 | 256-bit | [AesFieldEncryptionService](Services/AesFieldEncryptionService.cs) |
| **Searchable Fields** (Email Hash) | HMAC-SHA256 | 256-bit | [DeterministicEncryptionService](Services/DeterministicEncryptionService.cs) |
| **Audit Log Integrity** | SHA-256 + Hash Chain | - | [AuditLogHashService](Services/AuditLogHashService.cs) |
| **Transport** | TLS 1.3+ | - | Enforced in production (web.config / Kestrel) |

### Sensitive Data Classification

| Level | Category | Examples | Protection | Access |
|-------|----------|----------|-----------|--------|
| **L1 - Top Secret** 🔴 | Credentials & Keys | Passwords, encryption keys, API tokens | Encrypted at rest, never in logs, masked in UI | Super admin only |
| **L2 - Confidential** 🟡 | Personal Information | Email, phone, document content, payment data | Encrypted fields, masked for non-admin | Admin + resource owner |
| **L3 - Internal** 🟢 | Activity Records | Audit logs, IP addresses, system metrics | Database (encrypted connection), access-controlled | Admin + authorized staff |
| **L4 - Public** 🔵 | System Info | Announcements, policies, help docs | Unencrypted | All users |

### Data Masking Rules

Applied automatically in UI/reports for non-admin users:

```
Email:       user@example.com  → con***@example.com
Phone:       09123456789      → 0912****
IP Address:  192.168.1.100    → 192.168.x.x
Credit Card: 1234567890123456 → ****-****-****-3456
```

**Implementation**: [DataMaskingHelper](Services/DataMaskingHelper.cs), [MaskedContactHelper](Services/MaskedContactHelper.cs)

### Data Retention Policy

- **Audit Logs**: 1 year (queryable), then archive to cold storage (7 years)
- **Login Logs**: 90 days
- **User Documents**: Per subscription end + 30 days grace period
- **Deleted User Data**: Soft delete (30 days retention), then permanent deletion
- **Session Logs**: 30 days
- **Error Logs**: 90 days
- **Backups**: Daily incremental (7 days), weekly full (30 days), quarterly offline copy (1 year)

**Auto-Cleanup**: [DataRetentionCleanupService](Services/DataRetentionCleanupService.cs) runs daily per [RetentionSettings](Models/RetentionSettings.cs)

### Encryption Key Management

**Field Encryption Key**:
```powershell
# Generate 32-byte (256-bit) key
$bytes = New-Object byte[] 32
(New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
[Convert]::ToBase64String($bytes)

# Store via user-secrets (dev) or environment variable (prod)
dotnet user-secrets set "FieldEncryption:Key" "<base64_key>"
```

**Key Rotation Policy**:
- Quarterly minimum (or after suspected compromise)
- Document old key for data re-encryption
- Test decryption before rotation
- Notify admins of completion

---

## 3. ACCESS CONTROL POLICY (RBAC)

### Role Definitions

| Role | Scope | Purpose | Data Access |
|------|-------|---------|-------------|
| **super_admin** | System-wide | System administrator, security officer | All data, all barangays |
| **barangay_admin** | Barangay | Barangay administrator | Own barangay only |
| **council_member** | Barangay (Read-only) | Council representative | View own barangay, read-only |
| **staff** | Limited | Data entry staff | Personal documents + assigned resources |
| **guest** | Public | Unauthenticated | Landing page, registration only |

### Access Control Matrix

| Feature | Guest | Staff | Council | B. Admin | Super Admin |
|---------|-------|-------|---------|----------|------------|
| **Authentication** | | | | | |
| Login | ✅ | ✅ | ✅ | ✅ | ✅ |
| Register | ✅ | ❌ | ❌ | ❌ | ❌ |
| Password Reset | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Login History | ❌ | ✅ Own | ❌ | ✅ Barangay | ✅ All |
| **Dashboard** | | | | | |
| Home/Landing | ✅ | ✅ | ✅ | ✅ | ✅ |
| Personal Dashboard | ❌ | ✅ | ✅ | ✅ | ✅ |
| Barangay Dashboard | ❌ | ❌ | ✅ R/O | ✅ | ✅ |
| System Dashboard | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Documents** | | | | | |
| Upload | ❌ | ✅ | ❌ | ✅ | ✅ |
| View Own | ❌ | ✅ | ✅ Barangay | ✅ | ✅ |
| View All (Barangay) | ❌ | ❌ | ✅ | ✅ | ✅ |
| Edit | ❌ | ✅ Own | ❌ | ✅ Own | ✅ |
| Delete | ❌ | ✅ Own | ❌ | ✅ Own | ✅ |
| Export | ❌ | ✅ Own | ✅ Logged | ✅ Logged | ✅ Logged |
| **Reporting** | | | | | |
| View Reports | ❌ | ✅ Own | ✅ Barangay | ✅ | ✅ |
| Generate Reports | ❌ | ✅ Own | ❌ | ✅ | ✅ |
| **Audit & Security** | | | | | |
| View Audit Logs | ❌ | ❌ | ❌ | ❌ | ✅ R/O |
| View Security Dashboard | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Compliance Reports | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Administration** | | | | | |
| Manage Users | ❌ | ❌ | ❌ | ✅ Own B. | ✅ |
| Manage Roles | ❌ | ❌ | ❌ | ❌ | ✅ |
| System Settings | ❌ | ❌ | ❌ | ❌ | ✅ |
| Security Policies | ❌ | ❌ | ❌ | ❌ | ✅ R/O |
| Seed Data | ❌ | ❌ | ❌ | ❌ | ✅ |

**Legend**: ✅ = Allowed | ❌ = Denied | R/O = Read-Only | Own = Own resource only

### Authorization Rules

1. **Tenant Isolation** (Multi-tenancy)
   - Queries automatically filtered by `BarangayId` via [TenantQueryExtensions](Services/TenantQueryExtensions.cs)
   - Barangay admins cannot see other barangays
   - Staff see only assigned resources

2. **Ownership Rule**
   - Users can modify only their own resources
   - Admin exception: admins can modify within their scope

3. **Role Hierarchy**
   - Super Admin > Barangay Admin > Council Member > Staff > Guest
   - Lower roles cannot assign/modify higher roles

4. **MFA Requirement**
   - Admin operations require recent MFA verification
   - "Recent" = within last 30 minutes

### Enforcement Mechanisms

- **Server-side** (Primary):
  - `[Authorize]` attribute on controllers
  - `[RequireRoles(...)]` custom attribute
  - [AuthorizationFilters.cs](Filters/AuthorizationFilters.cs) - `DenyViewOnlyAttribute`
  - [BaseAppController](Controllers/BaseAppController.cs) - role helpers
  - [TenantQueryExtensions](Services/TenantQueryExtensions.cs) - auto tenant filtering

- **Client-side** (UI Enhancement only, NOT security):
  - Hidden nav items for unauthorized actions
  - Disabled buttons
  - Conditional views (for UX only)

---

## 4. INCIDENT RESPONSE PLAN

### Severity Levels

| Severity | Examples | Response Time | Action |
|----------|----------|----------------|--------|
| **Critical** 🔴 | Data breach, ransomware, system compromise | 15 min | Offline, forensics, restore backup |
| **High** 🟠 | Unauthorized access, account takeover | 1 hour | Isolate, disable, investigate |
| **Medium** 🟡 | Suspicious patterns, failed validations | 4 hours | Monitor, analyze, document |
| **Low** 🟢 | Info events, normal activity | 24 hours | Log, review in digest |

### Detection

**Automated Alerts**:
- 5+ failed logins from same IP/user → Medium alert
- Account lockout → Medium alert
- Audit log integrity mismatch → Critical alert
- Configuration change detected → High alert
- Bulk data export → Medium alert (logged)
- Cross-tenant data access → Critical alert
- Repeated validation failures → Medium alert

**Manual Detection**:
- Admin review of Security Dashboard (weekly)
- Audit log review (weekly)
- User reports via Help/Support

### Response Procedures

#### Step 1: Detect & Alert

```
Event → System Rule → Email + Dashboard → Admin Notified
```

#### Step 2: Classify Severity

Admin assesses:
- Scope (single user vs. system-wide)
- Data involved (public vs. sensitive)
- Business impact (operational, financial, reputational)

#### Step 3: Immediate Actions

**Critical**:
1. Isolate affected systems (offline if necessary)
2. Disable compromised accounts
3. Engage incident response team
4. Begin forensic analysis

**High**:
1. Disable user account immediately
2. Invalidate all sessions
3. Notify affected users
4. Review audit logs (past 24 hours)

**Medium**:
1. Increase monitoring of affected user/IP
2. Document findings in incident ticket
3. Review related events
4. Plan follow-up investigation

**Low**:
1. Log event
2. Monitor for patterns
3. Review in weekly digest

#### Step 4: Remediation

- Apply security patches
- Reset credentials
- Rotate encryption keys (if compromised)
- Restore data from clean backup if corruption detected
- Update access controls

#### Step 5: Recovery

- Restore systems to clean state
- Re-enable with enhanced monitoring
- Verify data integrity
- Communicate all-clear to users

#### Step 6: Review (Post-Incident)

**Blameless Post-Mortem** (within 48 hours):
- What happened? (timeline)
- Why did it happen? (root cause)
- What could have prevented it? (preventive controls)
- What should we do differently? (process improvements)
- Team training on lessons learned

**Documentation**:
- Update incident log
- Update runbooks/procedures
- Create GitHub issue for preventive fix
- Share learnings with team

### Contact Information

- **Security Officer**: [Name] - security@jasmineIT15.local
- **Emergency**: Immediate email + phone call
- **Normal**: Email incident form
- **Disclosure**: security@jasmineIT15.local (responsible disclosure, 90 days to fix)

---

## 5. AUDIT LOGGING & MONITORING POLICY

### What Gets Logged

**Authentication Events**:
- Login attempts (success/failure, IP, timestamp)
- Password changes
- Password reset requests
- MFA enrollment/disable
- MFA verification (success/failure)
- Session timeout
- Account lockout

**Authorization Events**:
- Permission denied attempts
- Role assignment/removal
- Privilege escalation attempts
- Cross-tenant access attempts

**Data Operations**:
- CRUD operations on sensitive resources
- Data exports/bulk downloads
- Document uploads
- Report generation
- Data deletion

**Configuration Changes**:
- Security policy changes
- Encryption key rotation
- System settings modifications
- User role changes
- Integration setup/modification

**Security Incidents**:
- Repeated validation failures
- Rate limit hits
- Audit log integrity mismatches
- Failed encryption operations
- Certificate expiration warnings

### Log Content

Each log entry includes:

```json
{
  "timestamp": "2026-05-09T10:30:45Z",
  "severity": "High",
  "eventType": "LOGIN_FAILURE",
  "userId": 123,
  "userEmail": "user@example.com (masked in UI)",
  "ipAddress": "192.168.x.x (masked)",
  "userAgent": "[Browser info]",
  "action": "Login attempt",
  "targetId": null,
  "targetType": null,
  "description": "Failed login due to invalid credentials",
  "success": false,
  "barangayId": 5,
  "sessionId": "[hashed session ID]"
}
```

### Log Retention & Storage

- **Online (Queryable)**: 90 days in database
- **Archive (Cold)**: 1 year in secure backup
- **Regulatory Hold**: 7 years if required by compliance
- **Encryption**: All logs encrypted in transit (TLS), at rest (database encryption)
- **Access**: Only Super Admin can view (fully logged separately)

### Log Access Control

- **Super Admin**: Full access to all audit logs
- **Barangay Admin**: Read-only, barangay-scoped only
- **Auditor Role** (future): Read-only across all data, cannot delete/modify
- **All Access Logged**: Who viewed which logs, when, from which IP

### Monitoring & Alerting

| Alert | Trigger | Recipient | Channel |
|-------|---------|-----------|---------|
| **Critical** | Audit integrity mismatch, data breach | Super Admin | Email + SMS + Dashboard |
| **High** | Account lockout, unauthorized access | Super Admin | Email + Dashboard |
| **Medium** | Repeated failures, rate limit hits | Super Admin | Email (daily digest) |
| **Low** | Normal activity, info events | Dashboard only | Security Dashboard |

**Dashboard Real-time Metrics**:
- Failed login attempts (last 24h)
- Account lockouts (last 24h)
- MFA verification failures
- Cross-tenant access attempts
- Configuration changes
- Bulk exports
- System health status

---

## 6. CODE AUDITING & SECURITY STANDARDS

### Automated Auditing Tools

**SonarQube** - Static Code Analysis:
- Scans for OWASP Top 10 vulnerabilities
- Detects hardcoded credentials
- Identifies insecure crypto usage
- Flags SQL injection risks
- Reports code quality metrics
- Configuration: [sonarqube.properties](sonarqube.properties) (when added)

**Dependency Scanning** - NuGet Vulnerability Detection:
- Scans package dependencies for CVEs
- CI/CD Pipeline: `.github/workflows/security-ci.yml`
- Runs on each push/PR
- Fails build on critical vulnerabilities
- Report: `dotnet list --vulnerable --include-transitive`

**Manual Code Review**:
- Security-critical code (auth, encryption, audit) reviewed by 2+ developers
- Before merge to main branch
- Checklist-based (see below)

### Code Review Checklist

Before approving any PR, verify:

```
□ No hardcoded credentials (passwords, API keys, secrets)
□ All user inputs validated (length, type, format)
□ Parameterized queries used (no string concatenation for SQL)
□ Proper error handling (no stack trace leaks)
□ Sensitive data encrypted before storage/transmission
□ Authentication required for protected endpoints
□ Authorization checks implemented (role + ownership)
□ Audit logging present (action, user, timestamp, IP)
□ Rate limiting applied (if applicable)
□ CSRF tokens on all forms (AutoValidateAntiforgeryToken)
□ Output encoded (no XSS vulnerabilities)
□ Security headers set (HSTS, CSP, X-Frame-Options)
□ Tests pass (unit, integration, security)
□ No deprecated/unsafe APIs used
```

### Vulnerability Management

**Process**:
1. Vulnerability detected (SonarQube, Dependency Check, user report)
2. Severity classified (Critical, High, Medium, Low)
3. GitHub issue created with details
4. Assigned to developer + reviewed by security
5. Fix implemented with tests
6. Security review before merge
7. Deployed to production
8. Verified in production logs

**SLA**:
- **Critical**: Fix within 24 hours
- **High**: Fix within 1 sprint
- **Medium**: Fix within 2 sprints
- **Low**: Fix within next sprint or backlog

---

## 7. PRODUCTION DEPLOYMENT SECURITY

### HTTPS/TLS Requirements

- **Minimum Version**: TLS 1.3
- **Certificate**: Valid, signed by trusted CA
- **Renewal**: Automatic (60 days before expiry)
- **Mixed Content**: Blocked (no HTTP resources on HTTPS page)
- **HTTP Redirect**: All HTTP → HTTPS with 301 redirect

**Configuration**: 
```csharp
// In Program.cs (production only)
app.UseHsts(); // Strict-Transport-Security header
app.UseHttpsRedirection();
```

### Security Headers

| Header | Value | Purpose |
|--------|-------|---------|
| **Strict-Transport-Security** | max-age=31536000; includeSubDomains | Force HTTPS for 1 year |
| **X-Frame-Options** | DENY | Prevent clickjacking |
| **X-Content-Type-Options** | nosniff | Prevent MIME sniffing |
| **X-XSS-Protection** | 1; mode=block | Enable browser XSS filter |
| **Content-Security-Policy** | default-src 'self' | Prevent XSS/injection |
| **Referrer-Policy** | strict-origin-when-cross-origin | Control referrer info |

### Environment-Specific Hardening

**Development**:
- Debug mode enabled (for troubleshooting)
- User-secrets for credentials
- HTTP allowed (HTTPS not required)
- Detailed error messages (safe environment)

**Staging**:
- Debug mode disabled
- HTTPS enforced
- User-secrets or env vars for credentials
- Generic error messages
- Production logging enabled

**Production**:
- Debug mode DISABLED (app.Environment.IsProduction())
- HTTPS enforced
- Environment variables for credentials (never user-secrets)
- All error messages generic
- Full security headers enabled
- Rate limiting enabled
- Audit logging enabled
- Backup/restore procedures tested
- Monitoring/alerting active

### Database Security

**Connection String**:
- Encrypted (EF Core connection string protection)
- TLS enforced: `Encrypt=true;TrustServerCertificate=false` (prod)
- Database login (not Windows auth)
- Least-privilege principle (minimal permissions needed)

**Backups**:
- Daily incremental backups (7 days retention)
- Weekly full backups (30 days retention)
- Quarterly offline copy (1 year, encrypted, offline storage)
- Backup encryption: AES-256
- Restore tests: Monthly

**Audit Trail**:
- All data modifications logged (triggers on sensitive tables)
- Database login audit enabled
- Failed connection attempts logged
- Administrative actions logged

---

## 8. COMPLIANCE & STANDARDS ALIGNMENT

### Frameworks & Standards Applied

**OWASP Top 10** (2021):
- ✅ A01: Broken Access Control → RBAC + authorization filters
- ✅ A02: Cryptographic Failures → Field encryption + HTTPS + hashing
- ✅ A03: Injection → Parameterized queries + input validation
- ✅ A04: Insecure Design → Secure by default, threat modeling
- ✅ A05: Security Misconfiguration → Secure defaults, hardening checklist
- ✅ A06: Vulnerable/Outdated → Dependency scanning, CI/CD checks
- ✅ A07: Authentication Failures → MFA, strong passwords, session mgmt
- ✅ A08: Data Integrity Failures → Audit log integrity chain (SHA-256)
- ✅ A09: Logging/Monitoring Failures → Comprehensive audit logging
- ✅ A10: SSRF → Input validation, URL whitelisting

**NIST Cybersecurity Framework** (CSF):
- **Identify**: Asset inventory, data classification
- **Protect**: Access control, encryption, secure coding
- **Detect**: Audit logging, monitoring, alerts
- **Respond**: Incident response plan, procedures
- **Recover**: Backup/restore, disaster recovery plan

**CIS Controls** (v8):
- ✅ Access Control (RBAC, MFA, least privilege)
- ✅ Asset Management (inventory, versioning)
- ✅ Data Protection (encryption, masking, retention)
- ✅ Secure Coding (input validation, error handling)
- ✅ Logging/Monitoring (comprehensive audit trail)

### Data Privacy & User Rights

**Principles**:
- **Data Minimization**: Collect only necessary personal data
- **Transparency**: Users informed what data is collected
- **User Control**: Users can view/delete their own data
- **Security**: Data protected with encryption + access controls
- **Retention**: Data deleted after retention period

**User Rights**:
- Request personal data export (within 30 days)
- Request data deletion (purged after 60 days)
- Revoke session/device access (immediate)
- View login history
- View documents accessed by others (audit trail)

### Backup & Disaster Recovery

**RPO** (Recovery Point Objective): 1 hour max data loss acceptable  
**RTO** (Recovery Time Objective): 4 hours max downtime acceptable

**Strategy**:
- Daily incremental backups (to meet 1-hour RPO)
- Weekly full backups
- Quarterly offline copies (secure, encrypted storage)
- Annual DR drill (test full restore procedure)
- Documented runbook for emergency restore

**Backup Content**:
- Database (full)
- Encryption keys (separate, offline)
- Configuration files (appsettings, certificates)
- Application files (for reinstallation)

---

## 9. CONTACT & REVIEW

**Security Officer**: [Your Name] - security@jasmineIT15.local  
**Incident Reporting**: Submit incident form or email security officer  
**Vulnerability Disclosure**: security@jasmineIT15.local (responsible disclosure, 90-day fix window)  
**Policy Review Cycle**: Annually (last: May 2026, next: May 2027)

**Document Control**:
- **Version**: 1.0
- **Approved By**: [Cyril Loyd Tomas]
- **Effective Date**: May 9, 2026
- **Last Modified**: May 9, 2026

---

**Appendices**:
- Incident Response Examples: See [INCIDENT_RESPONSE_EXAMPLES.md](INCIDENT_RESPONSE_EXAMPLES.md)
- Data Classification Details: See [DATA_CLASSIFICATION.md](DATA_CLASSIFICATION.md)
- Access Control Details: See [ACCESS_CONTROL_MATRIX.md](ACCESS_CONTROL_MATRIX.md)
