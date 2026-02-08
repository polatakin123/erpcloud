-- Test Verisi Oluşturma Script'i
-- PostgreSQL için
-- TenantId: a1b2c3d4-e5f6-7890-abcd-ef1234567890 (Demo Organizasyon)
-- ÖNEMLİ: Önce create-demo-org.sql script'ini çalıştırın!

BEGIN;

-- TenantId değişkeni
DO $$
DECLARE
    demo_tenant_id uuid := 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
    zero_user_id uuid := '00000000-0000-0000-0000-000000000000';
BEGIN

-- DELETE işlemleri - Child'dan Parent'a doğru (Foreign Key sırası)
DELETE FROM stock_card_fitments WHERE "TenantId" = demo_tenant_id;
DELETE FROM part_references WHERE "TenantId" = demo_tenant_id;
DELETE FROM product_variants WHERE "TenantId" = demo_tenant_id;
DELETE FROM vehicle_engines WHERE "TenantId" = demo_tenant_id;
DELETE FROM products WHERE "TenantId" = demo_tenant_id;
DELETE FROM vehicle_year_ranges WHERE "TenantId" = demo_tenant_id;
DELETE FROM vehicle_models WHERE "TenantId" = demo_tenant_id;
DELETE FROM vehicle_brands WHERE "TenantId" = demo_tenant_id;

-- 1. Araç Markaları
INSERT INTO vehicle_brands ("Id", "Code", "Name", "TenantId", "CreatedAt", "CreatedBy") VALUES
(gen_random_uuid(), 'TOYOTA', 'Toyota', demo_tenant_id, NOW(), zero_user_id),
(gen_random_uuid(), 'FORD', 'Ford', demo_tenant_id, NOW(), zero_user_id),
(gen_random_uuid(), 'BMW', 'BMW', demo_tenant_id, NOW(), zero_user_id),
(gen_random_uuid(), 'MERCEDES', 'Mercedes-Benz', demo_tenant_id, NOW(), zero_user_id),
(gen_random_uuid(), 'VW', 'Volkswagen', demo_tenant_id, NOW(), zero_user_id);

-- 2. Araç Modelleri
INSERT INTO vehicle_models ("Id", "BrandId", "Name", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'TOYOTA' AND "TenantId" = demo_tenant_id LIMIT 1),
    'Corolla', demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'TOYOTA' AND "TenantId" = demo_tenant_id LIMIT 1), 'Camry', demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'FORD' AND "TenantId" = demo_tenant_id LIMIT 1), 'Focus', demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'FORD' AND "TenantId" = demo_tenant_id LIMIT 1), 'Fiesta', demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'BMW' AND "TenantId" = demo_tenant_id LIMIT 1), '3 Serisi', demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'MERCEDES' AND "TenantId" = demo_tenant_id LIMIT 1), 'C-Class', demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_brands WHERE "Code" = 'VW' AND "TenantId" = demo_tenant_id LIMIT 1), 'Golf', demo_tenant_id, NOW(), zero_user_id;

-- 3. Yıl Aralıkları
INSERT INTO vehicle_year_ranges ("Id", "ModelId", "YearFrom", "YearTo", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    (SELECT "Id" FROM vehicle_models WHERE "Name" = 'Corolla' AND "TenantId" = demo_tenant_id LIMIT 1),
    2018, 2020, demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_models WHERE "Name" = 'Corolla' AND "TenantId" = demo_tenant_id LIMIT 1), 2021, 2024, demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_models WHERE "Name" = 'Focus' AND "TenantId" = demo_tenant_id LIMIT 1), 2015, 2019, demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_models WHERE "Name" = 'Fiesta' AND "TenantId" = demo_tenant_id LIMIT 1), 2016, 2020, demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_models WHERE "Name" = '3 Serisi' AND "TenantId" = demo_tenant_id LIMIT 1), 2018, 2022, demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_models WHERE "Name" = 'C-Class' AND "TenantId" = demo_tenant_id LIMIT 1), 2017, 2021, demo_tenant_id, NOW(), zero_user_id
UNION ALL SELECT gen_random_uuid(), (SELECT "Id" FROM vehicle_models WHERE "Name" = 'Golf' AND "TenantId" = demo_tenant_id LIMIT 1), 2019, 2023, demo_tenant_id, NOW(), zero_user_id;

-- 4. Motorlar
INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    '1.6 VVT-i', 'Benzin', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = 'Corolla' AND vyr."YearFrom" = 2018 AND vyr."TenantId" = demo_tenant_id LIMIT 1;

INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    '1.8 Hybrid', 'Hybrid', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = 'Corolla' AND vyr."YearFrom" = 2021 AND vyr."TenantId" = demo_tenant_id LIMIT 1;

INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    '1.5 EcoBoost', 'Benzin', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = 'Focus' AND vyr."TenantId" = demo_tenant_id LIMIT 1;

INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    '1.0 EcoBoost', 'Benzin', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = 'Fiesta' AND vyr."TenantId" = demo_tenant_id LIMIT 1;

INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    '320i 2.0T', 'Benzin', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = '3 Serisi' AND vyr."TenantId" = demo_tenant_id LIMIT 1;

INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    'C200 2.0T', 'Benzin', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = 'C-Class' AND vyr."TenantId" = demo_tenant_id LIMIT 1;

INSERT INTO vehicle_engines ("Id", "YearRangeId", "Code", "FuelType", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    vyr."Id",
    '1.4 TSI', 'Benzin', demo_tenant_id, NOW(), zero_user_id
FROM vehicle_year_ranges vyr
INNER JOIN vehicle_models vm ON vyr."ModelId" = vm."Id"
WHERE vm."Name" = 'Golf' AND vyr."TenantId" = demo_tenant_id LIMIT 1;

-- 5. 50 Ürün
WITH product_data AS (
    SELECT 
        generate_series(1, 50) as num
)
INSERT INTO products ("Id", "Code", "Name", "Description", "IsActive", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    'PRD' || lpad(num::text, 3, '0'),
    CASE (num % 8)
        WHEN 0 THEN 'Fren Balatası Set #' || num
        WHEN 1 THEN 'Hava Filtresi #' || num
        WHEN 2 THEN 'Yağ Filtresi #' || num
        WHEN 3 THEN 'Buji Takımı #' || num
        WHEN 4 THEN 'Amortisör #' || num
        WHEN 5 THEN 'Far Ampülü H7 #' || num
        WHEN 6 THEN 'Egzoz Susturucu #' || num
        WHEN 7 THEN 'Termostat #' || num
    END,
    'Yüksek kaliteli yedek parça',
    true,
    demo_tenant_id,
    NOW(),
    zero_user_id
FROM product_data;

-- 6. Varyantlar
INSERT INTO product_variants ("Id", "ProductId", "Sku", "Barcode", "Name", "Unit", "VatRate", "IsActive", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    p."Id",
    'SKU' || lpad(ROW_NUMBER() OVER (ORDER BY p."Code")::text, 4, '0'),
    '869' || (RANDOM() * 1000000000)::bigint,
    p."Name",
    'EA',
    0.20,
    true,
    demo_tenant_id,
    NOW(),
    zero_user_id
FROM products p
WHERE p."TenantId" = demo_tenant_id;

-- 7. OEM Kodları (Her varyanta 2 OEM, her 5 üründen biri ortak)
INSERT INTO part_references ("Id", "VariantId", "RefType", "RefCode", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    pv."Id",
    'OEM',
    'OEM' || lpad(((ROW_NUMBER() OVER (ORDER BY pv."Sku") - 1) / 5)::text, 2, '0') || 
    'A' || ((ROW_NUMBER() OVER (ORDER BY pv."Sku") - 1) % 5)::text,
    demo_tenant_id,
    NOW(),
    zero_user_id
FROM product_variants pv
WHERE pv."TenantId" = demo_tenant_id;

INSERT INTO part_references ("Id", "VariantId", "RefType", "RefCode", "TenantId", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    pv."Id",
    'OEM',
    'ALT' || lpad(((ROW_NUMBER() OVER (ORDER BY pv."Sku") - 1) / 5)::text, 2, '0') || 
    'X' || ((ROW_NUMBER() OVER (ORDER BY pv."Sku") - 1) % 5)::text,
    demo_tenant_id,
    NOW(),
    zero_user_id
FROM product_variants pv
WHERE pv."TenantId" = demo_tenant_id;

-- 8. Araç Uyumluluğu (Her varyanta rastgele 1-3 motor)
INSERT INTO stock_card_fitments ("Id", "VariantId", "VehicleEngineId", "Notes", "TenantId", "CreatedAt", "CreatedBy")
SELECT DISTINCT ON (pv."Id", ve."Id")
    gen_random_uuid(),
    pv."Id",
    ve."Id",
    'Araç uyumluluğu',
    demo_tenant_id,
    NOW(),
    zero_user_id
FROM product_variants pv
CROSS JOIN vehicle_engines ve
WHERE pv."TenantId" = demo_tenant_id
  AND ve."TenantId" = demo_tenant_id
  AND RANDOM() < 0.6
LIMIT 150;

-- Özet
RAISE NOTICE 'Test verisi oluşturuldu:';
RAISE NOTICE '  Brands: %', (SELECT COUNT(*) FROM vehicle_brands WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  Models: %', (SELECT COUNT(*) FROM vehicle_models WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  YearRanges: %', (SELECT COUNT(*) FROM vehicle_year_ranges WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  Engines: %', (SELECT COUNT(*) FROM vehicle_engines WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  Products: %', (SELECT COUNT(*) FROM products WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  Variants: %', (SELECT COUNT(*) FROM product_variants WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  OEM References: %', (SELECT COUNT(*) FROM part_references WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  Fitments: %', (SELECT COUNT(*) FROM stock_card_fitments WHERE "TenantId" = demo_tenant_id);

END $$;

COMMIT;

-- Sonuç Tablosu
SELECT 'Brands' as "EntityType", COUNT(*) as "Count" FROM vehicle_brands WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'Models', COUNT(*) FROM vehicle_models WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'YearRanges', COUNT(*) FROM vehicle_year_ranges WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'Engines', COUNT(*) FROM vehicle_engines WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'Products', COUNT(*) FROM products WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'Variants', COUNT(*) FROM product_variants WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'OEM References', COUNT(*) FROM part_references WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL SELECT 'Fitments', COUNT(*) FROM stock_card_fitments WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid;

