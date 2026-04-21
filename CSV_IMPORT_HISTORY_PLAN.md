# Feature Plan: History Import by CSV / Excel

**Status:** Planned  
**Created:** 2026-04-21  
**Relates to:** `add-transaction-modal`, `HoldingsController`, `HoldingsService`

---

## 1. Overview

Allow users to bulk-import their full buy/sell transaction history by uploading a CSV file
(Excel-compatible). This removes the need to add each transaction manually via the
`add-transaction-modal`.

Import is **all-or-nothing (atomic)**. If any row has a validation error or cannot be resolved,
the entire import is rejected and nothing is persisted.

---

## 2. CSV Columns (Required only)

| Column           | Type    | Notes                                     |
|------------------|---------|-------------------------------------------|
| `operation`      | string  | `BUY` or `SELL` (case-insensitive)        |
| `date`           | date    | `YYYY-MM-DD` or `DD/MM/YYYY`              |
| `ticker_or_isin` | string  | Ticker (e.g. `VWRL`) or ISIN              |
| `quantity`       | decimal | Must be > 0                               |
| `price`          | decimal | Price per unit, must be > 0               |

No optional columns. The `isIrishInvestor` and `taxRate` values are taken from the user's
existing settings (same as the manual add transaction modal).

---

## 3. Broker Format Presets

### How it works

Different brokers export CSVs with different column names, date formats, and operation labels.
Rather than forcing users to reformat their export, the user **manually selects their broker**
from a dropdown before uploading.

**Why manual selection, not auto-detection?**
Auto-detection is fragile — brokers change their export formats, files can be trimmed or edited,
and a wrong silent guess is worse than a clear choice. Manual selection is one extra click but
gives the user full confidence and control.

### UX Flow with Presets

```
[Select Broker (optional)] → [Upload File] → [Parse with preset mapping] → [Validation] → ...
```

A "Native format" option is always available (uses the standard columns from Section 2).

### Supported Broker Presets (Phase 1)

| Broker                   | Operation column  | Date format          | Ticker column         | Qty column        | Price column        |
|--------------------------|-------------------|----------------------|-----------------------|-------------------|---------------------|
| **Native**               | `operation`       | `YYYY-MM-DD`         | `ticker_or_isin`      | `quantity`        | `price`             |
| **Trading 212**          | `Action`          | `DD/MM/YYYY HH:mm`   | `Ticker`              | `No. of shares`   | `Price / share`     |
| **DEGIRO**               | `Tipo de ordem`   | `DD-MM-YYYY`         | `Produto` / `ISIN`    | `Quantidade`      | `Preço`             |
| **Revolut**              | `Type`            | `YYYY-MM-DD`         | `Ticker`              | `Quantity`        | `Price per share`   |
| **Interactive Brokers**  | `Buy/Sell`        | `YYYY-MM-DD`         | `Symbol`              | `Quantity`        | `T. Price`          |

> Each preset also maps operation values, e.g. Trading 212 uses `"Market buy"` / `"Market sell"`.

### Preset Definition (Frontend)

```typescript
export interface BrokerPreset {
  id: string;
  label: string;
  operationColumn: string;
  buyValue: string;       // e.g. "Market buy", "BUY", "Buy"
  sellValue: string;
  dateColumn: string;
  dateFormat: string;     // e.g. 'DD/MM/YYYY', 'YYYY-MM-DD'
  tickerColumn: string;
  quantityColumn: string;
  priceColumn: string;
}

export const BROKER_PRESETS: BrokerPreset[] = [
  {
    id: 'native',
    label: 'Native (this app)',
    operationColumn: 'operation',
    buyValue: 'BUY',
    sellValue: 'SELL',
    dateColumn: 'date',
    dateFormat: 'YYYY-MM-DD',
    tickerColumn: 'ticker_or_isin',
    quantityColumn: 'quantity',
    priceColumn: 'price',
  },
  {
    id: 'trading212',
    label: 'Trading 212',
    operationColumn: 'Action',
    buyValue: 'Market buy',
    sellValue: 'Market sell',
    dateColumn: 'Time',
    dateFormat: 'DD/MM/YYYY HH:mm',
    tickerColumn: 'Ticker',
    quantityColumn: 'No. of shares',
    priceColumn: 'Price / share',
  },
  // ... DEGIRO, Revolut, Interactive Brokers
];
```

A **`CsvParserService`** reads the file, applies the selected preset's column mapping, and
normalises all rows into the internal `ParsedImportRow` shape before validation runs.
All downstream logic (validation, ISIN resolution, summary table) is always the same
regardless of broker.

---

## 4. Import Flow (UX)

```
[Select Broker] → [Upload CSV] → [Parse & Validate] → [ISIN Resolution] → [Summary Table] → [Confirm]
```

### Step 1 — Select Broker & Upload
- New button **"Import History (CSV)"** on the dashboard, next to "Add Transaction".
- Opens `ImportHistoryModalComponent`.
- Dropdown to select broker preset (defaults to "Native").
- File input (`.csv`) + drag-and-drop zone.
- **"Download Template"** button — generates the correct template for the selected preset
  (Native format if Native is selected). 100% client-side, no backend call.

### Step 2 — Parse & Validate (Frontend, all-or-nothing)

Validation rules:
- All required columns present (checked against the selected preset's column names).
- `operation` maps to BUY or SELL via the preset's `buyValue`/`sellValue`.
- `date` is valid and parseable in the preset's expected format; not in the future.
- `quantity` > 0 and is a valid number.
- `price` > 0 and is a valid number.
- `ticker_or_isin` is not empty.

**If any row fails validation:**
- Show an error table listing each invalid row, the failing column, and the reason.
- The import cannot proceed until the user fixes and re-uploads the file.
- Nothing is sent to the backend.

### Step 3 — ISIN Resolution
For rows where `ticker_or_isin` is an ISIN (12-char alphanumeric, starts with 2 letters):
- Call existing `GET /api/holdings/search?q={isin}`.
- **1 result** → auto-resolve to that ticker.
- **Multiple results** → row flagged; user picks from inline dropdown in the summary table.
- **0 results** → row flagged as unresolvable error; **blocks confirmation** until deleted or fixed.

All ISIN resolutions must be completed before confirmation is enabled.

### Step 4 — Summary Table
Preview table of all parsed rows:

| # | Operation | Date       | Ticker            | Quantity | Price  | Status          |
|---|-----------|------------|-------------------|----------|--------|-----------------|
| 1 | BUY       | 2024-01-15 | VWRL              | 10       | 98.50  | ✅ Ready         |
| 2 | BUY       | 2024-03-10 | *(pick ticker ▼)* | 5        | 45.00  | ⚠️ Pick ticker  |
| 3 | SELL      | 2025-06-01 | AAPL              | 2        | 210.00 | ✅ Ready         |

- Inline `<select>` for unresolved ISINs, pre-populated from search results.
- User can **delete individual rows**.
- Counter: `X rows ready · Y require action`.
- **Confirm Import** disabled until all rows are ✅ Ready.

### Step 5 — Confirm & Import (Atomic)
- User clicks **Confirm Import**.
- Frontend calls `POST /api/holdings/import` with all resolved rows.
- Backend processes rows **sorted by date ascending** (FIFO correctness).
- If **any row fails** on the backend → the entire import is rolled back; no changes persisted.
- On success: modal closes, dashboard refreshes, success toast shown with row count.
- On failure: error message shown with the specific row(s) that caused the failure.

---

## 5. Backend

### 5.1 New DTOs

```csharp
// ETFTracker.Api/Dtos/ImportTransactionDto.cs

public class ImportTransactionRowDto
{
    public string Operation { get; set; } = string.Empty;  // "BUY" | "SELL"
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateOnly Date { get; set; }
}

public class ImportTransactionsRequestDto
{
    public List<ImportTransactionRowDto> Rows { get; set; } = new();
}

public class ImportTransactionsResultDto
{
    public int Imported { get; set; }
}
```

No partial-failure DTO needed — it's all or nothing.

### 5.2 New Endpoint

```
POST /api/holdings/import
Body: ImportTransactionsRequestDto
Response 200: ImportTransactionsResultDto
Response 400: { rowIndex, reason }  ← first failing row
Response 500: generic error
```

Added to `HoldingsController`.

### 5.3 Service Logic

```csharp
// Pseudocode for HoldingsService.ImportTransactionsAsync
public async Task<int> ImportTransactionsAsync(int userId, ImportTransactionsRequestDto dto, CancellationToken ct)
{
    var rows = dto.Rows.OrderBy(r => r.Date).ToList();  // FIFO order

    await using var transaction = await _db.Database.BeginTransactionAsync(ct);
    try
    {
        foreach (var (row, index) in rows.Select((r, i) => (r, i)))
        {
            if (row.Operation == "BUY")
                await AddTransactionAsync(userId, MapToCreateDto(row, userSettings), ct);
            else
                await _sellService.ConfirmSellAsync(..., ct);
        }
        await transaction.CommitAsync(ct);
        return rows.Count;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        throw;  // controller returns 400/500 with reason
    }
}
```

- Uses a **database transaction** — any failure rolls back everything.
- `isIrishInvestor` and `taxRate` are fetched from the user's settings, same as the manual flow.
- SELL rows use `ISellService.ConfirmSellAsync` for correct FIFO CGT calculation.

---

## 6. Frontend Components

### New Files
```
ETFTracker.Web/src/app/components/import-history-modal/
  import-history-modal.component.ts
  import-history-modal.component.html
  import-history-modal.component.scss

ETFTracker.Web/src/app/services/csv-parser.service.ts   ← broker preset mapping + parsing
```

### New ApiService Method
```typescript
importTransactions(rows: ImportTransactionRowDto[]): Observable<ImportTransactionsResultDto>
```

### Template Download (client-side, preset-aware)
```typescript
downloadTemplate(preset: BrokerPreset): void {
  const header = [
    preset.operationColumn,
    preset.dateColumn,
    preset.tickerColumn,
    preset.quantityColumn,
    preset.priceColumn,
  ].join(',');

  const examples = [
    [preset.buyValue,  '2024-01-15', 'VWRL',         '10', '98.50' ].join(','),
    [preset.buyValue,  '2024-03-10', 'IE00B3RBWM25', '5',  '45.00' ].join(','),
    [preset.sellValue, '2025-06-01', 'AAPL',          '2', '210.00'].join(','),
  ].join('\n');

  const csv = `${header}\n${examples}`;
  const blob = new Blob([csv], { type: 'text/csv' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `import_template_${preset.id}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}
```

---

## 7. Additional Options (Post-MVP)

| Option | Description |
|--------|-------------|
| **Duplicate detection** | Before confirming, warn if a row matches an existing transaction (same ticker, date, quantity). User confirms or cancels the entire import. |
| **Export current history** | "Export to CSV" button exports existing transactions in the native format — useful for backups and migration. |
| **Undo import** | After success, show an "Undo Import" button (session-scoped) that deletes all rows from that batch via a `batchImportId`. |
| **More broker presets** | Extend `BROKER_PRESETS` list as users request them. |
| **Dry-run API mode** | `POST /api/holdings/import?dryRun=true` — validates and reports without persisting. Useful for testing large files. |

---

## 8. Implementation Order

1. **Backend:** `ImportTransactionRowDto` + `POST /api/holdings/import` with DB transaction.
2. **Backend:** Fetch user settings (Irish investor flag, tax rate) inside import service.
3. **Frontend:** `BROKER_PRESETS` constants + `CsvParserService`.
4. **Frontend:** `downloadTemplate()` (preset-aware, client-side).
5. **Frontend:** `ImportHistoryModalComponent` — broker selector + upload step.
6. **Frontend:** Parse → validate → ISIN resolution pipeline.
7. **Frontend:** Summary table with inline ticker picker + delete row.
8. **Frontend:** Confirm step + progress indicator + result/error toast.
9. **Extras:** Duplicate detection, export, undo.

---

## 9. Files to Create / Modify

| File | Action |
|------|--------|
| `ETFTracker.Api/Dtos/ImportTransactionDto.cs` | **Create** |
| `ETFTracker.Api/Controllers/HoldingsController.cs` | **Modify** — add `POST /import` |
| `ETFTracker.Api/Services/IHoldingsService.cs` | **Modify** — add `ImportTransactionsAsync` |
| `ETFTracker.Api/Services/HoldingsService.cs` | **Modify** — implement atomic import |
| `ETFTracker.Web/src/app/services/api.service.ts` | **Modify** — add `importTransactions()` |
| `ETFTracker.Web/src/app/services/csv-parser.service.ts` | **Create** — broker presets + parser |
| `ETFTracker.Web/src/app/components/import-history-modal/*` | **Create** |
| Dashboard component HTML | **Modify** — add "Import History (CSV)" button |

