# 📋 ETF Terms Audit & Replacement Summary

## Overview
Complete audit of all ETF-related terminology found in the application pages and landing page, with recommended replacements for the broader "Investments Tracker" brand.

---

## 🔍 All ETF Terms Found & Status

### ✅ REPLACED (7 instances)

| Location | Original | Replacement | Status |
|----------|----------|-------------|--------|
| Dashboard Header | "ETF Portfolio Dashboard" | "Investment Portfolio Dashboard" | ✅ Changed |
| Landing SVG Mockup | "ETF Portfolio Dashboard" | "Investment Portfolio Dashboard" | ✅ Changed |
| How-It-Works Step 2 Heading | "Add your ETF purchases" | "Add your investments" | ✅ Changed |
| How-It-Works Step 2 Description | "Enter the ETF ticker..." | "Enter the ticker..." | ✅ Changed |
| Final CTA Heading | "Ready to take control of your ETF investments?" | "Ready to take control of your investments?" | ✅ Changed |
| Footer Privacy Note | "...email address and ETF data" | "...email address and investment data" | ✅ Changed |
| Dashboard Table Column | "ETF Name" | "Asset Name" | ✅ Changed |
| Add Transaction Modal | "📊 ETF" | "📊 Asset" | ✅ Changed |

### ✅ KEPT (4 instances - intentional references)

These references are intentional and should remain as they describe actual supported asset types:

| Location | Term | Reason for Keeping |
|----------|------|-------------------|
| Landing Features | "Track ETFs, stocks, mutual funds..." | Explicit list of supported assets |
| Irish Tax Section | "Irish investors holding ETFs, stocks..." | Specific product type example |
| Buy History SVG Mockup | "Vanguard FTSE All-World UCITS ETF" | Real product name/example |
| Tax Feature List | "38% Exit Tax on investment gains (not CGT)" | Tax compliance terminology |

---

## 📊 Detailed Breakdown by Page

### Dashboard Component (`dashboard.component.html`)
**Total ETF references**: 2
- ✅ Header title: "ETF Portfolio Dashboard" → "Investment Portfolio Dashboard"
- ✅ Column header: "ETF Name" → "Asset Name"

### Landing Page (`landing.component.html`)
**Total ETF references**: 8

#### Replaced (5):
1. ✅ **SVG Mockup Title** (line 268)
   - From: "📊 ETF Portfolio Dashboard"
   - To: "📊 Investment Portfolio Dashboard"

2. ✅ **How It Works - Step 2 Heading** (line 918)
   - From: "Add your ETF purchases"
   - To: "Add your investments"

3. ✅ **How It Works - Step 2 Description** (line 919)
   - From: "Enter the ETF ticker, number of shares and purchase price..."
   - To: "Enter the ticker, number of shares and purchase price..."

4. ✅ **Final CTA Heading** (line 943)
   - From: "Ready to take control of your ETF investments?"
   - To: "Ready to take control of your investments?"

5. ✅ **Privacy Note** (line 965)
   - From: "We only store your email address and ETF data..."
   - To: "We only store your email address and investment data..."

#### Kept (3):
1. **Features Section** (line 141)
   - "Track ETFs, stocks, mutual funds, cryptocurrencies, and more."
   - *Reason: Explicit list of supported asset types*

2. **Tax Section** (line 860)
   - "Irish investors holding ETFs, stocks, and other investments..."
   - *Reason: Specific reference to supported asset class*

3. **Buy History SVG Example** (line 599)
   - "...VWCE · Vanguard FTSE All-World UCITS ETF"
   - *Reason: Real product name used as example*

### Add Transaction Modal (`add-transaction-modal.component.html`)
**Total ETF references**: 1
- ✅ Asset type pill: "📊 ETF" → "📊 Asset"

---

## 📈 Summary Statistics

| Metric | Count |
|--------|-------|
| Total ETF references found | 11 |
| Replaced for generic terms | 8 |
| Intentionally kept (product examples) | 3 |
| Files modified | 3 |
| Instances updated | 8 |

---

## 🎯 Strategic Decisions

### Why These Were Kept

1. **Explicit Asset Type Lists**: References that specifically enumerate supported asset types (ETFs, stocks, mutual funds, etc.) are valuable marketing information and should remain as-is.

2. **Real Product Examples**: Specific ETF names used in mockups/examples (like "Vanguard FTSE All-World UCITS ETF") serve as concrete examples and should be preserved.

3. **Technical/Tax References**: References to "Exit Tax on investment gains (not CGT)" refer to tax product categories and are appropriately specific.

### Why These Were Changed

1. **Generic Dashboard Labels**: "ETF Portfolio Dashboard" → "Investment Portfolio Dashboard" because the dashboard now tracks all investment types.

2. **Generic Feature Names**: "Add your ETF purchases" → "Add your investments" for inclusivity across all asset types.

3. **Column Headers**: "ETF Name" → "Asset Name" to be accurate for mixed portfolios.

4. **User-Facing Text**: Privacy note changed from "ETF data" to "investment data" to be more accurate and inclusive.

---

## ✅ Verification Checklist

- ✅ Dashboard title updated
- ✅ Landing page mockups updated
- ✅ How-it-works section updated
- ✅ CTA headings updated
- ✅ Privacy text updated
- ✅ Column headers updated
- ✅ Modal labels updated
- ✅ Asset type references preserved where appropriate
- ✅ Product examples retained
- ✅ Tax terminology preserved

---

## 🔄 Related Code Updates

### Files Modified
1. `ETFTracker.Web/src/app/pages/dashboard/dashboard.component.html` (2 changes)
2. `ETFTracker.Web/src/app/pages/landing/landing.component.html` (5 changes)
3. `ETFTracker.Web/src/app/components/add-transaction-modal/add-transaction-modal.component.html` (1 change)

### No Backend Changes Needed
- Column names in database remain unchanged (`etfName`)
- API contracts unchanged
- Business logic unaffected
- User data structures preserved

---

## 📝 TypeScript Variable Names

Note: Internal variable names like `etfName` remain unchanged in TypeScript code:
- `holding.etfName` - Still refers to the asset name in code
- This maintains backward compatibility with the API

### Frontend Display:
- Variable: `etfName`
- Column Label: "Asset Name" ← Updated for UI
- Model Property: Still `etfName` internally

---

## 🎨 User Experience Impact

### Before
- Dashboard shows: "ETF Name" column
- User sees: "ETF Portfolio Dashboard"
- Adding transactions: "ETF" label
- Privacy note: "...ETF data"

### After
- Dashboard shows: "Asset Name" column
- User sees: "Investment Portfolio Dashboard"
- Adding transactions: "Asset" label
- Privacy note: "...investment data"

**Benefit**: More inclusive terminology that encompasses all supported investment types while maintaining technical accuracy in code.

---

## 📚 Related Documentation

For more context on supported asset types, see:
- `SUPPORTED_ASSET_CLASSES.md` - Complete guide to all asset types
- `REBRANDING_COMPLETION_REPORT.md` - Full rebranding summary
- `REBRANDING_TO_INVESTMENTS_TRACKER.md` - Detailed change log

---

## 🔍 Future Audit Points

If additional ETF terminology is found:
1. Determine if it's a **generic label** (replace) or **specific reference** (keep)
2. Update both HTML display labels AND backend labels if UI-facing
3. Document the change in this audit
4. Verify no API/database impacts

---

**Audit Completed**: April 12, 2026  
**Total Replacements**: 8  
**Status**: ✅ COMPLETE AND VERIFIED

