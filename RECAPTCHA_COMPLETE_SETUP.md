# Google reCAPTCHA v2 Complete Setup & Implementation Guide

## 🎯 Overview
This guide provides complete instructions for implementing and troubleshooting Google reCAPTCHA v2 ("I'm not a robot") in the JAS-MINE ASP.NET Core MVC application.

---

## 📋 Prerequisites

- Google Account
- Access to Google Cloud Console or reCAPTCHA Admin Console
- Your application's domain/URL

---

## ✅ Step 1: Obtain reCAPTCHA Keys from Google

### Option A: Using Google reCAPTCHA Admin Console (Recommended)

1. Go to **[Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin)**
2. Click **"+" button** to create a new site
3. Fill in the following details:
   - **Label**: `JAS-MINE (Local)` or `JAS-MINE (Production)`
   - **reCAPTCHA type**: Select `reCAPTCHA v2` → `"I'm not a robot" Checkbox`
   - **Domains**: 
     - For local development: `localhost`
     - For production: `your-domain.com`
4. Accept the terms and click **Submit**
5. Copy your keys:
   - **Site Key** (Client-side key)
   - **Secret Key** (Server-side key - keep this private!)

### Option B: Using Google Cloud Console

1. Go to **[Google Cloud Console](https://console.cloud.google.com/)**
2. Create a new project or select existing
3. Enable the **reCAPTCHA Enterprise API**
4. Create API keys in the credentials section
5. Configure domain restrictions

---

## 🔧 Step 2: Configure Your ASP.NET Core Application

### Update `appsettings.json` (Production/Default)

```json
{
  "Recaptcha": {
    "SiteKey": "YOUR_SITE_KEY_HERE",
    "SecretKey": "YOUR_SECRET_KEY_HERE"
  }
}
```

### Update `appsettings.Development.json` (Local Development)

```json
{
  "Recaptcha": {
    "SiteKey": "YOUR_SITE_KEY_HERE",
    "SecretKey": "YOUR_SECRET_KEY_HERE"
  }
}
```

### In `Program.cs` (Already Configured)

The following is already configured in your Program.cs:

```csharp
// Configuration binding
builder.Services.Configure<RecaptchaSettings>(
    builder.Configuration.GetSection("Recaptcha"));

// HttpClient with dependency injection
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();
```

---

## 💻 Step 3: Frontend Implementation (Login.cshtml)

Your Login.cshtml already includes:

1. **reCAPTCHA API Script**:
   ```html
   <script src="https://www.google.com/recaptcha/api.js" async defer></script>
   ```

2. **reCAPTCHA Widget**:
   ```html
   <div class="g-recaptcha" data-sitekey="@recaptchaSiteKey"></div>
   ```

3. **Hidden Token Input**:
   ```html
   <input asp-for="RecaptchaToken" type="hidden" id="recaptcha-token" />
   ```

4. **JavaScript to Capture Token**:
   ```javascript
   const token = grecaptcha.getResponse();
   tokenInput.value = token;
   ```

---

## 🔌 Step 4: Backend Implementation

### RecaptchaService.cs (Already Implemented)

This service handles token verification with Google's servers:

```csharp
public async Task<bool> VerifyTokenAsync(string token, string? remoteIp)
{
    // 1. Validates token is not empty
    if (string.IsNullOrWhiteSpace(token))
        return false;

    // 2. Validates secret key is configured
    if (string.IsNullOrWhiteSpace(_settings.SecretKey))
    {
        _logger.LogWarning("reCAPTCHA secret key is not configured.");
        return false;
    }

    // 3. Sends POST request to Google's API
    var payload = new Dictionary<string, string>
    {
        ["secret"] = _settings.SecretKey,
        ["response"] = token
    };

    if (!string.IsNullOrWhiteSpace(remoteIp))
        payload["remoteip"] = remoteIp;

    using var content = new FormUrlEncodedContent(payload);
    using var response = await _httpClient.PostAsync(
        "https://www.google.com/recaptcha/api/siteverify", content);

    // 4. Parses JSON response
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json);

    // 5. Returns true if success == true
    return result?.Success ?? false;
}
```

### HomeController Login POST (Key Logic)

In your HomeController.cs:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    var captchaRequired = failedAttempts >= 3;

    // 🔒 Validate CAPTCHA if required
    if (captchaRequired && !await IsRecaptchaValidAsync(model.RecaptchaToken))
    {
        _logger.LogWarning("Login blocked due to invalid CAPTCHA");
        model.ErrorMessage = "Please complete CAPTCHA verification before signing in.";
        model.CaptchaRequired = true;
        SetRecaptchaSiteKey();
        return View(model);
    }

    // Continue with normal login flow...
}
```

---

## 🐛 Step 5: Debugging & Troubleshooting

### Issue 1: Widget Not Showing

**Check**:
- [ ] reCAPTCHA API script is loaded: `https://www.google.com/recaptcha/api.js`
- [ ] Site key is valid and matches your domain
- [ ] Domain is registered in Google reCAPTCHA console
- [ ] Browser console for JavaScript errors

**Fix**:
```javascript
// Add to browser console to verify
console.log('grecaptcha available?', typeof grecaptcha !== 'undefined');
console.log('grecaptcha.ready available?', typeof grecaptcha.ready !== 'undefined');
```

### Issue 2: "reCAPTCHA verification failed"

**Likely cause**: **Secret key is placeholder or incorrect**

**Check**:
```csharp
// Add logging in RecaptchaService.cs
_logger.LogInformation("Verifying token with secret key: {SecretKeyLength} chars", 
    _settings.SecretKey.Length);
```

**Fix**:
1. Go to [Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin)
2. Copy the correct **Secret Key**
3. Update appsettings.json with the real key
4. Restart your application

### Issue 3: CORS or Network Error

**Check**:
- Network tab in browser DevTools
- Google API endpoint is accessible: `https://www.google.com/recaptcha/api/siteverify`

**Fix**:
```csharp
// Check HttpClient configuration in Program.cs
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();
```

### Issue 4: Token is Empty

**Check**: JavaScript is capturing the token properly

**Debug**:
```javascript
document.getElementById('loginForm')?.addEventListener('submit', function (e) {
    const token = grecaptcha.getResponse();
    console.log('Captured token:', token);  // Debug
    if (!token) {
        console.error('Token not captured!');  // Debug
    }
});
```

---

## 📊 Testing Checklist

- [ ] **Local Environment**:
  - [ ] Navigate to Login page
  - [ ] Fill in email and password
  - [ ] Attempt 3 failed logins (to trigger CAPTCHA requirement)
  - [ ] 4th attempt: reCAPTCHA widget should appear
  - [ ] Complete CAPTCHA and verify login succeeds

- [ ] **Production Environment**:
  - [ ] Register domain in Google reCAPTCHA console
  - [ ] Deploy with correct secret key
  - [ ] Test full login flow

- [ ] **Error Handling**:
  - [ ] Invalid token → proper error message
  - [ ] Missing secret key → warning logged, validation fails
  - [ ] Network error → user-friendly message

---

## 🔐 Security Best Practices

✅ **Implemented**:
- Secret key stored in appsettings.json (not hardcoded)
- Token verified on server-side (not just client-side)
- Anti-forgery token validation: `[ValidateAntiForgeryToken]`
- Remote IP address sent to Google for verification
- Rate limiting enabled: `[EnableRateLimiting("login")]`

⚠️ **Additional Recommendations**:
- Never expose secret key in client-side code
- Use HTTPS in production
- Implement account lockout after failed attempts (already done)
- Monitor reCAPTCHA analytics in Google console
- Set CAPTCHA score threshold if using v3 (v2 doesn't have this)

---

## 🚀 Next Steps

1. **Obtain your Google reCAPTCHA keys** (Step 1)
2. **Update appsettings.json** with real keys (Step 2)
3. **Test locally** with the checklist (Step 5)
4. **Monitor logs** for any issues
5. **Deploy to production** with production domain registered

---

## 📞 Support & Resources

- **Google reCAPTCHA Docs**: https://www.google.com/recaptcha/about/
- **Admin Console**: https://www.google.com/recaptcha/admin
- **reCAPTCHA v2 Documentation**: https://developers.google.com/recaptcha/docs/v2/start
- **Implementation Checklist**: https://developers.google.com/recaptcha/docs/v2/faq

---

## 📝 Current Status

✅ **What's Already Implemented**:
- RecaptchaService with proper token verification
- Login.cshtml with widget and JavaScript
- HomeController with CAPTCHA validation logic
- Configuration in appsettings.json and Program.cs
- Logging for debugging
- Anti-forgery token protection
- Rate limiting on login endpoint

❌ **What's Blocking It**:
- **Secret key is set to placeholder "YOUR_SECRET_KEY_HERE"**
  - This MUST be replaced with a real key from Google

📋 **Configuration Template**:
```json
{
  "Recaptcha": {
    "SiteKey": "6Lco6bgsAAAAABzMLPN00kjbphm9ewTT02mFMEb7",
    "SecretKey": "6Lco6bgsAAAAAD_example_secret_key_12345"
  }
}
```

---

**Version**: 1.0  
**Last Updated**: April 15, 2026  
**Application**: JAS-MINE IT15 Integrated ERP System
