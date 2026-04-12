# Swagger Setup - Quick Start Guide

## What Was Installed

Swagger API documentation has been added to the Investments Tracker API project using **Swashbuckle.AspNetCore**.

## Running the Application

```bash
cd ETFTracker.Api
dotnet run
```

## Accessing Swagger UI

### Development Environment
Visit: **`http://localhost:5000/swagger`** (or whatever port your app runs on)

You'll see:
- ✅ All API endpoints listed
- ✅ Endpoint descriptions
- ✅ Request/response schemas
- ✅ Input parameters documented
- ✅ Try-it-out buttons to test endpoints

## API Endpoints Documented

### Authentication (`/api/auth`)
- `GET /github` - Start GitHub login
- `GET /github/complete` - GitHub callback
- `GET /google` - Start Google login
- `GET /google/complete` - Google callback
- `GET /me` - Get current user info

### Holdings (`/api/holdings`)
- `GET /etf-description/{ticker}` - Get ETF details
- `GET /portfolio-evolution` - Portfolio growth history
- `GET /dashboard` - Full dashboard data
- `GET` - List all holdings
- `GET /{id}/history` - Transaction history
- `POST /transaction` - Add new transaction

### Projections (`/api/projections`)
- `GET` - Get current projection
- `PUT /settings` - Save projection settings
- `POST /versions` - Save projection version
- `GET /versions` - List all versions
- `GET /versions/{id}` - Get version details

## Files Modified

1. **ETFTracker.Api.csproj**
   - Added Swashbuckle.AspNetCore NuGet package
   - Enabled XML documentation generation

2. **Program.cs**
   - Added `builder.Services.AddSwaggerGen();`
   - Added Swagger UI middleware (development only)
   - Configured Swagger endpoint at `/swagger`

3. **Controllers/**
   - Added XML documentation comments
   - AuthController, HoldingsController with detailed descriptions

## Build Status

✅ **Build Succeeds** - 0 Errors, 204 Warnings (non-blocking)

## Environment-Specific Behavior

- **Development**: Swagger UI visible at `/swagger`
- **Production**: Swagger is disabled for security

## Next Steps

To use the Swagger UI:
1. Run the application
2. Navigate to http://localhost:5000/swagger
3. Click on any endpoint to expand it
4. Click "Try it out" to test the endpoint
5. For authenticated endpoints, you'll need a valid JWT token

## Notes

- All endpoint documentation is auto-generated from code XML comments
- Security definitions for JWT Bearer tokens are configured
- Swagger spec available at `/swagger/v1/swagger.json` for external tools

