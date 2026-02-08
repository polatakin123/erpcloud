# QA Verification - Vehicle Fitment Filtering

## Feature Overview
Vehicle-based fitment filtering in Fast Search allows users to filter search results based on vehicle compatibility. Only parts confirmed to fit the selected vehicle engine are shown (unless "include undefined" is enabled).

## Test Environment Setup
1. Ensure backend API is running (port 5039)
2. Ensure admin-desktop frontend is running
3. Have test data with:
   - Vehicle brands, models, years, and engines created
   - Stock cards with fitment data assigned
   - Stock cards without fitment data (undefined)
   - Part references (OEM codes) for testing equivalents

## Test Scenarios

### Scenario 1: Basic Vehicle Selection
**Steps:**
1. Navigate to "Hızlı Arama" (Fast Search) page
2. Verify VehicleFilterBar is visible with 4 dropdowns: Marka, Model, Yıl, Motor
3. Select a brand from "Marka" dropdown
4. Verify "Model" dropdown populates with models for selected brand
5. Select a model
6. Verify "Yıl" dropdown populates with year ranges
7. Select a year range
8. Verify "Motor" dropdown populates with engines
9. Select an engine
10. Verify selected vehicle summary appears below dropdowns

**Expected Result:**
- All dropdowns cascade correctly
- Only relevant options shown in each dropdown
- Selected vehicle displayed in readable format
- Loading states shown during data fetch

### Scenario 2: Vehicle Fitment Filtering - Compatible Only
**Steps:**
1. Complete vehicle selection (Scenario 1)
2. Enter search query for a part with known fitment (e.g., OEM code)
3. Observe search results

**Expected Result:**
- Only parts compatible with selected vehicle engine are shown
- Each compatible result displays green "Araç uyumlu" badge
- Results ordered by fitment priority (compatible+inStock first)
- No undefined fitment results visible

### Scenario 3: Include Undefined Fitment Toggle
**Steps:**
1. Complete vehicle selection
2. Search for parts
3. Enable "Uyumu tanımsız olanları da göster" checkbox
4. Observe updated results

**Expected Result:**
- Compatible parts appear first (priority 1-3)
- Undefined fitment parts appear at bottom (priority 4)
- Undefined results have gray "Uyum tanımsız" badge
- Undefined results visually muted (opacity-60, gray background)
- Toggle only appears when engine is selected

### Scenario 4: Fitment + Equivalent Search
**Steps:**
1. Select vehicle engine
2. Ensure "Muadil parçaları göster" is enabled
3. Search for OEM code that has equivalents
4. Observe results

**Expected Result:**
- Direct matches with compatible fitment shown first
- Equivalent matches with compatible fitment shown next
- Each result shows correct matchType (DIRECT/EQUIVALENT/BOTH)
- Fitment badges display correctly for all result types
- Undefined equivalents only shown if toggle enabled

### Scenario 5: Clear Vehicle Selection
**Steps:**
1. Complete vehicle selection
2. Click "Temizle" button on VehicleFilterBar
3. Perform search

**Expected Result:**
- All vehicle selections cleared
- Dropdowns reset to "Seçiniz..."
- Vehicle summary hidden
- "Uyumu tanımsız olanları da göster" toggle hidden
- Search returns all results (no fitment filtering)

### Scenario 6: Persistence Across Page Reload
**Steps:**
1. Select vehicle (Marka -> Model -> Yıl -> Motor)
2. Refresh browser page
3. Return to Fast Search page

**Expected Result:**
- Vehicle selection persists (stored in localStorage/Tauri store)
- Same vehicle automatically selected
- Search still filters by selected vehicle

### Scenario 7: Sell Button Integration
**Steps:**
1. Select vehicle engine
2. Search for compatible part
3. Click "Satışa Ekle" button on search result
4. Verify Sales Wizard opens with part pre-selected

**Expected Result:**
- Part correctly added to sales cart
- Selected vehicle context preserved
- Fitment information available in sales flow

### Scenario 8: Stock + Fitment Combined Filtering
**Steps:**
1. Select warehouse from "Depo" dropdown
2. Select vehicle engine
3. Search for parts
4. Observe results

**Expected Result:**
- Results show only compatible parts
- Stock information visible for selected warehouse
- Stock levels accurate
- Out-of-stock compatible parts shown (priority 3)
- In-stock compatible parts prioritized (priority 1-2)

### Scenario 9: No Compatible Results
**Steps:**
1. Select vehicle engine
2. Search for part that has NO fitment for selected engine
3. Disable "Uyumu tanımsız olanları da göster"

**Expected Result:**
- "Sonuç bulunamadı" (No results) message displayed
- Suggestion to enable undefined toggle or search differently

### Scenario 10: UI Turkish Localization
**Steps:**
1. Review all UI text in VehicleFilterBar and search results

**Expected Result:**
- All labels in Turkish:
  - "Marka" (Brand)
  - "Model" (Model)
  - "Yıl" (Year)
  - "Motor" (Engine)
  - "Temizle" (Clear)
  - "Araç uyumlu" (Vehicle compatible)
  - "Uyum tanımsız" (Fitment undefined)
  - "Uyumu tanımsız olanları da göster" (Show undefined fitment)
  - "Seçiniz..." (Select...)
  - "Yükleniyor..." (Loading...)

## Edge Cases to Test

### Edge Case 1: No Vehicles in Database
- VehicleFilterBar should show empty dropdowns with proper messaging
- Search should work without errors

### Edge Case 2: Partial Vehicle Selection
- Selecting only brand (no model/year/engine) should not apply fitment filtering
- Search should return all results

### Edge Case 3: Rapid Selection Changes
- Quickly changing vehicle selections should not cause errors
- Downstream dropdowns should clear correctly
- Search should use latest selection

### Edge Case 4: Backend API Failure
- Vehicle data fetch failure should show error state
- Search should still work (without fitment filtering)

## Performance Checks
- [ ] Vehicle dropdown data loads within 500ms
- [ ] Search with fitment filtering completes within 1 second
- [ ] No console errors during normal operation
- [ ] Vehicle selection state persists correctly

## Browser Compatibility
- [ ] Chrome
- [ ] Firefox
- [ ] Edge
- [ ] Tauri desktop app

## Acceptance Criteria
✅ All 10 test scenarios pass
✅ All edge cases handled gracefully
✅ Performance meets targets
✅ All text in Turkish
✅ Fitment badges display correctly
✅ Undefined results visually distinct and at bottom
✅ Vehicle selection persists across page reloads
✅ Backend integration working (engineId, includeUndefinedFitment params)

---

## Known Limitations
1. Fitment priority sorting done by backend (frontend preserves order)
2. Vehicle selection stored per-user (localStorage/Tauri)
3. Maximum 20 results per search page

## Related Documentation
- Backend: `tests/ErpCloud.Api.Tests/Services/VariantSearchServiceTests.cs`
- API: `/api/search/variants?engineId={id}&includeUndefinedFitment={bool}`
- Components: `VehicleFilterBar.tsx`, `FastSearchPage.tsx`
- Hooks: `useVehicleContext.ts`, `useVehicles.ts`
