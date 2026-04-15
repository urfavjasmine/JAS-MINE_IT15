# Google reCAPTCHA v3 Implementation Guide

**Status**: ✅ Complete & Ready to Deploy  
**Version**: v3 (Invisible, Score-Based)  
**Last Updated**: April 15, 2026  

---

## 🎯 Overview

**reCAPTCHA v3** provides bot protection without user interaction. It:
- ✅ Runs invisibly in the background
- ✅ Analyzes user behavior (mouse movements, typing patterns, etc.)
- ✅ Returns a score (0.0 to 1.0) indicating how likely the request is from a human
- ✅ Does NOT show a checkbox or widget
- ✅ Does NOT block legitimate users

### v2 vs v3 Comparison

| Feature | v2 Checkbox | v3 Invisible |
|---------|-------------|-------------|
| **User Interaction** | Required (click checkbox) | None (background) |
| **UX Impact** | High friction | No friction |
| **Verification** | Binary (pass/fail) | Probabilistic (score) |
| **Score Return** | N/A | 0.0-1.0 |
| **Best For** | High-security actions | Normal login/forms |

---

## 📋 Getting Your reCAPTCHA v3 Keys

### Step 1: Go to Google reCAPTCHA Admin Console

1. Visit: **https://www.google.com/recaptcha/admin**
2. Sign in with your Google account
3. Click **"+"** to create a new site

### Step 2: Configure Your Site

Fill in the following:

| Field | Value |
|-------|-------|
| **Label** | `JAS-MINE (Local)` or `JAS-MINE (Production)` |
| **reCAPTCHA type** | ✅ **reCAPTCHA v3** (NOT v2) |
| **Domains** | `localhost` (dev) or `your-domain.com` (prod) |

### Step 3: Copy Your Keys

After creation, you'll see:
- **Site Key**: Public key (safe to share)
- **Secret Key**: Private key (KEEP SECRET!)

```
Site Key:   6Lc...XXXX (example)
Secret Key: 6Lc...YYYY (example - KEEP SECRET!)
```

---

## ⚙️ Configuration

### Update `appsettings.json`

```json
{
  "Recaptcha": {
    "SiteKey": "YOUR_V3_SITE_KEY_HERE",
    "SecretKey": "YOUR_V3_SECRET_KEY_HERE",
    "VerifyUrl": "https://www.google.com/recaptcha/api/siteverify",
    "Action": "login",
    "ScoreThreshold": 0.5
  }
}
```

**Configuration Parameters**:
- `SiteKey`: Your public site key from Google
- `SecretKey`: Your private secret key (NEVER share this!)
- `Action`: The action name (e.g., "login", "submit", "contact")
- `ScoreThreshold`: Minimum score required (0.0-1.0)
  - `1.0` = Very strict (only clear humans)
  - `0.7` = Medium-high confidence
  - `0.5` = Balanced (default)
  - `0.3` = Permissive
  - `0.0` = Allow everything

### Update `appsettings.Development.json`

Same as above (for local testing)

---

## 🔄 How reCAPTCHA v3 Works in Your App

### Request Flow

```
1. User visits login page
   ↓
2. Page loads with invisible reCAPTCHA v3 script
   ↓
3. User fills in email and password
   ↓
4. User clicks "Sign In"
   ↓
5. JavaScript calls grecaptcha.execute('login')
   ↓
6. Google returns a token
   ↓
7. Form submitted with token in hidden field
   ↓
8. Server receives request and calls IsRecaptchaValidAsync()
   ↓
9. Backend calls Google's verify endpoint
   ↓
10. Google returns { success: true/false, score: 0.0-1.0 }
   ↓
11. Backend checks: score >= threshold?
    - YES: Proceed with login validation
    - NO: Reject request, log suspicious activity
   ↓
12. If login succeeds → User logged in
    If login fails → Show error
```

### Score Interpretation

Google's reCAPTCHA v3 returns a score based on:
- Mouse movements and clicking patterns
- Time spent on page
- Browser fingerprint
- IP reputation
- Account history
- And more...

```
Score Range | Interpretation | Recommendation
   0.9-1.0  | Very human-like | Allow
   0.7-0.9  | Likely human    | Allow
   0.5-0.7  | Uncertain       | Monitor/log
   0.3-0.5  | Suspicious      | Consider blocking
   0.0-0.3  | Very bot-like    | Block
```

---

## 📁 Code Implementation

### Backend (C#)

#### RecaptchaSettings.cs
```csharp
public class RecaptchaSettings
{
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    public string Action { get; set; } = "login";  // NEW for v3
    public float ScoreThreshold { get; set; } = 0.5f;  // NEW for v3
}
```

#### RecaptchaService.cs
Two methods available:

```csharp
// Simple boolean check
Task<bool> VerifyTokenAsync(string token, string? remoteIp);

// Detailed score information
Task<(bool isValid, float score, string details)> VerifyTokenWithScoreAsync(string token, string? remoteIp);
```

#### HomeController.cs Login POST
```csharp
// Verify reCAPTCHA v3 token
if (!await IsRecaptchaValidAsync(model.RecaptchaToken))
{
    model.ErrorMessage = "Security verification failed.";
    return View(model);
}

// Proceed with login if verification passes
```

### Frontend (HTML/JavaScript)

#### Login.cshtml
```html
<!-- Invisible reCAPTCHA v3 script (no widget) -->
<script src="https://www.google.com/recaptcha/api.js" async defer></script>

<!-- Hidden field for token -->
<input type="hidden" id="recaptcha-token" name="RecaptchaToken" />

<!-- Form submission -->
<form asp-action="Login" method="post">
    <!-- Email and password fields -->
    
    <button type="submit">Sign In</button>
</form>

<!-- JavaScript to handle v3 -->
<script>
    grecaptcha.ready(function() {
        document.getElementById('loginForm')?.addEventListener('submit', async function (e) {
            e.preventDefault();
            
            // Execute v3 to get token
            const token = await grecaptcha.execute('YOUR_SITE_KEY', { action: 'login' });
            
            // Set token and submit
            document.getElementById('recaptcha-token').value = token;
            this.submit();
        });
    });
</script>
```

---

## 🧪 Testing

### Local Testing

1. **Start your app**
   ```bash
   dotnet run
   ```

2. **Navigate to login page**
   - Go to: `https://localhost:5000/home/login`

3. **Check browser console** (F12 → Console)
   - Look for: `[reCAPTCHA v3] reCAPTCHA API ready`
   - This confirms the script loaded

4. **Test login**
   - Enter valid credentials
   - Click "Sign In"
   - Check for token execution in console

5. **Verify in application logs**
   - Look for: `reCAPTCHA v3 verification succeeded. Score: 0.XX`

### Score Thresholds to Test

| Scenario | Score | Result |
|----------|-------|--------|
| Normal human typing | 0.8-1.0 | ✅ Allowed |
| Rapid form filling | 0.6-0.8 | ✅ Allowed |
| Suspicious activity | 0.3-0.5 | ❌ Blocked |
| Bot-like behavior | 0.0-0.3 | ❌ Blocked |

---

## 📊 Monitoring & Adjusting Thresholds

### View Analytics

1. Go to: https://www.google.com/recaptcha/admin
2. Click your site name
3. View the **Analytics** tab to see:
   - Score distribution
   - Action metrics
   - Traffic patterns

### Adjust Score Threshold

```json
// In appsettings.json
"ScoreThreshold": 0.5
```

**Recommendations**:
- Start with `0.5` (balanced)
- Monitor false positives/negatives for 1-2 weeks
- Adjust based on analytics data

---

## 🐛 Debugging

### Enable Debug Logging

Browser console shows debug messages on localhost:
```
[reCAPTCHA v3] reCAPTCHA API ready
[reCAPTCHA v3] Form submit triggered, executing reCAPTCHA v3...
[reCAPTCHA v3] reCAPTCHA v3 token received: abc123...xyz
[reCAPTCHA v3] Submitting form with reCAPTCHA token
```

### Check Application Logs

Look for log entries:
```
reCAPTCHA v3 verification succeeded. Score: 0.87, Action: login, IP: 192.168.1.1
reCAPTCHA v3 verification failed. Score: 0.23, Details: Score below threshold, IP: 192.168.1.1
```

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Script not loading" | CDN blocked | Check firewall, enable Google APIs |
| "Token empty" | JavaScript error | Check browser console for errors |
| "Score always 0.0" | Test account | Use real account/domain |
| "Score too low" | Threshold too high | Lower `ScoreThreshold` in config |

---

## 🔐 Security Best Practices

✅ **Implemented**:
- Secret key in config (not hardcoded)
- Server-side verification only (not client-side)
- Remote IP sent to Google
- Action name matching
- Comprehensive logging

⚠️ **Your Responsibilities**:
- **Never expose secret key** in client-side code
- **Keep appsettings.json out of git** (add to .gitignore)
- **Use HTTPS only** in production
- **Monitor logs** for suspicious patterns
- **Review Google Analytics** regularly
- **Don't rely on score alone** - use with other validation

---

## 📚 Files Modified

1. **Models/RecaptchaSettings.cs**
   - Added `Action` property
   - Added `ScoreThreshold` property

2. **Services/IRecaptchaService.cs**
   - Added `VerifyTokenWithScoreAsync()` method
   - Updated documentation

3. **Services/RecaptchaService.cs**
   - Implemented score threshold checking
   - Added detailed error logging
   - Returns both boolean and score information

4. **Controllers/HomeController.cs**
   - Updated `IsRecaptchaValidAsync()` for v3 score handling
   - Enhanced logging with score information

5. **Views/Home/Login.cshtml**
   - Removed v2 checkbox widget
   - Added v3 token execution
   - Updated JavaScript for async token handling

6. **appsettings.json**
   - Added `Action` property
   - Added `ScoreThreshold` property
   - Updated comments for v3

7. **appsettings.Development.json**
   - Same updates as appsettings.json

---

## 🚀 Deployment Checklist

- [ ] Get real reCAPTCHA v3 Site Key and Secret Key from Google
- [ ] Update `appsettings.json` with real keys
- [ ] Register production domain in Google reCAPTCHA admin console
- [ ] Set appropriate `ScoreThreshold` (recommend starting with 0.5)
- [ ] Test locally with debug logging enabled
- [ ] Review application logs after first deployment
- [ ] Monitor Google Analytics for score distribution
- [ ] Adjust threshold based on first week's data
- [ ] Set up alerts for suspicious activity (low scores)

---

## 🎓 Key Differences: v2 → v3

### What Changed
- ✅ No visible widget (invisible)
- ✅ Automatic token generation (no user action required)
- ✅ Score-based verification (not binary)
- ✅ Action name required
- ✅ Better UX (no friction)
- ❌ Requires more sophisticated backend logic

### What Stayed the Same
- Server-side verification
- Remote IP tracking
- Secret key protection
- Google API integration
- Logging and monitoring

---

## 📞 Resources

- **Google reCAPTCHA Admin**: https://www.google.com/recaptcha/admin
- **reCAPTCHA v3 Docs**: https://developers.google.com/recaptcha/docs/v3
- **reCAPTCHA API**: https://developers.google.com/recaptcha/docs/verify
- **Best Practices**: https://developers.google.com/recaptcha/docs/scoring

---

## ✨ Summary

**reCAPTCHA v3 is now fully implemented in your application:**

✅ Invisible background verification  
✅ Score-based decision making  
✅ Server-side validation  
✅ Comprehensive logging  
✅ Production-ready code  

**Next Steps:**
1. Get your Site Key and Secret Key from Google reCAPTCHA admin console
2. Update `appsettings.json` with your real keys
3. Test locally
4. Deploy to production
5. Monitor score distribution in Google Analytics
6. Adjust `ScoreThreshold` as needed

---

**🎉 Your reCAPTCHA v3 implementation is ready!**
