# QA Verification - Tezgah (Quick Sale) Vehicle Fitment Integration

## Feature Overview
Vehicle fitment filtering is now integrated into the Tezgah (Quick Sale) page, allowing cashiers to filter parts by vehicle compatibility during fast sales operations. This prevents selling incompatible parts when a specific vehicle is being serviced.

## Test Environment Setup
1. Ensure backend API is running (port 5039)
2. Ensure admin-desktop frontend is running
3. Navigate to Tezgah → Hızlı Satış (Quick Sale)
4. Have test data with:
   - Vehicle brands, models, years, and engines
   - Stock cards with fitment data
   - Stock cards without fitment data
   - Barcode-enabled variants

## Test Scenarios

### Scenario 1: Mini Vehicle Selector - Collapsed State
**Steps:**
1. Navigate to "Hızlı Satış" page
2. Observe the vehicle selector in the left search panel (above barcode input)
3. Verify initial state is collapsed
4. Verify header shows "Araç seçilmedi"
5. Verify chevron-down icon is visible

**Expected Result:**
- Compact collapsed view
- Purple/blue gradient border
- Car icon visible
- No vehicle selected message

### Scenario 2: Mini Vehicle Selector - Expansion
**Steps:**
1. Click on the vehicle selector header
2. Verify it expands to show 4 dropdowns
3. Verify dropdowns are: Marka, Model, Yıl, Motor
4. Click header again to collapse

**Expected Result:**
- Smooth expansion/collapse animation
- All 4 dropdowns visible when expanded
- Chevron changes to up/down appropriately
- State persists during expansion

### Scenario 3: Vehicle Selection - Cascading Behavior
**Steps:**
1. Expand vehicle selector
2. Select a brand
3. Verify Model dropdown populates
4. Select a model
5. Verify Yıl dropdown populates
6. Select a year
7. Verify Motor dropdown populates
8. Select an engine
9. Verify summary updates

**Expected Result:**
- Each selection enables next dropdown
- Downstream selections clear when upstream changes
- Summary shows: Brand • Model • Year • Engine
- Green checkmark message appears: "✓ Araç seçildi - Uyumlu parçalar filtreleniyor"

### Scenario 4: Vehicle Selection - Persistence
**Steps:**
1. Complete vehicle selection (all 4 dropdowns)
2. Navigate away from Hızlı Satış page
3. Return to Hızlı Satış page
4. Verify vehicle selection is still active

**Expected Result:**
- Vehicle selection persists (shared context-store)
- Summary shows previously selected vehicle
- Dropdown values match previous selection

### Scenario 5: Barcode Search - With Vehicle Selected
**Steps:**
1. Select a vehicle (all 4 dropdowns)
2. Scan/enter barcode of compatible part
3. Press Enter
4. Observe search results

**Expected Result:**
- Search API called with `engineId` parameter
- Only compatible parts returned
- Compatible parts show green "Araç uyumlu" badge
- Incompatible parts NOT shown in results

### Scenario 6: Barcode Search - Without Vehicle Selected
**Steps:**
1. Clear vehicle selection (click X button)
2. Scan/enter barcode
3. Press Enter
4. Observe search results

**Expected Result:**
- Search API called WITHOUT `engineId` parameter
- All matching parts returned (no fitment filtering)
- No "Araç uyumlu" badges shown
- Standard search behavior (backward compatible)

### Scenario 7: Adding Compatible Part to Cart
**Steps:**
1. Select a vehicle
2. Search for compatible part
3. Click on search result to add to cart
4. Observe cart line

**Expected Result:**
- Part added to cart successfully
- Cart line shows green "Araç uyumlu" badge
- Badge appears below SKU in cart table
- CheckCircle icon visible in badge

### Scenario 8: Preventing Incompatible Part Addition
**Steps:**
1. Select a vehicle
2. Temporarily modify backend to return incompatible part (fitmentPriority=4)
3. Try to add incompatible part to cart

**Expected Result:**
- Alert dialog appears:
  - "⚠️ Uyumsuz Parça!"
  - "Bu parça seçili araçla uyumlu değil."
  - "Devam etmek için araç seçimini temizleyin."
- Part NOT added to cart
- Focus returns to barcode input

### Scenario 9: Clear Vehicle Selection (X Button)
**Steps:**
1. Complete vehicle selection
2. Click the X button on collapsed vehicle selector
3. Verify vehicle selection cleared
4. Perform barcode search

**Expected Result:**
- All vehicle selections cleared
- Summary returns to "Araç seçilmedi"
- X button disappears
- Search returns to standard behavior (no fitment filtering)
- Existing cart items lose "Araç uyumlu" badges on next search

### Scenario 10: Complete Sale with Vehicle Selected
**Steps:**
1. Select a vehicle
2. Add compatible parts to cart
3. Select sale type (Peşin or Veresiye)
4. Complete sale (F9)
5. Verify sale completes successfully

**Expected Result:**
- Sale processes normally
- No errors related to fitment data
- Invoice/shipment created successfully
- Vehicle selection persists for next sale
- Cart clears but vehicle selection remains

### Scenario 11: OEM Search with Vehicle Filtering
**Steps:**
1. Select a vehicle
2. Enter OEM code in "OEM / Ürün Adı" field (F2)
3. Click search button
4. Observe results

**Expected Result:**
- Search includes `engineId` parameter
- Results filtered by fitment
- Compatible parts show "Araç uyumlu" badge
- Equivalent matches (if any) also show "⚡ Muadil" badge

### Scenario 12: Mixed Cart - Compatible Parts Only
**Steps:**
1. Select vehicle
2. Add 3 different compatible parts
3. Verify all show "Araç uyumlu" badge
4. Complete sale
5. Verify no warnings or errors

**Expected Result:**
- All cart lines show green badge
- Sale completes without issues
- No fitment-related validation errors

## Edge Cases to Test

### Edge Case 1: Change Vehicle Mid-Sale
**Steps:**
1. Select vehicle A, add compatible parts to cart
2. Change vehicle selection to vehicle B
3. Try to add parts compatible with B

**Expected Result:**
- Cart retains parts from vehicle A (with badges)
- New parts compatible with B can be added
- Mixed compatibility acceptable (user responsibility)

### Edge Case 2: Clear Vehicle with Items in Cart
**Steps:**
1. Select vehicle, add compatible parts
2. Clear vehicle selection (X button)
3. Observe cart

**Expected Result:**
- Cart items remain
- "Araç uyumlu" badges remain on existing items
- New searches don't filter by vehicle
- No validation errors

### Edge Case 3: Collapsed Selector with Active Selection
**Steps:**
1. Select vehicle (all 4 dropdowns)
2. Collapse vehicle selector
3. Verify summary visible when collapsed

**Expected Result:**
- Collapsed header shows full vehicle summary
- 🚗 emoji prefix visible
- X button visible for quick clear
- Can expand to change selection

### Edge Case 4: Rapid Barcode Scans
**Steps:**
1. Select vehicle
2. Rapidly scan 5 barcodes in succession
3. Observe cart and search behavior

**Expected Result:**
- All searches use engineId parameter
- No race conditions
- All compatible parts added
- Incompatible parts rejected with alert

### Edge Case 5: Backend Fitment Data Missing
**Steps:**
1. Select vehicle
2. Search for part that has NO fitment data defined
3. Observe behavior

**Expected Result:**
- Part not returned in search results (backend filters it out with includeUndefinedFitment=false)
- No errors or crashes
- Clear search results if no compatible parts exist

## Performance Checks
- [ ] Vehicle dropdown loads within 500ms
- [ ] Barcode search completes within 800ms with engineId
- [ ] Cart updates instantly when adding items
- [ ] No UI freezing during vehicle selection
- [ ] Expansion/collapse animation smooth

## Visual Regression Checks
- [ ] MiniVehicleSelector fits in left panel without overflow
- [ ] Badges don't break cart table layout
- [ ] Colors consistent (green for compatible, purple for vehicle selector)
- [ ] Icons render correctly (Car, CheckCircle, X, ChevronDown/Up)

## Browser Compatibility
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Edge (latest)
- [ ] Tauri desktop app

## Acceptance Criteria
✅ MiniVehicleSelector visible and functional in Tezgah
✅ Collapsed by default for space efficiency
✅ Vehicle selection persists across page reloads
✅ Barcode/OEM search includes engineId when vehicle selected
✅ Only compatible parts returned when vehicle selected
✅ "Araç uyumlu" badge shows on compatible cart items
✅ Incompatible parts prevented from being added
✅ Clear (X) button works correctly
✅ No regression in normal quick sale flow (without vehicle)
✅ All text in Turkish
✅ Sale completes successfully with vehicle selected

---

## Known Limitations
1. Changing vehicle mid-sale doesn't re-validate existing cart items
2. Cart badge persists even after clearing vehicle (by design - shows what was selected at time of add)
3. If backend fitment data changes, page refresh required

## Backward Compatibility
- ✅ Existing quick sale flow works unchanged when no vehicle selected
- ✅ Barcode scanning behavior identical when vehicle not selected
- ✅ All existing keyboard shortcuts still functional (F1, F2, F3, F9, ESC)

## Related Documentation
- Backend: `VariantSearchService.SearchVariantsAsync()` with engineId parameter
- API: `/api/search/variants?q={query}&engineId={id}&includeUndefinedFitment=false`
- Components: `MiniVehicleSelector.tsx`, `FastSalesPage.tsx`
- Hooks: `useVehicleContext.ts` (shared with FastSearchPage)
- Store: `vehicle-context-store.ts` (shared persistence)

## Test Data Requirements
For comprehensive testing, ensure:
1. At least 3 vehicle brands with models/years/engines
2. 10+ stock cards with fitment data for various engines
3. 5+ stock cards WITHOUT fitment data
4. Parts with barcodes for barcode scanning tests
5. Parts with OEM codes for OEM search tests
6. Some parts with equivalent relationships (for muadil testing)

---

## Regression Testing Checklist

### Quick Sale Core Functionality (Must Not Break)
- [ ] Barcode scanning works
- [ ] OEM search works
- [ ] Adding items to cart works
- [ ] Quantity adjustment works
- [ ] Price/discount editing works
- [ ] Removing items works
- [ ] Peşin (cash) sale completes
- [ ] Veresiye (credit) sale completes
- [ ] Customer picker works (F3)
- [ ] Payment methods work (cash/card/bank)
- [ ] Invoice generation works
- [ ] Payment allocation works
- [ ] Keyboard shortcuts work (F1, F2, F3, F9, ESC)

### Integration Points
- [ ] Fast Search → Tezgah "Satışa Ekle" button still works
- [ ] Vehicle selection shared between Fast Search and Tezgah
- [ ] Warehouse selection still works
- [ ] Stock levels display correctly
- [ ] Customer selection dialog works
- [ ] Toast notifications work

---

## Success Metrics
- Zero regression bugs in existing quick sale flow
- Vehicle fitment filtering works in 100% of test scenarios
- Page load time < 2 seconds with vehicle selector
- User can complete a vehicle-filtered sale in < 30 seconds
- No console errors during normal operation
