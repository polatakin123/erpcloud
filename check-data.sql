-- Veritabanında veri var mı kontrol et
SELECT 'Products' as "Table", COUNT(*) as "Count" FROM products WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL
SELECT 'Brands', COUNT(*) FROM vehicle_brands WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL
SELECT 'Models', COUNT(*) FROM vehicle_models WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL
SELECT 'Engines', COUNT(*) FROM vehicle_engines WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid;
