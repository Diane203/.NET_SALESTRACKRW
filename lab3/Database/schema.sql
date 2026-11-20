CREATE DATABASE IF NOT EXISTS salestrackrw
	CHARACTER SET utf8mb4
	COLLATE utf8mb4_unicode_ci;

USE salestrackrw;

CREATE TABLE IF NOT EXISTS users (
	user_id INT AUTO_INCREMENT PRIMARY KEY,
	username VARCHAR(50) NOT NULL UNIQUE,
	password_hash CHAR(64) NOT NULL,
	full_name VARCHAR(120) NOT NULL,
	role ENUM('Administrator', 'Cashier') NOT NULL DEFAULT 'Cashier',
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS categories (
	category_id INT AUTO_INCREMENT PRIMARY KEY,
	name VARCHAR(100) NOT NULL UNIQUE,
	description VARCHAR(255) NULL,
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS products (
	product_id INT AUTO_INCREMENT PRIMARY KEY,
	product_code VARCHAR(40) NOT NULL UNIQUE,
	name VARCHAR(150) NOT NULL,
	category_id INT NOT NULL,
	purchase_price DECIMAL(15,2) NOT NULL,
	selling_price DECIMAL(15,2) NOT NULL,
	stock_quantity DECIMAL(15,3) NOT NULL DEFAULT 0,
	minimum_stock_level DECIMAL(15,3) NOT NULL DEFAULT 0,
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
	CONSTRAINT fk_products_category FOREIGN KEY (category_id) REFERENCES categories(category_id),
	CONSTRAINT chk_product_prices CHECK (purchase_price >= 0 AND selling_price >= 0),
	CONSTRAINT chk_product_stock CHECK (stock_quantity >= 0 AND minimum_stock_level >= 0)
);

CREATE TABLE IF NOT EXISTS stock_movements (
	movement_id BIGINT AUTO_INCREMENT PRIMARY KEY,
	product_id INT NOT NULL,
	movement_type ENUM('Received', 'Adjustment', 'Sale', 'Return') NOT NULL,
	quantity DECIMAL(15,3) NOT NULL,
	reason VARCHAR(255) NOT NULL,
	user_id INT NOT NULL,
	created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_stock_product FOREIGN KEY (product_id) REFERENCES products(product_id),
	CONSTRAINT fk_stock_user FOREIGN KEY (user_id) REFERENCES users(user_id),
	CONSTRAINT chk_stock_quantity CHECK (quantity > 0)
);

CREATE TABLE IF NOT EXISTS customers (
	customer_id INT AUTO_INCREMENT PRIMARY KEY,
	full_name VARCHAR(150) NOT NULL,
	telephone VARCHAR(30) NULL,
	address VARCHAR(255) NULL,
	is_active BOOLEAN NOT NULL DEFAULT TRUE,
	created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales (
	sale_id BIGINT AUTO_INCREMENT PRIMARY KEY,
	transaction_number VARCHAR(30) NOT NULL UNIQUE,
	customer_id INT NULL,
	user_id INT NOT NULL,
	sale_type ENUM('Retail', 'Wholesale') NOT NULL DEFAULT 'Retail',
	sale_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	subtotal DECIMAL(15,2) NOT NULL,
	amount_received DECIMAL(15,2) NOT NULL DEFAULT 0,
	payment_method ENUM('Cash', 'Mobile Money', 'Bank Transfer', 'Other') NULL,
	payment_status ENUM('Paid', 'Partially Paid', 'Credit') NOT NULL,
	notes VARCHAR(255) NULL,
	CONSTRAINT fk_sales_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
	CONSTRAINT fk_sales_user FOREIGN KEY (user_id) REFERENCES users(user_id),
	CONSTRAINT chk_sale_amounts CHECK (subtotal >= 0 AND amount_received >= 0 AND amount_received <= subtotal)
);

CREATE TABLE IF NOT EXISTS sale_items (
	sale_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
	sale_id BIGINT NOT NULL,
	product_id INT NOT NULL,
	quantity DECIMAL(15,3) NOT NULL,
	unit_price DECIMAL(15,2) NOT NULL,
	line_total DECIMAL(15,2) NOT NULL,
	CONSTRAINT fk_sale_items_sale FOREIGN KEY (sale_id) REFERENCES sales(sale_id),
	CONSTRAINT fk_sale_items_product FOREIGN KEY (product_id) REFERENCES products(product_id),
	CONSTRAINT chk_sale_item_values CHECK (quantity > 0 AND unit_price >= 0 AND line_total >= 0)
);

CREATE TABLE IF NOT EXISTS customer_payments (
	payment_id BIGINT AUTO_INCREMENT PRIMARY KEY,
	customer_id INT NOT NULL,
	sale_id BIGINT NULL,
	amount DECIMAL(15,2) NOT NULL,
	payment_method ENUM('Cash', 'Mobile Money', 'Bank Transfer', 'Other') NOT NULL,
	payment_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	user_id INT NOT NULL,
	notes VARCHAR(255) NULL,
	CONSTRAINT fk_customer_payment_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
	CONSTRAINT fk_customer_payment_sale FOREIGN KEY (sale_id) REFERENCES sales(sale_id),
	CONSTRAINT fk_customer_payment_user FOREIGN KEY (user_id) REFERENCES users(user_id),
	CONSTRAINT chk_customer_payment_amount CHECK (amount > 0)
);

CREATE TABLE IF NOT EXISTS expenses (
	expense_id BIGINT AUTO_INCREMENT PRIMARY KEY,
	expense_date DATE NOT NULL,
	category VARCHAR(100) NOT NULL,
	description VARCHAR(255) NOT NULL,
	amount DECIMAL(15,2) NOT NULL,
	user_id INT NOT NULL,
	created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_expense_user FOREIGN KEY (user_id) REFERENCES users(user_id),
	CONSTRAINT chk_expense_amount CHECK (amount > 0)
);

CREATE INDEX idx_products_name ON products(name);
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_stock_movements_product_date ON stock_movements(product_id, created_at);
CREATE INDEX idx_sales_date ON sales(sale_date);
CREATE INDEX idx_sales_customer ON sales(customer_id);
CREATE INDEX idx_customer_payments_customer ON customer_payments(customer_id);
CREATE INDEX idx_expenses_date ON expenses(expense_date);

INSERT INTO users (username, password_hash, full_name, role)
VALUES ('admin', SHA2('admin123', 256), 'System Administrator', 'Administrator')
ON DUPLICATE KEY UPDATE username = username;

INSERT INTO users (username, password_hash, full_name, role)
VALUES ('cashier', SHA2('cashier123', 256), 'Sales Cashier', 'Cashier')
ON DUPLICATE KEY UPDATE username = username;

INSERT INTO categories (name, description)
VALUES
	('General', 'General products'),
	('Beverages', 'Drinks and beverages'),
	('Food', 'Food products')
ON DUPLICATE KEY UPDATE name = name;
