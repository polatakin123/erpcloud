-- Walk-in Customer (Günlük Müşteri) için Party oluşturma
-- TenantId: a1b2c3d4-e5f6-7890-abcd-ef1234567890

BEGIN;

DO $$
DECLARE
    demo_tenant_id uuid := 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
    zero_user_id uuid := '00000000-0000-0000-0000-000000000000';
    walkin_party_id uuid := '00000000-0000-0000-0000-000000000001';
BEGIN

-- Walk-in Party oluştur
INSERT INTO parties ("Id", "Code", "Name", "Type", "IsActive", "TenantId", "CreatedAt", "CreatedBy")
VALUES (
    walkin_party_id,
    'WALKIN',
    'Günlük Müşteri',
    'CUSTOMER',
    true,
    demo_tenant_id,
    NOW(),
    zero_user_id
)
ON CONFLICT ("Id") DO UPDATE
SET 
    "Code" = EXCLUDED."Code",
    "Name" = EXCLUDED."Name",
    "Type" = EXCLUDED."Type",
    "IsActive" = EXCLUDED."IsActive";

RAISE NOTICE 'Walk-in customer oluşturuldu: %', walkin_party_id;

END $$;

COMMIT;

-- Sonuç kontrolü
SELECT "Id", "Code", "Name", "Type", "IsActive"
FROM parties 
WHERE "Id" = '00000000-0000-0000-0000-000000000001'::uuid;
