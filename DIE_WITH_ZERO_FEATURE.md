# 💀 Die With Zero — Feature Documentation

## Overview

**Die With Zero** is a complementary concept added to the FIRE Calculator, inspired by Bill Perkins' book *Die With Zero: Getting All You Can from Your Money and Your Life*.

While the classic FIRE approach aims to build a portfolio large enough to never deplete (via a Safe Withdrawal Rate), **Die With Zero** flips the question:

> *"What is the maximum I can spend each month so that my portfolio reaches exactly €0 by the end of my retirement horizon?"*

This is a useful tool for people who are comfortable spending down their wealth intentionally — prioritising experiences and quality of life over leaving an estate.

---

## How It Works

### Location

The toggle is located in the **🎯 FIRE Target** section of the FIRE Calculator, inside a dedicated **💀 Die With Zero** panel.

### Toggle: "Show Die With Zero info"

- When **unchecked** (default): the FIRE calculator behaves exactly as before.
- When **checked**: a new card, chart line, and summary text are revealed.

---

## Calculation

### Formula Used

The Die With Zero monthly spend is calculated using the **present-value annuity formula**:

```
monthly_spend = (portfolio_at_fire × monthly_rate) / (1 − (1 + monthly_rate)^(−n_months))
```

Where:
- `portfolio_at_fire` = the projected portfolio value at the moment FIRE is reached
- `monthly_rate` = `(1 + withdrawal_return_percent)^(1/12) − 1`
- `n_months` = `withdrawal_years × 12`

If `monthly_rate = 0` (i.e., 0% return), the formula degrades gracefully to:
```
monthly_spend = portfolio_at_fire / n_months
```

### Inputs Used
| Input | Source |
|-------|--------|
| Starting portfolio (at FIRE) | Output of the accumulation simulation |
| Annual Return % | **Withdrawal Phase → Annual Return %** field |
| Simulate (years) | **Withdrawal Phase → Simulate (years)** field |
| Inflation % | **Accumulation → Inflation %** field |

### Example

| Parameter | Value |
|-----------|-------|
| Portfolio at FIRE | €1,000,000 |
| Withdrawal Return | 5% per year |
| Simulate (years) | 30 |

```
monthly_rate = (1.05)^(1/12) − 1 ≈ 0.004074
n_months = 30 × 12 = 360

monthly_spend = (1,000,000 × 0.004074) / (1 − (1.004074)^(−360))
             ≈ €5,368/month  (≈ €64,420/year)
```

This contrasts with the classic 4% SWR result:
```
SWR annual = 1,000,000 × 4% = €40,000/year (€3,333/month)
```

At 5% return, Die With Zero allows **~60% more monthly spending** because it deliberately exhausts the portfolio.

---

## UI Elements Added

### 1. Toggle in FIRE Target Section
A checkbox labelled **"Show Die With Zero info"** inside a purple-styled 💀 Die With Zero panel. When enabled, a hint is shown explaining what the calculation does.

### 2. Result Card — "💀 Die With Zero — Monthly Spend"
Appears after the **Estimated Portfolio Duration** card when the toggle is on.

- Shows the **monthly spend** in a purple accent
- Shows the **annual equivalent** and the exact number of years to exhaustion

### 3. Chart Line — "Die With Zero"
A dashed **purple line** is added to the FIRE chart during the withdrawal phase, showing the portfolio trajectory under the Die With Zero spending plan.

- The line starts at the FIRE moment and trends to zero by the end of the simulation window
- A legend badge **"Die With Zero"** is shown in the chart header

### 4. Summary Text Addition
When the toggle is enabled, the friendly summary text paragraph is extended with:

> 💀 Die With Zero: spending €X/month (€Y/year) would exhaust the portfolio in exactly Z years.

---

## Key Differences vs. Standard FIRE Withdrawal

| Aspect | Standard FIRE (SWR) | Die With Zero |
|--------|---------------------|---------------|
| Goal | Portfolio survives indefinitely | Portfolio reaches €0 at target date |
| Monthly spend | Conservative (4% rule) | Maximised for the time horizon |
| Residual estate | Large (portfolio keeps growing) | Zero by design |
| Risk profile | Low depletion risk | Higher — no buffer if life extends beyond horizon |
| Philosophy | "Don't run out of money" | "Don't leave money on the table" |

---

## Implementation Notes

### Files Changed

| File | Change |
|------|--------|
| `dashboard.component.ts` | Added `showDieWithZero` flag, extended `fireResult` type, die-with-zero simulation in `calculateFire()`, `toggleDieWithZero()` method, chart dataset, summary text |
| `dashboard.component.html` | Toggle panel in FIRE Target, new result card, chart legend badge |
| `dashboard.component.scss` | `.fire-param-section--dwz`, `.fire-result-card--dwz`, `.fire-chart-phase-legend--dwz`, `.fire-toggle-label`, `.fire-toggle-text` |

### Re-rendering Behaviour
Toggling the Die With Zero switch calls `toggleDieWithZero()` which destroys and re-renders the chart to add/remove the purple line. This ensures the legend is always consistent with the visible data.

---

## Future Enhancements (Ideas)

- Add inflation-adjusted Die With Zero line to the chart
- Allow the user to input a **target residual** (e.g. "leave €50k for heirs") instead of strict zero
- Show a comparison table of SWR monthly spend vs Die With Zero monthly spend side by side
- Include age at portfolio exhaustion in the card

