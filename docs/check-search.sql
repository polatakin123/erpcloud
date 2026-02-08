-- Variant ve Product isimlerini kontrol et
SELECT pv."Id", pv."Sku", pv."Name" as variant_name, p."Name" as product_name
FROM product_variants pv
INNER JOIN products p ON p."Id" = pv."ProductId"
WHERE pv."TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
AND (
    pv."Name" ILIKE '%filtre%' 
    OR p."Name" ILIKE '%filtre%'
)
LIMIT 10;

-- Tüm variant isimlerini göster
SELECT pv."Sku", pv."Name" as variant_name, p."Name" as product_name
FROM product_variants pv
INNER JOIN products p ON p."Id" = pv."ProductId"
WHERE pv."TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
LIMIT 10;
