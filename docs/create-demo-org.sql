-- Demo Organizasyon Oluşturma
BEGIN;

-- Demo organizasyon oluştur
INSERT INTO organizations ("Id", "Code", "Name", "TaxNumber", "TenantId", "CreatedAt", "CreatedBy")
VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid,
    'DEMO',
    'Demo Organizasyon',
    '1234567890',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid,
    NOW(),
    '00000000-0000-0000-0000-000000000000'::uuid
)
ON CONFLICT ("Id") DO UPDATE SET
    "Name" = EXCLUDED."Name";

COMMIT;

-- Oluşturulan organizasyonu göster
SELECT 
    "Id" as org_id,
    "TenantId" as tenant_id,
    "Code",
    "Name",
    "TaxNumber"
FROM organizations
WHERE "Code" = 'DEMO';

-- Bu TenantId'yi kullan: a1b2c3d4-e5f6-7890-abcd-ef1234567890
