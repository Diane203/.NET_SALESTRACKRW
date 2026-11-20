USE salestrackrw;

ALTER TABLE sales
	ADD COLUMN IF NOT EXISTS sale_type ENUM('Retail', 'Wholesale') NOT NULL DEFAULT 'Retail' AFTER user_id;

INSERT INTO users (username, password_hash, full_name, role)
VALUES ('cashier', SHA2('cashier123', 256), 'Sales Cashier', 'Cashier')
ON DUPLICATE KEY UPDATE username = username;
