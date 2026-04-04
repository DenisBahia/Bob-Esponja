# Fixing Google OAuth redirect_uri_mismatch Error

## Problem
You're getting: **"Error 400: redirect_uri_mismatch"** when trying to authenticate via Google.

This error occurs when the redirect URI sent to Google doesn't match what's configured in the Google Cloud Console.

## Root Cause
The OAuth middleware automatically constructs the redirect URI based on the request scheme (HTTP/HTTPS) and host. If your deployed URL doesn't match what's configured in Google Cloud Console, the validation fails.

## Solution

### Step 1: Identify Your Deployed URL
First, determine what URL you'll be using in production:
- **Development**: `http://localhost:5098/signin-google`
- **Production (Example)**: `https://etftracker.onrender.com/signin-google`

### Step 2: Update Google Cloud Console
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Navigate to **APIs & Services > Credentials**
3. Click on your OAuth 2.0 Client ID
4. Under "Authorized redirect URIs", add **all** URLs your app uses:
   - `http://localhost:5098/signin-google` (for local development)
   - `https://yourdomain.com/signin-google` (for production)
   - `https://your-app.onrender.com/signin-google` (if using Render)

5. Save the changes

### Step 3: Update Your Application Configuration

#### For Development (if running locally)
The default configuration should work automatically for `http://localhost:5098/signin-google`

#### For Production Deployment
Set the following environment variables in your hosting platform (Render, Docker, etc.):

```
OAuth__Google__ClientId=your-google-client-id
OAuth__Google__ClientSecret=your-google-client-secret
OAuth__Google__RedirectUri=https://yourdomain.com/signin-google
```

**IMPORTANT**: The `OAuth__Google__RedirectUri` must exactly match one of the "Authorized redirect URIs" in Google Cloud Console.

### Step 4: Environment Variables Checklist

When deploying, ensure you set these variables in your hosting platform:

| Variable | Value | Example |
|----------|-------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=...;Database=...` |
| `Jwt__Key` | Your JWT signing key (min 32 chars) | Generated secret |
| `OAuth__Google__ClientId` | From Google Cloud Console | `xxx.apps.googleusercontent.com` |
| `OAuth__Google__ClientSecret` | From Google Cloud Console | Secret from Google |
| `OAuth__Google__RedirectUri` | Your deployed callback URL | `https://yourdomain.com/signin-google` |
| `OAuth__GitHub__ClientId` | From GitHub Settings (optional) | GitHub client ID |
| `OAuth__GitHub__ClientSecret` | From GitHub Settings (optional) | GitHub secret |
| `ExternalApis__EodhApi__ApiKey` | Your EODH API key | Your API key |
| `Frontend__BaseUrl` | Your frontend base URL | `https://yourdomain.com` |

### Step 5: Verify Your Setup

After deployment, test the authentication flow:

1. Click "Continue with Google" on the login page
2. You'll be redirected to Google's login
3. After successful login, you'll be redirected back to `https://yourdomain.com/signin-google`
4. The app should process the OAuth callback and create/update your user

## Troubleshooting

### If you still get redirect_uri_mismatch:

1. **Check the exact URL** in the error message or browser logs
2. **Verify the URL matches exactly** in Google Cloud Console (case-sensitive, protocol matters: http vs https)
3. **Clear browser cache** and try again
4. **Check the console logs** for the actual redirect URI being sent

### Quick URL Validation

The redirect URI format must be:
- Protocol: `https://` for production, `http://` for development
- Domain: Must be accessible from the internet
- Path: Exactly `/signin-google`
- No query parameters or fragments

**Valid examples:**
- ✅ `https://etftracker.onrender.com/signin-google`
- ✅ `http://localhost:5098/signin-google`
- ❌ `https://etftracker.onrender.com/signin-google/` (extra slash)
- ❌ `https://etftracker.onrender.com/signin-google?code=123` (query params)
- ❌ `http://192.168.1.100:5098/signin-google` (private IP, not accessible)

## Testing Locally

For local development, ensure:
1. You're using `http://localhost:5098` (not `127.0.0.1`)
2. You've added `http://localhost:5098/signin-google` to Google Cloud Console's redirect URIs
3. Your API is running on port 5098
4. Your frontend is configured to call `http://localhost:5098/api/auth/google`

## More Help

- [Google OAuth Redirect URI Documentation](https://developers.google.com/identity/protocols/oauth2/web-server#redirect-uri-validation)
- [ASP.NET OAuth Documentation](https://docs.microsoft.com/aspnet/core/security/authentication/social/google-logins)

