# Incident Response Examples & Runbooks

Real-world incident response scenarios with step-by-step procedures.

---

## Example 1: Failed Login Attack (Medium Severity)

**Scenario**: System detects 5+ failed login attempts from IP 203.0.113.45 within 10 minutes for user `user@barangay.local`

### Timeline

**T+0 minutes: Detection**
```
Event: 5 failed logins in 10 min from IP 203.0.113.45
Severity: Medium
Alert: Dashboard notification + email to security officer
Status: INCIDENT CREATED
```

**T+5 minutes: Classification**
- ✅ Is this legitimate? (user traveled, password reset)
- ✅ Is this attack? (attacker trying common passwords)
- ✅ Scope: 1 user account (isolated)

**Decision**: Classify as Medium (suspicious pattern, isolated)

### Immediate Response (Within 15 min)

**Step 1: Isolate**
```
Admin Action: 
  1. Go to Security Dashboard → Failed Login Attempts
  2. View IP 203.0.113.45 details
  3. Check: How many attempts? Which user accounts?
  4. If 1 user: Continue. If 5+ users: Escalate to High severity
```

**Step 2: Notify**
```
Admin Action:
  1. Send email to affected user: "Multiple login attempts detected"
  2. Suggestion: "Reset password or verify device"
  3. Include link to reset password or change security settings
  4. Request confirmation if legitimate
```

**Step 3: Document**
```
Incident Log Entry:
├─ Report ID: INC-20260509-001
├─ Time: 10:45 UTC
├─ Event: Failed login attack
├─ IP: 203.0.113.45
├─ User: user@barangay.local
├─ Attempts: 5
├─ Status: INVESTIGATION
└─ Assigned To: Security Officer
```

### Investigation (T+30 minutes to T+2 hours)

**Step 4: Analyze**
```
Questions to answer:
  □ What country/region is IP 203.0.113.45? (GeoIP lookup)
  □ Is it a datacenter / proxy / residential? (IP reputation check)
  □ Did same IP attack other users? (Query audit logs)
  □ Were any logins successful? (Check authentication logs)
  □ User's typical login IP? (View login history)
```

**Step 5: Query Audit Logs**
```csharp
// SQL or application query
SELECT * FROM AuditLog
WHERE 
  EventType = 'LOGIN_FAILURE'
  AND IpAddress LIKE '203.0.113.%'
  AND Timestamp > GETUTCDATE() - 30 -- Last 30 minutes
ORDER BY Timestamp DESC
```

**Step 6: Root Cause Analysis**
```
Possible causes:
  A) User password is weak / compromised elsewhere (reused password)
  B) Automated attack scanning for valid emails
  C) Malware on user's computer
  D) Attacker knew user's email (phishing)
  E) Company network compromise (internal attacker)
```

### Remediation (T+2 hours)

**Step 7: Apply Fixes**

**If Cause A (Weak Password)**:
```
Action:
  1. Email user: "Please reset password immediately"
  2. Force password change on next login
  3. If MFA enabled: Keep enabled
  4. If MFA disabled: Offer to enable (but don't force yet)
  5. Monitor next 24 hours for further attempts
```

**If Cause B (Email scanning)**:
```
Action:
  1. Block IP 203.0.113.45 at WAF/firewall (if available)
  2. Monitor for similar patterns from other IPs
  3. No user remediation needed (attack unsuccessful)
  4. Increase rate limiting if needed
```

**If Cause C (Malware)**:
```
Action:
  1. Email user: "Your computer may have malware"
  2. Recommend antivirus scan: Malwarebytes, Windows Defender full scan
  3. Reset password from clean device
  4. Enable MFA
  5. Revoke all existing sessions
  6. Change security questions / recovery email
```

**If Cause E (Internal Compromise)**:
```
Action:
  1. ESCALATE TO CRITICAL
  2. Isolate affected network segment
  3. Engage IT infrastructure team
  4. Begin forensics on internal systems
  5. Notify all users to change passwords
```

### Recovery (T+4-24 hours)

**Step 8: Verify**
```
Checklist:
  □ No further login attempts from IP 203.0.113.45
  □ User confirmed legitimate device/location
  □ Password changed successfully
  □ MFA working (if enabled)
  □ All user sessions valid
  □ User not reporting any data loss / modification
```

**Step 9: Monitor**
```
Continue monitoring for 7 days:
  □ Same IP no further attempts
  □ Same user no suspicious activity
  □ No related compromises on user account
```

### Post-Incident Review (T+48 hours)

**Step 10: Blameless Post-Mortem**

```
Discussion Topics:
  1. What happened?
     - 5 failed logins from 203.0.113.45
     - Attack lasted 10 minutes
     - User was not aware of attempts
     
  2. Why did it happen?
     - User's password was weak (from password reuse on public site)
     - No MFA enabled initially
     - Attack tool found email via public data breach
     
  3. What did we do right?
     - Alert fired within 10 minutes
     - Email sent to user immediately
     - No successful compromise
     
  4. What could we improve?
     - Enforce MFA for all users (currently admin-only)
     - Add notification on login from new device/location
     - Offer password strength feedback during login attempts
     
  5. Action items:
     - [ ] Email user: Suggest enabling MFA (Sep 2026)
     - [ ] Product team: Add risky login alerts (Sep 2026)
     - [ ] Security: Review similar patterns this week
```

**Step 11: Document & Close**
```
Final Report:
├─ Incident ID: INC-20260509-001
├─ Status: CLOSED
├─ Root Cause: Weak password + password reuse
├─ Data Compromised: No
├─ Action Items: 2 (see post-mortem)
├─ Lessons Learned: Enforce MFA for all users
└─ Review Date: Sep 2026
```

---

## Example 2: Unauthorized Data Access (High Severity)

**Scenario**: Audit log shows user from Barangay A accessed documents from Barangay B

### Timeline

**T+0 minutes: Detection**
```
Event: Cross-tenant access attempt
User: staff@barangay-a.local
Attempted Access: Document from Barangay B
Severity: HIGH (potential multi-tenant breach)
Alert: CRITICAL email + SMS to super admin
Status: INCIDENT CREATED - URGENT
```

### Immediate Response (Within 15 min)

**Step 1: Isolate User**
```
Admin Action (IMMEDIATE):
  1. Super admin logs in
  2. Go to Security Dashboard → Suspicious Activities
  3. Locate incident: Cross-tenant access
  4. Take action: PAUSE USER ACCOUNT
  5. Reason: "Suspected unauthorized access"
  6. Effect: User cannot login, all sessions invalidated
```

**Step 2: Contain Breach**
```
Questions:
  □ How many documents were accessed? (Query: WHERE UserId = X AND BarangayId != Y)
  □ When did access occur? (Get timestamp)
  □ Which barangay data was accessed? (Identify barangay B)
  □ How did this happen? (Technical cause: bug vs. deliberate)
```

**Step 3: Notify**
```
Immediate notifications:
  1. Email super admin: "Cross-tenant access detected, user paused"
  2. Email barangay B admin: "Your data may have been accessed by Barangay A user"
  3. Email barangay A admin: "User from your barangay attempted unauthorized access"
  4. Email security officer: Full details + investigation assigned
```

### Investigation (T+30 min to T+4 hours)

**Step 4: Forensic Analysis**

Query 1: What data was accessed?
```csharp
// Audit logs for this user + access outside their barangay
SELECT 
  a.Id, a.Timestamp, a.Action, a.TargetType, a.TargetId, a.Description
FROM AuditLog a
WHERE a.UserId = @staffUserId
  AND a.Timestamp > GETUTCDATE() - 24 -- Last 24 hours
  AND a.Action IN ('DOCUMENT_VIEW', 'DOCUMENT_DOWNLOAD', 'EXPORT')
ORDER BY a.Timestamp DESC
```

Query 2: Which barangay data?
```csharp
SELECT DISTINCT d.BarangayId, b.BarangayName, COUNT(*) as AccessCount
FROM Document d
  JOIN Barangay b ON d.BarangayId = b.Id
WHERE d.Id IN (
  SELECT CAST(JSON_VALUE(a.Description, '$.DocumentId') as INT)
  FROM AuditLog a
  WHERE a.UserId = @staffUserId AND a.Timestamp > ...
)
GROUP BY d.BarangayId, b.BarangayName
```

**Step 5: Root Cause Analysis**

Possible causes:
```
A) Authorization Filter Bug: Tenant filtering not applied correctly
B) Deliberate Breach: Malicious insider trying to access other barangay
C) Compromised Account: Attacker using stolen credentials
D) API Exploitation: Direct API call bypassing UI authorization
E) Database Direct Access: Attacker gained database login credentials
```

Investigation steps:
```
For Cause A (Bug):
  □ Check controller code: Is ApplyTenantFilter() called?
  □ Check database: Is data returned outside barangay scope?
  □ Reproduce: Can you access Barangay B document as Barangay A user?
  □ Test fix: Apply filter, re-test

For Cause B (Insider):
  □ Interview user: Why did they try to access other barangay?
  □ Check access pattern: Repeated or one-time?
  □ Check motivation: Do they have business reason to access?

For Cause C (Compromised):
  □ Check: When was password last changed?
  □ Check: MFA enabled? How was it bypassed?
  □ Check: Login from unusual location/device?
  □ Check: Any other users compromised?

For Cause D (API):
  □ Check API logs: Direct API calls made?
  □ Check: Attacker supplied different BarangayId in request?
  □ Check: API validation: Is BarangayId validated against user?

For Cause E (Database):
  □ Check: Database connection logs
  □ Check: Failed login attempts to database
  □ Check: Unusual SQL queries
```

### Remediation (T+4-8 hours)

**Step 6: Apply Fix**

**If Cause A (Bug)**:
```csharp
// BEFORE (buggy)
public IActionResult ViewDocument(int id)
{
    var doc = _context.Documents.FirstOrDefault(x => x.Id == id);
    return View(doc); // No tenant check!
}

// AFTER (fixed)
public IActionResult ViewDocument(int id)
{
    var barangayId = GetCurrentBarangayId();
    var doc = _context.Documents
        .ApplyTenantFilter(User) // ← ADDED
        .FirstOrDefault(x => x.Id == id);
    
    if (doc == null) return Forbidden();
    return View(doc);
}
```

Actions:
```
1. Fix code (add ApplyTenantFilter)
2. Run tests to verify fix works
3. Deploy to staging
4. Verify fix: Attempt same unauthorized access (should fail)
5. Deploy to production
6. Verify in production logs
```

**If Cause B (Insider)**:
```
Actions:
1. Interview staff member
2. If legitimate business need: Adjust permissions
3. If testing: Issue warning + retraining
4. If malicious: Escalate to HR/legal, revoke access
5. Document incident file
```

**If Cause C (Compromised)**:
```
Actions:
1. Force password reset
2. Enable MFA (if not already)
3. Send user: "Your account was compromised. Password reset required."
4. Revoke all active sessions
5. Monitor for next 7 days
6. Check if other users also compromised
```

**If Cause E (Database)**:
```
Actions:
1. ESCALATE to database administrator + IT security
2. Rotate database password immediately
3. Check database access logs for unauthorized logins
4. Reset any shared database accounts
5. Implement database-level encryption if not present
6. Engage incident response team
```

### Recovery (T+8-24 hours)

**Step 7: Verify & Restore**

```
Checklist:
  □ Bug fix deployed + verified working
  □ User account password changed
  □ MFA re-verified
  □ All affected data identified
  □ No ongoing access attempts from same user
  □ Notified all affected barangays
  □ Updated access logs
```

**Step 8: Re-enable User (if appropriate)**

```
If Cause A (Bug, not user's fault):
  1. Admin tests user can login and access correct barangay
  2. Send email: "Your account is active. If you see any issues, let us know."
  3. Resume monitoring

If Cause B/C/E (User or system compromise):
  1. Do NOT restore yet
  2. Continue investigation
  3. Restore only after resolution confirmed
```

### Post-Incident Review (T+48 hours)

**Step 9: Post-Mortem**

```
1. What happened?
   - Staff member from Barangay A viewed document from Barangay B
   - Root cause: [A/B/C/D/E - determined in investigation]

2. Impact?
   - Data accessed: [List documents/fields]
   - Data modified: None (read-only access)
   - Scope: Barangay B data only

3. What did we do right?
   - Alert fired immediately
   - User paused quickly
   - Investigation thorough

4. What could we improve?
   - Add real-time cross-tenant access alerts
   - Implement database-level access controls
   - Automated tenancy validation in all queries
   - Regular security code reviews

5. Action items:
   - [ ] Add ApplyTenantFilter to BaseAppController (auto-apply)
   - [ ] Create security code review process
   - [ ] Add unit tests for tenant isolation
   - [ ] Train dev team on multi-tenant patterns
```

**Step 10: Document & Close**

```
Final Report:
├─ Incident ID: INC-20260509-002
├─ Status: CLOSED
├─ Root Cause: [Determined]
├─ Data Compromised: [Barangay B documents - READ ONLY]
├─ Impact: LOW
├─ Action Items: 4 (see post-mortem)
├─ Code Changes: 1 pull request merged
└─ Follow-up Date: Sep 2026
```

---

## Example 3: Audit Log Integrity Mismatch (Critical Severity)

**Scenario**: System detects audit log hash mismatch - tamper attempt detected

### Response (Emergency Protocol)

**T+0: CRITICAL ALERT**
```
Event: Audit log integrity mismatch
Record ID: 12345
Expected Hash: abc123...
Actual Hash: xyz789...
Severity: CRITICAL - SUSPECTED TAMPERING
Alert: SMS + Email + Phone call to super admin
```

### Immediate Actions (T+0-15 min)

**Step 1: Freeze System**
```
Decision: Can system continue operating with audit log compromise?
Options:
  A) Continue with caution (if non-critical data affected)
  B) Degrade functionality (disable exports, limit operations)
  C) Go offline (worst case - full shutdown + forensics)

Typical action: Option A → Investigate while operational
If encryption keys compromised: Option C → Immediate offline
```

**Step 2: Identify Scope**
```
Questions:
  □ Which record was tampered? (ID 12345 - which operation?)
  □ How many records affected? (Query: WHERE HashVerificationFailed = 1)
  □ When was tampering? (Timestamp of record)
  □ What data is inconsistent? (Before/After values)
```

**Step 3: Suspect Investigation**
```
Questions:
  □ Who had access to database? (Database login audit)
  □ Were credentials compromised? (Check failed login attempts)
  □ Is this internal or external attack? (Source IP, access pattern)
  □ Other systems compromised? (Check other services)
```

### Forensic Analysis (T+15 min - 4 hours)

**Step 4: Verify Breach**

```csharp
// Check audit log integrity
SELECT 
  Id, 
  PreviousHash, 
  Hash, 
  Timestamp, 
  Action, 
  UserId,
  Description
FROM AuditLog
WHERE HashVerificationFailed = 1
ORDER BY Timestamp DESC
```

**Step 5: Determine Impact**

```
If tampered record is:
  □ Login attempt: Low impact (attacker might hide tracks)
  □ Configuration change: HIGH impact (system could be compromised)
  □ Data deletion: CRITICAL (evidence of breach deleted)
  □ User role change: CRITICAL (attacker escalated privileges)
```

### Recovery (T+4-8 hours)

**Step 6: Rollback & Investigate**

```
Actions:
  1. Restore database from clean backup (pre-tampering)
  2. Identify time range of tampering (from audit log)
  3. Re-create legitimate audit log entries
  4. Investigate what happened during tampering window
  5. Check data integrity: Were any records corrupted?
  6. Deploy forensics logs
```

**Step 7: Security Hardening**

```
Immediate actions:
  □ Rotate all database credentials
  □ Rotate encryption keys
  □ Enable database-level audit logging (if not enabled)
  □ Implement read-only audit log replica (immutable)
  □ Add real-time tamper detection
  □ Force password reset for all admins
```

---

## Quick Reference: Incident Severity & SLA

| Severity | Examples | Response Time | On-Call Required |
|----------|----------|-------------------|-----------------|
| **Critical** 🔴 | Data breach, system down, audit tampering | 15 min | YES - immediate |
| **High** 🟠 | Unauthorized access, account compromise | 1 hour | YES - within 1h |
| **Medium** 🟡 | Suspicious patterns, failed validations | 4 hours | Business hours |
| **Low** 🟢 | Info events, normal activity | 24 hours | No |

---

## Contacts & Escalation

**Security Officer**: security@jasmineIT15.local  
**Emergency Line**: [Phone number for on-call]  
**CEO/Management**: [Email for critical incidents]  
**IT Director**: [Contact info]

---

**Document Version**: 1.0  
**Last Updated**: May 9, 2026  
**Next Review**: August 9, 2026
