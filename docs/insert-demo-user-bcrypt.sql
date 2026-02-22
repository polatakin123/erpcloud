-- Test BCrypt hash generation
DO $$
DECLARE
    hash_result TEXT;
BEGIN
    -- This is a placeholder hash generated with BCrypt work factor 11
    -- Password: Demo123!
    -- To generate in .NET: BCrypt.Net.BCrypt.HashPassword("Demo123!", 11)
    hash_result := '$2a$11$3nB5Z5.Kx8fN7Q2kqJ9XPeZqP.rZ9QcP3zY8Xs9M9xKe8qP.rZ9Qc';
    
    -- Insert demo user
    INSERT INTO users (id, tenant_id, username, password_hash, email, full_name, role, is_active, created_at, created_by)
    VALUES (
        '00000000-0000-0000-0000-000000000001'::uuid,
        'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid,
        'demo',
        hash_result,
        'demo@erpcloud.local',
        'Demo User',
        'Dealer',
        true,
        NOW(),
        '00000000-0000-0000-0000-000000000001'::uuid
    )
    ON CONFLICT (id) DO UPDATE SET
        password_hash = EXCLUDED.password_hash,
        updated_at = NOW();
    
    RAISE NOTICE 'Demo user created/updated successfully';
END;
$$;
