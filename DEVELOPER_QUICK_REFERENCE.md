# Price Source Tracking - Developer Quick Reference

## 🎯 One-Page Summary

### What Was Added
A `priceSource` field to all holdings showing which API provided the price.

### Where to Find It
- **Database**: `holdings.price_source` column
- **Model**: `Holding.PriceSource` property
- **API**: `HoldingDto.PriceSource` field
- **Response**: `"priceSource": "Yahoo"` in JSON

---

## 📍 Code Locations

### Models
```
Models/Holding.cs
  Line 15: [MaxLength(50)] public string? PriceSource
```

### DTOs
```
Dtos/HoldingDto.cs
  Line 19: public string? PriceSource
```

### Services
```
Services/IPriceService.cs
  Line 6:  Task<PriceResult> GetPriceWithSourceAsync(...)
  Line 12: public class PriceResult

Services/PriceService.cs
  Line 160-203: GetPriceWithSourceAsync() implementation

Services/HoldingsService.cs
  Line 91-93: Use GetPriceWithSourceAsync()
  Line 98:    Include PriceSource in DTO
```

### Migrations
```
Migrations/20260402120239_AddPriceSourceToHoldings.cs
  Line 12: ALTER TABLE holdings ADD COLUMN price_source

Migrations/AppDbContextModelSnapshot.cs
  Line 57-60: PriceSource property configuration
```

---

## 🔄 Data Flow

```
GET /api/holdings
    ↓
HoldingsService.GetHoldingsAsync()
    ↓
For each holding:
    ├─ PriceService.GetPriceWithSourceAsync(ticker)
    │   ├─ Try Eodhd → return "Eodhd"
    │   ├─ Try Yahoo → return "Yahoo"
    │   ├─ Try Cache → return "Cache"
    │   └─ Fail → return null
    ├─ holding.PriceSource = result.Source
    ├─ Build HoldingDto with priceSource
    └─ SaveChangesAsync()
    ↓
Return JSON with priceSource field
```

---

## 📝 API Response

### Before
```json
{
  "ticker": "VWRL.XETRA",
  "currentPrice": 98.45,
  "totalValue": 9845.00
}
```

### After
```json
{
  "ticker": "VWRL.XETRA",
  "currentPrice": 98.45,
  "priceSource": "Yahoo",
  "totalValue": 9845.00
}
```

---

## 🛠️ Common Tasks

### Check If Column Exists
```sql
SELECT price_source FROM holdings LIMIT 1;
```

### Update Price Source Manually
```sql
UPDATE holdings 
SET price_source = 'Cache' 
WHERE ticker = 'VWRL.XETRA';
```

### See All Price Sources
```sql
SELECT ticker, price_source 
FROM holdings 
GROUP BY price_source 
ORDER BY COUNT(*) DESC;
```

### Clear Null Sources
```sql
UPDATE holdings 
SET price_source = 'Cache' 
WHERE price_source IS NULL;
```

---

## 🐛 Debugging

### Enable Debug Logging
```csharp
// In PriceService
_logger.LogInformation($"Getting price for {ticker} with source: {source}");
```

### Test Source Detection
```csharp
var result = await _priceService.GetPriceWithSourceAsync("VWRL.XETRA");
Console.WriteLine($"Price: {result.Price}, Source: {result.Source}");
```

### Check Database State
```sql
SELECT ticker, current_price, price_source 
FROM holdings 
WHERE user_id = 1;
```

---

## 🚀 Deployment Essentials

### Pre-Deployment
```bash
# Build project
dotnet build

# Check for errors
# Should show: Build succeeded.
```

### Deployment
```bash
# Apply migration
dotnet ef database update

# Or manually run:
# ALTER TABLE holdings ADD COLUMN price_source VARCHAR(50);
```

### Post-Deployment
```bash
# Test endpoint
curl http://localhost:5000/api/holdings

# Should include priceSource field
```

---

## 💾 Database Schema

```sql
-- New column
ALTER TABLE holdings 
ADD COLUMN price_source VARCHAR(50) NULL;

-- Column details
- Name: price_source
- Type: VARCHAR(50)
- Nullable: Yes
- Default: NULL
- Constraint: MaxLength(50)
```

---

## ✅ Configuration

### No Configuration Needed
- Automatic activation on first API call
- Database migration handles schema
- Service integration is automatic

### Optional Customization
```csharp
// Change source priority in GetPriceWithSourceAsync():
// 1. Eodhd (current primary)
// 2. Yahoo (current fallback 1)
// 3. Cache (current fallback 2)
```

---

## 📊 Possible Values

```
| Value   | Meaning                    |
|---------|----------------------------|
| Eodhd   | Eodhd API provided price  |
| Yahoo   | Yahoo Finance provided    |
| Cache   | Database cache provided   |
| null    | No price available        |
```

---

## 🔗 Related Files

**Core Files**
- Holding.cs - Model with PriceSource
- PriceService.cs - Source tracking logic
- HoldingsService.cs - API integration

**Supporting Files**
- HoldingDto.cs - API response DTO
- IPriceService.cs - Interface definition
- Migration files - Database schema

**Documentation**
- PRICE_SOURCE_FRONTEND_GUIDE.md - Display guidance
- PRICE_SOURCE_ARCHITECTURE.md - System design
- DEPLOYMENT_CHECKLIST.md - Deployment guide

---

## 📞 Support Matrix

| Issue | Location | Solution |
|-------|----------|----------|
| priceSource is null | HoldingDto | Refresh prices first |
| Column doesn't exist | Database | Run migration |
| Build fails | Code | Check imports |
| API doesn't return field | PriceService | Verify GetPriceWithSourceAsync |

---

## 🎓 Key Methods

### GetPriceWithSourceAsync()
```csharp
// Returns price + source
// Tries: Eodhd → Yahoo → Cache
public async Task<PriceResult> GetPriceWithSourceAsync(string ticker)
```

### PriceResult
```csharp
public class PriceResult
{
    public decimal? Price { get; set; }
    public string? Source { get; set; }
}
```

---

## ⚡ Performance

- **No new queries**: Uses existing price fetch logic
- **Response size**: +10-20 bytes per holding
- **Database impact**: Minimal (single column update)
- **API latency**: Unchanged

---

## 🔐 Security

- ✅ No sensitive data exposed
- ✅ Input validation via MaxLength
- ✅ Safe SQL migration
- ✅ No new attack vectors
- ✅ Follows existing security patterns

---

## 📈 Monitoring

### Log for Success
```
INFO: Successfully got price from Eodhd for VWRL.XETRA
INFO: Successfully got price from Yahoo for VWRL.XETRA
WARNING: Both APIs failed, using cached price
```

### Log for Issues
```
ERROR: Failed to get price for VWRL.XETRA from any source
```

---

## 🎯 Testing Checklist

- [ ] Build succeeds
- [ ] No compilation errors
- [ ] API returns priceSource field
- [ ] All sources appear in database
- [ ] Prices update correctly
- [ ] Performance unchanged

---

## 📚 Learn More

- `PRICE_SOURCE_ARCHITECTURE.md` - Deep dive
- `PRICE_SOURCE_FRONTEND_GUIDE.md` - Frontend info
- Code comments - Inline documentation
- Tests - See test cases (if available)

---

**Quick Access**: Save this file for quick reference during development!

