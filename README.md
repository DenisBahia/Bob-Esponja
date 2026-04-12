# 🎯 ETF Investment Tracker

A modern, full-stack web application for managing and tracking ETF (Exchange-Traded Fund) investments with real-time performance analytics, portfolio sharing, and tax compliance tools designed for Irish investors.

**Live Demo** | **Documentation** | **Support**

---

## ✨ Quick Highlights

- 📊 **Real-time Portfolio Dashboard** - Multi-period performance tracking (Daily, Weekly, Monthly, YTD)
- 🔄 **Automatic Price Updates** - Powered by Eodhd API with Yahoo Finance fallback
- 👥 **Portfolio Sharing** - Share portfolios with other investors with granular permission controls
- 📈 **Investment Projections** - Model future portfolio performance with custom parameters
- 💰 **Tax Compliance** - Automatic deemed disposal calculations for Irish investors
- 🎨 **Modern UI** - Responsive dark-theme design with smooth animations

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

---

## 🎯 Overview

ETF Investment Tracker is a comprehensive portfolio management solution built for modern investors. Whether you're managing a simple portfolio or complex multi-broker holdings, this application provides the tools you need to:

- **Track Holdings** across multiple brokers with real-time pricing
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
│   │   ├── HoldingsController.cs      # Holdings management
│   │   ├── ProjectionsController.cs   # Projection endpoints
│   │   └── SharingController.cs       # Portfolio sharing
│   │
│   ├── Services/
│   │   ├── HoldingsService.cs         # Holdings business logic
│   │   ├── PriceService.cs            # Price fetching & caching
│   │   ├── ProjectionService.cs       # Projection calculations
│   │   └── SharingService.cs          # Portfolio sharing logic
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
├── ETFTracker.Web/                    # Frontend (Angular)
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── holdings-table/
│   │   │   │   ├── add-transaction-modal/
│   │   │   │   ├── share-profile-modal/
│   │   │   │   ├── projections/
│   │   │   │   └── [other components]
│   │   │   │
│   │   │   ├── services/
│   │   │   │   ├── api.service.ts
│   │   │   │   ├── holdings.service.ts
│   │   │   │   ├── price.service.ts
│   │   │   │   ├── sharing.service.ts
│   │   │   │   └── auth.service.ts
│   │   │   │
│   │   │   ├── models/
│   │   │   ├── directives/
│   │   │   ├── pipes/
│   │   │   ├── app.component.ts
│   │   │   ├── app.module.ts
│   │   │   └── app-routing.module.ts
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
GET    /api/holdings/{id}/history # Transaction history
```

### Transactions

```http
POST   /api/transactions          # Add transaction
GET    /api/transactions/{id}     # Get transaction details
PUT    /api/transactions/{id}     # Update transaction
DELETE /api/transactions/{id}     # Delete transaction
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

## 🎨 Features Guide

### Using the Dashboard

1. **View Portfolio Overview**
   - See total portfolio value
   - Monitor multi-period performance (Daily/Weekly/Monthly/YTD)
   - Track top gainers and losers

2. **Add New Holdings**
   - Click "Add Transaction"
   - Enter ticker symbol
   - Specify quantity and purchase price
   - Automatic price fetching begins

3. **Monitor Performance**
   - Real-time price updates
   - Gain/loss calculations
   - Performance comparisons

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
| **Name** | ETF Investment Tracker (Bob Esponja) |
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

