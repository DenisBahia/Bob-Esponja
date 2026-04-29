# 🎯 Investments Tracker

A modern, full-stack web application for managing and tracking investments (ETFs, stocks, mutual funds, cryptocurrencies, and more) with real-time performance analytics, portfolio sharing, and tax compliance tools designed for Irish investors.

**Live Demo** | **Documentation** | **Support**

---

## 📺 Follow Us on YouTube

> **[🎬 Subscribe to the Investments Tracker YouTube Channel →](https://youtube.com/@InvestmentsTracker)**

We publish weekly videos covering:
- 🖥️ Feature walkthroughs and tutorials for the app
- 💰 Irish tax explainers (Deemed Disposal, CGT, Exit Tax)
- 📈 Portfolio management tips and best practices
- 🛠️ Open-source development updates (Angular · .NET · PostgreSQL)

[![YouTube Channel](https://img.shields.io/badge/YouTube-Subscribe-red?logo=youtube&logoColor=white)](https://youtube.com/@InvestmentsTracker)
[![GitHub](https://img.shields.io/badge/GitHub-Source%20Code-black?logo=github)](https://github.com/DenisBahia/Bob-Esponja)

> 📋 See [`YOUTUBE_CHANNEL.md`](./YOUTUBE_CHANNEL.md) for the full channel kit: description, banner templates, video scripts, and social media messages.

---

## ✨ Quick Highlights

- 📊 **Real-time Portfolio Dashboard** - Multi-period performance tracking (Daily, Weekly, Monthly, YTD)
- 🔄 **Automatic Price Updates** - Powered by Eodhd API with Yahoo Finance fallback
- 💸 **Sell Holdings** - FIFO-based sell workflow with real-time CGT/Exit Tax preview before confirming
- 📥 **CSV Import** - Bulk-import transaction history from broker CSV exports
- 🧾 **Tax Center** - Complete tax ledger tracking deemed disposal events and sell taxes
- ⚙️ **User Tax Settings** - Configure investor profile, tax rates, and annual CGT allowance
- 🎯 **Investment Goal** - Set a wealth target and track progress against it
- 👥 **Portfolio Sharing** - Share portfolios with other investors with granular permission controls
- 📈 **Investment Projections** - Model future portfolio performance with custom parameters
- 🎨 **Modern UI** - Responsive dark-theme design with smooth animations and a public landing page

---

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Setup & Installation](#setup--installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Endpoints](#api-endpoints)
- [Features Guide](#features-guide)
- [Development](#development)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)
- [Quick Links](#quick-links)

---

## 🎯 Overview

Investments Tracker is a comprehensive portfolio management solution built for modern investors. Whether you're managing a simple portfolio or complex multi-broker holdings, this application provides the tools you need to:

- **Track Holdings** across multiple brokers with real-time pricing (ETFs, stocks, mutual funds, crypto, commodities)
- **Analyze Performance** with detailed metrics and historical data
- **Plan Investments** using advanced projection modeling
- **Stay Compliant** with tax regulations (especially for Irish investors)
- **Share Insights** securely with other investors
- **Monitor Transactions** with complete purchase history

## ✨ Key Features

### 📊 Dashboard & Holdings Management
- **Multi-Period Performance Dashboard**
  - View gains/losses for Daily, Weekly, Monthly, and YTD periods
  - Real-time portfolio value tracking
  - Performance metrics across all holdings
  
- **Holdings Table with Advanced Filtering**
  - Ticker and ETF name
  - Quantity, average cost, and current price
  - Total value and performance metrics
  - Buy history and transaction tracking
  - Price source transparency

- **Transaction Management**
  - Easy modal interface to record purchases
  - Ticker selection with autocomplete
  - Quantity and purchase price entry
  - Automatic purchase date tracking
  - Transaction history per holding

### 📈 Performance & Analytics
- **Real-time Price Updates**
  - Primary source: Eodhd API
  - Fallback: Yahoo Finance API
  - Automatic caching and refresh mechanisms
  
- **Historical Data & Trends**
  - Daily price snapshots
  - Trend analysis and visualization
  - Performance comparisons across time periods
  
- **Advanced Calculations**
  - Multi-period returns analysis
  - Weighted average cost calculations
  - Gain/loss tracking in real-time
  - Currency conversion support

### 💰 Tax & Projections
- **Projection Settings Configuration**
  - Monthly buy amounts and annual increases
  - Expected yearly returns
  - Inflation rate adjustments
  - Deemed Disposal settings
  - Exit tax configuration
  - Deemed disposal exclusion options
  
- **Investment Projections Engine**
  - Model future portfolio performance
  - Scenario planning and analysis
  - Long-term wealth projection
  - Tax impact calculations

- **Tax Center (Tax Events Ledger)**
  - Full history of deemed disposal and sell tax events
  - Per-event breakdown: proceeds, cost basis, profit, tax due
  - Filtered views by holding
  - Aggregated tax summary with total liability across all events
  - Annual CGT allowance applied automatically

- **User Tax Settings**
  - Toggle between Irish Investor and Non-Irish Investor profiles
  - Configure Exit Tax %, Deemed Disposal %, CGT %, and SIA Annual %
  - Set annual tax-free CGT allowance
  - Pre-filled with Irish Revenue defaults (38% Exit Tax, €1,270 allowance)

### 🎯 Investment Goal
- **Set a Target Portfolio Value**
  - Define a target wealth amount and target date
  - Track current portfolio vs goal on the dashboard
  - Visual progress indicator
  - Updates automatically as portfolio value changes

### 💸 Sell Holdings
- **3-Step Sell Workflow**
  - Enter quantity, sell price, and date
  - Preview FIFO cost basis and real-time tax calculation before committing
  - Confirm and record the sale with full audit trail
  
- **Tax-Aware Selling**
  - Automatic FIFO lot matching
  - Detects ETF Exit Tax vs standard CGT based on asset type
  - Editable tax rate on the preview screen
  - Complete sell history per holding

### 📥 CSV Import
- **Bulk Import Transaction History**
  - Upload CSV exports from your broker
  - Native Investments Tracker CSV format supported
  - Multi-step import wizard: upload → resolve tickers → preview → import
  
- **Smart Ticker Resolution**
  - Detects ISIN codes and maps to tradeable tickers via Yahoo Finance search
  - Inline search to manually fix unresolved rows
  - Row-level status (ready / needs picker / error) with delete option before importing

### 👥 Portfolio Sharing
- **Share with Other Investors**
  - Invite users by email
  - Granular permission controls
  - Read-only or edit access options
  - Activity tracking and audit logs
  
- **View Shared Portfolios**
  - Access portfolios shared with you
  - Real-time shared data viewing
  - Non-intrusive view of shared holdings
  
- **Permission Management**
  - Grant/revoke access instantly
  - Track sharing status (Active, Pending, Revoked)
  - Readonly mode for sensitive accounts

## 🛠 Tech Stack

### Backend
| Technology | Version | Purpose |
|---|---|---|
| **.NET** | 10.0 | Web framework |
| **C#** | Latest | Backend language |
| **Entity Framework Core** | 10.0 | ORM |
| **PostgreSQL** | 12+ | Primary database |
| **Polly** | Latest | Resilience policies |
| **Swagger/OpenAPI** | 3.0 | API documentation |

### Frontend
| Technology | Version | Purpose |
|---|---|---|
| **Angular** | 21 (LTS) | Web framework |
| **TypeScript** | 5.9 | Language |
| **SCSS** | Latest | Styling |
| **Chart.js** | 4.5 | Data visualization |
| **npm** | 11.11+ | Package manager |

### External Services
| Service | Purpose | Tier |
|---|---|---|
| **Eodhd API** | Primary price data | Free/Paid |
| **Yahoo Finance** | Fallback pricing | Free |

## 📁 Project Structure

```
Bob Esponja/
│
├── ETFTracker.Api/                    # Backend (.NET)
│   ├── Controllers/
│   │   ├── AuthController.cs          # Authentication & Authorization
│   │   ├── HoldingsController.cs      # Holdings, ticker search, sell
│   │   ├── ProjectionsController.cs   # Projection endpoints
│   │   ├── SharingController.cs       # Portfolio sharing
│   │   ├── TaxEventsController.cs     # Tax event ledger
│   │   ├── UserSettingsController.cs  # User tax defaults
│   │   ├── GoalController.cs          # Investment goal
│   │   └── AssetTypeDefaultsController.cs  # Per-asset deemed-disposal defaults
│   │
│   ├── Services/
│   │   ├── HoldingsService.cs         # Holdings business logic
│   │   ├── PriceService.cs            # Price fetching & caching
│   │   ├── ProjectionService.cs       # Projection calculations
│   │   ├── SharingContextService.cs   # Sharing & permission context
│   │   ├── SellService.cs             # FIFO sell & tax calculation
│   │   ├── DeemedDisposalService.cs   # Deemed disposal logic
│   │   ├── GoalService.cs             # Investment goal logic
│   │   └── AssetTypeDeemedDisposalDefaultService.cs
│   │
│   ├── Models/
│   │   ├── Holding.cs
│   │   ├── Transaction.cs
│   │   ├── User.cs
│   │   ├── ProjectionSettings.cs
│   │   ├── PriceSnapshot.cs
│   │   ├── PortfolioShare.cs
│   │   └── [other domain models]
│   │
│   ├── Dtos/
│   │   ├── HoldingDto.cs
│   │   ├── ProjectionDto.cs
│   │   ├── SharingDtos.cs
│   │   ├── TransactionDto.cs
│   │   └── [other DTOs]
│   │
│   ├── Data/
│   │   └── AppDbContext.cs            # EF Core context
│   │
│   ├── Migrations/                    # Database migrations
│   │
│   ├── Program.cs                     # Application startup
│   ├── appsettings.json              # Configuration
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   └── ETFTracker.Api.csproj
│
├── ETFTracker.Web/                    # Frontend (Angular, standalone components)
│   ├── src/
│   │   ├── app/
│   │   │   ├── pages/
│   │   │   │   ├── landing/           # Public landing page
│   │   │   │   ├── dashboard/         # Main authenticated dashboard
│   │   │   │   ├── login/             # Login page
│   │   │   │   └── auth-callback/     # OAuth callback
│   │   │   │
│   │   │   ├── components/
│   │   │   │   ├── add-transaction-modal/  # Record a new buy
│   │   │   │   ├── buy-history-modal/      # View buy history per holding
│   │   │   │   ├── sell-modal/             # 3-step sell workflow
│   │   │   │   ├── import-history-modal/   # CSV bulk import
│   │   │   │   ├── tax-history-modal/      # Tax events viewer
│   │   │   │   ├── user-settings-modal/    # Tax defaults & investor profile
│   │   │   │   └── share-profile-modal/    # Portfolio sharing
│   │   │   │
│   │   │   ├── services/
│   │   │   │   ├── api.service.ts          # HTTP client wrapper
│   │   │   │   ├── auth.service.ts         # Auth state
│   │   │   │   ├── csv-parser.service.ts   # CSV parsing & broker presets
│   │   │   │   ├── seo.service.ts          # Meta tags & SEO
│   │   │   │   └── sharing-context.service.ts
│   │   │   │
│   │   │   ├── guards/
│   │   │   ├── interceptors/
│   │   │   ├── app.config.ts          # Standalone app config
│   │   │   ├── app.routes.ts          # Route definitions
│   │   │   └── app.ts                 # Root component
│   │   │
│   │   ├── assets/                    # Static assets
│   │   ├── environments/              # Environment configs
│   │   ├── styles.css                 # Global styles
│   │   ├── main.ts                    # Entry point
│   │   └── index.html
│   │
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   └── README.md
│
├── database_schema.sql                # Database DDL
├── migration_update_purchase_date.sql
├── EXPORT_DATA_FOR_RENDER.sql
│
├── Documentation/
│   ├── SETUP_GUIDE.md
│   ├── DEVELOPER_QUICK_REFERENCE.md
│   ├── DEPLOYMENT_GUIDE.md
│   ├── DEPLOYMENT_QUICK_REFERENCE.txt
│   ├── DOCUMENTATION_INDEX.md
│   ├── ETF_DESCRIPTION_SEARCH.md
│   ├── GOOGLE_OAUTH_FIX.md
│   ├── PRICE_SOURCE_TRACKING_COMPLETE.md
│   └── [other guides]
│
├── Bob Esponja.sln                    # Visual Studio solution
├── Dockerfile
├── docker-compose.yml
├── global.json
├── quick-start.sh
├── deploy-to-render.sh
└── README.md                          # This file
```

## 📦 Prerequisites

### Required Software
- **PostgreSQL** 12 or higher
- **.NET SDK** 10.0 or higher
- **Node.js** 20+ and **npm** 11+
- **Git** for version control

### Optional Tools
- **JetBrains Rider** (recommended for C# development)
- **Visual Studio Code** or **Visual Studio**
- **Angular CLI**: `npm install -g @angular/cli`
- **Docker** & **Docker Compose** (for containerized deployment)

### API Keys Required
- **Eodhd API Key** (https://eodhd.com)
  - Provides primary price data for ETFs
  - Free tier available with rate limits
  - Get your key and add to configuration

### Database
- PostgreSQL server (local or remote)
- Database creation privileges
- Valid connection credentials

## 🚀 Setup & Installation

### 1. Clone the Repository

```bash
git clone <repository-url>
cd "Bob Esponja"
```

### 2. Database Setup

#### Option A: Using SQL File
```bash
# Create database
psql -U denisbahia -c "CREATE DATABASE etf_tracker;"

# Import schema
psql -U denisbahia -d etf_tracker -f database_schema.sql
```

#### Option B: Using Entity Framework Migrations
```bash
cd ETFTracker.Api
dotnet ef database update
cd ..
```

#### Option C: Using Docker (if available)
```bash
docker-compose up -d postgres
# Wait for postgres to start
dotnet ef database update
```

### 3. Backend Setup

```bash
cd ETFTracker.Api

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Verify build success
dotnet --version
```

### 4. Frontend Setup

```bash
cd ETFTracker.Web

# Install npm dependencies
npm install

# Verify installation
npm list

# Optional: Build for production
npm run build
```

### 5. Verify Installation

```bash
# Backend version check
dotnet --version

# Node/npm version check
node --version
npm --version
```

## ⚙️ Configuration

### Backend Configuration

Edit `ETFTracker.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=etf_tracker;Username=denisbahia;Password=postgres;"
  },
  "ExternalApis": {
    "EodhApi": {
      "ApiKey": "YOUR_EODHD_API_KEY_HERE",
      "BaseUrl": "https://api.eodhd.com"
    },
    "YahooFinance": {
      "Enabled": true,
      "BaseUrl": "https://query1.finance.yahoo.com"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200", "https://yourdomain.com"]
  }
}
```

### Environment-Specific Configuration

**Development** (`appsettings.Development.json`):
- Override default settings for local development
- Enable detailed logging
- Use local database connection

**Production** (`appsettings.Production.json`):
- Secure connection strings
- API keys from environment variables
- Optimized logging levels

### Frontend Configuration

Environment files in `ETFTracker.Web/src/environments/`:

```typescript
// environment.ts (Development)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};

// environment.prod.ts (Production)
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com'
};
```

## ▶️ Running the Application

### Start Backend API

```bash
cd ETFTracker.Api

# Run in development mode
dotnet run

# Run on specific port
dotnet run --urls "https://localhost:5001"

# Run in release mode
dotnet run --configuration Release
```

**API Available At:**
- Base URL: `https://localhost:5000`
- API Docs: `https://localhost:5000/swagger`
- OpenAPI JSON: `https://localhost:5000/swagger/v1/swagger.json`

### Start Frontend Application

**In a new terminal:**

```bash
cd ETFTracker.Web

# Development server with live reload
npm start

# Specify port
npm start -- --port 4300

# Build for production
npm run build -- --configuration production
```

**Frontend Available At:**
- URL: `http://localhost:4200`
- Live reload enabled on file changes

### Quick Start Script (macOS/Linux)

```bash
# Make executable
chmod +x quick-start.sh

# Run script (starts both backend and frontend)
./quick-start.sh
```

### Docker Deployment

```bash
# Build and start containers
docker-compose up -d

# View logs
docker-compose logs -f

# Stop containers
docker-compose down
```

## 📡 API Endpoints

### Holdings Management

```http
GET    /api/holdings              # List all holdings
GET    /api/holdings/{id}         # Get holding details
POST   /api/holdings              # Create holding
PUT    /api/holdings/{id}         # Update holding
DELETE /api/holdings/{id}         # Delete holding
GET    /api/holdings/{id}/history # Buy transaction history
GET    /api/holdings/search?q=    # Ticker/instrument search (Yahoo Finance)
```

### Transactions

```http
POST   /api/transactions          # Add transaction
GET    /api/transactions/{id}     # Get transaction details
PUT    /api/transactions/{id}     # Update transaction
DELETE /api/transactions/{id}     # Delete transaction
```

### Sell Holdings

```http
POST   /api/holdings/{id}/sell/preview  # Preview FIFO cost basis & tax before selling
POST   /api/holdings/{id}/sell/confirm  # Confirm and record the sale
GET    /api/holdings/{id}/sell-history  # Sell history for a holding
```

### Projections

```http
GET    /api/projections/{holdingId}     # Get projection
GET    /api/projections/settings        # Get settings
POST   /api/projections/settings        # Update settings
POST   /api/projections/calculate       # Calculate projection
```

### Price Data

```http
GET    /api/prices/{ticker}            # Current price
GET    /api/prices/{ticker}/history    # Historical prices
POST   /api/prices/refresh             # Refresh all prices
```

### Portfolio Sharing

```http
POST   /api/sharing/invite             # Invite user to share
GET    /api/sharing/shares             # List shares (my portfolio)
GET    /api/sharing/shared-with-me     # List portfolios shared with me
DELETE /api/sharing/{shareId}          # Revoke share
PUT    /api/sharing/{shareId}          # Update share permissions
```

### Dashboard

```http
GET    /api/dashboard                  # Dashboard summary
GET    /api/dashboard/performance      # Performance metrics
GET    /api/dashboard/summary          # Portfolio summary
```

### Tax Events

```http
GET    /api/tax-events                 # All tax events (deemed disposal + sells)
GET    /api/tax-events?holdingId={id}  # Tax events filtered by holding
```

### User Settings

```http
GET    /api/user-settings/tax-defaults          # Get investor profile & tax rates
PUT    /api/user-settings/tax-defaults          # Update investor profile & tax rates
DELETE /api/user-settings/tax-defaults/reset    # Reset to defaults
```

### Investment Goal

```http
GET    /api/goal                       # Get the user's investment goal
PUT    /api/goal                       # Create or replace the investment goal
```

### Asset Type Defaults

```http
GET    /api/asset-type-defaults        # Get deemed-disposal defaults per asset type
POST   /api/asset-type-defaults        # Upsert deemed-disposal default for an asset type
```

## 🎨 Features Guide

### Using the Dashboard

1. **View Portfolio Overview**
   - See total portfolio value
   - Monitor multi-period performance (Daily/Weekly/Monthly/YTD)
   - Track top gainers and losers
   - See progress toward your investment goal

2. **Add New Holdings**
   - Click "Add Transaction"
   - Search for a ticker by name or symbol (powered by Yahoo Finance)
   - Specify quantity, purchase price, and date
   - Toggle deemed disposal on/off per transaction
   - Automatic price fetching begins

3. **Monitor Performance**
   - Real-time price updates
   - Gain/loss calculations
   - Performance comparisons

### Selling Holdings

1. **Open Sell Modal**
   - Click the sell button next to a holding
   - **Step 1**: Enter quantity to sell, sell price, and date

2. **Review Tax Preview**
   - **Step 2**: See FIFO cost basis, total profit, and tax calculation
   - Tax type is auto-detected (Exit Tax for Irish ETFs, CGT for equities)
   - Edit the tax rate directly on screen if needed

3. **Confirm Sale**
   - **Step 3**: Confirmation with full sale record
   - Sale is recorded in the holding's sell history and tax events ledger

### Importing Transaction History (CSV)

1. **Open Import Modal** from the dashboard
2. **Upload** a CSV file from your broker (or use the native Investments Tracker format)
3. **Resolve Tickers** - the import wizard auto-detects ISINs and searches Yahoo Finance
   - Rows showing "needs picker" have multiple matches — select the correct ticker
   - Rows with errors can be fixed with inline search or deleted
4. **Preview** all rows before committing — sort and review quantities, prices, and dates
5. **Import** — transactions are bulk-inserted into your portfolio

### Sharing Portfolios

1. **Share Your Portfolio**
   - Open Share Profile Modal
   - Enter email address
   - Select permissions (Read/Edit)
   - Send invite

2. **View Shared Portfolios**
   - Check "Shared with Me" tab
   - Click portfolio to view
   - Access read-only or editable data

3. **Manage Shares**
   - Revoke access anytime
   - Update permissions
   - Track share status

### Using Projections

1. **Configure Settings**
   - Set monthly investment amount
   - Annual increase percentage
   - Expected yearly return
   - Tax settings (Deemed Disposal, exit tax)

2. **Generate Projections**
   - View 5-year, 10-year, 20-year scenarios
   - Adjust parameters dynamically
   - Export projection data

### Tax Center

1. **View Tax Events**
   - Navigate to the Tax Center from any holding
   - See all deemed disposal events and sell taxes in a unified ledger
   - Filter by holding or view all events

2. **Tax Summary**
   - Total tax liability across all events
   - Annual CGT allowance automatically deducted
   - Per-event breakdown: proceeds, cost basis, profit, tax due

### User Tax Settings

1. **First-Time Setup**
   - Prompted on first login to configure your investor profile

2. **Investor Profile**
   - Toggle between Irish Investor and Non-Irish Investor
   - Irish defaults: Exit Tax 38%, Deemed Disposal 38%, CGT 38%, €1,270 allowance
   - Non-Irish defaults: CGT 38%, €3,000 allowance

3. **Customise Rates**
   - Override any rate for your personal situation
   - Changes apply to future sell previews and projections

### Investment Goal

1. **Set Your Goal**
   - Enter a target portfolio value (EUR) and target date
   - Saved against your account

2. **Track Progress**
   - Dashboard shows current value vs goal
   - Progress bar updates in real-time as prices change

---

## 🔧 Development

### Project Architecture

#### Backend Pattern: Service-Oriented
- **Controllers**: HTTP request/response handling
- **Services**: Business logic and calculations
- **Models**: Domain entities
- **DTOs**: Data transfer between layers
- **Migrations**: Version-controlled schema changes

#### Frontend Pattern: Component-Based
- **Components**: Reusable UI elements
- **Services**: API communication and state
- **Models**: TypeScript interfaces
- **Pipes**: Data transformation
- **Directives**: DOM manipulation

### Running Tests

#### Backend
```bash
cd ETFTracker.Api
dotnet test
```

#### Frontend
```bash
cd ETFTracker.Web
npm test
npm run test:coverage
```

### Building for Production

#### Backend
```bash
cd ETFTracker.Api
dotnet publish -c Release -o ./publish
```

#### Frontend
```bash
cd ETFTracker.Web
npm run build -- --configuration production
# Output in dist/etf-tracker-web
```

### Code Standards & Style Guide

**C# Backend**
- Follow Microsoft C# Coding Conventions
- Use meaningful variable names
- XML documentation comments
- Async/await patterns

**TypeScript/Angular**
- Follow Google Angular Style Guide
- Typed components and services
- RxJS observable patterns
- Reactive forms

**Styling (SCSS)**
- BEM naming convention
- CSS variables for theming
- Mobile-first responsive design
- Dark theme color palette

---

## 🐛 Troubleshooting

### Common Issues

#### Database Connection Fails
```bash
# Verify PostgreSQL is running
psql -U denisbahia

# Check connection string in appsettings.json
# Verify database exists
psql -U denisbahia -l

# Test connection
psql -U denisbahia -d etf_tracker -c "SELECT 1"
```

#### API Port Already in Use
```bash
# Find process using port 5000
lsof -i :5000

# Kill process
kill -9 <PID>

# Or use different port
dotnet run --urls "https://localhost:5001"
```

#### CORS Issues
- Check `appsettings.json` Cors section
- Verify frontend and backend URLs match
- Ensure credentials are included in requests

#### Price Data Not Updating
```bash
# Verify API key
echo $EODHD_API_KEY

# Check API rate limits
# Monitor logs for API errors
dotnet run --configuration Debug

# Fallback to Yahoo Finance should activate automatically
```

#### Frontend Build Errors
```bash
# Clear node_modules and reinstall
rm -rf node_modules
npm install

# Clear Angular cache
npm run ng:cache:clean

# Rebuild
npm run build
```

#### Authentication Issues
- Verify JWT configuration
- Check token expiration
- Clear browser storage
- Ensure API key is valid

### Debug Mode

#### Backend Debug
```bash
cd ETFTracker.Api
dotnet run --configuration Debug
# Add breakpoints in IDE
# Use debug console for inspection
```

#### Frontend Debug
```bash
cd ETFTracker.Web

# Development mode with source maps
npm start

# Browser DevTools
# Chrome: F12 → Sources tab
```

#### Database Query Debugging
```bash
# Enable SQL logging in appsettings.Development.json
"Microsoft.EntityFrameworkCore": "Debug"

# View generated SQL in output
```

---

## 📚 Documentation

Complete documentation available:

- **[SETUP_GUIDE.md](SETUP_GUIDE.md)** - Detailed installation guide
- **[DEVELOPER_QUICK_REFERENCE.md](DEVELOPER_QUICK_REFERENCE.md)** - Dev reference
- **[DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)** - Production deployment
- **[DATABASE_EXPORT_GUIDE.md](DATABASE_EXPORT_GUIDE.md)** - Data export procedures
- **[PRICE_SOURCE_TRACKING_COMPLETE.md](PRICE_SOURCE_TRACKING_COMPLETE.md)** - Price source details
- **[SUPPORTED_ASSET_CLASSES.md](SUPPORTED_ASSET_CLASSES.md)** - Supported investment types
- **[YOUTUBE_CHANNEL.md](YOUTUBE_CHANNEL.md)** - YouTube channel kit (descriptions, scripts, banners)
- **[DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)** - Full documentation index

## 🤝 Contributing

### Development Workflow

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Write clean, well-documented code
   - Follow style guidelines
   - Add tests for new features

3. **Commit Changes**
   ```bash
   git commit -m "feat: add new feature description"
   git commit -m "fix: resolve issue description"
   ```

4. **Push to Repository**
   ```bash
   git push origin feature/your-feature-name
   ```

5. **Submit Pull Request**
   - Describe changes clearly
   - Link related issues
   - Request code review

### Code Quality Checklist

- ✅ All tests pass
- ✅ Code follows style guidelines
- ✅ No console warnings/errors
- ✅ Documentation updated
- ✅ Performance optimizations reviewed
- ✅ Security implications considered

---

## 📝 License

This project is **proprietary and confidential**. Unauthorized copying, modification, or distribution is strictly prohibited.

For licensing inquiries, contact the project maintainers.

---

## 📞 Support & Contact

### Getting Help

1. **Check Documentation**
   - Review setup and developer guides
   - Search existing issues and discussions

2. **Consult Quick References**
   - Developer quick reference guide
   - Deployment quick reference
   - API documentation

3. **Review Code Comments**
   - Check recent commits for context
   - Review migration files
   - Check commit messages

### Report Issues

- Provide detailed error messages
- Include system information
- Specify steps to reproduce
- Attach relevant logs

---

## 📊 Project Metadata

| Property | Value |
|---|---|
| **Name** | Investments Tracker (Bob Esponja) |
| **Version** | 1.0.0+ |
| **Status** | Active Development |
| **Last Updated** | April 2026 |
| **License** | Proprietary |
| **Framework** | .NET 10 + Angular 21 |
| **Database** | PostgreSQL 12+ |

---

## 🔗 Quick Links

- **API Documentation**: `/swagger`
- **Database Schema**: `database_schema.sql`
- **Project Specs**: `project.md`
- **Docker Setup**: `Dockerfile`
- **Deploy Script**: `deploy-to-render.sh`

---

**Built with ❤️ for modern portfolio management**

**Questions?** Check the docs folder or review recent commit messages for implementation details.

