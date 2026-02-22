// Demo User Seed Data
INSERT INTO organizations (id, code, name, tenant_id, created_at, created_by)
VALUES (
  'a1b2c3d4-e5f6-7890-abcd-111111111111',
  'DEMO',
  'Demo Şirketi',
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  NOW(),
  '00000000-0000-0000-0000-000000000001'
);

INSERT INTO branches (id, organization_id, code, name, address, tenant_id, created_at, created_by)
VALUES (
  'a1b2c3d4-e5f6-7890-abcd-222222222222',
  'a1b2c3d4-e5f6-7890-abcd-111111111111',
  'MAIN',
  'Merkez Şube',
  'Demo Adres',
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  NOW(),
  '00000000-0000-0000-0000-000000000001'
);

-- Demo user
-- Password: Demo123!
-- BCrypt hash: $2a$11$XGZ0Z8kZhMJZF8Xs9M9xKe8qP.rZ9QcP3zY8Xs9M9xKe8qP.rZ9Qc
INSERT INTO users (id, tenant_id, username, password_hash, email, full_name, role, is_active, created_at, created_by)
VALUES (
  '00000000-0000-0000-0000-000000000001',
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  'demo',
  '$2a$11$XGZ0Z8kZhMJZF8Xs9M9xKe8qP.rZ9QcP3zY8Xs9M9xKe8qP.rZ9Qc',
  'demo@erpcloud.local',
  'Demo User',
  'Dealer',
  true,
  NOW(),
  '00000000-0000-0000-0000-000000000001'
);
