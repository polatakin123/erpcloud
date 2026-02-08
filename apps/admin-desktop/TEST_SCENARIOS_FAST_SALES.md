# 🧪 SPRINT-3.5 PHASE B1: Tezgâh (Quick Sale) Test Scenarios

## 📋 Overview
This document contains manual test scenarios for the **Dealer POS Experience Polish** (FastSalesPage keyboard-first enhancements).

**Test Date:** _______________  
**Tester:** _______________  
**Build Version:** _______________

---

## ✅ Test Scenario 1: Barcode Burst Detection + Cash Sale

### Objective
Verify barcode burst detection (auto qty++ within 2s) and complete a cash sale using only keyboard.

### Prerequisites
- At least 3 products with barcodes in system
- At least 1 product with stock ≥ 3
- Valid cashbox configured

### Steps

1. **Open FastSalesPage**
   - ✅ Pass | ❌ Fail | Notes: _______________

2. **Press F1** → Verify barcode input is focused and selected
   - ✅ Pass | ❌ Fail | Notes: _______________

3. **Scan/Type Product A barcode + Enter**
   - Expected: Product added to cart, toast shows product name
   - ✅ Pass | ❌ Fail | Notes: _______________

4. **Within 2 seconds, scan same barcode again**
   - Expected: Quantity increments to 2 (NO new line), toast shows "Miktar: 2 ✓"
   - ✅ Pass | ❌ Fail | Notes: _______________

5. **Wait 3+ seconds, then scan same barcode again**
   - Expected: Quantity increments to 3, toast shows qty update
   - ✅ Pass | ❌ Fail | Notes: _______________

6. **Press F1** → Scan Product B barcode
   - Expected: New line added, focus returns to barcode
   - ✅ Pass | ❌ Fail | Notes: _______________

7. **Press F1** → Scan Product C barcode
   - Expected: 3 lines total in cart
   - ✅ Pass | ❌ Fail | Notes: _______________

8. **Press ↓** → Verify first cart line gets blue ring highlight
   - ✅ Pass | ❌ Fail | Notes: _______________

9. **Press +** → Verify selected line quantity increments by 1
   - ✅ Pass | ❌ Fail | Notes: _______________

10. **Press -** → Verify selected line quantity decrements by 1
    - ✅ Pass | ❌ Fail | Notes: _______________

11. **Press ↓ twice** → Verify selection moves to line 3
    - ✅ Pass | ❌ Fail | Notes: _______________

12. **Press Delete** → Verify line 3 removed, selection shifts to line 2
    - ✅ Pass | ❌ Fail | Notes: _______________

13. **Press F6** → Verify payment method focused (cash only for peşin)
    - ✅ Pass | ❌ Fail | Notes: _______________

14. **Press F9** → Initiate sale
    - Expected: Processing steps shown, toast shows "✅ Peşin Satış Tamamlandı!" with invoice details
    - ✅ Pass | ❌ Fail | Invoice No: _______________

15. **Press F10** → Verify recent panels appear on right side
    - Expected: Recent items and recent sales panels visible
    - ✅ Pass | ❌ Fail | Notes: _______________

16. **Check Recent Sales Panel**
    - Expected: Just-completed sale appears at top with green "Peşin" badge
    - ✅ Pass | ❌ Fail | Notes: _______________

17. **Press F10 again** → Verify panels close
    - ✅ Pass | ❌ Fail | Notes: _______________

### Expected Result
✅ Complete sale using only F-keys, arrows, +/-, Delete - NO mouse clicks

### Actual Result
_______________________________________________________________

---

## ✅ Test Scenario 2: Search + Credit Sale (Keyboard-Only)

### Objective
Verify search workflow, customer picker, and credit sale (irsaliye) using keyboard.

### Prerequisites
- At least 2 products searchable by name/OEM
- At least 1 customer (party) configured with credit limit
- Valid warehouse configured

### Steps

1. **Open FastSalesPage**
   - ✅ Pass | ❌ Fail | Notes: _______________

2. **Press F2** → Verify OEM/Name search input focused and selected
   - ✅ Pass | ❌ Fail | Notes: _______________

3. **Type partial product name** (e.g., "filtre")
   - Expected: Search results appear automatically after 200ms debounce
   - ✅ Pass | ❌ Fail | Notes: _______________

4. **Click search result Product D** → Verify added to cart
   - Expected: Product added, focus returns to barcode input (F1)
   - ✅ Pass | ❌ Fail | Notes: _______________

5. **Press F2** → Search for Product E
   - Expected: 2 products in cart
   - ✅ Pass | ❌ Fail | Notes: _______________

6. **Click "Veresiye" button** (top-right payment panel)
   - Expected: Sale type changes to credit (purple)
   - ✅ Pass | ❌ Fail | Notes: _______________

7. **Press F7** → Verify customer picker modal opens
   - ✅ Pass | ❌ Fail | Notes: _______________

8. **Select a customer** → Click to select
   - Expected: Modal closes, customer shown in purple card with balance/limit
   - ✅ Pass | ❌ Fail | Customer: _______________

9. **Press ↓** → Select first cart line
   - ✅ Pass | ❌ Fail | Notes: _______________

10. **Press F4** → Verify discount input focused (if discount field exists on selected line)
    - ✅ Pass | ❌ Fail | Notes: _______________

11. **Type discount** (e.g., "10") → Press Enter
    - Expected: Discount applied, total recalculated
    - ✅ Pass | ❌ Fail | Notes: _______________

12. **Press F9** → Complete sale
    - Expected: Processing steps, toast shows "✅ Veresiye Satış Tamamlandı!" with irsaliye details
    - ✅ Pass | ❌ Fail | İrsaliye No: _______________

13. **Press F10** → Check recent sales panel
    - Expected: Just-completed sale appears with purple "Veresiye" badge
    - ✅ Pass | ❌ Fail | Notes: _______________

### Expected Result
✅ Complete credit sale with customer selection and discount using keyboard shortcuts

### Actual Result
_______________________________________________________________

---

## ✅ Test Scenario 3: Fitment Block + Stock Warnings

### Objective
Verify vehicle fitment check blocks incompatible parts, and stock warnings appear correctly.

### Prerequisites
- Vehicle selected via MiniVehicleSelector
- At least 1 product **incompatible** with selected vehicle
- At least 1 product with stock ≤ 0 (out of stock)
- At least 1 product with stock 1-4 (low stock)

### Steps

1. **Open FastSalesPage**
   - ✅ Pass | ❌ Fail | Notes: _______________

2. **Select a vehicle** using MiniVehicleSelector (top-left)
   - Expected: Vehicle shown above barcode input
   - ✅ Pass | ❌ Fail | Vehicle: _______________

3. **Press F2** → Search for **incompatible** product
   - ✅ Pass | ❌ Fail | Product: _______________

4. **Click incompatible product**
   - Expected: **Toast error** with red background: "❌ Araç Uyumsuzluğu - [Product] bu araca uyumlu değil"
   - Expected: Product **NOT** added to cart
   - ✅ Pass | ❌ Fail | Notes: _______________

5. **Press F2** → Search for **out-of-stock** product (stock ≤ 0)
   - ✅ Pass | ❌ Fail | Product: _______________

6. **Click out-of-stock product**
   - Expected: Product added to cart (if allowed)
   - Expected: **Red toast warning**: "❌ Stokta Yok - [Product] stoklarda bulunamadı"
   - Expected: Cart line shows red stock badge "Stok: 0"
   - ✅ Pass | ❌ Fail | Notes: _______________

7. **Press F2** → Search for **low-stock** product (stock 1-4)
   - ✅ Pass | ❌ Fail | Product: _______________

8. **Click low-stock product**
   - Expected: Product added to cart
   - Expected: **Yellow toast warning**: "⚠️ Düşük Stok - [Product] - Sadece X adet kaldı"
   - Expected: Cart line shows yellow stock badge "Stok: X"
   - ✅ Pass | ❌ Fail | Notes: _______________

9. **Verify cart line badges:**
   - Brand badge (with logo or initial)
   - Stock badge (color-coded: red/yellow/slate)
   - Fitment badge (green "Uyumlu" if compatible)
   - Profit badge (if available, shows %profit in red/yellow/green)
   - ✅ Pass | ❌ Fail | Notes: _______________

10. **Press ESC** → Verify cancel confirmation
    - Expected: Confirm dialog appears, "Satışı iptal etmek istediğinize emin misiniz?"
    - ✅ Pass | ❌ Fail | Notes: _______________

11. **Click OK** → Verify cart cleared
    - Expected: Cart empty, toast "Satış İptal Edildi"
    - ✅ Pass | ❌ Fail | Notes: _______________

### Expected Result
✅ Fitment check blocks incompatible parts, stock warnings appear with color-coded badges

### Actual Result
_______________________________________________________________

---

## 📝 10-Step Manual Test Script (Quick Smoke Test)

### Quick keyboard workflow validation (5 minutes)

1. **Open FastSalesPage** → Press F1 → Verify barcode input focused ✅ ❌
2. **Scan/Type barcode** + Enter → Verify product added with toast ✅ ❌
3. **Scan same barcode within 2s** → Verify qty++ with toast "Miktar: 2 ✓" ✅ ❌
4. **Press ↓** → Verify first line selected (blue ring) ✅ ❌
5. **Press +** → Verify qty increments ✅ ❌
6. **Press -** → Verify qty decrements ✅ ❌
7. **Press Delete** → Verify line removed ✅ ❌
8. **Press F2** → Type product name → Verify search results appear ✅ ❌
9. **Press F9** → Verify sale completes with toast ✅ ❌
10. **Press F10** → Verify recent panels toggle on/off ✅ ❌

---

## 🎯 Success Criteria

### Must Pass (Blocking)
- ✅ Barcode burst detection works (qty++ within 2s)
- ✅ All F-key shortcuts functional (F1, F2, F3, F4, F6, F7, F9, F10)
- ✅ Arrow keys navigate cart lines (↑↓)
- ✅ +/- keys adjust quantity
- ✅ Delete key removes selected line
- ✅ ESC cancels sale with confirmation
- ✅ Toast notifications replace all alerts
- ✅ Recent sales panel saves and displays sales
- ✅ Stock warnings show with correct colors
- ✅ Build passes with 0 TypeScript errors

### Should Pass (Nice-to-Have)
- ✅ Fitment check blocks incompatible parts
- ✅ Recent items panel tracks last additions
- ✅ Profit badges display correctly
- ✅ Debounced search performs well (200ms)
- ✅ Cart line selection visual feedback clear

### Known Limitations
- Settings integration not implemented (confirmBeforeSale, barcodeBurstQtyIncrement, showRecentPanels)
- F4 discount focus only works if discount field is editable on selected line
- Recent items panel onAddToCart requires SearchResult object (may need mapping)

---

## 📊 Test Summary

| Scenario | Pass | Fail | Notes |
|----------|------|------|-------|
| Scenario 1: Barcode Burst + Cash | ☐ | ☐ | |
| Scenario 2: Search + Credit | ☐ | ☐ | |
| Scenario 3: Fitment + Stock Warnings | ☐ | ☐ | |
| 10-Step Smoke Test | ☐ | ☐ | |

**Overall Status:** ☐ PASS | ☐ FAIL | ☐ BLOCKED

**Blockers:** _______________________________________________________________

**Notes:** _______________________________________________________________

---

## 🐛 Bugs Found

| ID | Severity | Description | Steps to Reproduce |
|----|----------|-------------|-------------------|
| 1 | | | |
| 2 | | | |
| 3 | | | |

---

## ✨ Enhancements Noted

1. _______________________________________________________________
2. _______________________________________________________________
3. _______________________________________________________________

---

**Test Completed By:** _______________  
**Date:** _______________  
**Sign-off:** _______________
