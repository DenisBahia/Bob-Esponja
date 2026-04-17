# Tax Functionality Testing Checklist

Use this checklist to verify the tax functionality is working correctly in your ETF Tracker application.

## Pre-Testing Setup

- [ ] Database migrations applied (check for `20260417154011_AddTaxEvents`)
- [ ] Backend API running
- [ ] Frontend application running
- [ ] User logged in with test account
- [ ] Irish Investor toggle is ON (🍀 Irish indicator should be green)

## Test 1: Deemed Disposal - New Transaction

### Steps:
1. [ ] Navigate to dashboard
2. [ ] Click "+ Add Buy"
3. [ ] Enter transaction details:
   - Ticker: Any valid ticker (e.g., "VWRL.L")
   - Quantity: 10
   - Purchase Price: 100
   - **Purchase Date: Set to 8+ years ago** (e.g., "2018-01-01")
   - [ ] Ensure "Irish Investor" toggle is ON
   - Tax Rate: 41 (default for Irish Exit Tax)
4. [ ] Click "Add Transaction"
5. [ ] Wait for confirmation

### Expected Results:
- [ ] Transaction added successfully
- [ ] In Holdings table, the new holding appears
- [ ] **"Taxes" column shows a value** (should have pulsing dot indicator)
- [ ] Click the "Taxes" button for this holding
- [ ] Tax History Modal opens
- [ ] Modal shows:
  - [ ] "Tax Pending" card with amount > 0
  - [ ] Event table with "8-yr Deemed Disposal" event
  - [ ] Event date matches the 8-year anniversary
  - [ ] Status shows "Pending"

## Test 2: Tax Summary Section

### Steps:
1. [ ] Scroll down to bottom of portfolio section (below Portfolio Allocation)
2. [ ] Locate "🧾 Tax Summary" section

### Expected Results:
- [ ] Tax Summary section is visible
- [ ] Overview cards display:
  - [ ] "Total Pending" with amount > 0
  - [ ] "Pending 2026" (or current year)
  - [ ] "Total Paid" (initially 0)
  - [ ] "Next Deemed Disposal" with date (if applicable)
- [ ] Per-holding breakdown table shows:
  - [ ] Your holding ticker
  - [ ] Event row with type, date, gain, rate, tax amount
  - [ ] Status badge showing "Pending"
  - [ ] "Mark Paid" button available

## Test 3: Mark Single Tax Event as Paid

### Steps:
1. [ ] In Tax Summary section, locate an event with "Pending" status
2. [ ] Click "Mark Paid" button for that event

### Expected Results:
- [ ] Status changes from "Pending" to "Paid"
- [ ] Date appears next to status
- [ ] "Total Paid" card updates
- [ ] "Total Pending" card decreases
- [ ] Pulsing dot indicator updates/disappears if no more pending

## Test 4: Mark All Year as Paid

### Steps:
1. [ ] Ensure you have at least one pending tax event
2. [ ] Click "Mark 2026 as Paid" button (or current year)

### Expected Results:
- [ ] All events from that year change to "Paid"
- [ ] "Total Pending" updates to 0 (or amount from other years)
- [ ] "Total Paid" increases
- [ ] Date appears for all marked events

## Test 5: Per-Holding Tax History

### Steps:
1. [ ] Go back to Holdings table
2. [ ] Click "Taxes" button for any holding with tax events

### Expected Results:
- [ ] Tax History Modal opens
- [ ] Shows ticker in title
- [ ] Summary cards at top:
  - [ ] Tax Pending
  - [ ] Tax Paid
  - [ ] Next Deemed Disposal (if applicable)
- [ ] Events table shows all events for this holding
- [ ] Each event displays:
  - [ ] Date
  - [ ] Type (badge: "8-yr Deemed Disposal" or "Sell")
  - [ ] Quantity
  - [ ] Cost Basis per unit
  - [ ] Price per unit at event
  - [ ] Taxable Gain (green if positive, red if negative)
  - [ ] Tax Rate
  - [ ] Tax Due
  - [ ] Status (badge: "Pending" or "Paid")
  - [ ] Action ("Mark Paid" button or paid date)
- [ ] Can mark individual events as paid from this modal

## Test 6: Sell Transaction Creates Tax Event

### Steps:
1. [ ] In Holdings table, locate a holding with "availableQuantity > 0"
2. [ ] Click "Sell" button
3. [ ] Enter sell details:
   - Quantity: Less than available (e.g., 2 if you have 10)
   - Sell Price: Any value (e.g., 120)
   - Sell Date: Today's date
   - Ensure Irish Investor is ON
   - Tax Rate: 41
4. [ ] Click "Preview"
5. [ ] Review preview (should show CGT Due)
6. [ ] Click "Confirm Sell"

### Expected Results:
- [ ] Sell completes successfully
- [ ] Tax Summary section updates
- [ ] A new "Sell" type tax event appears
- [ ] Event shows:
  - [ ] Taxable Gain (sell price - cost basis)
  - [ ] Tax Amount (gain × 41%)
  - [ ] Status: "Pending"
- [ ] Holding's "Taxes" button shows increased amount
- [ ] "Total Pending" in overview cards increases

## Test 7: Multiple Deemed Disposal Cycles

### Steps:
1. [ ] Add a very old transaction (16+ years ago)
   - Example: Purchase Date: "2010-01-01"
2. [ ] Wait for transaction to complete

### Expected Results:
- [ ] Multiple deemed disposal events created (8-year and 16-year)
- [ ] Each event has correct:
  - [ ] Anniversary date (2018-01-01 and 2026-01-01)
  - [ ] Cost basis (first uses purchase price, second uses first deemed disposal price)
  - [ ] Quantity (decreases if partial sells occurred between events)

## Test 8: Read-Only Shared Profile

### Steps:
1. [ ] Share your profile with another user (read-only)
2. [ ] Log in as that user
3. [ ] View the shared profile

### Expected Results:
- [ ] Tax Summary section is visible
- [ ] Can view all tax events
- [ ] "Mark Paid" buttons are hidden (read-only)
- [ ] "Mark 2026 as Paid" button is hidden
- [ ] Tax History Modal can be opened but no edit actions available

## Test 9: Empty States

### Steps:
1. [ ] Create a new test user with no transactions
2. [ ] Navigate to dashboard

### Expected Results:
- [ ] Tax Summary section shows:
  - [ ] "No tax events recorded yet..." message
  - [ ] No event tables displayed
- [ ] Opening Tax History Modal for any holding shows:
  - [ ] "No tax events recorded for this holding yet." message

## Test 10: Non-Irish Investor Mode

### Steps:
1. [ ] Toggle OFF the Irish Investor switch (🍀 Irish)
2. [ ] Add a buy transaction (8+ years old)

### Expected Results:
- [ ] Transaction adds successfully
- [ ] **NO deemed disposal events are created**
- [ ] Tax Summary shows no events
- [ ] "Taxes" column shows 0
- [ ] If you now sell, a tax event IS created (non-Irish CGT rate)

## Troubleshooting

### Tax Summary Section Not Visible
- **Check**: Dashboard HTML line ~335, should be `*ngIf="activeMainSection === 'portfolio'"`
- **Fix**: Change from 'holdings' to 'portfolio' if needed

### Deemed Disposal Events Not Created
- **Check**: Irish Investor toggle is ON when adding transaction
- **Check**: Purchase date is at least 8 years before today
- **Check**: Backend logs for warnings about missing historical prices
- **Fix**: Ensure price data exists for the ticker on the anniversary date

### Tax Events Not Showing in UI
- **Check**: Browser console for API errors
- **Check**: Network tab shows successful GET /api/tax-events response
- **Check**: taxSummary is not null in component state
- **Fix**: Verify API endpoint returns data, check authentication

### Compilation Errors
- **Backend**: Run `dotnet build` in ETFTracker.Api directory
- **Frontend**: Run `npm run build` in ETFTracker.Web directory
- **Check**: All migration files applied to database

## Success Criteria

All tests should pass with:
- ✅ Deemed disposal events created automatically
- ✅ Sell events created on transaction
- ✅ Tax Summary section displays correctly
- ✅ Mark as paid functionality works
- ✅ Per-holding tax history accessible
- ✅ Read-only mode respected
- ✅ Empty states display appropriately
- ✅ Irish/Non-Irish investor modes work correctly

---

**After completing these tests**, the tax functionality is verified as working correctly and ready for production use.

