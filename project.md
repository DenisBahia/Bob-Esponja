# ETF Investment Tracking Web Application

## Project Overview
A web application designed to help ETF investors in Ireland manage their investment portfolio across multiple brokers, track holdings, manage deemed disposal tax obligations, and visualize investment performance.

## Phase 1: Holdings Management & Tracking

### Purpose
Enable users to control and monitor all their ETF investments across different brokers with real-time price updates and performance tracking.

### Key Features

#### 1. Main Dashboard
- **Header Section (All Periods Displayed):**
    - Total holdings amount (EUR)
    - Daily gain/loss (EUR and %)
    - Weekly gain/loss (EUR and %)
    - Monthly gain/loss (EUR and %)
    - YTD gain/loss (EUR and %)

- **Holdings Table (All Periods Displayed):**
    - **Static Columns:**
        - Ticker
        - ETF Name
        - Quantity
        - Average Cost
        - Current Price
        - Total Value

    - **Dynamic Columns (One Set Per Period):**
        - Daily Gain/Loss (EUR and %)
        - Weekly Gain/Loss (EUR and %)
        - Monthly Gain/Loss (EUR and %)
        - YTD Gain/Loss (EUR and %)

    - **Actions:**
        - "Buy History" button - View transaction history for each holding
        - "Add New Buy" button - Open modal dialog to record new purchase

#### 2. Buy Transaction Modal
Opens when user clicks "Add New Buy" with fields:
- Ticker
- Quantity
- Purchase Price (EUR)
- Purchase Date

#### 3. Performance Tracking
- Daily snapshot storage for historical data
- All periods (Daily, Weekly, Monthly, YTD) calculated and displayed simultaneously
- Real-time gain/loss calculations for each period

### Technical Requirements

#### Backend
- **Framework:** .NET 6+ (C#)
- **Architecture:** RESTful API
- **Key Endpoints:**
    - GET /api/holdings - List all holdings with all period calculations
    - GET /api/holdings/{id}/history - Buy history for a specific holding
    - POST /api/holdings - Add new buy
    - GET /api/prices/{ticker} - Get current price for ticker
    - GET /api/dashboard - Dashboard summary data with all period metrics

#### Frontend
- **Framework:** Angular (latest LTS)
- **Key Components:**
    - Dashboard main page with multi-period header
    - Holdings table with multi-period columns
    - Add buy modal dialog
    - Buy history view
    - Responsive table layout for multiple period columns

#### Database
- **Type:** PostgreSQL
- **Key Tables:**
    - Users
    - Holdings (ticker, average cost, quantity)
    - Transactions (buys)
    - Price Snapshots (daily snapshots for historical tracking)

### External APIs

#### Price Data Sources
1. **Primary:** Eodhd API
    - Preferred provider
    - Daily request limit applies

2. **Fallback:** Yahoo Finance API
    - Used when Eodhd fails for specific ticker
    - Ensures price data availability

#### Price Fetching Strategy
- Attempt to fetch from Eodhd for each ticker
- If Eodhd fails for a specific ticker, automatically fall back to Yahoo Finance
- Note: Some tickers may fail on Eodhd but succeed on Yahoo Finance
- Store prices with timestamp for snapshot tracking

### Data Model Highlights

#### Holdings
- Ticker
- ETF Name
- Total Quantity
- Average Cost (EUR)
- Associated Broker

#### Transactions
- Ticker
- Quantity
- Purchase Price
- Purchase Date
- Associated Holding

#### Price Snapshots
- Ticker
- Price (EUR)
- Snapshot Date
- Source (Eodhd/Yahoo)

### User Workflows

#### Adding a New Buy
1. Click "Add New Buy" button
2. Fill in: Ticker, Quantity, Purchase Price, Purchase Date
3. Submit
4. System updates holding record and recalculates average cost
5. Dashboard updates automatically with recalculated period metrics

#### Viewing Buy History
1. In holdings table, click "Buy History" for desired ticker
2. View all transactions for that ticker with dates and prices
3. Close and return to main view

#### Monitoring Performance
1. Dashboard displays all period metrics (Daily, Weekly, Monthly, YTD) simultaneously
2. Header shows aggregate gains/losses for each period
3. Table shows per-holding gains/losses for each period
4. All metrics update in real-time as prices change

### UI Layout Notes
- Table columns should be organized for easy scanning (ticker, name, quantity, then current metrics, then all daily metrics, all weekly metrics, etc.)
- Consider horizontal scrolling or responsive collapsible columns for very wide layouts on smaller screens
- Header metrics displayed as cards or a summary row for clear visibility

### Future Phases (Out of Scope)
- Multi-broker aggregation and broker linking
- Deemed disposal tax calculations (8-year rule for Irish investors)
- Projected holdings projections
- Advanced analytics and graphs
- Annual goal setting and tracking
- Buy order recommendations

### Success Criteria for Phase 1
- ✅ Users can add new ETF purchases
- ✅ Dashboard displays accurate current holdings
- ✅ All four period metrics (Daily, Weekly, Monthly, YTD) visible simultaneously
- ✅ Real-time price updates from integrated APIs
- ✅ Accurate gain/loss calculations for each period
- ✅ Buy history accessible per holding
- ✅ Clean, intuitive user interface with multi-period visibility

---

**Tech Stack Summary:**
- Backend: .NET C# (REST API)
- Frontend: Angular
- Database: PostgreSQL
- External Services: Eodhd API, Yahoo Finance API
