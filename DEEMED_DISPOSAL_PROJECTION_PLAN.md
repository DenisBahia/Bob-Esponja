# Deemed Disposal in Projections — Implementation Plan & Reference

> Irish-investor only feature.  
> Toggle: **Apply Deemed Disposal** in projection settings.  
> DD % is read-only in projections — it always comes from **User Settings → Deemed Disposal %**.

---

## 1. Background & Business Rules

| Rule | Detail |
|---|---|
| Who | Irish investors only (`UserSettings.IsIrishInvestor`) |
| Toggle | `applyDeemedDisposal: boolean` stored in `ProjectionSettings` |
| DD % source | `UserSettings.DeemedDisposalPercent` — **not** editable in projection UI |
| Tax label | **"Exit Tax %"** when DD on · **"CGT %"** when DD off |
| DD trigger | Every 8 years after each annual contribution cohort |
| DD base | The profit attributable **to that cohort only** (not whole portfolio) |
| Final-year tax | `(totalProfit − Σ deemedDisposalPaid) × exitTaxPercent` |
| No double taxation | All DD already paid is deducted from the final-year exit tax base |

---

## 2. Deemed Disposal Calculation Model

The projection models **annual contribution cohorts**:

- **Cohort 0** = the current portfolio value (`currentTotal`) invested at year 0  
- **Cohort i** = the annual contributions added at year `i` (for `i = 1 … N`)

Each cohort grows at the same `YearlyReturnPercent` independently.

### 2a. Cohort value at any future year

```
cohortValue(cohort, year) = initialCohortAmount × (1 + r)^(year − cohort)
```

### 2b. DD event detection

A DD event fires for **cohort `c`** at year `y` when:

```
(y - c) % 8 == 0  AND  (y - c) > 0
```

### 2c. DD amount

```
ddProfit  = cohortValue(c, y) − cohortCostBasis(c)
ddTaxDue  = max(0, ddProfit) × deemedDisposalPercent / 100
```

Cost basis is **stepped up** to `cohortValue(c, y)` after each DD event.

### 2d. Exit Tax in the final year N

Irish Revenue rule: exit tax is computed on the **full profit at exit**, then DD taxes already paid are deducted as a **tax credit** — not as a reduction to the profit base.

```
grossTotalProfit = max(0, finalPortfolioValue − totalAmountInvested)
grossExitTax     = round(grossTotalProfit × exitTaxRate / 100, 2)
exitTaxDue       = max(0, grossExitTax − totalDDPaid)
```

> ⚠️ Incorrect alternative (do NOT use): `(grossTotalProfit − totalDDPaid) × exitTaxRate`  
> That formula under-credits the DD paid by a factor of `(1 − rate)` and produces a higher exit tax than legally required.

---

## 3. Changes Made

### 3.1 Backend

| File | Change |
|---|---|
| `Dtos/ProjectionDto.cs` | Added `ApplyDeemedDisposal`, `DeemedDisposalPercent` to `ProjectionSettingsDto`; added `DeemedDisposalPaid` to `ProjectionDataPointDto` |
| `Models/ProjectionSettings.cs` | Added `ApplyDeemedDisposal` property |
| `Models/ProjectionVersion.cs` | Added `ApplyDeemedDisposal`, `DeemedDisposalPercent` properties |
| `Data/AppDbContext.cs` | Mapped new columns for both models |
| `Services/ProjectionService.cs` | Rewrote `ComputeDataPointsAsync` with cohort engine; added `GetDeemedDisposalPercentAsync`; updated all save/get methods |
| `Migrations/…AddDeemedDisposalToProjections.cs` | New EF migration |

### 3.2 Frontend

| File | Change |
|---|---|
| `api.service.ts` | Added `applyDeemedDisposal`, `deemedDisposalPercent` to `ProjectionSettingsDto`; added `deemedDisposalPaid` to `ProjectionDataPointDto` |
| `dashboard.component.ts` | Added `projectionTaxRateLabel` getter, `onDeemedDisposalToggled()`, `getTaxDueForPoint()`, `getTaxBadgeLabel()`; updated default settings object; updated `onUserSettingsClosed` |
| `dashboard.component.html` | Added DD toggle section in projection settings panel; updated Tax Due column to show `DD` / `Exit Tax` badges |
| `dashboard.component.scss` | Added `.tax-badge`, `.badge-dd`, `.badge-exit` styles; added `.dd-toggle-section` styles |

---

## 4. Tax Due Column Behaviour

| Year | DD on | Tax Due cell |
|---|---|---|
| Regular year, no event | any | — |
| 8th / 16th… year (DD fires) | on | `€4,120 DD` |
| Final year (exit tax) | on | `€18,340 Exit Tax` |
| Final year | off | `€18,340` (no badge) |

---

## 5. Database Columns Added

| Table | Column | Type | Default |
|---|---|---|---|
| `projection_settings` | `apply_deemed_disposal` | `boolean` | `false` |
| `projection_versions` | `apply_deemed_disposal` | `boolean` | `false` |
| `projection_versions` | `deemed_disposal_percent` | `decimal(5,2)` | `0` |

---

## 6. Edge Cases

| Case | Handling |
|---|---|
| Non-Irish investor | Toggle section hidden in UI; `applyDeemedDisposal` stays `false` |
| DD % = 0 | No DD events fire (guard in engine) |
| ProjectionYears < 8 | No DD events; exit/CGT tax only |
| Final year is also a DD year | DD fires first (cost-basis stepped up), then exit tax on remaining taxable base |
| User changes DD % in user settings | Resolved fresh on every `GET /projections` and `POST /projections/calculate` |
| Saved version | `DeemedDisposalPercent` snapshot is stored in `projection_versions` — no drift |

