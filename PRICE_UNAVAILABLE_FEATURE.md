# Price Unavailable Feature

## Overview
Updated the application to load the dashboard even when price data cannot be retrieved for one or more tickers. Tickers with unavailable prices are visually marked with a red line in the holdings table.

## Changes Made

### Backend (C# API)

#### 1. **IPriceService.cs**
- Changed `GetPriceAsync()` return type from `Task<decimal>` to `Task<decimal?>`
- Now returns `null` instead of throwing an exception when price cannot be fetched

#### 2. **PriceService.cs**
- Updated `GetPriceAsync()` to return `null` instead of throwing an exception
- The method now gracefully handles API failures and returns `null` when:
  - Eodhd API fails
  - Yahoo Finance API fails
  - No cached price snapshot is available

#### 3. **HoldingDto.cs**
- Added new property: `bool PriceUnavailable { get; set; }`
- This flag is set to `true` when a price cannot be retrieved for a ticker

#### 4. **HoldingsService.cs**
- Updated `GetHoldingsAsync()` to handle null prices gracefully
- Sets `PriceUnavailable = true` when price is null
- Uses 0 as display price when price is unavailable
- Dashboard loads successfully even if some holdings have missing prices

### Frontend (Angular)

#### 1. **dashboard.component.html**
- Added `[ngClass]="{'price-unavailable': holding.priceUnavailable}"` to holding rows
- Rows with unavailable prices now have the `price-unavailable` CSS class

#### 2. **dashboard.component.scss**
- Added styling for `.price-unavailable` class:
  - **3px red left border** on the row
  - **Light red background** (rgba(220, 53, 69, 0.05)) for subtle highlighting
  - Rows remain interactive and hoverable

## Behavior

### Before
- Dashboard would fail to load if any price fetch operation failed
- User would see an error message and have to retry

### After
- Dashboard loads successfully with all holdings
- Rows with unavailable prices are marked with:
  - **Red left border** (3px solid #dc3545)
  - **Light red background**
- Current Price displays as 0 for unavailable tickers
- Total Value and metrics show 0 for unavailable tickers
- User can still view all holdings and perform other operations

## Visual Indicator
Tickers with unavailable prices are easily identifiable by:
1. **Red left border** on the table row
2. **Subtle red background** tint
3. **Current Price** and **Total Value** columns show 0

## Testing
To test this feature:
1. Stop the price service APIs (disconnect internet or disable API calls)
2. Load the dashboard - it should load successfully
3. Any tickers without cached prices should be marked with the red line

