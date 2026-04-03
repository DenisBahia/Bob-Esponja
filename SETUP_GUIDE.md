# ETF Investment Tracking Application - Complete Setup Guide

## Project Overview

A full-stack ETF portfolio tracking application with:
- **Backend**: .NET 10 REST API with Entity Framework Core and PostgreSQL
- **Frontend**: Angular 21 standalone components with responsive UI
- **Database**: PostgreSQL with comprehensive schema
- **External APIs**: Eodhd (primary) and Yahoo Finance (fallback) for price fetching

## Prerequisites

- PostgreSQL 12+ installed and running
- .NET 10 SDK
- Node.js 20+ and npm
- Angular CLI (`npm install -g @angular/cli`)

## Database Setup

### 1. Create PostgreSQL Database

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE etf_tracker;

# Exit psql
\q
```

### 2. Run Database Schema

```bash
# Execute the SQL schema file
psql -U postgres -d etf_tracker -f database_schema.sql
```

Alternatively, use Entity Framework migrations (from the API project):

```bash
cd ETFTracker.Api
dotnet ef database update
```

## Backend Setup

### 1. Configure Connection String

Edit `ETFTracker.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=etf_tracker;Username=postgres;Password=YOUR_PASSWORD;"
  },
  "ExternalApis": {
    "EodhApi": {
      "ApiKey": "YOUR_EODHD_API_KEY_HERE"
    }
  }
}
```

**Important**: Replace `YOUR_PASSWORD` with your PostgreSQL password and `YOUR_EODHD_API_KEY_HERE` with your actual Eodhd API key (get one at https://eodhd.com).

### 2. Build Backend

```bash
cd ETFTracker.Api
dotnet build
```

### 3. Run Backend

```bash
cd ETFTracker.Api
dotnet run
# API will be available at https://localhost:5000/api
```

The API will:
- Automatically create the database if it doesn't exist
- Listen on `https://localhost:5000`
- Serve API endpoints at `/api/*`

## Frontend Setup

### 1. Install Dependencies

```bash
cd ETFTracker.Web
npm install
```

### 2. Configure API URL

The Angular app is configured to connect to `http://localhost:5000/api`. If you're running the API on a different port, update the API URL in:

```typescript
// src/app/services/api.service.ts
private apiUrl = 'http://localhost:5000/api';
```

### 3. Development Server

```bash
cd ETFTracker.Web
npm start
# or
ng serve
# App will be available at http://localhost:4200
```

### 4. Production Build

```bash
cd ETFTracker.Web
npm run build
# Output will be in dist/ETFTracker.Web
```

## API Endpoints

### Dashboard
- `GET /api/holdings/dashboard` - Get complete dashboard with header metrics and all holdings

### Holdings
- `GET /api/holdings` - Get all holdings for the user
- `POST /api/holdings/transaction` - Add new buy transaction

Request body for POST:
```json
{
  "ticker": "VWRL",
  "quantity": 10.5,
  "purchasePrice": 45.50,
  "purchaseDate": "2024-03-31"
}
```

### History
- `GET /api/holdings/{holdingId}/history` - Get buy history for a specific holding

## Features

### Phase 1: Holdings Management (Current)

✅ **Dashboard Header**
- Total holdings amount in EUR
- Daily, Weekly, Monthly, YTD gain/loss (EUR and %)

✅ **Holdings Table**
- All positions displayed
- 4 period views simultaneously (Daily, Weekly, Monthly, YTD)
- Current price and total value
- Gain/loss calculations for each period

✅ **Add New Buy**
- Modal dialog to record new purchase
- Ticker, quantity, purchase price, purchase date
- Automatic average cost recalculation
- Real-time dashboard refresh

✅ **Buy History**
- View all transactions per holding
- Date, quantity, price, and total invested

✅ **Price Fetching**
- Eodhd API (primary source)
- Yahoo Finance fallback
- Daily snapshot storage for historical data
- Automatic period calculations

## Project Structure

```
Bob Esponja/
├── ETFTracker.Api/                 # .NET Backend
│   ├── Controllers/                # API endpoints
│   ├── Models/                     # Entity models
│   ├── Services/                   # Business logic
│   ├── Data/                       # DbContext & migrations
│   ├── Dtos/                       # Data transfer objects
│   └── appsettings.json           # Configuration
├── ETFTracker.Web/                 # Angular Frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── pages/             # Page components
│   │   │   ├── components/        # Reusable components
│   │   │   └── services/          # API & business services
│   │   └── styles.scss            # Global styles
│   └── angular.json               # Angular config
├── database_schema.sql            # PostgreSQL schema
└── project.md                     # Project specifications
```

## Default Test User

The backend uses a default user ID of `1`. To create initial test data:

```bash
cd ETFTracker.Api
dotnet run
# Make a POST request to add a transaction
```

## Troubleshooting

### Backend Issues

**Problem**: `Unable to connect to database`
- Check PostgreSQL is running: `pg_isready`
- Verify connection string in appsettings.json
- Ensure database `etf_tracker` exists

**Problem**: `API not accessible from frontend`
- Check backend is running on https://localhost:5000
- Verify CORS is enabled in Program.cs
- Check browser console for CORS errors

### Frontend Issues

**Problem**: `Angular app won't load`
- Clear cache: `npm cache clean --force`
- Delete node_modules: `rm -rf node_modules`
- Reinstall: `npm install`

**Problem**: `Prices not updating`
- Check Eodhd API key is configured correctly
- Verify internet connection
- Check browser console for API errors

### Price Fetching Issues

**Problem**: `All prices failing`
- Eodhd API key invalid or expired
- Rate limit exceeded (Eodhd has daily limits)
- Check Yahoo Finance is accessible

The system automatically:
- Falls back to Yahoo Finance if Eodhd fails
- Uses last known price if both fail
- Stores daily snapshots for historical accuracy

## Development Notes

### Adding New Holdings
Currently, holdings are added through the "Add New Buy" modal. The system automatically:
1. Creates a new holding if it doesn't exist
2. Recalculates average cost
3. Updates total quantity

### Period Calculations
- **Daily**: Last 1 day
- **Weekly**: Last 7 days
- **Monthly**: Last 30 days
- **YTD**: Days since January 1st

## Next Steps (Phase 2)

- Multi-broker portfolio aggregation
- Deemed disposal tax calculations
- Advanced analytics & charts
- Annual goal setting
- Buy recommendations

## Support

For issues or feature requests, please refer to the project.md file for detailed specifications.

