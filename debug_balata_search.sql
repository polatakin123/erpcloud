-- DEBUG: Check BALATA product status
-- Run this in your database to see why search returns nothing

-- 1. Check if BALATA product exists
SELECT 
    p.id as product_id,
    p.name,
    p.code,
    p.status,
    p.is_active as product_is_active,
    p.tenant_id
FROM products p
WHERE LOWER(p.name) LIKE '%balata%';

-- 2. Check variants of BALATA product
SELECT 
    p.name as product_name,
    v.id as variant_id,
    v.sku,
    v.name as variant_name,
    v.is_active as variant_is_active,
    v.price,
    v.tenant_id
FROM products p
INNER JOIN product_variants v ON v.product_id = p.id
WHERE LOWER(p.name) LIKE '%balata%';

-- 3. Check tenant context
-- Replace <your-tenant-id> with actual tenant ID from token
SELECT 
    p.name as product_name,
    v.sku,
    v.name as variant_name,
    v.is_active,
    p.tenant_id
FROM products p
INNER JOIN product_variants v ON v.product_id = p.id
WHERE LOWER(p.name) LIKE '%balata%'
  AND p.tenant_id = '<your-tenant-id>';  -- CHECK THIS!

-- 4. Check OEM codes for BALATA
SELECT 
    p.name as product_name,
    v.sku,
    pr.ref_type,
    pr.ref_code
FROM products p
INNER JOIN product_variants v ON v.product_id = p.id
LEFT JOIN part_references pr ON pr.variant_id = v.id
WHERE LOWER(p.name) LIKE '%balata%';

-- EXPECTED ISSUES:
-- ❌ v.is_active = false  → Variant inactive (won't show in search)
-- ❌ p.status != 'Active' → Product inactive
-- ❌ Tenant ID mismatch   → Multi-tenant isolation
-- ❌ No variants exist    → Product has no variants to search
