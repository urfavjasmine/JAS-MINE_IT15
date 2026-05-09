# Input Validation & Error Handling Guide

Guide for developers on implementing input validation and error handling across the system.

---

## Table of Contents
1. [Using Custom Validation Attributes](#using-custom-validation-attributes)
2. [Using IValidationService](#using-ivalidationservice)
3. [Error Handling Best Practices](#error-handling-best-practices)
4. [Logging Validation Failures](#logging-validation-failures)
5. [Testing Validation](#testing-validation)

---

## Using Custom Validation Attributes

### Custom Attributes Available

Located in [Validations/CustomValidationAttributes.cs](Validations/CustomValidationAttributes.cs):

- `[ValidEmail]` - Email address validation (stricter than built-in)
- `[StrongPassword]` - 12+ chars, uppercase, lowercase, digit, special char, 4+ unique
- `[ValidPhoneNumber]` - Philippine phone number format
- `[ValidStringLength]` - String length range validation
- `[ValidFileExtension]` - File extension whitelist
- `[ValidRange]` - Numeric range validation

### Example: LoginViewModel

**BEFORE** (no validation):
```csharp
public class LoginViewModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}
```

**AFTER** (with validation):
```csharp
using JAS_MINE_IT15.Validations;
using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [ValidEmail(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StrongPassword(ErrorMessage = "Password must be at least 12 characters with uppercase, lowercase, digit, and special character")]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}
```

### Example: CreateUserViewModel

```csharp
public class CreateUserViewModel
{
    [Required(ErrorMessage = "First name is required")]
    [ValidStringLength(2, 50, ErrorMessage = "First name must be 2-50 characters")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [ValidEmail]
    public string Email { get; set; }

    [Required(ErrorMessage = "Phone is required")]
    [ValidPhoneNumber]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StrongPassword]
    public string Password { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; }

    [Required(ErrorMessage = "Barangay is required")]
    [ValidRange(1, int.MaxValue, ErrorMessage = "Please select a valid barangay")]
    public int BarangayId { get; set; }
}
```

### Example: DocumentUploadViewModel

```csharp
public class DocumentUploadViewModel
{
    [Required(ErrorMessage = "Please select a file")]
    [ValidFileExtension("pdf", "docx", "xlsx", ErrorMessage = "Only PDF, DOCX, and XLSX files are allowed")]
    public IFormFile File { get; set; }

    [Required(ErrorMessage = "Document title is required")]
    [ValidStringLength(3, 200)]
    public string Title { get; set; }

    [ValidStringLength(0, 500, ErrorMessage = "Description must not exceed 500 characters")]
    public string Description { get; set; }
}
```

---

## Using IValidationService

For business logic validation requiring database checks.

### Inject IValidationService

```csharp
public class DocumentsController : BaseAppController
{
    private readonly IValidationService _validationService;

    public DocumentsController(
        ApplicationDbContext context,
        IValidationService validationService) : base(context)
    {
        _validationService = validationService;
    }
}
```

### Validate Document Upload

```csharp
[HttpPost]
[RequireRoles("super_admin", "barangay_admin", "staff")]
public async Task<IActionResult> UploadDocument(DocumentUploadViewModel model)
{
    if (!ModelState.IsValid)
        return View(model); // ValidatePostModelFilter handles this

    // Business logic validation
    var uploadValidation = await _validationService.ValidateDocumentUploadAsync(model.File);
    if (!uploadValidation.IsValid)
    {
        ModelState.AddModelError("File", uploadValidation.Errors[0]);
        return View(model);
    }

    // Proceed with upload
    // ...
}
```

### Validate Email Uniqueness

```csharp
[HttpPost]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    // Check email is unique
    var emailValidation = await _validationService.ValidateUniqueEmailAsync(model.Email);
    if (!emailValidation.IsValid)
    {
        ModelState.AddModelError("Email", emailValidation.Errors[0]);
        return View(model);
    }

    // Proceed with registration
    // ...
}
```

### Validate Subscription Change

```csharp
[HttpPost]
[RequireRoles("barangay_admin")]
public async Task<IActionResult> ChangePlan(int barangayId, string newPlan)
{
    var validation = await _validationService.ValidateSubscriptionChangeAsync(barangayId, newPlan);
    if (!validation.IsValid)
    {
        return BadRequest(new { errors = validation.Errors });
    }

    // Process plan change
    // ...
}
```

### Validate Data Export

```csharp
[HttpPost]
public async Task<IActionResult> ExportDocuments(int barangayId)
{
    var exportValidation = await _validationService.ValidateDataExportAsync(User.GetUserId(), barangayId);
    if (!exportValidation.IsValid)
    {
        return StatusCode(429, new { message = exportValidation.Errors[0] }); // Too Many Requests
    }

    // Process export
    // ...
}
```

---

## Error Handling Best Practices

### ❌ DO NOT do this

```csharp
// ❌ Exposing exception details
try
{
    var user = _context.Users.FirstOrDefault(u => u.Id == id);
    return Ok(user);
}
catch (Exception ex)
{
    return BadRequest(ex.Message); // Leaks technical details!
}

// ❌ No error handling
var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
if (doc == null)
    return NotFound(); // No message, confusing for users

// ❌ Logging sensitive data
_logger.LogError($"Failed to process email {user.Email}"); // PII in logs!
```

### ✅ DO this instead

```csharp
// ✅ Generic error message with logging
try
{
    var user = _context.Users.FirstOrDefault(u => u.Id == id);
    if (user == null)
    {
        _logger.LogWarning("User {UserId} not found", id);
        return NotFound(new { message = "The requested resource was not found." });
    }
    return Ok(user);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Database query failed for user lookup");
    return StatusCode(500, new { message = "An error occurred. Please try again later." });
}

// ✅ Helpful error messages without technical details
public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException()
        : base("The document you requested could not be found or you don't have permission to access it.")
    {
    }
}

try
{
    var doc = _context.Documents
        .ApplyTenantFilter(User)
        .FirstOrDefault(d => d.Id == id)
        ?? throw new DocumentNotFoundException();
    
    return Ok(doc);
}
catch (DocumentNotFoundException ex)
{
    return NotFound(new { message = ex.Message });
}

// ✅ Masked logging
_logger.LogWarning("Failed to process email for user {UserId}", userId); // No PII
```

### Generic Error Response Structure

For API endpoints, always return consistent error format:

```json
{
  "success": false,
  "message": "Operation failed. Please try again.",
  "errors": [
    {
      "field": "email",
      "message": "Email already in use"
    }
  ],
  "timestamp": "2026-05-09T10:30:45Z"
}
```

**C# code**:
```csharp
return BadRequest(new
{
    success = false,
    message = "Validation failed",
    errors = new[] { new { field = "email", message = "Email already in use" } },
    timestamp = DateTime.UtcNow
});
```

---

## Logging Validation Failures

The [ValidatePostModelFilter](Filters/ValidatePostModelFilter.cs) automatically logs all POST validation failures with:
- User ID
- IP address
- Controller/Action
- Error details
- Timestamp

### View Validation Failure Logs

```csharp
// In your audit dashboard or log viewer
SELECT 
  Timestamp, 
  UserId, 
  IpAddress, 
  Description
FROM AuditLog
WHERE Action = 'VALIDATION_FAILURE'
  AND Timestamp > DATEADD(DAY, -7, GETUTCDATE())
ORDER BY Timestamp DESC
```

### Interpret Validation Patterns

Repeated validation failures from same IP/user may indicate:
- User confusion (normal)
- Brute force attack (malicious)
- API integration issues (technical)

Monitor dashboard alerts for:
- **10+ failures from same IP in 1 hour** → Potential attack
- **5+ failures from same user in 1 hour** → Possible account compromise
- **Repeated same field failure** → UI bug or integration error

---

## Testing Validation

### Unit Tests

```csharp
[TestClass]
public class ValidationTests
{
    private readonly IValidationService _validationService;

    [TestMethod]
    public void ValidatePassword_TooShort_ReturnsFalse()
    {
        var result = _validationService.ValidatePassword("Short1!", "user@test.com", "testuser");
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("12 characters")));
    }

    [TestMethod]
    public void ValidatePassword_NoSpecialChar_ReturnsFalse()
    {
        var result = _validationService.ValidatePassword("ValidPassword123", "user@test.com", "testuser");
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("special character")));
    }

    [TestMethod]
    public void ValidatePassword_ContainsEmail_ReturnsFalse()
    {
        var result = _validationService.ValidatePassword("TestUser@email123", "user@email.com", "testuser");
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("email")));
    }

    [TestMethod]
    public void ValidatePassword_Valid_ReturnsTrue()
    {
        var result = _validationService.ValidatePassword("ValidP@ssw0rd", "user@test.com", "testuser");
        Assert.IsTrue(result.IsValid);
    }
}
```

### Integration Tests

```csharp
[TestMethod]
public async Task ValidateDocumentUpload_ValidFile_ReturnsSuccess()
{
    var file = new FormFile(
        new MemoryStream(Encoding.UTF8.GetBytes("PDF content")),
        0,
        100,
        "file",
        "test.pdf");

    var result = await _validationService.ValidateDocumentUploadAsync(file);
    Assert.IsTrue(result.IsValid);
}

[TestMethod]
public async Task ValidateDocumentUpload_TooLarge_ReturnsFalse()
{
    var largeContent = new byte[60000000]; // 60 MB
    var file = new FormFile(
        new MemoryStream(largeContent),
        0,
        largeContent.Length,
        "file",
        "test.pdf");

    var result = await _validationService.ValidateDocumentUploadAsync(file);
    Assert.IsFalse(result.IsValid);
    Assert.IsTrue(result.Errors.Any(e => e.Contains("50 MB")));
}
```

### Manual Testing Checklist

- [ ] Fill form with empty fields → Shows required errors
- [ ] Enter invalid email → Shows email validation error
- [ ] Enter weak password (e.g., "Pass123") → Shows strength requirements
- [ ] Enter password containing username → Shows rejection
- [ ] Upload oversized file → Shows file size limit error
- [ ] Upload disallowed file type → Shows extension error
- [ ] Submit form → Check Serilog/audit logs for validation entry
- [ ] Check security dashboard → Validation failures appear in logs

---

## Common Pitfalls & Solutions

| Issue | Solution |
|-------|----------|
| Validation passes but data is invalid | Use IValidationService for business logic checks |
| User doesn't understand error message | Use ValidStringLength instead of generic [StringLength] for context |
| PII appears in logs | Use DataMaskingHelper or sanitized logging |
| Duplicate validation logic | Create custom attribute or IValidationService method |
| Client-side validation only (no server) | Always validate server-side, client validation is UX only |
| Stack trace shown to user | Use generic error response + log details server-side |

---

## References

- Custom Attributes: [Validations/CustomValidationAttributes.cs](Validations/CustomValidationAttributes.cs)
- Validation Service: [Services/IValidationService.cs](Services/IValidationService.cs)
- Validation Filter: [Filters/ValidatePostModelFilter.cs](Filters/ValidatePostModelFilter.cs)
- Security Policies: [SECURITY.md](SECURITY.md#5-audit-logging--monitoring-policy)
- Data Masking: [Services/DataMaskingHelper.cs](Services/DataMaskingHelper.cs)
