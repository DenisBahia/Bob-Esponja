# Deployment Guide - Google OAuth Fix

## Summary of Changes

I've updated your application to properly handle Google OAuth redirect URI configuration for both development and production environments.

### Files Modified:

1. **ETFTracker.Api/Program.cs**
   - Added support for configurable redirect URIs via environment variables
   - Added `OAuth:Google:RedirectUri` configuration option
   - Google OAuth now respects custom redirect URIs in production

2. **ETFTracker.Api/appsettings.json** (Development)
   - Added `OAuth.GitHub.RedirectUri` field (null for auto-detection)
   - Added `OAuth.Google.RedirectUri` field (null for auto-detection)

3. **ETFTracker.Api/appsettings.Production.json**
   - Added `OAuth` section with `RedirectUri` placeholders
   - Added `Frontend.BaseUrl` configuration

4. **Dockerfile**
   - Updated documentation with complete list of required environment variables
   - Clarified the importance of `OAuth__Google__RedirectUri`

### New Documentation:
- **GOOGLE_OAUTH_FIX.md** - Complete troubleshooting guide with step-by-step instructions

## For Your Release

When deploying to production, set these environment variables in your hosting platform:

### Required for Google OAuth to Work:
```
OAuth__Google__ClientId=your-google-client-id-here
OAuth__Google__ClientSecret=your-google-client-secret-here
OAuth__Google__RedirectUri=https://yourdomain.com/signin-google
```

### All Required Environment Variables:
```
ConnectionStrings__DefaultConnection=Host=your-db-host;Port=5432;Database=your-db;Username=user;Password=pass
Jwt__Key=your-secret-key-min-32-chars
Jwt__Issuer=ETFTracker
Jwt__Audience=ETFTracker
OAuth__Google__ClientId=your-google-client-id
OAuth__Google__ClientSecret=your-google-client-secret
OAuth__Google__RedirectUri=https://yourdomain.com/signin-google
OAuth__GitHub__ClientId=your-github-client-id (optional)
OAuth__GitHub__ClientSecret=your-github-client-secret (optional)
ExternalApis__EodhApi__ApiKey=your-eodh-api-key
Frontend__BaseUrl=https://yourdomain.com
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
```

## Key Steps to Deploy:

1. **Update Google Cloud Console**
   - Add your production redirect URI to authorized URIs
   - Format: `https://yourdomain.com/signin-google`

2. **Set Environment Variables**
   - In your hosting platform (Render, Docker environment, etc.)
   - Most important: `OAuth__Google__RedirectUri` must match Google Cloud Console exactly

3. **Test OAuth Flow**
   - After deployment, test "Continue with Google"
   - Verify redirect works correctly

4. **If Error 400 Persists**
   - Check that redirect URI in Google Cloud exactly matches your deployment URL
   - Verify `OAuth__Google__RedirectUri` environment variable is set correctly
   - Ensure the domain is accessible from the internet (not localhost or private IPs)

## Development Setup (Local)

For local development, the app defaults to:
- API: `http://localhost:5098`
- Google callback: `http://localhost:5098/signin-google`

Just ensure you've added `http://localhost:5098/signin-google` to Google Cloud Console.

## Questions?

Refer to **GOOGLE_OAUTH_FIX.md** for detailed troubleshooting steps.

