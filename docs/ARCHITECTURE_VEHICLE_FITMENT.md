# Vehicle Fitment Filtering - Component Architecture

## Component Hierarchy

```
FastSearchPage
│
├─ VehicleFilterBar (NEW)
│  ├─ useVehicleContext (NEW)
│  │  └─ VehicleContextStore (NEW)
│  │     └─ localStorage / Tauri Store
│  │
│  └─ Vehicle Hooks (EXISTING)
│     ├─ useVehicleBrands()
│     ├─ useVehicleModels(brandId)
│     ├─ useVehicleYearRanges(modelId)
│     └─ useVehicleEngines(yearRangeId)
│
├─ Search Controls
│  ├─ Query Input (debounced 200ms)
│  ├─ Warehouse Dropdown
│  ├─ "Muadil parçaları göster" Toggle
│  └─ "Uyumu tanımsız olanları da göster" Toggle (conditional)
│
└─ Search Results
   ├─ useVariantSearch (MODIFIED)
   │  ├─ query
   │  ├─ warehouseId
   │  ├─ engineId (NEW)
   │  ├─ includeEquivalents
   │  └─ includeUndefinedFitment (NEW)
   │
   └─ Results Table
      └─ Each Result Row
         ├─ Product Info
         ├─ OEM Codes
         ├─ Match Type Badge
         ├─ Fitment Badge (NEW - conditional)
         │  ├─ "Araç uyumlu" (green)
         │  └─ "Uyum tanımsız" (gray, muted)
         ├─ Stock Info
         └─ Action Buttons
```

## Data Flow

```
User Actions:
1. Select Marka → setSelectedBrand()
                → Clear Model/Year/Engine
                → VehicleContextStore.setSelectedBrand()
                → localStorage/Tauri

2. Select Model → setSelectedModel()
                → Clear Year/Engine
                → VehicleContextStore.setSelectedModel()
                → localStorage/Tauri

3. Select Yıl   → setSelectedYear()
                → Clear Engine
                → VehicleContextStore.setSelectedYear()
                → localStorage/Tauri

4. Select Motor → setSelectedEngine()
                → VehicleContextStore.setSelectedEngine()
                → localStorage/Tauri
                → Triggers search with engineId

Search Flow:
User types query (debounced 200ms)
    ↓
useVariantSearch hook
    ↓
API: GET /api/search/variants?q={query}&engineId={id}&includeUndefinedFitment={bool}
    ↓
Backend: VariantSearchService.SearchVariantsAsync()
    ├─ Direct matches (by name/SKU/barcode/OEM)
    ├─ Equivalent matches (via OEM graph expansion)
    ├─ Fitment filtering (if engineId provided)
    │  ├─ Priority 1: Compatible + InStock + Direct
    │  ├─ Priority 2: Compatible + InStock + Equivalent
    │  ├─ Priority 3: Compatible + OutOfStock
    │  └─ Priority 4: Undefined (only if includeUndefinedFitment=true)
    └─ Sort by FitmentPriority ASC
    ↓
Response with fitmentPriority, isCompatible flags
    ↓
Frontend renders:
    ├─ Compatible results first (priorities 1-3)
    │  └─ Green badge "Araç uyumlu"
    └─ Undefined results last (priority 4)
       └─ Gray badge "Uyum tanımsız" (muted opacity)
```

## State Management Pattern

```typescript
// Similar to Branch/Warehouse context
VehicleContext {
  selectedBrandId: string | null
  selectedModelId: string | null
  selectedYearId: string | null
  selectedEngineId: string | null
}

// Stored in:
- Browser: localStorage.getItem('selectedBrandId')
- Tauri: Store.get('selectedBrandId')

// Accessed via:
const { selectedEngineId, setSelectedEngine } = useVehicleContext();
```

## API Contract

### Request
```http
GET /api/search/variants?q=OEM123&warehouseId={guid}&engineId={guid}&includeEquivalents=true&includeUndefinedFitment=false
```

### Response
```json
{
  "results": [
    {
      "variantId": "...",
      "sku": "SKU-001",
      "name": "Product Name",
      "oemRefs": ["OEM123", "OEM456"],
      "matchType": "DIRECT",
      "matchedBy": "OEM",
      "fitmentPriority": 1,
      "isCompatible": true,
      "hasDefinedFitment": true,
      "available": 10
    },
    {
      "variantId": "...",
      "sku": "SKU-002",
      "name": "Another Product",
      "oemRefs": ["OEM789"],
      "matchType": "DIRECT",
      "matchedBy": "NAME",
      "fitmentPriority": 4,
      "isCompatible": false,
      "hasDefinedFitment": false,
      "available": 5
    }
  ],
  "total": 2,
  "query": "OEM123"
}
```

## UI States

### 1. No Vehicle Selected
```
VehicleFilterBar: All dropdowns enabled, no selection
Toggle "Uyumu tanımsız...": HIDDEN
Search Results: All results shown (no fitment filtering)
Badges: HIDDEN
```

### 2. Vehicle Selected, Compatible Results
```
VehicleFilterBar: Engine selected, "Temizle" button visible
Toggle "Uyumu tanımsız...": VISIBLE, UNCHECKED
Search Results: Only compatible parts (priority 1-3)
Badges: "Araç uyumlu" (green) on all results
```

### 3. Vehicle Selected, Include Undefined ON
```
VehicleFilterBar: Engine selected
Toggle "Uyumu tanımsız...": VISIBLE, CHECKED
Search Results: 
  - Compatible parts first (priority 1-3) → green badge
  - Undefined parts last (priority 4) → gray badge, muted
```

### 4. Loading State
```
Dropdown: "Yükleniyor..." text shown
Dropdown: Disabled (grayed out)
```

## CSS Classes Used

```css
/* VehicleFilterBar container */
.bg-gradient-to-r.from-purple-50.to-blue-50
.border.border-purple-200

/* Dropdowns */
.focus:ring-2.focus:ring-purple-500
.disabled:bg-gray-100.disabled:cursor-not-allowed

/* Compatible Badge */
.bg-green-100.text-green-800

/* Undefined Badge */
.bg-gray-100.text-gray-600

/* Undefined Result Row */
.opacity-60.bg-gray-50

/* Clear Button */
.border.border-gray-300.hover:bg-gray-50
```

## Testing Flow

```
Manual QA → 10 Scenarios → Edge Cases → Performance → Acceptance
    ↓           ↓             ↓             ↓            ↓
  Setup    Selection    No Data      <500ms       All Pass
  Data     Cascade      Errors       Load Time      ↓
           Filtering    API Fail                 Deploy
           Badges
           Persistence
```

## Integration Points

### Existing Features
- ✅ Warehouse selection (works together with vehicle filter)
- ✅ Equivalent search (OEM graph expansion)
- ✅ "Satışa Ekle" button (adds to sales cart)
- ✅ Stock display (warehouseId parameter)

### New Features
- ✅ Vehicle-based filtering (engineId parameter)
- ✅ Undefined fitment toggle (includeUndefinedFitment parameter)
- ✅ Fitment badges (isCompatible flag)
- ✅ Visual priority indicators (fitmentPriority ordering)

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Vehicle dropdown load | <500ms | Per cascade level |
| Search with fitment | <1s | Including API roundtrip |
| State persistence | <100ms | Tauri Store save |
| Cache hit | Instant | React Query 30s staleTime |

## File Size Impact

| File | Lines Added | Lines Modified |
|------|-------------|----------------|
| VehicleFilterBar.tsx | 167 (new) | - |
| useVehicleContext.ts | 78 (new) | - |
| vehicle-context-store.ts | 157 (new) | - |
| FastSearchPage.tsx | ~50 | ~30 |
| usePartReferences.ts | ~15 | ~20 |
| **TOTAL** | **~467 lines** | **~50 lines** |

---

## Quick Reference: Key Concepts

**Fitment Priority Scale:**
1. Compatible + InStock + Direct Match
2. Compatible + InStock + Equivalent Match
3. Compatible + OutOfStock
4. Undefined Fitment (no StockCardFitment record)

**Visual Indicators:**
- 🟢 Green badge = Compatible with selected vehicle
- ⚪ Gray badge = Fitment undefined for this vehicle
- Muted row = Undefined fitment (lower priority)

**Cascading Logic:**
Marka → clears Model, Yıl, Motor
Model → clears Yıl, Motor
Yıl → clears Motor
Motor → (final selection, triggers filtering)

**Storage Keys:**
- `selectedBrandId`
- `selectedModelId`
- `selectedYearId`
- `selectedEngineId`
