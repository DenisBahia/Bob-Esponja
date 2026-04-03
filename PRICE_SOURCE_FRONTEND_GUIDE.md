# Price Source Frontend Integration Guide

## Overview
The API now returns a `priceSource` field for each holding, indicating which data source provided the current price.

## Response Field

### New Field: `priceSource`
- **Type**: `string | null`
- **Location**: In each holding object within the `/api/holdings` response
- **Possible Values**:
  - `"Eodhd"` - Price from Eodhd API (premium data)
  - `"Yahoo"` - Price from Yahoo Finance API (fallback)
  - `"Cache"` - Price from cached historical snapshot (emergency fallback)
  - `null` - Price unavailable (check `priceUnavailable` flag)

## Example API Response

```json
{
  "header": {
    "totalHoldingsAmount": 9895.225,
    "totalVariation": { ... },
    "dailyMetrics": { ... }
  },
  "holdings": [
    {
      "id": 1,
      "ticker": "VWRL.XETRA",
      "etfName": "Vanguard FTSE All-World UCITS ETF",
      "quantity": 100.5,
      "averageCost": 98.20,
      "currentPrice": 98.45,
      "totalValue": 9895.225,
      "priceUnavailable": false,
      "priceSource": "Yahoo",
      "dailyMetrics": { "gainLossEur": 25.50, "gainLossPercent": 0.26 },
      "weeklyMetrics": { ... },
      "monthlyMetrics": { ... },
      "ytdMetrics": { ... }
    }
  ]
}
```

## Frontend Display Options

### 1. Simple Badge/Tag
```html
<span class="price-source-badge" [ngClass]="'source-' + holding.priceSource">
  {{ holding.priceSource || 'N/A' }}
</span>
```

### 2. Tooltip Information
```html
<span [title]="getSourceDescription(holding.priceSource)">
  💰 {{ holding.currentPrice }}
</span>
```

### 3. Color Coding
```css
/* CSS for visual indicators */
.source-Eodhd {
  background-color: #4CAF50; /* Green - Premium source */
  color: white;
}

.source-Yahoo {
  background-color: #2196F3; /* Blue - Primary fallback */
  color: white;
}

.source-Cache {
  background-color: #FF9800; /* Orange - Cached data */
  color: white;
}

[priceSource="null"],
[priceUnavailable="true"] {
  background-color: #f44336; /* Red - Unavailable */
  color: white;
}
```

### 4. Source Descriptions (for UI)
```typescript
const sourceDescriptions = {
  'Eodhd': 'Real-time price from Eodhd API (Premium)',
  'Yahoo': 'Real-time price from Yahoo Finance',
  'Cache': 'Cached price from last update',
  null: 'Price currently unavailable'
};

getSourceDescription(source: string | null): string {
  return sourceDescriptions[source] || 'Unknown source';
}
```

## Angular Component Example

```typescript
import { Component, OnInit } from '@angular/core';
import { HoldingsService } from './services/holdings.service';

@Component({
  selector: 'app-holdings',
  templateUrl: './holdings.component.html',
  styleUrls: ['./holdings.component.scss']
})
export class HoldingsComponent implements OnInit {
  holdings: any[] = [];
  sourceIcons = {
    'Eodhd': '⭐',
    'Yahoo': '📊',
    'Cache': '📦',
    null: '❌'
  };

  constructor(private holdingsService: HoldingsService) {}

  ngOnInit() {
    this.loadHoldings();
  }

  loadHoldings() {
    this.holdingsService.getHoldings().subscribe(data => {
      this.holdings = data.holdings;
    });
  }

  getSourceLabel(source: string | null): string {
    const labels = {
      'Eodhd': 'Premium API',
      'Yahoo': 'Yahoo Finance',
      'Cache': 'Cached',
      null: 'Unavailable'
    };
    return labels[source] || 'Unknown';
  }

  getSourceColor(source: string | null): string {
    const colors = {
      'Eodhd': 'success',
      'Yahoo': 'info',
      'Cache': 'warning',
      null: 'danger'
    };
    return colors[source] || 'secondary';
  }
}
```

## HTML Template Example

```html
<div class="holdings-table">
  <table>
    <thead>
      <tr>
        <th>Ticker</th>
        <th>Name</th>
        <th>Price</th>
        <th>Source</th>
        <th>Total Value</th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let holding of holdings">
        <td>{{ holding.ticker }}</td>
        <td>{{ holding.etfName }}</td>
        <td class="price-cell" 
            [class.unavailable]="holding.priceUnavailable">
          {{ holding.currentPrice | currency }}
        </td>
        <td>
          <span class="badge" 
                [ngClass]="'badge-' + getSourceColor(holding.priceSource)"
                [title]="getSourceDescription(holding.priceSource)">
            {{ sourceIcons[holding.priceSource] }} {{ getSourceLabel(holding.priceSource) }}
          </span>
        </td>
        <td>{{ holding.totalValue | currency }}</td>
      </tr>
    </tbody>
  </table>
</div>
```

## Data Quality Indicators

### User Guidance
- **Green (Eodhd)**: Most reliable - real-time premium data
- **Blue (Yahoo)**: Reliable - real-time public data
- **Orange (Cache)**: May be stale - using last known good value
- **Red (Null)**: No data available - check network/API status

### Suggested Messages
```typescript
const sourceMessages = {
  'Eodhd': 'Live price from premium data provider',
  'Yahoo': 'Live price from Yahoo Finance',
  'Cache': 'Latest known price (may be outdated)',
  null: 'Price not available right now'
};
```

## Error Handling

```typescript
// In component
handlePriceUnavailable(holding: any) {
  if (holding.priceUnavailable) {
    return `Price unavailable - Last known: ${holding.priceSource || 'never fetched'}`;
  }
  return `Last updated via ${holding.priceSource || 'unknown source'}`;
}
```

## Performance Considerations

- Price source information is cached in the database
- No additional API calls needed for source data
- Response size increases negligibly (string field)
- Source updates only when prices are refreshed

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| All sources show "Cache" | Both APIs failing | Check API credentials/network |
| priceSource is null | First time loading | Wait for first price refresh |
| priceSource never updates | Caching issue | Clear cache or refresh holdings |
| Wrong source displayed | Database sync issue | Refresh price data |

---

**Note**: The `priceSource` field is automatically populated and updated by the backend. Frontend only needs to display it.

