# Plan: Sell Asset Functionality with FIFO CGT + Persistent Lot Tracking

Add partial sell support to the holdings table. A two-step modal gathers the sell details and shows the CGT/Exit-Tax breakdown before committing. All FIFO lot consumption and deemed-disposal adjustments are persisted in the DB to prevent any duplicate taxation on future sells. The existing history modal gains a "Sells" tab alongside the existing "Buys" tab.

---

## Steps

### 1. Add `SellRecord` + `SellLotAllocation` models and EF migration

Register in `AppDbContext.cs`:

**`sell_records` table**
| Column | Type | Notes |
|---|---|---|
| `id` | int PK | |
| `holding_id` | int FK → holdings | |
| `sell_date` | date | |
| `sell_price` | decimal(12,4) | price per unit at sell |
| `quantity` | decimal(12,4) | total units sold |
| `total_profit` | decimal(12,2) | FIFO-computed profit |
| `cgt_paid` | decimal(12,2) | tax paid on this sell |
| `tax_rate_used` | decimal(5,2) | % used (38, 0, custom) |
| `is_irish_investor` | bool | snapshot of investor type |
| `created_at` | timestamptz | |

**`sell_lot_allocations` table**
| Column | Type | Notes |
|---|---|---|
| `id` | int PK | |
| `sell_record_id` | int FK → sell_records | |
| `buy_transaction_id` | int FK → transactions | |
| `quantity_consumed` | decimal(12,4) | how much of the buy lot was used |
| `original_cost_per_unit` | decimal(12,4) | original purchase price of the buy |
| `adjusted_cost_per_unit` | decimal(12,4) | stepped-up cost basis (= original if no deemed disposal) |
| `deemed_disposal_date` | date nullable | last 8-yr anniversary used, if Irish |
| `deemed_disposal_price_per_unit` | decimal(12,4) nullable | price at that anniversary |
| `profit_on_lot` | decimal(12,2) | profit attributed to this lot |
| `created_at` | timestamptz | |

Relationships:
- `SellRecord` → one `Holding` (FK, cascade delete)
- `SellRecord` → many `SellLotAllocation`
- `SellLotAllocation` → one buy `Transaction` (FK, restrict delete — cannot delete a buy that has been (partially) sold)

---

### 2. Create `SellService.cs`

Interface `ISellService`:
```csharp
Task<SellPreviewDto> PreviewSellAsync(int holdingId, int userId, decimal qty, decimal sellPrice,
    DateOnly sellDate, bool isIrishInvestor, decimal taxRate, CancellationToken ct = default);

Task<SellRecordDto> ConfirmSellAsync(int holdingId, int userId, decimal qty, decimal sellPrice,
    DateOnly sellDate, bool isIrishInvestor, decimal taxRate, CancellationToken ct = default);

Task<List<SellRecordDto>> GetSellHistoryAsync(int holdingId, int userId, CancellationToken ct = default);
```

**FIFO engine** (used by both Preview and Confirm):
1. Load all buy `Transaction` rows for the holding, ordered by `PurchaseDate ASC`.
2. For each buy lot, compute **remaining qty** = `transaction.Quantity − SUM(sla.quantity_consumed)` from all existing `SellLotAllocation` rows referencing that transaction.
3. Skip fully consumed lots (remaining qty = 0).
4. Walk remaining lots in order, consuming `qty` across them until satisfied. If requested qty > total available qty, throw a validation exception (400).
5. Per consumed lot portion:
   - **Irish investor**: find the latest 8-year anniversary of `PurchaseDate` that is **before** `sellDate`. Look up the nearest `PriceSnapshot` on or before that anniversary date. If found: `adjustedCostPerUnit = snapshotPrice`, store `deemed_disposal_date` and `deemed_disposal_price_per_unit`. If no snapshot found, fall back to `originalPurchasePrice`. If no anniversary has occurred before `sellDate`: `adjustedCostPerUnit = originalPurchasePrice`.
   - **Non-Irish investor**: `adjustedCostPerUnit = originalPurchasePrice` (no deemed disposal logic).
   - `profitOnLot = (sellPrice − adjustedCostPerUnit) × quantityConsumed`
6. `totalProfit = SUM(profitOnLot)` across all lots.
7. `cgtDue = MAX(0, totalProfit) × taxRate / 100` — never negative.

**`ConfirmSellAsync`** wraps in a DB transaction:
- Saves `SellRecord`.
- Saves all `SellLotAllocation` rows.
- Decrements `Holding.Quantity` by sold qty (does **not** delete the holding when it reaches 0 — keep it for history).
- Updates `Holding.AverageCost` using only the remaining unconsumed buy lots (FIFO remainder after subtracting consumed quantities and computing weighted average).
- Does **not** touch `ProjectionSettings`, goals, or projections.

---

### 3. Add sell endpoints to `HoldingsController.cs`

```
POST /api/holdings/{holdingId}/sell/preview
  Body: { quantity, sellPrice, sellDate, isIrishInvestor, taxRate }
  Returns: SellPreviewDto

POST /api/holdings/{holdingId}/sell/confirm
  Body: { quantity, sellPrice, sellDate, isIrishInvestor, taxRate }
  Returns: SellRecordDto

GET  /api/holdings/{holdingId}/sell-history
  Returns: SellRecordDto[]
```

All three endpoints:
- Require `[Authorize]`.
- Return 403 if `IsReadOnly`.
- Return 400 with `{ message }` on validation failure (e.g., qty > available).

---

### 4. New DTOs in `TransactionDto.cs` (or a new `SellDto.cs`)

```csharp
// Request
public class SellRequestDto {
    public decimal Quantity { get; set; }
    public decimal SellPrice { get; set; }
    public DateOnly SellDate { get; set; }
    public bool IsIrishInvestor { get; set; }
    public decimal TaxRate { get; set; }
}

// Per-lot breakdown row (preview + history)
public class SellLotBreakdownDto {
    public int BuyTransactionId { get; set; }
    public DateOnly BuyDate { get; set; }
    public decimal QuantityConsumed { get; set; }
    public decimal OriginalCostPerUnit { get; set; }
    public decimal AdjustedCostPerUnit { get; set; }
    public DateOnly? DeemedDisposalDate { get; set; }
    public decimal? DeemedDisposalPricePerUnit { get; set; }
    public decimal ProfitOnLot { get; set; }
}

// Preview result (not yet saved)
public class SellPreviewDto {
    public decimal AvailableQuantity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CgtDue { get; set; }
    public decimal TaxRateUsed { get; set; }
    public List<SellLotBreakdownDto> Lots { get; set; } = new();
}

// Saved sell record
public class SellRecordDto {
    public int Id { get; set; }
    public int HoldingId { get; set; }
    public DateOnly SellDate { get; set; }
    public decimal SellPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CgtPaid { get; set; }
    public decimal TaxRateUsed { get; set; }
    public bool IsIrishInvestor { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SellLotBreakdownDto> Lots { get; set; } = new();
}
```

---

### 5. Update `HoldingDto` + `GetHoldingsAsync`

In `HoldingDto.cs` add:
```csharp
public decimal TotalTaxPaid { get; set; }       // SUM(cgt_paid) from sell_records
public decimal AvailableQuantity { get; set; }  // Quantity minus all consumed lot qty
```

In `HoldingsService.GetHoldingsAsync`:
- Load all `SellLotAllocation` rows grouped by `buy_transaction_id` (consumed qty per transaction).
- Load all `SellRecord` rows grouped by `holding_id` (total tax paid per holding).
- Compute `AvailableQuantity = SUM(buy transactions remaining)`.
- Populate `TotalTaxPaid` and `AvailableQuantity` on each `HoldingDto`.

---

### 6. Extend `api.service.ts`

Add interfaces:
```typescript
export interface SellRequestDto {
  quantity: number;
  sellPrice: number;
  sellDate: string;       // "YYYY-MM-DD"
  isIrishInvestor: boolean;
  taxRate: number;
}

export interface SellLotBreakdownDto {
  buyTransactionId: number;
  buyDate: string;
  quantityConsumed: number;
  originalCostPerUnit: number;
  adjustedCostPerUnit: number;
  deemedDisposalDate: string | null;
  deemedDisposalPricePerUnit: number | null;
  profitOnLot: number;
}

export interface SellPreviewDto {
  availableQuantity: number;
  totalProfit: number;
  cgtDue: number;
  taxRateUsed: number;
  lots: SellLotBreakdownDto[];
}

export interface SellRecordDto {
  id: number;
  holdingId: number;
  sellDate: string;
  sellPrice: number;
  quantity: number;
  totalProfit: number;
  cgtPaid: number;
  taxRateUsed: number;
  isIrishInvestor: boolean;
  createdAt: string;
  lots: SellLotBreakdownDto[];
}
```

Add `HoldingDto` fields:
```typescript
totalTaxPaid: number;
availableQuantity: number;
```

Add methods:
```typescript
previewSell(holdingId: number, dto: SellRequestDto): Observable<SellPreviewDto>
confirmSell(holdingId: number, dto: SellRequestDto): Observable<SellRecordDto>
getSellHistory(holdingId: number): Observable<SellRecordDto[]>
```

---

### 7. Create `SellModalComponent`

New standalone Angular component at `src/app/components/sell-modal/`.

**Inputs:**
- `holding: HoldingDto`
- `isIrishInvestor: boolean`
- `projectionSettings: ProjectionSettingsDto | null`

**Outputs:**
- `sold: EventEmitter<void>`
- `cancelled: EventEmitter<void>`

**Step 1 — Enter details:**
- Quantity input (max = `holding.availableQuantity`; validation error if exceeded)
- Sell price input (pre-filled with `holding.currentPrice`)
- Sell date picker (default today)
- Computed proceeds display (`qty × sellPrice`)
- "Calculate Tax" button → calls `previewSell` → switches to Step 2

**Step 2 — Review & confirm:**
- Per-lot breakdown table:
  | Buy Date | Qty Consumed | Original Cost | Adjusted Cost | Deemed Disposal Date | Profit |
- Summary row: Total Profit, Tax Rate (editable `<input type="number">`), CGT Due (auto-updated as user edits rate)
- Note: *"Editing the tax rate will update your default rate in Projections settings"*
- "Confirm Sell" button → calls `confirmSell`; if tax rate was changed also calls `saveProjectionSettings` (updates `exitTaxPercent` for Irish / `cgtPercent` for non-Irish)
- "← Back" button → returns to Step 1
- On success: emit `sold` and close

**Tax rate defaults (from `projectionSettings`, or fallback):**
- Irish investor: `projectionSettings?.exitTaxPercent ?? 38`
- Non-Irish: `projectionSettings?.cgtPercent ?? 0`

---

### 8. Update `BuyHistoryModalComponent` → Transaction History Modal

- Rename `<h2>Buy History</h2>` to `<h2>Transaction History</h2>`.
- Add `Buys` / `Sells` tab toggle (default: Buys tab active).
- Buys tab: existing table unchanged.
- Sells tab:
  - Calls `getSellHistory(holdingId)` on load (lazy, only when tab is first opened).
  - Shows a table with: Sell Date, Quantity, Sell Price, Total Proceeds, Total Profit, CGT Paid, Tax Rate.
  - Each row is expandable (click to toggle) to show the lot-by-lot FIFO breakdown (Buy Date, Qty Consumed, Original Cost/unit, Adjusted Cost/unit, Deemed Disposal Date, Profit on Lot).
  - Green row = profit > 0; red row = profit ≤ 0.
  - No edit/delete on sell records (audit trail — immutable once committed).

---

### 9. Update holdings table in `dashboard.component.html` + `dashboard.component.ts`

**HTML changes:**
- Add sortable `Tax Paid` column header in `<thead>` (after the existing YTD column).
- Add `<td>{{ formatCurrency(holding.totalTaxPaid) }}</td>` in each `<tr>`.
- Add "Sell" `<button>` in the actions `<td>`, visible only when `!sharingCtx.isReadOnly() && holding.availableQuantity > 0`.
- Add `<app-sell-modal>` below `<app-buy-history-modal>`, shown via `showSellModal` flag.

**TS changes:**
- Import `SellModalComponent`.
- Add `showSellModal = false` and `selectedSellHolding: HoldingDto | null = null`.
- Add `onSellClick(holding: HoldingDto)` → sets `selectedSellHolding = holding`, `showSellModal = true`.
- Add `onSellModalClosed()` and `onSellConfirmed()` → reload dashboard + close modal.
- Add `taxPaid` case to `sortHoldingsBy` switch.

---

## Further Considerations

1. **Multiple deemed disposals per lot** — a buy from 2010 has 8-year anniversaries in 2018 and 2026. The engine uses the *last* anniversary before `sellDate`. Each `SellLotAllocation` stores the exact anniversary used, so future partial sells of the same lot can determine whether a newer anniversary has occurred since the last sell was committed.

2. **Tax-rate edit scope** — saving the edited tax % from the sell popup updates `ProjectionSettings.ExitTaxPercent` (Irish) or `CgtPercent` (non-Irish), which also affects the Projections page. A small inline note in the popup ("This will update your default tax rate for Projections too") keeps the user informed.

3. **Zero-quantity holdings remain visible** — the holding is kept at `Quantity = 0` after a full sell. The "Sell" button is hidden when `availableQuantity = 0`. `TotalTaxPaid` is still shown for audit purposes.

4. **Cannot delete a buy that has been (partially) sold** — the FK from `SellLotAllocation.buy_transaction_id → transactions.id` uses `RESTRICT` on delete. The backend should surface a clear error message if the user attempts to delete such a buy via the History modal.

5. **History modal shows remaining quantity per buy lot** — in the Buys tab, each buy row can show `Remaining: X units` (= original qty minus consumed from `SellLotAllocation`) so the user can see what's still available in each lot.

