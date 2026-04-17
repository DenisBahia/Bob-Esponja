# ETF Tracker: Improved Tax Functionality Plan

## 1. Deemed Disposal for Irish Investors

- **Trigger:** When a buy is added, check if the buy date is 8 years before today.
- **Action:** If so, calculate deemed disposal according to Irish tax rules.
- **Note:** Users may add past buys when the app goes live, so this must work retroactively.

## 2. Tax Log (All Customers)

- **Log all taxes paid and due** in a new DB table.
- **Populated at two moments:**
  - When deemed disposal is due (for Irish investors).
  - When a sell happens (for all, as per current rules).
- **Each event** adds to the user's tax due.

## 3. Marking Taxes as Paid

- **User can flag taxes as paid** (after real-world payment).
- **UI:** Remove "Tax Paid" column in holdings table.
- **Add "Taxes" column:** Opens a modal with tax history for that asset, showing all events and current tax due.

## 4. Tax History Modal

- **Shows all tax events** for the asset (deemed disposals, sells).
- **Shows status:** Paid or pending, with date paid if applicable.
- **Allows marking individual events as paid.**

## 5. Consolidated Tax Section

- **New section after Portfolio Allocation** on dashboard.
- **Shows all tax events** (all assets, all years).
- **Summary cards:** Total pending, total paid, pending for current year, next deemed disposal date.
- **Per-asset breakdown:** Grouped by holding, with all events and actions.

## 6. Database

- **New table:** `TaxEvents`
  - Fields: id, userId, holdingId, eventType (Sell/DeemedDisposal), eventDate, quantity, costBasis, priceAtEvent, taxableGain, taxAmount, taxRate, status (Pending/Paid), paidAt, createdAt, etc.
- **Relations:** Linked to holdings, buy/sell records.

## 7. API

- **Endpoints:**
  - `GET /tax-events?holdingId=...` (per asset or all)
  - `PUT /tax-events/{id}/mark-paid`
  - `PUT /tax-events/mark-all-paid?year=...`
- **DTOs:** For tax event, summary, mark-paid.

## 8. Frontend

- **AddTransactionModal:** Pass `isIrishInvestor` and `taxRate` as inputs.
- **Holdings Table:** Replace "Tax Paid" with "Taxes" button.
- **TaxHistoryModal:** New component for per-asset tax history.
- **Tax Summary Section:** New dashboard section with summary and per-asset breakdown.
- **Styles:** Add SCSS for new UI elements.

## 9. Other Notes

- **Partial sells:** All logic must handle partial sells correctly.
- **Deemed disposal:** After each deemed disposal, only gains since last event are taxed.
- **Marking paid:** User can mark all events for a year as paid, or individually.
- **Migration:** Old tax paid columns removed, new table used for all tax tracking.

---

**Suggestions:**
- Consider adding notifications for upcoming deemed disposals.
- Allow export of tax log for user records.
- Add tooltips/help for Irish tax rules in UI.

