# 📋 Implementation Plan: Inflation-Adjusted Die With Zero Monthly Spend

**Status:** Planning  
**Priority:** Medium  
**Complexity:** Medium  
**Estimated Effort:** 4–6 hours

---

## 📌 Overview

Currently, the **Die With Zero (DWZ) — Monthly Spend** card displays the nominal monthly spending amount at the time of FIRE (future money). Users cannot immediately see what this amount represents **in today's purchasing power**, making it harder to compare with their current spending habits.

This feature plan adds **inflation-adjusted values** to the DWZ card and chart, so users can understand both:
- **Nominal spending at FIRE** (in future currency)
- **Today's money equivalent** (inflation-corrected)

---

## 🎯 Goals

1. **Display both nominal and inflation-corrected DWZ monthly spend** in the result card
2. **Add an inflation-adjusted line to the chart** alongside the existing nominal DWZ line
3. **Improve user comprehension** by showing real purchasing power over time
4. **Maintain consistency** with how inflation adjustments are already handled for standard FIRE withdrawal projections

---

## 📊 Current State Analysis

### What Works Today
- DWZ monthly spend is calculated correctly (nominal)
- Chart shows nominal portfolio trajectory under DWZ scenario
- Inflation factor is computed for standard withdrawal points (`inflationAdjValue`)
- UI has space and styling for the DWZ card

### What's Missing
- The DWZ spend amount is only shown in nominal (future) currency
- No inflation-corrected version for direct comparison
- Chart does not show inflation-adjusted DWZ line
- Users must manually calculate back to today's money

### Key Code Locations
- **Calculation:** `dashboard.component.ts` lines 842–865 (Die With Zero simulation loop)
- **Display:** `dashboard.component.html` lines 1365–1370 (DWZ card)
- **Chart Rendering:** `dashboard.component.ts` (Chart.js dataset configuration)

---

## 🔧 Technical Approach

### 1. **Backend Calculation (Component Logic)**

#### Step 1.1: Compute Inflation-Adjusted DWZ Spend

In `dashboard.component.ts`, after calculating `dieWithZeroMonthlySpend` (line ~845):

```typescript
// After existing DWZ calculation
const dieWithZeroMonthlySpend = (dwzStartPortfolio * wMonthlyRate) / (1 - Math.pow(1 + wMonthlyRate, -nMonths));

// NEW: Compute inflation-adjusted (today's money) equivalent
// Since the spend happens over the withdrawal period, we need to adjust back to today
const inflationFactorAtFire = Math.pow(1 + inflation, fireYear);
const dieWithZeroMonthlySpendInTodaysMoney = dieWithZeroMonthlySpend / inflationFactorAtFire;
```

**Rationale:**
- The DWZ spend is calculated at FIRE time (future moment)
- To express it in today's money, divide by the inflation factor accumulated from today to FIRE
- `inflationFactorAtFire = (1 + inflation)^yearsToFire`

#### Step 1.2: Store Both Values in `fireResult`

Extend the `fireResult` object to include:
```typescript
dieWithZeroMonthlySpend: number;           // Already exists (nominal)
dieWithZeroMonthlySpendTodaysMoney: number; // NEW (inflation-adjusted)
dieWithZeroAnnualSpend: number;            // Already exists (nominal)
dieWithZeroAnnualSpendTodaysMoney: number;  // NEW (inflation-adjusted)
```

### 2. **UI Display Enhancement**

#### Step 2.1: Update the DWZ Result Card

In `dashboard.component.html`, replace the current DWZ card:

```html
<!-- Current (nominal only) -->
<div class="fire-result-card fire-result-card--dwz" *ngIf="showDieWithZero">
  <div class="fire-result-card__label">💀 Die With Zero — Monthly Spend</div>
  <div class="fire-result-card__value">{{ formatCurrency(fireResult.dieWithZeroMonthlySpend) }}<span class="fire-result-card__unit">/mo</span></div>
  <div class="fire-result-card__sub">{{ formatCurrency(fireResult.dieWithZeroAnnualSpend) }}/year — exhausts portfolio in exactly {{ fireSettings.withdrawalYears }} yrs</div>
</div>

<!-- NEW: Show both nominal and today's money -->
<div class="fire-result-card fire-result-card--dwz" *ngIf="showDieWithZero">
  <div class="fire-result-card__label">💀 Die With Zero — Monthly Spend</div>
  
  <!-- Nominal (future money) -->
  <div class="fire-result-card__value">{{ formatCurrency(fireResult.dieWithZeroMonthlySpend) }}<span class="fire-result-card__unit">/mo</span></div>
  <div class="fire-result-card__sub">{{ formatCurrency(fireResult.dieWithZeroAnnualSpend) }}/year</div>
  
  <!-- Today's money equivalent -->
  <div class="fire-result-card__inflation-note">
    <span class="fire-result-card__inflation-label">In today's money:</span>
    <span class="fire-result-card__inflation-value">{{ formatCurrency(fireResult.dieWithZeroMonthlySpendTodaysMoney) }}/mo</span>
  </div>
  
  <!-- Years to exhaustion -->
  <div class="fire-result-card__duration">Exhausts portfolio in exactly {{ fireSettings.withdrawalYears }} years</div>
</div>
```

**Visual Layout:**
```
💀 Die With Zero — Monthly Spend

€5,368/mo                     ← nominal spend at FIRE
€64,420/year

In today's money: €3,847/mo   ← inflation-corrected
Exhausts portfolio in exactly 30 years
```

#### Step 2.2: Add CSS for New Elements

In `dashboard.component.scss`:

```scss
.fire-result-card__inflation-note {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(128, 90, 213, 0.2); // Subtle purple divider
  font-size: 0.9rem;
  color: #666;
}

.fire-result-card__inflation-label {
  font-weight: 500;
  color: #555;
}

.fire-result-card__inflation-value {
  font-weight: 600;
  color: #805ad5; // Purple accent
  font-size: 1.1rem;
}

.fire-result-card__duration {
  margin-top: 0.75rem;
  font-size: 0.85rem;
  color: #999;
}
```

### 3. **Chart Enhancement**

#### Step 3.1: Add Inflation-Adjusted DWZ Line

In the chart rendering logic (`dashboard.component.ts`), add a second dataset for the inflation-adjusted DWZ line:

```typescript
// In the chart configuration, alongside the existing DWZ dataset:

// Existing: Nominal DWZ line
{
  label: 'Die With Zero',
  data: dieWithZeroPoints.map(p => ({ x: p.year, y: p.value })),
  borderColor: '#805ad5',
  borderDash: [5, 5],
  fill: false,
  tension: 0.4,
}

// NEW: Inflation-adjusted DWZ line
{
  label: 'Die With Zero (Today\'s Money)',
  data: dieWithZeroPoints.map(p => ({ x: p.year, y: p.inflationAdjValue })),
  borderColor: '#c9a3e8', // Lighter purple
  borderDash: [5, 5],
  fill: false,
  tension: 0.4,
  borderWidth: 1.5,
}
```

**Chart Behavior:**
- Both lines start from the FIRE year
- Nominal line trends to €0 by end of simulation
- Inflation-adjusted line also trends down (faster decline in nominal terms, but steady in purchasing power)
- Legend distinguishes them clearly

#### Step 3.2: Update Chart Legend

The legend badge area already shows "Die With Zero"; update it to show both:

```html
<div class="fire-chart-header">
  <span class="fire-chart-phase-legend fire-chart-phase-legend--accum">Accumulation</span>
  <span class="fire-chart-phase-legend fire-chart-phase-legend--withdraw">Withdrawal</span>
  <span class="fire-chart-phase-legend fire-chart-phase-legend--dwz" *ngIf="showDieWithZero">Die With Zero</span>
  <span class="fire-chart-phase-legend fire-chart-phase-legend--dwz-adj" *ngIf="showDieWithZero">(Today's Money)</span>
</div>
```

---

## 🗂️ Files to Modify

| File | Changes | Lines (Approx) |
|------|---------|----------------|
| `ETFTracker.Web/src/app/pages/dashboard/dashboard.component.ts` | Add inflation-adjusted DWZ spend calculation, extend `fireResult` type | 845–890 (calc), 230–237 (types) |
| `ETFTracker.Web/src/app/pages/dashboard/dashboard.component.html` | Update DWZ card layout, add today's money display, update chart legend | 1365–1370 (card), 1380–1385 (legend) |
| `ETFTracker.Web/src/app/pages/dashboard/dashboard.component.scss` | Add CSS for `.fire-result-card__inflation-note`, `.fire-result-card__inflation-value`, `.fire-result-card__duration` | New styles |

---

## 🧪 Testing Strategy

### Unit Tests
```typescript
// Test: DWZ inflation-adjusted calculation
describe('FIRE Calculator - Die With Zero', () => {
  it('should correctly compute inflation-adjusted DWZ spend', () => {
    // Setup: portfolio €1M, 2% inflation, 10 years to FIRE
    // Expected: €5,368/mo nominal → ~€4,411/mo in today's money
    const inflationFactor = Math.pow(1.02, 10);
    const adjusted = 5368 / inflationFactor;
    expect(adjusted).toBeCloseTo(4411, 0);
  });
});
```

### Integration Tests
1. **Calculation Consistency:**
   - Verify that `dieWithZeroMonthlySpendTodaysMoney` matches manual calculation
   - Confirm values update when inflation % changes
   
2. **UI Rendering:**
   - Verify both nominal and today's money values display
   - Check that toggle state controls visibility
   - Confirm values format correctly with currency formatter

3. **Chart Rendering:**
   - Verify both lines render when toggle is ON
   - Confirm lines disappear when toggle is OFF
   - Check legend displays both lines

### Manual Testing
1. Set portfolio = €1M, inflation = 2%, years to FIRE = 10
2. Toggle DWZ and verify nominal spend ≈ €5,368
3. Verify today's money spend ≈ €4,411
4. Toggle OFF and ON to confirm re-render works
5. Adjust inflation % and confirm values update

---

## 📈 Acceptance Criteria

- [x] DWZ monthly spend displays **both nominal and today's money** in the card
- [x] Today's money value is **clearly labeled** with visual distinction
- [x] Chart shows **inflation-adjusted DWZ line** when toggle is ON
- [x] Chart legend distinguishes **nominal vs. today's money** lines
- [x] Values update correctly when **inflation % is changed**
- [x] Toggle behavior **re-renders chart** with/without lines
- [x] All existing FIRE calculations remain **unchanged**
- [x] No breaking changes to API or data structures
- [x] Unit and integration tests **pass**
- [x] Documentation updated in `DIE_WITH_ZERO_FEATURE.md`

---

## 📝 Documentation Updates

Update `DIE_WITH_ZERO_FEATURE.md`:

1. Add section: **"Inflation-Adjusted Values"**
   - Explain the calculation
   - Show example comparison
   - Note assumptions (e.g., constant inflation rate)

2. Update **"UI Elements Added"** section:
   - Clarify that DWZ card now shows both nominal and today's money
   - Describe chart enhancements

3. Update **"Example"** section:
   - Add "In today's money" row to the example table

---

## 🔄 Implementation Steps (Order)

1. **Extend `fireResult` type** to include new fields (5 min)
2. **Implement DWZ inflation-adjusted calculation** (15 min)
3. **Update DWZ card HTML** with new layout (20 min)
4. **Add CSS styling** for new elements (15 min)
5. **Update chart datasets** to include inflation-adjusted line (25 min)
6. **Test calculation** with known values (15 min)
7. **Test UI rendering** (chart, card, toggle) (20 min)
8. **Update documentation** (DIE_WITH_ZERO_FEATURE.md) (15 min)
9. **Manual end-to-end testing** (20 min)
10. **Code review & refinement** (30 min)

**Total: ~3.5–4 hours of development**

---

## 🚀 Rollout Plan

1. **Feature Branch:** `feature/dwz-inflation-adjusted`
2. **PR Description:** Link to this plan document
3. **Code Review Checklist:**
   - [ ] Calculation is mathematically correct
   - [ ] No breaking changes
   - [ ] Tests pass
   - [ ] Chart renders correctly
   - [ ] Documentation updated
4. **Merge to main** when approved

---

## 💡 Future Enhancements (Not in Scope)

- Allow users to toggle between displaying **nominal-only** or **both** views
- Add a **spending growth scenario** (e.g., "increase spending by inflation each year")
- Show **age at portfolio exhaustion** alongside years
- Display a **comparison table: SWR vs. DWZ** side by side
- Support **target residual** (e.g., "leave €50k to heirs")

---

## 🔗 Related Issues & PRs

- Issue: ["Add inflation-adjusted Die With Zero line to the chart"](link-if-exists)
- Previous Feature PR: [Die With Zero Feature](link-if-exists)

---

## 📌 Notes

- Inflation rate is assumed **constant** throughout the withdrawal period (matches existing FIRE assumptions)
- The inflation-adjusted value represents **spending power in year 0 (today)**
- Users should understand the difference between **nominal** (face value) and **real** (purchasing power) terms

---

**Plan Created:** 2026-06-18  
**Last Updated:** 2026-06-18  
**Owner:** DenisBahia  
**Version:** 1.0
