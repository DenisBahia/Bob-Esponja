# ETF Description Search Feature

## Overview
Added automatic ETF description lookup when adding a new buy transaction. When users enter a ticker symbol, the application searches EODHD and Yahoo Finance APIs to find and display the ETF name/description.

## Changes Made

### Backend (C# API)

#### 1. **IPriceService.cs**
- Added `Task<string?> GetEtfDescriptionAsync(string ticker, ...)` method signature

#### 2. **PriceService.cs**
- Implemented `GetEtfDescriptionAsync()` - tries EODHD first, then falls back to Yahoo Finance
- Implemented `GetEodhDescriptionAsync()` - fetches ETF name from EODHD's `name` field
- Implemented `GetYahooDescriptionAsync()` - fetches ETF name from Yahoo Finance's `longName` or `shortName` fields
  - Also applies XETRA → DE ticker conversion for Yahoo

#### 3. **HoldingsController.cs**
- Added `GetEtfDescription(string ticker)` endpoint
- Route: `GET /api/holdings/etf-description/{ticker}`
- Returns: `{ description: string }`
- Handles errors gracefully, returns "ETF not found" if description can't be retrieved

### Frontend (Angular)

#### 1. **api.service.ts**
- Added `getEtfDescription(ticker: string): Observable<{ description: string }>` method
- Calls backend endpoint to fetch ETF description

#### 2. **add-transaction-modal.component.ts**
- Added `etfDescription` property to store fetched description
- Added `loadingDescription` flag to show loading state
- Implemented `onTickerChange()` event handler
- Implemented `fetchEtfDescription()` method with debouncing (500ms delay)
  - Prevents excessive API calls while user is still typing
  - Clears description when ticker is cleared

#### 3. **add-transaction-modal.component.html**
- Added `(ngModelChange)="onTickerChange($event)"` to ticker input
- Added loading spinner: displays "Loading ETF info..." with animation
- Added description display box: shows ETF name with blue background when available
- Description only shows after user stops typing for 500ms

#### 4. **add-transaction-modal.component.scss**
- Added `.ticker-loading` styling:
  - Spinner animation (rotating circle)
  - Loading text with flex alignment
- Added `.ticker-description` styling:
  - Light blue background (#e7f3ff)
  - Blue left border (3px)
  - Dark blue text (#003d7a)
  - Slide-down animation
- Added keyframe animations:
  - `spin` - 360° rotation for spinner
  - `slideDown` - smooth appearance of description box

## User Experience

### Flow
1. User opens "Add New Buy" modal
2. User enters ticker symbol (e.g., "VWRL")
3. After 500ms of no typing, API is called
4. Loading spinner appears: "Loading ETF info..."
5. Description fetches from EODHD (or Yahoo if EODHD fails)
6. Description displays in blue box below ticker input
7. User can proceed with adding transaction

### Examples
- **VWRL** → "Vanguard FTSE All-World ETF"
- **E500.XETRA** → "iShares EURO STOXX 50 UCITS ETF"
- **Unknown ticker** → No description shown (no error)

## API Fallback Strategy

**For ETF Description:**
1. Try EODHD API (`/api/real-time/{ticker}` → `name` field)
2. If EODHD fails, try Yahoo Finance (`longName` field)
3. If Yahoo `longName` not available, try `shortName`
4. If all fail, show nothing (graceful degradation)

**Note:** Yahoo ticker conversion (XETRA → DE) is applied only for Yahoo API calls.

## Benefits
- ✅ Users see what ETF they're buying before confirming
- ✅ Typo detection - if description doesn't appear, ticker might be wrong
- ✅ No extra clicks needed - information appears automatically
- ✅ Debouncing prevents API spam
- ✅ Graceful fallback if services are unavailable
- ✅ Works offline for previously cached tickers

