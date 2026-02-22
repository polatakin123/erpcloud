-- Permissions table
CREATE TABLE permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(100) NOT NULL UNIQUE,
    name varchar(200) NOT NULL,
    description text,
    category varchar(50),
    created_at timestamp NOT NULL DEFAULT NOW()
);

-- User permissions junction
CREATE TABLE user_permissions (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    permission_id uuid NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    granted_at timestamp NOT NULL DEFAULT NOW(),
    granted_by uuid REFERENCES users(id),
    PRIMARY KEY (user_id, permission_id)
);

CREATE INDEX idx_user_permissions_user ON user_permissions(user_id);
CREATE INDEX idx_permissions_code ON permissions(code);

-- Seed permissions
INSERT INTO permissions (code, name, category) VALUES
('POS.VIEW', 'Tezgah Görüntüleme', 'POS'),
('POS.SELL', 'Satış Yapma', 'POS'),
('POS.REFUND', 'İade İşlemi', 'POS'),
('POS.DISCOUNT_APPLY', 'İndirim Uygulama', 'POS'),
('POS.PRICE_OVERRIDE', 'Fiyat Değiştirme', 'POS'),
('STOCK.VIEW', 'Stok Görüntüleme', 'STOCK'),
('STOCK.ADJUST', 'Stok Ayarlama', 'STOCK'),
('FINANCE.VIEW', 'Finans Görüntüleme', 'FINANCE'),
('FINANCE.COLLECT', 'Tahsilat Yapma', 'FINANCE'),
('ADMIN.USERS_MANAGE', 'Kullanıcı Yönetimi', 'ADMIN'),
('ADMIN.SETTINGS', 'Ayarlar', 'ADMIN'),
('ADMIN.REPORTS_ALL', 'Tüm Raporlar', 'ADMIN');
