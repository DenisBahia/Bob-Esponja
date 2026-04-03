# Price Source Tracking Implementation Summary

## Overview
Successfully implemented price source tracking for ETF holdings. The application now records which API (Eodhd, Yahoo Finance, or Cache) provided the current price for each holding.

## Changes Made

### 1. Database Schema
- **File**: Holdings table in PostgreSQL
- **Change**: Added `price_source VARCHAR(50)` column
- **SQL**: `ALTER TABLE holdings ADD COLUMN price_source VARCHAR(50);`
- **Status**: ✅ Applied directly to database

### 2. Model Layer

#### Holding.cs (Model)
```csharp
// Added property with MaxLength annotation
[MaxLength(50)]
public string? PriceSource { get; set; }
```

#### HoldingDto.cs (DTO)
```csharp
// Added property to expose price source to frontend
public string? PriceSource { get; set; }
```

### 3. Service Layer

#### IPriceService.cs
- Added new interface method: `Task<PriceResult> GetPriceWithSourceAsync(string ticker, CancellationToken cancellationToken = default);`
- Added new DTO: `PriceResult` with `Price` and `Source` properties

#### PriceService.cs
- Refactored `GetPriceAsync()` to call `GetPriceWithSourceAsync()` and return only the price
- Implemented `GetPriceWithSourceAsync()` that returns both price and source:
  - Tries Eodhd API first, returns `{Price, Source: "Eodhd"}`
  - Falls back to Yahoo Finance, returns `{Price, Source: "Yahoo"}`
  - Falls back to cached price snapshots, returns `{Price, Source: lastSnapshot.Source ?? "Cache"}`
  - Returns `{Price: null, Source: null}` if all sources fail

#### HoldingsService.cs
- Updated `GetHoldingsAsync()` to:
  - Use `GetPriceWithSourceAsync()` instead of `GetPriceAsync()`
  - Store the price source in the holding entity: `holding.PriceSource = priceResult.Source`
  - Include price source in the returned DTO: `PriceSource = priceResult.Source`
  - Save changes to persist the price source to the database

### 4. Migration Files
- Created migration: `20260402120239_AddPriceSourceToHoldings.cs`
- Created designer file: `20260402120239_AddPriceSourceToHoldings.Designer.cs`
- Updated: `AppDbContextModelSnapshot.cs` with the new PriceSource property

## How It Works

1. **Price Fetching**: When holdings are retrieved, the system calls `GetPriceWithSourceAsync()`
2. **Source Detection**: The method tracks which API provided the price (Eodhd, Yahoo, or Cache)
3. **Storage**: The price source is saved to the `holdings.price_source` column in the database
4. **API Response**: The price source is included in the `HoldingDto` sent to the frontend
5. **Display**: The frontend can now show which source provided each ETF's current price

## Supported Price Sources

| Source | Description |
|--------|-------------|
| `Eodhd` | Price from Eodhd API (primary source) |
| `Yahoo` | Price from Yahoo Finance API (fallback) |
| `Cache` | Price from cached snapshots (fallback when both APIs fail) |
| `null` | Price unavailable from all sources |

## Example Response

```json
{
  "id": 1,
  "ticker": "VWRL.XETRA",
  "etfName": "Vanguard FTSE All-World UCITS ETF",
  "quantity": 100,
  "currentPrice": 98.45,
  "totalValue": 9845,
  "priceUnavailable": false,
  "priceSource": "Yahoo",
  "weeklyMetrics": { ... },
  "monthlyMetrics": { ... }
}
```

## Benefits

✅ Transparency: Users can see which data source their prices come from
✅ Debugging: Easier to diagnose price-related issues
✅ Data Quality: Helps understand price reliability
✅ Auditing: Track when cached vs. real-time data is being used
✅ Fallback Tracking: Know when systems are relying on cached data

## Testing

Build Status: ✅ Successful (0 errors, 0 warnings)

To test:
1. Start the application: `dotnet run`
2. Call the `/api/holdings` endpoint
3. Each holding will include `priceSource` field showing the source of its current price

