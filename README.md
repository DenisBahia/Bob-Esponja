# ETF Investment Tracker

A comprehensive full-stack web application for managing and tracking ETF (Exchange-Traded Fund) investments across multiple brokers. Designed specifically for Irish investors to manage portfolio holdings, track performance, and handle tax obligations like deemed disposal.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Setup & Installation](#setup--installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Endpoints](#api-endpoints)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)

## 🎯 Overview

ETF Investment Tracker is designed to help investors:
- **Manage Holdings**: Track ETF positions across multiple brokers
- **Monitor Performance**: Real-time gain/loss tracking across different time periods (Daily, Weekly, Monthly, YTD)
- **Track Transactions**: Maintain a complete history of purchases and transactions
- **Tax Compliance**: Calculate and monitor tax obligations (e.g., deemed disposal for Irish investors)
- **Price Tracking**: Automatic price updates from multiple sources (Eodhd API with Yahoo Finance fallback)

## ✨ Key Features

### Dashboard & Holdings Management
- **Multi-Period Performance Dashboard**: View gains/losses across Daily, Weekly, Monthly, and YTD periods
- **Holdings Table**: Comprehensive view of all positions with:
  - Ticker and ETF name
  - Quantity and average cost
  - Current price and total value
  - Performance metrics for multiple time periods
  - Buy history and transaction tracking
  
- **Add Transactions**: Easy modal interface to record new purchases with:
  - Ticker selection
  - Quantity and purchase price
  - Purchase date tracking

### Performance & Analytics
- **Real-time Price Updates**: Automatic price fetching from Eodhd API with Yahoo Finance fallback
- **Historical Data**: Daily price snapshots for trend analysis
- **Multi-period Calculations**: Simultaneous tracking of Daily, Weekly, Monthly, and YTD performance
- **Buy History**: Complete transaction history for each holding

### Tax & Projections
- **Projection Settings**: Configure investment scenarios with:
  - Monthly buy amounts and annual increases
  - Expected yearly returns and inflation rates
  - Capital Gains Tax (CGT) and exit tax settings
  - Deemed disposal exclusion options
  
- **Investment Projections**: Model future portfolio performance based on configurable parameters

## 🛠 Tech Stack

### Backend
- **Framework**: .NET 10 (C# with WebAPI)
- **ORM**: Entity Framework Core 10.0
- **Database**: PostgreSQL 12+
- **HTTP Client**: HttpClientFactory with Polly for resilience
- **API Documentation**: OpenAPI/Swagger

### Frontend
- **Framework**: Angular 21 (latest LTS)
- **Language**: TypeScript 5.9
- **Styling**: CSS with responsive design
- **Charting**: Chart.js 4.5
- **Package Manager**: npm 11.11

### External APIs
- **Primary Price Source**: Eodhd API
- **Fallback Source**: Yahoo Finance API
- **Resilience**: Automatic fallback mechanism for price fetching

## 📁 Project Structure

```
Bob Esponja/
├── ETFTracker.Api/                 # Backend (.NET)
│   ├── Controllers/                # API endpoints
│   │   ├── HoldingsController.cs
│   │   └── ProjectionsController.cs
│   ├── Services/                   # Business logic
│   │   ├── HoldingsService.cs
│   │   ├── PriceService.cs
│   │   └── ProjectionService.cs
│   ├── Models/                     # Domain models
│   │   ├── Holding.cs
│   │   ├── Transaction.cs
│   │   ├── User.cs
│   │   ├── ProjectionSettings.cs
│   │   └── PriceSnapshot.cs
│   ├── Dtos/                       # Data Transfer Objects
│   ├── Data/                       # Database context
│   │   └── AppDbContext.cs
│   ├── Migrations/                 # EF Core migrations
│   ├── Program.cs                  # Application startup
│   └── appsettings.json           # Configuration
│
├── ETFTracker.Web/                 # Frontend (Angular)
│   ├── src/
│   │   ├── app/                    # Angular components and services
│   │   ├── assets/                 # Static assets
│   │   ├── environments/           # Environment configurations
│   │   ├── main.ts                 # Entry point
│   │   └── styles.css              # Global styles
│   ├── angular.json                # Angular CLI config
│   ├── package.json                # Dependencies
│   └── tsconfig.json               # TypeScript config
│
├── Database/
│   └── database_schema.sql         # Database schema
│
├── Documentation/                  # Project documentation
│   ├── SETUP_GUIDE.md
│   ├── DEVELOPER_QUICK_REFERENCE.md
│   ├── DEPLOYMENT_GUIDE.md
│   └── ...
│
└── Configuration files
    ├── global.json
    └── Bob Esponja.sln             # Visual Studio solution file
```

## 📦 Prerequisites

### Required Software
- **PostgreSQL** 12 or higher
- **.NET SDK** 10.0 or higher
- **Node.js** 20+ and npm 11+
- **Git** for version control

### Optional Tools
- **Visual Studio Code** or **Visual Studio** for development
- **JetBrains Rider** for C# development (recommended)
- **Angular CLI**: `npm install -g @angular/cli`

### API Keys Required
- **Eodhd API Key**: Get one at https://eodhd.com
  - Provides primary price data for ETFs
  - Free tier available with request limits
  
### Database
- PostgreSQL server running locally or remote
- Appropriate credentials for database creation

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
psql -U postgres -c "CREATE DATABASE etf_tracker;"

# Run schema
psql -U postgres -d etf_tracker -f database_schema.sql
```

#### Option B: Using Entity Framework Migrations
```bash
cd ETFTracker.Api
dotnet ef database update
cd ..
```

### 3. Backend Setup

```bash
cd ETFTracker.Api

# Install NuGet packages
dotnet restore

# Build the project
dotnet build
```

### 4. Frontend Setup

```bash
cd ETFTracker.Web

# Install npm dependencies
npm install

# Build frontend (optional)
npm run build
```

## ⚙️ Configuration

### Backend Configuration

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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**Important Configuration Keys:**
- `ConnectionStrings:DefaultConnection`: PostgreSQL connection string
- `ExternalApis:EodhApi:ApiKey`: Your Eodhd API key for price data

### Environment-Specific Configuration

- **Development**: `appsettings.Development.json` (overrides default)
- **Production**: `appsettings.Production.json` (if deployed)

### Frontend Configuration

Environment settings are in `ETFTracker.Web/src/environments/`:
- `environment.ts`: Development environment
- `environment.prod.ts`: Production environment

Configure API base URL if needed.

## ▶️ Running the Application

### Start Backend API

```bash
cd ETFTracker.Api
dotnet run

# The API will be available at:
# https://localhost:5000/api
# OpenAPI docs: https://localhost:5000/openapi/v1.json
```

### Start Frontend Application

**In a new terminal:**

```bash
cd ETFTracker.Web
npm start

# The application will be available at:
# http://localhost:4200
```

The frontend will automatically proxy API calls to the backend.

### Quick Start Script (macOS/Linux)

```bash
# Make the script executable
chmod +x quick-start.sh

# Run the script
./quick-start.sh
```

## 📡 API Endpoints

### Holdings Management

- **GET** `/api/holdings` - List all holdings with performance metrics
- **GET** `/api/holdings/{id}` - Get specific holding details
- **POST** `/api/holdings` - Create new holding
- **PUT** `/api/holdings/{id}` - Update holding
- **DELETE** `/api/holdings/{id}` - Delete holding
- **GET** `/api/holdings/{id}/history` - Get transaction history

### Projections

- **GET** `/api/projections/{holdingId}` - Get projection for holding
- **GET** `/api/projections/settings` - Get projection settings
- **POST** `/api/projections/settings` - Update projection settings
- **GET** `/api/projections/calculate` - Calculate projection

### Price Data

- **GET** `/api/prices/{ticker}` - Get current price for ticker
- **GET** `/api/prices/{ticker}/history` - Get historical prices

### Dashboard

- **GET** `/api/dashboard` - Get dashboard summary with all performance metrics

## 🔧 Development

### Project Structure Explained

#### Backend Architecture
- **Controllers**: Handle HTTP requests and return responses
- **Services**: Contain business logic and data processing
  - `HoldingsService`: Manages holdings operations
  - `PriceService`: Fetches and caches price data
  - `ProjectionService`: Calculates future projections
- **Models**: Domain entities representing database tables
- **DTOs**: Data Transfer Objects for API communication
- **Migrations**: Database schema versioning with EF Core

#### Frontend Architecture
- **Components**: Reusable UI building blocks
- **Services**: Handle API communication and business logic
- **Models**: TypeScript interfaces for type safety
- **Directives & Pipes**: Custom Angular functionality

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
```

### Building for Production

#### Backend
```bash
cd ETFTracker.Api
dotnet publish -c Release
```

#### Frontend
```bash
cd ETFTracker.Web
npm run build -- --configuration production
```

### Code Standards

- **C#**: Follow Microsoft's C# Coding Conventions
- **TypeScript/Angular**: Follow Google's Angular Style Guide
- **Formatting**: Use provided code formatter (Prettier for TypeScript)

## 📚 Key Documentation

- **[SETUP_GUIDE.md](SETUP_GUIDE.md)** - Complete installation and setup guide
- **[DEVELOPER_QUICK_REFERENCE.md](DEVELOPER_QUICK_REFERENCE.md)** - Quick reference for developers
- **[DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)** - Production deployment instructions
- **[DATABASE_SCHEMA.sql](database_schema.sql)** - Complete database schema
- **[project.md](project.md)** - Detailed project specifications

## 🤝 Contributing

### Development Workflow

1. Create a feature branch: `git checkout -b feature/your-feature-name`
2. Make your changes and commit: `git commit -m "Add your feature"`
3. Push to the branch: `git push origin feature/your-feature-name`
4. Open a Pull Request

### Code Quality

- Ensure tests pass before submitting PR
- Follow code style guidelines
- Write meaningful commit messages
- Update documentation as needed

## 🐛 Troubleshooting

### Common Issues

#### Database Connection Fails
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Ensure database exists and credentials are correct

#### API Port Already in Use
```bash
# Change the port in launchSettings.json or run on different port
dotnet run --urls "https://localhost:5001"
```

#### CORS Issues
- Check CORS policy in `Program.cs`
- Ensure frontend and backend URLs match configuration

#### Price Data Not Updating
- Verify Eodhd API key is correct
- Check API rate limits haven't been exceeded
- Fallback to Yahoo Finance should activate automatically

### Debug Mode

#### Backend
```bash
cd ETFTracker.Api
dotnet run --configuration Debug
```

#### Frontend
```bash
cd ETFTracker.Web
npm start -- --poll=2000
```

## 📝 License

This project is proprietary and confidential. Unauthorized copying or distribution is prohibited.

## 📞 Support & Contact

For issues, questions, or feature requests:
1. Check existing documentation in the docs folder
2. Review the developer quick reference guide
3. Check recent commit messages for context

---

**Last Updated**: April 2026

**Project Name**: ETF Investment Tracker (Bob Esponja)

**Version**: 1.0.0+

**Status**: Active Development

