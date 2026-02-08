-- Demo Tenant için Branch ve Warehouse Oluşturma
-- TenantId: a1b2c3d4-e5f6-7890-abcd-ef1234567890

BEGIN;

DO $$
DECLARE
    demo_tenant_id uuid := 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
    zero_user_id uuid := '00000000-0000-0000-0000-000000000000';
    demo_org_id uuid;
    main_branch_id uuid;
    main_warehouse_id uuid;
BEGIN

-- 0. Demo Tenant'ın Organization ID'sini bul
SELECT "Id" INTO demo_org_id 
FROM organizations 
WHERE "TenantId" = demo_tenant_id 
LIMIT 1;

IF demo_org_id IS NULL THEN
    RAISE EXCEPTION 'Demo tenant için organization bulunamadı!';
END IF;

RAISE NOTICE 'Organization ID: %', demo_org_id;

-- 1. Ana Şube (Main Branch)
INSERT INTO branches ("Id", "Code", "Name", "OrganizationId", "TenantId", "CreatedAt", "CreatedBy")
VALUES (
    gen_random_uuid(),
    'MAIN',
    'Ana Şube',
    demo_org_id,
    demo_tenant_id,
    NOW(),
    zero_user_id
)
ON CONFLICT DO NOTHING
RETURNING "Id" INTO main_branch_id;

-- If branch already exists, get its ID
IF main_branch_id IS NULL THEN
    SELECT "Id" INTO main_branch_id 
    FROM branches 
    WHERE "Code" = 'MAIN' AND "TenantId" = demo_tenant_id 
    LIMIT 1;
END IF;

RAISE NOTICE 'Branch ID: %', main_branch_id;

-- 2. Ana Depo (Main Warehouse)
INSERT INTO warehouses ("Id", "Code", "Name", "Type", "BranchId", "IsDefault", "TenantId", "CreatedAt", "CreatedBy")
VALUES (
    gen_random_uuid(),
    'WH001',
    'Ana Depo',
    'GENERAL',
    main_branch_id,
    true,
    demo_tenant_id,
    NOW(),
    zero_user_id
)
ON CONFLICT DO NOTHING
RETURNING "Id" INTO main_warehouse_id;

-- If warehouse already exists, get its ID
IF main_warehouse_id IS NULL THEN
    SELECT "Id" INTO main_warehouse_id 
    FROM warehouses 
    WHERE "Code" = 'WH001' AND "TenantId" = demo_tenant_id 
    LIMIT 1;
END IF;

RAISE NOTICE 'Warehouse ID: %', main_warehouse_id;

-- 3. Ek Depo (İkinci depo - opsiyonel)
INSERT INTO warehouses ("Id", "Code", "Name", "Type", "BranchId", "IsDefault", "TenantId", "CreatedAt", "CreatedBy")
VALUES (
    gen_random_uuid(),
    'WH002',
    'Yedek Depo',
    'GENERAL',
    main_branch_id,
    false,
    demo_tenant_id,
    NOW(),
    zero_user_id
)
ON CONFLICT DO NOTHING;

-- Özet
RAISE NOTICE 'Branch ve Warehouse oluşturuldu:';
RAISE NOTICE '  Branches: %', (SELECT COUNT(*) FROM branches WHERE "TenantId" = demo_tenant_id);
RAISE NOTICE '  Warehouses: %', (SELECT COUNT(*) FROM warehouses WHERE "TenantId" = demo_tenant_id);

END $$;

COMMIT;

-- Sonuç kontrolü
SELECT 'Branches' as "Type", "Code", "Name", "Id"
FROM branches 
WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid
UNION ALL
SELECT 'Warehouses' as "Type", "Code", "Name", "Id"
FROM warehouses
WHERE "TenantId" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid;
