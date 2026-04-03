# Buy History Modal Enhancement

## Overview
Updated the buy history popup to display current price and variation data for each purchase, allowing users to see how each individual buy is performing.

## Changes Made

### Backend (C# API)

#### 1. **TransactionDto.cs**
- Added `decimal CurrentPrice { get; set; }` - Current price of the ETF at time of viewing
- Added `decimal VariationEur { get; set; }` - Gain/loss in EUR per share
- Added `decimal VariationPercent { get; set; }` - Gain/loss percentage per share

#### 2. **HoldingsService.cs - GetHoldingHistoryAsync()**
- Updated to fetch the holding and its ticker symbol
- Gets current price using the PriceService
- Calculates variation in EUR: `(currentPrice - purchasePrice) * quantity`
- Calculates variation percentage: `((currentPrice - purchasePrice) / purchasePrice) * 100`
- Returns complete transaction data with calculated variations

### Frontend (Angular)

#### 1. **api.service.ts - TransactionDto Interface**
- Added `currentPrice: number`
- Added `variationEur: number`
- Added `variationPercent: number`

#### 2. **buy-history-modal.component.ts**
- Added `formatPercent()` method to format percentage values with +/- sign and 2 decimals

#### 3. **buy-history-modal.component.html**
- Expanded table headers from 4 to 7 columns:
  - Date
  - Quantity
  - Purchase Price
  - Total Invested
  - **Current Price** (NEW)
  - **Variation** (NEW - EUR amount)
  - **Variation %** (NEW - percentage)
- Added `[ngClass]` binding to highlight rows:
  - Green for positive variations
  - Red for negative variations

#### 4. **buy-history-modal.component.scss**
- Added row styling for positive/negative variations:
  - **Positive rows**: Green left border (3px) and green text
  - **Negative rows**: Red left border (3px) and red text
  - Font weight set to 500 for better visibility

## Data Display Example

For a transaction with:
- Purchase Price: €100
- Quantity: 10 shares
- Current Price: €105

Display shows:
- Total Invested: €1,000
- Current Price: €105
- Variation: €50 (10 shares × €5)
- Variation %: +5.00%

## Visual Indicators

### Positive Performance (Green)
- Left border: 3px solid green (#28a745)
- Text color: Green
- Example: €+50 (+5.00%)

### Negative Performance (Red)
- Left border: 3px solid red (#dc3545)
- Text color: Red
- Example: €-30 (-3.00%)

## Testing

1. Open the dashboard and click "History" on any holding
2. View the buy history with:
   - Current market price for all transactions
   - Individual gain/loss in EUR for each purchase
   - Individual gain/loss percentage for each purchase
3. Green highlights indicate profitable purchases
4. Red highlights indicate underwater purchases

