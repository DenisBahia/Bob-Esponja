# Tax Redesign Plan

## Overview of Current State

| Area | Current Behaviour |
|---|---|
| `Transaction` model | No asset type or deemed disposal flag |
| `SellRecord` / `TaxEvent` | Tax amount stored but no distinction loss/profit per CGT/Exit Tax; no yearly consolidation |
| `ProjectionSettings` | Has `IsIrishInvestor`, `CgtPercent`, `ExitTaxPercent`, `DeemedDisposalPercent`, `SiaAnnualPercent`, `TaxFreeAllowancePerYear` |
| `DeemedDisposalService` | Always applies deemed disposal to ALL Irish investor buys regardless of asset type |
| `SellService` | Applies deemed disposal cost adjustment to all Irish investor lots regardless of buy flag |
| `TaxEventsController` | Returns flat event list; allowance logic is broken (deducts from `TaxAmount` not from gain) |
| Projections (`ProjectionService`) | Applies deemed disposal to all buy lots; treats `ExitTaxPercent` as final-year sell tax; no per-asset-type distinction |
| Frontend | "deemed disposals and sells" text shown to all users; no deemed disposal toggle per buy; no yearly recalculation button |

---

## Phase 1 — Database Changes

### 1.1 New table: `asset_type_deemed_disposal_defaults`
Stores the user's preferred deemed-disposal flag per asset class so the buy modal can default it automatically.

```sql
CREATE TABLE asset_type_deemed_disposal_defaults (
  id                  SERIAL PRIMARY KEY,
  user_id             INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  asset_type          VARCHAR(50) NOT NULL,  -- e.g. 'ETF', 'EQUITY', 'MUTUAL_FUND', 'CRYPTO', etc.
  deemed_disposal_due BOOLEAN NOT NULL DEFAULT false,
  updated_at          TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(user_id, asset_type)
);
```

### 1.2 Alter `transactions` — add `deemed_disposal_due`
```sql
ALTER TABLE transactions ADD COLUMN deemed_disposal_due BOOLEAN NOT NULL DEFAULT false;
```
This is the **buy-level flag** that governs whether deemed disposal and Exit Tax (vs CGT) rules apply to each lot.

### 1.3 Alter `sell_records` — add `tax_type` column + rename `cgt_paid`
```sql
ALTER TABLE sell_records RENAME COLUMN cgt_paid TO tax_amount_saved;
ALTER TABLE sell_records ADD COLUMN tax_type VARCHAR(20) NOT NULL DEFAULT 'CGT'; -- 'CGT' | 'ExitTax'
```

### 1.4 Alter `tax_events` — add `tax_sub_type`
```sql
ALTER TABLE tax_events ADD COLUMN tax_sub_type VARCHAR(20);
-- values: 'CGT' | 'ExitTax' | 'DeemedDisposal'
```

### 1.5 Drop `sell_records.is_irish_investor`
Investor type is deterministic from `projection_settings` at query time. Drop since data is being wiped.
```sql
ALTER TABLE sell_records DROP COLUMN is_irish_investor;
```

### 1.6 New table: `annual_tax_summary`
Stores the result of the year-end recalculation. One row per `(user_id, tax_year, tax_type, holding_id)`.

- For CGT rows: `holding_id = NULL` (all assets combined into one CGT pot per year).
- For Exit Tax rows: `holding_id` = specific holding (pot is per asset per year).

```sql
CREATE TABLE annual_tax_summary (
  id                      SERIAL PRIMARY KEY,
  user_id                 INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  tax_year                INT NOT NULL,
  tax_type                VARCHAR(20) NOT NULL,   -- 'CGT' | 'ExitTax'
  holding_id              INT REFERENCES holdings(id) ON DELETE CASCADE,
  total_profits           DECIMAL(12,2) NOT NULL DEFAULT 0,
  total_losses            DECIMAL(12,2) NOT NULL DEFAULT 0,
  net_gain                DECIMAL(12,2) NOT NULL DEFAULT 0,
  allowance_applied       DECIMAL(12,2) NOT NULL DEFAULT 0,  -- CGT only
  deemed_disposal_credit  DECIMAL(12,2) NOT NULL DEFAULT 0,  -- ExitTax only
  taxable_gain            DECIMAL(12,2) NOT NULL DEFAULT 0,
  tax_due                 DECIMAL(12,2) NOT NULL DEFAULT 0,
  tax_rate_used           DECIMAL(5,2)  NOT NULL DEFAULT 0,
  status                  VARCHAR(20)   NOT NULL DEFAULT 'Pending',
  recalculated_at         TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(user_id, tax_year, tax_type, holding_id)
);
```

**Loss rules:**
- Losses are **never** carried forward — neither for CGT nor for Exit Tax.
- For CGT: losses offset profits within the **same year** only.
- For Exit Tax: losses offset profits within the **same asset + same year** pot only. Cross-asset offset is not allowed.

### 1.7 `projection_versions` — add missing columns
`ProjectionVersion` is missing `IsIrishInvestor`, `TaxFreeAllowancePerYear`, `DeemedDisposalPercent` columns (they exist in `ProjectionSettings`). Add them so saved versions reload correctly.

```sql
ALTER TABLE projection_versions
  ADD COLUMN is_irish_investor          BOOLEAN       NOT NULL DEFAULT false,
  ADD COLUMN tax_free_allowance_per_year DECIMAL(15,2) NOT NULL DEFAULT 0,
  ADD COLUMN deemed_disposal_percent    DECIMAL(5,2)  NOT NULL DEFAULT 41;
```

---

## Phase 2 — Backend Models & DTOs

### 2.1 New model: `AssetTypeDeemedDisposalDefault`
```csharp
public class AssetTypeDeemedDisposalDefault
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AssetType { get; set; } = string.Empty; // "ETF", "EQUITY", etc.
    public bool DeemedDisposalDue { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
}
```

### 2.2 Update `Transaction` model
```csharp
/// <summary>
/// True = this buy is subject to Deemed Disposal and Exit Tax rules (Irish investors).
/// False = CGT rules apply.
/// Set by user at buy time, defaulted from AssetTypeDeemedDisposalDefault.
/// </summary>
public bool DeemedDisposalDue { get; set; } = false;
```

### 2.3 Update `SellRecord` model
- Remove `IsIrishInvestor`
- Rename `CgtPaid` → `TaxAmountSaved`
- Add `TaxType` (`"CGT"` | `"ExitTax"`)

### 2.4 Update `TaxEvent` model
```csharp
/// <summary>Sub-type for display and calculation routing.</summary>
public string? TaxSubType { get; set; } // "CGT" | "ExitTax" | "DeemedDisposal"
```

### 2.5 New model: `AnnualTaxSummary`
```csharp
public class AnnualTaxSummary
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TaxYear { get; set; }
    public string TaxType { get; set; } = string.Empty;    // "CGT" | "ExitTax"
    public int? HoldingId { get; set; }                    // null for CGT, set for ExitTax
    public decimal TotalProfits { get; set; }
    public decimal TotalLosses { get; set; }
    public decimal NetGain { get; set; }
    public decimal AllowanceApplied { get; set; }
    public decimal DeemedDisposalCredit { get; set; }
    public decimal TaxableGain { get; set; }
    public decimal TaxDue { get; set; }
    public decimal TaxRateUsed { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RecalculatedAt { get; set; }
    public User? User { get; set; }
    public Holding? Holding { get; set; }
}
```

### 2.6 New DTO: `AssetTypeDeemedDisposalDefaultDto`
```csharp
public class AssetTypeDeemedDisposalDefaultDto
{
    public string AssetType { get; set; } = string.Empty;
    public bool DeemedDisposalDue { get; set; }
}
```

### 2.7 Updated `TaxSummaryDto`
```csharp
public class TaxSummaryDto
{
    // Existing fields kept:
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public List<TaxEventDto> Events { get; set; } = new();

    // New / changed:
    public bool IsIrishInvestor { get; set; }
    public DateOnly? NextDeemedDisposalDate { get; set; }  // null when !IsIrishInvestor

    // Annual CGT consolidation (non-Irish + Irish CGT lots)
    public List<TaxYearSummaryDto> CgtByYear { get; set; } = new();

    // Exit Tax pots — per asset per year (Irish only)
    public List<ExitTaxPotDto> ExitTaxPots { get; set; } = new();

    // Remove: AnnualTaxFreeAllowance, AllowanceByYear, TotalPendingAfterAllowance
    // (these are now computed inside TaxYearSummaryDto)
}

public class TaxYearSummaryDto
{
    public int Year { get; set; }
    public decimal TotalProfits { get; set; }
    public decimal TotalLosses { get; set; }       // negative number
    public decimal NetGain { get; set; }
    public decimal TaxFreeAllowance { get; set; }
    public decimal TaxableGain { get; set; }       // max(0, NetGain - Allowance)
    public decimal TaxDue { get; set; }
    public string Status { get; set; } = "Pending";
}

public class ExitTaxPotDto
{
    public int HoldingId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalProfits { get; set; }
    public decimal TotalLosses { get; set; }       // intra-asset intra-year losses only
    public decimal DeemedDisposalCreditUsed { get; set; }
    public decimal NetTaxableGain { get; set; }    // max(0, profits + losses - credit)
    public decimal TaxDue { get; set; }
    public string Status { get; set; } = "Pending";
}
```

### 2.8 Updated `SellDto.cs`
```csharp
public class SellPreviewDto
{
    public decimal AvailableQuantity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CgtDue { get; set; }
    public decimal TaxRateUsed { get; set; }
    public string TaxType { get; set; } = string.Empty;  // "CGT" | "ExitTax"
    public bool HasLosses { get; set; }
    public List<SellLotBreakdownDto> Lots { get; set; } = new();
}

public class SellRecordDto
{
    // existing fields...
    public string TaxType { get; set; } = string.Empty;  // "CGT" | "ExitTax"
    // Remove: IsIrishInvestor
}
```

### 2.9 Updated `HoldingDto.cs`
```csharp
public decimal TotalTaxPaid { get; set; }
public decimal TotalTaxPending { get; set; }
public decimal TotalExitTaxPending { get; set; }   // Irish: Exit Tax sell events
public decimal TotalCgtPending { get; set; }        // Irish: CGT sell events / Non-Irish: all CGT
public decimal AvailableQuantity { get; set; }
public DateOnly? NextDeemedDisposalDate { get; set; } // null when !IsIrishInvestor or no deemed disposal buys
```

### 2.10 Updated `ProjectionSettingsDto`
```csharp
/// <summary>When false, deemed disposal loop is skipped in projection. Controlled by toggle in UI.</summary>
public bool DeemedDisposalEnabled { get; set; } = true;
```

---

## Phase 3 — Backend Service Changes

### 3.1 New service: `IAssetTypeDeemedDisposalDefaultService`
```
GET  /api/asset-type-defaults          → List<AssetTypeDeemedDisposalDefaultDto>
POST /api/asset-type-defaults          → upsert for a given assetType
```
Used by the buy modal to pre-fill the deemed disposal toggle and to persist user preference after each buy.

### 3.2 Update `SellService`

**A. Remove `isIrishInvestor` parameter** from public methods. Load `IsIrishInvestor` from `ProjectionSettings` inside the service.

**B. Per-lot deemed disposal check** — in `ComputeLotsAsync`, check `txn.DeemedDisposalDue` (not `isIrishInvestor`) to decide whether to call `ResolveLotTaxBasisAsync`. Non-deemed-disposal lots always use original cost regardless of investor type.

**C. Determine `TaxType` for the sell:**
- If consumed lots have `DeemedDisposalDue = true` → `TaxType = "ExitTax"`
- If consumed lots have `DeemedDisposalDue = false` → `TaxType = "CGT"`
- Mixed lot sell (FIFO crosses a boundary): use the flag of the majority consumed quantity.

**D. Fix loss storage** — remove `Math.Max(0, totalProfit)`:
```csharp
// Save totalProfit as-is (can be negative for losses).
// For CGT sells: taxAmountSaved = 0 at sell time. Tax computed at year-end via recalculate button.
// For Exit Tax sells: compute immediately using pot logic (see RecalculateTaxYearAsync).
```

**E. TaxEvent creation per sell:**
- CGT sell: `TaxSubType = "CGT"`, `TaxAmount = 0` (provisional, updated by recalc).
- Exit Tax sell: `TaxSubType = "ExitTax"`, `TaxAmount = computed exit tax`.

### 3.3 Update `DeemedDisposalService`

Replace the blanket `if (!isIrishInvestor) return;` check. Instead filter transactions by the buy-level flag:

```csharp
// Only process transactions where deemed_disposal_due = true
var transactions = await _db.Transactions
    .Where(t => t.HoldingId == holdingId
             && t.PurchaseDate <= threshold
             && t.DeemedDisposalDue)       // ← new filter
    .OrderBy(t => t.PurchaseDate)
    .ToListAsync(ct);
```

The yearly loop, cost basis tracking, and credit logic remain correct as-is.

**Deemed disposal credit calculation (clarification):**

The accumulating credit pattern works from original buy price (not rolling cost basis):
```
Buy price = €1, qty = 100

Year 8:  Market price = €2
         Total profit = (2 - 1) * 100 = €100
         Deemed disposal due = €100 * 38% = €38
         Credit accumulated = €38

Year 16: Market price = €2.50
         Total profit from original buy = (2.50 - 1) * 100 = €150
         Deemed disposal due = (€150 * 38%) - €38 = €57 - €38 = €19
         Credit accumulated = €57

Year 24: and so on...
```
This is already correctly implemented in `DeemedDisposalService.GetCostBasisForAnniversary` — no change needed beyond the `DeemedDisposalDue` filter.

### 3.4 New service method: `RecalculateTaxYearAsync(int userId, int year)`
Exposed via `POST /api/tax-events/recalculate-year?year={year}`.

**CGT calculation (non-Irish + Irish non-deemed-disposal sells):**
1. Load all `SellRecord`s for `userId` where `sell_date.Year = year` and `TaxType = "CGT"`.
2. `totalProfits = sum(TotalProfit where > 0)`.
3. `totalLosses = sum(TotalProfit where < 0)`.
4. `netGain = totalProfits + totalLosses`.
5. Load `TaxFreeAllowancePerYear` from `ProjectionSettings` (0 for Irish investors).
6. `taxableGain = max(0, netGain - allowance)`.
7. `taxDue = taxableGain > 0 ? taxableGain * CgtPercent / 100 : 0`.
8. Upsert `annual_tax_summary` row: `tax_type = 'CGT'`, `holding_id = NULL`.

**Exit Tax calculation (Irish, deemed-disposal sells) — per asset per year:**

For each distinct `(holdingId)` that has `SellRecord`s with `TaxType = "ExitTax"` and `sell_date.Year = year`:
1. `totalProfits = sum(TotalProfit where > 0)` for this holding + year.
2. `totalLosses = sum(TotalProfit where < 0)` for this holding + year.
   - Intra-asset intra-year loss offset is **allowed**.
   - Cross-asset offset is **not allowed** (each pot is independent).
3. `netGain = totalProfits + totalLosses`.
4. Load all `DeemedDisposal` `TaxEvent`s for this `holdingId` where `EventDate` is before end of `year` → sum `TaxAmount` as `deemedDisposalCredit`.
5. `taxableGain = max(0, netGain - deemedDisposalCredit)`.
6. `taxDue = taxableGain * ExitTaxPercent / 100`.
7. Upsert `annual_tax_summary` row: `tax_type = 'ExitTax'`, `holding_id = holdingId`.

**Loss rules enforced in all calculations:**
- Losses are **never** carried forward to another year.
- If `netGain < 0` for CGT: `taxDue = 0`. Display net loss as informational only.
- If `netGain < 0` for an Exit Tax pot: `taxDue = 0`. Display net loss as informational only.

**Response DTO:**
```csharp
public class RecalculateTaxYearResultDto
{
    public int Year { get; set; }
    public decimal CgtTaxDue { get; set; }
    public List<ExitTaxPotDto> ExitTaxPots { get; set; } = new();
    public decimal TotalTaxDue { get; set; }
}
```

### 3.5 Update `TaxEventsController`

- `GET /api/tax-events` → return enhanced `TaxSummaryDto` with `IsIrishInvestor`, `CgtByYear`, `ExitTaxPots`.
- `POST /api/tax-events/recalculate-year?year={year}` → calls `RecalculateTaxYearAsync`, returns `RecalculateTaxYearResultDto`.
- Remove broken allowance deduction logic (currently deducts from `TaxAmount` directly instead of computing from gain).
- `NextDeemedDisposalDate` → only computed and returned when `IsIrishInvestor = true`.

### 3.6 Update `ProjectionService`

**A.** Check `txn.DeemedDisposalDue` per buy lot (not `isIrishInvestor`) to decide whether to apply the deemed disposal tax loop.

**B.** Add `DeemedDisposalEnabled` toggle to settings: when `false`, skip the entire deemed disposal loop.

**C.** For the final-year sell tax: use `ExitTaxPercent` for lots where `DeemedDisposalDue = true`, and `CgtPercent` for lots where `DeemedDisposalDue = false`.

**D.** Rename internal variable `cgtByYear` → `deemedDisposalByYear` for clarity.

**E.** SIA calculation: no logic changes, labels only (see §4.1).

### 3.7 Update `HoldingsService`

Populate the new split tax fields in `HoldingDto`:
- `TotalExitTaxPending`: sum of `TaxAmount` from `TaxEvent`s with `TaxSubType = "ExitTax"` and `Status = "Pending"` for this holding.
- `TotalCgtPending`: sum of `TaxDue` from `AnnualTaxSummary` rows with `TaxType = "CGT"` and `Status = "Pending"` that include sells from this holding. (Note: CGT is a combined pot so apportion by holding contribution, or simply show the holding's raw profit contribution.)
- `NextDeemedDisposalDate`: only compute for holdings that have at least one buy with `DeemedDisposalDue = true`.

---

## Phase 4 — Frontend Changes

### 4.1 User Settings Modal (`user-settings-modal.component.html`)

**Irish investor section:**
- `"Exit Tax %"` → `"CGT / Exit Tax %"` + tooltip: _"Exit Tax applies to ETFs and funds subject to deemed disposal. For other assets it is called CGT and works identically."_
- `"Deemed Disposal %"` hint → add: _"Only applied to buys you flag as deemed disposal due (e.g. ETFs and funds). Can be set per buy."_
- `"SIA Annual %"` → `"SIA Annual % (probably from 2027 on)"`
- Add a visual separator above SIA: `<hr>` with label **"SIA — Planned for 2027"**

**Non-Irish investor section:** no changes needed.

### 4.2 Buy Modal (`add-transaction-modal.component.html`) — Irish investors only

Add deemed disposal toggle below the purchase date field, **only visible when `isIrishInvestor`**:

```html
<div class="form-group toggle-row" *ngIf="isIrishInvestor">
  <label>Deemed Disposal Due</label>
  <div class="toggle-hint">
    Mark if this asset is subject to the 8-year deemed disposal rule (e.g. ETFs, funds).
    Your preference is remembered per asset type.
  </div>
  <div class="toggle-switch" [class.active]="deemedDisposalDue"
       (click)="deemedDisposalDue = !deemedDisposalDue">
    <span class="toggle-track"><span class="toggle-thumb"></span></span>
  </div>
</div>
```

**On ticker select:** call `GET /api/asset-type-defaults` and match `selectedResult.quoteType` to pre-fill `deemedDisposalDue`.

**On submit:** call `POST /api/asset-type-defaults` to persist the preference for this asset type so the next buy of the same type defaults correctly.

### 4.3 Sell Modal (`sell-modal.component.html`)

- **Remove** `isIrishInvestor` as an input parameter — derive from user settings loaded by the parent.
- Show `"Deemed Disposal"` column in the lot table **only when the specific lot has `deemedDisposalDue = true`** (not for all Irish investors).
- For **CGT sells** (lots without deemed disposal flag):
  - Show `TotalProfit` (profit or loss) labelled as "Profit / Loss".
  - Show `Tax Due = €0` with note: _"Calculated at year-end via Recalculate button."_
  - On confirm success → show **step 3 confirmation screen**:
    - If loss: _"Loss of €X recorded for [year]. It will be offset against your CGT profits within [year]."_
    - If profit: _"Profit of €X recorded. Run the year-end recalculation in the Tax Summary to update your CGT estimate."_
- For **Exit Tax sells** (lots with deemed disposal flag):
  - Show deemed disposal credit deduction in the summary.
  - Show final `Tax Due` as computed immediately.
  - On confirm success → step 3: _"Exit Tax of €X recorded for [ticker] ([year] pot)."_
- Rename `taxRateLabel` → `"CGT / Exit Tax %"` for Irish investors.

### 4.4 Tax Summary Section — Dashboard (`dashboard.component.html`)

**Subtitle text (conditional):**
```html
<p class="tax-summary-sub" *ngIf="isIrishInvestor">
  All taxable events — deemed disposals and sells — across your portfolio
</p>
<p class="tax-summary-sub" *ngIf="!isIrishInvestor">
  All taxable sell events across your portfolio
</p>
```

**"No tax events" message (conditional):**
```html
<ng-container *ngIf="isIrishInvestor">
  Events appear automatically when sells are made or 8-year deemed disposal rules are triggered.
</ng-container>
<ng-container *ngIf="!isIrishInvestor">
  Events appear automatically when sells are made.
</ng-container>
```

**Recalculate button** — add next to "PENDING (current year)" heading:
```html
<div class="pending-header">
  <span>PENDING {{ currentYear }}</span>
  <button class="btn-recalc" (click)="recalculateTaxYear(currentYear)"
          [disabled]="recalculating" title="Recalculate tax due for {{ currentYear }}">
    {{ recalculating ? '…' : '🔄 Recalculate' }}
  </button>
</div>
```

**Post-recalculate banner** (dismissable, shown when `taxDue > 0`):
```html
<div class="tax-recalc-banner" *ngIf="taxBannerMessage" (click)="taxBannerMessage = null">
  💡 {{ taxBannerMessage }} <span class="dismiss">✕</span>
</div>
```
```typescript
// In recalculateTaxYear():
if (result.totalTaxDue > 0) {
  this.taxBannerMessage = `Estimated tax due for ${year}: ${this.formatCurrency(result.totalTaxDue)}.`;
}
```

**Tax Summary table redesign:**

For **CGT** (non-Irish and Irish CGT lots) — year-grouped view:

| Year | Profits | Losses | Net Gain | Allowance | Taxable Gain | Tax Due | Status |
|---|---|---|---|---|---|---|---|
| 2025 | €500 | -€80 | €420 | €3,000 | €0 | €0 | — |
| 2026 | €1,200 | -€200 | €1,000 | €3,000 | €0 | €0 | — |

- When `netGain < 0`: display _"Net loss of €X — no CGT due."_ Losses do not carry forward.

For **Exit Tax** (Irish only) — per-asset per-year pots, with tooltip `ⓘ`:
> _"Exit Tax losses within this asset and year can offset gains from the same asset in the same year. They cannot reduce Exit Tax on other assets."_

| Asset | Year | Profits | Losses | DD Credit | Taxable Gain | Tax Due | Status |
|---|---|---|---|---|---|---|---|
| VWRL | 2025 | €800 | -€50 | €120 | €630 | €258 | Pending |

For **Deemed Disposal** (Irish only) — keep existing event-by-event display (no changes to this section).

### 4.5 Holdings Table — Tax Column

```html
<!-- Irish investors: split ET / CGT -->
<td *ngIf="isIrishInvestor">
  <span *ngIf="h.totalExitTaxPending > 0" class="tax-badge et"
        title="Exit Tax pending">ET: {{ formatCurrency(h.totalExitTaxPending) }}</span>
  <span *ngIf="h.totalCgtPending > 0" class="tax-badge cgt"
        title="CGT pending">CGT: {{ formatCurrency(h.totalCgtPending) }}</span>
  <span *ngIf="h.totalTaxPaid > 0" class="tax-paid-badge">
    Paid: {{ formatCurrency(h.totalTaxPaid) }}</span>
  <span *ngIf="h.totalTaxPending === 0 && h.totalTaxPaid === 0">—</span>
</td>

<!-- Non-Irish investors: single CGT column -->
<td *ngIf="!isIrishInvestor">
  <span *ngIf="h.totalTaxPending > 0">{{ formatCurrency(h.totalTaxPending) }}</span>
  <span *ngIf="h.totalTaxPending === 0">—</span>
</td>
```

`NextDeemedDisposalDate` column: only render for Irish investors AND only for holdings that have at least one buy with `DeemedDisposalDue = true`.

### 4.6 Projections Tab (`dashboard.component.html`)

**Replace "Deemed Disposal %" input with a toggle + conditional input** (Irish investors only):
```html
<div class="param-group" *ngIf="isIrishInvestor">
  <label>Apply Deemed Disposal in Projection</label>
  <div class="toggle-hint">Only enable if you hold ETFs or funds subject to the 8-year rule.</div>
  <div class="toggle-switch"
       [class.active]="projectionSettings.deemedDisposalEnabled"
       (click)="projectionSettings.deemedDisposalEnabled = !projectionSettings.deemedDisposalEnabled">
    <span class="toggle-track"><span class="toggle-thumb"></span></span>
  </div>
</div>
<div class="param-group" *ngIf="isIrishInvestor && projectionSettings.deemedDisposalEnabled">
  <label>Deemed Disposal %</label>
  <input type="number" step="0.1" min="0" max="100"
         [(ngModel)]="projectionSettings.deemedDisposalPercent" />
</div>
```

**Rename "Exit Tax %" → "CGT / Exit Tax %"** everywhere in the projections panel and in the saved versions table column header.

**SIA section** — add `"(probably from 2027 on)"` to the label and hint text everywhere SIA appears in projections.

---

## Phase 5 — Migration Steps (in order)

1. Run SQL migrations (§1.1 – §1.7).
2. Add `AssetTypeDeemedDisposalDefault` and `AnnualTaxSummary` models + EF Core config in `AppDbContext`.
3. Update `Transaction`, `SellRecord`, `TaxEvent` models.
4. Update `ProjectionVersion` model with missing tax columns.
5. Create `IAssetTypeDeemedDisposalDefaultService` + `GET/POST /api/asset-type-defaults` controller.
6. Create `RecalculateTaxYearAsync` method + `POST /api/tax-events/recalculate-year` endpoint.
7. Refactor `SellService`: remove `isIrishInvestor` param, add per-lot `DeemedDisposalDue` check, fix loss storage, add `TaxType`.
8. Update `DeemedDisposalService`: filter by `txn.DeemedDisposalDue = true`.
9. Update `TaxEventsController`: new `TaxSummaryDto` fields, remove broken allowance logic, gate `NextDeemedDisposalDate` on `IsIrishInvestor`.
10. Update `ProjectionService`: add `DeemedDisposalEnabled` toggle, per-lot check, rename labels.
11. Update `HoldingsService`: compute `TotalExitTaxPending` / `TotalCgtPending` splits, gate `NextDeemedDisposalDate`.
12. Frontend: all changes in §4.1 – §4.6.

---

## Additional Features (confirmed)

### Sell Modal — Step 3 Confirmation Screen
After confirming a sell, show a dedicated confirmation step before closing:
- **CGT loss**: _"Loss of €X recorded for [year]. It will be offset against your CGT profits within [year]."_
- **CGT profit**: _"Profit of €X recorded. Run the year-end recalculation in the Tax Summary to update your CGT estimate."_
- **Exit Tax**: _"Exit Tax of €X recorded for [ticker] ([year] pot)."_

### Post-Recalculate Banner
After the recalculate button runs: if `totalTaxDue > 0`, show a dismissable banner in the Tax Summary section:
> _"Estimated tax due for [year]: €X."_

### Exit Tax Pot Tooltip
Next to each Exit Tax pot heading in the Tax Summary, show an `ⓘ` icon with tooltip:
> _"Exit Tax losses within this asset and year can offset gains from the same asset in the same year. They cannot reduce Exit Tax on other assets."_

---

## Loss Rules Summary

| Scenario | Losses offset? | Carry forward? |
|---|---|---|
| CGT — same year, across all assets | ✅ Yes | ❌ No |
| CGT — different years | ❌ No | ❌ No |
| Exit Tax — same asset, same year | ✅ Yes | ❌ No |
| Exit Tax — different assets, same year | ❌ No | ❌ No |
| Exit Tax — same asset, different years | ❌ No | ❌ No |
| Deemed Disposal | N/A (only profits trigger tax) | N/A |

