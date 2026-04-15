# Google reCAPTCHA v2 Integration Guide

Your ASP.NET Core MVC project now has Google reCAPTCHA v2 ("I'm not a robot") fully integrated into the login system. This document explains the setup and configuration.

## ✅ Integration Status

The following components are already implemented:

### Frontend (Login.cshtml)
- ✅ reCAPTCHA widget embedded in the login form
- ✅ Google reCAPTCHA API script loaded conditionally
- ✅ JavaScript validation before form submission
- ✅ Hidden token field to capture the reCAPTCHA token

### Backend (HomeController.cs)
- ✅ Login GET method sets the reCAPTCHA site key
- ✅ Login POST method validates reCAPTCHA response
- ✅ reCAPTCHA required after 3 failed login attempts (session-based)
- ✅ Proper error handling and logging

### Services
- ✅ `IRecaptchaService` interface for token verification
- ✅ `RecaptchaService` implementation that:
  - Sends POST requests to Google's verification endpoint
  - Validates tokens server-side only (secure)
  - Includes user's remote IP for fraud detection
  - Logs verification failures

### Models & Configuration
- ✅ `RecaptchaSettings` model with SiteKey, SecretKey, and VerifyUrl
- ✅ `LoginViewModel` with RecaptchaToken and CaptchaRequired properties
- ✅ Program.cs configures reCAPTCHA services and dependency injection

## 🔑 Configuration

### 1. Set Your reCAPTCHA Secret Key

**DO NOT commit the secret key to version control!**

#### Option A: User Secrets (Recommended for Development)

Use .NET User Secrets to store the secret key locally:

```powershell
# Run in the project directory: d:\JAS-MINE_IT15
cd d:\JAS-MINE_IT15

# Initialize user secrets (one time only)
dotnet user-secrets init

# Set the reCAPTCHA secret key
dotnet user-secrets set "Recaptcha:SecretKey" "YOUR_ACTUAL_SECRET_KEY_HERE"
```

#### Option B: Environment Variables (Recommended for Production)

Set an environment variable on your server:

**Windows:**
```
System Properties → Environment Variables
Name: Recaptcha__SecretKey
Value: YOUR_ACTUAL_SECRET_KEY_HERE
```

**Linux/Docker:**
```bash
export Recaptcha__SecretKey="YOUR_ACTUAL_SECRET_KEY_HERE"
```

**Azure App Service:**
```
Configuration → Application settings
Name: Recaptcha__SecretKey
Value: YOUR_ACTUAL_SECRET_KEY_HERE
```

### 2. Verify Current Configuration

**appsettings.json** (Committed to source control):
```json
"Recaptcha": {
  "SiteKey": "6Lco6bgsAAAAABzMLPN00kjbphm9ewTT02mFMEb7",
  "SecretKey": "YOUR_SECRET_KEY_HERE"
}
```

**appsettings.Development.json** (Local development):
```json
"Recaptcha": {
  "SiteKey": "6Lco6bgsAAAAABzMLPN00kjbphm9ewTT02mFMEb7",
  "SecretKey": "YOUR_SECRET_KEY_HERE"
}
```

Replace `YOUR_SECRET_KEY_HERE` with your actual reCAPTCHA secret key from the [Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin).

## 🔒 Security Features

1. **Server-Side Validation Only**
   - Token verified exclusively on the server
   - No sensitive data stored in cookies or session
   - Secret key never exposed to the client

2. **Failed Attempt Tracking**
   - reCAPTCHA widget appears after 3 failed login attempts
   - Prevents brute-force attacks
   - Session-based, cleared on successful login

3. **Rate Limiting**
   - Integrated with ASP.NET Core rate limiting
   - Max 5 login attempts per IP per minute
   - Returns HTTP 429 (Too Many Requests) when exceeded

4. **CSRF Protection**
   - `[ValidateAntiForgeryToken]` attribute on Login POST
   - Automatic token validation via `AutoValidateAntiforgeryTokenAttribute`

5. **User IP Tracking**
   - Remote IP sent to Google for fraud detection
   - Helpful for detecting distributed attacks

6. **HTTPS Only (Recommended)**
   - Cookie secure policy set to `CookieSecurePolicy.Always`
   - Configure SSL certificate on production server

## 📋 How It Works

### Login Flow:

1. **User visits login page:**
   - If ≤3 failed attempts: Simple email/password form
   - If >3 failed attempts: reCAPTCHA widget displayed

2. **User completes reCAPTCHA:**
   - Google's API generates a token
   - Token stored in hidden form field
   - User submits form

3. **Server validates:**
   - Backend calls `IsRecaptchaValidAsync(token)`
   - Sends POST to `https://www.google.com/recaptcha/api/siteverify`
   - Includes secret key (never exposed to client)
   - Google responds with success/failure

4. **Login continues:**
   - If reCAPTCHA valid + credentials correct → Login successful
   - If either fails → Show error, re-display form

### Key Code Components:

**Login GET (d:\JAS-MINE_IT15\Controllers\HomeController.cs:960)**
```csharp
public IActionResult Login(int? planId = null)
{
    SetRecaptchaSiteKey(); // Passes site key to view
    return View(new LoginViewModel
    {
        CaptchaRequired = GetLoginFailedAttempts() >= 3
    });
}
```

**Login POST (d:\JAS-MINE_IT15\Controllers\HomeController.cs:986)**
```csharp
if (captchaRequired && !await IsRecaptchaValidAsync(model.RecaptchaToken))
{
    model.ErrorMessage = "Please complete CAPTCHA verification before signing in.";
    model.CaptchaRequired = true;
    SetRecaptchaSiteKey();
    return View(model);
}
```

## 🧪 Testing

### Test in Development:

1. Open `https://localhost:7001/Home/Login` (adjust port)
2. First 3 login attempts: No reCAPTCHA widget
3. After 3 failed attempts: reCAPTCHA widget appears
4. Complete reCAPTCHA and login

### Monitor Verification:
- Check logs for reCAPTCHA verification attempts
- Look for warnings if secret key is missing/invalid
- Verify token validation in `/api/siteverify` response

## 📊 Configuration Hierarchy

The application reads reCAPTCHA settings in this order (later overrides earlier):

1. `appsettings.json` (checked in to source control)
2. `appsettings.{Environment}.json` (development, staging, production)
3. User Secrets (development only, local machine)
4. Environment Variables (production)

**Recommended Setup:**
- Store Site Key in `appsettings.json` (public, it's meant to be)
- Store Secret Key in User Secrets (development) or Environment Variables (production)

## 🚀 Production Deployment

1. **Set environment variable on server:**
   ```
   Recaptcha__SecretKey=your_production_secret_key
   ```

2. **Enable HTTPS:**
   - Install SSL certificate
   - Update `CookieSecurePolicy.Always` enforces HTTPS

3. **Monitor reCAPTCHA Analytics:**
   - Visit [Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin)
   - Review verification analytics and adjust sensitivity if needed

4. **Rotate Keys Periodically:**
   - Generate new Site/Secret keys in admin console
   - Update configuration
   - Deactivate old keys

## 🐛 Troubleshooting

### "Security verification is unavailable"
- **Cause:** Site key missing or empty in configuration
- **Fix:** Verify reCAPTCHA configuration is loaded from appsettings.json

### Token verification fails silently
- **Cause:** Secret key missing, incorrect, or rate limited by Google
- **Fix:** Log entries will show the root cause; check ILogger output

### reCAPTCHA widget never appears
- **Cause:** Failed attempts counter not incrementing properly
- **Fix:** Verify session middleware is configured (`builder.Services.AddSession()`)

### CORS errors when verifying
- **Cause:** Network restriction on server
- **Fix:** Ensure server can make HTTPS POST requests to `www.google.com`

## 📚 References

- [Google reCAPTCHA v2 Documentation](https://developers.google.com/recaptcha/docs/display)
- [Google reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin)
- [ASP.NET Core Configuration Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [User Secrets in .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets/)

## ✨ Summary

Your login system now has production-ready reCAPTCHA v2 integration with:
- ✅ Automatic trigger after 3 failed attempts
- ✅ Server-side token verification
- ✅ Secure secret key storage
- ✅ Rate limiting
- ✅ CSRF protection
- ✅ Comprehensive error handling
- ✅ Logging and monitoring

**Next Steps:**
1. Replace `YOUR_SECRET_KEY_HERE` with your actual secret key
2. Use User Secrets for development
3. Use environment variables for production
4. Test the login flow with multiple failed attempts
5. Monitor reCAPTCHA analytics in the admin console
