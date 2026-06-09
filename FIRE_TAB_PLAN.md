# FIRE Tab — Implementation Plan

## Overview

Add a **🔥 FIRE** tab to the right of the existing **📈 Projections** tab in the main navigation bar.  
FIRE = Financial Independence, Retire Early.

The tab is fully self-contained: it holds its own parameter form, a rich results summary, and a two-phase chart (accumulation → withdrawal).

---

## Core FIRE Maths (Reference)

| Concept | Formula |
|---|---|
| **FIRE Number** | `Annual Net Expenses ÷ Safe Withdrawal Rate` |
| **Annual Net Expenses** | `(Monthly Expenses × 12) − Annual Other Income` |
| **Years to FIRE** | Solved numerically year-by-year from current portfolio + contributions |
| **FIRE Age** | `Current Age + Years to FIRE` |
| **Inflation-Adjusted FIRE Number** | `FIRE Number × (1 + Inflation)^YearsToFire` |
| **Portfolio Duration (safety)** | Years until portfolio hits zero if withdrawals continue at FIRE rate |

---

## Navigation

**File:** `dashboard.component.html`

Add after the Projections `<button>` (line ~113):

```html
<button class="main-menu-btn"
        *ngIf="canViewProjections"
        [class.active]="activeMainSection === 'fire'"
        (click)="setMainSection('fire')">
  🔥 FIRE
</button>
```

Guard with `*ngIf="canViewProjections"` same as Projections.

---

## Fields — In Order of Appearance

All fields live in a `fireSettings` object in the component.  
Fields are grouped in cards/sections for clarity.

---

### 🟦 Section 1 — Your Situation Today

> _"Where are you starting from?"_

| # | Field | Type | Default | Notes |
|---|---|---|---|---|
| 1 | **Current Age** | Number (integer, 1–99) | — | Required. Anchors all age-based results. |
| 2 | **Starting Portfolio Value** | Number (decimal ≥ 0) | Live portfolio total | Same copy-from-portfolio button (📋) as in Projections. Also shows a "Copy from Projections" button (⚡) to pull `projectionSettings.startAmount`. |

---

### 🟩 Section 2 — Accumulation Phase Parameters

> _"How will you keep building until you retire?"_

A **"Copy from Projections ⚡"** button at the top of this section copies all five values at once from the current Projections settings. Individual fields remain editable after copying.

| # | Field | Type | Default | Notes |
|---|---|---|---|---|
| 3 | **Monthly Investment** | Number (decimal ≥ 0) | From Projections | How much you invest each month during accumulation. Mirrors `monthlyBuyAmount`. |
| 4 | **Annual Investment Increase %** | Number (0–100) | From Projections | How much the monthly amount grows each year (salary raises, etc.). Mirrors `annualBuyIncreasePercent`. |
| 5 | **Expected Annual Return % (Accumulation)** | Number (0–100) | From Projections | Growth rate while building the portfolio. Mirrors `yearlyReturnPercent`. |
| 6 | **Inflation %** | Number (0–100) | From Projections | Used to adjust future values to today's money. Mirrors `inflationPercent`. |

---

### 🟥 Section 3 — FIRE Target

> _"How much do you need, and when?"_

| # | Field | Type | Default | Notes |
|---|---|---|---|---|
| 7 | **Monthly Expenses in Retirement** | Number (decimal ≥ 0) | — | Required. Your expected monthly spending in retirement (in today's money). This is the most critical FIRE input. |
| 8 | **Other Monthly Income in Retirement** | Number (decimal ≥ 0) | 0 | Optional. Pension, state pension, rental income, part-time work, etc. Reduces how much the portfolio must cover. Net = Expenses − Other Income. |
| 9 | **Safe Withdrawal Rate %** | Number (0.1–20) | 4.0 | The classic "4% rule". Portfolio needed = Net Annual Expenses ÷ SWR. User can adjust (e.g. 3% for lean FIRE, 5% for coast). |

---

### 🟨 Section 4 — Withdrawal Phase Parameters

> _"What happens after you retire?"_

| # | Field | Type | Default | Notes |
|---|---|---|---|---|
| 10 | **Expected Annual Return % (Withdrawal)** | Number (0–100) | Same as Accumulation Return | Portfolio may be shifted to lower-risk assets after retiring. Defaults to Accumulation Return for simplicity; user can lower it (e.g. 5% → 3%). |
| 11 | **Withdrawal Phase Duration (years)** | Number (integer, 1–60) | 30 | How many years to model in retirement (e.g. 30 years past FIRE age). Affects the withdrawal chart and portfolio depletion check. |

---

### 🔒 Section 5 — Tax (Read-Only, Informational)

> _"Tax context used in results."_

| # | Field | Type | Source |
|---|---|---|---|
| 12 | **Exit Tax / CGT Rate** | Read-only display | Pulled from `UserSettings` exactly as in Projections. |

No tax is applied year-by-year in FIRE mode (for simplicity). Tax rate is displayed for transparency and used in a note that the FIRE number shown is pre-tax.

---

### 🎛️ Calculate Button

A prominent **"Calculate FIRE 🔥"** button triggers the calculation.  
Results update live whenever fields change (same UX pattern as Projections' Apply & Save, but instant preview without persisting).

A separate **"Save FIRE Settings 💾"** button persists the settings to a new `fire_settings` DB table.

---

## Results Section

Displayed below the form in a card grid, visible only after a valid calculation.

### 🏁 Result Cards (displayed side by side)

| Card | Title | Value | Description |
|---|---|---|---|
| A | **Your FIRE Number** | e.g. `€1,250,000` | Portfolio needed to retire: `Net Annual Expenses ÷ SWR` |
| B | **Inflation-Adjusted FIRE Number** | e.g. `€1,680,000` | FIRE Number in future money, accounting for inflation over the years to FIRE |
| C | **Years to FIRE** | e.g. `14 years` | Years until projected portfolio ≥ FIRE Number |
| D | **FIRE Age** | e.g. `Age 49` | `Current Age + Years to FIRE` |
| E | **Portfolio at FIRE** | e.g. `€1,310,000` | Actual projected value when FIRE is first reached |
| F | **Portfolio Surplus / Shortfall** | e.g. `+€60,000` | `Portfolio at FIRE − FIRE Number` (green if positive, red if negative) |
| G | **Estimated Portfolio Duration** | e.g. `38 years` | How many years the portfolio lasts in withdrawal phase before hitting zero. "Never" if portfolio grows faster than withdrawals. |

---

### 📝 Friendly Summary Text

Below the result cards, a paragraph of human-readable text, e.g.:

> _"Based on your inputs, you could reach Financial Independence at **age 49** (in **14 years**). You need a portfolio of **€1,250,000** (€1,680,000 in today's prices adjusted for inflation at 2.5%/year) to cover **€3,500/month** in expenses after **€500/month** from other income, using a **4% safe withdrawal rate**._
>
> _Your projected portfolio would reach **€1,310,000** — a **€60,000 surplus** over your FIRE number._
>
> _In withdrawal, assuming a **3% annual return**, your portfolio is estimated to last **38 years**, carrying you to age **87**. 🎉"_

Special cases:
- **Already FI:** _"🎉 Congratulations! Based on your current portfolio, you are already Financially Independent."_
- **Shortfall:** Warn that portfolio won't reach FIRE within projection horizon. Show how much monthly investment would be needed.
- **Portfolio Depletes:** Warn with age at depletion. Suggest reducing SWR or increasing accumulation.

---

## Chart

### Type
Line chart (Chart.js, matching existing style) with two visually distinct phases.

### Lines / Datasets

| Dataset | Colour | Phase |
|---|---|---|
| **Portfolio Value (Gross)** | Blue | Both phases |
| **Portfolio Value (Inflation-Adjusted)** | Blue dashed | Both phases |
| **FIRE Number Threshold** | Orange horizontal line | Both phases |
| **Inflation-Adjusted FIRE Number** | Orange dashed horizontal | Both phases |

### X-Axis
Year labels from today up to `FIRE Age + Withdrawal Duration`.  
Secondary label shows age (e.g. "2034 (Age 43)").

### Visual Annotations
- **Vertical dashed line** at the FIRE year with label "🔥 FIRE reached (Age X)".  
- Background shaded in two colours: light blue = accumulation zone, light amber = withdrawal zone.

### Canvas
`<canvas #fireChart></canvas>` inside a `projection-chart-wrapper` div.

---

## Component Changes

### `dashboard.component.ts`

1. Add `activeMainSection = 'fire'` to the union type / switch.
2. Add `fireSettings` object:
```typescript
fireSettings = {
  currentAge: null as number | null,
  startAmount: null as number | null,
  monthlyInvestment: 500,
  annualInvestmentIncreasePercent: 2,
  accumulationReturnPercent: 7,
  inflationPercent: 2.5,
  monthlyExpenses: null as number | null,
  otherMonthlyIncome: 0,
  safeWithdrawalRate: 4,
  withdrawalReturnPercent: 7,
  withdrawalYears: 30,
};
```
3. Add `fireResult` object to hold calculated values.
4. Add `calculateFire()` method with the accumulation loop + withdrawal simulation.
5. Add `copyProjectionSettingsToFire()` method.
6. Add `renderFireChart()` method using Chart.js with two-phase visualisation.
7. Add `@ViewChild('fireChart') fireChartRef!: ElementRef<HTMLCanvasElement>`.
8. Save/load fire settings via a new API endpoint.

### `dashboard.component.html`
New `<div *ngIf="activeMainSection === 'fire'">` block after Projections block.

### `dashboard.component.scss`
New styles for:
- `.fire-section` wrapper
- `.fire-params` form card
- `.fire-results-grid` card grid
- `.fire-summary-text` friendly paragraph
- `.fire-chart-wrapper` canvas container
- `.fire-phase-accent` accumulation/withdrawal colour coding

---

## Backend (API)

### New Model: `FireSettings`
```
fire_settings table:
  id, user_id, current_age, start_amount,
  monthly_investment, annual_investment_increase_percent,
  accumulation_return_percent, inflation_percent,
  monthly_expenses, other_monthly_income,
  safe_withdrawal_rate, withdrawal_return_percent,
  withdrawal_years,
  created_at, updated_at
```

### New Endpoints
| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/fire-settings` | Load saved settings |
| `PUT` | `/api/fire-settings` | Save settings |

> **Note:** FIRE calculation is done entirely in the frontend (client-side) — no backend calculation endpoint needed, keeping it simple and fast. The backend only persists the user's settings.

---

## Out of Scope (Phase 1)

- Per-year investment overrides in FIRE mode (already available in Projections)
- Deemed Disposal simulation in withdrawal phase
- SIA tax in withdrawal phase
- Multiple FIRE scenarios / saved versions

These can be added in a future phase once the base feature is validated.

---

## File Changes Summary

| File | Change |
|---|---|
| `dashboard.component.html` | Add FIRE nav button + FIRE section block |
| `dashboard.component.ts` | Add `fireSettings`, `fireResult`, `calculateFire()`, `renderFireChart()`, `copyProjectionSettingsToFire()` |
| `dashboard.component.scss` | Add FIRE-specific styles |
| `ETFTracker.Api/Models/FireSettings.cs` | New model |
| `ETFTracker.Api/Dtos/FireSettingsDto.cs` | New DTO |
| `ETFTracker.Api/Controllers/FireSettingsController.cs` | New controller |
| `ETFTracker.Api/Data/AppDbContext.cs` | Register `FireSettings` DbSet + EF config |
| `ETFTracker.Api/Migrations/` | New migration for `fire_settings` table |

---

## Open Questions for Review

1. **Currency symbol** — the UI uses `$` in Projections but the domain is Irish/EU. Should FIRE use `€`? Or follow the existing `formatCurrency()` helper?
2. **Persistence** — should FIRE settings be saved automatically (like Projections) or only on explicit "Save" button click?
3. **Tax in FIRE number** — should the FIRE number account for CGT/Exit Tax on the final lump sum, or stay pre-tax (simpler, conservative)?
4. **Coast FIRE** — future phase or include now? Coast FIRE = the amount you need *today* so that even without further contributions, compound growth alone reaches FIRE Number by a target retirement age.
5. **FIRE Number tooltip** — show formula breakdown on hover?

