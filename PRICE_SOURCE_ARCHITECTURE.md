# Price Source Tracking - Data Flow Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend (Angular)                        │
│  Displays price + source badge (Eodhd/Yahoo/Cache/Unavailable)  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ GET /api/holdings
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                    ETFTracker.Api                                 │
│                                                                   │
│  HoldingsController                                              │
│    ↓                                                              │
│  HoldingsService.GetHoldingsAsync()                              │
│    ├─ ForEach holding:                                           │
│    │  ├─ priceResult = GetPriceWithSourceAsync(ticker)           │
│    │  ├─ holding.PriceSource = priceResult.Source              │
│    │  └─ Build HoldingDto with priceSource                      │
│    └─ SaveChangesAsync() → persist to DB                        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ Returns JSON with priceSource
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                    PriceService Layer                             │
│                                                                   │
│  GetPriceWithSourceAsync(ticker)                                 │
│    │                                                              │
│    ├─ Try Eodhd API                                              │
│    │  └─ if Success → return {Price, Source: "Eodhd"}          │
│    │  else ↓                                                      │
│    │                                                              │
│    ├─ Try Yahoo Finance API                                      │
│    │  └─ if Success → return {Price, Source: "Yahoo"}           │
│    │  else ↓                                                      │
│    │                                                              │
│    ├─ Query Price Snapshots (Cache)                              │
│    │  └─ if Found → return {Price, Source: "Cache"}             │
│    │  else ↓                                                      │
│    │                                                              │
│    └─ return {Price: null, Source: null}                         │
│                                                                   │
└────────────────────────┬────────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
          ▼              ▼              ▼
    ┌─────────────┐ ┌────────────┐ ┌──────────────┐
    │ Eodhd API   │ │ Yahoo API  │ │ PostgreSQL   │
    │ (Primary)   │ │ (Fallback) │ │ (Cache)      │
    └─────────────┘ └────────────┘ └──────────────┘
```

## Price Source Resolution Flow

```
START: Need Price for VWRL.XETRA
│
├─ Is Eodhd API available?
│  ├─ YES → Call Eodhd, get price
│  │        Return {price: 98.45, source: "Eodhd"} ✓
│  │
│  └─ NO → Next step
│
├─ Is Yahoo Finance API available?
│  ├─ YES → Call Yahoo, get price
│  │        Return {price: 98.45, source: "Yahoo"} ✓
│  │
│  └─ NO → Next step
│
├─ Is there a cached snapshot?
│  ├─ YES → Query database
│  │        Return {price: 98.40, source: "Cache"} ⚠️
│  │        (may be outdated)
│  │
│  └─ NO → Final step
│
└─ No data available
   Return {price: null, source: null} ❌
   Set priceUnavailable: true
```

## Database Schema Update

```sql
-- BEFORE
holdings
├── id (PK)
├── user_id (FK)
├── ticker
├── etf_name
├── quantity
├── average_cost
├── broker
├── created_at
└── updated_at

-- AFTER (new column added)
holdings
├── id (PK)
├── user_id (FK)
├── ticker
├── etf_name
├── quantity
├── average_cost
├── broker
├── price_source          ← NEW: tracks data source
├── created_at
└── updated_at
```

## API Response Structure

```json
{
  "header": {
    "totalHoldingsAmount": 50000.00,
    "totalVariation": { ... },
    "dailyMetrics": { ... }
  },
  "holdings": [
    {
      // Existing fields
      "id": 1,
      "ticker": "VWRL.XETRA",
      "etfName": "Vanguard FTSE All-World UCITS ETF",
      "quantity": 100.5,
      "averageCost": 98.20,
      "currentPrice": 98.45,
      "totalValue": 9895.225,
      "priceUnavailable": false,
      
      // NEW FIELD
      "priceSource": "Yahoo",
      
      // Existing metric fields
      "dailyMetrics": { ... },
      "weeklyMetrics": { ... },
      "monthlyMetrics": { ... },
      "ytdMetrics": { ... }
    }
  ]
}
```

## Code Flow: GetPriceWithSourceAsync

```csharp
public async Task<PriceResult> GetPriceWithSourceAsync(string ticker)
{
    // Step 1: Try Eodhd (Primary)
    var eodhPrice = await GetEodhPriceAsync(ticker);
    if (eodhPrice.HasValue)
    {
        await SavePriceSnapshotAsync(ticker, eodhPrice.Value, "Eodhd");
        return new PriceResult 
        { 
            Price = eodhPrice.Value, 
            Source = "Eodhd"  ← Primary source
        };
    }
    
    // Step 2: Try Yahoo (Fallback 1)
    var yahooPrice = await GetYahooPriceAsync(ticker);
    if (yahooPrice.HasValue)
    {
        await SavePriceSnapshotAsync(ticker, yahooPrice.Value, "Yahoo");
        return new PriceResult 
        { 
            Price = yahooPrice.Value, 
            Source = "Yahoo"  ← Fallback 1
        };
    }
    
    // Step 3: Check Cache (Fallback 2)
    var lastSnapshot = await _context.PriceSnapshots
        .Where(ps => ps.Ticker == ticker)
        .OrderByDescending(ps => ps.SnapshotDate)
        .FirstOrDefaultAsync();
    
    if (lastSnapshot != null)
    {
        return new PriceResult 
        { 
            Price = lastSnapshot.Price, 
            Source = lastSnapshot.Source ?? "Cache"  ← Fallback 2
        };
    }
    
    // Step 4: No data available
    return new PriceResult 
    { 
        Price = null, 
        Source = null  ← No data
    };
}
```

## Data Persistence Flow

```
1. API Request → GetHoldingsAsync()
                     ↓
2. For Each Holding → GetPriceWithSourceAsync()
                     ↓
3. Get Price + Source {98.45, "Yahoo"}
                     ↓
4. Update Holding Entity
   holding.PriceSource = "Yahoo"
                     ↓
5. Build HoldingDto
   dto.PriceSource = "Yahoo"
                     ↓
6. SaveChangesAsync()
   UPDATE holdings SET price_source = 'Yahoo'
                     ↓
7. Return to Frontend
   JSON includes priceSource: "Yahoo"
```

## Source Priority Matrix

```
┌─────────────┬──────────────┬───────────┬──────────────────┐
│ Source      │ Reliability  │ Freshness │ Used When        │
├─────────────┼──────────────┼───────────┼──────────────────┤
│ Eodhd       │ ⭐⭐⭐⭐⭐ │ Real-time │ Primary (always) │
│ Yahoo       │ ⭐⭐⭐⭐  │ Real-time │ Eodhd fails      │
│ Cache       │ ⭐⭐⭐     │ Stale     │ Both APIs fail   │
│ Unavailable │ ❌           │ N/A       │ Cache empty      │
└─────────────┴──────────────┴───────────┴──────────────────┘
```

## Frontend Rendering Options

### Option 1: Simple Label
```
VWRL.XETRA | $98.45 | Yahoo
```

### Option 2: Color Badge
```
VWRL.XETRA | $98.45 | [Yahoo Finance] (blue)
```

### Option 3: Icon + Label
```
VWRL.XETRA | $98.45 | 📊 Yahoo
```

### Option 4: Tooltip
```
VWRL.XETRA | $98.45  (hover: "Real-time price from Yahoo Finance")
```

## Migration Path

```
Old System (No tracking)
    │
    ├─ Price: 98.45
    ├─ Source: ??? (unknown)
    │
    ▼
New System (With tracking)
    │
    ├─ Price: 98.45
    ├─ Source: "Eodhd" (tracked)
    │
    ├─ Next refresh:
    ├─ Price: 98.50
    ├─ Source: "Yahoo"
    │
    └─ Users see data source for transparency
```

---

This visual flow shows how price sources are tracked, stored, and displayed throughout the system.

