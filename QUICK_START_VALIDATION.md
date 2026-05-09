# Quick Integration Guide - 5 Minute Setup

Simple step-by-step guide to add the new validation to your existing controllers & ViewModels.

---

## Step 1: Add Validation Attributes to ViewModels (2 min)

### Before (No Validation)
```csharp
public class LoginViewModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

### After (With Validation)
```csharp
using System.ComponentModel.DataAnnotations;
using JAS_MINE_IT15.Validations; // ← ADD THIS

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [ValidEmail] // ← Uses custom attribute
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StrongPassword] // ← Uses custom attribute
    public string Password { get; set; }

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }
}
```

**Key Points**:
- `[Required]` - Built-in, ensures field not empty
- `[ValidEmail]` - Custom attribute from CustomValidationAttributes.cs
- `[StrongPassword]` - Custom attribute (12+ chars, uppercase, lowercase, digit, special)
- Error messages optional but recommended

---

## Step 2: Add IValidationService to Controller (1 min)

### Before (No Business Logic Validation)
```csharp
public class UsersController : BaseAppController
{
    public UsersController(ApplicationDbContext context) : base(context) { }
}
```

### After (With Service Injection)
```csharp
using JAS_MINE_IT15.Services; // ← ADD THIS

public class UsersController : BaseAppController
{
    private readonly IValidationService _validationService; // ← ADD THIS

    public UsersController(
        ApplicationDbContext context,
        IValidationService validationService) // ← ADD THIS PARAMETER
        : base(context)
    {
        _validationService = validationService; // ← ADD THIS
    }
}
```

---

## Step 3: Use IValidationService in Action Methods (2 min)

### Email Uniqueness Check
```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserViewModel model)
{
    if (!ModelState.IsValid)
        return View(model); // ValidatePostModelFilter logs this automatically

    // ← ADD THIS BLOCK:
    var emailValidation = await _validationService.ValidateUniqueEmailAsync(model.Email);
    if (!emailValidation.IsValid)
    {
        ModelState.AddModelError("Email", emailValidation.Errors[0]);
        return View(model);
    }

    // Proceed with user creation...
    return RedirectToAction("Index");
}
```

### Document Upload Validation
```csharp
[HttpPost]
public async Task<IActionResult> UploadDocument(DocumentUploadViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    // ← ADD THIS BLOCK:
    var uploadValidation = await _validationService.ValidateDocumentUploadAsync(model.File);
    if (!uploadValidation.IsValid)
    {
        ModelState.AddModelError("File", uploadValidation.Errors[0]);
        return View(model);
    }

    // Proceed with upload...
    return RedirectToAction("Index");
}
```

### Subscription Change Validation
```csharp
[HttpPost]
public async Task<IActionResult> ChangePlan(int barangayId, string newPlan)
{
    // ← ADD THIS BLOCK:
    var planValidation = await _validationService.ValidateSubscriptionChangeAsync(barangayId, newPlan);
    if (!planValidation.IsValid)
    {
        return BadRequest(new { message = planValidation.Errors[0] });
    }

    // Proceed with plan change...
    return Ok(new { message = "Plan changed successfully" });
}
```

---

## Step 4: Verify in Audit Logs (Optional - Verify It Works)

Check that validation failures are being logged:

```csharp
// SQL query
SELECT TOP 20 
    Timestamp, 
    UserId, 
    IpAddress, 
    Action,
    Description
FROM AuditLog
WHERE Action = 'VALIDATION_FAILURE'
ORDER BY Timestamp DESC
```

Or via controller:
```csharp
public class AuditLogsController : BaseAppController
{
    [HttpGet]
    [RequireRoles("super_admin")]
    public IActionResult ValidationFailures()
    {
        var failures = _context.AuditLogs
            .Where(a => a.Action == "VALIDATION_FAILURE")
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToList();

        return View(failures);
    }
}
```

---

## Available Validation Attributes

Quick reference for all custom attributes:

```csharp
// Email validation (strict - no + addressing)
[ValidEmail]
public string Email { get; set; }

// Password requirements (12+ chars, uppercase, lowercase, digit, special)
[StrongPassword]
public string Password { get; set; }

// Philippine phone format (09XXXXXXXXX or +639XXXXXXXXX)
[ValidPhoneNumber]
public string Phone { get; set; }

// String length range
[ValidStringLength(3, 100)]
public string Title { get; set; }

// File extension whitelist
[ValidFileExtension("pdf", "docx", "xlsx")]
public IFormFile Document { get; set; }

// Numeric range
[ValidRange(1, 100)]
public int Quantity { get; set; }
```

---

## Available IValidationService Methods

Quick reference for all business logic validation methods:

```csharp
// Check file upload (size, type, content)
await _validationService.ValidateDocumentUploadAsync(file);

// Validate subscription plan change
await _validationService.ValidateSubscriptionChangeAsync(barangayId, newPlan);

// Validate budget allocation
await _validationService.ValidateBudgetAllocationAsync(barangayId, amount);

// Check email is unique
await _validationService.ValidateUniqueEmailAsync(email);

// Validate barangay exists and active
await _validationService.ValidateBarangayAsync(barangayId);

// Validate role name
await _validationService.ValidateRoleAsync(roleName);

// Check user permission for operation
await _validationService.ValidatePermissionAsync(userId, operation);

// Validate data export frequency
await _validationService.ValidateDataExportAsync(userId, barangayId);

// Validate password meets requirements
_validationService.ValidatePassword(password, email, username);

// Validate file content (magic bytes)
await _validationService.ValidateFileContentAsync(file);
```

---

## Common Patterns

### Pattern 1: Form Validation
```csharp
[HttpPost]
public async Task<IActionResult> ProcessForm(MyViewModel model)
{
    if (!ModelState.IsValid) // Catches attribute validation errors
        return View(model);

    // Optional: Add business logic validation
    var result = await _validationService.ValidateSomething(...);
    if (!result.IsValid)
    {
        ModelState.AddModelError("field", result.Errors[0]);
        return View(model);
    }

    // Process...
    return RedirectToAction("Success");
}
```

### Pattern 2: API Validation
```csharp
[HttpPost]
[Route("api/users")]
public async Task<IActionResult> CreateUserApi([FromBody] CreateUserDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(new { message = "Validation failed", errors = ModelState });

    var result = await _validationService.ValidateUniqueEmailAsync(dto.Email);
    if (!result.IsValid)
        return BadRequest(new { message = result.Errors[0] });

    // Create user...
    return Ok(new { message = "User created" });
}
```

### Pattern 3: Import/Bulk Operations
```csharp
[HttpPost]
public async Task<IActionResult> ImportUsers(IFormFile file)
{
    // Validate file first
    var fileValidation = await _validationService.ValidateDocumentUploadAsync(file);
    if (!fileValidation.IsValid)
        return BadRequest(fileValidation.Errors[0]);

    // Then validate content
    var contentValidation = await _validationService.ValidateFileContentAsync(file);
    if (!contentValidation.IsValid)
        return BadRequest(contentValidation.Errors[0]);

    // Import...
    return Ok("Import complete");
}
```

---

## Testing Your Validation

### Test in Browser
1. Fill form with invalid data:
   - Email: "invalid"
   - Password: "short" 
   - Phone: "123" 
2. Submit form
3. Should see validation error messages (not technical details)
4. Check browser console for no JavaScript errors

### Test in Logs
```
// Try to trigger validation failure
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"email":"invalid","password":"short"}'

// Check audit logs for validation entry
SELECT * FROM AuditLog 
WHERE Action = 'VALIDATION_FAILURE' 
  AND CreatedAt > GETDATE() - 1/24 -- Last hour
```

---

## Troubleshooting

### Q: Validation not firing?
**A**: Check:
1. ViewModel has `[Required]` or custom attribute
2. ModelState.IsValid check in controller
3. ValidatePostModelFilter is registered in Program.cs ✅ (already done)

### Q: Error message shows technical details?
**A**: Use generic error response pattern:
```csharp
try
{
    // operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    return BadRequest(new { message = "Operation failed. Please try again." });
}
```

### Q: IValidationService not found?
**A**: Check:
1. Service registered in Program.cs ✅ (already done)
2. Correct namespace: `using JAS_MINE_IT15.Services;`
3. Constructor parameter: `IValidationService validationService`

### Q: Validation failures not appearing in logs?
**A**: Check:
1. ValidatePostModelFilter is running (check Application logs)
2. POST request (not GET)
3. ModelState.IsValid is false (has errors)
4. Query audit logs: `WHERE Action = 'VALIDATION_FAILURE'`

---

## Before & After Comparison

| Aspect | Before | After |
|--------|--------|-------|
| **Email Validation** | No check | Custom [ValidEmail] + IValidationService |
| **Password Requirements** | Manual code | Custom [StrongPassword] + service check |
| **Validation Logging** | Not logged | Auto-logged by ValidatePostModelFilter |
| **Error Messages** | May leak details | Generic + logged on server |
| **Business Logic Checks** | Inline in controller | Centralized IValidationService |
| **Code Reuse** | Repeated validation | Single service method |
| **Testing** | Hard to test | Easy to mock IValidationService |

---

## Success Checklist

- ✅ Added `[Required]` and custom attributes to ViewModels
- ✅ Injected `IValidationService` in controllers
- ✅ Called validation methods in action POST handlers
- ✅ Added error handling with generic messages
- ✅ Tested in browser (submitted invalid data)
- ✅ Verified audit logs show validation entries
- ✅ Build succeeds with 0 errors

**Result**: ✅ **Input validation fully implemented & safe to deploy**

---

**Time to Complete**: 5-10 minutes per controller  
**Risk Level**: ⚠️ LOW (only adding validation, not changing existing logic)  
**Rollback**: Simple (remove attributes & validation calls)

**Questions?** See [VALIDATION_GUIDE.md](VALIDATION_GUIDE.md) for detailed documentation.
