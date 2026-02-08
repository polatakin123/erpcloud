/**
 * Example: How to integrate OEM Reference Panel into Variant Detail Page
 * 
 * This file demonstrates integration patterns for the OEM/Equivalent Search feature.
 */

import { useParams } from 'react-router-dom';
import OemReferencePanel from '../components/OemReferencePanel';

/**
 * EXAMPLE 1: Variant Detail Page
 * 
 * Add the OEM Reference Panel as a section in your variant detail page.
 */
export function VariantDetailPage() {
  const { variantId } = useParams<{ variantId: string }>();
  
  // ... your existing variant detail logic ...
  
  return (
    <div className="max-w-7xl mx-auto p-6 space-y-6">
      {/* Existing sections: General Info, Pricing, etc. */}
      
      {/* OEM References Section */}
      <OemReferencePanel 
        variantId={variantId!} 
        variantName="Example Product Variant"  // Pass variant name for context
      />
      
      {/* Other sections... */}
    </div>
  );
}

/**
 * EXAMPLE 2: Sales Wizard - Variant Selection Step
 * 
 * Replace basic variant dropdown with Fast Search component.
 */
import { useState } from 'react';
import { useVariantSearch } from '../hooks/usePartReferences';

export function SalesWizardVariantStep() {
  const [query, setQuery] = useState('');
  const [selectedVariantId, setSelectedVariantId] = useState<string | null>(null);
  
  const { data: searchResults } = useVariantSearch({
    query,
    includeEquivalents: true,
    warehouseId: 'your-warehouse-id',  // Optional
  });

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold">Select Product</h2>
      
      {/* Search Input */}
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search by name, SKU, barcode, or OEM code..."
        className="w-full px-4 py-2 border rounded-lg"
      />

      {/* Results */}
      {searchResults?.results.map((variant) => (
        <div
          key={variant.variantId}
          onClick={() => setSelectedVariantId(variant.variantId)}
          className={`p-4 border rounded-lg cursor-pointer hover:bg-gray-50 ${
            selectedVariantId === variant.variantId ? 'ring-2 ring-blue-500' : ''
          }`}
        >
          <div className="flex items-center justify-between">
            <div>
              <div className="font-medium">{variant.name}</div>
              <div className="text-sm text-gray-600">SKU: {variant.sku}</div>
              {variant.oemRefs.length > 0 && (
                <div className="text-xs text-gray-500 mt-1">
                  OEM: {variant.oemRefs.join(', ')}
                </div>
              )}
            </div>
            
            <div className="text-right">
              {/* Match Type Badge */}
              {variant.matchType === 'EQUIVALENT' && (
                <span className="inline-block px-2 py-1 text-xs bg-yellow-100 text-yellow-800 rounded">
                  Equivalent Part
                </span>
              )}
              
              {/* Stock Info */}
              {variant.available !== undefined && (
                <div className="text-sm mt-1">
                  Stock: {variant.available} available
                </div>
              )}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

/**
 * EXAMPLE 3: Purchase Wizard - With Equivalent Suggestions
 * 
 * Show equivalent alternatives when selecting a variant.
 */
export function PurchaseWizardWithEquivalents() {
  const [mainVariantId, setMainVariantId] = useState<string>('');
  const [showEquivalents, setShowEquivalents] = useState(false);
  
  // Get the main variant's OEM codes
  const { data: mainVariantOems } = usePartReferences(mainVariantId);
  
  // Search for equivalents using the first OEM code
  const { data: equivalents } = useVariantSearch({
    query: mainVariantOems?.[0]?.refCode || '',
    includeEquivalents: true,
  });

  return (
    <div>
      {/* Main variant selection */}
      <div>
        <label>Selected Variant</label>
        {/* ... variant selector ... */}
      </div>

      {/* Show equivalents button */}
      {mainVariantOems && mainVariantOems.length > 0 && (
        <button
          onClick={() => setShowEquivalents(!showEquivalents)}
          className="text-blue-600 text-sm mt-2"
        >
          {showEquivalents ? 'Hide' : 'Show'} equivalent alternatives
        </button>
      )}

      {/* Equivalents list */}
      {showEquivalents && equivalents && (
        <div className="mt-4 p-4 bg-blue-50 rounded-lg">
          <h3 className="font-medium mb-2">Equivalent Alternatives</h3>
          <div className="space-y-2">
            {equivalents.results
              .filter(v => v.variantId !== mainVariantId)
              .map(variant => (
                <div key={variant.variantId} className="flex justify-between items-center">
                  <span className="text-sm">{variant.name}</span>
                  <button
                    onClick={() => setMainVariantId(variant.variantId)}
                    className="text-xs text-blue-600 hover:underline"
                  >
                    Switch to this
                  </button>
                </div>
              ))}
          </div>
        </div>
      )}
    </div>
  );
}

/**
 * EXAMPLE 4: API Usage (TypeScript/JavaScript)
 * 
 * Direct API calls without React hooks.
 */
import { apiClient } from '../lib/api-client';

// Add OEM code to variant
async function addOemCode(variantId: string, oemCode: string) {
  try {
    const response = await apiClient.post(
      `/variants/${variantId}/references`,
      {
        refType: 'OEM',
        refCode: oemCode,
      }
    );
    console.log('OEM code added:', response.data);
  } catch (error: any) {
    if (error.response?.data?.error === 'duplicate_reference') {
      alert('This OEM code is already added');
    } else {
      alert('Failed to add OEM code');
    }
  }
}

// Search for parts
async function searchParts(query: string, includeEquivalents: boolean = true) {
  const params = new URLSearchParams({
    q: query,
    includeEquivalents: String(includeEquivalents),
  });
  
  const response = await apiClient.get(`/search/variants?${params}`);
  return response.data.results;
}

// Get all OEM codes for a variant
async function getOemCodes(variantId: string) {
  const response = await apiClient.get(`/variants/${variantId}/references`);
  return response.data.filter((ref: any) => ref.refType === 'OEM');
}

/**
 * EXAMPLE 5: Backend Integration Test
 * 
 * Sample test scenario for OEM/Equivalent search.
 */
/*
// C# - Integration Test Example

[Fact]
public async Task SearchByOem_ReturnsEquivalentParts()
{
    // Arrange
    var variantA = await CreateVariantAsync("Product A");
    var variantB = await CreateVariantAsync("Product B");
    var variantC = await CreateVariantAsync("Product C");
    
    await AddOemCodeAsync(variantA.Id, "12345");
    await AddOemCodeAsync(variantB.Id, "12345");
    await AddOemCodeAsync(variantB.Id, "67890");
    await AddOemCodeAsync(variantC.Id, "67890");
    
    // Act
    var results = await SearchVariantsAsync("12345", includeEquivalents: true);
    
    // Assert
    Assert.Equal(3, results.Count);  // A (direct), B (direct), C (transitive)
    Assert.Contains(results, r => r.VariantId == variantA.Id && r.MatchType == "DIRECT");
    Assert.Contains(results, r => r.VariantId == variantB.Id && r.MatchType == "BOTH");
    Assert.Contains(results, r => r.VariantId == variantC.Id && r.MatchType == "EQUIVALENT");
}
*/
