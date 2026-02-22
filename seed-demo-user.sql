-- Insert demo user with BCrypt hash
-- Password: Demo123!
-- Hash generated with BCrypt work factor 11

INSERT INTO users (id, tenant_id, username, password_hash, email, full_name, role, is_active, created_at, created_by)
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid,
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid,
    'demo',
    '$2a$11$N9qo8uLOickgx2ZMRZoMye.IjefOJu5YW8fJjL1hX3pz8M3qP7LZ2',
    'demo@erpcloud.local',
    'Demo User',
    'Dealer',
    true,
    NOW(),
    '00000000-0000-0000-0000-000000000001'::uuid
)
ON CONFLICT (id) DO NOTHING;

SELECT 'Demo user created: username=demo, password=Demo123!' as result;
