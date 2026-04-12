# 📊 Investments Tracker - Supported Asset Classes

## Overview

**Investments Tracker** now supports tracking a wide range of investment types beyond just ETFs. The platform can manage any security or asset class with available pricing data from Yahoo Finance or Eodhd API.

## Supported Investment Types

### 1. 📈 **Exchange-Traded Funds (ETFs)**
- All ETF types and markets
- Multiple currencies and exchanges
- Sector-specific, regional, and global ETFs
- **Example sources**: 
  - [Yahoo Finance ETFs](https://finance.yahoo.com/etfs)
  - European exchanges (XETRA, LSE, etc.)

### 2. 📊 **Stocks / Equities**
- Individual company shares
- All major exchanges (NYSE, NASDAQ, etc.)
- International stocks
- Blue-chip and growth stocks
- **Example sources**:
  - [Yahoo Finance Most Active](https://finance.yahoo.com/most-active)
  - [NASDAQ Listed Companies](https://www.nasdaq.com/)
  - [NYSE Listed Companies](https://www.nyse.com/)

### 3. 💼 **Mutual Funds**
- Traditional mutual funds
- Index funds
- Active/passive management
- **Example sources**:
  - [Yahoo Finance Mutual Funds](https://finance.yahoo.com/mutualfunds)

### 4. 🪙 **Cryptocurrencies & Digital Assets**
- Bitcoin (BTC)
- Ethereum (ETH)
- Altcoins and tokens
- Stablecoins
- **Example sources**:
  - [Yahoo Finance Cryptocurrencies](https://finance.yahoo.com/cryptocurrencies)

### 5. 💱 **Foreign Exchange (Forex)**
- Currency pairs (EUR/USD, GBP/JPY, etc.)
- Cross rates
- Exotic pairs
- **Example sources**:
  - [Yahoo Finance Currencies](https://finance.yahoo.com/currencies)

### 6. 🛢️ **Commodities**
- Precious metals (Gold, Silver, Platinum)
- Energy (Crude Oil, Natural Gas)
- Agricultural products (Wheat, Corn, Soybeans)
- Industrial metals
- **Example sources**:
  - [Yahoo Finance Commodities](https://finance.yahoo.com/commodities)

### 7. 📉 **Futures Contracts**
- Commodity futures
- Financial futures (Treasury, S&P 500)
- Energy futures
- Currency futures
- **Example sources**:
  - [Yahoo Finance Futures](https://finance.yahoo.com/futures)

### 8. 🌍 **World Indices**
- Major stock indices (S&P 500, DAX, FTSE, Nikkei)
- Regional indices
- Bond indices
- Sector indices
- **Example sources**:
  - [Yahoo Finance World Indices](https://finance.yahoo.com/world-indices)

## Key Capabilities for All Asset Types

### Real-Time Pricing 📍
- **Primary Source**: Eodhd API (Enterprise-grade data)
- **Fallback**: Yahoo Finance (Reliable secondary source)
- **Smart Caching**: Reduces API calls while maintaining accuracy
- **Source Badge**: Display shows which source provided the current price

### Multi-Period Analytics 📊
For any asset, track performance across:
- **Daily**: Today's change
- **Weekly**: Last 7 days
- **Monthly**: Last 30 days
- **Year-to-Date**: Performance this calendar year
- **Custom Periods**: Any timeframe

### Portfolio Management 💼
- Mix multiple asset types in a single portfolio
- Track allocation across different asset classes
- Rebalancing recommendations
- Performance attribution by asset type

### Irish Tax Compliance ✅
- Automatic deemed disposal calculations
- Exit tax modeling
- SIA (Société d'Investissement à Capital Variable) support
- Tax event tracking and reporting

### Advanced Features 🚀
- **Historical Tracking**: See portfolio evolution over time
- **Projections**: Model future wealth with custom assumptions
- **Portfolio Sharing**: Share read-only or editable views
- **Transaction History**: Complete buy/sell history per asset
- **Performance Analysis**: Per-purchase gain/loss tracking

## Data Source Integration

### Yahoo Finance Coverage
All asset types are available through Yahoo Finance:
- 150,000+ ETFs
- Millions of stocks globally
- Crypto and forex pairs
- Commodity contracts
- Indices and benchmarks

### Eodhd API Coverage
Enterprise-grade data provider:
- Historical end-of-day prices
- Technical indicators
- Fundamental data
- Multiple exchanges and markets

## Usage Examples

### Example 1: Global Diversified Portfolio
```
- VWCE (ETF): Vanguard World
- GOOGL (Stock): Google
- BTC-USD (Crypto): Bitcoin
- EUR/USD (Forex): Euro
- GLD (Commodity ETF): Gold
```
All tracked in a single portfolio with unified performance metrics.

### Example 2: Income Portfolio
```
- VTI (ETF): Total US Stock Market
- SCHD (ETF): Dividend ETF
- JNJ (Stock): Johnson & Johnson
- T (Stock): AT&T
```
Track yields and dividend yields across all holdings.

### Example 3: Speculative Trading
```
- CL=F (Crude Oil Futures)
- BTC-USD (Bitcoin)
- NQ=F (Nasdaq 100 Futures)
- TSLA (Tesla Stock)
```
Monitor volatile assets with real-time price updates.

## Getting Started with Different Asset Types

1. **Sign in** to your Investments Tracker account
2. **Add New Investment** - Enter the ticker/symbol
3. **Enter Details**:
   - Number of units/shares/contracts
   - Purchase price/entry price
   - Purchase date
4. **Track** - Real-time pricing and analytics
5. **Analyze** - View performance, projections, and tax impact

## Supported Data Features by Asset Type

| Feature | Stocks | ETFs | Crypto | Forex | Commodities | Indices |
|---------|--------|------|--------|-------|-------------|---------|
| Real-time prices | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Historical data | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Multi-period analytics | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tax tracking | ✅* | ✅* | ✅ | ✅ | ✅ | ✅* |
| Dividends/Yields | ✅ | ✅ | ❌ | ❌ | Varies | ✅ |
| Projections | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

*Irish tax rules apply

## Future Enhancements

Planned features for expanded asset class support:
- 🔄 Crypto staking/yield tracking
- 💵 Multi-currency portfolio support
- 📊 Sector rotation analysis
- 🎯 Factor-based portfolio analysis
- 🤖 ML-powered rebalancing suggestions
- 🌐 Real-time forex conversion

## Technical Requirements

- Valid ticker/symbol recognized by Yahoo Finance or Eodhd
- Supporting data availability
- Real-time price coverage (varies by region/asset)

---

**Investments Tracker** - Track Everything, Invest Smarter 📈

For questions or suggestions on supported assets, please contact support or check the documentation.

