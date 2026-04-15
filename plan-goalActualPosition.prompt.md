# Plan: Overlay Actual Holdings on Both Goal Charts

Add a second "Actual Position" series to both goal charts using data already available in the frontend (`portfolioEvolution.dataPoints` + `dashboard.header.totalHoldingsAmount` + `goalVersionSettings`). No backend changes required.

---

## Data Sources (all already loaded in the frontend)

| Source | Field | Used for |
|---|---|---|
| `portfolioEvolution.dataPoints` | `{ date: "yyyy-MM-dd", totalValue, hasBuy }[]` | Past actual values (year-end & month-end) |
| `dashboard.header.totalHoldingsAmount` | `number` | Current live portfolio total |
| `goalVersionSettings` | `ProjectionSettingsDto` | Monthly buy, return %, annual buy increase % |
| `goalDataPoints` | `GoalDataPointDto[]` | Year labels for the year chart |

---

## Steps

### 1. Add `computeActualYearlyPositions()` helper in `dashboard.component.ts`

Returns `(number | null)[]` — one value per entry in `goalDataPoints` (same order/length).

Logic per year:

- **year < currentYear** → find the last `portfolioEvolution.dataPoints` entry whose `date` starts with `"${year}-"` and return its `totalValue`. Return `null` if no data found for that year.
- **year === currentYear** → start from `dashboard.header.totalHoldingsAmount`:
  1. Determine `currentMonth` (1-based, April = 4 in 2026).
  2. Check if a buy has occurred this month (`portfolioEvolution.dataPoints.some(p => p.date.startsWith("${currentYear}-${mm}-") && p.hasBuy)`).
  3. `currentMonthValue = currentHoldings + (buyThisMonth ? 0 : adjustedMonthlyBuy)` — add the planned monthly buy if it hasn't happened yet.
  4. For each remaining full month (`currentMonth + 1` through `12`): `value = (value + adjustedMonthlyBuy) * monthlyGrowthFactor`.
  5. Return the December year-end estimate.
- **year > currentYear** → compound forward from the previous year's estimated value (entry at `goalDataPoints.indexOf(year) - 1`):
  - For each of 12 months: `value = (value + adjustedMonthlyBuy) * monthlyGrowthFactor`.
  - `adjustedMonthlyBuy = settings.monthlyBuyAmount * Math.pow(1 + annualBuyIncreasePercent/100, yearIndex)`.
  - Return the December estimate.

Return `null` for any year where `goalVersionSettings` is null and estimation is required (current/future years).

---

### 2. Add `computeActualMonthlyPositions()` helper in `dashboard.component.ts`

Returns `{ value: number | null; type: 'actual' | 'current' | 'forecast' }[]` — 12 entries (Jan–Dec of the current year).

Logic per month `m` (1-based):

- **m < currentMonth** → find the last `portfolioEvolution.dataPoints` entry whose `date` starts with `"${currentYear}-${mm}-"`. Return `{ value: totalValue ?? null, type: 'actual' }`.
- **m === currentMonth** →
  - `buyThisMonth = portfolioEvolution.dataPoints.some(p => date in current year/month && p.hasBuy)`
  - `value = dashboard.header.totalHoldingsAmount + (buyThisMonth ? 0 : adjustedMonthlyBuy)`
  - Return `{ value, type: 'current' }`.
- **m > currentMonth** → compound iteratively from the previous month's value:
  - `value = (prev + adjustedMonthlyBuy) * monthlyGrowthFactor`
  - Return `{ value, type: 'forecast' }`.
  - Return `{ value: null, type: 'forecast' }` if `goalVersionSettings` is null.

Where:
- `monthlyGrowthFactor = Math.pow(1 + settings.yearlyReturnPercent / 100, 1 / 12)`
- `adjustedMonthlyBuy = settings.monthlyBuyAmount * Math.pow(1 + settings.annualBuyIncreasePercent / 100, yearIndex)` (same `yearIndex` formula as in `computeMonthlyGoalTargets()`)

---

### 3. Update `renderGoalChart()` in `dashboard.component.ts`

Add two new datasets alongside the existing green goal line:

**Dataset A — "Actual / Estimated Position" (solid orange)**
- Color: `#f89b29`
- `data`: `computeActualYearlyPositions()` values for past years + current year estimate (null for future years).
- Style: `fill: false`, `tension: 0.35`, `pointRadius: 5`, `borderDash: []` (solid).

**Dataset B — "Position Forecast" (dashed orange)**
- Color: `rgba(248, 155, 41, 0.55)`
- `data`: null for past years, current year estimate bridged into future years (same bridge pattern as existing monthly chart).
- Style: `fill: false`, `tension: 0.35`, `pointRadius: 4`, `borderDash: [5, 4]` (dashed).

The current year value is included in **both** datasets (acts as the bridge point between solid actual and dashed forecast).

---

### 4. Update `renderMonthlyGoalChart()` in `dashboard.component.ts`

Add two new datasets for actual/forecast position (using `computeActualMonthlyPositions()`):

**Dataset C — "Actual Position" (solid orange)**
- Color: `#f89b29`
- `data`: values where `type === 'actual'` or `type === 'current'`, null for future months.
- Current month point: `pointRadius: 9`, `pointBackgroundColor: '#f05252'` (highlighted).
- Past month points: `pointRadius: 5`.

**Dataset D — "Position Forecast" (dashed orange)**
- Color: `rgba(248, 155, 41, 0.45)`
- `data`: current month value bridged into `type === 'forecast'` values, null for past months.
- Style: `borderDash: [5, 4]`, `pointRadius: 4`.

---

### 5. Re-trigger goal charts when `portfolioEvolution` arrives late

In `loadPortfolioEvolution()`'s `.next` callback, after setting `this.portfolioEvolution = data`, add:

```typescript
// If the user is already viewing the goal section with charts rendered,
// invalidate them so they re-render with actual position data.
if (this.activeMainSection === 'goal' && this.goalDataPoints.length > 0) {
  this.goalChartRendered = false;
  this.goalChart?.destroy();
  this.goalChart = null;
  this.goalMonthlyChartRendered = false;
  this.goalMonthlyChart?.destroy();
  this.goalMonthlyChart = null;
}
```

---

## Further Considerations

1. **Settings dependency for projections** — current-year and future estimates require `goalVersionSettings`. If the goal has no `sourceVersionId`, the actual series shows actual past data only with `null` for current/future estimates. Charts render gracefully with a partial series.

2. **Year chart current-year point semantics** — current year shows the projected *year-end* value (December), not today's value, for consistency with the goal line's year-end anchors.

3. **Annual buy increase application** — future year monthly buys use `settings.monthlyBuyAmount * Math.pow(1 + annualBuyIncreasePercent/100, yearIndex)`, same formula as in the existing `computeMonthlyGoalTargets()` method.

4. **Missing history for early years** — years before the user's first transaction have no entries in `portfolioEvolution`; those points render as `null` (gap in the line), which is the correct behaviour.

5. **Month padding for date matching** — when filtering `portfolioEvolution` by month, zero-pad: `String(month).padStart(2, '0')` to match the `"yyyy-MM-dd"` format.

